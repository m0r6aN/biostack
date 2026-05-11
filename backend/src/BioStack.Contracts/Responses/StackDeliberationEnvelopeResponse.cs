namespace BioStack.Contracts.Responses;

/// <summary>
/// Output for POST /api/v1/stack-review/envelope.
/// Fields are in strict doctrine order: deterministic findings ALWAYS first.
/// effect_status on every finding is "commentary-only" — non-negotiable.
/// </summary>
public sealed record StackDeliberationEnvelopeResponse(
    // 1. Deterministic findings (BioStack-native, fully inspectable)
    IReadOnlyList<DeterministicFindingResponse> DeterministicFindings,
    // 2. Role-Based Perspective Review
    IReadOnlyDictionary<string, PerspectiveReviewResponse> PerspectiveReviews,
    // 3. Contradiction Review (counter-position, always non-executable)
    ContradictionReviewResponse ContradictionReview,
    // 4. Confidence Profile
    ConfidenceProfileResponse ConfidenceProfile,
    // 5. Reasoning Graph reference
    ReasoningGraphRefResponse ReasoningGraph,
    // Non-negotiable doctrine stamp
    string EffectStatus);   // always "commentary-only"

public sealed record DeterministicFindingResponse(
    string FindingId,
    string Code,
    string Category,
    string Narrative,
    IReadOnlyList<string> CompoundSlugs,
    decimal RiskScoreContribution,
    string EvidenceTier,
    string EffectStatus);   // always "commentary-only"

public sealed record PerspectiveReviewResponse(
    string Role,
    IReadOnlyList<PerspectiveFindingResponse> Findings,
    string Summary,
    string EffectStatus);   // always "commentary-only"

public sealed record PerspectiveFindingResponse(
    string FindingId,
    string Category,
    string Narrative,
    string Severity,
    string EffectStatus);   // always "commentary-only"

public sealed record ContradictionReviewResponse(
    string CounterPlanNarrative,
    bool IsExecutable,          // always false
    string EffectStatus);       // always "commentary-only"

public sealed record ConfidenceProfileResponse(
    string Model,
    string Epistemic,
    string EvidenceSupport,
    string ContradictionDensity,
    string CalibrationVersion);

public sealed record ReasoningGraphRefResponse(
    string GraphId,
    int NodeCount,
    int EdgeCount);
