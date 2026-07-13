namespace BioStack.Api.Endpoints;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BioStack.Api.Auth;
using BioStack.Application.Services;
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
        "/protocol-console",
        "/mission-control",
        "/profiles",
        "/compounds",
        "/protocols",
        "/my-protocol",
        "/checkins",
        "/timeline",
        "/calculators",
        "/knowledge",
        "/admin",
        ProductContract.Current.Routes.Canonical["onboarding"],
        ProductContract.Current.Routes.Canonical["analyzer"],
        "/"
    ];

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/start", Start)
            .WithName("StartPasswordlessAuth")
            .RequireRateLimiting("auth-start");

        group.MapPost("/verify", VerifyApi)
            .WithName("VerifyMagicLink")
            .RequireRateLimiting("auth-verify");

        group.MapGet("/session", GetSession)
            .WithName("GetAuthSession");

        group.MapPost("/logout", Logout)
            .WithName("Logout");

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/auth/verify", VerifyDevelopmentRedirect)
                .WithTags("Auth")
                .WithName("VerifyMagicLinkDevelopmentRedirect")
                .RequireRateLimiting("auth-verify");
        }
    }

    private static async Task<IResult> Start(
        StartAuthRequest request,
        BioStackDbContext db,
        IAppUserRepository userRepo,
        IMagicLinkDelivery delivery,
        IConfiguration config,
        ILoggerFactory loggerFactory,
        IWebHostEnvironment environment,
        HttpContext http,
        CancellationToken ct)
    {
        var normalizedRedirect = NormalizeRedirectPath(request.RedirectPath);
        var redirectPath = normalizedRedirect.Path;
        var contact = NormalizeEmail(request.Contact);

        if (normalizedRedirect.UsedFallback && !string.IsNullOrWhiteSpace(request.RedirectPath))
        {
            loggerFactory.CreateLogger("BioStack.AuthFlow").LogWarning(
                new EventId(6901, "AuthReturnPathRejected"),
                "Rejected an unapproved auth return path and used the canonical fallback.");
        }

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

        await db.AuthChallenges
            .Where(c =>
                c.IdentityId == identity.Id &&
                c.Channel == EmailChannel &&
                c.ChallengeType == MagicLinkType &&
                c.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.ConsumedAtUtc, now), ct);

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

        // Magic links point to the frontend, never a state-changing GET endpoint. Outside
        // development the token uses a URL fragment, which browsers do not send in the
        // initial HTTP request or Referer header. The frontend exchanges it by POST.
        var frontendBaseUrl = (config["FrontendUrl"] ?? config["Auth:FrontendUrl"] ?? "http://localhost:3043").TrimEnd('/');
        var tokenDelimiter = environment.IsDevelopment() ? "?token=" : "#token=";
        var magicLink = $"{frontendBaseUrl}/auth/verify{tokenDelimiter}{Uri.EscapeDataString(rawToken)}";
        await delivery.SendAsync(contact, magicLink, redirectPath, challenge.ExpiresAtUtc, ct);

        return Results.Ok(new { message = "If that email can sign in, we sent a link." });
    }

    private static async Task<IResult> VerifyApi(
        VerifyAuthRequest request,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var redirectPath = await VerifyAndSignInAsync(request.Token, db, http, ct);
        if (redirectPath is null)
        {
            return Results.BadRequest(new { code = "invalid_link", message = "This sign-in link is invalid, expired, or already used." });
        }

        return Results.Ok(new VerifyAuthResponse(redirectPath));
    }

    private static async Task<IResult> VerifyDevelopmentRedirect(
        string? token,
        BioStackDbContext db,
        IConfiguration config,
        HttpContext http,
        CancellationToken ct)
    {
        var frontendUrl = (config["FrontendUrl"] ?? config["Auth:FrontendUrl"] ?? "http://localhost:3043").TrimEnd('/');
        var redirectPath = await VerifyAndSignInAsync(token, db, http, ct);
        return Results.Redirect(redirectPath is null
            ? $"{frontendUrl}/auth/signin?error=invalid-link"
            : $"{frontendUrl}{redirectPath}");
    }

    private static async Task<string?> VerifyAndSignInAsync(
        string? token,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Pragma = "no-cache";
        http.Response.Headers.Append("Referrer-Policy", "no-referrer");

        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = HashSecret(token);
        var now = DateTime.UtcNow;
        var claimed = await db.AuthChallenges
            .Where(c =>
                c.TokenHash == tokenHash &&
                c.Channel == EmailChannel &&
                c.ChallengeType == MagicLinkType &&
                c.ConsumedAtUtc == null &&
                c.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.ConsumedAtUtc, now)
                .SetProperty(c => c.AttemptCount, c => c.AttemptCount + 1), ct);

        if (claimed != 1)
        {
            await db.AuthChallenges
                .Where(c =>
                    c.TokenHash == tokenHash &&
                    c.Channel == EmailChannel &&
                    c.ChallengeType == MagicLinkType)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(c => c.AttemptCount, c => c.AttemptCount + 1),
                    ct);
            return null;
        }

        var challenge = await db.AuthChallenges
            .Include(c => c.Identity)
            .ThenInclude(i => i.User)
            .SingleAsync(c => c.TokenHash == tokenHash && c.Channel == EmailChannel && c.ChallengeType == MagicLinkType, ct);

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

        var redirectPath = NormalizeRedirectPath(challenge.RedirectPath).Path;
        var hasCurrentConsent = user.ConsentAcceptedAtUtc.HasValue &&
            string.Equals(user.ConsentVersion, ConsentGate.CurrentConsentVersion, StringComparison.Ordinal);
        if (!hasCurrentConsent)
        {
            redirectPath = $"/onboarding/consent?returnTo={Uri.EscapeDataString(redirectPath)}";
        }

        return redirectPath;
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

    private static NormalizedRedirectPath NormalizeRedirectPath(string? redirectPath)
    {
        if (string.IsNullOrWhiteSpace(redirectPath) ||
            !redirectPath.StartsWith("/", StringComparison.Ordinal) ||
            redirectPath.StartsWith("//", StringComparison.Ordinal) ||
            redirectPath.Contains('\\'))
        {
            return new NormalizedRedirectPath(
                ProductContract.Current.Routes.Canonical["postSignInDefault"],
                !string.IsNullOrWhiteSpace(redirectPath));
        }

        var normalizedRoute = ProductContract.Current.NormalizeRouteAlias(redirectPath);
        var pathOnly = normalizedRoute.Split('?', '#')[0];
        var isAllowed = RedirectAllowlist.Any(allowed =>
            allowed == "/" ? pathOnly == "/" : pathOnly.Equals(allowed, StringComparison.OrdinalIgnoreCase) || pathOnly.StartsWith($"{allowed}/", StringComparison.OrdinalIgnoreCase))
            ;
        return isAllowed
            ? new NormalizedRedirectPath(normalizedRoute, false)
            : new NormalizedRedirectPath(ProductContract.Current.Routes.Canonical["postSignInDefault"], true);
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

    private sealed record NormalizedRedirectPath(string Path, bool UsedFallback);
}
