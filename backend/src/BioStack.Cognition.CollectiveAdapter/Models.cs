// ─────────────────────────────────────────────────────────────────────────────
// ADAPTER SHIM — BioStack.Cognition.CollectiveAdapter
//
// These types are LOCAL CONTRACT MIRRORS of the keon.collective data model.
// They are NOT the real Keon.Collective types. This file exists only to allow
// BioStack to compile without the real Keon.Collective.Core NuGet package.
//
// SWAP PATH: Remove this project and add the real NuGet package reference.
// The namespace (Keon.Collective) matches the real package to eliminate
// using-statement churn at swap time.
// ─────────────────────────────────────────────────────────────────────────────

namespace Keon.Collective;

// ── Value-object wrappers ────────────────────────────────────────────────────
public record IntentId(string Value);
public record ClaimId(string Value);
public record CollapseId(string Value);
public record EvidenceRefId(string Value);

// ── Context stamps ───────────────────────────────────────────────────────────
public record TenantContext(string TenantId);
public record ActorContext(string ActorId, string ActorKind = "System");
public record CorrelationContext(string CorrelationId, string TraceId = "");

// ── CollectiveIntent ─────────────────────────────────────────────────────────
public record CollectiveIntent(
    IntentId IntentId,
    string Goal,
    string IntentPayloadJson,
    TenantContext TenantContext,
    ActorContext ActorContext,
    CorrelationContext CorrelationContext);

// ── TemporalEchoBranch ───────────────────────────────────────────────────────
public enum TemporalEchoState { Draft, Evaluated, Collapsed }

public record TemporalEchoBranch(
    string BranchId,
    string Hypothesis,
    string PlanPayloadJson,
    decimal UtilityScore,
    decimal RiskScore,
    IReadOnlyList<ClaimId> ClaimRefs,
    IReadOnlyList<string> Participants,
    TemporalEchoState State,
    int LineageDepth);

// ── ClaimGraph types ─────────────────────────────────────────────────────────
public record EvidenceRef(
    EvidenceRefId EvidenceRefId,
    string SourceKind,
    string CanonicalReference,
    DateTime ReferencedAtUtc);

public record AssumptionRef(
    string Description,
    bool IsEffectBearing,       // BioStack INVARIANT: always false
    ClaimId? ResolvingClaimId);

public record ClaimNode(
    ClaimId ClaimId,
    string OwningBranchId,
    IntentId IntentId,
    string Content,
    IReadOnlyList<EvidenceRef> EvidenceRefs,
    IReadOnlyList<AssumptionRef> Assumptions,
    bool IsEffectBearing,       // BioStack INVARIANT: always false
    DateTime CreatedUtc);

public enum ClaimEdgeKind { Supports, Refines, Challenges }

public record ClaimEdge(
    ClaimId FromClaimId,
    ClaimId ToClaimId,
    ClaimEdgeKind Kind);

public record ClaimGraph(
    IReadOnlyList<ClaimNode> Nodes,
    IReadOnlyList<ClaimEdge> Edges);

// ── BranchCollapseRecord ─────────────────────────────────────────────────────
public enum BranchCollapseDisposition { Selected, Contested, Indeterminate }

public record BranchCollapseRecord(
    CollapseId CollapseId,
    IntentId IntentId,
    IReadOnlyList<string> CandidateBranchIds,
    string SelectedBranchId,
    BranchCollapseDisposition Disposition,
    string SelectionRationale,
    string ComparativeHeatSummary,
    string ComparativeUtilitySummary,
    string ChallengeSummary,
    string? WitnessDigestId,
    DateTime TimestampUtc);

// ── BranchRefinementOptions ──────────────────────────────────────────────────
public record BranchRefinementOptions(int MaxIterations = 1, bool AllowBranchSplit = false);

// ── CognitiveDensityEnvelope output types ────────────────────────────────────
public enum PerspectiveKind { Optimizer, Skeptic, Regulator, Historian }
public enum FindingSeverity { Info, Warning, Critical }

public record PerspectiveFinding(
    string FindingId,
    string Category,
    string Narrative,
    FindingSeverity Severity);

public record PerspectiveReview(
    PerspectiveKind Kind,
    IReadOnlyList<PerspectiveFinding> Findings,
    string Summary);

public record BranchPerspectiveReview(
    IReadOnlyDictionary<PerspectiveKind, PerspectiveReview> PerspectiveReviews);

public record ContradictionReview(
    string CounterPlanNarrative,
    bool CounterPlanIsExecutable,   // BioStack: always false
    bool IsExecutable);             // BioStack: always false

public record ConfidenceProfile(
    string Model,
    string Epistemic,
    string EvidenceSupport,
    string ContradictionDensity,
    string CalibrationVersion);

public record ReasoningGraphRef(
    string GraphId,
    int NodeCount,
    int EdgeCount);

public record CognitiveDensityEnvelope(
    BranchPerspectiveReview BranchPerspectiveReview,
    ContradictionReview ContradictionReview,
    ConfidenceProfile ConfidenceProfile,
    ReasoningGraphRef ReasoningGraphRef);
