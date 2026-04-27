namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
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
                    services.RemoveAll<DbContextOptions<BioStackDbContext>>();
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
        Assert.Equal("http://localhost:3043/profiles", verified.Headers.Location?.ToString());
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
        Assert.Equal("http://localhost:3043/profiles", verified.Headers.Location?.ToString());

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

    [Fact]
    public async Task RedirectAllowlist_IsEnforced()
    {
        await StartAsync("redirect@example.com", "https://evil.example/profiles");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var challenge = await db.AuthChallenges
            .Include(c => c.Identity)
            .SingleAsync(c => c.Identity.ValueNormalized == "redirect@example.com");

        Assert.Equal("/mission-control", challenge.RedirectPath);
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
