namespace BioStack.Contracts.Responses;

public sealed class ProtocolSequenceExpectationSnapshot
{
    public Guid ProtocolId { get; init; }
    public string BaselineSource { get; init; } = "insufficient_history";
    public int HistoricalRunCount { get; init; }
    public ExpectedNextEventSummary? ExpectedNextEvent { get; init; }
    public IReadOnlyList<ExpectedTransitionSummary> CommonTransitions { get; init; } = [];
    public CurrentSequenceStatusSummary? CurrentStatus { get; init; }
}

public sealed class ExpectedNextEventSummary
{
    public string EventType { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string TimingWindow { get; init; } = default!;
    public string Confidence { get; init; } = "none";
}

public sealed class ExpectedTransitionSummary
{
    public string FromState { get; init; } = default!;
    public string ToEventType { get; init; } = default!;
    public string TimingPattern { get; init; } = default!;
    public int ObservedCount { get; init; }
}

public sealed class CurrentSequenceStatusSummary
{
    public string State { get; init; } = "unknown";
    public IReadOnlyList<string> Notes { get; init; } = [];
}
