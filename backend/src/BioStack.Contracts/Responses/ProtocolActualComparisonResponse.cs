namespace BioStack.Contracts.Responses;

public sealed record ProtocolActualComparisonResponse(
    SimulationResultResponse Simulation,
    ProtocolRunResponse? Run,
    ProtocolRunSummaryResponse? RunSummary,
    List<ProtocolRunObservationResponse> Observations,
    List<ActualTrendResponse> ActualTrends,
    List<ProtocolRunInsightResponse> Insights,
    List<string> Highlights
);

public sealed record ActualTrendResponse(
    string Metric,
    decimal? BeforeAverage,
    decimal? AfterAverage,
    string Direction
);
