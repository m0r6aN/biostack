namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// BIO-PAIRWISE-004 / PW-004-A1: the fourteen hand-authored <see cref="CompoundInteractionHint"/>
/// catalog rows carry no citation of any kind (2026-09-17 owner ratification). This parcel gives
/// the entity an explicit sourced/unsourced provenance discriminator defaulting to unsourced, and
/// proves the quarantine is structural rather than a convention: an unsourced hint continues to
/// drive the internal score for an entitled (reviewed_relationship_graph) caller unchanged, but
/// never reaches an anonymous or Observer response, and nothing labelled as evidence (reasoning
/// text, confidence, mechanism) leaks through the pair-reduction boundary already established by
/// the 2026-09-16 owner ruling (B3) and routed through
/// <see cref="BioStack.Application.Services.InteractionIntelligenceProjection"/>. A sourced hint is
/// not additionally blocked anywhere — the discriminator, not a table, decides, and this parcel
/// does not re-open the reasoning-gating ruling itself (that remains Out of Scope; see the spec).
/// </summary>
[Trait("Category", "Integration")]
public sealed class InteractionHintQuarantineIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    private const string UnsourcedCompoundA = "QuarantineHintAlpha";
    private const string UnsourcedCompoundB = "QuarantineHintBeta";
    private const string SourcedCompoundA = "QuarantineHintGamma";
    private const string SourcedCompoundB = "QuarantineHintDelta";

    private const decimal UnsourcedStrength = 0.85m;
    private const string UnsourcedNotes = "Quarantine fixture: unsourced hint, must never be evidence.";
    private const decimal SourcedStrength = 0.80m;
    private const string SourcedNotes = "Quarantine fixture: sourced hint, not blocked by quarantine.";
    private const string SourceReference = "https://example.test/evidence/quarantine-fixture-1";

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-hint-quarantine-{Guid.NewGuid():N}.db");
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
                            ["FrontendUrl"] = "http://localhost:3044",
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

        await using var seedScope = _factory.Services.CreateAsyncScope();
        var knowledgeSource = seedScope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
        foreach (var name in new[] { UnsourcedCompoundA, UnsourcedCompoundB, SourcedCompoundA, SourcedCompoundB })
        {
            await knowledgeSource.UpsertCompoundAsync(new KnowledgeEntry { CanonicalName = name });
        }

        var db = seedScope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var (unsourcedA, unsourcedB) = CompoundInteractionHintRepository.NormalizePair(UnsourcedCompoundA, UnsourcedCompoundB);
        var (sourcedA, sourcedB) = CompoundInteractionHintRepository.NormalizePair(SourcedCompoundA, SourcedCompoundB);

        db.CompoundInteractionHints.Add(new CompoundInteractionHint
        {
            Id = Guid.NewGuid(),
            CompoundA = unsourcedA,
            CompoundB = unsourcedB,
            InteractionType = InteractionType.Synergistic,
            Strength = UnsourcedStrength,
            Notes = UnsourcedNotes,
            // Explicit for test clarity, matching the entity's default.
            IsSourced = false,
            SourceReference = null,
        });

        db.CompoundInteractionHints.Add(new CompoundInteractionHint
        {
            Id = Guid.NewGuid(),
            CompoundA = sourcedA,
            CompoundB = sourcedB,
            InteractionType = InteractionType.Synergistic,
            Strength = SourcedStrength,
            Notes = SourcedNotes,
            IsSourced = true,
            SourceReference = SourceReference,
        });

        await db.SaveChangesAsync();
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

    // --- AC3: catalog defaults persist and are explicitly marked unsourced -------------------

    [Fact]
    public async Task CatalogDefaults_AllFourteenRows_PersistAndAreMarkedUnsourced()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var hintRepository = scope.ServiceProvider.GetRequiredService<ICompoundInteractionHintRepository>();

        await CompoundInteractionHintCatalog.SeedDefaultsAsync(hintRepository);

        var catalogPairs = CompoundInteractionHintCatalog.Defaults
            .Select(hint => CompoundInteractionHintRepository.NormalizePair(hint.CompoundA, hint.CompoundB))
            .ToHashSet();

        var allHints = await hintRepository.GetAllAsync();
        var persisted = allHints
            .Where(hint => catalogPairs.Contains((hint.CompoundA, hint.CompoundB)))
            .ToList();

        Assert.Equal(14, CompoundInteractionHintCatalog.Defaults.Count);
        Assert.Equal(14, persisted.Count);
        Assert.All(persisted, hint =>
        {
            Assert.False(hint.IsSourced, $"{hint.CompoundA}/{hint.CompoundB} must default to unsourced");
            Assert.Null(hint.SourceReference);
        });
    }

    // --- AC4: unsourced hint reaches no anonymous response, no Observer response, no evidence -

    [Fact]
    public async Task UnsourcedHint_Anonymous_SeesNoReasoningOrEvidence()
    {
        var response = await CheckInteractionsAsync(UnsourcedCompoundA, UnsourcedCompoundB);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await ParseJsonAsync(response);

        AssertReducedShapeOnly(root);
        var pair = Assert.Single(root.GetProperty("pairs").EnumerateArray());
        Assert.Equal(UnsourcedCompoundA, pair.GetProperty("compoundA").GetString());
        Assert.Equal(UnsourcedCompoundB, pair.GetProperty("compoundB").GetString());
        Assert.Equal(JsonValueKind.Null, pair.GetProperty("severity").ValueKind);

        var raw = root.GetRawText();
        Assert.DoesNotContain(UnsourcedNotes, raw, StringComparison.Ordinal);
        Assert.DoesNotContain("evidence", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnsourcedHint_Observer_SeesNoReasoningOrEvidence()
    {
        await SignInAsync("hint-quarantine-observer@example.com");

        var response = await CheckInteractionsAsync(UnsourcedCompoundA, UnsourcedCompoundB);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await ParseJsonAsync(response);

        AssertReducedShapeOnly(root);
        var pair = Assert.Single(root.GetProperty("pairs").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, pair.GetProperty("severity").ValueKind);

        var raw = root.GetRawText();
        Assert.DoesNotContain(UnsourcedNotes, raw, StringComparison.Ordinal);
    }

    // --- AC5/Intent: unsourced hint still drives the internal score for an entitled caller -----

    [Fact]
    public async Task UnsourcedHint_EntitledCaller_StillDrivesScoreUnchanged()
    {
        await SignInAsync("hint-quarantine-entitled@example.com");
        await SetSubscriptionAsync(ProductTier.Operator);

        var response = await CheckInteractionsAsync(UnsourcedCompoundA, UnsourcedCompoundB);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await ParseJsonAsync(response);

        Assert.False(root.TryGetProperty("pairs", out _), "entitled caller must get the full shape, not the reduced one");
        var interaction = Assert.Single(root.GetProperty("interactions").EnumerateArray());
        Assert.Equal("Synergistic", interaction.GetProperty("type").GetString());
        Assert.Equal((double)UnsourcedStrength, interaction.GetProperty("confidence").GetDouble());
        Assert.Equal(UnsourcedNotes, interaction.GetProperty("reason").GetString());
        Assert.True(interaction.GetProperty("hintBacked").GetBoolean());
    }

    // --- AC6: a sourced hint is not blocked by the quarantine; the discriminator decides -------

    [Fact]
    public async Task SourcedHint_Anonymous_SameReducedShapeAsUnsourced_NotBlockedOrSpecialCased()
    {
        var response = await CheckInteractionsAsync(SourcedCompoundA, SourcedCompoundB);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await ParseJsonAsync(response);

        AssertReducedShapeOnly(root);
        var pair = Assert.Single(root.GetProperty("pairs").EnumerateArray());
        Assert.Equal(SourcedCompoundA, pair.GetProperty("compoundA").GetString());
        Assert.Equal(SourcedCompoundB, pair.GetProperty("compoundB").GetString());
    }

    [Fact]
    public async Task SourcedHint_EntitledCaller_FullReasoningShape_NotBlocked()
    {
        await SignInAsync("hint-quarantine-sourced-entitled@example.com");
        await SetSubscriptionAsync(ProductTier.Operator);

        var response = await CheckInteractionsAsync(SourcedCompoundA, SourcedCompoundB);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await ParseJsonAsync(response);

        var interaction = Assert.Single(root.GetProperty("interactions").EnumerateArray());
        Assert.Equal("Synergistic", interaction.GetProperty("type").GetString());
        Assert.Equal((double)SourcedStrength, interaction.GetProperty("confidence").GetDouble());
        Assert.Equal(SourcedNotes, interaction.GetProperty("reason").GetString());
        Assert.True(interaction.GetProperty("hintBacked").GetBoolean());
    }

    // --- Helpers --------------------------------------------------------------------------------

    private Task<HttpResponseMessage> CheckInteractionsAsync(string compoundA, string compoundB) =>
        _client.PostAsJsonAsync(
            "/api/v1/knowledge/interaction-check",
            new OverlapCheckRequest(new List<string> { compoundA, compoundB }));

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static void AssertReducedShapeOnly(JsonElement root)
    {
        var reasoningProperties = new[]
        {
            "reason", "message", "confidence", "sharedPathways", "topFindings",
            "interactions", "counterfactuals", "swaps", "compositeScore", "score",
        };
        foreach (var property in reasoningProperties)
        {
            Assert.False(root.TryGetProperty(property, out _), $"reduced shape must not carry '{property}'");
        }

        Assert.True(root.TryGetProperty("pairs", out _));
        Assert.Equal(new[] { "pairs" }, root.EnumerateObject().Select(property => property.Name).ToArray());
    }

    private async Task SignInAsync(string email)
    {
        await _client.PostAsJsonAsync(
            "/api/v1/auth/start",
            new StartAuthRequest(email, "email", "/profiles"));

        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");

        var consent = await _client.PostAsJsonAsync("/api/v1/consent", new { });
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
    }

    private async Task SetSubscriptionAsync(ProductTier tier)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var appUserId = await db.AppUsers.Select(u => u.Id).SingleAsync();

        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            AppUserId = appUserId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(30),
            Tier = tier,
            ProductCode = tier.ToString().ToLowerInvariant(),
            StripeCustomerId = "cus_hint_quarantine_test",
            StripeSubscriptionId = "sub_hint_quarantine_test",
            StripePriceId = "price_operator",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
