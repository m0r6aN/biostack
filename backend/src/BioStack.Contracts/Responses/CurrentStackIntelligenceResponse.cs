namespace BioStack.Contracts.Responses;

public sealed record CurrentStackIntelligenceResponse(
    StackScoreResponse StackScore,
    SimulationResultResponse Simulation,
    InteractionIntelligenceResponse InteractionIntelligence
);
