namespace BioStack.Infrastructure.Keon;

/// <summary>
/// Stable receipt-class identifiers for the Governed Spine (Lane G receipt taxonomy).
///
/// A receipt class answers "why does this receipt matter?" — it names the governed
/// reasoning event, not merely that "something happened". Classes are dot-delimited
/// <c>family.subject.action</c> strings and are persisted on every <see cref="ReceiptRequest"/>.
///
/// DOCTRINE: A receipt is warranted only when evidence is used, a policy is evaluated,
/// a safety boundary is triggered, a knowledge artifact changes state, a user-facing
/// intelligence output is generated, a user observation is recorded, an admin governance
/// decision is made, or an export/report is generated. Do NOT mint classes for page views
/// or low-value UI actions.
///
/// Many classes below are declared for forthcoming lanes. Only classes wired to currently
/// implemented behavior are issued today; the rest are reserved so future agents extend the
/// taxonomy without re-litigating naming.
/// </summary>
public static class ReceiptClass
{
    /// <summary>
    /// Sentinel for spine rows written before the receipt taxonomy existed (backfilled by the
    /// AddReceiptClassToSpine migration) and the store-level default. Distinguishes "predates
    /// classification" from a genuinely classified receipt; never issued by new code.
    /// </summary>
    public const string LegacyUnclassified = "legacy.unclassified";

    // ── Knowledge-source intake & transcripts ──────────────────────────────────
    public const string SourceIntakeReceived = "source.intake.received";
    public const string SourceTranscriptResolved = "source.transcript.resolved";
    public const string SourceCandidateStaged = "source.candidate.staged";
    public const string SourceReviewStateChanged = "source.review-state.changed";
    public const string SourceArtifactPromoted = "source.artifact.promoted";

    // ── Evidence claims & contradictions ───────────────────────────────────────
    public const string EvidenceClaimCreated = "evidence.claim.created";
    public const string EvidenceClaimUpdated = "evidence.claim.updated";
    public const string EvidenceContradictionDetected = "evidence.contradiction.detected";
    public const string EvidenceContradictionResolved = "evidence.contradiction.resolved";

    // ── Runtime intelligence artifacts ─────────────────────────────────────────
    public const string IntelligenceSubstanceProfileGenerated = "intelligence.substance-profile.generated";
    public const string IntelligenceCompatibilityMatrixRebuilt = "intelligence.compatibility-matrix.rebuilt";
    public const string IntelligenceGraphArtifactUsed = "intelligence.graph-artifact.used";

    // ── Deliberation ───────────────────────────────────────────────────────────
    public const string DeliberationStackReviewCompleted = "deliberation.stack-review.completed";

    // ── Safety & policy ────────────────────────────────────────────────────────
    public const string SafetyGateTriggered = "safety.gate.triggered";
    public const string SafetyWarningSurfaced = "safety.warning.surfaced";
    public const string SafetyUnsafeRequestRefused = "safety.unsafe-request.refused";

    // ── Personalization & recommendation ───────────────────────────────────────
    public const string PersonalizationOverlayApplied = "personalization.overlay.applied";
    public const string RecommendationRationaleGenerated = "recommendation.rationale.generated";

    // ── Protocol lifecycle ─────────────────────────────────────────────────────
    public const string ProtocolPhasePlanGenerated = "protocol.phase-plan.generated";
    public const string ProtocolReviewCompleted = "protocol.review.completed";
    public const string MonitoringPlanGenerated = "monitoring.plan.generated";

    // ── User observations ──────────────────────────────────────────────────────
    public const string UserDoseLogRecorded = "user.dose-log.recorded";
    public const string UserOutcomeObservationRecorded = "user.outcome-observation.recorded";

    // ── Exports & admin governance ─────────────────────────────────────────────
    public const string ExportCareTeamSummaryGenerated = "export.care-team-summary.generated";
    public const string AdminOverridePerformed = "admin.override.performed";
}
