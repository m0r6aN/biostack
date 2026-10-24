namespace BioStack.KnowledgeWorker.Jobs;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;

public interface IResearchPlanningJob : IIngestionJob { }

/// <summary>
/// DB-free scheduled planner (RunMode=ResearchPlanning). Reads the seed corpus and the
/// discovery watchlist, writes one schema-valid research-request batch for update checks on
/// existing entries and new-compound discovery. Scheduled by Azure cron (infra/azure/knowledge-schedules.bicep)
/// or by the worker loop (<see cref="WorkerOptions.LoopEnabled"/>).
/// </summary>
public sealed class ResearchPlanningJob : IResearchPlanningJob
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly WorkerOptions _options;
    private readonly IResearchArtifactValidator _validator;
    private readonly TimeProvider _clock;

    public ResearchPlanningJob(WorkerOptions options, IResearchArtifactValidator validator, TimeProvider? clock = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _clock = clock ?? TimeProvider.System;
    }

    public Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        var seedPath = Resolve(_options.SeedFilePath);
        if (!File.Exists(seedPath)) return Task.FromResult(JobRunResult.Failure($"Seed not found at '{seedPath}'."));
        var seed = JsonNode.Parse(File.ReadAllText(seedPath)) as JsonArray
                   ?? throw new InvalidOperationException($"Expected seed JSON array at '{seedPath}'.");

        var watchlist = Array.Empty<DiscoveryCandidate>() as IReadOnlyList<DiscoveryCandidate>;
        if (!string.IsNullOrWhiteSpace(_options.ResearchDiscoveryWatchlistPath))
        {
            var watchPath = Resolve(_options.ResearchDiscoveryWatchlistPath);
            if (File.Exists(watchPath))
                watchlist = ResearchSchedulePlanner.ParseWatchlist(JsonNode.Parse(File.ReadAllText(watchPath)));
            else
                context.Logger.LogWarning("[ResearchPlanning] Discovery watchlist not found at {Path}; discovery skipped.", watchPath);
        }

        var now = _clock.GetUtcNow();
        var outputDir = Resolve(_options.ResearchPlanningOutputDirectory);
        var earlier = Directory.Exists(outputDir)
            ? Directory.GetFiles(outputDir, "research-request-scheduled-*.json").Select(f => JsonNode.Parse(File.ReadAllText(f)))
            : Enumerable.Empty<JsonNode?>();
        var recent = ResearchSchedulePlanner.RecentUpdateRequests(earlier);
        var batch = ResearchSchedulePlanner.Plan(seed, watchlist, new ResearchScheduleSettings(
            _options.ResearchPlanningIncludeUpdates,
            _options.ResearchPlanningIncludeDiscovery,
            _options.ResearchUpdateCheckStaleDays,
            _options.MaxBatchSize,
            _options.MaxBatchSize), now, recent);

        for (var i = 0; i < seed.Count; i++) context.IncrementScanned();
        if (batch is null)
        {
            context.Logger.LogInformation("[ResearchPlanning] Nothing due — no request batch written.");
            return Task.FromResult(JobRunResult.FromContext(context));
        }

        var validation = _validator.Validate(ResearchArtifactKind.ResearchRequestBatch, batch);
        if (!validation.IsValid)
        {
            return Task.FromResult(JobRunResult.Failure(
                "Planned research-request batch failed schema validation: " +
                string.Join("; ", validation.Errors.Select(e => e.ToString()))));
        }

        var count = ((JsonArray)batch["requests"]!).Count;
        for (var i = 0; i < count; i++) context.IncrementCreated();

        if (context.DryRun)
        {
            context.Logger.LogInformation("[ResearchPlanning] DryRun — {Count} request(s) planned, nothing written.", count);
            return Task.FromResult(JobRunResult.FromContext(context));
        }

        Directory.CreateDirectory(outputDir);
        var file = Path.Combine(outputDir,
            $"research-request-{batch["batch"]!["batchId"]!.GetValue<string>()}.json");
        File.WriteAllText(file, batch.ToJsonString(JsonOptions));
        context.Logger.LogInformation("[ResearchPlanning] Wrote {Count} request(s) to {File} at {Now}.",
            count, file, now.ToString("O", CultureInfo.InvariantCulture));
        return Task.FromResult(JobRunResult.FromContext(context));
    }

    private static string Resolve(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
}
