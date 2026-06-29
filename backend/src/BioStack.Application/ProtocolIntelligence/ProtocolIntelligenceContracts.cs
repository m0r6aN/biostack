namespace BioStack.Application.ProtocolIntelligence;

// Build-time promotion-gate contracts for Protocol Intelligence artifacts.
// These types describe what may enter the knowledge graph; they never describe
// a runtime user-facing surface. Nothing here emits narrative — the gate only
// decides whether a candidate artifact is structurally complete, doctrine-clean
// (via DoctrineSanitizer), and cleared for promotion.

public sealed record ProtocolIntelligenceArtifactSet(
    IReadOnlyDictionary<string, PromotionTargetContract> PromotionTargets,
    IReadOnlyList<string> SupportedRelationshipIds,
    IReadOnlySet<string> GlobalBlockedOutputs,
    IReadOnlySet<string> AllBlockedOutputs,
    IReadOnlyList<string> SideEffectRequiredArtifactFields,
    IReadOnlyDictionary<string, SourceClassContract> SourceClasses,
    IReadOnlyDictionary<string, string> ArtifactVersions,
    IReadOnlyList<string> AvailableObservabilityModules);

public sealed record PromotionTargetContract(
    string Id,
    IReadOnlyList<string> RequiredFields,
    string ReviewGate,
    bool ForbiddenOutputScanRequired);

public sealed record SourceClassContract(
    string Id,
    bool WarningFirst,
    IReadOnlyList<string> BlockedOutputs);

public sealed record PromotionGateRequest(
    string ArtifactType,
    Dictionary<string, object?> Artifact,
    IReadOnlyList<string>? ClaimTags = null);

public sealed record PromotionGateResult(
    bool CanPromote,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RequiredFieldsMissing,
    // Artifact field paths whose user-facing text tripped DoctrineSanitizer. When non-empty,
    // BlockingReasons contains GateReasons.DoctrineViolation. This is the ONLY forbidden-output
    // authority in BioStack — there is no parallel phrase list or output scanner.
    IReadOnlyList<string> DoctrineViolationFields,
    bool RequiresHumanReview);

public static class GateReasons
{
    public const string UnknownArtifactType = "unknown_artifact_type";
    public const string ReviewStatusNotApproved = "review_status_not_approved";
    public const string HumanReviewRequired = "human_review_required";
    public const string RequiredFieldsMissing = "required_fields_missing";
    public const string DoctrineViolation = "doctrine_violation";
}
