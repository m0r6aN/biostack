namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// The bio-observational-v2 bump (approved by Clint 2026-10-28): everyone on v1 must re-consent before writing,
/// the routes a person must always be able to reach stay open, and the v1 acceptance is kept as history.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConsentV2ReconsentIntegrationTests : IAsyncLifetime
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
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-consent-v2-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                ["FrontendUrl"] = "http://localhost:3043",
                ["PublicApiUrl"] = "http://localhost:5000",
                ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveBioStackDbContext();
                services.AddDbContext<BioStackDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
            });
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch (IOException) { }
    }

    [Fact]
    public void TheServerPinsVersionTwo()
    {
        Assert.Equal("bio-observational-v2", ConsentGate.CurrentConsentVersion);
    }

    [Fact]
    public async Task AUserWhoAcceptedV1_IsStale_AndWritesAreBlockedUntilTheyAgreeToV2()
    {
        var userId = await SignInAsync("v1-user@example.com");
        await SeedHistoryAsync(userId, acceptedV1: true);

        var status = await _client.GetFromJsonAsync<ConsentStatusResponse>("/api/v1/consent", JsonOptions);
        Assert.NotNull(status);
        Assert.False(status.Accepted);
        Assert.Equal("bio-observational-v1", status.ConsentVersion);          // what the page uses to show "what changed"
        Assert.Equal("bio-observational-v2", status.CurrentVersion);

        var write = await _client.PostAsJsonAsync("/api/v1/profiles",
            new CreateProfileRequest("Stale", Sex.Unspecified, 70m, 30, "g", "n"), JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Equal("consent_required", (await write.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("code").GetString());

        var accept = await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest(null), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        var after = await _client.PostAsJsonAsync("/api/v1/profiles",
            new CreateProfileRequest("Fresh", Sex.Unspecified, 70m, 30, "g", "n"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, after.StatusCode);
    }

    [Fact]
    public async Task TheRoutesAPersonMustAlwaysReach_StayOpenWhileConsentIsOnV1()
    {
        var userId = await SignInAsync("exempt@example.com");
        await SeedHistoryAsync(userId, acceptedV1: true);

        // Reads
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/profiles")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/consent")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/auth/session")).StatusCode);
        // Export (both formats) and the data summary
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/account/export?format=json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/account/export?format=csv")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/account/data-summary")).StatusCode);
        // Deletion (request step; the link goes to the account's email)
        var deletion = await _client.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = "DELETE MY ACCOUNT" });
        Assert.Equal(HttpStatusCode.Accepted, deletion.StatusCode);
        // Declining and signing out
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/v1/consent/decline", new RecordConsentRequest(null), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task SigningInAgain_OnV1_IsSentToTheConsentPage()
    {
        var userId = await SignInAsync("redirect@example.com");
        await SeedHistoryAsync(userId, acceptedV1: true);

        await _client.PostAsync("/api/v1/auth/logout", null);
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest("redirect@example.com", "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = new Uri(doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!);
        var verify = await _client.GetAsync($"{link.AbsolutePath}{link.Query}");

        Assert.Equal(HttpStatusCode.Redirect, verify.StatusCode);
        Assert.Contains("/onboarding/consent", verify.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AcceptingV2_KeepsTheV1AcceptanceInHistory_AndRepeatingAddsNothing()
    {
        var userId = await SignInAsync("history@example.com");
        await SeedHistoryAsync(userId, acceptedV1: true);

        await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest(null), JsonOptions);
        await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest(null), JsonOptions);   // idempotent

        var events = await EventsAsync(userId);
        Assert.Equal(2, events.Count);
        Assert.Equal(("accepted", "bio-observational-v1"), (events[0].EventType, events[0].Version));
        Assert.Equal(("accepted", "bio-observational-v2"), (events[1].EventType, events[1].Version));
        Assert.True(events[0].OccurredAtUtc < events[1].OccurredAtUtc);

        using var scope = _factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().AppUsers.AsNoTracking().SingleAsync(u => u.Id == userId);
        Assert.Equal("bio-observational-v2", user.ConsentVersion);   // latest value moved on; history did not
    }

    [Fact]
    public async Task DeclineThenAccept_RecordsEveryChoiceInOrder()
    {
        var userId = await SignInAsync("sequence@example.com");

        await _client.PostAsJsonAsync("/api/v1/consent/decline", new RecordConsentRequest(null), JsonOptions);
        await _client.PostAsJsonAsync("/api/v1/consent/decline", new RecordConsentRequest(null), JsonOptions);   // idempotent
        await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest(null), JsonOptions);

        var events = await EventsAsync(userId);
        Assert.Equal(["declined", "accepted"], events.Select(e => e.EventType).ToArray());
        Assert.All(events, e => Assert.Equal("bio-observational-v2", e.Version));
    }

    [Fact]
    public async Task ClientCannotInventAVersionInTheHistory()
    {
        var userId = await SignInAsync("invented@example.com");
        await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest("attacker-v999"), JsonOptions);

        Assert.Equal("bio-observational-v2", Assert.Single(await EventsAsync(userId)).Version);
    }

    [Fact]
    public async Task ConsentHistory_IsOnlyVisibleToItsOwner_InTheExport()
    {
        var userId = await SignInAsync("owner-history@example.com");
        await SeedHistoryAsync(userId, acceptedV1: true);
        await _client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest(null), JsonOptions);

        using var other = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SignInAsync(other, "someone-else@example.com");
        var theirs = await other.GetFromJsonAsync<JsonElement>("/api/v1/account/export");
        Assert.Equal(0, theirs.GetProperty("counts").GetProperty("ConsentEvents").GetInt32());

        var mine = await _client.GetFromJsonAsync<JsonElement>("/api/v1/account/export");
        Assert.Equal(2, mine.GetProperty("counts").GetProperty("ConsentEvents").GetInt32());
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────

    private async Task<List<ConsentEvent>> EventsAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return (await db.ConsentEvents.AsNoTracking().Where(e => e.UserId == userId).ToListAsync()).OrderBy(e => e.OccurredAtUtc).ToList();
    }

    /// <summary>The state a v1 user is in after the migration backfill: latest value on the user plus one history row.</summary>
    private async Task SeedHistoryAsync(Guid userId, bool acceptedV1)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var at = DateTime.UtcNow.AddDays(-20);
        var user = await db.AppUsers.SingleAsync(u => u.Id == userId);
        if (acceptedV1)
        {
            user.ConsentAcceptedAtUtc = at;
            user.ConsentVersion = "bio-observational-v1";
            db.ConsentEvents.Add(new ConsentEvent { Id = Guid.NewGuid(), UserId = userId, EventType = ConsentEvent.Accepted, Version = "bio-observational-v1", OccurredAtUtc = at });
        }

        await db.SaveChangesAsync();
    }

    private Task<Guid> SignInAsync(string email) => SignInAsync(_client, email);

    private async Task<Guid> SignInAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First(m => m.GetProperty("contact").GetString() == email).GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().AppUsers.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
    }
}
