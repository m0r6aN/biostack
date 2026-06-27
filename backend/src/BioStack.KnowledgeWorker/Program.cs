using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using BioStack.KnowledgeWorker.Pipeline.Graph;
using BioStack.KnowledgeWorker.Workers;
using BioStack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

static RunMode ResolveConfiguredRunMode(IConfiguration configuration, bool isProduction)
{
    var options = new WorkerOptions();
    configuration.GetSection("Worker").Bind(options);
    return ProductionSafetyGuard.ResolveRunMode(options, isProduction);
}

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();

        if (Enum.TryParse<LogLevel>(
                context.Configuration["Worker:LogLevel"],
                ignoreCase: true,
                out var level))
        {
            logging.SetMinimumLevel(level);
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Information);
        }
    })
    .ConfigureServices((context, services) =>
    {
        // ── Typed config ─────────────────────────────────────────────────────
        services.Configure<WorkerOptions>(context.Configuration.GetSection("Worker"));
        services.AddSingleton(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorkerOptions>>().Value);

        var mode = ResolveConfiguredRunMode(context.Configuration, context.HostingEnvironment.IsProduction());

        // ── Provider policy (Npgsql-only, fail-closed in Production) ─────────
        var isProd = context.HostingEnvironment.IsProduction();
        if (mode != RunMode.Research &&
            mode != RunMode.PromotionImportDryRun &&
            mode != RunMode.ProtocolIntelligenceEvaluation)
        {
            var connectionString = ProductionSafetyGuard.EnforcePostgresOnly(context.Configuration, isProd);

            services.AddDbContext<BioStackDbContext>(options =>
                options.UseNpgsql(connectionString,
                    npgsql => npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null)));
        }

        // ── Ingestion pipeline ───────────────────────────────────────────────
        services.AddSingleton<ISubstanceRecordLoader,     SubstanceRecordLoader>();
        services.AddSingleton<ISubstanceRecordValidator>(_ =>
        {
            var schemaPath = Path.Combine(AppContext.BaseDirectory, "Schemas", "substance-record.schema.json");
            return SubstanceRecordValidator.LoadFromFile(schemaPath);
        });
        services.AddSingleton<ISubstanceRecordNormalizer, SubstanceRecordNormalizer>();
        services.AddSingleton<ITrustGate,                 TrustGate>();
        services.AddSingleton<ISubstanceCanonicalizer,    SubstanceCanonicalizer>();
        services.AddSingleton<IIngestionPipeline,         IngestionPipeline>();
        services.AddSingleton<IResearchArtifactLoader,    ResearchArtifactLoader>();
        services.AddSingleton<IResearchArtifactValidator>(_ =>
            ResearchArtifactValidator.LoadFromDirectory(Path.Combine(AppContext.BaseDirectory, "Schemas")));
        services.AddSingleton<IEvidencePacketPreprocessor, EvidencePacketPreprocessor>();
        services.AddSingleton<ISourceRegistryAuthorizer, SourceRegistryAuthorizer>();
        services.AddSingleton<IEvidencePacketSubstanceRecordCompiler, EvidencePacketSubstanceRecordCompiler>();
        services.AddSingleton<IResearchReviewQueueBuilder, ResearchReviewQueueBuilder>();
        services.AddSingleton<IResearchSummaryBuilder, ResearchSummaryBuilder>();
        services.AddSingleton<IResearchTaskQueueBuilder, ResearchTaskQueueBuilder>();
        services.AddSingleton<IPromotionManifestBuilder, PromotionManifestBuilder>();
        services.AddSingleton<IRelationshipPacketAuthorizer, RelationshipPacketAuthorizer>();
        services.AddSingleton<ICompoundGraphBuilder, CompoundGraphBuilder>();
        services.AddSingleton<IReviewResolutionPlanBuilder, ReviewResolutionPlanBuilder>();
        services.AddSingleton<IPromotionExporter, PromotionExporter>();
        services.AddSingleton<IPromotionImportPreviewBuilder, PromotionImportPreviewBuilder>();

        // ── Persistence + jobs (scoped: share DbContext within a single run) ─
        if (mode != RunMode.Research && mode != RunMode.PromotionImportDryRun)
        {
            services.AddScoped<IKnowledgeSource, DatabaseKnowledgeSource>();
            services.AddScoped<ICompoundInteractionHintRepository, CompoundInteractionHintRepository>();
        }
        services.AddScoped<ISeedJob,         SeedJob>();
        services.AddScoped<IRefreshJob,      RefreshJob>();
        services.AddScoped<IResearchJob,     ResearchJob>();
        services.AddScoped<IPromotionImportDryRunJob, PromotionImportDryRunJob>();
        services.AddScoped<IProtocolIntelligenceEvaluationWorker, ProtocolIntelligenceEvaluationWorker>();

        // ── Hosted one-shot worker ───────────────────────────────────────────
        services.AddHostedService<IngestionWorker>();
    });

var host = builder.Build();
var effectiveMode = ResolveConfiguredRunMode(host.Services.GetRequiredService<IConfiguration>(),
    host.Services.GetRequiredService<IHostEnvironment>().IsProduction());

// ── Postgres connectivity check ──────────────────────────────────────────────
// Fail fast if the database is unreachable before handing off to the hosted service.
var startupLogger = host.Services.GetRequiredService<ILogger<Program>>();
if (effectiveMode != RunMode.Research &&
    effectiveMode != RunMode.PromotionImportDryRun &&
    effectiveMode != RunMode.ProtocolIntelligenceEvaluation)
{
    try
    {
        await using var startupScope = host.Services.CreateAsyncScope();
        var db = startupScope.ServiceProvider.GetRequiredService<BioStackDbContext>();

        startupLogger.LogInformation("[Startup] Verifying Postgres connectivity...");
        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
            throw new InvalidOperationException("CanConnectAsync returned false — database is unreachable.");

        startupLogger.LogInformation("[Startup] Postgres connectivity verified.");
        startupLogger.LogInformation("[Startup] Ensuring database schema exists...");
        await db.Database.EnsureCreatedAsync();
        await InteractionSchemaBootstrapper.EnsureCompoundInteractionHintsTableAsync(db);
        var hintRepository = startupScope.ServiceProvider.GetRequiredService<ICompoundInteractionHintRepository>();
        await CompoundInteractionHintCatalog.SeedDefaultsAsync(hintRepository);
        startupLogger.LogInformation("[Startup] Database schema ready.");
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(ex, "[Startup] Fatal: cannot connect to Postgres. Worker will not start.");
        return 2;
    }
}
else
{
    startupLogger.LogInformation("[Startup] {Mode} mode selected — skipping Postgres connectivity check.", effectiveMode);
}

await host.RunAsync();
return IngestionWorker.LastExitCode;
