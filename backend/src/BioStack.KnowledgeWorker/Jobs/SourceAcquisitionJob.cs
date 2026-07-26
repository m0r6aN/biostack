namespace BioStack.KnowledgeWorker.Jobs;

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;

public interface ISourceAcquisitionJob : IIngestionJob;

public sealed class SourceAcquisitionJob : ISourceAcquisitionJob
{
    private readonly WorkerOptions _options;
    private readonly IResearchArtifactValidator _validator;
    private readonly ISourceAcquisitionPlanBuilder _planBuilder;
    private readonly ISourceAcquisitionExecutionPreflight _preflight;
    private readonly ISourceAcquisitionAdapterFactory _adapterFactory;
    private readonly ISourceAcquisitionRunner _runner;
    private readonly bool _isProduction;

    public SourceAcquisitionJob(
        WorkerOptions options,
        IResearchArtifactValidator validator,
        ISourceAcquisitionPlanBuilder planBuilder,
        ISourceAcquisitionExecutionPreflight preflight,
        ISourceAcquisitionAdapterFactory adapterFactory,
        ISourceAcquisitionRunner runner)
        : this(
            options,
            validator,
            planBuilder,
            preflight,
            adapterFactory,
            runner,
            isProduction: false)
    {
    }

    public SourceAcquisitionJob(
        WorkerOptions options,
        IResearchArtifactValidator validator,
        ISourceAcquisitionPlanBuilder planBuilder,
        ISourceAcquisitionExecutionPreflight preflight,
        ISourceAcquisitionAdapterFactory adapterFactory,
        ISourceAcquisitionRunner runner,
        IHostEnvironment environment)
        : this(
            options,
            validator,
            planBuilder,
            preflight,
            adapterFactory,
            runner,
            environment?.IsProduction()
                ?? throw new ArgumentNullException(nameof(environment)))
    {
    }

    private SourceAcquisitionJob(
        WorkerOptions options,
        IResearchArtifactValidator validator,
        ISourceAcquisitionPlanBuilder planBuilder,
        ISourceAcquisitionExecutionPreflight preflight,
        ISourceAcquisitionAdapterFactory adapterFactory,
        ISourceAcquisitionRunner runner,
        bool isProduction)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _planBuilder = planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
        _preflight = preflight ?? throw new ArgumentNullException(nameof(preflight));
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _isProduction = isProduction;
    }

    public async Task<JobRunResult> RunAsync(
        IngestionContext context,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(_options, _isProduction);

        var request = await LoadValidatedAsync(
            _options.SourceAcquisitionResearchRequestPath!,
            ResearchArtifactKind.ResearchRequestBatch,
            cancellationToken);
        var decisions = await LoadValidatedAsync(
            _options.SourceAcquisitionDecisionPath!,
            ResearchArtifactKind.SourceAuthorizationDecisionBatch,
            cancellationToken);
        var registry = await LoadValidatedAsync(
            _options.SourceAcquisitionRegistryPath!,
            ResearchArtifactKind.SourceRegistry,
            cancellationToken);

        var registrySha256 = Sha256(registry.Bytes);
        var bindings = new SourceAcquisitionInputBindings(
            Sha256(request.Bytes),
            Sha256(decisions.Bytes),
            registrySha256);
        var plan = _planBuilder.Build(
            request.Node,
            decisions.Node,
            registry.Node,
            registrySha256,
            RecommendedOfficialSourcePlanningAdapters.All);
        var preflight = _preflight.Evaluate(
            plan,
            _adapterFactory.Descriptors,
            SourceAcquisitionCampaignExpectation.CurrentRecommendedSevenActivation);
        if (!preflight.CanActivate
            || preflight.UniqueRequestCount != 70
            || preflight.IntentCount != 490
            || preflight.ReadyCount != 490
            || preflight.BlockedCount != 0
            || preflight.SourceCount != 7)
        {
            throw new InvalidOperationException(
                "Source acquisition preflight did not match 70/490/490/0/7.");
        }

        var adapters = _adapterFactory.Create(registrySha256, _options);
        var configuration = new SourceAcquisitionRuntimeConfiguration(
            _options.ResearchOutputDirectory,
            _options.SourceAcquisitionCycleId!,
            _options.SourceAcquisitionCandidateRetentionDays!.Value,
            _options.SourceAcquisitionReceiptRetentionDays!.Value,
            _options.SourceAcquisitionStorageProvider,
            _options.SourceAcquisitionBlobServiceUri,
            _options.SourceAcquisitionBlobContainerName,
            _options.SourceAcquisitionBlobPrefix,
            _options.SourceAcquisitionManagedIdentityClientId,
            _isProduction);
        var run = await _runner.RunAsync(
            plan,
            preflight,
            adapters,
            bindings,
            configuration,
            cancellationToken);

        for (var index = 0; index < run.Manifest.IntentCount; index++)
        {
            context.IncrementScanned();
        }
        for (var index = 0;
             index < run.Manifest.CompletedCount;
             index++)
        {
            context.IncrementCreated();
        }
        for (var index = 0; index < run.Manifest.NoMatchCount; index++)
        {
            context.IncrementUnchanged();
        }
        for (var index = 0;
             index < run.Manifest.CompletedCount
                     + run.Manifest.NoMatchCount
                     + run.Manifest.ManualReviewPendingCount
                     + run.Manifest.ErrorCount;
             index++)
        {
            context.IncrementFlaggedForReview();
        }
        if (!run.Manifest.Complete)
        {
            context.IncrementFailed();
        }

        context.LogSummary(nameof(SourceAcquisitionJob));
        return JobRunResult.FromContext(context) with
        {
            ErrorMessage = run.Manifest.Complete
                ? null
                : "Source acquisition halted incomplete; inspect the bounded run manifest.",
        };
    }

    private async Task<BoundedJsonArtifact> LoadValidatedAsync(
        string configuredPath,
        ResearchArtifactKind kind,
        CancellationToken cancellationToken)
    {
        var path = ResolveInputPath(configuredPath);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Required {kind} input does not exist.");
        }

        var bytes = await ReadBoundedAsync(path, cancellationToken);
        JsonNode node;
        try
        {
            node = JsonNode.Parse(bytes)
                   ?? throw new InvalidOperationException(
                       $"Required {kind} input parsed to null.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Required {kind} input is not valid JSON.");
        }

        var validation = _validator.Validate(kind, node);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Required {kind} input failed schema validation.");
        }
        return new BoundedJsonArtifact(bytes, node);
    }

    private static async Task<byte[]> ReadBoundedAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > SourceAcquisitionRuntimeLimits.MaximumInputBytes)
        {
            throw new InvalidOperationException(
                "A source-acquisition input exceeded the fixed size limit.");
        }

        using var buffer = new MemoryStream((int)stream.Length);
        var chunk = new byte[16 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read
                > SourceAcquisitionRuntimeLimits.MaximumInputBytes)
            {
                throw new InvalidOperationException(
                    "A source-acquisition input exceeded the fixed size limit.");
            }
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        return buffer.ToArray();
    }

    private static void ValidateConfiguration(
        WorkerOptions options,
        bool isProduction)
    {
        if (string.IsNullOrWhiteSpace(options.SourceAcquisitionResearchRequestPath)
            || string.IsNullOrWhiteSpace(options.SourceAcquisitionDecisionPath)
            || string.IsNullOrWhiteSpace(options.SourceAcquisitionRegistryPath))
        {
            throw new InvalidOperationException(
                "Source acquisition requires explicit request, decision, and registry paths.");
        }
        if (string.IsNullOrWhiteSpace(options.SourceAcquisitionCycleId))
        {
            throw new InvalidOperationException(
                "Source acquisition requires an explicit caller-supplied cycle ID.");
        }
        if (options.SourceAcquisitionCandidateRetentionDays is null or <= 0
            || options.SourceAcquisitionReceiptRetentionDays is null or <= 0
            || options.SourceAcquisitionCandidateRetentionDays
            > SourceAcquisitionRuntimeLimits.MaximumRetentionDays
            || options.SourceAcquisitionReceiptRetentionDays
            > SourceAcquisitionRuntimeLimits.MaximumRetentionDays)
        {
            throw new InvalidOperationException(
                "Source acquisition requires positive explicit retention values.");
        }
        if (isProduction
            && (!string.Equals(
                    options.SourceAcquisitionStorageProvider,
                    "AzureBlob",
                    StringComparison.OrdinalIgnoreCase)
                || options.SourceAcquisitionCandidateRetentionDays != 30
                || options.SourceAcquisitionReceiptRetentionDays != 30))
        {
            throw new InvalidOperationException(
                "Production source acquisition requires AzureBlob storage and exact 30/30-day retention.");
        }
        if (!string.IsNullOrWhiteSpace(options.SourceAcquisitionPubMedApiKey))
        {
            throw new InvalidOperationException(
                "PubMed API-key configuration is not approved for this runtime.");
        }
        if (string.IsNullOrWhiteSpace(options.SourceAcquisitionPubMedTool)
            || string.IsNullOrWhiteSpace(options.SourceAcquisitionPubMedContactEmail))
        {
            throw new InvalidOperationException(
                "PubMed tool name and contact email are required.");
        }
    }

    private static string ResolveInputPath(string path) =>
        Path.GetFullPath(
            Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, path));

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed record BoundedJsonArtifact(byte[] Bytes, JsonNode Node);
}
