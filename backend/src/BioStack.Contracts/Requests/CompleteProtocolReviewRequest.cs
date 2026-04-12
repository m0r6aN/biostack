namespace BioStack.Contracts.Requests;

public sealed record CompleteProtocolReviewRequest(
    Guid? RunId,
    string? Notes
);
