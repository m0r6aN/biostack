namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Domain.Entities.Graph;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Lane C: the graph-backed intelligence read endpoints disclose source (graph vs fallback) and
/// emit an <c>intelligence.graph-artifact.used</c> receipt — with the graph hash and compound
/// evidence refs — whenever a reviewed graph artifact informs a user-facing response.
/// </summary>
[Trait("Category", "Integration")]
public class IntelligenceEndpointsIntegrationTests : IAsyncLifetime
{
    private static readonly Guid TestUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string GraphHash = "sha256:integration-graph";

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private sealed class FixedCurrentUserAccessor : ICurrentUserAccessor
    {
        public Guid GetCurrentUserId() => TestUserId;
    }

    public async Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"intel-test-{Guid.NewGuid():N}.db");

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

        // Seed a reviewed graph so the compatibility endpoint is graph-backed.
        await using var scope = _factory.Services.CreateAsyncScope();
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
                new CompoundGraphRelationship
                {
                    SubjectCompound = "Creatine",
                    SubjectSlug = "creatine",
                    ObjectCompound = "Beta-Alanine",
                    ObjectSlug = "beta-alanine",
                    RelationshipType = GraphRelationshipType.SynergizesWith,
                    Directionality = GraphRelationshipType.Bidirectional,
                    Confidence = "high",
                    EvidenceTier = "Strong",
                    SourceRefsJson = "[\"source:pubmed-1\"]",
                    Reason = "Reviewed synergy.",
                    SafetyConcernLevel = GraphRelationshipType.SafetyConcern.None,
                    ReviewState = "reviewed",
                },
            },
            Array.Empty<CompoundGraphFinding>());
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Compatibility_ReturnsGraphSource_WhenGraphBacked()
    {
        var response = await _client.GetAsync(
            "/api/v1/intelligence/compatibility?compounds=Creatine&compounds=Beta-Alanine");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("graph", root.GetProperty("source").GetString());
        Assert.Equal(GraphHash, root.GetProperty("graphArtifactHash").GetString());

        var rel = Assert.Single(root.GetProperty("relationships").EnumerateArray());
        Assert.Equal("synergizes_with", rel.GetProperty("relationshipType").GetString());
        Assert.Equal("graph", rel.GetProperty("source").GetString());
    }

    [Fact]
    public async Task Compatibility_EmitsGraphArtifactUsedReceipt_WithEvidenceRefs()
    {
        var response = await _client.GetAsync(
            "/api/v1/intelligence/compatibility?compounds=Creatine&compounds=Beta-Alanine");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        var entries = await spine.GetByActorAsync($"user:{TestUserId}");

        var receipt = Assert.Single(entries, e => e.ReceiptClass == "intelligence.graph-artifact.used");
        Assert.NotEqual("biostack-system", receipt.ActorId);

        var refs = JsonSerializer.Deserialize<List<string>>(receipt.EvidenceRefsJson)!;
        Assert.Contains($"compound-graph:{GraphHash}", refs);
        Assert.Contains("compound:creatine", refs);
        Assert.Contains("compound:beta-alanine", refs);
        Assert.Contains("source:pubmed-1", refs);
    }

    [Fact]
    public async Task CompoundRelationships_ReturnsGraphBackedEdges()
    {
        var response = await _client.GetAsync(
            "/api/v1/intelligence/compounds/Creatine/relationships");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("graph", root.GetProperty("source").GetString());
        Assert.Equal(GraphHash, root.GetProperty("graphArtifactHash").GetString());
        Assert.NotEmpty(root.GetProperty("relationships").EnumerateArray());
    }
}
