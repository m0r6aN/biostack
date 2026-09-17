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
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Owner ruling 2026-09-16, parcel B3: per-pair interaction reasoning is gated behind the
/// reviewed_relationship_graph entitlement (Operator) on every surface, including
/// /api/v1/protocols/*, which previously served the full reasoning shape to every authenticated
/// tier. Observer must see only the pair names and a severity value — no reasoning fields at all
/// (asserted by absence, in the spirit of
/// KnowledgeEndpointsIntegrationTests.GetCompound_PublicSerializationPreservesEvidenceAndOmitsIndividualizedActionFields).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProtocolInteractionReasoningGatingIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private Guid _userId;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-interaction-gating-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
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

        // Two compounds whose KnowledgeEntry AvoidWith fields mutually reference each other, so
        // the fallback interaction path (no reviewed graph needed) deterministically produces an
        // Interfering pair with reasoning ("Avoid-with guidance directly links these compounds.").
        await using (var seedScope = _factory.Services.CreateAsyncScope())
        {
            var knowledgeSource = seedScope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
            await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry
            {
                CanonicalName = "GatingProbeAlpha",
                AvoidWith = new List<string> { "GatingProbeBeta" },
            });
            await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry
            {
                CanonicalName = "GatingProbeBeta",
            });
        }

        _userId = await SignInAsync("interaction-gating@example.com");
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
    public async Task GetProtocol_Observer_SeesPairAndSeverityOnly_NoReasoningFields()
    {
        var profile = await CreateProfileAsync("Observer gating probe");
        await CreateActiveCompoundAsync(profile.Id, "GatingProbeAlpha");
        await CreateActiveCompoundAsync(profile.Id, "GatingProbeBeta");
        var protocolId = await SaveProtocolAsync(profile.Id, "Observer gating protocol");

        // No subscription row was created for this user, so FeatureGate.GetEffectiveTierAsync
        // resolves to Observer — the default, ungated tier this defect used to leak reasoning to.
        var response = await _client.GetAsync($"/api/v1/protocols/{protocolId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var intelligence = doc.RootElement.GetProperty("interactionIntelligence");

        // The entire per-pair reasoning vocabulary must be absent, not merely empty.
        var reasoningProperties = new[] { "reason", "message", "confidence", "sharedPathways", "topFindings", "interactions", "counterfactuals", "swaps", "compositeScore", "score" };
        foreach (var property in reasoningProperties)
        {
            Assert.False(intelligence.TryGetProperty(property, out _), $"reduced shape must not carry '{property}'");
        }

        // Only pair names and severity survive.
        Assert.True(intelligence.TryGetProperty("pairs", out var pairs));
        var pair = Assert.Single(pairs.EnumerateArray());
        // Protocol item order is not a contract; verify the exact unordered pair.
        Assert.Equal(new[] { "GatingProbeAlpha", "GatingProbeBeta" },
            new[] { pair.GetProperty("compoundA").GetString(), pair.GetProperty("compoundB").GetString() }
                .OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Assert.Equal("Interfering", pair.GetProperty("severity").GetString());
        Assert.True(intelligence.TryGetProperty("summary", out _));

        // The same reasoning text must not have leaked through Simulation.Insights either.
        var insights = doc.RootElement.GetProperty("simulation").GetProperty("insights").EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        Assert.DoesNotContain(insights, i => i != null &&
            i.Contains("GatingProbeAlpha", StringComparison.Ordinal) &&
            i.Contains("GatingProbeBeta", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetProtocol_Operator_SeesFullReasoningShape()
    {
        var profile = await CreateProfileAsync("Operator gating probe");
        await CreateActiveCompoundAsync(profile.Id, "GatingProbeAlpha");
        await CreateActiveCompoundAsync(profile.Id, "GatingProbeBeta");
        var protocolId = await SaveProtocolAsync(profile.Id, "Operator gating protocol");

        await SetSubscriptionAsync(ProductTier.Operator, DateTime.UtcNow.AddDays(30));

        var response = await _client.GetAsync($"/api/v1/protocols/{protocolId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var intelligence = doc.RootElement.GetProperty("interactionIntelligence");

        Assert.False(intelligence.TryGetProperty("pairs", out _), "full shape must not carry the reduced 'pairs' field");
        Assert.True(intelligence.TryGetProperty("interactions", out var interactions));
        var interaction = Assert.Single(interactions.EnumerateArray());
        Assert.Equal("Interfering", interaction.GetProperty("type").GetString());
        Assert.Equal(
            "Avoid-with guidance directly links these compounds.",
            interaction.GetProperty("reason").GetString());
        Assert.True(interaction.TryGetProperty("confidence", out _));

        var insights = doc.RootElement.GetProperty("simulation").GetProperty("insights").EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        // Simulation carries the finding message, not the underlying pair's Reason text.
        var expectedFinding = $"{interaction.GetProperty("compoundA").GetString()} and {interaction.GetProperty("compoundB").GetString()} raise a review-first interaction signal.";
        Assert.Contains(expectedFinding, insights);
    }

    [Fact]
    public async Task GetProtocol_Anonymous_Unauthorized()
    {
        var profile = await CreateProfileAsync("Anonymous gating probe");
        await CreateActiveCompoundAsync(profile.Id, "GatingProbeAlpha");
        var protocolId = await SaveProtocolAsync(profile.Id, "Anonymous gating protocol");

        using var anonymousClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await anonymousClient.GetAsync($"/api/v1/protocols/{protocolId}");

        // Anonymous access to /api/v1/protocols/* is unchanged by this fix: RequireAuthorization()
        // already blocked it before, and still does.
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.Redirect,
            $"expected anonymous access to be refused, got {response.StatusCode}");
    }

    private async Task<Guid> SignInAsync(string email)
    {
        await _client.PostAsJsonAsync(
            "/api/v1/auth/start",
            new StartAuthRequest(email, "email", "/profiles"),
            JsonOptions);

        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");

        var consent = await _client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
    }

    private async Task<ProfileResponse> CreateProfileAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/profiles",
            new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private async Task CreateActiveCompoundAsync(Guid profileId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            new CreateCompoundRequest(
                name,
                CompoundCategory.Peptide,
                DateTime.UtcNow.Date.AddDays(-7),
                null,
                CompoundStatus.Active,
                "notes",
                SourceType.Manual),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<Guid> SaveProtocolAsync(Guid profileId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/protocols",
            new SaveProtocolRequest(name),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var protocol = await response.Content.ReadFromJsonAsync<ProtocolResponse>(JsonOptions);
        return protocol!.Id;
    }

    private async Task SetSubscriptionAsync(ProductTier tier, DateTime periodEndUtc)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(s => s.AppUserId == _userId);
        if (subscription is null)
        {
            subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                AppUserId = _userId,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1),
                StripeCustomerId = "cus_gating_test",
                StripeSubscriptionId = "sub_gating_test",
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Subscriptions.Add(subscription);
        }

        subscription.Tier = tier;
        subscription.ProductCode = tier.ToString().ToLowerInvariant();
        subscription.StripePriceId = tier == ProductTier.Commander ? "price_commander" : "price_operator";
        subscription.CurrentPeriodEndUtc = periodEndUtc;
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
