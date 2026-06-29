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

    /// <summary>
    /// Local/offline research artifact processing. Validates candidate/source/evidence
    /// JSON, compiles draft substance records, and emits review/report artifacts without
    /// connecting to or writing a database.
    /// </summary>
    Research = 3,

    /// <summary>
    /// Offline promotion-import dry run. Consumes promotion export + import preview
    /// artifacts, validates safety gates, emits a dry-run report, and performs no
    /// database writes.
    /// </summary>
    PromotionImportDryRun = 4,

    /// <summary>
    /// Offline Protocol Intelligence evaluation. Loads the canonical
    /// <c>research/protocol-intelligence/*.json</c> artifacts, exercises the build-time
    /// <c>ProtocolIntelligenceGate</c> (fail-closed conformance plus any supplied candidate
    /// artifacts), and emits a deterministic evaluation report. Performs no database writes
    /// and exposes no runtime surface; doctrine enforcement is delegated to DoctrineSanitizer.
    /// </summary>
    ProtocolIntelligenceEvaluation = 5,
}
