namespace BioStack.Api.Auth;

using BioStack.Application.Services;

/// <summary>
/// Endpoint filter that blocks data-writing endpoints when the authenticated user
/// has not recorded consent. Anonymous requests are not affected: authentication
/// middleware will short-circuit them with 401 before this filter runs.
/// </summary>
public sealed class RequireConsentFilter : IEndpointFilter
{
    private readonly IConsentGate _consentGate;

    public RequireConsentFilter(IConsentGate consentGate)
    {
        _consentGate = consentGate;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        // Authentication runs first; an unauthenticated request never reaches here when
        // the endpoint requires authorization. Defense-in-depth: bail out if somehow it does.
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return await next(context);
        }

        bool granted;
        try
        {
            granted = await _consentGate.IsConsentGrantedAsync(httpContext.RequestAborted);
        }
        catch (UnauthorizedAccessException)
        {
            // Bad/missing user id claim — let auth pipeline surface 401 semantics.
            return Results.Unauthorized();
        }
        catch (InvalidOperationException)
        {
            // Authenticated principal references a user row that no longer exists.
            return Results.Unauthorized();
        }

        if (!granted)
        {
            return Results.Json(
                new { code = "consent_required", url = "/onboarding/consent" },
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}

public static class RequireConsentFilterExtensions
{
    /// <summary>
    /// Apply the consent gate to this write endpoint. Reads remain accessible because the
    /// extension is only attached to explicit write routes.
    /// </summary>
    public static RouteHandlerBuilder RequireConsent(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<RequireConsentFilter>();
}
