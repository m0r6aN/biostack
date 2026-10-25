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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class WatchlistEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _userA = null!;
    private HttpClient _userB = null!;
    private string _dbPath = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-watch-{Guid.NewGuid():N}.db");
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

        _userA = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _userB = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _userA.Dispose();
        _userB.Dispose();
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
    public async Task Watchlist_RequiresSignIn_OneTapWatch_IsIdempotent_AndOwnerScoped()
    {
        using var anonymous = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/watchlist")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/watchlist", new { targetType = "compound", targetId = "bpc-157" })).StatusCode);

        await SignInAndAcceptConsentAsync(_userA, "watch-a@example.com");
        await SignInAndAcceptConsentAsync(_userB, "watch-b@example.com");

        var first = await _userA.PostAsJsonAsync("/api/v1/watchlist", new { targetType = "compound", targetId = "BPC-157" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var id = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await _userA.PostAsJsonAsync("/api/v1/watchlist", new { targetType = "compound", targetId = "bpc-157" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _userA.PostAsJsonAsync("/api/v1/watchlist", new { targetType = "vs", targetId = "bpc-157/tb-500" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _userA.PostAsJsonAsync("/api/v1/watchlist", new { targetType = "compound", targetId = "a/b" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _userA.PostAsJsonAsync("/api/v1/watchlist", new { targetType = "person", targetId = "x" })).StatusCode);

        var list = await _userA.GetFromJsonAsync<JsonElement>("/api/v1/watchlist");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal(0, (await _userB.GetFromJsonAsync<JsonElement>("/api/v1/watchlist")).GetArrayLength());

        Assert.Equal(HttpStatusCode.NotFound, (await _userB.DeleteAsync($"/api/v1/watchlist/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _userA.DeleteAsync($"/api/v1/watchlist/{id}")).StatusCode);
        Assert.Equal(1, (await _userA.GetFromJsonAsync<JsonElement>("/api/v1/watchlist")).GetArrayLength());
        Assert.Equal(0, (await _userA.GetFromJsonAsync<JsonElement>("/api/v1/alerts/notices")).GetArrayLength());
    }

    [Fact]
    public async Task Preferences_GlobalFrequency_AndPublicOneClickUnsubscribe()
    {
        await SignInAndAcceptConsentAsync(_userA, "prefs-a@example.com");
        var defaults = await _userA.GetFromJsonAsync<JsonElement>("/api/v1/alerts/preferences");
        Assert.Equal("instant", defaults.GetProperty("frequency").GetString());
        Assert.True(defaults.GetProperty("emailEnabled").GetBoolean());

        Assert.Equal(HttpStatusCode.BadRequest, (await _userA.PutAsJsonAsync("/api/v1/alerts/preferences", new { frequency = "hourly", emailEnabled = true })).StatusCode);
        var weekly = await _userA.PutAsJsonAsync("/api/v1/alerts/preferences", new { frequency = "weekly", emailEnabled = true });
        Assert.Equal(HttpStatusCode.OK, weekly.StatusCode);

        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            token = (await db.EvidenceAlertPreferences.SingleAsync()).UnsubscribeToken;
        }
        using var anonymous = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.PostAsync($"/api/v1/alerts/unsubscribe/{token}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.PostAsync("/api/v1/alerts/unsubscribe/not-a-real-token-123456", null)).StatusCode);

        var after = await _userA.GetFromJsonAsync<JsonElement>("/api/v1/alerts/preferences");
        Assert.False(after.GetProperty("emailEnabled").GetBoolean());
        Assert.Equal("weekly", after.GetProperty("frequency").GetString());
    }

    [Fact]
    public void WatchlistMigration_IsDiscoverable_AndCreatesTheThreeTables()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var script = db.GetService<IMigrator>().GenerateScript(
            "20261024010000_MapEvidenceTierToSevenTierScale",
            "20261025000000_AddEvidenceWatchlistAndAlerts");
        Assert.Contains("CREATE TABLE \"EvidenceWatches\"", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"EvidenceAlertPreferences\"", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"EvidenceAlertLog\"", script, StringComparison.Ordinal);
    }

    private async Task SignInAndAcceptConsentAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement
            .EnumerateArray()
            .Single(message => message.GetProperty("contact").GetString() == email)
            .GetProperty("link")
            .GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        var consent = await client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest(null), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
    }

}
