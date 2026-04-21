namespace BioStack.KnowledgeWorker.Workers;

using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>
/// One-shot hosted service. Resolves <see cref="RunMode"/>, dispatches the matching
/// <see cref="IIngestionJob"/>, and requests application shutdown when the job completes.
/// The worker is designed for Azure Container App Jobs: Azure decides when to run,
/// the app decides what kind of run it is. There is no internal long-running loop.
/// </summary>
public sealed class IngestionWorker : BackgroundService
{
    private readonly ILogger<IngestionWorker>  _logger;
    private readonly WorkerOptions             _options;
    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly IHostApplicationLifetime  _lifetime;
    private readonly IHostEnvironment          _env;

    public IngestionWorker(
        ILogger<IngestionWorker>  logger,
        IOptions<WorkerOptions>   options,
        IServiceScopeFactory      scopeFactory,
        IHostApplicationLifetime  lifetime,
        IHostEnvironment          env)
    {
        _logger       = logger;
        _options      = options.Value;
        _scopeFactory = scopeFactory;
        _lifetime     = lifetime;
        _env          = env;
    }

    /// <summary>Exit code surfaced via <c>Environment.ExitCode</c> for the Azure job runner.</summary>
    public static int LastExitCode { get; private set; } = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RunMode mode;
        try
        {
            mode = ProductionSafetyGuard.ResolveRunMode(_options, _env.IsProduction());
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[IngestionWorker] Fatal: could not resolve RunMode.");
            LastExitCode = 2;
            _lifetime.StopApplication();
            return;
        }

        _logger.LogInformation(
            "[IngestionWorker] Starting one-shot — RunMode={Mode} DryRun={DryRun} MaxBatchSize={Batch} " +
            "SeedFile={File} ScopeHint={ScopeHint}",
            mode, _options.DryRun, _options.MaxBatchSize,
            _options.SeedFilePath, _options.ScopeHint ?? "(none)");

        try
        {
            await RunJobAsync(mode, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[IngestionWorker] Run cancelled.");
            LastExitCode = 130;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[IngestionWorker] Unhandled exception during job execution.");
            LastExitCode = 1;
        }

        _logger.LogInformation("[IngestionWorker] One-shot complete — requesting shutdown.");
        _lifetime.StopApplication();
    }

    private async Task RunJobAsync(RunMode mode, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        IIngestionJob job = mode switch
        {
            RunMode.Seed    => scope.ServiceProvider.GetRequiredService<ISeedJob>(),
            RunMode.Refresh => scope.ServiceProvider.GetRequiredService<IRefreshJob>(),
            _               => throw new InvalidOperationException($"Unknown RunMode: {mode}"),
        };

        var runLogger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(mode.ToString() + "Job");

        var context = new Jobs.IngestionContext(_options, runLogger, _options.ScopeHint);

        var result = await job.RunAsync(context, ct);

        LogResult(mode.ToString(), result);
        if (!result.Success) LastExitCode = 1;
    }

    private static void LogResult(string jobName, JobRunResult result)
    {
        if (result.Success) return;

        Console.Error.WriteLine(
            $"[{jobName}] Completed with failures — Failed={result.FailedCount} Error={result.ErrorMessage ?? "(none)"}");
    }
}
