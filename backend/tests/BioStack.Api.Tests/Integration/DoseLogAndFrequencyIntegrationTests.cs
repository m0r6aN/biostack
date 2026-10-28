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
/// The per-compound dose log (the amount the user says they actually took) and the structured
/// frequency the schedule uses. Every amount and every frequency is only what the user entered: these
/// tests pin validation, the consent gate, due-day filtering, history, and removal on deletion.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DoseLogAndFrequencyIntegrationTests : IAsyncLifetime
{
    private const string Email = "doselog-owner@example.com";

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
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-doselog-{Guid.NewGuid():N}.db");
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
                        ["FrontendUrl"] = "http://localhost:3044",
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

    // 2026-10-05 is a Monday; every date below is in the past so logging is never "in the future".
    private static readonly DateTime Start = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string Monday = "2026-10-05";
    private const string Thursday = "2026-10-08";

    private static CreateCompoundRequest Request(
        string name = "BPC-157",
        decimal? amount = null,
        string? unit = null,
        string? frequencyText = null,
        string? kind = null,
        int? interval = null,
        int[]? weekdays = null,
        int? timesPerWeek = null,
        DateTime? frequencyStart = null,
        string[]? times = null,
        DateTime? startDate = null) => new(
            name,
            CompoundCategory.Peptide,
            startDate ?? Start,
            null,
            CompoundStatus.Active,
            "my own notes",
            SourceType.Manual,
            "",
            "",
            null,
            amount,
            unit,
            frequencyText,
            null,
            kind,
            interval,
            weekdays,
            timesPerWeek,
            frequencyStart,
            times);

    private string Url(Guid profileId, string path) => $"/api/v1/profiles/{profileId}/protocol/{path}";

    private async Task<DayScheduleResponse> ScheduleAsync(Guid profileId, string date)
        => (await _client.GetFromJsonAsync<DayScheduleResponse>(Url(profileId, $"schedule?date={date}"), JsonOptions))!;

    private Task<HttpResponseMessage> LogAsync(Guid profileId, string date, params LogDoseEntryRequest[] entries)
        => _client.PostAsJsonAsync(Url(profileId, "doses/log"), new LogProtocolDosesRequest(date, entries), JsonOptions);

    private async Task<List<DoseLogEntryResponse>> HistoryAsync(Guid profileId, string query = "")
        => (await _client.GetFromJsonAsync<List<DoseLogEntryResponse>>(Url(profileId, "doses/history" + query), JsonOptions))!;

    private async Task<int> EntryCountAsync(Guid profileId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.DoseLogEntries.CountAsync(e => e.PersonId == profileId);
    }

    // ── full pipeline: 250 mcg twice weekly on Mon/Thu ──────────────────────

    [Fact]
    public async Task FullPipeline_250McgMonThu_ScheduleOnlyOnThoseDays_LogOnMonday_HistoryShowsIt()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(
            amount: 250m, unit: "mcg", frequencyText: "twice weekly", kind: "weekdays", weekdays: [1, 4]));
        Assert.Equal("weekdays", created.FrequencyKind);
        Assert.Equal([1, 4], created.FrequencyWeekdays);

        // Only Monday and Thursday carry the compound across the whole week.
        var dueDates = new List<string>();
        for (var day = 4; day <= 11; day++)
        {
            var date = $"2026-10-{day:00}";
            var schedule = await ScheduleAsync(profileId, date);
            if (schedule.Items.Count > 0)
            {
                var item = Assert.Single(schedule.Items);
                Assert.Equal("BPC-157", item.Name);
                Assert.Equal("scheduled", item.Due);
                Assert.Equal("Mon, Thu", item.Cadence);
                Assert.Equal("250 mcg · twice weekly", item.Dose);
                dueDates.Add(date);
            }
        }
        Assert.Equal(["2026-10-05", "2026-10-08"], dueDates);

        // The user logs the amount they actually took on Monday. Nothing is prefilled by the server.
        var log = await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 250m, "mcg", "left side"));
        Assert.Equal(HttpStatusCode.NoContent, log.StatusCode);

        var history = Assert.Single(await HistoryAsync(profileId));
        Assert.Equal(created.Id, history.CompoundId);
        Assert.Equal("BPC-157", history.CompoundName);
        Assert.Equal(Monday, history.LogDate);
        Assert.Equal(250m, history.Amount);
        Assert.Equal("mcg", history.Unit);
        Assert.Equal("left side", history.Note);

        var monday = Assert.Single((await ScheduleAsync(profileId, Monday)).Items);
        Assert.Equal("completed", monday.Status);
        var logged = Assert.Single(monday.Logged!);
        Assert.Equal(250m, logged.Amount);
        Assert.Equal("mcg", logged.Unit);

        var thursday = Assert.Single((await ScheduleAsync(profileId, Thursday)).Items);
        Assert.Equal("upcoming", thursday.Status);
        Assert.Empty(thursday.Logged!);
    }

    // ── logging amounts ─────────────────────────────────────────────────────

    [Fact]
    public async Task MarkDayTaken_WithoutEntries_StillWorks_AndIsIdempotent()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(amount: 250m, unit: "mcg"));

        var first = await _client.PostAsJsonAsync(Url(profileId, "doses/log"), new { date = Monday });
        var second = await _client.PostAsJsonAsync(Url(profileId, "doses/log"), new LogProtocolDosesRequest(Monday), JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal("completed", Assert.Single((await ScheduleAsync(profileId, Monday)).Items).Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Equal(1, await db.TimelineEvents.CountAsync(e => e.PersonId == profileId && e.RelatedEntityType == "ProtocolPortalDoseLog"));
        Assert.Empty(await db.DoseLogEntries.ToListAsync());
    }

    [Fact]
    public async Task MarkDayTaken_WithAnEmptyEntriesList_BehavesLikeTheOldRequest()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request());

        var response = await LogAsync(profileId, Monday);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("completed", Assert.Single((await ScheduleAsync(profileId, Monday)).Items).Status);
        Assert.Equal(0, await EntryCountAsync(profileId));
    }

    [Fact]
    public async Task LoggingAmounts_DoesNotMarkOtherCompoundsTaken()
    {
        var profileId = await CreateProfileAsync();
        var first = await CreateAsync(profileId, Request("First"));
        await CreateAsync(profileId, Request("Second"));

        await LogAsync(profileId, Monday, new LogDoseEntryRequest(first.Id, 100m, "mcg"));

        var items = (await ScheduleAsync(profileId, Monday)).Items;
        Assert.Equal("completed", items.Single(i => i.Name == "First").Status);
        Assert.Equal("upcoming", items.Single(i => i.Name == "Second").Status);
    }

    [Fact]
    public async Task LogEntry_ForADifferentAmountThanTheSavedEntry_IsStoredAsTyped()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg"));

        await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 0.5m, "mg"));

        var entry = Assert.Single(await HistoryAsync(profileId));
        Assert.Equal(0.5m, entry.Amount);
        Assert.Equal("mg", entry.Unit);
        Assert.Equal(250m, (await LoadRecordAsync(created.Id)).DoseAmount);
    }

    [Fact]
    public async Task LogEntry_NormalisesUnitSpelling_TrimsNote_AndAllowsTwoDosesOnTheSameDay()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());

        var response = await LogAsync(profileId, Monday,
            new LogDoseEntryRequest(created.Id, 5m, "iu", "  morning  "),
            new LogDoseEntryRequest(created.Id, 5m, "IU", null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var history = await HistoryAsync(profileId);
        Assert.Equal(2, history.Count);
        Assert.All(history, e => Assert.Equal("IU", e.Unit));
        Assert.Contains(history, e => e.Note == "morning");
    }

    [Fact]
    public async Task LogEntry_WithoutAnAmount_IsRejected_EvenWhenTheCompoundHasASavedDose_NothingIsFilledIn()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(amount: 250m, unit: "mcg"));

        var noAmount = await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, null, "mcg"));
        var noUnit = await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 250m, null));
        var neither = await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, noAmount.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noUnit.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, neither.StatusCode);
        Assert.Equal(0, await EntryCountAsync(profileId));
    }

    [Theory]
    [InlineData(0.0, "mcg", "doseAmount")]
    [InlineData(-5.0, "mcg", "doseAmount")]
    [InlineData(0.00001, "mcg", "doseAmount")]
    [InlineData(250.0, "lbs", "doseUnit")]
    [InlineData(250.0, "mcg/kg", "doseUnit")]
    [InlineData(250.0, "cc", "doseUnit")]
    public async Task LogEntry_WithInvalidAmountOrUnit_Returns400KeyedByTheEntryField(double amount, string unit, string field)
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());

        var response = await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, (decimal)amount, unit));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(problem.GetProperty("errors").TryGetProperty($"entries[0].{field}", out _), await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await EntryCountAsync(profileId));
    }

    [Fact]
    public async Task LogEntry_RangesAndNonNumbers_AreRejectedByTheJsonContract()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());

        foreach (var amount in new[] { "\"250-500\"", "\"NaN\"", "\"Infinity\"", "\"1e999\"" })
        {
            var json = $$"""{"date":"{{Monday}}","entries":[{"compoundId":"{{created.Id}}","amount":{{amount}},"unit":"mcg"}]}""";
            var response = await _client.PostAsync(Url(profileId, "doses/log"), new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

            Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnsupportedMediaType, $"{amount} -> {response.StatusCode}");
        }

        Assert.Equal(0, await EntryCountAsync(profileId));
    }

    [Fact]
    public async Task LogEntry_OverlongNote_FutureTime_AndTooManyEntries_AreRejected()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());

        var longNote = await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 1m, "mg", new string('x', 501)));
        var future = await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 1m, "mg", null, DateTime.UtcNow.AddDays(3)));
        var many = await LogAsync(profileId, Monday,
            Enumerable.Range(0, 51).Select(_ => new LogDoseEntryRequest(created.Id, 1m, "mg")).ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, longNote.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, many.StatusCode);
        Assert.Equal(0, await EntryCountAsync(profileId));
    }

    [Fact]
    public async Task LogEntries_AreAllOrNothing_WhenOneEntryIsInvalid()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());

        var response = await LogAsync(profileId, Monday,
            new LogDoseEntryRequest(created.Id, 250m, "mcg"),
            new LogDoseEntryRequest(created.Id, -1m, "mcg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(problem.GetProperty("errors").TryGetProperty("entries[1].doseAmount", out _));
        Assert.Equal(0, await EntryCountAsync(profileId));
    }

    [Fact]
    public async Task LogEntry_ForAnUnknownCompoundOrBadDate_IsRejected()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());

        var unknown = await LogAsync(profileId, Monday, new LogDoseEntryRequest(Guid.NewGuid(), 1m, "mg"));
        var badDate = await LogAsync(profileId, "05/10/2026", new LogDoseEntryRequest(created.Id, 1m, "mg"));

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badDate.StatusCode);
        Assert.Equal(0, await EntryCountAsync(profileId));
    }

    [Fact]
    public async Task LogEntry_ForACompoundOnAnotherProfile_IsRejected()
    {
        var profileA = await CreateProfileAsync();
        var profileB = await CreateProfileAsync();
        var onB = await CreateAsync(profileB, Request("Other"));

        var response = await LogAsync(profileA, Monday, new LogDoseEntryRequest(onB.Id, 1m, "mg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await EntryCountAsync(profileA));
        Assert.Equal(0, await EntryCountAsync(profileB));
    }

    // ── consent gate (Washington MHMDA) ─────────────────────────────────────

    [Fact]
    public async Task LogEntry_WithoutCurrentConsent_Returns403_AndStoresNothing()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());
        await SetConsentAsync(accepted: false);

        var response = await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 250m, "mcg"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", body.GetProperty("code").GetString());
        Assert.Equal(0, await EntryCountAsync(profileId));
    }

    [Fact]
    public async Task MarkDayTaken_WithoutCurrentConsent_StillReturns403()
    {
        var profileId = await CreateProfileAsync();
        await SetConsentAsync(accepted: false);

        var response = await _client.PostAsJsonAsync(Url(profileId, "doses/log"), new { date = Monday });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEntry_WithoutCurrentConsent_Returns403_AndKeepsTheEntry()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());
        await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 250m, "mcg"));
        var entry = Assert.Single(await HistoryAsync(profileId));
        await SetConsentAsync(accepted: false);

        var response = await _client.DeleteAsync(Url(profileId, $"doses/log/{entry.Id}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, await EntryCountAsync(profileId));
    }

    // ── history ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task History_IsNewestFirst_FilterableByDate_AndLimited()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());
        await LogAsync(profileId, "2026-10-05", new LogDoseEntryRequest(created.Id, 1m, "mg"));
        await LogAsync(profileId, "2026-10-08", new LogDoseEntryRequest(created.Id, 2m, "mg"));
        await LogAsync(profileId, "2026-10-12", new LogDoseEntryRequest(created.Id, 3m, "mg"));

        var all = await HistoryAsync(profileId);
        Assert.Equal(["2026-10-12", "2026-10-08", "2026-10-05"], all.Select(e => e.LogDate));

        var ranged = await HistoryAsync(profileId, "?from=2026-10-06&to=2026-10-09");
        Assert.Equal(2m, Assert.Single(ranged).Amount);

        Assert.Equal(2, (await HistoryAsync(profileId, "?limit=2")).Count);

        var bad = await _client.GetAsync(Url(profileId, "doses/history?from=yesterday"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task History_CanBeDeletedEntryByEntry()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());
        await LogAsync(profileId, Monday,
            new LogDoseEntryRequest(created.Id, 1m, "mg"),
            new LogDoseEntryRequest(created.Id, 2m, "mg"));
        var entries = await HistoryAsync(profileId);

        var response = await _client.DeleteAsync(Url(profileId, $"doses/log/{entries[0].Id}"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(await HistoryAsync(profileId));
        var again = await _client.DeleteAsync(Url(profileId, $"doses/log/{entries[0].Id}"));
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task OtherUsers_CannotReadLogOrDeleteSomeoneElsesDoseLog()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());
        await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 250m, "mcg"));
        var entry = Assert.Single(await HistoryAsync(profileId));

        using var other = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SignInAsync(other, "doselog-intruder@example.com");

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Url(profileId, "doses/history"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync(Url(profileId, $"doses/log/{entry.Id}"))).StatusCode);
        var log = await other.PostAsJsonAsync(Url(profileId, "doses/log"),
            new LogProtocolDosesRequest(Monday, [new LogDoseEntryRequest(created.Id, 1m, "mg")]), JsonOptions);
        // The existing log endpoint reports a profile the caller does not own as a 400, as it did before.
        Assert.Equal(HttpStatusCode.BadRequest, log.StatusCode);
        Assert.Equal(1, await EntryCountAsync(profileId));
    }

    [Fact]
    public async Task AnotherProfileOfTheSameUser_CannotDeleteThisProfilesEntry()
    {
        var profileA = await CreateProfileAsync();
        var profileB = await CreateProfileAsync();
        var created = await CreateAsync(profileA, Request());
        await LogAsync(profileA, Monday, new LogDoseEntryRequest(created.Id, 250m, "mcg"));
        var entry = Assert.Single(await HistoryAsync(profileA));

        var response = await _client.DeleteAsync(Url(profileB, $"doses/log/{entry.Id}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, await EntryCountAsync(profileA));
    }

    // ── deletion ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletingTheCompound_RemovesItsLoggedAmounts_AndOnlyThoseOfThatCompound()
    {
        var profileId = await CreateProfileAsync();
        var doomed = await CreateAsync(profileId, Request("Doomed"));
        var kept = await CreateAsync(profileId, Request("Kept"));
        await LogAsync(profileId, Monday,
            new LogDoseEntryRequest(doomed.Id, 1m, "mg"),
            new LogDoseEntryRequest(kept.Id, 2m, "mg"));

        var response = await _client.DeleteAsync($"/api/v1/profiles/{profileId}/compounds/{doomed.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remaining = Assert.Single(await HistoryAsync(profileId));
        Assert.Equal(kept.Id, remaining.CompoundId);
    }

    [Fact]
    public async Task DeletingTheProfile_RemovesEveryLoggedAmountOnIt()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request());
        await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 250m, "mcg"));
        Assert.Equal(1, await EntryCountAsync(profileId));

        var response = await _client.DeleteAsync($"/api/v1/profiles/{profileId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Empty(await db.DoseLogEntries.ToListAsync());
    }

    // ── structured frequency on the compound ────────────────────────────────

    [Fact]
    public async Task Create_WithoutStructuredFrequency_LeavesItBlank_AndNothingIsInferredFromFreeText()
    {
        var profileId = await CreateProfileAsync();

        var created = await CreateAsync(profileId, Request(frequencyText: "twice weekly"));

        Assert.Null(created.FrequencyKind);
        Assert.Null(created.FrequencyIntervalDays);
        Assert.Empty(created.FrequencyWeekdays!);
        Assert.Null(created.FrequencyTimesPerWeek);
        Assert.Null(created.FrequencyStartDate);
        Assert.Empty(created.FrequencyTimesOfDay!);
        var stored = await LoadRecordAsync(created.Id);
        Assert.Null(stored.FrequencyKind);
        Assert.Null(stored.FrequencyWeekdays);
    }

    [Fact]
    public async Task Create_PersistsEveryStructuredFrequencyKind()
    {
        var profileId = await CreateProfileAsync();

        var daily = await CreateAsync(profileId, Request("Daily", kind: "daily", times: ["20:00", "08:00"], frequencyStart: new DateTime(2026, 10, 3)));
        var everyN = await CreateAsync(profileId, Request("EveryN", kind: "every_n_days", interval: 3));
        var perWeek = await CreateAsync(profileId, Request("PerWeek", kind: "times_per_week", timesPerWeek: 2));
        var asNeeded = await CreateAsync(profileId, Request("AsNeeded", kind: "as_needed"));

        Assert.Equal(["08:00", "20:00"], daily.FrequencyTimesOfDay);
        Assert.Equal(new DateTime(2026, 10, 3), daily.FrequencyStartDate!.Value.Date);
        Assert.Equal(3, everyN.FrequencyIntervalDays);
        Assert.Equal(2, perWeek.FrequencyTimesPerWeek);
        Assert.Equal("as_needed", asNeeded.FrequencyKind);
    }

    [Theory]
    [InlineData("monthly", null, null, null, "frequencyKind")]
    [InlineData("every_n_days", 1, null, null, "frequencyIntervalDays")]
    [InlineData("every_n_days", null, null, null, "frequencyIntervalDays")]
    [InlineData("weekdays", null, null, null, "frequencyWeekdays")]
    [InlineData("weekdays", null, new[] { 7 }, null, "frequencyWeekdays")]
    [InlineData("times_per_week", null, null, 0, "frequencyTimesPerWeek")]
    [InlineData("daily", null, new[] { 1 }, null, "frequencyWeekdays")]
    [InlineData(null, null, new[] { 1, 4 }, null, "frequencyKind")]
    public async Task Create_WithInvalidFrequency_Returns400KeyedByTheOffendingField(
        string? kind, int? interval, int[]? weekdays, int? perWeek, string field)
    {
        var profileId = await CreateProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            Request(kind: kind, interval: interval, weekdays: weekdays, timesPerWeek: perWeek),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Update_ChangesAndClearsTheStructuredFrequency_AndKeepsTheFreeText()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(frequencyText: "twice weekly"));

        var update = await _client.PutAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds/{created.Id}",
            new UpdateCompoundRequest(created.Name, created.Category, created.StartDate, null, created.Status, created.Notes, created.SourceType,
                "", "", null, null, null, "twice weekly", null, "weekdays", null, [1, 4], null, null, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
        Assert.Equal([1, 4], updated.FrequencyWeekdays);
        Assert.Equal("twice weekly", updated.DoseFrequency);

        var clear = await _client.PutAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds/{created.Id}",
            new UpdateCompoundRequest(created.Name, created.Category, created.StartDate, null, created.Status, created.Notes, created.SourceType,
                "", "", null, null, null, "twice weekly", null),
            JsonOptions);
        var cleared = (await clear.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
        Assert.Null(cleared.FrequencyKind);
        Assert.Empty(cleared.FrequencyWeekdays!);
        Assert.Null((await LoadRecordAsync(created.Id)).FrequencyWeekdays);
    }

    [Fact]
    public async Task Create_WithStructuredFrequency_WithoutCurrentConsent_Returns403()
    {
        var profileId = await CreateProfileAsync();
        await SetConsentAsync(accepted: false);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds", Request(kind: "weekdays", weekdays: [1, 4]), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── the schedule uses the frequency ─────────────────────────────────────

    [Fact]
    public async Task Schedule_ExistingFreeTextOnlyRow_StillShowsDaily_LabelledWithItsText_AndNoStructuredCadence()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(amount: 250m, unit: "mcg", frequencyText: "twice weekly"));

        for (var day = 5; day <= 11; day++)
        {
            var item = Assert.Single((await ScheduleAsync(profileId, $"2026-10-{day:00}")).Items);
            Assert.Equal("250 mcg · twice weekly", item.Dose);
            Assert.Null(item.Cadence);
            Assert.Equal("scheduled", item.Due);
        }
    }

    [Fact]
    public async Task Schedule_EveryThreeDays_ShowsOnlyOnTheCalendarGrid_AcrossAMonthBoundary()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(kind: "every_n_days", interval: 3, startDate: new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc)));

        var due = new List<string>();
        foreach (var date in new[] { "2026-09-29", "2026-09-30", "2026-10-01", "2026-10-02", "2026-10-03", "2026-10-04", "2026-10-05" })
        {
            if ((await ScheduleAsync(profileId, date)).Items.Count == 1)
            {
                due.Add(date);
            }
        }

        Assert.Equal(["2026-09-29", "2026-10-02", "2026-10-05"], due);
    }

    [Fact]
    public async Task Schedule_EveryTwoDays_AcrossUsDaylightSavingEnd_NeverShiftsOrRepeatsADay()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(kind: "every_n_days", interval: 2, frequencyStart: new DateTime(2026, 10, 30), startDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)));

        var due = new List<string>();
        foreach (var date in new[] { "2026-10-30", "2026-10-31", "2026-11-01", "2026-11-02", "2026-11-03" })
        {
            if ((await ScheduleAsync(profileId, date)).Items.Count == 1)
            {
                due.Add(date);
            }
        }

        Assert.Equal(["2026-10-30", "2026-11-01", "2026-11-03"], due);
    }

    [Fact]
    public async Task Schedule_NothingBeforeTheFrequencyStartDate()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(kind: "daily", frequencyStart: new DateTime(2026, 10, 7)));

        Assert.Empty((await ScheduleAsync(profileId, "2026-10-06")).Items);
        Assert.Single((await ScheduleAsync(profileId, "2026-10-07")).Items);
    }

    [Fact]
    public async Task Schedule_TimesOfDay_ReplaceTheTimeLabel()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(kind: "daily", times: ["20:00", "08:00"]));

        var item = Assert.Single((await ScheduleAsync(profileId, Monday)).Items);

        Assert.Equal("08:00, 20:00", item.Time);
    }

    [Fact]
    public async Task Schedule_TimesPerWeek_IsAWeeklyTarget_WithTheWeeksLoggedCount()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(kind: "times_per_week", timesPerWeek: 2));
        await LogAsync(profileId, Monday, new LogDoseEntryRequest(created.Id, 250m, "mcg"));

        var tuesday = Assert.Single((await ScheduleAsync(profileId, "2026-10-06")).Items);
        Assert.Equal("weekly_target", tuesday.Due);
        Assert.Equal("2 times per week (weekly target)", tuesday.Cadence);
        Assert.Equal(1, tuesday.WeekLogged);

        var nextWeek = Assert.Single((await ScheduleAsync(profileId, "2026-10-13")).Items);
        Assert.Equal(0, nextWeek.WeekLogged);
    }

    [Fact]
    public async Task Schedule_AsNeeded_IsListedAsAsNeeded_AndNeverCountedTowardsADay()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request("Scheduled", kind: "weekdays", weekdays: [1]));
        await CreateAsync(profileId, Request("PRN", kind: "as_needed"));
        await UpsertCommanderSubscriptionAsync();

        var tuesday = await ScheduleAsync(profileId, "2026-10-06");
        var prn = Assert.Single(tuesday.Items);
        Assert.Equal("PRN", prn.Name);
        Assert.Equal("as_needed", prn.Due);

        var week = (await _client.GetFromJsonAsync<List<WeekDayResponse>>(Url(profileId, "schedule/week?start=2026-10-04"), JsonOptions))!;
        Assert.Equal([0, 1, 0, 0, 0, 0, 0], week.Select(d => d.ItemCount));
    }

    [Fact]
    public async Task Schedule_WeekCounts_FollowTheChosenWeekdays()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(kind: "weekdays", weekdays: [1, 4]));
        await UpsertCommanderSubscriptionAsync();

        var week = (await _client.GetFromJsonAsync<List<WeekDayResponse>>(Url(profileId, "schedule/week?start=2026-10-04"), JsonOptions))!;

        // The week strip starts on Sunday: Sun Mon Tue Wed Thu Fri Sat.
        Assert.Equal([0, 1, 0, 0, 1, 0, 0], week.Select(d => d.ItemCount));
    }

    [Fact]
    public async Task Schedule_WithoutADate_UsesTheCallersTimeZoneToPickToday()
    {
        var profileId = await CreateProfileAsync();
        await CreateAsync(profileId, Request(kind: "daily", startDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var ahead = (await _client.GetFromJsonAsync<DayScheduleResponse>(Url(profileId, "schedule?tz=Pacific/Kiritimati"), JsonOptions))!;
        var behind = (await _client.GetFromJsonAsync<DayScheduleResponse>(Url(profileId, "schedule?tz=Etc/GMT%2B12"), JsonOptions))!;
        var unknown = (await _client.GetFromJsonAsync<DayScheduleResponse>(Url(profileId, "schedule?tz=Not/AZone"), JsonOptions))!;

        // UTC+14 and UTC-12 are 26 hours apart, so their calendar dates always differ by one or two days;
        // an unknown zone falls back to the UTC date. (ResolveToday's exact dates are pinned in the domain tests.)
        var gap = (DateTime.Parse(ahead.DateIso) - DateTime.Parse(behind.DateIso)).Days;
        Assert.InRange(gap, 1, 2);
        Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM-dd"), unknown.DateIso);
    }

    [Fact]
    public async Task Portal_Aggregate_UsesTheFrequencyAndShowsLoggedAmounts()
    {
        var profileId = await CreateProfileAsync();
        var created = await CreateAsync(profileId, Request(
            amount: 250m, unit: "mcg", kind: "weekdays", weekdays: [(int)DateTime.UtcNow.DayOfWeek]));
        await LogAsync(profileId, DateTime.UtcNow.ToString("yyyy-MM-dd"), new LogDoseEntryRequest(created.Id, 250m, "mcg"));
        await UpsertCommanderSubscriptionAsync();

        var portal = (await _client.GetFromJsonAsync<ProtocolPortalResponse>(Url(profileId, "portal?tz=UTC"), JsonOptions))!;

        var item = Assert.Single(portal.Today.Items);
        Assert.Equal("completed", item.Status);
        Assert.Equal(250m, Assert.Single(item.Logged!).Amount);
        Assert.Equal("1 of 7 days logged", portal.Stats.Single(s => s.Label.StartsWith("Adherence")).Caption);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private async Task<CompoundResponse> CreateAsync(Guid profileId, CreateCompoundRequest request)
    {
        var response = await _client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/compounds", request, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
