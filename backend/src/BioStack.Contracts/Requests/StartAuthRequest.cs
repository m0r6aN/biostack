namespace BioStack.Contracts.Requests;

public sealed record StartAuthRequest(
    string Contact,
    string Channel,
    string? RedirectPath
);
