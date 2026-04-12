namespace BioStack.Contracts.Responses;

public sealed class ProtocolDriftSnapshot
{
    public Guid ProtocolId { get; init; }
    public string DriftState { get; init; } = "none";
    public string BaselineSource { get; init; } = "insufficient_history";
    public IReadOnlyList<DriftSignalSummary> Signals { get; init; } = [];
    public RegimeClassificationSummary? RegimeClassification { get; init; }
}

public sealed class DriftSignalSummary
{
    public string Type { get; init; } = default!;
    public string Severity { get; init; } = "mild";
    public string Description { get; init; } = default!;
}

public sealed class RegimeClassificationSummary
{
    public string State { get; init; } = "stable";
    public IReadOnlyList<string> ContributingFactors { get; init; } = [];
}
