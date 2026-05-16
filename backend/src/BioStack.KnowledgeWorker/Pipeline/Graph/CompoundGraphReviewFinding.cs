namespace BioStack.KnowledgeWorker.Pipeline.Graph;

using System.Text.Json.Serialization;

public sealed record CompoundGraphReviewFinding(
    string FindingId,
    CompoundGraphFindingType FindingType,
    CompoundGraphFindingSeverity Severity,
    IReadOnlyList<string> CompoundRefs,
    IReadOnlyList<string> EdgeRefs,
    string Summary,
    string RecommendedAction,
    bool NeedsHumanReview);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompoundGraphFindingType
{
    SynergyChainWithConflict,
    SameCategoryOpposingEffects,
    SharedPathwayAdditiveRisk,
    PopularStackInsufficientEvidence,
    CommunityClaimContradictedByAuthority,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompoundGraphFindingSeverity
{
    Low,
    Moderate,
    High,
    Critical,
}
