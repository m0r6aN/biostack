namespace BioStack.Contracts.Responses;

public sealed record SimulationResultResponse(
    List<SimulationTimelineEntryResponse> Timeline,
    List<string> Insights
);

public sealed record SimulationTimelineEntryResponse(
    string DayRange,
    List<string> Signals
);
