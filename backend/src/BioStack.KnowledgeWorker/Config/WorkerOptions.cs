namespace BioStack.KnowledgeWorker.Config;

/// <summary>
/// Typed configuration for the knowledge ingestion worker.
/// Bound from the "Worker" configuration section.
/// </summary>
public sealed class WorkerOptions
{
    /// <summary>
    /// When true, the worker computes and logs all deltas but does not write any changes to the database.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Which job to execute on this invocation. One-shot: the worker resolves this,
    /// runs exactly one pass, and exits. Azure Container App Job scheduling decides
    /// how often to invoke. Parsed from <c>Worker:RunMode</c> (string: "Seed" | "Refresh").
    /// If unset, <see cref="SeedOnStartup"/> is consulted as a legacy fallback.
    /// </summary>
    public RunMode? RunMode { get; set; }

    /// <summary>
    /// Legacy fallback: when true and <see cref="RunMode"/> is unset, the worker resolves to <see cref="Config.RunMode.Seed"/>.
    /// Prefer setting <see cref="RunMode"/> explicitly; this flag is retained for backward config compatibility only.
    /// </summary>
    public bool SeedOnStartup { get; set; } = true;

    /// <summary>
    /// Retained for configuration-compatibility. The worker no longer runs a long-running loop;
    /// schedule Refresh runs via Azure Container App Job cron instead.
    /// </summary>
    public int RefreshIntervalHours { get; set; } = 24;

    /// <summary>
    /// Maximum number of substances processed per batch within a single job run.
    /// </summary>
    public int MaxBatchSize { get; set; } = 50;

    /// <summary>
    /// Path to the curated substance identity seed file (JSON).
    /// Relative to the worker's working directory or absolute.
    /// </summary>
    public string SeedFilePath { get; set; } = "Seeds/substances-seed.json";

    /// <summary>
    /// Optional canonical name or class tag used to scope a targeted job run.
    /// When set, only substances matching this hint are processed.
    /// </summary>
    public string? ScopeHint { get; set; }

    /// <summary>
    /// Trust threshold values used by the publish decision engine.
    /// </summary>
    public TrustThresholdOptions TrustThresholds { get; set; } = new();

    /// <summary>
    /// Minimum log level for the worker process. Parsed at startup.
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}

/// <summary>
/// Per-trust-class confidence thresholds.
/// A candidate must meet its class threshold to be eligible for auto-publish.
/// </summary>
public sealed class TrustThresholdOptions
{
    /// <summary>Class A field minimum confidence for auto-upsert.</summary>
    public decimal ClassA { get; set; } = 0.70m;

    /// <summary>Class B field minimum confidence for auto-upsert (empty field only).</summary>
    public decimal ClassB { get; set; } = 0.85m;
}
