namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Contracts.Requests;
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
using AppSubscription = BioStack.Domain.Entities.Subscription;

[Trait("Category", "Integration")]
public sealed class ProtocolIntelligenceEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private Guid _userId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-pi-{Guid.NewGuid():N}.db");
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
        _userId = await SignInAsync("protocol-intelligence@example.com");
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
    public async Task ContractsEndpoint_ExposesIdsButNotRestrictedSourceText()
    {
        var response = await _client.GetAsync("/api/v1/knowledge/protocol-intelligence/contracts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Contains("substance_affects_pathway", json);
        Assert.Contains("sourcing_guidance", json);
        Assert.DoesNotContain("Source research:", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PubMed/PMC", json, StringComparison.OrdinalIgnoreCase);
        Assert.True(doc.RootElement.GetProperty("supportedRelationshipIds").GetArrayLength() > 0);
        Assert.True(doc.RootElement.GetProperty("blockedOutputIds").GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetProtocolIntelligence_ReturnsUnknownWhenNoReviewedArtifactExists()
    {
        var response = await _client.GetAsync($"/api/v1/protocols/{Guid.NewGuid()}/intelligence");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Unknown", doc.RootElement.GetProperty("status").GetString());
        Assert.True(doc.RootElement.GetProperty("unknowns").GetArrayLength() > 0);
    }

    [Fact]
    public async Task ObserverPreview_ReceivesUpgradeHooksAndNoCommanderOnlyAmbiguityPanel()
    {
        var response = await PreviewAsync(ProductTier.Observer, ReviewedRelationship(), ReviewedAmbiguity());

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, doc.RootElement.GetProperty("relationships").GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("ambiguitySignals").GetArrayLength());
        Assert.Contains("Operator", doc.RootElement.GetProperty("upgradeHooks").ToString());
        Assert.Contains("Commander", doc.RootElement.GetProperty("upgradeHooks").ToString());
    }

    [Fact]
    public async Task OperatorPreview_ReceivesReviewedRelationshipAndSourceQualityButNoCommanderAmbiguity()
    {
        var response = await PreviewAsync(ProductTier.Operator, ReviewedRelationship(), ReviewedSourceQuality(), ReviewedAmbiguity());

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, doc.RootElement.GetProperty("relationships").GetArrayLength());
        Assert.Equal(1, doc.RootElement.GetProperty("sourceQualityWarnings").GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("ambiguitySignals").GetArrayLength());
        Assert.Contains("Commander", doc.RootElement.GetProperty("upgradeHooks").ToString());
    }

    [Fact]
    public async Task CommanderPreview_ReceivesAmbiguityAndLongitudinalUpgradeIsIncludedByTier()
    {
        var response = await PreviewAsync(ProductTier.Commander, ReviewedRelationship(), ReviewedAmbiguity(), ReviewedHighRisk());

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, doc.RootElement.GetProperty("relationships").GetArrayLength());
        Assert.Equal(1, doc.RootElement.GetProperty("ambiguitySignals").GetArrayLength());
        Assert.Equal(1, doc.RootElement.GetProperty("highRiskWarnings").GetArrayLength());
    }

    [Fact]
    public async Task Preview_DoesNotExposeUnreviewedArtifactsOrForbiddenPhrases()
    {
        var pending = ReviewedRelationship();
        pending["reviewStatus"] = "pending";
        var forbidden = ReviewedRelationship();
        forbidden["userFacingExplanation"] = "You should start this because it is safe and effective.";

        var response = await PreviewAsync(ProductTier.Commander, pending, forbidden);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Unknown", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(0, doc.RootElement.GetProperty("relationships").GetArrayLength());
        Assert.DoesNotContain("You should start", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("safe and effective", json, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<HttpResponseMessage> PreviewAsync(ProductTier tier, params Dictionary<string, object?>[] artifacts)
    {
        await SetTierAsync(tier);
        var payload = new
        {
            reviewedArtifacts = artifacts.Select(artifact => new
            {
                artifactType = artifact["artifactType"],
                artifact = artifact.Where(pair => pair.Key != "artifactType").ToDictionary(pair => pair.Key, pair => pair.Value)
            }).ToArray()
        };

        return await _client.PostAsJsonAsync($"/api/v1/protocols/{Guid.NewGuid()}/intelligence/preview", payload, JsonOptions);
    }

    private static Dictionary<string, object?> ReviewedRelationship() => new()
    {
        ["artifactType"] = "relationship_artifact",
        ["relationshipType"] = "substance_affects_pathway",
        ["subject"] = "Semaglutide",
        ["object"] = "GI symptom tracking",
        ["phaseContext"] = "active",
        ["goalContext"] = "observation",
        ["evidenceTier"] = "clinical_study",
        ["confidence"] = "moderate",
        ["sourceRefs"] = new[] { "pmid:123" },
        ["sourceAuthorityMix"] = "official_and_literature",
        ["safetyConcernLevel"] = "medium",
        ["productHandling"] = "label_specific",
        ["reviewStatus"] = "approved",
        ["userFacingExplanation"] = "Reviewed relationship only.",
        ["userFacingBoundary"] = "Observation prompt only."
    };

    private static Dictionary<string, object?> ReviewedAmbiguity() => new()
    {
        ["artifactType"] = "side_effect_ambiguity_artifact",
        ["symptomOrOutcome"] = "nausea reported",
        ["onsetWindow"] = "recent check-ins",
        ["recentChanges"] = new[] { "phase changed" },
        ["phaseContext"] = "active",
        ["overlapDomains"] = new[] { "gi_symptoms" },
        ["sourceQualityFlags"] = Array.Empty<string>(),
        ["highRiskCategoryFlags"] = Array.Empty<string>(),
        ["evidenceTier"] = "observational",
        ["confidence"] = "low",
        ["userFacingBoundary"] = "Observation prompt only.",
        ["reviewStatus"] = "approved"
    };

    private static Dictionary<string, object?> ReviewedSourceQuality() => new()
    {
        ["artifactType"] = "source_quality_artifact",
        ["subject"] = "Research chemical",
        ["sourceClass"] = "gray_market",
        ["authorityRefs"] = new[] { "source-registry:wada" },
        ["identityConfidence"] = "low",
        ["purityConfidence"] = "low",
        ["labelConfidence"] = "low",
        ["warningFirst"] = true,
        ["blockedOutputs"] = new[] { "sourcing_guidance" },
        ["reviewStatus"] = "approved",
        ["userFacingBoundary"] = "Source-quality warning only."
    };

    private static Dictionary<string, object?> ReviewedHighRisk() => new()
    {
        ["artifactType"] = "high_risk_warning_artifact",
        ["category"] = "investigational_peptides",
        ["requiredWarnings"] = new[] { "regulatory_status", "source_quality_uncertainty" },
        ["sourceRefs"] = new[] { "source-registry:clinicaltrials" },
        ["safetyConcernLevel"] = "high",
        ["productHandling"] = "warning_first",
        ["blockedOutputs"] = new[] { "claims_investigational_peptides_safe_or_effective" },
        ["evidenceTier"] = "warning_first",
        ["confidence"] = "reviewed",
        ["reviewStatus"] = "approved",
        ["userFacingBoundary"] = "Warning-first context only."
    };

    private async Task SetTierAsync(ProductTier tier)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        db.Subscriptions.RemoveRange(db.Subscriptions.Where(subscription => subscription.AppUserId == _userId));
        if (tier > ProductTier.Observer)
        {
            db.Subscriptions.Add(new AppSubscription
            {
                Id = Guid.NewGuid(),
                AppUserId = _userId,
                ProductCode = tier == ProductTier.Commander ? "commander" : "operator",
                Tier = tier,
                StripeCustomerId = "cus_protocol_intelligence",
                StripeSubscriptionId = $"sub_{tier}",
                StripePriceId = $"price_{tier}",
                Status = SubscriptionStatus.Active,
                CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1),
                CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(30),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
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
}
