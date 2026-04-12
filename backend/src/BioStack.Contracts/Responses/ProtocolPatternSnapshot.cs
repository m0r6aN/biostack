namespace BioStack.Contracts.Responses;

public sealed class ProtocolPatternSnapshot
{
    public Guid ProtocolId { get; init; }
    public int HistoricalRunCount { get; init; }
    public string PatternConfidence { get; init; } = "none";
    public IReadOnlyList<MetricPatternSummary> MetricPatterns { get; init; } = [];
    public IReadOnlyList<EventPatternSummary> EventPatterns { get; init; } = [];
    public IReadOnlyList<SequencePatternSummary> SequencePatterns { get; init; } = [];
    public PatternComparisonSummary? CurrentRunComparison { get; init; }
}

public sealed class MetricPatternSummary
{
    public string Metric { get; init; } = default!;
    public string Observation { get; init; } = default!;
}

public sealed class EventPatternSummary
{
    public string EventType { get; init; } = default!;
    public string TimingPattern { get; init; } = default!;
}

public sealed class SequencePatternSummary
{
    public IReadOnlyList<string> Sequence { get; init; } = [];
    public string Description { get; init; } = default!;
}

public sealed class PatternComparisonSummary
{
    public string Similarity { get; init; } = "none";
    public IReadOnlyList<string> MatchingSignals { get; init; } = [];
    public IReadOnlyList<string> DivergentSignals { get; init; } = [];
}
