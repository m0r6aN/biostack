namespace BioStack.Contracts.Responses;

public sealed record ProtocolRunResponse(
    Guid Id,
    Guid ProtocolId,
    Guid PersonId,
    string ProtocolName,
    int ProtocolVersion,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status,
    string Notes
);

public sealed record ProtocolRunInsightResponse(
    string Type,
    string Message,
    List<string> RelatedSignals
);

public sealed record ProtocolRunSignalSummaryResponse(
    string Metric,
    string Direction,
    string Magnitude
);

public sealed record ProtocolRunSummaryResponse(
    ProtocolRunResponse Run,
    int DaysActive,
    List<ProtocolRunSignalSummaryResponse> Signals,
    int AlignedCount,
    int DivergingCount
);

public sealed record ProtocolRunObservationResponse(
    Guid CheckInId,
    DateTime Date,
    int Day,
    int Energy,
    int SleepQuality,
    int Appetite,
    int Recovery
);
