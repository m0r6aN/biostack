namespace BioStack.Contracts.Responses;

public sealed record ProtocolResponse(
    Guid Id,
    Guid PersonId,
    string Name,
    int Version,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<ProtocolItemResponse> Items,
    StackScoreResponse StackScore,
    SimulationResultResponse Simulation,
    ProtocolActualComparisonResponse? ActualComparison
);
