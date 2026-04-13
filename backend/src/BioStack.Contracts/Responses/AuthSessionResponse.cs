namespace BioStack.Contracts.Responses;

public sealed record AuthSessionResponse(
    bool Authenticated,
    UserInfoDto? User
);
