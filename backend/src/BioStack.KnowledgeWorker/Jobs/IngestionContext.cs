namespace BioStack.KnowledgeWorker.Jobs;

using BioStack.KnowledgeWorker.Config;

/// <summary>
/// Scoped state for a single ingestion job run.
/// Carries dry-run mode, counters, scope hint, options, and the run-scoped logger.
/// Passed through the pipeline from job dispatch to individual reconciliation steps.
/// </summary>
public sealed class IngestionContext
{
    public bool DryRun { get; }
    public string? ScopeHint { get; }
    public WorkerOptions Options { get; }
    public ILogger Logger { get; }

    // ── Counters ──────────────────────────────────────────────────────────────
    private int _scanned;
    private int _created;
    private int _updated;
    private int _unchanged;
    private int _flaggedForReview;
    private int _failed;

    public int ScannedCount          => _scanned;
    public int CreatedCount          => _created;
    public int UpdatedCount          => _updated;
    public int UnchangedCount        => _unchanged;
    public int FlaggedForReviewCount => _flaggedForReview;
    public int FailedCount           => _failed;

    public IngestionContext(WorkerOptions options, ILogger logger, string? scopeHint = null)
    {
        Options    = options;
        Logger     = logger;
        DryRun     = options.DryRun;
        ScopeHint  = scopeHint;
    }

    public void IncrementScanned()          => Interlocked.Increment(ref _scanned);
    public void IncrementCreated()          => Interlocked.Increment(ref _created);
    public void IncrementUpdated()          => Interlocked.Increment(ref _updated);
    public void IncrementUnchanged()        => Interlocked.Increment(ref _unchanged);
    public void IncrementFlaggedForReview() => Interlocked.Increment(ref _flaggedForReview);
    public void IncrementFailed()           => Interlocked.Increment(ref _failed);

    /// <summary>
    /// Emits a structured summary log line at the end of a job run.
    /// </summary>
    public void LogSummary(string jobName)
    {
        Logger.LogInformation(
            "[{Job}] Run complete — Scanned={Scanned} Created={Created} Updated={Updated} " +
            "Unchanged={Unchanged} FlaggedForReview={Flagged} Failed={Failed} DryRun={DryRun}",
            jobName, _scanned, _created, _updated, _unchanged, _flaggedForReview, _failed, DryRun);
    }
}
