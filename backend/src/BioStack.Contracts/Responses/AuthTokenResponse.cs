namespace BioStack.Contracts.Responses;

public sealed record AuthTokenResponse(
    string AccessToken,
    string TokenType,
    int    ExpiresInSeconds,
    UserInfoDto User
);

public sealed record UserInfoDto(
    Guid   Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    // Role is an integer — keeps the semantic opaque in transport
    int Role
);
