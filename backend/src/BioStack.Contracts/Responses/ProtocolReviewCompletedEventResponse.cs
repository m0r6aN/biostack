namespace BioStack.Contracts.Responses;

public sealed record ProtocolReviewCompletedEventResponse(
    Guid Id,
    Guid ProtocolId,
    Guid? RunId,
    DateTime CompletedAtUtc,
    string Notes
);
