namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Owner ruling 2026-09-16 (B3), extended 2026-09-16 (B4): "The public view should definitely be
/// the same as observed [Observer]." <c>POST /api/v1/knowledge/interaction-check</c> — the public,
/// anonymous-by-design "check two compounds" surface identified but left ungated in B3 — must
/// return the same reduced shape (pair names/ids and severity, no reasoning) that Observer gets
/// from every other surface, routed through the same <see cref="InteractionIntelligenceProjection"/>
/// #369 introduced. An authenticated caller holding reviewed_relationship_graph gets the full shape
/// from this same endpoint, since it accepts (but does not require) authenticated calls.
///
/// Mirrors the style of
/// KnowledgeEndpointsIntegrationTests.GetCompound_PublicSerializationPreservesEvidenceAndOmitsIndividualizedActionFields
/// and ProtocolInteractionReasoningGatingIntegrationTests (assert reasoning fields absent, not
/// merely empty).
/// </summary>
[Trait("Category", "Integration")]
public sealed class PublicInteractionCheckGatingIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-public-interaction-check-{Guid.NewGuid():N}.db");
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
        // Interfering pair with reasoning ("Avoid-with guidance directly links these compounds."),
        // same fixture shape ProtocolInteractionReasoningGatingIntegrationTests uses.
        await using var seedScope = _factory.Services.CreateAsyncScope();
        var knowledgeSource = seedScope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
        await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry
        {
            CanonicalName = "PublicCheckAlpha",
            AvoidWith = new List<string> { "PublicCheckBeta" },
        });
        await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry
        {
            CanonicalName = "PublicCheckBeta",
        });
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
    public async Task CheckInteractions_Anonymous_SeesPairAndSeverityOnly_NoReasoningFields()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/knowledge/interaction-check",
            new OverlapCheckRequest(new List<string> { "PublicCheckAlpha", "PublicCheckBeta" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        // The entire per-pair reasoning vocabulary must be absent, not merely empty.
        var reasoningProperties = new[]
        {
            "reason", "message", "confidence", "sharedPathways", "topFindings",
            "interactions", "counterfactuals", "swaps", "compositeScore", "score"
        };
        foreach (var property in reasoningProperties)
        {
            Assert.False(root.TryGetProperty(property, out _), $"reduced shape must not carry '{property}'");
        }

        // Only pair names and severity survive.
        Assert.True(root.TryGetProperty("pairs", out var pairs));
        var pair = Assert.Single(pairs.EnumerateArray());
        Assert.Equal("PublicCheckAlpha", pair.GetProperty("compoundA").GetString());
        Assert.Equal("PublicCheckBeta", pair.GetProperty("compoundB").GetString());
        Assert.Equal("Interfering", pair.GetProperty("severity").GetString());
        Assert.True(root.TryGetProperty("summary", out _));
    }

    [Fact]
    public async Task CheckInteractions_AuthenticatedOperator_SeesFullReasoningShape()
    {
        await SignInAsync("public-interaction-check-operator@example.com");
        await SetSubscriptionAsync(ProductTier.Operator, DateTime.UtcNow.AddDays(30));

        var response = await _client.PostAsJsonAsync(
            "/api/v1/knowledge/interaction-check",
            new OverlapCheckRequest(new List<string> { "PublicCheckAlpha", "PublicCheckBeta" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.False(root.TryGetProperty("pairs", out _), "full shape must not carry the reduced 'pairs' field");
        Assert.True(root.TryGetProperty("interactions", out var interactions));
        var interaction = Assert.Single(interactions.EnumerateArray());
        Assert.Equal("Interfering", interaction.GetProperty("type").GetString());
        Assert.Equal(
            "Avoid-with guidance directly links these compounds.",
            interaction.GetProperty("reason").GetString());
        Assert.True(interaction.TryGetProperty("confidence", out _));
    }

    [Fact]
    public async Task CheckInteractions_AuthenticatedObserver_SeesPairAndSeverityOnly_NoReasoningFields()
    {
        // Signed in, but no subscription row: FeatureGate.GetEffectiveTierAsync resolves to
        // Observer, the same default tier the anonymous caller is treated as, per the ruling.
        await SignInAsync("public-interaction-check-observer@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/knowledge/interaction-check",
            new OverlapCheckRequest(new List<string> { "PublicCheckAlpha", "PublicCheckBeta" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.False(root.TryGetProperty("interactions", out _));
        var pair = Assert.Single(root.GetProperty("pairs").EnumerateArray());
        Assert.Equal("Interfering", pair.GetProperty("severity").GetString());
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

    private async Task SetSubscriptionAsync(ProductTier tier, DateTime periodEndUtc)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var subscription = await db.Subscriptions.SingleOrDefaultAsync();
        var appUserId = await db.AppUsers.Select(u => u.Id).SingleAsync();
        if (subscription is null)
        {
            subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                AppUserId = appUserId,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1),
                StripeCustomerId = "cus_public_check_test",
                StripeSubscriptionId = "sub_public_check_test",
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
