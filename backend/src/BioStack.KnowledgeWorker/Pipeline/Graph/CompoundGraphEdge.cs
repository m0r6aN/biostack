namespace BioStack.KnowledgeWorker.Pipeline.Graph;

using System.Text.Json.Serialization;

public sealed record CompoundGraphEdge(
    string EdgeId,
    string From,
    string To,
    CompoundGraphEdgeType EdgeType,
    string? RelationshipType,
    string? AssertedRelationshipType,
    string? EffectDomain,
    string? EvidenceTier,
    string? Confidence,
    IReadOnlyList<string> SourceRefs,
    IReadOnlyList<string> ClaimRefs,
    IReadOnlyList<string> ReviewFlags,
    bool NeedsReview,
    CommunitySignal? CommunitySignal,
    SourceAuthorityMix SourceAuthorityMix);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompoundGraphEdgeType
{
    BelongsToCategory,
    AffectsPathway,
    HasTarget,
    HasBenefitClaim,
    HasRiskClaim,
    PairsWith,
    AvoidWith,
    SynergizesWith,
    Complements,
    RedundantWith,
    ConflictsWith,
    OpposesEffect,
    HasCommunitySignal,
    SupportedBy,
    ContradictedBy,
    SourceDerivedFrom,
}
