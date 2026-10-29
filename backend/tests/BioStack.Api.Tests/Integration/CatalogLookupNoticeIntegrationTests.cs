namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Everything I'm Taking, phase 3: the internal "is this already in BioStack?" lookup and the in-app notices.
/// Nothing leaves the server, nothing is linked without the person's confirmation, and everything is owner-only.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CatalogLookupNoticeIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _a = null!;
    private HttpClient _b = null!;
    private string _dbPath = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-lookup-{Guid.NewGuid():N}.db");
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
        _a = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _b = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        db.KnowledgeEntries.AddRange(
            new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = "Creatine", Classification = CompoundCategory.Supplement, Aliases = ["creatine monohydrate"] },
            new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = "Semaglutide", Classification = CompoundCategory.Peptide, Aliases = ["Ozempic", "Wegovy"] },
            new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = "Magnesium", Classification = CompoundCategory.Mineral },
            new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = "BPC-157", Classification = CompoundCategory.Peptide });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _a.Dispose();
        _b.Dispose();
        await _factory.DisposeAsync();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch (IOException) { }
    }

    // ── The lookup finds a match and leaves the decision to the person ──────────────────────────

    [Fact]
    public async Task ABrandName_IsOfferedAsSameAs_AndNothingIsLinkedBehindTheirBack()
    {
        var (_, profile) = await StartAsync(_a, "brand@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Ozempic");

        var accepted = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = "Ozempic", compoundId = item.Id }, JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var created = (await accepted.Content.ReadFromJsonAsync<CatalogLookupResponse>(JsonOptions))!;
        Assert.Equal("Ozempic", created.QueryName);
        Assert.Equal(item.Id, created.CompoundId);

        var done = await WaitAsync(_a, created.Id);
        Assert.Equal("matched", done.Status);
        var offer = Assert.Single(done.Candidates);
        Assert.Equal("semaglutide", offer.Slug);
        Assert.Equal("same_as", offer.Label);
        Assert.Equal("alias", offer.Kind);

        // The item is exactly as the person left it: not linked, still unverified.
        var after = await GetItemAsync(_a, profile.Id, item.Id);
        Assert.Null(after.CatalogSlug);
        Assert.Equal("unverified", after.VerificationState);
    }

    [Fact]
    public async Task AMisspelling_IsOfferedAsSimilar()
    {
        var (_, profile) = await StartAsync(_a, "typo@example.com");
        var item = await AddItemAsync(_a, profile.Id, "cretine");

        var done = await CreateAndWaitAsync(_a, profile.Id, "cretine", item.Id);

        Assert.Equal("matched", done.Status);
        Assert.Equal("creatine", done.Candidates[0].Slug);
        Assert.Equal("similar", done.Candidates[0].Label);
        Assert.Equal("fuzzy", done.Candidates[0].Kind);
    }

    [Fact]
    public async Task ANameThatIsNotInTheLibrary_EndsWithNoMatch_NoOffer_NoNote()
    {
        var (_, profile) = await StartAsync(_a, "nomatch@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Zzyzx Blend");

        var done = await CreateAndWaitAsync(_a, profile.Id, "Zzyzx Blend", item.Id);

        Assert.Equal("no_match", done.Status);
        Assert.Empty(done.Candidates);
        Assert.NotNull(done.CompletedAtUtc);
        Assert.Equal(0, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
    }

    // ── Notices: the offer waits for them even if they leave ─────────────────────────────────────

    [Fact]
    public async Task AWaitingOffer_LeavesANoteThatOpensTheTakingTab()
    {
        var (_, profile) = await StartAsync(_a, "note@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Wegovy");
        var lookup = await CreateAndWaitAsync(_a, profile.Id, "Wegovy", item.Id);

        Assert.Equal(1, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
        var notes = (await _a.GetFromJsonAsync<List<NoticeResponse>>("/api/v1/notices?unread=true", JsonOptions))!;
        var note = Assert.Single(notes);
        Assert.Equal("catalog_match", note.Kind);
        Assert.Equal(lookup.Id, note.RelatedId);
        Assert.Equal("/my-protocol?tab=taking", note.ActionPath);
        Assert.Equal("Wegovy", note.Params.GetProperty("queryName").GetString());
        Assert.Equal("Semaglutide", note.Params.GetProperty("candidateName").GetString());
        Assert.False(note.Read);
    }

    [Fact]
    public async Task ConfirmingTheOffer_LinksTheItem_AndAnswersTheNote()
    {
        var (_, profile) = await StartAsync(_a, "link@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Ozempic");
        var lookup = await CreateAndWaitAsync(_a, profile.Id, "Ozempic", item.Id);

        var response = await _a.PostAsJsonAsync($"/api/v1/catalog-lookups/{lookup.Id}/link", new { slug = "semaglutide" }, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("linked", (await response.Content.ReadFromJsonAsync<CatalogLookupResponse>(JsonOptions))!.Status);
        var linked = await GetItemAsync(_a, profile.Id, item.Id);
        Assert.Equal("semaglutide", linked.CatalogSlug);
        Assert.Equal("catalog", linked.VerificationState);
        Assert.Equal("catalog", linked.CategorySource);
        Assert.Equal(CompoundCategory.Peptide, linked.Category);   // from the catalog
        Assert.Equal(0, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
        Assert.Empty((await _a.GetFromJsonAsync<List<NoticeResponse>>("/api/v1/notices", JsonOptions))!);

        // Confirming again changes nothing.
        var again = await _a.PostAsJsonAsync($"/api/v1/catalog-lookups/{lookup.Id}/link", new { slug = "semaglutide" }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task SayingNotTheSame_KeepsTheItemAsItWas_AndAskingAgainDoesNotStartOver()
    {
        var (_, profile) = await StartAsync(_a, "decline@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Ozempic");
        var lookup = await CreateAndWaitAsync(_a, profile.Id, "Ozempic", item.Id);

        var decline = await _a.PostAsync($"/api/v1/catalog-lookups/{lookup.Id}/decline", null);

        Assert.Equal(HttpStatusCode.OK, decline.StatusCode);
        Assert.Equal("declined", (await decline.Content.ReadFromJsonAsync<CatalogLookupResponse>(JsonOptions))!.Status);
        Assert.Null((await GetItemAsync(_a, profile.Id, item.Id)).CatalogSlug);
        Assert.Equal(0, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);

        // One lookup per person per name: the same name, even spelled differently, returns the stored answer.
        var again = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = "  OZEMPIC ", compoundId = item.Id }, JsonOptions);
        var stored = (await again.Content.ReadFromJsonAsync<CatalogLookupResponse>(JsonOptions))!;
        Assert.Equal(lookup.Id, stored.Id);
        Assert.Equal("declined", stored.Status);
        using var scope = _factory.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().CatalogLookups.CountAsync());
    }

    [Fact]
    public async Task OnlyAnOfferedMatchCanBeLinked_AndOnlyWhileTheLookupIsWaiting()
    {
        var (_, profile) = await StartAsync(_a, "guard@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Ozempic");
        var lookup = await CreateAndWaitAsync(_a, profile.Id, "Ozempic", item.Id);

        // A client cannot link an item the lookup did not offer, however real it is.
        var notOffered = await _a.PostAsJsonAsync($"/api/v1/catalog-lookups/{lookup.Id}/link", new { slug = "creatine" }, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, notOffered.StatusCode);
        Assert.Equal("not_offered", (await notOffered.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("code").GetString());
        Assert.Null((await GetItemAsync(_a, profile.Id, item.Id)).CatalogSlug);

        var blank = await _a.PostAsJsonAsync($"/api/v1/catalog-lookups/{lookup.Id}/link", new { slug = "" }, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

        // A lookup with no match is not waiting for an answer.
        var noMatchItem = await AddItemAsync(_a, profile.Id, "Zzyzx Blend");
        var none = await CreateAndWaitAsync(_a, profile.Id, "Zzyzx Blend", noMatchItem.Id);
        var notWaiting = await _a.PostAsJsonAsync($"/api/v1/catalog-lookups/{none.Id}/link", new { slug = "creatine" }, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, notWaiting.StatusCode);
        Assert.Equal("not_waiting", (await notWaiting.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task ALookupWithoutAnItem_CanBeReadButNotLinked()
    {
        var (_, profile) = await StartAsync(_a, "noitem@example.com");

        var done = await CreateAndWaitAsync(_a, profile.Id, "Ozempic", compoundId: null);

        Assert.Equal("matched", done.Status);
        Assert.Equal(0, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);   // nothing to attach an offer to
        var link = await _a.PostAsJsonAsync($"/api/v1/catalog-lookups/{done.Id}/link", new { slug = "semaglutide" }, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, link.StatusCode);
        Assert.Equal("no_item", (await link.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task IfThePersonLinksTheItemThemselves_TheOfferStopsBeingOffered()
    {
        var (_, profile) = await StartAsync(_a, "self@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Ozempic");
        var accepted = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = "Ozempic", compoundId = item.Id }, JsonOptions);
        var created = (await accepted.Content.ReadFromJsonAsync<CatalogLookupResponse>(JsonOptions))!;
        await WaitAsync(_a, created.Id);
        var open = (await _a.GetFromJsonAsync<List<CatalogLookupResponse>>($"/api/v1/profiles/{profile.Id}/catalog-lookups", JsonOptions))!;
        Assert.Single(open);

        // Linking through the normal edit path leaves nothing to offer.
        var put = await _a.PutAsync($"/api/v1/profiles/{profile.Id}/compounds/{item.Id}", new StringContent(
            "{\"name\":\"Ozempic\",\"category\":\"Peptide\",\"status\":\"Active\",\"notes\":\"\",\"sourceType\":\"Manual\",\"catalogSlug\":\"semaglutide\"}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var linkedItem = await GetItemAsync(_a, profile.Id, item.Id);
        Assert.Equal("semaglutide", linkedItem.CatalogSlug);
        Assert.Empty((await _a.GetFromJsonAsync<List<CatalogLookupResponse>>($"/api/v1/profiles/{profile.Id}/catalog-lookups", JsonOptions))!);
    }

    [Fact]
    public async Task TheSameNameForAnotherItem_RunsTheFreeLocalCheckAgain()
    {
        var (_, profile) = await StartAsync(_a, "again@example.com");
        var first = await AddItemAsync(_a, profile.Id, "Ozempic");
        var lookup = await CreateAndWaitAsync(_a, profile.Id, "Ozempic", first.Id);
        await _a.PostAsync($"/api/v1/catalog-lookups/{lookup.Id}/decline", null);
        var second = await AddItemAsync(_a, profile.Id, "Ozempic pen");

        var again = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = "Ozempic", compoundId = second.Id }, JsonOptions);
        var stored = (await again.Content.ReadFromJsonAsync<CatalogLookupResponse>(JsonOptions))!;

        Assert.Equal(lookup.Id, stored.Id);
        Assert.Equal(second.Id, stored.CompoundId);
        Assert.Equal("matched", (await WaitAsync(_a, lookup.Id)).Status);
    }

    // ── Validation ──────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    [InlineData("https://shop.example.com/creatine")]
    [InlineData("www.example.com")]
    [InlineData("me@example.com")]
    [InlineData("@handle")]
    [InlineData("12345")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("creatine; drop table")]
    public async Task ANameThatIsNotAnItemName_IsRefused(string typed)
    {
        var (_, profile) = await StartAsync(_a, $"bad-{Guid.NewGuid():N}@example.com");

        var response = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = typed }, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().CatalogLookups.CountAsync());
    }

    [Fact]
    public async Task ADoseTypedWithTheName_IsDroppedBeforeAnythingIsStored()
    {
        var (_, profile) = await StartAsync(_a, "dose@example.com");

        var response = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = "Creatine 5 g" }, JsonOptions);

        var created = (await response.Content.ReadFromJsonAsync<CatalogLookupResponse>(JsonOptions))!;
        Assert.Equal("Creatine", created.QueryName);
        using var scope = _factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().CatalogLookups.SingleAsync();
        Assert.Equal("Creatine", stored.QueryName);
        Assert.Equal("creatine", stored.NameKey);
    }

    [Fact]
    public async Task AnItemFromAnotherProfileOrPerson_CannotBeAttached()
    {
        var (_, profileA) = await StartAsync(_a, "attach-a@example.com");
        var (_, profileB) = await StartAsync(_b, "attach-b@example.com");
        var itemB = await AddItemAsync(_b, profileB.Id, "Ozempic");

        var response = await _a.PostAsJsonAsync($"/api/v1/profiles/{profileA.Id}/catalog-lookups", new { name = "Ozempic", compoundId = itemB.Id }, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var notOwned = await _a.PostAsJsonAsync($"/api/v1/profiles/{profileB.Id}/catalog-lookups", new { name = "Ozempic" }, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, notOwned.StatusCode);
    }

    // ── Owner-only ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LookupsAndNotes_AreVisibleOnlyToTheirOwner()
    {
        var (_, profileA) = await StartAsync(_a, "owner-a@example.com");
        await StartAsync(_b, "owner-b@example.com");
        var item = await AddItemAsync(_a, profileA.Id, "Ozempic");
        var lookup = await CreateAndWaitAsync(_a, profileA.Id, "Ozempic", item.Id);
        var note = (await _a.GetFromJsonAsync<List<NoticeResponse>>("/api/v1/notices", JsonOptions))!.Single();

        Assert.Equal(HttpStatusCode.NotFound, (await _b.GetAsync($"/api/v1/catalog-lookups/{lookup.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _b.PostAsJsonAsync($"/api/v1/catalog-lookups/{lookup.Id}/link", new { slug = "semaglutide" }, JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _b.PostAsync($"/api/v1/catalog-lookups/{lookup.Id}/decline", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _b.GetAsync($"/api/v1/profiles/{profileA.Id}/catalog-lookups")).StatusCode);
        Assert.Null((await GetItemAsync(_a, profileA.Id, item.Id)).CatalogSlug);

        Assert.Empty((await _b.GetFromJsonAsync<List<NoticeResponse>>("/api/v1/notices", JsonOptions))!);
        Assert.Equal(0, (await _b.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await _b.PostAsync($"/api/v1/notices/{note.Id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _b.DeleteAsync($"/api/v1/notices/{note.Id}")).StatusCode);
        Assert.Equal(1, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
    }

    [Fact]
    public async Task SignedOut_NothingIsReachable()
    {
        using var anonymous = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/notices")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/notices/unread-count")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v1/catalog-lookups/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync($"/api/v1/profiles/{Guid.NewGuid()}/catalog-lookups", new { name = "Creatine" }, JsonOptions)).StatusCode);
    }

    // ── Consent: writes wait for agreement; reading and acknowledging never do ───────────────────

    [Fact]
    public async Task WithoutCurrentConsent_CreatingLinkingAndDecliningAreBlocked_ButReadingIsNot()
    {
        var (userId, profile) = await StartAsync(_a, "stale@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Ozempic");
        var lookup = await CreateAndWaitAsync(_a, profile.Id, "Ozempic", item.Id);
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().AppUsers.Where(u => u.Id == userId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.ConsentVersion, "bio-observational-v1"));
        }

        async Task AssertBlocked(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("consent_required", (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("code").GetString());
        }

        await AssertBlocked(await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = "Creatine" }, JsonOptions));
        await AssertBlocked(await _a.PostAsJsonAsync($"/api/v1/catalog-lookups/{lookup.Id}/link", new { slug = "semaglutide" }, JsonOptions));
        await AssertBlocked(await _a.PostAsync($"/api/v1/catalog-lookups/{lookup.Id}/decline", null));

        Assert.Equal(HttpStatusCode.OK, (await _a.GetAsync($"/api/v1/catalog-lookups/{lookup.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _a.GetAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _a.GetAsync("/api/v1/notices")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _a.GetAsync("/api/v1/notices/unread-count")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _a.PostAsync("/api/v1/notices/read-all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _a.GetAsync("/api/v1/account/export?format=json")).StatusCode);
        Assert.Null((await GetItemAsync(_a, profile.Id, item.Id)).CatalogSlug);
    }

    // ── Limits ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AtMostThirtyNewNamesADay_PerPerson()
    {
        var (userId, profile) = await StartAsync(_a, "daily@example.com");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            for (var i = 0; i < CatalogLookupService.MaxNewLookupsPerDay; i++)
            {
                db.CatalogLookups.Add(new CatalogLookup { Id = Guid.NewGuid(), UserId = userId, PersonId = profile.Id, NameKey = $"seeded{i}", QueryName = $"Seeded {i}", Status = CatalogLookupStatus.NoMatch });
            }

            await db.SaveChangesAsync();
        }

        var over = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = "One more" }, JsonOptions);
        Assert.Equal(HttpStatusCode.TooManyRequests, over.StatusCode);
        Assert.Equal("catalog_lookup_limit", (await over.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("code").GetString());

        // A name already asked about is still answered from storage.
        var known = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = "Seeded 3" }, JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
    }

    [Fact]
    public async Task TheCreateRateLimit_SlowsOneAddressDown()
    {
        var (_, profile) = await StartAsync(_a, "burst@example.com");
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 22; i++)
        {
            var response = await _a.PostAsJsonAsync($"/api/v1/profiles/{profile.Id}/catalog-lookups", new { name = $"Burst item {(char)('a' + i)}{(char)('a' + i)}" }, JsonOptions);
            codes.Add(response.StatusCode);
        }

        Assert.Equal(20, codes.Count(c => c == HttpStatusCode.Accepted));
        Assert.Equal(2, codes.Count(c => c == HttpStatusCode.TooManyRequests));
    }

    // ── Background processing ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task APendingLookupThatWasNeverQueued_IsPickedUpBySweep()
    {
        var (userId, profile) = await StartAsync(_a, "sweep@example.com");
        var lookupId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            db.CatalogLookups.Add(new CatalogLookup
            {
                Id = lookupId, UserId = userId, PersonId = profile.Id, NameKey = "creatine", QueryName = "Creatine",
                Status = CatalogLookupStatus.Pending, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            });
            await db.SaveChangesAsync();
        }

        var service = _factory.Services.GetServices<IHostedService>().OfType<CatalogLookupHostedService>().Single();
        await service.SweepOnceAsync(CancellationToken.None);

        var done = await WaitAsync(_a, lookupId);
        Assert.Equal("matched", done.Status);
        Assert.Equal("creatine", done.Candidates[0].Slug);
    }

    [Fact]
    public async Task ProcessingIsIdempotent_AndLeavesAnAnsweredLookupAlone()
    {
        var (_, profile) = await StartAsync(_a, "idem@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Ozempic");
        var lookup = await CreateAndWaitAsync(_a, profile.Id, "Ozempic", item.Id);
        await _a.PostAsync($"/api/v1/catalog-lookups/{lookup.Id}/decline", null);

        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICatalogLookupProcessor>().ProcessAsync(lookup.Id);

        var stored = await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().CatalogLookups.AsNoTracking().SingleAsync(l => l.Id == lookup.Id);
        Assert.Equal("declined", stored.Status);
        Assert.Equal(0, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
    }

    // ── Notices as a feature ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Notes_CanBeReadDismissedAndMarkedAllRead()
    {
        var (userId, _) = await StartAsync(_a, "inbox@example.com");
        var ids = new List<Guid>();
        using (var scope = _factory.Services.CreateScope())
        {
            var notices = scope.ServiceProvider.GetRequiredService<INoticeService>();
            for (var i = 0; i < 3; i++)
            {
                await notices.UpsertAsync(userId, "test_kind", $"k{i}", "notice.test", new { n = i }, "/my-protocol", null);
            }
        }

        var list = (await _a.GetFromJsonAsync<List<NoticeResponse>>("/api/v1/notices", JsonOptions))!;
        Assert.Equal(3, list.Count);
        Assert.Equal(3, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
        ids.AddRange(list.Select(n => n.Id));

        Assert.Equal(HttpStatusCode.NoContent, (await _a.PostAsync($"/api/v1/notices/{ids[0]}/read", null)).StatusCode);
        Assert.Equal(2, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
        Assert.Equal(2, (await _a.GetFromJsonAsync<List<NoticeResponse>>("/api/v1/notices?unread=true", JsonOptions))!.Count);

        Assert.Equal(HttpStatusCode.NoContent, (await _a.DeleteAsync($"/api/v1/notices/{ids[1]}")).StatusCode);
        Assert.Equal(2, (await _a.GetFromJsonAsync<List<NoticeResponse>>("/api/v1/notices", JsonOptions))!.Count);

        Assert.Equal(HttpStatusCode.NoContent, (await _a.PostAsync("/api/v1/notices/read-all", null)).StatusCode);
        Assert.Equal(0, (await _a.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notices/unread-count", JsonOptions))!.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await _a.PostAsync($"/api/v1/notices/{Guid.NewGuid()}/read", null)).StatusCode);
    }

    [Fact]
    public async Task ARepeatedKey_UpdatesOneNote_AndOnlyChangedTextComesBackAsUnread()
    {
        var (userId, _) = await StartAsync(_a, "dedupe@example.com");
        using var scope = _factory.Services.CreateScope();
        var notices = scope.ServiceProvider.GetRequiredService<INoticeService>();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();

        await notices.UpsertAsync(userId, "test_kind", "same", "notice.test", new { name = "A" }, null, null);
        await notices.UpsertAsync(userId, "test_kind", "same", "notice.test", new { name = "A" }, null, null);
        Assert.Equal(1, await db.UserNotices.CountAsync(n => n.UserId == userId));

        var id = (await db.UserNotices.AsNoTracking().SingleAsync(n => n.UserId == userId)).Id;
        await notices.MarkReadAsync(userId, id);
        await notices.UpsertAsync(userId, "test_kind", "same", "notice.test", new { name = "A" }, null, null);   // nothing changed
        Assert.Equal(0, await notices.UnreadCountAsync(userId));

        await notices.UpsertAsync(userId, "test_kind", "same", "notice.test", new { name = "B" }, null, null);   // text changed
        Assert.Equal(1, await notices.UnreadCountAsync(userId));
        Assert.Equal(1, await db.UserNotices.CountAsync(n => n.UserId == userId));
    }

    [Theory]
    [InlineData("https://evil.example/phish")]
    [InlineData("//evil.example/phish")]
    [InlineData("javascript:alert(1)")]
    [InlineData("my-protocol")]
    [InlineData("/ok\\..\\bad")]
    [InlineData("/path://x")]
    public async Task ANoteCanOnlyPointInsideTheApp(string path)
    {
        var (userId, _) = await StartAsync(_a, $"path-{Guid.NewGuid():N}@example.com");
        using var scope = _factory.Services.CreateScope();
        var notices = scope.ServiceProvider.GetRequiredService<INoticeService>();

        await Assert.ThrowsAsync<ArgumentException>(() => notices.UpsertAsync(userId, "test_kind", "k", "notice.test", null, path, null));
        Assert.False(NoticeService.IsAppPath(path));
        Assert.True(NoticeService.IsAppPath("/my-protocol?tab=taking"));
        Assert.True(NoticeService.IsAppPath(null));
    }

    [Fact]
    public async Task OldAnsweredNotesAreRemoved_AndAPersonNeverHoldsMoreThanTwoHundred()
    {
        var (userId, _) = await StartAsync(_a, "retention@example.com");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var notices = scope.ServiceProvider.GetRequiredService<INoticeService>();
        var old = DateTime.UtcNow.AddDays(-100);
        db.UserNotices.Add(new UserNotice { Id = Guid.NewGuid(), UserId = userId, Kind = "old", TitleKey = "t", ParamsJson = "{}", DedupeKey = "old-read", CreatedAtUtc = old, UpdatedAtUtc = old, ReadAtUtc = old });
        db.UserNotices.Add(new UserNotice { Id = Guid.NewGuid(), UserId = userId, Kind = "old", TitleKey = "t", ParamsJson = "{}", DedupeKey = "old-unread", CreatedAtUtc = old, UpdatedAtUtc = old });
        await db.SaveChangesAsync();

        await notices.UpsertAsync(userId, "test_kind", "fresh", "notice.test", null, null, null);

        var keys = await db.UserNotices.AsNoTracking().Where(n => n.UserId == userId).Select(n => n.DedupeKey).ToListAsync();
        Assert.DoesNotContain("old-read", keys);       // read for 90+ days: gone
        Assert.Contains("old-unread", keys);           // never seen: kept

        for (var i = 0; i < NoticeService.PerUserCap + 20; i++)
        {
            await notices.UpsertAsync(userId, "test_kind", $"bulk-{i}", "notice.test", null, null, null);
        }

        Assert.True(await db.UserNotices.CountAsync(n => n.UserId == userId) <= NoticeService.PerUserCap);
        Assert.Contains(await db.UserNotices.AsNoTracking().Where(n => n.UserId == userId).Select(n => n.DedupeKey).ToListAsync(), k => k == $"bulk-{NoticeService.PerUserCap + 19}");
    }

    [Fact]
    public async Task TheLookupAndNoticeTables_AreExportedAndDeletedWithTheAccount()
    {
        var (userId, profile) = await StartAsync(_a, "export@example.com");
        var item = await AddItemAsync(_a, profile.Id, "Ozempic");
        await CreateAndWaitAsync(_a, profile.Id, "Ozempic", item.Id);

        var export = await _a.GetStringAsync("/api/v1/account/export?format=json");

        using var json = JsonDocument.Parse(export);
        var tables = json.RootElement.GetProperty("tables");
        Assert.Equal(1, tables.GetProperty("CatalogLookups").GetArrayLength());
        Assert.Equal(1, tables.GetProperty("UserNotices").GetArrayLength());
        Assert.Contains("Ozempic", export, StringComparison.Ordinal);
        Assert.NotEqual(Guid.Empty, userId);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────────

    private async Task<(Guid UserId, ProfileResponse Profile)> StartAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First(m => m.GetProperty("contact").GetString() == email).GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        var consent = await client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);

        var response = await client.PostAsJsonAsync("/api/v1/profiles", new CreateProfileRequest("Taker", Sex.Unspecified, 80m, 35, "goal", "notes"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;

        using var scope = _factory.Services.CreateScope();
        var id = await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().AppUsers.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        return (id, profile);
    }

    private async Task<CompoundResponse> AddItemAsync(HttpClient client, Guid profileId, string name)
    {
        var body = "{\"name\":\"" + name + "\",\"category\":\"Supplement\",\"status\":\"Active\",\"notes\":\"\",\"sourceType\":\"Manual\"}";
        var response = await client.PostAsync($"/api/v1/profiles/{profileId}/compounds", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
    }

    private async Task<CompoundResponse> GetItemAsync(HttpClient client, Guid profileId, Guid itemId)
    {
        var all = (await client.GetFromJsonAsync<List<CompoundResponse>>($"/api/v1/profiles/{profileId}/compounds", JsonOptions))!;
        return all.Single(c => c.Id == itemId);
    }

    private async Task<CatalogLookupResponse> CreateAndWaitAsync(HttpClient client, Guid profileId, string name, Guid? compoundId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/catalog-lookups", new { name, compoundId }, JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CatalogLookupResponse>(JsonOptions))!;
        return await WaitAsync(client, created.Id);
    }

    /// <summary>Polls the way the app does until the background check has answered.</summary>
    private static async Task<CatalogLookupResponse> WaitAsync(HttpClient client, Guid lookupId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var current = (await client.GetFromJsonAsync<CatalogLookupResponse>($"/api/v1/catalog-lookups/{lookupId}", JsonOptions))!;
            if (current.Status != "pending" || DateTime.UtcNow > deadline) return current;
            await Task.Delay(50);
        }
    }
}
