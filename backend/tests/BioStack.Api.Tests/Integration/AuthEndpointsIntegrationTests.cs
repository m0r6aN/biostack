namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
