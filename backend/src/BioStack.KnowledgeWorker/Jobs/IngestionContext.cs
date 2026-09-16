namespace BioStack.KnowledgeWorker.Jobs;

using System.Text;
using BioStack.KnowledgeWorker.Config;

/// <summary>One row of the per-record DryRun/Refresh plan table.</summary>
public sealed record PromotionPlanRow(string CanonicalName, string Action, string Reason);

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
    private int _skippedUnpromoted;
    private int _flaggedForReview;
    private int _failed;

    public int ScannedCount           => _scanned;
    public int CreatedCount           => _created;
    public int UpdatedCount           => _updated;
    public int UnchangedCount         => _unchanged;
    public int SkippedUnpromotedCount => _skippedUnpromoted;
    public int FlaggedForReviewCount  => _flaggedForReview;
    public int FailedCount            => _failed;

    private readonly List<PromotionPlanRow> _planRows = new();

    /// <summary>
    /// Per-record plan built up during the run — one row per scanned record (including rejected and failed records),
    /// recording what happened (or would happen, in DryRun) and why. Printed as a table
    /// at the end of a DryRun so the summary counters are never the only way to read the
    /// diff a Refresh is about to make.
    /// </summary>
    public IReadOnlyList<PromotionPlanRow> PlanRows => _planRows;

    public IngestionContext(WorkerOptions options, ILogger logger, string? scopeHint = null)
    {
        Options    = options;
        Logger     = logger;
        DryRun     = options.DryRun;
        ScopeHint  = scopeHint;
    }

    public void IncrementScanned()           => Interlocked.Increment(ref _scanned);
    public void IncrementCreated()           => Interlocked.Increment(ref _created);
    public void IncrementUpdated()           => Interlocked.Increment(ref _updated);
    public void IncrementUnchanged()         => Interlocked.Increment(ref _unchanged);
    public void IncrementSkippedUnpromoted() => Interlocked.Increment(ref _skippedUnpromoted);
    public void IncrementFlaggedForReview()  => Interlocked.Increment(ref _flaggedForReview);
    public void IncrementFailed()            => Interlocked.Increment(ref _failed);

    public void RecordPlanRow(string canonicalName, string action, string reason)
    {
        lock (_planRows)
        {
            _planRows.Add(new PromotionPlanRow(canonicalName, action, reason));
        }
    }

    /// <summary>
    /// Emits a structured summary log line at the end of a job run. In DryRun, the
    /// counters are honest would-be dispositions (WouldCreate/WouldUpdate/Unchanged/
    /// SkippedUnpromoted) computed by actually comparing against the database read-only —
    /// never the always-zero placeholders a naive "log and return" DryRun would produce —
    /// followed by the full per-record plan table.
    /// </summary>
    public void LogSummary(string jobName)
    {
        if (DryRun)
        {
            Logger.LogInformation(
                "[{Job}] DRY-RUN summary — Scanned={Scanned} WouldCreate={Created} WouldUpdate={Updated} "
                + "Unchanged={Unchanged} SkippedUnpromoted={SkippedUnpromoted} FlaggedForReview={Flagged} "
                + "Failed={Failed} (no writes were made)",
                jobName, _scanned, _created, _updated, _unchanged, _skippedUnpromoted, _flaggedForReview, _failed);
            Logger.LogInformation("[{Job}] DRY-RUN per-record plan:\n{Table}", jobName, BuildPlanTable());
            return;
        }

        Logger.LogInformation(
            "[{Job}] Run complete — Scanned={Scanned} Created={Created} Updated={Updated} " +
            "Unchanged={Unchanged} SkippedUnpromoted={SkippedUnpromoted} FlaggedForReview={Flagged} " +
            "Failed={Failed} DryRun={DryRun}",
            jobName, _scanned, _created, _updated, _unchanged, _skippedUnpromoted, _flaggedForReview, _failed, DryRun);
    }

    /// <summary>Renders <see cref="PlanRows"/> as a simple fixed-width text table.</summary>
    public string BuildPlanTable()
    {
        if (_planRows.Count == 0)
        {
            return "(no records scanned)";
        }

        var nameWidth   = Math.Max(4, _planRows.Max(r => r.CanonicalName.Length));
        var actionWidth = Math.Max(6, _planRows.Max(r => r.Action.Length));

        var sb = new StringBuilder();
        sb.Append("Name".PadRight(nameWidth)).Append("  ")
          .Append("Action".PadRight(actionWidth)).Append("  ")
          .Append("Reason").Append('\n');
        sb.Append(new string('-', nameWidth)).Append("  ")
          .Append(new string('-', actionWidth)).Append("  ")
          .Append(new string('-', 6)).Append('\n');

        foreach (var row in _planRows)
        {
            sb.Append(row.CanonicalName.PadRight(nameWidth)).Append("  ")
              .Append(row.Action.PadRight(actionWidth)).Append("  ")
              .Append(row.Reason).Append('\n');
        }

        return sb.ToString();
    }
}
