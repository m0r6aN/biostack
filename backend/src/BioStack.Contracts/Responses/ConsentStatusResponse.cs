namespace BioStack.Contracts.Responses;

public sealed record ConsentStatusResponse(
    bool Accepted,
    DateTime? ConsentAcceptedAtUtc,
    string? ConsentVersion);
