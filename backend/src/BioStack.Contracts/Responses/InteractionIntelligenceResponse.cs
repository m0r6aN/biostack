namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record InteractionIntelligenceResponse(
    InteractionSummaryResponse Summary,
    ProtocolInteractionScoreResponse Score,
    double CompositeScore,
    List<InteractionFindingResponse> TopFindings,
    List<InteractionResultResponse> Interactions,
    List<InteractionCounterfactualResponse> Counterfactuals
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
    bool HintBacked
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
