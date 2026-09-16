namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Api.Endpoints;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Xunit;

[Trait("Category", "Integration")]
public sealed class AuthEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-auth-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                        ["FrontendUrl"] = "http://localhost:3043",
                        ["PublicApiUrl"] = "http://localhost:5000",
                        ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveBioStackDbContext();
                    services.AddDbContext<BioStackDbContext>(options =>
                        options.UseSqlite($"Data Source={_dbPath}"));
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task StartAuth_ReturnsNeutralResponseAndStoresOnlyTokenHash()
    {
        var response = await StartAsync("User@Example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("If that email can sign in", body);

        var link = await LatestMagicLinkAsync();
        var token = ReadToken(link);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.AuthChallenges
            .Include(c => c.Identity)
            .SingleAsync(c => c.Identity.ValueNormalized == "user@example.com");

        Assert.NotEqual(token, challenge.TokenHash);
        Assert.DoesNotContain(token, challenge.TokenHash);
        Assert.Equal(64, challenge.TokenHash.Length);
    }

    [Fact]
    public async Task VerifyApi_ExchangesTokenByPostAndDoesNotExposeTokenInRedirect()
    {
        await StartAsync("post-exchange@example.com", "/profiles?bootstrap=tools");
        var token = ReadToken(await LatestMagicLinkAsync());

        var verified = await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(token), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        var result = await verified.Content.ReadFromJsonAsync<VerifyAuthResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("/onboarding/consent?returnTo=%2Fprofiles%3Fbootstrap%3Dtools", result.RedirectPath);
        Assert.DoesNotContain(token, result.RedirectPath);
        Assert.Contains("no-store", verified.Headers.CacheControl?.ToString());
        Assert.Contains("biostack_session", string.Join(";", verified.Headers.GetValues("Set-Cookie")));

        var reused = await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(token), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
    }

    [Fact]
    public async Task StartAuth_ResendInvalidatesEarlierUnusedLink()
    {
        await StartAsync("resend@example.com", "/profiles");
        var firstToken = ReadToken(await LatestMagicLinkAsync());
        await StartAsync("resend@example.com", "/profiles");
        var secondToken = ReadToken(await LatestMagicLinkAsync());

        Assert.NotEqual(firstToken, secondToken);
        var earlier = await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(firstToken), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, earlier.StatusCode);

        var latest = await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(secondToken), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, latest.StatusCode);
    }

    [Fact]
    public async Task Verify_RejectsExpiredTokenAndPreventsReuse()
    {
        await StartAsync("expired@example.com");
        var link = await LatestMagicLinkAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            var challenge = await db.AuthChallenges
                .Include(c => c.Identity)
                .SingleAsync(c => c.Identity.ValueNormalized == "expired@example.com");
            challenge.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var expired = await _client.GetAsync(ReadPathAndQuery(link));
        Assert.Equal(HttpStatusCode.Redirect, expired.StatusCode);
        Assert.Contains("error=invalid-link", expired.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Verify_IssuesSessionAndTokenCannotBeReused()
    {
        await StartAsync("session@example.com", "/profiles");
        var link = await LatestMagicLinkAsync();
        var pathAndQuery = ReadPathAndQuery(link);

        var verified = await _client.GetAsync(pathAndQuery);

        Assert.Equal(HttpStatusCode.Redirect, verified.StatusCode);
        Assert.Equal("http://localhost:3043/onboarding/consent?returnTo=%2Fprofiles", verified.Headers.Location?.ToString());
        Assert.Contains("biostack_session", string.Join(";", verified.Headers.GetValues("Set-Cookie")));

        var session = await _client.GetFromJsonAsync<AuthSessionResponse>("/api/v1/auth/session");
        Assert.NotNull(session);
        Assert.True(session.Authenticated);
        Assert.Equal("session@example.com", session.User?.Email);

        var reused = await _client.GetAsync(pathAndQuery);
        Assert.Equal(HttpStatusCode.Redirect, reused.StatusCode);
        Assert.Contains("error=invalid-link", reused.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Verify_ConcurrentConsumers_IssuesExactlyOneSession()
    {
        await StartAsync("concurrent-session@example.com", "/profiles");
        var pathAndQuery = ReadPathAndQuery(await LatestMagicLinkAsync());
        using var firstClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using var secondClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var responses = await Task.WhenAll(
            firstClient.GetAsync(pathAndQuery),
            secondClient.GetAsync(pathAndQuery));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Redirect, response.StatusCode));
        Assert.Single(responses, response =>
            string.Equals(
                response.Headers.Location?.ToString(),
                "http://localhost:3043/onboarding/consent?returnTo=%2Fprofiles",
                StringComparison.Ordinal));
        Assert.Single(responses, response =>
            response.Headers.Location?.ToString().Contains("error=invalid-link", StringComparison.Ordinal) == true);
        Assert.Single(responses, response =>
            response.Headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value => value.Contains("biostack_session", StringComparison.Ordinal)));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Equal(1, await db.Sessions.CountAsync());
        var challenge = await db.AuthChallenges.SingleAsync();
        Assert.NotNull(challenge.ConsumedAtUtc);
        Assert.Equal(2, challenge.AttemptCount);
    }

    [Fact]
    public async Task Session_WithoutCookie_ReturnsAnonymousContract()
    {
        var response = await _client.GetAsync("/api/v1/auth/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions);
        Assert.NotNull(session);
        Assert.False(session.Authenticated);
        Assert.Null(session.User);
    }

    [Fact]
    public async Task Session_WithUnreadableCookie_ReturnsAnonymousAndExpiresCookie()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/session");
        request.Headers.Add("Cookie", "biostack_session=not-a-valid-protected-ticket");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions);
        Assert.NotNull(session);
        Assert.False(session.Authenticated);
        Assert.Null(session.User);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie =>
            cookie.StartsWith("biostack_session=", StringComparison.Ordinal) &&
            cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MagicLink_NewUserCanLoginCreateProfileSaveCompoundAndRecordCalculation()
    {
        await StartAsync("new-user@example.com", "/profiles");
        var link = await LatestMagicLinkAsync();

        var verified = await _client.GetAsync(ReadPathAndQuery(link));

        Assert.Equal(HttpStatusCode.Redirect, verified.StatusCode);
        Assert.Equal("http://localhost:3043/onboarding/consent?returnTo=%2Fprofiles", verified.Headers.Location?.ToString());

        var session = await _client.GetFromJsonAsync<AuthSessionResponse>("/api/v1/auth/session");
        Assert.NotNull(session);
        Assert.True(session.Authenticated);
        Assert.Equal("new-user@example.com", session.User?.Email);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            var user = await db.AppUsers.SingleAsync(u => u.Email == "new-user@example.com");
            var identity = await db.AuthIdentities.SingleAsync(i => i.UserId == user.Id);
            var activeSession = await db.Sessions.SingleAsync(s => s.UserId == user.Id && s.RevokedAtUtc == null);

            Assert.Equal("email", user.Provider);
            Assert.Equal("new-user@example.com", user.ProviderKey);
            Assert.True(identity.IsVerified);
            Assert.True(activeSession.ExpiresAtUtc > DateTime.UtcNow);
        }

        // Server consent gate: brand-new authenticated users must record consent before
        // creating any data. Posting to /api/v1/profiles without consent now returns 403.
        var blockedBeforeConsent = await _client.PostAsJsonAsync("/api/v1/profiles", new CreateProfileRequest(
            "Magic Link User",
            Sex.Unspecified,
            82.5m,
            34,
            "Validate first-run flow",
            "Created after passwordless sign-in"));
        Assert.Equal(HttpStatusCode.Forbidden, blockedBeforeConsent.StatusCode);
        var blockedBody = await blockedBeforeConsent.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", blockedBody.GetProperty("code").GetString());

        var consentResponse = await _client.PostAsJsonAsync("/api/v1/consent", new { });
        Assert.Equal(HttpStatusCode.OK, consentResponse.StatusCode);

        var profileResponse = await _client.PostAsJsonAsync("/api/v1/profiles", new CreateProfileRequest(
            "Magic Link User",
            Sex.Unspecified,
            82.5m,
            34,
            "Validate first-run flow",
            "Created after passwordless sign-in"));

        Assert.Equal(HttpStatusCode.Created, profileResponse.StatusCode);
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("Magic Link User", profile.DisplayName);

        var compoundResponse = await _client.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/compounds", new CreateCompoundRequest(
            "BPC-157",
            CompoundCategory.Peptide,
            DateTime.UtcNow.Date,
            null,
            CompoundStatus.Active,
            "First saved compound",
            SourceType.Manual,
            "Recovery",
            "Manual entry",
            49.99m));

        Assert.Equal(HttpStatusCode.Created, compoundResponse.StatusCode);
        var compound = await compoundResponse.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions);
        Assert.NotNull(compound);
        Assert.Equal(profile.Id, compound.PersonId);
        Assert.Equal("BPC-157", compound.Name);

        var compounds = await _client.GetFromJsonAsync<CompoundResponse[]>($"/api/v1/profiles/{profile.Id}/compounds", JsonOptions);
        Assert.NotNull(compounds);
        Assert.Contains(compounds, saved => saved.Id == compound.Id);

        var calculationResponse = await _client.PostAsJsonAsync("/api/v1/calculators/reconstitution", new ReconstitutionRequest(5m, 2.5m));
        Assert.Equal(HttpStatusCode.OK, calculationResponse.StatusCode);
        var calculation = await calculationResponse.Content.ReadFromJsonAsync<CalculatorResultResponse>(JsonOptions);
        Assert.NotNull(calculation);
        Assert.Equal(2000m, calculation.Output);
        Assert.Equal("mcg/mL", calculation.Unit);

        var protocolResponse = await _client.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/protocols", new SaveProtocolRequest("First active stack"));
        if (protocolResponse.StatusCode != HttpStatusCode.Created)
        {
            var body = await protocolResponse.Content.ReadAsStringAsync();
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            var profileExists = await db.PersonProfiles.AnyAsync(saved => saved.Id == profile.Id);
            var activeCompounds = await db.CompoundRecords.CountAsync(saved => saved.PersonId == profile.Id && saved.Status == CompoundStatus.Active);
            var protocols = await db.Protocols.CountAsync(saved => saved.PersonId == profile.Id);

            Assert.Fail(
                $"Expected protocol save to return 201 Created, got {(int)protocolResponse.StatusCode} {protocolResponse.StatusCode}. " +
                $"Body: {body}. Profile exists: {profileExists}. Active compounds: {activeCompounds}. Protocols saved: {protocols}.");
        }

        var protocol = await protocolResponse.Content.ReadFromJsonAsync<ProtocolResponse>(JsonOptions);
        Assert.NotNull(protocol);
        Assert.Equal(profile.Id, protocol.PersonId);
        Assert.Contains(protocol.Items, item => item.CompoundRecordId == compound.Id);

        var computationResponse = await _client.PostAsJsonAsync($"/api/v1/protocols/{protocol.Id}/computations", new CreateProtocolComputationRequest(
            null,
            "reconstitution",
            """{"peptideAmountMg":5,"diluentVolumeMl":2.5}""",
            """{"output":2000,"unit":"mcg/mL"}"""));

        Assert.Equal(HttpStatusCode.Created, computationResponse.StatusCode);
        var computation = await computationResponse.Content.ReadFromJsonAsync<ProtocolComputationRecordResponse>(JsonOptions);
        Assert.NotNull(computation);
        Assert.Equal(protocol.Id, computation.ProtocolId);
        Assert.Equal("reconstitution", computation.Type);
    }

    [Fact]
    public async Task VerifyApi_ReturningUserWithCurrentConsentKeepsApprovedReturnPath()
    {
        await StartAsync("returning@example.com", "/profiles");
        var firstToken = ReadToken(await LatestMagicLinkAsync());
        var firstSignIn = await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(firstToken), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, firstSignIn.StatusCode);

        var consent = await _client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
        await _client.PostAsync("/api/v1/auth/logout", null);

        await StartAsync("returning@example.com", "/profiles?bootstrap=tools");
        var returningToken = ReadToken(await LatestMagicLinkAsync());
        var returning = await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(returningToken), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, returning.StatusCode);
        var result = await returning.Content.ReadFromJsonAsync<VerifyAuthResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("/profiles?bootstrap=tools", result.RedirectPath);
    }

    [Fact]
    public async Task Logout_ClearsSession()
    {
        await StartAsync("logout@example.com");
        var link = await LatestMagicLinkAsync();
        await _client.GetAsync(ReadPathAndQuery(link));

        var logout = await _client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("biostack_session=", string.Join(";", logout.Headers.GetValues("Set-Cookie")));

        var session = await _client.GetFromJsonAsync<AuthSessionResponse>("/api/v1/auth/session");
        Assert.NotNull(session);
        Assert.False(session.Authenticated);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Session_ServerStateInvalidation_FailsClosed(bool revoke)
    {
        var email = revoke ? "revoked-session@example.com" : "expired-session@example.com";
        await StartAsync(email, "/profiles");
        await _client.GetAsync(ReadPathAndQuery(await LatestMagicLinkAsync()));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            var session = await db.Sessions
                .Include(item => item.User)
                .SingleAsync(item => item.User.Email == email && item.RevokedAtUtc == null);
            if (revoke)
            {
                session.RevokedAtUtc = DateTime.UtcNow;
            }
            else
            {
                session.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            }

            await db.SaveChangesAsync();
        }

        var sessionResponse = await _client.GetFromJsonAsync<AuthSessionResponse>("/api/v1/auth/session");
        Assert.NotNull(sessionResponse);
        Assert.False(sessionResponse.Authenticated);
        Assert.Null(sessionResponse.User);

        var protectedResponse = await _client.GetAsync("/api/v1/profiles");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task RedirectAllowlist_IsEnforced()
    {
        await StartAsync("redirect@example.com", "https://evil.example/profiles");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.AuthChallenges
            .Include(c => c.Identity)
            .SingleAsync(c => c.Identity.ValueNormalized == "redirect@example.com");

        Assert.Equal(ProductContract.Current.Routes.Canonical["postSignInDefault"], challenge.RedirectPath);
    }

    [Theory]
    [InlineData("/onboarding", "/start")]
    [InlineData("/map", "/tools/analyzer")]
    public async Task RedirectAllowlist_NormalizesContractAliases(string requested, string expected)
    {
        var email = $"alias-{Guid.NewGuid():N}@example.com";
        await StartAsync(email, requested);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.AuthChallenges
            .Include(c => c.Identity)
            .SingleAsync(c => c.Identity.ValueNormalized == email);

        Assert.Equal(expected, challenge.RedirectPath);
    }

    [Fact]
    public async Task RedirectAllowlist_AllowsProtocolPortal()
    {
        await StartAsync("portal-redirect@example.com", "/my-protocol");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.AuthChallenges
            .Include(c => c.Identity)
            .SingleAsync(c => c.Identity.ValueNormalized == "portal-redirect@example.com");

        Assert.Equal("/my-protocol", challenge.RedirectPath);
    }

    [Fact]
    public async Task RedirectAllowlist_AllowsApprovedBillingPlanCallback()
    {
        await StartAsync("billing-redirect@example.com", "/billing?plan=operator");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.AuthChallenges
            .Include(c => c.Identity)
            .SingleAsync(c => c.Identity.ValueNormalized == "billing-redirect@example.com");

        Assert.Equal("/billing?plan=operator", challenge.RedirectPath);
    }

    [Theory]
    [InlineData("internal", "http://localhost:3043", true, true)]
    [InlineData("hybrid", "http://localhost:3043", true, true)]
    [InlineData("smart-card", "http://localhost:3043", true, true)]
    [InlineData("internal", "https://wrong-origin.example", true, false)]
    [InlineData("internal", "http://localhost:3043", false, false)]
    public async Task BrowserPasskeyCredential_RegistersAndAuthenticatesWithProtocolEnumValues(
        string transport, string origin, bool userVerified, bool accepted)
    {
        await StartAsync("browser-passkey@example.com", "/account/security");
        await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(ReadToken(await LatestMagicLinkAsync())), JsonOptions);
        using var optionsResponse = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/register/options", new { displayName = "Test passkey" });
        Assert.Equal(HttpStatusCode.OK, optionsResponse.StatusCode);
        using var options = JsonDocument.Parse(await optionsResponse.Content.ReadAsStringAsync());
        var requestId = options.RootElement.GetProperty("requestId").GetString();
        var publicKey = options.RootElement.GetProperty("publicKey");
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var credentialId = RandomNumberGenerator.GetBytes(32);
        var credentialIdText = WebEncoders.Base64UrlEncode(credentialId);
        var clientData = CreateBrowserClientData("webauthn.create", publicKey.GetProperty("challenge").GetString()!, origin);
        var authData = CreateAuthenticatorData(key, credentialId, registration: true, userVerified);
        var attestation = new CborWriter();
        attestation.WriteStartMap(3);
        attestation.WriteTextString("fmt");
        attestation.WriteTextString("none");
        attestation.WriteTextString("attStmt");
        attestation.WriteStartMap(0);
        attestation.WriteEndMap();
        attestation.WriteTextString("authData");
        attestation.WriteByteString(authData);
        attestation.WriteEndMap();
        // These strings and fields match navigator.credentials.create(), not .NET enum serialization.
        var registration = new
        {
            requestId,
            displayName = "Test passkey",
            credential = new
            {
                id = credentialIdText,
                rawId = credentialIdText,
                type = "public-key",
                response = new
                {
                    attestationObject = WebEncoders.Base64UrlEncode(attestation.Encode()),
                    clientDataJSON = WebEncoders.Base64UrlEncode(clientData),
                    transports = new[] { transport },
                },
                clientExtensionResults = new { credProps = new { rk = true } },
            },
        };
        using var registered = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/register/complete", registration);
        if (!accepted)
        {
            Assert.Equal(HttpStatusCode.BadRequest, registered.StatusCode);
            using var rejectedScope = _factory.Services.CreateScope();
            var rejectedDb = rejectedScope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            Assert.Empty(await rejectedDb.PasskeyCredentials.ToListAsync());
            Assert.NotNull((await rejectedDb.PasskeyOperationChallenges.SingleAsync()).ConsumedAtUtc);
            return;
        }
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        using var replay = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/register/complete", registration);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);

        using var assertionOptionsResponse = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/authenticate/options", new { redirectPath = "/account/security" });
        using var assertionOptions = JsonDocument.Parse(await assertionOptionsResponse.Content.ReadAsStringAsync());
        var assertionClientData = CreateBrowserClientData("webauthn.get", assertionOptions.RootElement.GetProperty("publicKey").GetProperty("challenge").GetString()!);
        var assertionAuthData = CreateAuthenticatorData(key, credentialId, registration: false);
        var signature = key.SignData([.. assertionAuthData, .. SHA256.HashData(assertionClientData)], HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        using var authenticated = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/authenticate/complete", new
        {
            requestId = assertionOptions.RootElement.GetProperty("requestId").GetString(),
            credential = new
            {
                id = credentialIdText,
                rawId = credentialIdText,
                type = "public-key",
                response = new
                {
                    authenticatorData = WebEncoders.Base64UrlEncode(assertionAuthData),
                    clientDataJSON = WebEncoders.Base64UrlEncode(assertionClientData),
                    signature = WebEncoders.Base64UrlEncode(signature),
                    userHandle = publicKey.GetProperty("user").GetProperty("id").GetString(),
                },
                clientExtensionResults = new { },
            },
        });
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var saved = await db.PasskeyCredentials.SingleAsync();
        Assert.Equal("public-key", saved.CredentialType);
        Assert.Equal(transport, saved.Transports);
        Assert.Equal(1u, saved.SignatureCounter);
        Assert.NotNull(saved.LastUsedAtUtc);
    }

    // Reproduces the platform-authenticator behavior behind owner finding #2: Windows Hello and
    // Google Password Manager synced passkeys report a signature counter of 0 on every
    // assertion (they do not track a monotonically increasing counter at all), so the stored
    // counter from registration and the counter reported at sign-in are both 0. Per WebAuthn
    // §7.2 step 21, the clone-detection counter check only runs when the stored or reported
    // counter is nonzero, so 0 vs 0 must be treated as "this authenticator doesn't use a
    // counter" and skipped — not as "the counter did not advance". If Fido2NetLib (or a future
    // change to this endpoint) ever tightened that to require signCount > storedSignCount
    // unconditionally, this is exactly the case that would then fail for every Windows
    // Hello / Google Password Manager passkey while a Fact.Failed real-hardware-key hint like
    // this would not repro on a YubiKey (which does increment). NOTE: could not be executed in
    // this sandbox — `dotnet restore` is blocked (no api.nuget.org egress) — so this is
    // unverified against the real Fido2 4.0.1 assembly; see passkey-diagnostic.md.
    [Fact]
    public async Task DiscoverablePasskeyAssertion_SucceedsWhenAuthenticatorNeverIncrementsSignatureCounter()
    {
        await StartAsync("zero-counter-passkey@example.com", "/account/security");
        await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(ReadToken(await LatestMagicLinkAsync())), JsonOptions);
        using var optionsResponse = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/register/options", new { displayName = "Windows Hello" });
        using var options = JsonDocument.Parse(await optionsResponse.Content.ReadAsStringAsync());
        var requestId = options.RootElement.GetProperty("requestId").GetString();
        var publicKey = options.RootElement.GetProperty("publicKey");
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var credentialId = RandomNumberGenerator.GetBytes(32);
        var credentialIdText = WebEncoders.Base64UrlEncode(credentialId);
        var clientData = CreateBrowserClientData("webauthn.create", publicKey.GetProperty("challenge").GetString()!);
        var authData = CreateAuthenticatorData(key, credentialId, registration: true, signCount: 0);
        var attestation = new CborWriter();
        attestation.WriteStartMap(3);
        attestation.WriteTextString("fmt");
        attestation.WriteTextString("none");
        attestation.WriteTextString("attStmt");
        attestation.WriteStartMap(0);
        attestation.WriteEndMap();
        attestation.WriteTextString("authData");
        attestation.WriteByteString(authData);
        attestation.WriteEndMap();
        using var registered = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/register/complete", new
        {
            requestId,
            displayName = "Windows Hello",
            credential = new
            {
                id = credentialIdText,
                rawId = credentialIdText,
                type = "public-key",
                response = new
                {
                    attestationObject = WebEncoders.Base64UrlEncode(attestation.Encode()),
                    clientDataJSON = WebEncoders.Base64UrlEncode(clientData),
                    transports = new[] { "internal" },
                },
                clientExtensionResults = new { credProps = new { rk = true } },
            },
        });
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

        using var assertionOptionsResponse = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/authenticate/options", new { redirectPath = "/account/security" });
        using var assertionOptions = JsonDocument.Parse(await assertionOptionsResponse.Content.ReadAsStringAsync());
        var assertionClientData = CreateBrowserClientData("webauthn.get", assertionOptions.RootElement.GetProperty("publicKey").GetProperty("challenge").GetString()!);
        // The defect this reproduces: signCount stays 0 here, exactly as Windows Hello and
        // Google Password Manager report it, instead of the 1u every other test in this file uses.
        var assertionAuthData = CreateAuthenticatorData(key, credentialId, registration: false, signCount: 0);
        var signature = key.SignData([.. assertionAuthData, .. SHA256.HashData(assertionClientData)], HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        using var authenticated = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/authenticate/complete", new
        {
            requestId = assertionOptions.RootElement.GetProperty("requestId").GetString(),
            credential = new
            {
                id = credentialIdText,
                rawId = credentialIdText,
                type = "public-key",
                response = new
                {
                    authenticatorData = WebEncoders.Base64UrlEncode(assertionAuthData),
                    clientDataJSON = WebEncoders.Base64UrlEncode(assertionClientData),
                    signature = WebEncoders.Base64UrlEncode(signature),
                    userHandle = publicKey.GetProperty("user").GetProperty("id").GetString(),
                },
                clientExtensionResults = new { },
            },
        });

        var body = await authenticated.Content.ReadAsStringAsync();
        Assert.True(authenticated.StatusCode == HttpStatusCode.OK, $"Expected OK for a zero-counter platform authenticator assertion, got {authenticated.StatusCode}: {body}");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var saved = await db.PasskeyCredentials.SingleAsync();
        Assert.Equal(0u, saved.SignatureCounter);
        Assert.NotNull(saved.LastUsedAtUtc);
    }

    private static byte[] CreateBrowserClientData(string type, string challenge, string origin = "http://localhost:3043") =>
        JsonSerializer.SerializeToUtf8Bytes(new { type, challenge, origin, crossOrigin = false });

    private static byte[] CreateAuthenticatorData(ECDsa key, byte[] credentialId, bool registration, bool userVerified = true, uint? signCount = null)
    {
        using var stream = new MemoryStream();
        stream.Write(SHA256.HashData(Encoding.UTF8.GetBytes("localhost")));
        stream.WriteByte((byte)(0x01 | (userVerified ? 0x04 : 0) | (registration ? 0x40 : 0))); // UP + UV (+ attested credential data).
        Span<byte> counter = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(counter, signCount ?? (registration ? 0u : 1u));
        stream.Write(counter);
        if (registration)
        {
            stream.Write(new byte[16]); // Anonymized AAGUID for none attestation.
            Span<byte> length = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(length, checked((ushort)credentialId.Length));
            stream.Write(length);
            stream.Write(credentialId);
            var parameters = key.ExportParameters(false);
            var cose = new CborWriter();
            cose.WriteStartMap(5);
            cose.WriteInt32(1); cose.WriteInt32(2); // EC2.
            cose.WriteInt32(3); cose.WriteInt32(-7); // ES256.
            cose.WriteInt32(-1); cose.WriteInt32(1); // P-256.
            cose.WriteInt32(-2); cose.WriteByteString(parameters.Q.X!);
            cose.WriteInt32(-3); cose.WriteByteString(parameters.Q.Y!);
            cose.WriteEndMap();
            stream.Write(cose.Encode());
        }
        return stream.ToArray();
    }

    [Fact]
    public async Task PasskeyRegistrationOptions_RequireVerifiedEmailAndStoreHashedSingleUseCeremony()
    {
        await StartAsync("passkey-enroll@example.com", "/account/security");
        var token = ReadToken(await LatestMagicLinkAsync());
        var signedIn = await _client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyAuthRequest(token), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/passkeys/register/options",
            new { displayName = "Windows Hello" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var requestId = body.RootElement.GetProperty("requestId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(requestId));
        var selection = body.RootElement.GetProperty("publicKey").GetProperty("authenticatorSelection");
        Assert.Equal("required", selection.GetProperty("residentKey").GetString());
        Assert.Equal("required", selection.GetProperty("userVerification").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.PasskeyOperationChallenges.SingleAsync();
        Assert.Equal("registration", challenge.Operation);
        Assert.NotEqual(requestId, challenge.RequestIdHash);
        Assert.DoesNotContain(requestId!, challenge.RequestIdHash);
        Assert.Equal(64, challenge.RequestIdHash.Length);
        Assert.InRange(challenge.ExpiresAtUtc - challenge.CreatedAtUtc, TimeSpan.FromMinutes(4.9), TimeSpan.FromMinutes(5.1));
    }

    [Fact]
    public async Task PasskeyRegistrationOptions_RejectAnonymousUsers()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/passkeys/register/options",
            new { displayName = "Anonymous" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PasskeyRegistrationOptions_ExcludesExistingCredentialsWithStoredTransports()
    {
        await StartAsync("passkey-exclude@example.com", "/account/security");
        await _client.PostAsJsonAsync(
            "/api/v1/auth/verify",
            new VerifyAuthRequest(ReadToken(await LatestMagicLinkAsync())),
            JsonOptions);

        var existingCredentialId = RandomNumberGenerator.GetBytes(32);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            var emailIdentity = await db.AuthIdentities.SingleAsync();
            var passkeyIdentity = new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = emailIdentity.UserId,
                Type = "passkey",
                ValueNormalized = new string('B', 64),
                IsVerified = true,
                VerifiedAtUtc = DateTime.UtcNow,
            };
            passkeyIdentity.PasskeyCredential = new PasskeyCredential
            {
                Id = Guid.NewGuid(),
                IdentityId = passkeyIdentity.Id,
                CredentialId = existingCredentialId,
                PublicKey = [4, 5, 6],
                UserHandle = emailIdentity.UserId.ToByteArray(),
                DisplayName = "Existing passkey",
                Transports = "internal,hybrid",
            };
            db.AuthIdentities.Add(passkeyIdentity);
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/passkeys/register/options",
            new { displayName = "Second passkey" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var excludeCredentials = body.RootElement.GetProperty("publicKey").GetProperty("excludeCredentials");
        Assert.Equal(1, excludeCredentials.GetArrayLength());
        var excluded = excludeCredentials[0];
        Assert.Equal("public-key", excluded.GetProperty("type").GetString());
        Assert.Equal(WebEncoders.Base64UrlEncode(existingCredentialId), excluded.GetProperty("id").GetString());
        var transports = excluded.GetProperty("transports").EnumerateArray().Select(t => t.GetString()).ToArray();
        Assert.Equal(new[] { "internal", "hybrid" }, transports);
    }

    [Fact]
    public async Task DiscoverablePasskeyAuthentication_UsesNoCredentialAllowListAndNormalizesRedirect()
    {
        var optionsResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/passkeys/authenticate/options",
            new { redirectPath = "https://evil.example/profiles" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, optionsResponse.StatusCode);
        using var body = JsonDocument.Parse(await optionsResponse.Content.ReadAsStringAsync());
        var publicKey = body.RootElement.GetProperty("publicKey");
        Assert.Equal("required", publicKey.GetProperty("userVerification").GetString());
        Assert.Empty(publicKey.GetProperty("allowCredentials").EnumerateArray());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.PasskeyOperationChallenges.SingleAsync();
        Assert.Equal("authentication", challenge.Operation);
        Assert.Equal(ProductContract.Current.Routes.Canonical["postSignInDefault"], challenge.RedirectPath);
    }

    [Fact]
    public async Task PasskeyAuthenticationChallenge_IsConsumedBeforeCredentialVerificationAndCannotReplay()
    {
        var optionsResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/passkeys/authenticate/options",
            new { redirectPath = "/profiles" },
            JsonOptions);
        using var optionsBody = JsonDocument.Parse(await optionsResponse.Content.ReadAsStringAsync());
        var requestId = optionsBody.RootElement.GetProperty("requestId").GetString();
        var completion = new PasskeyEndpoints.CompletePasskeyAuthenticationRequest(
            requestId!,
            new AuthenticatorAssertionRawResponse
            {
                Id = "AQ",
                RawId = [1],
                Type = PublicKeyCredentialType.PublicKey,
                Response = new AuthenticatorAssertionRawResponse.AssertionResponse
                {
                    AuthenticatorData = [1],
                    Signature = [1],
                    ClientDataJson = [1],
                    UserHandle = null,
                },
                ClientExtensionResults = new AuthenticationExtensionsClientOutputs(),
            });

        var first = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/authenticate/complete", completion);
        var replay = await _client.PostAsJsonAsync("/api/v1/auth/passkeys/authenticate/complete", completion);

        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.PasskeyOperationChallenges.SingleAsync();
        Assert.NotNull(challenge.ConsumedAtUtc);
        Assert.Equal(2, challenge.AttemptCount);
        Assert.Empty(await db.Sessions.ToListAsync());
    }

    [Fact]
    public async Task PasskeyRemoval_FailsClosedWithoutVerifiedEmailRecovery()
    {
        await StartAsync("passkey-remove@example.com");
        await _client.PostAsJsonAsync(
            "/api/v1/auth/verify",
            new VerifyAuthRequest(ReadToken(await LatestMagicLinkAsync())),
            JsonOptions);

        Guid credentialId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            var emailIdentity = await db.AuthIdentities.SingleAsync();
            emailIdentity.IsVerified = false;
            var passkeyIdentity = new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = emailIdentity.UserId,
                Type = "passkey",
                ValueNormalized = new string('A', 64),
                IsVerified = true,
                VerifiedAtUtc = DateTime.UtcNow,
            };
            credentialId = Guid.NewGuid();
            passkeyIdentity.PasskeyCredential = new PasskeyCredential
            {
                Id = credentialId,
                IdentityId = passkeyIdentity.Id,
                CredentialId = [1, 2, 3],
                PublicKey = [4, 5, 6],
                UserHandle = emailIdentity.UserId.ToByteArray(),
                DisplayName = "Recovery guard",
            };
            db.AuthIdentities.Add(passkeyIdentity);
            await db.SaveChangesAsync();
        }

        var response = await _client.DeleteAsync($"/api/v1/auth/passkeys/{credentialId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.True(await verificationDb.PasskeyCredentials.AnyAsync(c => c.Id == credentialId));
    }

    [Fact]
    public async Task DevInbox_ReturnsLatestLink()
    {
        await StartAsync("inbox@example.com", "/timeline");

        var messages = await ReadInboxAsync();

        Assert.NotEmpty(messages);
        Assert.Equal("inbox@example.com", messages[0].GetProperty("contact").GetString());
        Assert.Contains("/auth/verify?token=", messages[0].GetProperty("link").GetString());
        Assert.Equal("/timeline", messages[0].GetProperty("redirectPath").GetString());
    }

    [Fact]
    public async Task OAuthCallbackEndpoint_IsRemoved()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/oauth-callback", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> StartAsync(string contact, string redirectPath = "/mission-control")
        => _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(contact, "email", redirectPath));

    private async Task<string> LatestMagicLinkAsync()
    {
        var messages = await ReadInboxAsync();
        return messages[0].GetProperty("link").GetString()!;
    }

    private async Task<JsonElement[]> ReadInboxAsync()
    {
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        return doc.RootElement.EnumerateArray().Select(element => element.Clone()).ToArray();
    }

    private static string ReadToken(string link)
    {
        var uri = new Uri(link);
        var query = QueryHelpers.ParseQuery(uri.Query);
        return query["token"].FirstOrDefault() ?? throw new InvalidOperationException("Magic link did not include token.");
    }

    private static string ReadPathAndQuery(string link)
    {
        var uri = new Uri(link);
        return $"{uri.AbsolutePath}{uri.Query}";
    }
}
