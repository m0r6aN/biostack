namespace BioStack.Contracts.Responses;

public sealed record ProtocolComputationRecordResponse(
    Guid Id,
    Guid ProtocolId,
    Guid? RunId,
    string Type,
    string InputSnapshot,
    string OutputResult,
    DateTime TimestampUtc
);

public sealed record ProtocolReviewCompletedEventResponse(
    Guid Id,
    Guid ProtocolId,
    Guid? RunId,
    DateTime CompletedAtUtc,
    string Notes
);
