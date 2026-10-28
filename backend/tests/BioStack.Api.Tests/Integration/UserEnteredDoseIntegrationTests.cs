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

/// <summary>
/// User-entered structured dose (amount, unit, frequency, time or schedule) on compound records.
/// The dose is only ever what the user typed: these tests pin validation, persistence, the consent
/// gate, the schedule and provider-summary read paths, and that deleting the profile removes it.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UserEnteredDoseIntegrationTests : IAsyncLifetime
{
    private const string Email = "dose-owner@example.com";

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-dose-{Guid.NewGuid():N}.db");
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
        await SignInAsync();
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

    private static CreateCompoundRequest Request(
        string name = "BPC-157",
        decimal? amount = null,
        string? unit = null,
        string? frequency = null,
        string? time = null,
        CompoundStatus status = CompoundStatus.Active) => new(
            name,
            CompoundCategory.Peptide,
            DateTime.UtcNow.Date.AddDays(-2),
            null,
            status,
            "my own notes",
            SourceType.Manual,
            "",
            "",
            null,
            amount,
            unit,
            frequency,
            time);

    // ── persistence ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_With250McgTwiceWeekly_PersistsAndIsReturnedVerbatim()
    {
        var profileId = await CreateProfileAsync();

        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg", frequency: "twice weekly"));

        Assert.Equal(250m, created.DoseAmount);
        Assert.Equal("mcg", created.DoseUnit);
        Assert.Equal("twice weekly", created.DoseFrequency);
        Assert.Null(created.DoseTime);
        Assert.Equal("my own notes", created.Notes);

        var stored = await LoadRecordAsync(created.Id);
        Assert.Equal(250m, stored.DoseAmount);
        Assert.Equal("mcg", stored.DoseUnit);
        Assert.Equal("twice weekly", stored.DoseFrequency);

        var listed = (await _client.GetFromJsonAsync<List<CompoundResponse>>($"/api/v1/profiles/{profileId}/compounds", JsonOptions))!;
        var single = Assert.Single(listed);
        Assert.Equal(250m, single.DoseAmount);
        Assert.Equal("mcg", single.DoseUnit);
        Assert.Equal("twice weekly", single.DoseFrequency);
    }

    [Fact]
    public async Task Create_WithoutAnyDose_LeavesEveryDoseFieldBlank_NothingIsPrefilledOrComputed()
    {
        var profileId = await CreateProfileAsync();

        var created = await CreateAsync(profileId, Request());

        Assert.Null(created.DoseAmount);
        Assert.Null(created.DoseUnit);
        Assert.Null(created.DoseFrequency);
        Assert.Null(created.DoseTime);

        var stored = await LoadRecordAsync(created.Id);
        Assert.Null(stored.DoseAmount);
        Assert.Null(stored.DoseUnit);
        Assert.Null(stored.DoseFrequency);
        Assert.Null(stored.DoseTime);
    }

    [Fact]
    public async Task Create_NormalisesUnitSpelling_AndTrimsFrequencyAndTime()
    {
        var profileId = await CreateProfileAsync();

        var created = await CreateAsync(profileId, Request(amount: 1.5m, unit: "ML", frequency: "  daily ", time: " 8 AM "));

        Assert.Equal("mL", created.DoseUnit);
        Assert.Equal("daily", created.DoseFrequency);
        Assert.Equal("8 AM", created.DoseTime);
    }

    [Fact]
    public async Task Update_ChangesAndClearsTheDose_AndNotesAreKept()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg", frequency: "twice weekly"));

        var changed = await UpdateAsync(profileId, created, amount: 0.5m, unit: "mg", frequency: "daily", time: "evening");
        Assert.Equal(0.5m, changed.DoseAmount);
        Assert.Equal("mg", changed.DoseUnit);
        Assert.Equal("daily", changed.DoseFrequency);
        Assert.Equal("evening", changed.DoseTime);
        Assert.Equal("my own notes", changed.Notes);

        var cleared = await UpdateAsync(profileId, created, amount: null, unit: null, frequency: null, time: null);
        Assert.Null(cleared.DoseAmount);
        Assert.Null(cleared.DoseUnit);
        Assert.Null(cleared.DoseFrequency);
        Assert.Null(cleared.DoseTime);
        Assert.Equal("my own notes", cleared.Notes);

        var stored = await LoadRecordAsync(created.Id);
        Assert.Null(stored.DoseAmount);
        Assert.Null(stored.DoseUnit);
    }

    [Fact]
    public async Task ExistingRowsWithoutDoseColumns_KeepWorking()
    {
        // A row written before this release has NULL in every new column.
        var profileId = await CreateProfileAsync();
        var legacyId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            db.CompoundRecords.Add(new CompoundRecord
            {
                Id = legacyId,
                PersonId = profileId,
                Name = "Legacy compound",
                Category = CompoundCategory.Peptide,
                Status = CompoundStatus.Active,
                Notes = "250 mcg twice weekly (free text from before the dose fields)",
                StartDate = DateTime.UtcNow.Date.AddDays(-30),
            });
            await db.SaveChangesAsync();
        }

        var listed = (await _client.GetFromJsonAsync<List<CompoundResponse>>($"/api/v1/profiles/{profileId}/compounds", JsonOptions))!;
        var legacy = Assert.Single(listed);
        Assert.Equal(legacyId, legacy.Id);
        Assert.Null(legacy.DoseAmount);
        Assert.Null(legacy.DoseUnit);
        // Free-text Notes are left exactly as the user wrote them; nothing is parsed into the new fields.
        Assert.Contains("250 mcg twice weekly", legacy.Notes, StringComparison.Ordinal);

        var updated = await UpdateAsync(profileId, legacy, amount: null, unit: null, frequency: null, time: null);
        Assert.Equal("Legacy compound", updated.Name);
    }

    // ── validation ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, "mg", "doseAmount")]
    [InlineData(-5, "mg", "doseAmount")]
    [InlineData(250, null, "doseUnit")]
    [InlineData(250, "tablets", "doseUnit")]
    public async Task Create_WithInvalidDose_Returns400KeyedByTheOffendingField(double amount, string? unit, string field)
    {
        var profileId = await CreateProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            Request(amount: (decimal)amount, unit: unit),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out var messages), $"expected an error for {field}");
        Assert.False(string.IsNullOrWhiteSpace(messages[0].GetString()));

        Assert.Empty(await _client.GetFromJsonAsync<List<CompoundResponse>>($"/api/v1/profiles/{profileId}/compounds", JsonOptions) ?? []);
    }

    [Fact]
    public async Task Create_WithUnitButNoAmount_IsRejected()
    {
        var profileId = await CreateProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds", Request(unit: "mg"), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.GetProperty("errors").TryGetProperty("doseAmount", out _));
    }

    [Fact]
    public async Task Create_WithOverlongFrequency_IsRejected()
    {
        var profileId = await CreateProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds", Request(frequency: new string('x', 101)), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.GetProperty("errors").TryGetProperty("doseFrequency", out _));
    }

    [Fact]
    public async Task Update_WithInvalidDose_Returns400_AndKeepsTheStoredDose()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg"));

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds/{created.Id}",
            new UpdateCompoundRequest(created.Name, created.Category, created.StartDate, null, created.Status, created.Notes, created.SourceType,
                created.Goal, created.Source, created.PricePaid, -1m, "mcg", null, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = await LoadRecordAsync(created.Id);
        Assert.Equal(250m, stored.DoseAmount);
    }

    [Fact]
    public async Task Create_AcceptsVeryLargeAndVerySmallAmounts_BecauseThereAreNoRecommendedRanges()
    {
        var profileId = await CreateProfileAsync();

        var big = await CreateAsync(profileId, Request("Big", amount: 100000m, unit: "units"));
        var small = await CreateAsync(profileId, Request("Small", amount: 0.0001m, unit: "g"));

        Assert.Equal(100000m, big.DoseAmount);
        Assert.Equal(0.0001m, small.DoseAmount);
    }

    // ── consent gate (Washington MHMDA) ─────────────────────────────────────

    [Fact]
    public async Task Create_WithDose_WithoutCurrentConsent_Returns403_AndStoresNothing()
    {
        var profileId = await CreateProfileAsync();
        await SetConsentAsync(accepted: false);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds", Request(amount: 250m, unit: "mcg", frequency: "twice weekly"), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", body.GetProperty("code").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Empty(await db.CompoundRecords.Where(c => c.PersonId == profileId).ToListAsync());
    }

    [Fact]
    public async Task Update_WithDose_WithoutCurrentConsent_Returns403_AndKeepsTheStoredDose()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg"));
        await SetConsentAsync(accepted: false);

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds/{created.Id}",
            new UpdateCompoundRequest(created.Name, created.Category, created.StartDate, null, created.Status, created.Notes, created.SourceType,
                created.Goal, created.Source, created.PricePaid, 999m, "mg", "daily", null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(250m, (await LoadRecordAsync(created.Id)).DoseAmount);
    }

    // ── schedule (/my-protocol) and provider summary ────────────────────────

    [Fact]
    public async Task Schedule_ShowsTheUsersOwnDoseAndTime_AndNothingForCompoundsWithoutOne()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request("Entered", amount: 250m, unit: "mcg", frequency: "twice weekly", time: "mornings"));
        await CreateAsync(profileId, Request("Blank"));

        var day = (await _client.GetFromJsonAsync<DayScheduleResponse>($"/api/v1/profiles/{profileId}/protocol/schedule", JsonOptions))!;

        var entered = Assert.Single(day.Items, i => i.Name == "Entered");
        Assert.Equal("250 mcg · twice weekly", entered.Dose);
        Assert.Equal("mornings", entered.Time);

        var blank = Assert.Single(day.Items, i => i.Name == "Blank");
        Assert.Null(blank.Dose);
        Assert.Equal("As scheduled", blank.Time);
    }

    [Fact]
    public async Task Portal_TodaysSchedule_ShowsTheDoseTheUserEntered()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(amount: 250m, unit: "mcg", frequency: "twice weekly"));
        await UpsertCommanderSubscriptionAsync();

        var portal = (await _client.GetFromJsonAsync<ProtocolPortalResponse>($"/api/v1/profiles/{profileId}/protocol/portal", JsonOptions))!;

        var item = Assert.Single(portal.Today.Items);
        Assert.Equal("250 mcg · twice weekly", item.Dose);
    }

    [Fact]
    public async Task SavedProtocol_SnapshotsTheDose_ForTheProviderSummary()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg", frequency: "twice weekly", time: "mornings"));

        var save = await _client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/protocols", new SaveProtocolRequest("Dose snapshot"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, save.StatusCode);
        var protocol = (await save.Content.ReadFromJsonAsync<ProtocolResponse>(JsonOptions))!;

        var item = Assert.Single(protocol.Items);
        Assert.Equal(250m, item.Compound!.DoseAmount);
        Assert.Equal("mcg", item.Compound.DoseUnit);
        Assert.Equal("twice weekly", item.Compound.DoseFrequency);
        Assert.Equal("mornings", item.Compound.DoseTime);

        // The saved protocol keeps what the user entered at save time even if they later change the live record.
        await UpdateAsync(profileId, created, amount: 3m, unit: "mg", frequency: "daily", time: null);
        var reread = (await _client.GetFromJsonAsync<List<ProtocolResponse>>($"/api/v1/profiles/{profileId}/protocols", JsonOptions))!;
        var snapshot = Assert.Single(Assert.Single(reread).Items).Compound!;
        Assert.Equal(250m, snapshot.DoseAmount);
        Assert.Equal("mcg", snapshot.DoseUnit);
        Assert.Equal("twice weekly", snapshot.DoseFrequency);
    }

    // ── deletion and export ─────────────────────────────────────────────────

    [Fact]
    public async Task DeletingTheCompound_RemovesItsDose()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg", frequency: "twice weekly"));

        var response = await _client.DeleteAsync($"/api/v1/profiles/{profileId}/compounds/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.False(await db.CompoundRecords.AnyAsync(c => c.Id == created.Id));
    }

    [Fact]
    public async Task DeletingTheProfile_RemovesEveryStoredDoseOnIt()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request("One", amount: 250m, unit: "mcg", frequency: "twice weekly"));
        await CreateAsync(profileId, Request("Two", amount: 5m, unit: "IU", time: "night"));

        var response = await _client.DeleteAsync($"/api/v1/profiles/{profileId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Empty(await db.CompoundRecords.Where(c => c.PersonId == profileId).ToListAsync());
        Assert.Empty(await db.CompoundRecords.Where(c => c.DoseAmount != null || c.DoseFrequency != null || c.DoseTime != null).ToListAsync());
    }

    [Fact]
    public async Task ExportRead_ReturnsEveryDoseFieldTheUserEntered_ForEachCompound()
    {
        // The compound list is the complete per-profile read of a user's entries (the operations
        // report export only carries counts). Every dose field the user entered must come back.
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request("One", amount: 250m, unit: "mcg", frequency: "twice weekly", time: "mornings"));

        using var doc = JsonDocument.Parse(await _client.GetStringAsync($"/api/v1/profiles/{profileId}/compounds"));
        var row = doc.RootElement.EnumerateArray().Single();
        Assert.Equal(250m, row.GetProperty("doseAmount").GetDecimal());
        Assert.Equal("mcg", row.GetProperty("doseUnit").GetString());
        Assert.Equal("twice weekly", row.GetProperty("doseFrequency").GetString());
        Assert.Equal("mornings", row.GetProperty("doseTime").GetString());
    }

    [Fact]
    public async Task OtherUsers_CannotReadOrChangeSomeoneElsesDose()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg"));

        using var otherClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SignInAsync(otherClient, "dose-intruder@example.com");

        var read = await otherClient.GetAsync($"/api/v1/profiles/{profileId}/compounds");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);

        var write = await otherClient.PutAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds/{created.Id}",
            new UpdateCompoundRequest(created.Name, created.Category, created.StartDate, null, created.Status, "", created.SourceType,
                "", "", null, 1m, "mg", null, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, write.StatusCode);
        Assert.Equal(250m, (await LoadRecordAsync(created.Id)).DoseAmount);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private async Task<CompoundResponse> CreateAsync(Guid profileId, CreateCompoundRequest request)
    {
        var response = await _client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/compounds", request, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
    }

    private async Task<CompoundResponse> UpdateAsync(
        Guid profileId, CompoundResponse source, decimal? amount, string? unit, string? frequency, string? time)
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds/{source.Id}",
            new UpdateCompoundRequest(source.Name, source.Category, source.StartDate, source.EndDate, source.Status, source.Notes, source.SourceType,
                source.Goal, source.Source, source.PricePaid, amount, unit, frequency, time),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
    }

    private async Task<CompoundRecord> LoadRecordAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.CompoundRecords.AsNoTracking().SingleAsync(c => c.Id == id);
    }

    private async Task UpsertCommanderSubscriptionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var userId = await db.AppUsers.Where(u => u.Email == Email).Select(u => u.Id).SingleAsync();
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            AppUserId = userId,
            StripeCustomerId = "cus_test",
            StripeSubscriptionId = "sub_test",
            CreatedAtUtc = DateTime.UtcNow,
            Tier = ProductTier.Commander,
            ProductCode = "commander",
            Status = SubscriptionStatus.Active,
            CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-10),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(20),
            CancelAtPeriodEnd = false,
            StripePriceId = "price_commander",
            UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task SetConsentAsync(bool accepted)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var user = await db.AppUsers.SingleAsync(u => u.Email == Email);
        user.ConsentAcceptedAtUtc = accepted ? DateTime.UtcNow : null;
        user.ConsentVersion = accepted ? user.ConsentVersion : null;
        await db.SaveChangesAsync();
    }

    private async Task<Guid> CreateProfileAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/profiles",
            new CreateProfileRequest("Dose Owner", Sex.Unspecified, 75m, 30, "goal", "notes"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!.Id;
    }

    private Task SignInAsync() => SignInAsync(_client, Email);

    private static async Task SignInAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/compounds"));
        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        var consent = await client.PostAsJsonAsync("/api/v1/consent", new { });
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
    }
}
