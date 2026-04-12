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
