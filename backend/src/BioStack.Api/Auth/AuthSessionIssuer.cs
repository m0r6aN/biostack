namespace BioStack.Api.Auth;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

internal static class AuthSessionIssuer
{
    internal static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    public static async Task<string> SignInAsync(
        AppUser user,
        string redirectPath,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var sessionToken = GenerateToken();
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashSecret(sessionToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(SessionLifetime),
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http.Request.Headers.UserAgent.ToString(),
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(BuildClaims(user, sessionToken), CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = session.ExpiresAtUtc,
                AllowRefresh = false,
            });

        var hasCurrentConsent = user.ConsentAcceptedAtUtc.HasValue &&
            string.Equals(user.ConsentVersion, ConsentGate.CurrentConsentVersion, StringComparison.Ordinal);
        return hasCurrentConsent
            ? redirectPath
            : $"/onboarding/consent?returnTo={Uri.EscapeDataString(redirectPath)}";
    }

    private static IEnumerable<Claim> BuildClaims(AppUser user, string sessionToken) =>
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

    internal static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    internal static string HashSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
