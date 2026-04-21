namespace BioStack.KnowledgeWorker.Config;

/// <summary>
/// The worker is an Azure Container App Job: it resolves a single RunMode,
/// executes exactly one pass, and exits. Scheduling belongs to Azure, not
/// to an internal long-running loop.
/// </summary>
public enum RunMode
{
    /// <summary>
    /// One-shot initial ingestion / backfill from <c>Seeds/substances-seed.json</c>.
    /// Safe to run once per environment promotion.
    /// </summary>
    Seed = 1,

    /// <summary>
    /// Periodic upsert/update pass. Scheduled externally (e.g. Azure Container App Job cron).
    /// Idempotent: no-ops against unchanged records.
    /// </summary>
    Refresh = 2,
}
