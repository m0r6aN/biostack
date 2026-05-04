namespace BioStack.Api.Auth;

using BioStack.Application.Services;

public sealed class DevBypassCurrentUserAccessor : ICurrentUserAccessor
{
    public static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid GetCurrentUserId() => UserId;
}
