namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
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

[Trait("Category", "Integration")]
public sealed class ProtocolPortalEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private Guid _userId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-portal-{Guid.NewGuid():N}.db");
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
        _userId = await SignInAsync("portal-user@example.com");
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
    public async Task GetPortal_ReturnsOk_WithFlatShapeAndSectionMeta()
    {
        var profile = await CreateProfileAsync("Portal Aggregate");
        await UpsertSubscriptionAsync(_userId, ProductTier.Commander);

        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/portal");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        // Flat shape contract — all required top-level keys present.
        foreach (var key in new[]
                 {
                     "overview", "stats", "today", "week", "daySchedules",
                     "diet", "supplements", "monitoring", "milestones", "resources",
                 })
        {
            Assert.True(root.TryGetProperty(key, out _), $"missing key: {key}");
        }

        // Additive provenance map present and includes curated + operational sections.
        Assert.True(root.TryGetProperty("sectionMeta", out var meta));
        Assert.Equal("curated_baseline", meta.GetProperty("diet").GetProperty("status").GetString());
        Assert.Equal("knowledge_baseline", meta.GetProperty("diet").GetProperty("source").GetString());
        Assert.False(string.IsNullOrEmpty(meta.GetProperty("diet").GetProperty("baselineVersion").GetString()));
    }

    [Fact]
    public async Task GetPortal_WithNoLogs_ProducesNoFabricatedOperationalData()
    {
        // Fresh profile: no compounds, no check-ins, no dose logs.
        var profile = await CreateProfileAsync("Honest Empty State");
        await UpsertSubscriptionAsync(_userId, ProductTier.Commander);

        var portal = await _client.GetFromJsonAsync<ProtocolPortalResponse>(
            $"/api/v1/profiles/{profile.Id}/protocol/portal", JsonOptions);
        Assert.NotNull(portal);

        // Adherence must NOT be a fabricated percentage.
        var adherence = portal!.Stats.Single(s => s.Label.StartsWith("Adherence", StringComparison.Ordinal));
        Assert.Equal("Not enough logs yet", adherence.Value);
        Assert.Null(adherence.Unit);

        // Weight trend must NOT be a fabricated number.
        var weight = portal.Stats.Single(s => s.Label == "Weight trend");
        Assert.Equal("Not enough check-ins yet", weight.Value);

        // Next labs must NOT be a fabricated date.
        var labs = portal.Stats.Single(s => s.Label == "Next labs due");
        Assert.Equal("Not scheduled", labs.Value);

        // Today's schedule must be an honest empty state, not invented doses.
        Assert.Empty(portal.Today.Items);
        Assert.Contains("No scheduled items", portal.Today.Subtitle, StringComparison.Ordinal);

        // No day in the week should claim scheduled items.
        Assert.All(portal.Week, day => Assert.Equal(0, day.ItemCount));

        // Provenance reflects unavailable operational data.
        Assert.Equal("unavailable", portal.SectionMeta["stats"].Status);
        Assert.Equal("unavailable", portal.SectionMeta["today"].Status);
        Assert.False(string.IsNullOrEmpty(portal.SectionMeta["today"].EmptyState));
    }

    [Fact]
    public async Task GetPortal_WithDoseLog_ReflectsRealAdherence()
    {
        var profile = await CreateProfileAsync("Real Adherence");
        await CreateActiveCompoundAsync(profile.Id, "Retatrutide");
        await UpsertSubscriptionAsync(_userId, ProductTier.Commander);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var log = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profile.Id}/protocol/doses/log", new LogProtocolDosesRequest(today), JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, log.StatusCode);

        var portal = await _client.GetFromJsonAsync<ProtocolPortalResponse>(
            $"/api/v1/profiles/{profile.Id}/protocol/portal", JsonOptions);
        Assert.NotNull(portal);

        var adherence = portal!.Stats.Single(s => s.Label.StartsWith("Adherence", StringComparison.Ordinal));
        // One of seven days logged → ~14%, derived from a real log (not fabricated).
        Assert.Equal("%", adherence.Unit);
        Assert.NotEqual("Not enough logs yet", adherence.Value);
        Assert.Equal("derived", portal.SectionMeta["stats"].Status);
        Assert.True(portal.Today.Items.Count > 0);
        Assert.Equal("completed", portal.Today.Items[0].Status);
    }

    [Fact]
    public async Task GetActive_ReturnsOverviewAndStats_AtObserverTier()
    {
        var profile = await CreateProfileAsync("Active Section");

        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/active");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var active = await response.Content.ReadFromJsonAsync<ProtocolActiveResponse>(JsonOptions);
        Assert.NotNull(active);
        Assert.Equal("Active Section", active!.Overview.ClientName);
        Assert.NotEmpty(active.Stats);
    }

    [Fact]
    public async Task GetPortal_Returns402ForObserver()
    {
        var profile = await CreateProfileAsync("Observer Aggregate Gate");

        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/portal");

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ProductErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.True(error!.UpgradeRequired);
        Assert.Equal("Observer", error.Tier);
        Assert.Equal("portal_aggregate_commander", error.Code);
    }

    [Fact]
    public async Task GetSupplementsAndResources_ObserverTier_ReturnOk()
    {
        var profile = await CreateProfileAsync("Observer Sections");

        Assert.Equal(HttpStatusCode.OK,
            (await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/supplements")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/resources")).StatusCode);
    }

    [Fact]
    public async Task OperatorGatedSections_Return402ForObserver()
    {
        var profile = await CreateProfileAsync("Observer Gated");

        foreach (var path in new[] { "schedule/week", "diet", "milestones" })
        {
            var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/{path}");
            Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ProductErrorResponse>(JsonOptions);
            Assert.NotNull(error);
            Assert.True(error!.UpgradeRequired);
            Assert.Equal("Observer", error.Tier);
        }
    }

    [Fact]
    public async Task MonitoringSection_Returns402ForOperator_AndOkForCommander()
    {
        var profile = await CreateProfileAsync("Monitoring Gate");

        await UpsertSubscriptionAsync(_userId, ProductTier.Operator);
        var operatorResponse = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/monitoring");
        Assert.Equal(HttpStatusCode.PaymentRequired, operatorResponse.StatusCode);

        // Operator CAN access Operator-gated sections.
        Assert.Equal(HttpStatusCode.OK,
            (await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/diet")).StatusCode);

        await UpsertSubscriptionAsync(_userId, ProductTier.Commander);
        var commanderResponse = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/monitoring");
        Assert.Equal(HttpStatusCode.OK, commanderResponse.StatusCode);
    }

    [Fact]
    public async Task Endpoints_EnforceOwnership_AcrossUsers()
    {
        var profile = await CreateProfileAsync("Owner");

        // Switch identity to a different user; the profile is not theirs.
        await SignInAsync("intruder@example.com");
        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/portal");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LogDoses_ReturnsNoContent_AndIsIdempotentPerDay()
    {
        var profile = await CreateProfileAsync("Dose Log");
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var first = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profile.Id}/protocol/doses/log", new LogProtocolDosesRequest(today), JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profile.Id}/protocol/doses/log", new LogProtocolDosesRequest(today), JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task SendCareTeamMessage_ReturnsNoContent()
    {
        var profile = await CreateProfileAsync("Care Team");

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profile.Id}/care-team/message",
            new CareTeamMessageRequest("Feeling great this week, thanks."), JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_DefaultsToToday_WhenDateOmitted()
    {
        var profile = await CreateProfileAsync("Schedule Default");

        var day = await _client.GetFromJsonAsync<DayScheduleResponse>(
            $"/api/v1/profiles/{profile.Id}/protocol/schedule", JsonOptions);
        Assert.NotNull(day);
        Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM-dd"), day!.DateIso);
    }

    [Fact]
    public async Task GetPortal_NoRealSource_NeverEmitsMockOperationalLiterals()
    {
        // Fresh profile: no protocol items, no compounds, no check-ins, no dose logs.
        // With NO real operational source, any demo/mock operational value in the
        // aggregate would necessarily be fabricated. This guards against accidental
        // mock leakage (e.g. copying frontend/src/lib/mock/protocolPortal.ts values
        // while porting the shape) on the production aggregate path.
        var profile = await CreateProfileAsync("No Mock Leakage");
        await UpsertSubscriptionAsync(_userId, ProductTier.Commander);

        var raw = await _client.GetStringAsync($"/api/v1/profiles/{profile.Id}/protocol/portal");
        var portal = JsonSerializer.Deserialize<ProtocolPortalResponse>(raw, JsonOptions);
        Assert.NotNull(portal);

        // (1) Honest operational empty-states — no fabricated metrics.
        Assert.Equal(
            "Not enough logs yet",
            portal!.Stats.Single(s => s.Label.StartsWith("Adherence", StringComparison.Ordinal)).Value);
        Assert.Equal("Not enough check-ins yet", portal.Stats.Single(s => s.Label == "Weight trend").Value);
        Assert.Equal("Not scheduled", portal.Stats.Single(s => s.Label == "Next labs due").Value);
        Assert.Empty(portal.Today.Items);
        Assert.All(portal.Week, day => Assert.Equal(0, day.ItemCount));
        // No dose may claim a "completed" status when nothing was logged.
        Assert.DoesNotContain(
            portal.Today.Items,
            item => string.Equals(item.Status, "completed", StringComparison.OrdinalIgnoreCase));

        // (2) Curated baseline must remain narrative-only and be flagged as such.
        Assert.False(string.IsNullOrWhiteSpace(portal.Diet.Title));
        Assert.NotEmpty(portal.Diet.Targets);
        Assert.NotEmpty(portal.Supplements.Entries);
        Assert.NotEmpty(portal.Monitoring.AdjustmentRules);
        Assert.NotEmpty(portal.Milestones);
        Assert.NotEmpty(portal.Resources);
        foreach (var key in new[] { "diet", "supplements", "monitoring", "milestones", "resources" })
        {
            Assert.Equal("curated_baseline", portal.SectionMeta[key].Status);
            Assert.Equal("knowledge_baseline", portal.SectionMeta[key].Source);
        }

        // (3) None of the frontend demo payload's known operational literals may appear
        //     anywhere in the operational projection. Scoped to operational strings
        //     (not provenance timestamps) so the assertion stays durable — e.g. a
        //     generatedAtUtc millisecond fragment can never false-positive on "6.4".
        //     Legitimate compound names (e.g. "MOTS-c") are NOT banned: they only ever
        //     reach the projection from a real ProtocolItem, which this profile has none of.
        string[] mockOperationalLiterals =
        {
            "94%", "-6.4", "6.4", "08:00 AM", "08:15 AM", "Completed",
            "Feb 18", "5 days remaining", "3 items",
        };
        var operationalText = string.Join("\n", CollectOperationalStrings(portal));
        foreach (var literal in mockOperationalLiterals)
        {
            Assert.False(
                operationalText.Contains(literal, StringComparison.Ordinal),
                $"mock operational literal leaked into aggregate with no real source: '{literal}'");
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Flattens every operational (non-narrative, non-provenance) string in the
    /// aggregate so the mock-leakage scan targets only the user-specific projection.
    /// </summary>
    private static IEnumerable<string?> CollectOperationalStrings(ProtocolPortalResponse portal)
    {
        yield return portal.Overview.Status;
        yield return portal.Overview.StartedOnUtc;
        yield return portal.Overview.CurrentPhase.Label;

        foreach (var stat in portal.Stats)
        {
            yield return stat.Value;
            yield return stat.Unit;
            yield return stat.Caption;
        }

        var days = new List<DayScheduleResponse> { portal.Today };
        days.AddRange(portal.DaySchedules.Values);
        foreach (var day in days)
        {
            yield return day.Title;
            yield return day.Subtitle;
            foreach (var item in day.Items)
            {
                yield return item.Time;
                yield return item.Name;
                yield return item.Detail;
                yield return item.Status;
            }
        }

        foreach (var weekDay in portal.Week)
        {
            yield return weekDay.DayLabel;
            yield return weekDay.WeekdayLabel;
            yield return weekDay.Tag;
        }
    }

    private async Task<Guid> SignInAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        var consent = await _client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
    }

    private async Task<ProfileResponse> CreateProfileAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/profiles", new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "Metabolic optimization", "notes"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private async Task CreateActiveCompoundAsync(Guid profileId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            new CreateCompoundRequest(name, CompoundCategory.Peptide, DateTime.UtcNow.Date.AddDays(-7), null, CompoundStatus.Active, "notes", SourceType.Manual),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task UpsertSubscriptionAsync(Guid userId, ProductTier tier)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.AppUserId == userId);
        if (subscription is null)
        {
            subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                AppUserId = userId,
                StripeCustomerId = "cus_test",
                StripeSubscriptionId = "sub_test",
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Subscriptions.Add(subscription);
        }

        subscription.Tier = tier;
        subscription.ProductCode = tier.ToString().ToLowerInvariant();
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-10);
        subscription.CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(20);
        subscription.CancelAtPeriodEnd = false;
        subscription.StripePriceId = tier == ProductTier.Commander ? "price_commander" : "price_operator";
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
