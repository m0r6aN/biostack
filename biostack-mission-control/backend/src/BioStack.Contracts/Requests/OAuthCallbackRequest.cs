namespace BioStack.Contracts.Requests;

/// <summary>
/// Posted by the Next.js backend after a successful OAuth callback.
/// The frontend resolves the provider token to profile info and sends it here.
/// </summary>
public sealed record OAuthCallbackRequest(
    string Provider,
    string ProviderAccountId,
    string Email,
    string Name,
    string? Image
);
