namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Text.Json;
using BioStack.Infrastructure.Knowledge;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "Integration")]
public class TrustLedgerEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"tl-test-{Guid.NewGuid():N}.db");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection",
                    $"Data Source={dbPath}");
                builder.UseSetting("Database:Provider", "sqlite");
                builder.UseSetting("Jwt:Secret",
                    "test-secret-key-at-least-32-chars-long!!");
                builder.UseSetting("Jwt:Issuer", "biostack");
                builder.UseSetting("Jwt:Audience", "biostack-ui");
            });

        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var knowledgeSource = scope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
        var seedSource = new LocalKnowledgeSource();
        var seedEntries = await seedSource.GetAllCompoundsAsync();
        foreach (var entry in seedEntries)
            await knowledgeSource.UpsertCompoundAsync(entry);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetTrustLedger_ForKnownCompound_Returns200WithExpectedShape()
    {
        // Use the first compound whose canonical name has no hyphens,
        // so that the slug round-trips cleanly through SlugToName()
        // (slug = name.ToLower(); SlugToName replaces hyphens with spaces)
        var seedSource = new LocalKnowledgeSource();
        var compounds = await seedSource.GetAllCompoundsAsync();
        var first = compounds.FirstOrDefault(c => !c.CanonicalName.Contains('-'));
        Assert.NotNull(first);

        var slug = first.CanonicalName.ToLowerInvariant().Replace(' ', '-');
        var response = await _client.GetAsync($"/api/v1/knowledge/compounds/{slug}/trust-ledger");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("slug", out _), "Response must have 'slug'");
        Assert.True(root.TryGetProperty("evidenceTier", out _), "Response must have 'evidenceTier'");
        Assert.True(root.TryGetProperty("status", out _), "Response must have 'status'");
    }

    [Fact]
    public async Task GetTrustLedger_ForUnknownCompound_Returns404()
    {
        var response = await _client.GetAsync(
            "/api/v1/knowledge/compounds/this-compound-does-not-exist-xyz/trust-ledger");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTrustLedger_ReviewGatedCompound_DoesNotExposeFullClaimsShape()
    {
        // Seed a compound with unknown evidence tier (triggers review-gated)
        await using var scope = _factory.Services.CreateAsyncScope();
        var knowledgeSource = scope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
        await knowledgeSource.UpsertCompoundAsync(new BioStack.Domain.Entities.KnowledgeEntry
        {
            CanonicalName = "TestReviewGatedCompound",
            EvidenceTier = BioStack.Domain.Enums.EvidenceTier.Unknown,
            RegulatoryStatus = "Unclassified",
            MechanismSummary = "SECRET INTERNAL NOTES",
            SourceReferences = [],
        });

        var response = await _client.GetAsync(
            "/api/v1/knowledge/compounds/testreviewgatedcompound/trust-ledger");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("review-gated",
            doc.RootElement.GetProperty("status").GetString());

        // Full TrustLedgerResponse shape (with claims) must NOT appear
        Assert.False(doc.RootElement.TryGetProperty("claims", out _),
            "Claims must not be exposed in review-gated response");
    }
}
