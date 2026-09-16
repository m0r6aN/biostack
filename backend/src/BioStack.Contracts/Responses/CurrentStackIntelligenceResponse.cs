namespace BioStack.Contracts.Responses;

public sealed record CurrentStackIntelligenceResponse(
    StackScoreResponse StackScore,
    SimulationResultResponse Simulation,
    // Shaped by entitlement at the single projection point (InteractionIntelligenceProjection):
    // InteractionIntelligenceResponse (full reasoning) with reviewed_relationship_graph, otherwise
    // ReducedInteractionIntelligenceResponse (pair names + severity only). Declared as object so
    // System.Text.Json serializes the runtime shape rather than always emitting the full one.
    object InteractionIntelligence
);
