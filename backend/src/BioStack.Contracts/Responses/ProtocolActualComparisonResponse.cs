namespace BioStack.Contracts.Responses;

public sealed record ProtocolActualComparisonResponse(
    SimulationResultResponse Simulation,
    List<ActualTrendResponse> ActualTrends,
    List<string> Highlights
);

public sealed record ActualTrendResponse(
    string Metric,
    decimal? BeforeAverage,
    decimal? AfterAverage,
    string Direction
);
