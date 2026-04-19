namespace BioStack.Api.Auth;

using System.Security.Claims;
using BioStack.Application.Services;

public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var claimValue = user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(claimValue, out var userId))
            throw new UnauthorizedAccessException("An authenticated user is required.");

        return userId;
    }
}
