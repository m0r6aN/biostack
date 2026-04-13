namespace BioStack.Api.Endpoints;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BioStack.Api.Auth;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

public static class AuthEndpoints
{
    private const string EmailIdentityType = "email";
    private const string EmailChannel = "email";
    private const string MagicLinkType = "magic_link";
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    private static readonly string[] RedirectAllowlist =
    [
        "/mission-control",
        "/profiles",
        "/compounds",
        "/protocols",
        "/checkins",
        "/timeline",
        "/calculators",
        "/knowledge",
        "/admin",
        "/onboarding",
        "/"
    ];

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/start", Start)
            .WithName("StartPasswordlessAuth")
            .RequireRateLimiting("auth-start");

        group.MapGet("/session", GetSession)
            .WithName("GetAuthSession");

        group.MapPost("/logout", Logout)
            .WithName("Logout");

        app.MapGet("/auth/verify", Verify)
            .WithTags("Auth")
            .WithName("VerifyMagicLink")
            .RequireRateLimiting("auth-verify");
    }

    private static async Task<IResult> Start(
        StartAuthRequest request,
        BioStackDbContext db,
        IAppUserRepository userRepo,
        IMagicLinkDelivery delivery,
        IConfiguration config,
        HttpContext http,
        CancellationToken ct)
    {
        var redirectPath = NormalizeRedirectPath(request.RedirectPath);
        var contact = NormalizeEmail(request.Contact);

        if (request.Channel != EmailChannel || contact is null)
        {
            return Results.Ok(new { message = "If that email can sign in, we sent a link." });
        }

        var now = DateTime.UtcNow;
        var user = await userRepo.FindByEmailAsync(contact, ct)
            ?? await userRepo.UpsertAsync(new AppUser
            {
                Provider = EmailIdentityType,
                ProviderKey = contact,
                Email = contact,
                DisplayName = contact,
            }, ct);

        var identity = await db.AuthIdentities
            .FirstOrDefaultAsync(i => i.Type == EmailIdentityType && i.ValueNormalized == contact, ct);

        if (identity is null)
        {
            identity = new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = EmailIdentityType,
                ValueNormalized = contact,
                CreatedAtUtc = now,
            };
            db.AuthIdentities.Add(identity);
        }
        else if (identity.UserId != user.Id)
        {
            identity.UserId = user.Id;
        }

        var rawToken = GenerateToken();
        var challenge = new AuthChallenge
        {
            Id = Guid.NewGuid(),
            IdentityId = identity.Id,
            Channel = EmailChannel,
            ChallengeType = MagicLinkType,
            TokenHash = HashSecret(rawToken),
            ExpiresAtUtc = now.Add(ChallengeLifetime),
            CreatedAtUtc = now,
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            RedirectPath = redirectPath,
        };

        db.AuthChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        var publicApiUrl = config["PublicApiUrl"] ?? config["Auth:PublicApiUrl"] ?? $"{http.Request.Scheme}://{http.Request.Host}";
        var magicLink = $"{publicApiUrl.TrimEnd('/')}/auth/verify?token={Uri.EscapeDataString(rawToken)}";
        await delivery.SendAsync(contact, magicLink, redirectPath, challenge.ExpiresAtUtc, ct);

        return Results.Ok(new { message = "If that email can sign in, we sent a link." });
    }

    private static async Task<IResult> Verify(
        string? token,
        BioStackDbContext db,
        IConfiguration config,
        HttpContext http,
        CancellationToken ct)
    {
        var frontendUrl = (config["FrontendUrl"] ?? config["Auth:FrontendUrl"] ?? "http://localhost:3043").TrimEnd('/');
        var failureRedirect = $"{frontendUrl}/auth/signin?error=invalid-link";

        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Redirect(failureRedirect);
        }

        var tokenHash = HashSecret(token);
        var now = DateTime.UtcNow;
        var challenge = await db.AuthChallenges
            .Include(c => c.Identity)
            .ThenInclude(i => i.User)
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash && c.Channel == EmailChannel && c.ChallengeType == MagicLinkType, ct);

        if (challenge is null)
        {
            return Results.Redirect(failureRedirect);
        }

        challenge.AttemptCount++;

        if (challenge.ConsumedAtUtc is not null || challenge.ExpiresAtUtc <= now)
        {
            await db.SaveChangesAsync(ct);
            return Results.Redirect(failureRedirect);
        }

        challenge.ConsumedAtUtc = now;
        if (!challenge.Identity.IsVerified)
        {
            challenge.Identity.IsVerified = true;
            challenge.Identity.VerifiedAtUtc = now;
        }

        var sessionToken = GenerateToken();
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = challenge.Identity.UserId,
            TokenHash = HashSecret(sessionToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(SessionLifetime),
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http.Request.Headers.UserAgent.ToString(),
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        var user = challenge.Identity.User;
        var claims = BuildClaims(user, sessionToken);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = session.ExpiresAtUtc,
                AllowRefresh = false,
            });

        return Results.Redirect($"{frontendUrl}{NormalizeRedirectPath(challenge.RedirectPath)}");
    }

    private static IResult GetSession(HttpContext http)
    {
        var user = UserFromClaims(http.User);
        return Results.Ok(user is null
            ? new AuthSessionResponse(false, null)
            : new AuthSessionResponse(true, user));
    }

    private static async Task<IResult> Logout(
        HttpContext http,
        BioStackDbContext db,
        CancellationToken ct)
    {
        var sessionToken = http.User.FindFirst("session_token")?.Value;
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            var tokenHash = HashSecret(sessionToken);
            var session = await db.Sessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, ct);
            if (session is not null && session.RevokedAtUtc is null)
            {
                session.RevokedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static IEnumerable<Claim> BuildClaims(AppUser user, string sessionToken)
    {
        return
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("sub", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("email", user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim("name", user.DisplayName),
            new Claim("avatar", user.AvatarUrl ?? string.Empty),
            new Claim("role", ((int)user.Role).ToString()),
            new Claim("session_token", sessionToken),
        ];
    }

    private static UserInfoDto? UserFromClaims(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId))
        {
            return null;
        }

        var role = principal.FindFirst("role")?.Value;
        return new UserInfoDto(
            Id: userId,
            Email: principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            DisplayName: principal.FindFirst("name")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            AvatarUrl: principal.FindFirst("avatar")?.Value,
            Role: int.TryParse(role, out var parsedRole) ? parsedRole : 0);
    }

    private static string? NormalizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !new EmailAddressAttribute().IsValid(normalized))
        {
            return null;
        }

        return normalized;
    }

    private static string NormalizeRedirectPath(string? redirectPath)
    {
        if (string.IsNullOrWhiteSpace(redirectPath) ||
            !redirectPath.StartsWith("/", StringComparison.Ordinal) ||
            redirectPath.StartsWith("//", StringComparison.Ordinal) ||
            redirectPath.Contains('\\'))
        {
            return "/mission-control";
        }

        var pathOnly = redirectPath.Split('?', '#')[0];
        return RedirectAllowlist.Any(allowed =>
            allowed == "/" ? pathOnly == "/" : pathOnly.Equals(allowed, StringComparison.OrdinalIgnoreCase) || pathOnly.StartsWith($"{allowed}/", StringComparison.OrdinalIgnoreCase))
            ? redirectPath
            : "/mission-control";
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
