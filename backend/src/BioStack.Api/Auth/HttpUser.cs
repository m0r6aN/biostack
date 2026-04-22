namespace BioStack.Api.Auth;

using System.Security.Claims;

public static class HttpUser
{
    public static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
