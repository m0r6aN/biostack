namespace BioStack.Api.Tests.AccountData;

using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Api.AccountData;
using BioStack.Api.Tests.Integration;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class AccountExportDeletionIntegrationTests : IAsyncLifetime
{
    private const string Phrase = "DELETE MY ACCOUNT";
    private WebApplicationFactory<Program> _factory = null!;
    private string _dbPath = string.Empty;
    private readonly FakeStripe _stripe = new();
    private readonly RecordingSender _mail = new();

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-account-{Guid.NewGuid():N}.db");
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
                services.AddSingleton<IStripeCustomerCloser>(_stripe);
                services.AddSingleton<IAccountEmailSender>(_mail);
            });
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch (IOException) { }
    }

    // ── Export ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Export_RequiresSignIn()
    {
        using var anonymous = NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/account/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/account/data-summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token = "x" })).StatusCode);
    }

    [Fact]
    public async Task Export_Json_ContainsEveryExportedTable_OnlyForThisUser_AndNoSecrets()
    {
        using var a = NewClient();
        using var b = NewClient();
        var userA = await SignInAsync(a, "export-a@example.com");
        var userB = await SignInAsync(b, "export-b@example.com");
        await SeedAsync(userA, "export-a@example.com", "AAA");
        await SeedAsync(userB, "export-b@example.com", "BBB");

        var response = await a.GetAsync("/api/v1/account/export?format=json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.EndsWith(".json", response.Content.Headers.ContentDisposition.FileName);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());

        var raw = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        var tables = doc.RootElement.GetProperty("tables");

        foreach (var table in AccountDataCatalog.ExportedTables)
        {
            Assert.True(tables.TryGetProperty(table.Name, out var rows), $"export is missing {table.Name}");
            // SpineEntries etc. are seeded too, so every exported table must have data for the seeded user.
            Assert.True(rows.GetArrayLength() > 0, $"{table.Name} exported no rows for a user who has data in it");
        }

        Assert.Equal("export-a@example.com", tables.GetProperty("AppUsers")[0].GetProperty("email").GetString());
        Assert.Equal(userA, doc.RootElement.GetProperty("userId").GetGuid());

        // The other user's data never appears.
        Assert.DoesNotContain("BBB", raw);
        Assert.DoesNotContain("export-b@example.com", raw);
        Assert.DoesNotContain(userB.ToString(), raw);
        // The user's own entries are there as typed.
        Assert.Contains("Item-AAA", raw);
        Assert.Contains("Profile-AAA", raw);

        // Secrets and key material are never exported.
        foreach (var banned in new[] { "tokenHash", "publicKey", "credentialId", "userHandle", "optionsJson", "requestIdHash", "unsubscribeToken", "submitterKey" })
            Assert.DoesNotContain($"\"{banned}\"", raw, StringComparison.OrdinalIgnoreCase);

        // The retained-but-not-deleted tables and the not-included tables are named.
        Assert.Contains(doc.RootElement.GetProperty("retainedButNotDeleted").EnumerateArray(), n => n.GetProperty("table").GetString() == "SpineEntries");
        Assert.Contains(doc.RootElement.GetProperty("notIncluded").EnumerateArray(), n => n.GetProperty("table").GetString() == "ClaimCheckQueries");
    }

    [Fact]
    public async Task Export_Csv_IsAZipWithOneFilePerExportedTable()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "csv@example.com");
        await SeedAsync(userA, "csv@example.com", "CSV");
        await SetNoteAsync(userA, "=HYPERLINK(\"http://example.com\")");

        var response = await a.GetAsync("/api/v1/account/export?format=csv");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType!.MediaType);

        using var zip = new ZipArchive(await response.Content.ReadAsStreamAsync(), ZipArchiveMode.Read);
        var names = zip.Entries.Select(e => e.FullName).ToHashSet();
        Assert.Contains("README.txt", names);
        foreach (var table in AccountDataCatalog.ExportedTables)
            Assert.Contains($"{table.Name}.csv", names);

        using var profiles = new StreamReader(zip.GetEntry("PersonProfiles.csv")!.Open());
        var csv = await profiles.ReadToEndAsync();
        Assert.Contains("Profile-CSV", csv);
        Assert.Contains("'=HYPERLINK", csv);       // spreadsheet formula guard
        Assert.DoesNotContain(",=HYPERLINK", csv);
    }

    [Fact]
    public async Task Export_RejectsUnknownFormat()
    {
        using var a = NewClient();
        await SignInAsync(a, "fmt@example.com");
        Assert.Equal(HttpStatusCode.BadRequest, (await a.GetAsync("/api/v1/account/export?format=xml")).StatusCode);
    }

    [Fact]
    public async Task Export_IsRateLimitedPerUser()
    {
        using var a = NewClient();
        using var b = NewClient();
        await SignInAsync(a, "rate-a@example.com");
        await SignInAsync(b, "rate-b@example.com");

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/api/v1/account/export")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await a.GetAsync("/api/v1/account/export")).StatusCode);
        // Another person's budget is separate.
        Assert.Equal(HttpStatusCode.OK, (await b.GetAsync("/api/v1/account/export")).StatusCode);
    }

    [Fact]
    public async Task ExportAndDeletion_WorkWhileConsentIsOutOfDate()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "stale@example.com");
        await SetConsentAsync(userA, "bio-observational-v0-stale");

        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/api/v1/account/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase })).StatusCode);
        var token = TokenFrom(_mail.LastTo("stale@example.com"));
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token })).StatusCode);
    }

    [Fact]
    public async Task DataSummary_ListsCountsAndTheBackupWindow()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "summary@example.com");
        await SeedAsync(userA, "summary@example.com", "SUM");

        var body = await a.GetFromJsonAsync<JsonElement>("/api/v1/account/data-summary");
        Assert.Equal(35, body.GetProperty("backupRetentionDays").GetInt32());
        Assert.Equal(Phrase, body.GetProperty("confirmationPhrase").GetString());
        Assert.True(body.GetProperty("deletionAvailable").GetBoolean());
        var spine = body.GetProperty("tables").EnumerateArray().Single(t => t.GetProperty("name").GetString() == "SpineEntries");
        Assert.Equal("kept", spine.GetProperty("onDeletion").GetString());
        var items = body.GetProperty("tables").EnumerateArray().Single(t => t.GetProperty("name").GetString() == "CompoundRecords");
        Assert.True(items.GetProperty("count").GetInt32() > 0);
        Assert.Equal("removed", items.GetProperty("onDeletion").GetString());
    }

    // ── Deletion: confirmation flow ──────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("delete my account")]
    [InlineData("DELETE")]
    public async Task DeletionRequest_NeedsTheTypedPhrase(string? phrase)
    {
        using var a = NewClient();
        await SignInAsync(a, "phrase@example.com");
        var before = _mail.Sent.Count;

        var response = await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = phrase });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("confirmation_required", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(before, _mail.Sent.Count);
    }

    [Fact]
    public async Task DeletionRequest_EmailsAFreshLinkToTheAccountAddress_AndDeletesNothing()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "link@example.com");
        await SeedAsync(userA, "link@example.com", "LNK");
        var before = await CountsAsync(userA, "link@example.com");

        var response = await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var mail = _mail.LastTo("link@example.com");
        Assert.Contains("Confirm deleting your BioStack account", mail.Subject);
        Assert.Contains("/account/data/delete/confirm?token=", mail.PlainText);
        Assert.Contains("expires", mail.PlainText);
        AssertSameExceptLinks(before, await CountsAsync(userA, "link@example.com"));
    }

    [Fact]
    public async Task DeletionConfirm_RejectsMissingWrongAndOtherUsersTokens()
    {
        using var a = NewClient();
        using var b = NewClient();
        var userA = await SignInAsync(a, "tok-a@example.com");
        var userB = await SignInAsync(b, "tok-b@example.com");
        await SeedAsync(userA, "tok-a@example.com", "TKA");
        await SeedAsync(userB, "tok-b@example.com", "TKB");
        var beforeA = await CountsAsync(userA, "tok-a@example.com");
        var beforeB = await CountsAsync(userB, "tok-b@example.com");

        await b.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        var tokenB = TokenFrom(_mail.LastTo("tok-b@example.com"));

        foreach (var token in new[] { null, "", "not-a-real-token", tokenB })
        {
            var response = await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("invalid_link", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }

        Assert.Equal(beforeA, await CountsAsync(userA, "tok-a@example.com"));
        AssertSameExceptLinks(beforeB, await CountsAsync(userB, "tok-b@example.com"));   // B asked for a link, so one challenge row is expected
    }

    [Fact]
    public async Task DeletionConfirm_RejectsAnExpiredLink()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "expired@example.com");
        await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        var token = TokenFrom(_mail.LastTo("expired@example.com"));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            await db.AuthChallenges.Where(c => c.ChallengeType == AccountDeletionService.ChallengeType)
                .ExecuteUpdateAsync(u => u.SetProperty(c => c.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token })).StatusCode);
        Assert.True(await UserExistsAsync(userA));
    }

    [Fact]
    public async Task DeletionLink_CannotSignInAnyone()
    {
        using var a = NewClient();
        await SignInAsync(a, "nosignin@example.com");
        await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        var token = TokenFrom(_mail.LastTo("nosignin@example.com"));

        using var stranger = NewClient();
        var verify = await stranger.PostAsJsonAsync("/api/v1/auth/verify", new { token });
        Assert.Equal(HttpStatusCode.BadRequest, verify.StatusCode);
    }

    [Fact]
    public async Task DeletionRequest_IsRateLimited()
    {
        using var a = NewClient();
        await SignInAsync(a, "dr-rate@example.com");
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Accepted, (await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase })).StatusCode);
    }

    [Fact]
    public async Task OperatorAccounts_AreProtectedFromSelfServeDeletion()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "operator@example.com");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            await db.AppUsers.Where(u => u.Id == userA).ExecuteUpdateAsync(u => u.SetProperty(x => x.Role, UserRole.Admin));
        }

        var response = await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(await UserExistsAsync(userA));
    }

    // ── Deletion: the full erase ─────────────────────────────────────────────────────

    [Fact]
    public async Task Deletion_EmptiesEveryPerUserTable_ForTheDeletedUserOnly()
    {
        using var a = NewClient();
        using var b = NewClient();
        var userA = await SignInAsync(a, "gone@example.com");
        var userB = await SignInAsync(b, "stays@example.com");
        await SeedAsync(userA, "gone@example.com", "AAA");
        await SeedAsync(userB, "stays@example.com", "BBB");

        AccountScope scopeA, scopeB;
        using (var scope = _factory.Services.CreateScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<AccountScopeResolver>();
            scopeA = (await resolver.ResolveAsync(userA, default))!;
            scopeB = (await resolver.ResolveAsync(userB, default))!;
        }

        // Every user-scoped table really has data for A before we start (so the test can't pass vacuously
        // when a new table is added but never seeded).
        var beforeA = await CountsAsync(scopeA);
        var beforeB = await CountsAsync(scopeB);
        foreach (var table in AccountDataCatalog.ExportedTables)
        {
            Assert.True(beforeA[table.Name] > 0, $"seed more data: {table.Name} is empty for the user being deleted");
            Assert.True(beforeB[table.Name] > 0, $"seed more data: {table.Name} is empty for the user who must be left alone");
        }

        // An anonymous submission (signed-out visitor) must survive, and so must global content.
        var globalBefore = await GlobalSnapshotAsync();

        await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        var token = TokenFrom(_mail.LastTo("gone@example.com"));
        var response = await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("deleted", body.GetProperty("status").GetString());
        Assert.Equal(35, body.GetProperty("backupRetentionDays").GetInt32());
        Assert.True(body.GetProperty("rowsAffected").GetInt32() > 20);
        Assert.True(body.GetProperty("receiptEmailQueued").GetBoolean());
        Assert.Contains(response.Headers, h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)); // cookie cleared

        // A: every deleted table is empty. Retained pseudonymous rows remain; the account row is gone.
        var afterA = await CountsAsync(scopeA);
        foreach (var table in AccountDataCatalog.DeletedTables)
            Assert.True(afterA[table.Name] == 0, $"{table.Name} still has {afterA[table.Name]} rows for the deleted user");
        Assert.Equal(beforeA["SpineEntries"], afterA["SpineEntries"]);

        // Provider requests are redacted in place, never left holding the email.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            Assert.DoesNotContain(await db.ProviderAccessRequests.AsNoTracking().ToListAsync(), r => r.Email == "gone@example.com");
            var redacted = await db.ProviderAccessRequests.AsNoTracking().Where(r => r.Name == "[redacted]").ToListAsync();
            Assert.Single(redacted);
            Assert.EndsWith("@redacted.invalid", redacted[0].Email);
            Assert.Equal(Guid.Empty, (await db.ProviderAccessAuditEntries.AsNoTracking().Where(e => e.RequestId == redacted[0].Id).SingleAsync()).ActorId);

            // Anonymous claim submission and unrelated tables are untouched.
            Assert.Equal(1, await db.ClaimSubmissions.CountAsync(s => s.ClaimText == "anonymous visitor claim AAA"));
            Assert.Equal(1, await db.ClaimSubmissions.CountAsync(s => s.ClaimText == "claim by BBB"));
            Assert.False(await db.ClaimSubmissions.AnyAsync(s => s.ClaimText == "claim by AAA"));
            Assert.False(await db.AppUsers.AnyAsync(u => u.Id == userA));
            Assert.False(await db.PersonProfiles.AnyAsync(p => p.DisplayName == "Profile-AAA"));
            Assert.False(await db.CompoundRecords.AnyAsync(c => c.Name == "Item-AAA"));
            Assert.False(await db.AuthIdentities.AnyAsync(i => i.ValueNormalized == "gone@example.com"));
            Assert.False(await db.LeadCaptures.AnyAsync(l => l.Email == "gone@example.com"));
        }

        // B: every table is exactly as before.
        Assert.Equal(beforeB, await CountsAsync(scopeB));
        Assert.Equal(globalBefore, await GlobalSnapshotAsync());
        Assert.Equal(HttpStatusCode.OK, (await b.GetAsync("/api/v1/account/export")).StatusCode);

        // The session is dead: the old cookie no longer authenticates.
        Assert.Equal(HttpStatusCode.Unauthorized, (await a.GetAsync("/api/v1/account/export")).StatusCode);
        var session = await a.GetFromJsonAsync<JsonElement>("/api/v1/auth/session");
        Assert.False(session.GetProperty("authenticated").GetBoolean());

        // Stripe was told to end billing for exactly this user's customer.
        Assert.Equal(["cus_AAA"], _stripe.Deleted);
    }

    [Fact]
    public async Task Deletion_LeavesOnlyAMinimalNonIdentifyingRecord_AndSendsAReceipt()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "receipt@example.com");
        await SeedAsync(userA, "receipt@example.com", "RCP");

        await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        var token = TokenFrom(_mail.LastTo("receipt@example.com"));
        var response = await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var record = await db.AccountDeletionRecords.AsNoTracking().SingleAsync();
        Assert.Equal(body.GetProperty("receiptId").GetGuid(), record.Id);
        Assert.NotEqual(userA, record.Id);
        Assert.Equal(DateTime.UtcNow.Date, record.DeletedOnUtc);                       // day only, no time of day
        Assert.Equal(record.DeletedOnUtc.AddDays(35), record.BackupsAgeOutByUtc);
        Assert.True(record.RowsAffected > 0);
        Assert.Equal(AccountDataCatalog.ProcedureVersion, record.ProcedureVersion);

        // Structurally nothing in the record can point at a person.
        var columns = db.Model.FindEntityType(typeof(AccountDeletionRecord))!.GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(["BackupsAgeOutByUtc", "DeletedOnUtc", "Id", "ProcedureVersion", "RowsAffected"], columns);

        // Receipt goes to the account's address, states the 35-day backup window, and carries no health values.
        var mail = _mail.Sent.Last(m => m.Subject == "Your BioStack account was deleted");
        Assert.Equal("receipt@example.com", mail.To);
        Assert.Contains("35 days", mail.PlainText);
        Assert.Contains(record.BackupsAgeOutByUtc.ToString("yyyy-MM-dd"), mail.PlainText);
        Assert.Contains(record.Id.ToString(), mail.PlainText);
        Assert.DoesNotContain("Item-RCP", mail.PlainText);
        Assert.DoesNotContain("Profile-RCP", mail.PlainText);
    }

    [Fact]
    public async Task Deletion_StillSucceeds_WhenTheReceiptEmailFails()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "mailfail@example.com");
        await SeedAsync(userA, "mailfail@example.com", "MF");
        await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        var token = TokenFrom(_mail.LastTo("mailfail@example.com"));
        _mail.FailReceipts = true;

        var response = await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("receiptEmailQueued").GetBoolean());
        Assert.False(await UserExistsAsync(userA));
    }

    [Fact]
    public async Task Deletion_IsBlocked_AndNothingErased_WhenBillingCannotBeCancelled_ThenRetrySucceeds()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "billing@example.com");
        await SeedAsync(userA, "billing@example.com", "BIL");
        var before = await CountsAsync(userA, "billing@example.com");
        await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        var token = TokenFrom(_mail.LastTo("billing@example.com"));

        _stripe.Fail = true;
        var blocked = await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, blocked.StatusCode);
        Assert.Equal("billing_unavailable", (await blocked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        AssertSameExceptLinks(before, await CountsAsync(userA, "billing@example.com"));
        Assert.True(await UserExistsAsync(userA));

        _stripe.Fail = false;
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token })).StatusCode);
        Assert.False(await UserExistsAsync(userA));
    }

    [Fact]
    public async Task Deletion_WithNoBilling_DoesNotCallStripe()
    {
        using var a = NewClient();
        var userA = await SignInAsync(a, "free@example.com");
        await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        var token = TokenFrom(_mail.LastTo("free@example.com"));
        _stripe.Fail = true;   // would block if it were called

        Assert.Equal(HttpStatusCode.OK, (await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token })).StatusCode);
        Assert.Empty(_stripe.Deleted);
        Assert.False(await UserExistsAsync(userA));
    }

    [Fact]
    public async Task ADeletedPerson_CanSignUpAgain_AsABrandNewAccount()
    {
        using var a = NewClient();
        var first = await SignInAsync(a, "again@example.com");
        await SeedAsync(first, "again@example.com", "AGN");
        await a.PostAsJsonAsync("/api/v1/account/deletion/request", new { confirmation = Phrase });
        await a.PostAsJsonAsync("/api/v1/account/deletion/confirm", new { token = TokenFrom(_mail.LastTo("again@example.com")) });

        using var again = NewClient();
        var second = await SignInAsync(again, "again@example.com");

        Assert.NotEqual(first, second);
        var export = await again.GetFromJsonAsync<JsonElement>("/api/v1/account/export");
        Assert.Equal(0, export.GetProperty("counts").GetProperty("PersonProfiles").GetInt32());
        Assert.Equal(0, export.GetProperty("counts").GetProperty("CompoundRecords").GetInt32());
        Assert.Equal(0, export.GetProperty("counts").GetProperty("SpineEntries").GetInt32());
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────

    /// <summary>Requesting a deletion link adds one sign-in-artifact row. Nothing else may change.</summary>
    private static void AssertSameExceptLinks(Dictionary<string, int> before, Dictionary<string, int> after)
    {
        foreach (var (table, count) in before.Where(kv => kv.Key != "AuthChallenges"))
            Assert.True(count == after[table], $"{table} changed: {count} -> {after[table]}");
    }

    private HttpClient NewClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<Guid> SignInAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"));
        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First(m => m.GetProperty("contact").GetString() == email).GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var id = await db.AppUsers.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        await db.AppUsers.Where(u => u.Id == id).ExecuteUpdateAsync(u => u
            .SetProperty(x => x.ConsentAcceptedAtUtc, DateTime.UtcNow)
            .SetProperty(x => x.ConsentVersion, BioStack.Application.Services.ConsentGate.CurrentConsentVersion));
        return id;
    }

    private static string TokenFrom(AccountEmail mail)
    {
        var line = mail.PlainText.Split('\n').Select(l => l.Trim()).First(l => l.Contains("/account/data/delete/confirm"));
        return Uri.UnescapeDataString(System.Web.HttpUtility.ParseQueryString(new Uri(line).Query)["token"]!);
    }

    private async Task<bool> UserExistsAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().AppUsers.AnyAsync(u => u.Id == userId);
    }

    private async Task SetConsentAsync(Guid userId, string version)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        await db.AppUsers.Where(u => u.Id == userId).ExecuteUpdateAsync(u => u.SetProperty(x => x.ConsentVersion, version));
    }

    private async Task SetNoteAsync(Guid userId, string note)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        await db.PersonProfiles.Where(p => p.OwnerId == userId).ExecuteUpdateAsync(p => p.SetProperty(x => x.Notes, note));
    }

    private async Task<Dictionary<string, int>> CountsAsync(Guid userId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var resolved = await scope.ServiceProvider.GetRequiredService<AccountScopeResolver>().ResolveAsync(userId, default);
        return await CountsAsync(resolved ?? new AccountScope(userId, [email], $"user:{userId}", "none"));
    }

    private async Task<Dictionary<string, int>> CountsAsync(AccountScope scope)
    {
        using var serviceScope = _factory.Services.CreateScope();
        var db = serviceScope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var result = new Dictionary<string, int>();
        foreach (var table in AccountDataCatalog.ExportedTables)
            result[table.Name] = await table.CountAsync(db, scope, default);
        return result;
    }

    /// <summary>Rows in tables that are not about any person, so deletion must not change them.</summary>
    private async Task<string> GlobalSnapshotAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return string.Join('|',
            await db.KnowledgeEntries.CountAsync(),
            await db.InteractionFlags.CountAsync(),
            await db.ClaimCheckQueries.CountAsync(),
            await db.StripeWebhookEvents.CountAsync(),
            await db.SpineChainCheckpoints.CountAsync(),
            await db.ClaimSubmissions.CountAsync(s => s.ClaimText.StartsWith("anonymous visitor claim")));
    }

    /// <summary>Gives the user at least one row in every per-user table. Tag makes the values unique per user.</summary>
    private async Task SeedAsync(Guid userId, string email, string tag)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var resolved = (await scope.ServiceProvider.GetRequiredService<AccountScopeResolver>().ResolveAsync(userId, default))!;
        var now = DateTime.UtcNow;

        var profile = new PersonProfile { Id = Guid.NewGuid(), OwnerId = userId, DisplayName = $"Profile-{tag}", Notes = $"notes-{tag}" };
        db.PersonProfiles.Add(profile);
        db.ProfileGoals.Add(new ProfileGoal { Id = Guid.NewGuid(), ProfileId = profile.Id, GoalDefinitionId = $"goal-{tag}" });

        var item = new CompoundRecord { Id = Guid.NewGuid(), PersonId = profile.Id, Name = $"Item-{tag}", Notes = $"custom item typed by {tag}" };
        db.CompoundRecords.Add(item);
        db.DoseLogEntries.Add(new DoseLogEntry { Id = Guid.NewGuid(), PersonId = profile.Id, CompoundRecordId = item.Id, LogDate = now.Date, TakenAtUtc = now, Amount = 1m, Unit = "mg", Note = $"log-{tag}" });

        var origin = new Protocol { Id = Guid.NewGuid(), PersonId = profile.Id, Name = $"Protocol-{tag}", Version = 1 };
        var run = new ProtocolRun { Id = Guid.NewGuid(), ProtocolId = origin.Id, PersonId = profile.Id, StartedAtUtc = now };
        var revision = new Protocol { Id = Guid.NewGuid(), PersonId = profile.Id, Name = $"Protocol-{tag}", Version = 2, ParentProtocolId = origin.Id, OriginProtocolId = origin.Id };
        db.Protocols.AddRange(origin, revision);
        db.ProtocolRuns.Add(run);
        db.ProtocolItems.Add(new ProtocolItem { Id = Guid.NewGuid(), ProtocolId = revision.Id, CompoundRecordId = item.Id, CompoundNameSnapshot = $"Item-{tag}" });
        db.ProtocolComputationRecords.Add(new ProtocolComputationRecord { Id = Guid.NewGuid(), ProtocolId = revision.Id, ProtocolRunId = run.Id, Type = "t", InputSnapshot = "{}", OutputResult = "{}", TimestampUtc = now });
        db.ProtocolReviewCompletedEvents.Add(new ProtocolReviewCompletedEvent { Id = Guid.NewGuid(), ProtocolId = revision.Id, ProtocolRunId = run.Id, CompletedAtUtc = now });
        db.ProtocolPhases.Add(new ProtocolPhase { Id = Guid.NewGuid(), PersonId = profile.Id, Name = $"Phase-{tag}" });
        db.TimelineEvents.Add(new TimelineEvent { Id = Guid.NewGuid(), PersonId = profile.Id, Title = $"Event-{tag}", OccurredAtUtc = now, RelatedEntityType = "none" });
        db.CheckIns.Add(new CheckIn { Id = Guid.NewGuid(), PersonId = profile.Id, ProtocolRunId = run.Id, Date = now.Date, Notes = $"checkin-{tag}" });

        db.EvidenceWatches.Add(new EvidenceWatch { Id = Guid.NewGuid(), UserId = userId, TargetType = "compound", TargetId = $"slug-{tag}" });
        db.EvidenceAlertPreferences.Add(new EvidenceAlertPreference { UserId = userId, Frequency = "weekly", EmailEnabled = true, UnsubscribeToken = $"unsub-{tag}" });
        db.EvidenceAlertLog.Add(new EvidenceAlertLogEntry { Id = Guid.NewGuid(), UserId = userId, AlertType = "verdict", CompoundSlug = $"slug-{tag}", ClaimId = "c1", Version = 1, OldVerdict = "a", NewVerdict = "b", Reason = "r", Delivery = "inapp" });

        db.Subscriptions.Add(new Subscription { Id = Guid.NewGuid(), AppUserId = userId, ProductCode = "pro", StripeCustomerId = $"cus_{tag}", StripeSubscriptionId = $"sub_{tag}", Status = SubscriptionStatus.Active });
        if (tag == "AAA") await db.AppUsers.Where(u => u.Id == userId).ExecuteUpdateAsync(u => u.SetProperty(x => x.StripeCustomerId, "cus_AAA"));

        db.ClaimSubmissions.Add(new ClaimSubmission { Id = Guid.NewGuid(), CompoundSlug = "creatine", ClaimText = $"claim by {tag}", SubmitterKey = resolved.SubmitterKey });
        db.ClaimSubmissions.Add(new ClaimSubmission { Id = Guid.NewGuid(), CompoundSlug = "creatine", ClaimText = $"anonymous visitor claim {tag}", SubmitterKey = $"anon-{tag}" });
        db.LeadCaptures.Add(new LeadCapture { Id = Guid.NewGuid(), Email = email, Source = $"src-{tag}" });
        var providerRequest = new BioStack.Domain.Entities.ProviderAccessRequest { Id = Guid.NewGuid(), Email = email, Name = $"Name-{tag}", Organization = $"Org-{tag}", Role = "MD", Status = "new", ConsentVersion = "v1", ConsentRecordedAtUtc = now };
        db.ProviderAccessRequests.Add(providerRequest);
        db.ProviderAccessAuditEntries.Add(new ProviderAccessAuditEntry { Id = Guid.NewGuid(), RequestId = providerRequest.Id, ActorId = userId, FromStatus = "new", ToStatus = "reviewed", OccurredAtUtc = now });

        var identity = await db.AuthIdentities.SingleAsync(i => i.UserId == userId);
        db.PasskeyCredentials.Add(new PasskeyCredential { Id = Guid.NewGuid(), IdentityId = identity.Id, CredentialId = Guid.NewGuid().ToByteArray(), PublicKey = [9, 9], UserHandle = [7], CredentialType = "public-key", DisplayName = $"Key-{tag}", Transports = "internal" });
        db.PasskeyOperationChallenges.Add(new PasskeyOperationChallenge { Id = Guid.NewGuid(), UserId = userId, Operation = "register", RequestIdHash = $"rid-{tag}", OptionsJson = "{}", RedirectPath = "/", CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(5) });

        db.ConsentEvents.Add(new ConsentEvent { Id = Guid.NewGuid(), UserId = userId, EventType = ConsentEvent.Accepted, Version = "bio-observational-v1", OccurredAtUtc = now.AddDays(-30) });
        db.ConsentEvents.Add(new ConsentEvent { Id = Guid.NewGuid(), UserId = userId, EventType = ConsentEvent.Accepted, Version = $"bio-observational-v2", OccurredAtUtc = now });

        db.SpineEntries.Add(new SpineEntry
        {
            ReceiptUri = $"keon://receipt/{Guid.NewGuid()}", SubjectUri = $"subject:{tag}", TenantId = "biostack-public", ActorId = resolved.ActorId,
            Decision = "non-effecting", PolicyHashValue = "h", PolicyHashVersion = "1", InputHash = "i", EffectStatus = "none",
            SequenceNumber = 1_000_000 + Math.Abs(tag.GetHashCode() % 1000), PreviousEntryHash = "p", EntryHash = $"e-{tag}-{Guid.NewGuid():N}",
        });

        await db.SaveChangesAsync();
    }

    private sealed class FakeStripe : IStripeCustomerCloser
    {
        public bool IsConfigured => true;
        public bool Fail { get; set; }
        public List<string> Deleted { get; } = [];

        public Task DeleteCustomerAsync(string customerId, CancellationToken ct)
        {
            if (Fail) throw new InvalidOperationException("stripe down");
            lock (Deleted) Deleted.Add(customerId);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSender : IAccountEmailSender
    {
        private readonly List<AccountEmail> _sent = [];
        public bool FailReceipts { get; set; }
        public IReadOnlyList<AccountEmail> Sent { get { lock (_sent) return _sent.ToList(); } }

        public Task SendAsync(AccountEmail email, CancellationToken ct)
        {
            if (FailReceipts && email.Subject.Contains("was deleted")) throw new InvalidOperationException("mail down");
            lock (_sent) _sent.Add(email);
            return Task.CompletedTask;
        }

        public AccountEmail LastTo(string address) => Sent.Last(m => m.To == address && m.Subject.StartsWith("Confirm"));
    }
}
