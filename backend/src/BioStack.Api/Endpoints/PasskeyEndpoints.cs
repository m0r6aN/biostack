namespace BioStack.Api.Endpoints;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using BioStack.Api.Auth;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;

public static class PasskeyEndpoints
{
    private const string PasskeyIdentityType = "passkey";
    private const string EmailIdentityType = "email";
    private const string RegistrationOperation = "registration";
    private const string AuthenticationOperation = "authentication";
    private static readonly TimeSpan CeremonyLifetime = TimeSpan.FromMinutes(5);

    public static void MapPasskeyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth/passkeys")
            .WithTags("Auth");

        group.MapGet("/status", Status)
            .WithName("GetPasskeyStatus");

        group.MapPost("/register/options", BeginRegistration)
            .WithName("BeginPasskeyRegistration")
            .RequireAuthorization()
            .RequireRateLimiting("auth-verify");

        group.MapPost("/register/complete", CompleteRegistration)
            .WithName("CompletePasskeyRegistration")
            .RequireAuthorization()
            .RequireRateLimiting("auth-verify");

        group.MapPost("/authenticate/options", BeginAuthentication)
            .WithName("BeginPasskeyAuthentication")
            .RequireRateLimiting("auth-start");

        group.MapPost("/authenticate/complete", CompleteAuthentication)
            .WithName("CompletePasskeyAuthentication")
            .RequireRateLimiting("auth-verify");

        group.MapGet("", ListCredentials)
            .WithName("ListPasskeys")
            .RequireAuthorization();

        group.MapDelete("/{credentialId:guid}", RemoveCredential)
            .WithName("RemovePasskey")
            .RequireAuthorization()
            .RequireRateLimiting("auth-verify");
    }

    private static async Task<IResult> Status(
        PasskeyFeatureConfiguration feature,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var userId = CurrentUserId(http.User);
        var count = feature.Enabled && userId.HasValue
            ? await db.PasskeyCredentials.CountAsync(c => c.Identity.UserId == userId.Value, ct)
            : 0;
        return Results.Ok(new { enabled = feature.Enabled, enrolled = count > 0, credentialCount = count });
    }

    private static async Task<IResult> BeginRegistration(
        BeginPasskeyRegistrationRequest request,
        PasskeyFeatureConfiguration feature,
        IFido2 fido2,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        if (!feature.Enabled)
        {
            return Disabled();
        }

        var userId = CurrentUserId(http.User);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var user = await db.AppUsers
            .Include(u => u.AuthIdentities)
            .SingleOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!user.AuthIdentities.Any(i => i.Type == EmailIdentityType && i.IsVerified))
        {
            return Results.Json(
                new { code = "verified_email_required", message = "Verify your email before adding a passkey." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var existing = await db.PasskeyCredentials
            .Where(c => c.Identity.UserId == user.Id)
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToListAsync(ct);
        var userHandle = user.Id.ToByteArray();
        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = userHandle,
                Name = user.Email,
                DisplayName = user.DisplayName,
            },
            ExcludeCredentials = existing,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required,
            },
            AttestationPreference = AttestationConveyancePreference.None,
        });

        var requestId = await StoreChallengeAsync(
            RegistrationOperation,
            user.Id,
            options.ToJson(),
            "/",
            db,
            http,
            ct);
        return Results.Ok(new { requestId, publicKey = ToProtocolJson(options.ToJson()) });
    }

    private static async Task<IResult> CompleteRegistration(
        CompletePasskeyRegistrationRequest request,
        PasskeyFeatureConfiguration feature,
        IFido2 fido2,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        if (!feature.Enabled)
        {
            return Disabled();
        }

        var userId = CurrentUserId(http.User);
        if (!userId.HasValue || string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Results.Unauthorized();
        }

        var challenge = await ClaimChallengeAsync(request.RequestId, RegistrationOperation, userId, db, ct);
        if (challenge is null)
        {
            return InvalidCeremony();
        }

        try
        {
            var options = CredentialCreateOptions.FromJson(challenge.OptionsJson);
            var result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = request.Credential,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = async (args, callbackCt) =>
                    !await db.PasskeyCredentials.AnyAsync(c => c.CredentialId == args.CredentialId, callbackCt),
            }, ct);

            if (!result.User.Id.AsSpan().SequenceEqual(userId.Value.ToByteArray()))
            {
                return InvalidCeremony();
            }

            var now = DateTime.UtcNow;
            var identity = new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                Type = PasskeyIdentityType,
                ValueNormalized = HashBytes(result.Id),
                IsVerified = true,
                CreatedAtUtc = now,
                VerifiedAtUtc = now,
            };
            var credential = new PasskeyCredential
            {
                Id = Guid.NewGuid(),
                IdentityId = identity.Id,
                CredentialId = result.Id,
                PublicKey = result.PublicKey,
                UserHandle = result.User.Id,
                CredentialType = result.Type.ToString().ToLowerInvariant().Replace("publickey", "public-key", StringComparison.Ordinal),
                SignatureCounter = result.SignCount,
                Transports = string.Join(',', result.Transports.Select(value => value.ToString().ToLowerInvariant())),
                AaGuid = result.AaGuid,
                IsBackupEligible = result.IsBackupEligible,
                IsBackedUp = result.IsBackedUp,
                DisplayName = NormalizeDisplayName(request.DisplayName),
                CreatedAtUtc = now,
            };
            identity.PasskeyCredential = credential;
            db.AuthIdentities.Add(identity);
            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(credential));
        }
        catch (Fido2VerificationException)
        {
            return InvalidCeremony();
        }
        catch (DbUpdateException)
        {
            return InvalidCeremony();
        }
    }

    private static async Task<IResult> BeginAuthentication(
        BeginPasskeyAuthenticationRequest request,
        PasskeyFeatureConfiguration feature,
        IFido2 fido2,
        BioStackDbContext db,
        HttpContext http,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (!feature.Enabled)
        {
            return Disabled();
        }

        var normalizedRedirect = AuthEndpoints.NormalizeRedirectPath(request.RedirectPath);
        if (normalizedRedirect.UsedFallback && !string.IsNullOrWhiteSpace(request.RedirectPath))
        {
            loggerFactory.CreateLogger("BioStack.AuthFlow").LogWarning(
                new EventId(6902, "PasskeyReturnPathRejected"),
                "Rejected an unapproved passkey return path and used the canonical fallback.");
        }

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = [],
            UserVerification = UserVerificationRequirement.Required,
        });
        var requestId = await StoreChallengeAsync(
            AuthenticationOperation,
            null,
            options.ToJson(),
            normalizedRedirect.Path,
            db,
            http,
            ct);
        return Results.Ok(new { requestId, publicKey = ToProtocolJson(options.ToJson()) });
    }

    private static async Task<IResult> CompleteAuthentication(
        CompletePasskeyAuthenticationRequest request,
        PasskeyFeatureConfiguration feature,
        IFido2 fido2,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        if (!feature.Enabled)
        {
            return Disabled();
        }

        var challenge = await ClaimChallengeAsync(request.RequestId, AuthenticationOperation, null, db, ct);
        if (challenge is null || request.Credential.RawId is not { Length: > 0 })
        {
            return InvalidCeremony();
        }

        var credential = await db.PasskeyCredentials
            .Include(c => c.Identity)
            .ThenInclude(i => i.User)
            .SingleOrDefaultAsync(c => c.CredentialId == request.Credential.RawId, ct);
        if (credential is null || !credential.Identity.IsVerified)
        {
            return InvalidCeremony();
        }

        if (credential.SignatureCounter is < 0 or > uint.MaxValue ||
            !string.Equals(credential.CredentialType, "public-key", StringComparison.Ordinal))
        {
            return InvalidCeremony();
        }

        try
        {
            var options = AssertionOptions.FromJson(challenge.OptionsJson);
            var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = request.Credential,
                OriginalOptions = options,
                StoredPublicKey = credential.PublicKey,
                StoredSignatureCounter = checked((uint)credential.SignatureCounter),
                IsUserHandleOwnerOfCredentialIdCallback = (args, _) =>
                    Task.FromResult(
                        args.CredentialId.AsSpan().SequenceEqual(credential.CredentialId) &&
                        args.UserHandle.AsSpan().SequenceEqual(credential.UserHandle)),
            }, ct);

            credential.SignatureCounter = result.SignCount;
            credential.IsBackedUp = result.IsBackedUp;
            credential.LastUsedAtUtc = DateTime.UtcNow;
            var redirectPath = AuthEndpoints.NormalizeRedirectPath(challenge.RedirectPath).Path;
            var finalRedirect = await AuthSessionIssuer.SignInAsync(credential.Identity.User, redirectPath, db, http, ct);
            return Results.Ok(new { redirectPath = finalRedirect });
        }
        catch (Fido2VerificationException)
        {
            return InvalidCeremony();
        }
    }

    private static async Task<IResult> ListCredentials(
        PasskeyFeatureConfiguration feature,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        if (!feature.Enabled)
        {
            return Disabled();
        }

        var userId = CurrentUserId(http.User);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var credentials = await db.PasskeyCredentials
            .Where(c => c.Identity.UserId == userId.Value)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => ToResponse(c))
            .ToListAsync(ct);
        return Results.Ok(credentials);
    }

    private static async Task<IResult> RemoveCredential(
        Guid credentialId,
        PasskeyFeatureConfiguration feature,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        if (!feature.Enabled)
        {
            return Disabled();
        }

        var userId = CurrentUserId(http.User);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var credential = await db.PasskeyCredentials
            .Include(c => c.Identity)
            .SingleOrDefaultAsync(c => c.Id == credentialId && c.Identity.UserId == userId.Value, ct);
        if (credential is null)
        {
            return Results.NotFound();
        }

        var hasVerifiedEmailRecovery = await db.AuthIdentities.AnyAsync(
            i => i.UserId == userId.Value && i.Type == EmailIdentityType && i.IsVerified,
            ct);
        if (!hasVerifiedEmailRecovery)
        {
            return Results.Conflict(new
            {
                code = "recovery_required",
                message = "Add and verify an email recovery identity before removing this passkey.",
            });
        }

        db.AuthIdentities.Remove(credential.Identity);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<string> StoreChallengeAsync(
        string operation,
        Guid? userId,
        string optionsJson,
        string redirectPath,
        BioStackDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var requestId = AuthSessionIssuer.GenerateToken();
        db.PasskeyOperationChallenges.Add(new PasskeyOperationChallenge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Operation = operation,
            RequestIdHash = AuthSessionIssuer.HashSecret(requestId),
            OptionsJson = optionsJson,
            RedirectPath = redirectPath,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(CeremonyLifetime),
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
        });
        await db.SaveChangesAsync(ct);
        return requestId;
    }

    private static async Task<PasskeyOperationChallenge?> ClaimChallengeAsync(
        string? requestId,
        string operation,
        Guid? userId,
        BioStackDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return null;
        }

        var requestIdHash = AuthSessionIssuer.HashSecret(requestId);
        var now = DateTime.UtcNow;
        var claimed = await db.PasskeyOperationChallenges
            .Where(c =>
                c.RequestIdHash == requestIdHash &&
                c.Operation == operation &&
                c.UserId == userId &&
                c.ConsumedAtUtc == null &&
                c.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.ConsumedAtUtc, now)
                .SetProperty(c => c.AttemptCount, c => c.AttemptCount + 1), ct);
        if (claimed != 1)
        {
            await db.PasskeyOperationChallenges
                .Where(c => c.RequestIdHash == requestIdHash && c.Operation == operation)
                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.AttemptCount, c => c.AttemptCount + 1), ct);
            return null;
        }

        return await db.PasskeyOperationChallenges.SingleAsync(
            c => c.RequestIdHash == requestIdHash && c.Operation == operation,
            ct);
    }

    private static Guid? CurrentUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static object ToResponse(PasskeyCredential credential) => new
    {
        credential.Id,
        credential.DisplayName,
        transports = credential.Transports.Split(',', StringSplitOptions.RemoveEmptyEntries),
        credential.IsBackupEligible,
        credential.IsBackedUp,
        credential.CreatedAtUtc,
        credential.LastUsedAtUtc,
    };

    private static string NormalizeDisplayName(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? "Passkey"
            : normalized[..Math.Min(normalized.Length, 100)];
    }

    private static string HashBytes(byte[] value) =>
        Convert.ToHexString(SHA256.HashData(value));

    private static JsonElement ToProtocolJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static IResult Disabled() => Results.NotFound(new
    {
        code = "passkeys_disabled",
        message = "Passkey authentication is not enabled on this deployment.",
    });

    private static IResult InvalidCeremony() => Results.BadRequest(new
    {
        code = "invalid_passkey",
        message = "This passkey request is invalid, expired, or already used.",
    });

    public sealed record BeginPasskeyRegistrationRequest(string? DisplayName);
    public sealed record CompletePasskeyRegistrationRequest(
        string RequestId,
        AuthenticatorAttestationRawResponse Credential,
        string? DisplayName);
    public sealed record BeginPasskeyAuthenticationRequest(string? RedirectPath);
    public sealed record CompletePasskeyAuthenticationRequest(
        string RequestId,
        AuthenticatorAssertionRawResponse Credential);
}
