namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stripe;
using Xunit;

[Trait("Category", "Integration")]
public sealed class EverythingTakingCatalogCapIntegrationTests : IAsyncLifetime
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
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-taking-{Guid.NewGuid():N}.db");
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
                        ["Stripe:OperatorPriceId"] = "price_operator",
                        ["Stripe:CommanderPriceId"] = "price_commander",
                        ["Stripe:WebhookSecret"] = "whsec_test",
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
            if (System.IO.File.Exists(_dbPath))
            {
                System.IO.File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
        }
    }

    private async Task<(Guid UserId, ProfileResponse Profile)> StartAsync(string email)
    {
        var userId = await SignInAsync(email);
        var profile = await CreateProfileAsync("Taker");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        db.KnowledgeEntries.AddRange(
            new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = "Creatine", Classification = CompoundCategory.Supplement, Aliases = ["creatine monohydrate"] },
            new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = "Vitamin D3", Classification = CompoundCategory.Vitamin },
            new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = "BPC-157", Classification = CompoundCategory.Peptide });
        await db.SaveChangesAsync();
        return (userId, profile);
    }

    private Task<HttpResponseMessage> PostRawAsync(Guid profileId, string json)
        => _client.PostAsync($"/api/v1/profiles/{profileId}/compounds", new StringContent(json, Encoding.UTF8, "application/json"));

    private static string Body(string name, string category, string? slug = null, string status = "Active", string extra = "")
        => "{\"name\":\"" + name + "\",\"category\":\"" + category + "\",\"status\":\"" + status + "\",\"notes\":\"\",\"sourceType\":\"Manual\""
           + (slug is null ? "" : ",\"catalogSlug\":\"" + slug + "\"") + extra + "}";

    [Fact]
    public async Task LinkingSetsServerDerivedState_AndNeverPrefillsAnyDose()
    {
        var (_, profile) = await StartAsync("link-user@example.com");

        var response = await PostRawAsync(profile.Id, Body("My creatine", "Peptide", "creatine"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
        Assert.Equal("creatine", created.CatalogSlug);
        Assert.Equal(CompoundCategory.Supplement, created.Category); // from the catalog, not the client's Peptide
        Assert.Equal("catalog", created.VerificationState);
        Assert.Equal("catalog", created.CategorySource);
        Assert.True(created.IncludeInProviderSummary);
        Assert.Null(created.DoseAmount);
        Assert.Null(created.DoseUnit);
        Assert.Null(created.DoseFrequency);
        Assert.Null(created.DoseTime);
        Assert.Null(created.FrequencyKind);
    }

    [Fact]
    public async Task ForgedClientStateAndCategory_AreIgnored_AndStillCountTowardTheCap()
    {
        var (_, profile) = await StartAsync("forge-user@example.com");
        var limit = ProductContract.Current.GetLimit(FeatureCodes.ActiveCompounds, ProductTier.Observer)!.Value;
        const string forged = ",\"verificationState\":\"catalog\",\"categorySource\":\"catalog\",\"catalogSlug\":null";

        for (var i = 1; i <= limit; i++)
        {
            var ok = await PostRawAsync(profile.Id, Body($"Custom {i}", "Supplement", extra: forged));
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
            var created = (await ok.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
            Assert.Equal("unverified", created.VerificationState);
            Assert.Equal("user", created.CategorySource);
        }

        var blocked = await PostRawAsync(profile.Id, Body("One more vitamin", "Vitamin", extra: forged));
        Assert.Equal(HttpStatusCode.PaymentRequired, blocked.StatusCode);
        var error = (await blocked.Content.ReadFromJsonAsync<ProductErrorResponse>(JsonOptions))!;
        Assert.Equal("observer_active_compound_limit", error.Code);
        Assert.Contains("research or unconfirmed", error.Message);

        // a confirmed catalog supplement is still addable at the cap; a research record is not
        Assert.Equal(HttpStatusCode.Created, (await PostRawAsync(profile.Id, Body("Creatine", "Other", "creatine"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostRawAsync(profile.Id, Body("D3", "Other", "vitamin-d3"))).StatusCode);
        Assert.Equal(HttpStatusCode.PaymentRequired, (await PostRawAsync(profile.Id, Body("BPC", "Other", "bpc-157"))).StatusCode);

        var taking = await _client.GetFromJsonAsync<TakingResponse>($"/api/v1/profiles/{profile.Id}/taking", JsonOptions);
        Assert.NotNull(taking);
        Assert.Equal(limit + 2, taking.Items.Count);
        Assert.Equal(limit, taking.Cap.Counted);
        Assert.Equal(limit, taking.Cap.Limit);
        Assert.Equal(2, taking.Items.Count(item => !item.CountsTowardCap));
    }

    [Fact]
    public async Task UnknownCatalogSlug_Returns400WithTheFieldName()
    {
        var (_, profile) = await StartAsync("unknown-slug@example.com");

        var response = await PostRawAsync(profile.Id, Body("Mystery", "Supplement", "nope-not-real"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("catalogSlug", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StaleClientUpdateWithoutNewFields_KeepsLinkAndProviderSummaryChoice_AndToggleRoundTrips()
    {
        var (_, profile) = await StartAsync("stale-client@example.com");
        var created = (await (await PostRawAsync(profile.Id, Body("Creatine", "Other", "creatine"))).Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;

        // an old tab's PUT knows nothing about catalogSlug or includeInProviderSummary
        var stale = await _client.PutAsync($"/api/v1/profiles/{profile.Id}/compounds/{created.Id}",
            new StringContent(Body("Creatine", "Other", status: "Paused"), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
        var kept = (await stale.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
        Assert.Equal("creatine", kept.CatalogSlug);
        Assert.Equal(CompoundCategory.Supplement, kept.Category);
        Assert.True(kept.IncludeInProviderSummary);

        var toggled = await _client.PutAsync($"/api/v1/profiles/{profile.Id}/compounds/{created.Id}",
            new StringContent(Body("Creatine", "Other", status: "Active", extra: ",\"includeInProviderSummary\":false"), Encoding.UTF8, "application/json"));
        Assert.False((await toggled.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!.IncludeInProviderSummary);
        var list = await _client.GetFromJsonAsync<List<CompoundResponse>>($"/api/v1/profiles/{profile.Id}/compounds", JsonOptions);
        Assert.False(list!.Single().IncludeInProviderSummary);

        var unlinked = await _client.PutAsync($"/api/v1/profiles/{profile.Id}/compounds/{created.Id}",
            new StringContent(Body("Creatine", "Supplement", slug: "", status: "Active"), Encoding.UTF8, "application/json"));
        var after = (await unlinked.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
        Assert.Null(after.CatalogSlug);
        Assert.Equal("unverified", after.VerificationState);
    }

    [Fact]
    public async Task Taking_OffersExactMatches_ButNeverAutoLinks_AndRequiresOwnership()
    {
        var (_, profile) = await StartAsync("suggest-user@example.com");
        await PostRawAsync(profile.Id, Body("creatine monohydrate", "Other"));

        var taking = await _client.GetFromJsonAsync<TakingResponse>($"/api/v1/profiles/{profile.Id}/taking", JsonOptions);

        var item = Assert.Single(taking!.Items);
        Assert.Null(item.Compound.CatalogSlug);
        Assert.True(item.CountsTowardCap);
        var suggestion = Assert.Single(item.Suggestions);
        Assert.Equal("creatine", suggestion.Slug);
        Assert.Equal("alias", suggestion.MatchedOn);

        var other = await _client.GetAsync($"/api/v1/profiles/{Guid.NewGuid()}/taking");
        Assert.True(other.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden, other.StatusCode.ToString());
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
        var response = await _client.PostAsJsonAsync("/api/v1/profiles", new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }
}
