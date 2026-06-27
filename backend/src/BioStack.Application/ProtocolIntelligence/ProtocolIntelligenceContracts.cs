namespace BioStack.Application.ProtocolIntelligence;

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
    IReadOnlyList<string> ForbiddenOutputMatches,
    bool RequiresHumanReview);

public sealed record ProtocolIntelligenceReviewedArtifact(
    string ArtifactType,
    Dictionary<string, object?> Artifact);
