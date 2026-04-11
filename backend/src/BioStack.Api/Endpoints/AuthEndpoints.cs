namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        // Called by Next.js server-side after a successful OAuth callback.
        // Returns a JWT the frontend stores in its session and forwards on every API request.
        group.MapPost("/oauth-callback", OAuthCallback)
            .WithName("OAuthCallback");

        // Returns the current user's info from the JWT (no DB hit needed — claims are authoritative).
        group.MapGet("/me", GetMe)
            .WithName("GetMe")
            .RequireAuthorization();
    }

    private static async Task<IResult> OAuthCallback(
        OAuthCallbackRequest request,
        IAppUserRepository   userRepo,
        IJwtTokenService     jwtService,
        IConfiguration       config,
        CancellationToken    ct)
    {
        // Only accept calls from our own Next.js backend (shared secret header)
        // This prevents anything on the internet from minting tokens.
        // In production, also pin to the Docker network.
        var expectedSecret = config["Auth:CallbackSecret"];
        // (Validated in middleware — see Program.cs)

        var user = await userRepo.UpsertAsync(new AppUser
        {
            Provider    = request.Provider.ToLowerInvariant(),
            ProviderKey = request.ProviderAccountId,
            Email       = request.Email,
            DisplayName = request.Name,
            AvatarUrl   = request.Image,
        }, ct);

        var token   = jwtService.GenerateToken(user);
        var expiry  = int.TryParse(config["Jwt:ExpiryMinutes"], out var m) ? m * 60 : 3600;

        return Results.Ok(new AuthTokenResponse(
            AccessToken:     token,
            TokenType:       "Bearer",
            ExpiresInSeconds: expiry,
            User: new UserInfoDto(
                Id:          user.Id,
                Email:       user.Email,
                DisplayName: user.DisplayName,
                AvatarUrl:   user.AvatarUrl,
                Role:        (int)user.Role
            )
        ));
    }

    private static IResult GetMe(HttpContext ctx)
    {
        var sub   = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = ctx.User.FindFirst("email")?.Value ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name  = ctx.User.FindFirst("name")?.Value ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role  = ctx.User.FindFirst("role")?.Value;
        var avatar= ctx.User.FindFirst("avatar")?.Value;

        if (sub is null) return Results.Unauthorized();

        return Results.Ok(new UserInfoDto(
            Id:          Guid.Parse(sub),
            Email:       email ?? string.Empty,
            DisplayName: name  ?? string.Empty,
            AvatarUrl:   avatar,
            Role:        int.TryParse(role, out var r) ? r : 0
        ));
    }
}
