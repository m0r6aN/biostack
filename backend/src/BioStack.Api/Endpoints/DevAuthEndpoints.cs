namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;

/// <summary>
/// Dev-only endpoints — registered only when ASPNETCORE_ENVIRONMENT=Development.
/// Never shipped to production.
/// </summary>
public static class DevAuthEndpoints
{
    public static void MapDevAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Returns a signed admin JWT without requiring OAuth.
        // Useful for local development when no OAuth providers are configured.
        app.MapPost("/api/v1/auth/dev-token", (IJwtTokenService jwt) =>
        {
            var devAdmin = new AppUser
            {
                Id          = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Provider    = "dev",
                ProviderKey = "dev-admin",
                Email       = "dev@localhost",
                DisplayName = "Dev Admin",
                Role        = UserRole.Admin,
            };

            var token = jwt.GenerateToken(devAdmin);
            return Results.Ok(new { token });
        });
    }
}
