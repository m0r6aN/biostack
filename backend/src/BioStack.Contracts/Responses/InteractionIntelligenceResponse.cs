namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record InteractionIntelligenceResponse(
    InteractionSummaryResponse Summary,
    ProtocolInteractionScoreResponse Score,
    double CompositeScore,
    List<InteractionFindingResponse> TopFindings,
    List<InteractionResultResponse> Interactions,
    List<InteractionCounterfactualResponse> Counterfactuals,
    List<InteractionSwapRecommendationResponse> Swaps,
    // Lane C: discloses whether the evaluation drew on the reviewed compound graph ("graph") or
    // only the denormalized KnowledgeEntry fallback ("fallback"), plus the graph artifact hash used.
    string Source = IntelligenceSource.Fallback,
    string? GraphArtifactHash = null
);

public sealed record InteractionSummaryResponse(
    int Synergies,
    int Redundancies,
    int Interferences
);

public sealed record ProtocolInteractionScoreResponse(
    double SynergyScore,
    double RedundancyPenalty,
    double InterferencePenalty
);

public sealed record InteractionFindingResponse(
    InteractionType Type,
    List<string> Compounds,
    string Message,
    double Confidence
);

public sealed record InteractionResultResponse(
    string CompoundA,
    string CompoundB,
    InteractionType Type,
    double Confidence,
    List<string> SharedPathways,
    string Reason,
    bool HintBacked,
    // Lane C: "graph" when this pair came from the reviewed compound graph, "fallback" when derived
    // from KnowledgeEntry string fields (weaker confidence). GraphArtifactHash set only when graph-backed.
    string Source = IntelligenceSource.Fallback,
    string? GraphArtifactHash = null
);

public sealed record InteractionCounterfactualResponse(
    string RemovedCompound,
    double VariantScore,
    double DeltaScore,
    double DeltaPercent,
    string Verdict,
    string Recommendation,
    InteractionSummaryResponse Summary,
    List<InteractionFindingResponse> TopFindings
);

public sealed record InteractionSwapRecommendationResponse(
    string OriginalCompound,
    string CandidateCompound,
    double BaselineScore,
    double VariantScore,
    double DeltaScore,
    double DeltaPercent,
    string Verdict,
    List<string> Reasons,
    string Recommendation,
    double SimilarityScore,
    InteractionSummaryResponse Summary,
    List<InteractionFindingResponse> TopFindings
);

public static class SwapVerdicts
{
    public const string LikelyImproves = "likely_improves";
    public const string LittleExpectedChange = "little_expected_change";
    public const string LikelyWorsens = "likely_worsens";
}

public static class SwapReasonAtoms
{
    public const string ReducesRedundancy = "reduces_redundancy";
    public const string PreservesSynergy = "preserves_synergy";
    public const string LowersInterference = "lowers_interference";
    public const string ImprovesGoalAlignment = "improves_goal_alignment";
    public const string ImprovesSignalClarity = "improves_signal_clarity";
    public const string StrongerEvidence = "stronger_evidence";
    public const string LowerEstimatedCost = "lower_estimated_cost";
}
