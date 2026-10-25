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

    /// <summary>
    /// Governed, DB-free source acquisition. Loads the exact approved research request,
    /// source-decision, and registry artifacts, performs the strict activation preflight,
    /// and writes normalized review-required candidates and minimal receipts only.
    /// </summary>
    SourceAcquisition = 6,

    /// <summary>
    /// Source-free and database-free retention enforcement for source-acquisition
    /// artifacts. It never invokes an acquisition adapter or a source endpoint.
    /// </summary>
    SourceAcquisitionRetention = 7,

    /// <summary>
    /// Scheduled, database-free research planning. Reads the seed corpus and the discovery
    /// watchlist and writes a research-request batch of update checks for existing entries
    /// and new-compound discovery requests.
    /// </summary>
    ResearchPlanning = 8,

    /// <summary>
    /// Database-free two-reviewer AI council. Reviews due records with two configured models,
    /// writes consensus decisions and escalate-to-owner markers as a review-decision batch,
    /// owner escalations with distinct options, and a known-risk log of legal items.
    /// </summary>
    CouncilReview = 9,

    /// <summary>
    /// Database-free open discovery. Queries ClinicalTrials.gov v2, PubMed E-utilities, openFDA and
    /// bioRxiv/medRxiv for compounds and therapies in the scope taxonomy, matches names and code names
    /// against the corpus, and writes a report, a research-request batch and draft records.
    /// </summary>
    Discovery = 10,

    /// <summary>
    /// Database-free Hype Gap signal collection (growth spec). Queries the configured public-volume
    /// sources (X recent counts, YouTube Data API) for compounds with public claims and writes an
    /// editor-input report. Never edits records or sets hype scores. No-op with no source configured.
    /// </summary>
    HypeSignals = 11,
}

public static class WorkerRunModePolicy
{
    public static bool IsDatabaseFree(RunMode mode) =>
        mode is RunMode.Research
            or RunMode.SourceAcquisition
            or RunMode.SourceAcquisitionRetention
            or RunMode.PromotionImportDryRun
            or RunMode.ProtocolIntelligenceEvaluation
            or RunMode.ResearchPlanning
            or RunMode.CouncilReview
            or RunMode.Discovery
            or RunMode.HypeSignals;
}
