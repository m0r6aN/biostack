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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class ConsentGateIntegrationTests : IAsyncLifetime
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
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-consent-{Guid.NewGuid():N}.db");
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

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
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
    public async Task GetConsent_Anonymous_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/consent");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostConsent_Anonymous_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/consent", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConsent_AfterSignIn_ReportsNotAccepted()
    {
        await SignInAsync("status@example.com");
        var status = await _client.GetFromJsonAsync<ConsentStatusResponse>("/api/v1/consent", JsonOptions);

        Assert.NotNull(status);
        Assert.False(status.Accepted);
        Assert.Null(status.ConsentAcceptedAtUtc);
        Assert.Null(status.ConsentVersion);
    }

    [Fact]
    public async Task PostConsent_RecordsTimestampAndVersion()
    {
        await SignInAsync("recorder@example.com");

        var before = DateTime.UtcNow.AddSeconds(-1);
        var response = await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest(null), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ConsentStatusResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.True(body.Accepted);
        Assert.Equal("bio-observational-v1", body.ConsentVersion);
        Assert.NotNull(body.ConsentAcceptedAtUtc);
        Assert.True(body.ConsentAcceptedAtUtc >= before);

        var fetched = await _client.GetFromJsonAsync<ConsentStatusResponse>("/api/v1/consent", JsonOptions);
        Assert.NotNull(fetched);
        Assert.True(fetched.Accepted);
        Assert.Equal("bio-observational-v1", fetched.ConsentVersion);
        Assert.Equal(body.ConsentAcceptedAtUtc, fetched.ConsentAcceptedAtUtc);
    }

    [Fact]
    public async Task PostConsent_IsIdempotentForSameVersion()
    {
        await SignInAsync("idem@example.com");

        var first = await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest("bio-observational-v1"), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<ConsentStatusResponse>(JsonOptions);

        await Task.Delay(20);

        var second = await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest("bio-observational-v1"), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<ConsentStatusResponse>(JsonOptions);

        Assert.NotNull(firstBody);
        Assert.NotNull(secondBody);
        Assert.Equal(firstBody.ConsentAcceptedAtUtc, secondBody.ConsentAcceptedAtUtc);
    }

    [Fact]
    public async Task ProfileWrite_AnonymousStill401_NotConsentRequired()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/profiles",
            new CreateProfileRequest("Anon", Sex.Unspecified, 70m, 30, "g", "n"), JsonOptions);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProfileWrite_AuthenticatedNoConsent_Returns403WithConsentCode()
    {
        await SignInAsync("profile-blocked@example.com");
        var response = await _client.PostAsJsonAsync("/api/v1/profiles",
            new CreateProfileRequest("Blocked", Sex.Unspecified, 70m, 30, "g", "n"), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", body.GetProperty("code").GetString());
        Assert.Equal("/onboarding/consent", body.GetProperty("url").GetString());
    }

    [Fact]
    public async Task ProfileWrite_AfterConsent_Succeeds()
    {
        await SignInAsync("profile-ok@example.com");
        await AcceptConsentAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/profiles",
            new CreateProfileRequest("Ready", Sex.Unspecified, 70m, 30, "g", "n"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ProfileRead_WithoutConsent_StillReturnsOk()
    {
        await SignInAsync("reader@example.com");
        var response = await _client.GetAsync("/api/v1/profiles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompoundWrite_AuthenticatedNoConsent_Returns403()
    {
        await SignInAsync("compound-blocked@example.com");
        await AcceptConsentAsync();
        var profile = await CreateProfileAsync("Compound Owner");
        await RevokeConsentDirectAsync("compound-blocked@example.com");

        var response = await _client.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/compounds",
            new CreateCompoundRequest(
                "Vitamin D",
                CompoundCategory.Supplement,
                DateTime.UtcNow.Date,
                null,
                CompoundStatus.Active,
                "notes",
                SourceType.Manual,
                "goal",
                "manual",
                10m), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CheckInWrite_AuthenticatedNoConsent_Returns403()
    {
        await SignInAsync("checkin-blocked@example.com");
        await AcceptConsentAsync();
        var profile = await CreateProfileAsync("Check-in Owner");
        await RevokeConsentDirectAsync("checkin-blocked@example.com");

        var response = await _client.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/checkins",
            new CreateCheckInRequest(DateTime.UtcNow.Date, 80m, 7, 7, 7, 7, Notes: "blocked"), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CheckInRead_WithoutConsent_StillReturnsOk()
    {
        await SignInAsync("checkin-reader@example.com");
        await AcceptConsentAsync();
        var profile = await CreateProfileAsync("Reader");
        await RevokeConsentDirectAsync("checkin-reader@example.com");

        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/checkins");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtocolWrite_AuthenticatedNoConsent_Returns403()
    {
        await SignInAsync("protocol-blocked@example.com");
        await AcceptConsentAsync();
        var profile = await CreateProfileAsync("Protocol Owner");
        await RevokeConsentDirectAsync("protocol-blocked@example.com");

        var response = await _client.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/protocols",
            new SaveProtocolRequest("Blocked Stack"), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProtocolRunWrite_AuthenticatedNoConsent_Returns403()
    {
        await SignInAsync("run-blocked@example.com");
        await AcceptConsentAsync();
        var profile = await CreateProfileAsync("Run Owner");

        var compoundResponse = await _client.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/compounds",
            new CreateCompoundRequest(
                "Run Compound",
                CompoundCategory.Supplement,
                DateTime.UtcNow.Date,
                null,
                CompoundStatus.Active,
                "notes",
                SourceType.Manual,
                "goal",
                "manual",
                10m), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, compoundResponse.StatusCode);

        var protocolResponse = await _client.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/protocols",
            new SaveProtocolRequest("Run Stack"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, protocolResponse.StatusCode);
        var protocol = await protocolResponse.Content.ReadFromJsonAsync<ProtocolResponse>(JsonOptions);

        await RevokeConsentDirectAsync("run-blocked@example.com");

        var response = await _client.PostAsync($"/api/v1/protocols/{protocol!.Id}/runs", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", body.GetProperty("code").GetString());
    }

    private async Task<Guid> SignInAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
    }

    private async Task AcceptConsentAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task RevokeConsentDirectAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var user = await db.AppUsers.SingleAsync(u => u.Email == email);
        user.ConsentAcceptedAtUtc = null;
        user.ConsentVersion = null;
        await db.SaveChangesAsync();
    }

    private async Task<ProfileResponse> CreateProfileAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/profiles",
            new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }
}
