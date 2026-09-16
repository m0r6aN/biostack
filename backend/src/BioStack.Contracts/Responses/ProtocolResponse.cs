namespace BioStack.Contracts.Responses;

public sealed record ProtocolResponse(
    Guid Id,
    Guid PersonId,
    string Name,
    int Version,
    Guid? ParentProtocolId,
    Guid? OriginProtocolId,
    Guid? EvolvedFromRunId,
    bool IsDraft,
    string EvolutionContext,
    bool IsCurrentVersion,
    List<ProtocolVersionSummaryResponse> PriorVersions,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<ProtocolItemResponse> Items,
    StackScoreResponse StackScore,
    SimulationResultResponse Simulation,
    // Shaped by entitlement at the single projection point (InteractionIntelligenceProjection):
    // InteractionIntelligenceResponse (full reasoning) with reviewed_relationship_graph, otherwise
    // ReducedInteractionIntelligenceResponse (pair names + severity only). Declared as object so
    // System.Text.Json serializes the runtime shape rather than always emitting the full one.
    object InteractionIntelligence,
    ProtocolRunResponse? ActiveRun,
    ProtocolVersionDiffResponse? VersionDiff,
    ProtocolActualComparisonResponse? ActualComparison
);

public sealed record ProtocolVersionSummaryResponse(
    Guid Id,
    string Name,
    int Version,
    bool IsDraft,
    DateTime CreatedAtUtc
);

public sealed record ProtocolVersionDiffResponse(
    Guid FromProtocolId,
    Guid ToProtocolId,
    List<ProtocolVersionChangeResponse> Changes
);

public sealed record ProtocolVersionChangeResponse(
    string ChangeType,
    string Scope,
    string Subject,
    string Before,
    string After
);
