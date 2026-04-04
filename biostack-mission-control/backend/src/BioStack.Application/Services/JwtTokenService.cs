namespace BioStack.Application.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Issues short-lived JWTs that the Next.js frontend forwards to every API request.
/// Claims include the user's internal ID, email, display name, and role.
/// The role claim is a plain integer — no "Admin" string is ever sent directly.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int    _expiryMinutes;

    public JwtTokenService(IConfiguration config)
    {
        _secret        = config["Jwt:Secret"]   ?? throw new InvalidOperationException("Jwt:Secret is required");
        _issuer        = config["Jwt:Issuer"]   ?? "biostack";
        _audience      = config["Jwt:Audience"] ?? "biostack-ui";
        _expiryMinutes = int.TryParse(config["Jwt:ExpiryMinutes"], out var m) ? m : 60;
    }

    public string GenerateToken(AppUser user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name",     user.DisplayName),
            new Claim("avatar",   user.AvatarUrl ?? string.Empty),
            new Claim("role",     ((int)user.Role).ToString()),   // 0=User, 1=Admin
            new Claim("provider", user.Provider),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public interface IJwtTokenService
{
    string GenerateToken(AppUser user);
}
