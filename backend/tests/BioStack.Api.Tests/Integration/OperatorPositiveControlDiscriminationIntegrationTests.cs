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
/// H1-POSITIVE-CONTROL (Gate 2 hardening parcel, bounded-remediation:H1-from-BIO-LOCAL-005-review-2).
///
/// BIO-LOCAL-005's independent reviewer #2 passed 8/10 adversarial probes but recorded one
/// critical caveat (`biostack-coordination/logs/bio-local-005-retro-2.log`): the Operator-tier
/// "full shape" positive control recorded in
/// docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-005-guidance-enforcement-proof.md
/// was a manual, live HTTP replay (synthetic SQLite rows inserted via `sqlite3` inside the dev
/// container, dev magic-link inbox) that the reviewer could not independently reproduce despite
/// creating equivalent test data and valid JWTs — "This leaves open whether system truly
/// discriminates or implements blanket-deny. Code structure supports discrimination, but I
/// couldn't observe it working live."
///
/// This test is the automated, deterministic, locally-runnable replacement for that manual
/// replay. It reproduces the same sequential discrimination table BIO-LOCAL-005's evidence file
/// recorded by hand (anonymous/Observer reduced -> Operator full -> downgrade reverts live) as a
/// single `dotnet test` run requiring no Docker, no live SMTP, and no manual SQLite fixture
/// insertion, for every reasoning-gated surface named by the B3/B4/B5 owner rulings
/// (`docs/guidance/RATIFICATION.md`): the public `interaction-check` and `overlap-check`
/// endpoints, and the authenticated `/api/v1/protocols/{id}` surface.
///
/// Individual surface/tier pairs are already covered piecemeal by
/// <see cref="PublicInteractionCheckGatingIntegrationTests"/>,
/// <see cref="PublicOverlapCheckGatingIntegrationTests"/>, and
/// <see cref="ProtocolInteractionReasoningGatingIntegrationTests"/>. This file adds the one
/// scenario none of those cover automatically — a LAPSED Operator subscription
/// (`CurrentPeriodEndUtc` in the past) reverting a previously-entitled session back to the
/// reduced shape on the very next request — and consolidates the full discrimination matrix
/// (anonymous, Observer, Operator, Commander, expired-Operator) into one artifact a reviewer can
/// point to as "the" positive control.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OperatorPositiveControlDiscriminationIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private WebApplicationFactory<Program> _factory = null!;
    private string _dbPath = string.Empty;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-h1-positive-control-{Guid.NewGuid():N}.db");
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

        // Two compounds whose KnowledgeEntry fields deterministically produce a non-Neutral,
        // reasoning-bearing pair on both the interaction and overlap fallback evaluators, mirroring
        // the fixture pattern the existing per-surface gating tests already use.
        await using var seedScope = _factory.Services.CreateAsyncScope();
        var knowledgeSource = seedScope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
        await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry
        {
            CanonicalName = "H1ProbeAlpha",
            AvoidWith = new List<string> { "H1ProbeBeta" },
            Pathways = new List<string> { "h1-probe-pathway" },
        });
        await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry
        {
            CanonicalName = "H1ProbeBeta",
            Pathways = new List<string> { "h1-probe-pathway" },
        });
    }

    public async Task DisposeAsync()
    {
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
    public async Task InteractionCheck_DiscriminationMatrix_EntitledFull_UnentitledReduced_LiveReevaluatedOnDowngrade()
    {
        var anonymousClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var anonymous = await CheckInteractionsAsync(anonymousClient);
        AssertReducedInteractionShape(anonymous);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var userId = await SignInAsync(client, "h1-interaction-check@example.com");

        var observer = await CheckInteractionsAsync(client);
        AssertReducedInteractionShape(observer);

        await SetSubscriptionAsync(userId, ProductTier.Operator, DateTime.UtcNow.AddDays(30));
        var operatorShape = await CheckInteractionsAsync(client);
        AssertFullInteractionShape(operatorShape);

        await SetSubscriptionAsync(userId, ProductTier.Commander, DateTime.UtcNow.AddDays(30));
        var commanderShape = await CheckInteractionsAsync(client);
        AssertFullInteractionShape(commanderShape);

        // Fail-closed liveness: a lapsed Operator subscription (same session, no re-login) must
        // revert to the reduced shape on the very next request, proving entitlement is checked
        // live per-request rather than cached on the session.
        await SetSubscriptionAsync(userId, ProductTier.Operator, DateTime.UtcNow.AddDays(-1));
        var expiredOperator = await CheckInteractionsAsync(client);
        AssertReducedInteractionShape(expiredOperator);
    }

    [Fact]
    public async Task OverlapCheck_DiscriminationMatrix_EntitledFull_UnentitledReduced_LiveReevaluatedOnDowngrade()
    {
        var anonymousClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var anonymous = await CheckOverlapAsync(anonymousClient);
        AssertReducedOverlapShape(anonymous);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var userId = await SignInAsync(client, "h1-overlap-check@example.com");

        var observer = await CheckOverlapAsync(client);
        AssertReducedOverlapShape(observer);

        await SetSubscriptionAsync(userId, ProductTier.Operator, DateTime.UtcNow.AddDays(30));
        var operatorShape = await CheckOverlapAsync(client);
        AssertFullOverlapShape(operatorShape);

        await SetSubscriptionAsync(userId, ProductTier.Commander, DateTime.UtcNow.AddDays(30));
        var commanderShape = await CheckOverlapAsync(client);
        AssertFullOverlapShape(commanderShape);

        await SetSubscriptionAsync(userId, ProductTier.Operator, DateTime.UtcNow.AddDays(-1));
        var expiredOperator = await CheckOverlapAsync(client);
        AssertReducedOverlapShape(expiredOperator);
    }

    [Fact]
    public async Task ProtocolInteractionIntelligence_DiscriminationMatrix_EntitledFull_UnentitledReduced_LiveReevaluatedOnDowngrade()
    {
        using var anonymousClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var userId = await SignInAsync(client, "h1-protocol-gating@example.com");
        var profile = await CreateProfileAsync(client, "H1 positive-control probe");
        await CreateActiveCompoundAsync(client, profile.Id, "H1ProbeAlpha");
        await CreateActiveCompoundAsync(client, profile.Id, "H1ProbeBeta");
        var protocolId = await SaveProtocolAsync(client, profile.Id, "H1 positive-control protocol");

        // Anonymous access to the owner's protocol is refused outright (no reduced/full shape
        // applies because authentication itself is required) — the first row of the matrix.
        var anonymousResponse = await anonymousClient.GetAsync($"/api/v1/protocols/{protocolId}");
        Assert.True(
            anonymousResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.Redirect,
            $"expected anonymous access to be refused, got {anonymousResponse.StatusCode}");

        var observer = await GetProtocolAsync(client, protocolId);
        AssertReducedInteractionShape(observer.GetProperty("interactionIntelligence"));

        await SetSubscriptionAsync(userId, ProductTier.Operator, DateTime.UtcNow.AddDays(30));
        var operatorShape = await GetProtocolAsync(client, protocolId);
        AssertFullInteractionShape(operatorShape.GetProperty("interactionIntelligence"));

        await SetSubscriptionAsync(userId, ProductTier.Commander, DateTime.UtcNow.AddDays(30));
        var commanderShape = await GetProtocolAsync(client, protocolId);
        AssertFullInteractionShape(commanderShape.GetProperty("interactionIntelligence"));

        await SetSubscriptionAsync(userId, ProductTier.Operator, DateTime.UtcNow.AddDays(-1));
        var expiredOperator = await GetProtocolAsync(client, protocolId);
        AssertReducedInteractionShape(expiredOperator.GetProperty("interactionIntelligence"));
    }

    private static readonly string[] InteractionReasoningProperties =
    {
        "reason", "message", "confidence", "sharedPathways", "topFindings",
        "interactions", "counterfactuals", "swaps", "compositeScore", "score",
    };

    private static void AssertReducedInteractionShape(JsonElement root)
    {
        foreach (var property in InteractionReasoningProperties)
        {
            Assert.False(root.TryGetProperty(property, out _), $"reduced shape must not carry '{property}'");
        }

        Assert.True(root.TryGetProperty("pairs", out var pairs));
        var pair = Assert.Single(pairs.EnumerateArray());
        Assert.Equal(new[] { "compoundA", "compoundB", "severity" },
            pair.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.Equal(JsonValueKind.Null, pair.GetProperty("severity").ValueKind);
        Assert.Equal(new[] { "pairs" }, root.EnumerateObject().Select(p => p.Name).ToArray());
    }

    private static void AssertFullInteractionShape(JsonElement root)
    {
        Assert.False(root.TryGetProperty("pairs", out _), "full shape must not carry the reduced 'pairs' field");
        Assert.True(root.TryGetProperty("interactions", out var interactions));
        var interaction = Assert.Single(interactions.EnumerateArray());
        Assert.Equal("Interfering", interaction.GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(interaction.GetProperty("reason").GetString()));
        Assert.True(interaction.TryGetProperty("confidence", out _));
    }

    private static void AssertReducedOverlapShape(JsonElement root)
    {
        var overlaps = root.GetProperty("overlaps");
        var flag = Assert.Single(overlaps.EnumerateArray());
        Assert.False(flag.TryGetProperty("description", out _), "reduced shape must not carry 'description'");
        Assert.False(flag.TryGetProperty("evidenceConfidence", out _), "reduced shape must not carry 'evidenceConfidence'");
        Assert.Equal(new[] { "compoundNames", "createdAtUtc", "id", "severity" },
            flag.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.Equal(JsonValueKind.Null, flag.GetProperty("severity").ValueKind);
    }

    private static void AssertFullOverlapShape(JsonElement root)
    {
        var overlaps = root.GetProperty("overlaps");
        var flag = Assert.Single(overlaps.EnumerateArray());
        Assert.True(flag.TryGetProperty("description", out var description));
        Assert.False(string.IsNullOrWhiteSpace(description.GetString()));
        Assert.True(flag.TryGetProperty("evidenceConfidence", out _));
    }

    private static Task<HttpResponseMessage> PostInteractionCheckAsync(HttpClient client) =>
        client.PostAsJsonAsync(
            "/api/v1/knowledge/interaction-check",
            new OverlapCheckRequest(new List<string> { "H1ProbeAlpha", "H1ProbeBeta" }),
            JsonOptions);

    private static async Task<JsonElement> CheckInteractionsAsync(HttpClient client)
    {
        var response = await PostInteractionCheckAsync(client);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static async Task<JsonElement> CheckOverlapAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge/overlap-check",
            new OverlapCheckRequest(new List<string> { "H1ProbeAlpha", "H1ProbeBeta" }),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static async Task<JsonElement> GetProtocolAsync(HttpClient client, Guid protocolId)
    {
        var response = await client.GetAsync($"/api/v1/protocols/{protocolId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
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

    private async Task<ProfileResponse> CreateProfileAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/profiles",
            new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private static async Task CreateActiveCompoundAsync(HttpClient client, Guid profileId, string name)
    {
        var response = await client.PostAsJsonAsync(
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

    private static async Task<Guid> SaveProtocolAsync(HttpClient client, Guid profileId, string name)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/protocols",
            new SaveProtocolRequest(name),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var protocol = await response.Content.ReadFromJsonAsync<ProtocolResponse>(JsonOptions);
        return protocol!.Id;
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
                CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-10),
                StripeCustomerId = "cus_h1_positive_control",
                StripeSubscriptionId = "sub_h1_positive_control",
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Subscriptions.Add(subscription);
        }

        subscription.Tier = tier;
        subscription.Status = SubscriptionStatus.Active;
        subscription.ProductCode = tier.ToString().ToLowerInvariant();
        subscription.StripePriceId = tier == ProductTier.Commander ? "price_commander" : "price_operator";
        subscription.CurrentPeriodEndUtc = periodEndUtc;
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
