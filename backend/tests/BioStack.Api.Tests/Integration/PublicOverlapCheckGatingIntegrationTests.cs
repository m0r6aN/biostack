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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Owner ruling 2026-09-16, extended 2026-09-17: the ruling that per-pair interaction reasoning is
/// gated behind reviewed_relationship_graph (Operator) on every surface is extended explicitly to
/// POST /api/v1/knowledge/overlap-check — the live public compatibility tool
/// (frontend/src/components/knowledge/OverlapResults.tsx) that every visitor, signed in or not,
/// calls ungated. This mirrors ProtocolInteractionReasoningGatingIntegrationTests but asserts the
/// InteractionFlagResponse/ReducedInteractionFlagResponse boundary instead of
/// InteractionIntelligenceResponse/ReducedInteractionIntelligenceResponse.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PublicOverlapCheckGatingIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _anonymousClient = null!;
    private string _dbPath = string.Empty;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-overlap-gating-{Guid.NewGuid():N}.db");
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

        // Two compounds sharing a pathway so EvaluateByNamesAsync (used by CheckOverlapAsync) deterministically
        // produces a non-Neutral flag with per-pair reasoning attached — the same fixture pattern
        // OverlapServiceTests uses at the unit level.
        await using (var seedScope = _factory.Services.CreateAsyncScope())
        {
            var knowledgeSource = seedScope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
            await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry
            {
                CanonicalName = "OverlapProbeAlpha",
                Aliases = new List<string>(),
                Pathways = new List<string> { "overlap-probe-pathway" },
            });
            await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry
            {
                CanonicalName = "OverlapProbeBeta",
                Aliases = new List<string>(),
                Pathways = new List<string> { "overlap-probe-pathway" },
            });
        }

        _anonymousClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        _anonymousClient.Dispose();
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
    public async Task CheckOverlap_Anonymous_SeesNamesAndSeverityOnly_NoReasoningFields()
    {
        var response = await _anonymousClient.PostAsJsonAsync(
            "/api/v1/knowledge/overlap-check",
            new OverlapCheckRequest(new List<string> { "OverlapProbeAlpha", "OverlapProbeBeta" }),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var overlaps = doc.RootElement.GetProperty("overlaps");
        var flag = Assert.Single(overlaps.EnumerateArray());

        // The reasoning vocabulary must be absent entirely, not merely empty/null.
        Assert.False(flag.TryGetProperty("description", out _), "reduced shape must not carry 'description'");
        Assert.False(flag.TryGetProperty("evidenceConfidence", out _), "reduced shape must not carry 'evidenceConfidence'");

        // The flag itself — which pair, how severe — survives, unauthenticated.
        Assert.True(flag.TryGetProperty("id", out _));
        Assert.True(flag.TryGetProperty("createdAtUtc", out _));
        AssertReducedFlag(flag);
        var compoundNames = flag.GetProperty("compoundNames").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("OverlapProbeAlpha", compoundNames);
        Assert.Contains("OverlapProbeBeta", compoundNames);
    }

    [Fact]
    public async Task CheckOverlap_ObserverTier_SeesNamesAndSeverityOnly_NoReasoningFields()
    {
        // No subscription row is created, so FeatureGate.GetEffectiveTierAsync resolves to Observer
        // — the default, previously-ungated tier this defect leaked reasoning to.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SignInAsync(client, "overlap-observer@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge/overlap-check",
            new OverlapCheckRequest(new List<string> { "OverlapProbeAlpha", "OverlapProbeBeta" }),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var overlaps = doc.RootElement.GetProperty("overlaps");
        var flag = Assert.Single(overlaps.EnumerateArray());

        Assert.False(flag.TryGetProperty("description", out _), "reduced shape must not carry 'description'");
        Assert.False(flag.TryGetProperty("evidenceConfidence", out _), "reduced shape must not carry 'evidenceConfidence'");
        AssertReducedFlag(flag);
    }

    [Fact]
    public async Task CheckOverlap_Operator_SeesFullReasoningShape()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var userId = await SignInAsync(client, "overlap-operator@example.com");
        await SetSubscriptionAsync(userId, ProductTier.Operator, DateTime.UtcNow.AddDays(30));

        var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge/overlap-check",
            new OverlapCheckRequest(new List<string> { "OverlapProbeAlpha", "OverlapProbeBeta" }),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var overlaps = doc.RootElement.GetProperty("overlaps");
        var flag = Assert.Single(overlaps.EnumerateArray());

        Assert.True(flag.TryGetProperty("description", out var description));
        Assert.Contains("overlap-probe-pathway", description.GetString());
        Assert.True(flag.TryGetProperty("evidenceConfidence", out _));
        Assert.Equal("PathwayOverlap", flag.GetProperty("overlapType").GetString());
    }

    private static void AssertReducedFlag(JsonElement flag)
    {
        Assert.Equal(new[] { "compoundNames", "createdAtUtc", "id", "severity" },
            flag.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Assert.Equal(JsonValueKind.Null, flag.GetProperty("severity").ValueKind);
    }

    private async Task<Guid> SignInAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync(
            "/api/v1/auth/start",
            new StartAuthRequest(email, "email", "/profiles"),
            JsonOptions);

        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().Last().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");

        var consent = await client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
    }

    private async Task SetSubscriptionAsync(Guid userId, ProductTier tier, DateTime periodEndUtc)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(s => s.AppUserId == userId);
        if (subscription is null)
        {
            subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                AppUserId = userId,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1),
                StripeCustomerId = "cus_overlap_gating_test",
                StripeSubscriptionId = "sub_overlap_gating_test",
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
