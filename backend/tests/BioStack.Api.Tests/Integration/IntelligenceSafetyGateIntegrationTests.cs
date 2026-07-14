namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Entities.Graph;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Lane H: the user-facing intelligence endpoints route every response through the safety gate.
/// Normal output is allowed, high-risk pairs are warned (warning-first framing + safety receipt),
/// and doctrine-violating reason text is constrained (rewritten + safety receipt) — none of it can
/// bypass the gate, and the graph/compound evidence refs survive on the safety receipt.
/// </summary>
[Trait("Category", "Integration")]
public class IntelligenceSafetyGateIntegrationTests : IAsyncLifetime
{
    private static readonly Guid TestUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private const string GraphHash = "sha256:safety-graph";

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private sealed class FixedCurrentUserAccessor : ICurrentUserAccessor
    {
        public Guid GetCurrentUserId() => TestUserId;
    }

    public async Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"intel-safety-{Guid.NewGuid():N}.db");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
                builder.UseSetting("Database:Provider", "sqlite");
                builder.UseSetting("Jwt:Secret", "test-secret-key-at-least-32-chars-long!!");
                builder.UseSetting("Jwt:Issuer", "biostack");
                builder.UseSetting("Jwt:Audience", "biostack-ui");

                builder.ConfigureServices(services =>
                {
                    services.AddAuthorization(opts =>
                    {
                        opts.DefaultPolicy = new AuthorizationPolicyBuilder()
                            .RequireAssertion(_ => true)
                            .Build();
                    });
                    services.AddScoped<ICurrentUserAccessor, FixedCurrentUserAccessor>();
                });
            });

        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        db.AppUsers.Add(new AppUser
        {
            Id = TestUserId,
            Provider = "email",
            ProviderKey = "intelligence-safety@example.com",
            Email = "intelligence-safety@example.com",
            DisplayName = "Intelligence Safety User",
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow,
        });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            AppUserId = TestUserId,
            Tier = ProductTier.Operator,
            ProductCode = "operator",
            Status = SubscriptionStatus.Active,
            CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(30),
            StripeCustomerId = "cus_intelligence_safety",
            StripeSubscriptionId = "sub_intelligence_safety",
            StripePriceId = "price_operator",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var store = scope.ServiceProvider.GetRequiredService<ICompoundGraphStore>();
        await store.PublishAsync(
            new CompoundGraphArtifact
            {
                ArtifactHash = GraphHash,
                BuilderVersion = "1.0.0",
                GeneratedAtUtc = DateTime.UtcNow,
                ReviewState = "provisional",
            },
            new[]
            {
                Rel("Creatine", "creatine", "Beta-Alanine", "beta-alanine",
                    GraphRelationshipType.SynergizesWith, "Reviewed synergy."),
                // High-risk pair (both SARMs/research compounds) → warning-first.
                Rel("Ostarine", "ostarine", "Cardarine", "cardarine",
                    GraphRelationshipType.SynergizesWith, "Some users report a strength signal."),
                // Doctrine-violating reason text → constrained.
                Rel("Magnesium", "magnesium", "Zinc", "zinc",
                    GraphRelationshipType.ConflictsWith, "You should take zinc separately from magnesium."),
            },
            Array.Empty<CompoundGraphFinding>());
    }

    private static CompoundGraphRelationship Rel(
        string subj, string subjSlug, string obj, string objSlug, string type, string reason)
        => new()
        {
            SubjectCompound = subj,
            SubjectSlug = subjSlug,
            ObjectCompound = obj,
            ObjectSlug = objSlug,
            RelationshipType = type,
            Directionality = GraphRelationshipType.Bidirectional,
            Confidence = "moderate",
            EvidenceTier = "Limited",
            SourceRefsJson = "[\"source:pubmed-1\"]",
            Reason = reason,
            SafetyConcernLevel = GraphRelationshipType.SafetyConcern.None,
            ReviewState = "reviewed",
        };

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<JsonElement> GetCompatibilityAsync(string a, string b)
    {
        var response = await _client.GetAsync($"/api/v1/intelligence/compatibility?compounds={a}&compounds={b}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private async Task<List<string>> SafetyReceiptClassesAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        var entries = await spine.GetByActorAsync($"user:{TestUserId}");
        return entries.Select(e => e.ReceiptClass).ToList();
    }

    [Fact]
    public async Task NormalPair_IsAllowed_AndEmitsNoSafetyReceipt()
    {
        var root = await GetCompatibilityAsync("Creatine", "Beta-Alanine");

        Assert.Equal("allowed", root.GetProperty("safetyStatus").GetString());
        var warnings = root.GetProperty("warnings");
        Assert.True(warnings.ValueKind is JsonValueKind.Null || warnings.GetArrayLength() == 0);

        var classes = await SafetyReceiptClassesAsync();
        Assert.DoesNotContain("safety.warning.surfaced", classes);
        Assert.DoesNotContain("safety.gate.triggered", classes);
        // The graph-artifact-used receipt is still emitted for the graph-backed answer.
        Assert.Contains("intelligence.graph-artifact.used", classes);
    }

    [Fact]
    public async Task HighRiskPair_IsWarned_WithFraming_AndWarningReceiptPreservingEvidenceRefs()
    {
        var root = await GetCompatibilityAsync("Ostarine", "Cardarine");

        Assert.Equal("warning", root.GetProperty("safetyStatus").GetString());
        Assert.NotEqual(0, root.GetProperty("warnings").GetArrayLength());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("safetyReceiptId").GetString()));

        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        var entries = await spine.GetByActorAsync($"user:{TestUserId}");
        var receipt = Assert.Single(entries, e => e.ReceiptClass == "safety.warning.surfaced");

        var refs = JsonSerializer.Deserialize<List<string>>(receipt.EvidenceRefsJson)!;
        Assert.Contains($"compound-graph:{GraphHash}", refs);
        Assert.Contains("compound:ostarine", refs);
        Assert.Contains("compound:cardarine", refs);
        Assert.Contains(refs, r => r.StartsWith("policy:"));
        Assert.Contains(refs, r => r.StartsWith("safety-gate:"));
    }

    [Fact]
    public async Task DoctrineViolatingReason_IsConstrained_AndEmitsGateTriggeredReceipt()
    {
        var root = await GetCompatibilityAsync("Magnesium", "Zinc");

        Assert.Equal("constrained", root.GetProperty("safetyStatus").GetString());

        var rel = Assert.Single(root.GetProperty("relationships").EnumerateArray());
        var reason = rel.GetProperty("reason").GetString();
        Assert.DoesNotContain("you should", reason, StringComparison.OrdinalIgnoreCase);

        var classes = await SafetyReceiptClassesAsync();
        Assert.Contains("safety.gate.triggered", classes);
    }
}
