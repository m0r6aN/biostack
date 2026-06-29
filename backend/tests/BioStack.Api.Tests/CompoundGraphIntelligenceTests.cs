namespace BioStack.Api.Tests;

using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Application.Services.Intelligence;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Entities.Graph;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Lane C: proves the materialized compound graph round-trips with provenance, that runtime
/// interaction intelligence prefers graph-backed relationships over KnowledgeEntry string inference,
/// and that the read service discloses graph vs fallback source.
/// </summary>
public sealed class CompoundGraphIntelligenceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public CompoundGraphIntelligenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private BioStackDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite(_connection).Options);

    // ── Persistence round-trip + provenance ──────────────────────────────────

    [Fact]
    public async Task PublishAsync_PersistsAndReadsBack_PreservingProvenance()
    {
        var hash = "sha256:abc123";
        await PublishGraphAsync(hash, ("Creatine", "Beta-Alanine", GraphRelationshipType.SynergizesWith, "high", "Strong"));

        await using var context = CreateDbContext();
        var store = new CompoundGraphStore(context);

        var active = await store.GetActiveArtifactAsync();
        Assert.NotNull(active);
        Assert.Equal(hash, active!.ArtifactHash);
        Assert.Equal("1.0.0", active.BuilderVersion);
        Assert.Equal(1, active.RelationshipCount);

        var edge = await store.FindRelationshipAsync("Creatine", "Beta-Alanine");
        Assert.NotNull(edge);
        Assert.Equal(GraphRelationshipType.SynergizesWith, edge!.RelationshipType);
        Assert.Equal("high", edge.Confidence);
        Assert.Equal("Strong", edge.EvidenceTier);
        Assert.Equal(active.Id, edge.GraphArtifactId);

        var refs = JsonSerializer.Deserialize<List<string>>(edge.SourceRefsJson);
        Assert.Contains("source:pubmed-1", refs!);
    }

    [Fact]
    public async Task FindRelationshipAsync_IsOrderIndependent()
    {
        await PublishGraphAsync("sha256:order", ("Creatine", "Beta-Alanine", GraphRelationshipType.SynergizesWith, "high", "Strong"));

        await using var context = CreateDbContext();
        var store = new CompoundGraphStore(context);

        var forward = await store.FindRelationshipAsync("Creatine", "Beta-Alanine");
        var reverse = await store.FindRelationshipAsync("beta-alanine", "creatine");

        Assert.NotNull(forward);
        Assert.NotNull(reverse);
        Assert.Equal(forward!.Id, reverse!.Id);
    }

    [Fact]
    public async Task PublishAsync_DeactivatesPriorArtifact()
    {
        await PublishGraphAsync("sha256:v1", ("Creatine", "Beta-Alanine", GraphRelationshipType.SynergizesWith, "high", "Strong"));
        await PublishGraphAsync("sha256:v2", ("Creatine", "Beta-Alanine", GraphRelationshipType.ConflictsWith, "moderate", "Anecdotal"));

        await using var context = CreateDbContext();
        var activeCount = await context.CompoundGraphArtifacts.CountAsync(a => a.IsActive);
        Assert.Equal(1, activeCount);

        var store = new CompoundGraphStore(context);
        var active = await store.GetActiveArtifactAsync();
        Assert.Equal("sha256:v2", active!.ArtifactHash);
    }

    // ── Read service discloses source ────────────────────────────────────────

    [Fact]
    public async Task GraphIntelligenceService_CompatibilityIsGraphBacked_WhenEdgeExists()
    {
        await PublishGraphAsync("sha256:svc", ("Creatine", "Beta-Alanine", GraphRelationshipType.SynergizesWith, "high", "Strong"));

        await using var context = CreateDbContext();
        var service = new GraphIntelligenceService(new CompoundGraphStore(context));

        var result = await service.GetCompatibilityAsync(new[] { "Creatine", "Beta-Alanine" });

        Assert.Equal(IntelligenceSource.Graph, result.Source);
        Assert.Equal("sha256:svc", result.GraphArtifactHash);
        var rel = Assert.Single(result.Relationships);
        Assert.Equal(GraphRelationshipType.SynergizesWith, rel.RelationshipType);
        Assert.Equal(IntelligenceSource.Graph, rel.Source);
    }

    [Fact]
    public async Task GraphIntelligenceService_DisclosesFallback_WhenNoEdge()
    {
        await PublishGraphAsync("sha256:none", ("Creatine", "Beta-Alanine", GraphRelationshipType.SynergizesWith, "high", "Strong"));

        await using var context = CreateDbContext();
        var service = new GraphIntelligenceService(new CompoundGraphStore(context));

        // A pair with no reviewed edge.
        var result = await service.GetCompatibilityAsync(new[] { "Creatine", "Tongkat Ali" });

        var rel = Assert.Single(result.Relationships);
        Assert.Equal(IntelligenceSource.Fallback, rel.Source);
        Assert.Equal(GraphRelationshipType.UnknownOrInsufficientEvidence, rel.RelationshipType);
        Assert.Equal(IntelligenceSource.Fallback, result.Source);
    }

    [Fact]
    public async Task GraphIntelligenceService_IsFallback_WhenNoGraphPublished()
    {
        await using var context = CreateDbContext();
        var service = new GraphIntelligenceService(new CompoundGraphStore(context));

        var result = await service.GetCompatibilityAsync(new[] { "Creatine", "Beta-Alanine" });

        Assert.Equal(IntelligenceSource.Fallback, result.Source);
        Assert.Null(result.GraphArtifactHash);
    }

    // ── Runtime interaction intelligence prefers graph over string inference ──

    [Fact]
    public async Task InteractionIntelligence_PrefersGraph_OverStringInference()
    {
        await PublishGraphAsync("sha256:runtime", ("Creatine", "Beta-Alanine", GraphRelationshipType.SynergizesWith, "high", "Strong"));

        await using var context = CreateDbContext();
        var service = BuildInteractionService(context);

        // Entries share no pathway/hint, so without the graph the pair would be Neutral.
        var entries = new List<KnowledgeEntry>
        {
            NewEntry("Creatine"),
            NewEntry("Beta-Alanine"),
        };

        var response = await service.EvaluateAsync(entries);

        Assert.Equal(IntelligenceSource.Graph, response.Source);
        Assert.Equal("sha256:runtime", response.GraphArtifactHash);

        var interaction = Assert.Single(response.Interactions);
        Assert.Equal(InteractionType.Synergistic, interaction.Type);
        Assert.Equal(IntelligenceSource.Graph, interaction.Source);
        Assert.Equal("sha256:runtime", interaction.GraphArtifactHash);
        Assert.False(interaction.HintBacked);
    }

    [Fact]
    public async Task InteractionIntelligence_FallsBack_WhenNoGraphRelationship()
    {
        await using var context = CreateDbContext();
        var service = BuildInteractionService(context);

        var entries = new List<KnowledgeEntry>
        {
            NewEntry("Creatine", pathways: new[] { "atp-regeneration" }),
            NewEntry("Beta-Alanine", pathways: new[] { "atp-regeneration" }),
        };

        var response = await service.EvaluateAsync(entries);

        Assert.Equal(IntelligenceSource.Fallback, response.Source);
        Assert.Null(response.GraphArtifactHash);
        var interaction = Assert.Single(response.Interactions);
        Assert.Equal(IntelligenceSource.Fallback, interaction.Source);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private InteractionIntelligenceService BuildInteractionService(BioStackDbContext context)
        => new(
            new DatabaseKnowledgeSource(context),
            new CompoundInteractionHintRepository(context),
            new CompoundGraphStore(context));

    private static KnowledgeEntry NewEntry(string name, IEnumerable<string>? pathways = null) => new()
    {
        Id = Guid.NewGuid(),
        CanonicalName = name,
        Pathways = (pathways ?? Array.Empty<string>()).ToList(),
    };

    private async Task PublishGraphAsync(
        string hash,
        params (string Subject, string Object, string Type, string Confidence, string Tier)[] relationships)
    {
        await using var context = CreateDbContext();
        var store = new CompoundGraphStore(context);

        var artifact = new CompoundGraphArtifact
        {
            ArtifactHash = hash,
            BuilderVersion = "1.0.0",
            GeneratedAtUtc = DateTime.UtcNow,
            ReviewState = "provisional",
        };

        var rels = relationships.Select(r => new CompoundGraphRelationship
        {
            SubjectCompound = r.Subject,
            SubjectSlug = CompoundSlug.From(r.Subject),
            ObjectCompound = r.Object,
            ObjectSlug = CompoundSlug.From(r.Object),
            RelationshipType = r.Type,
            Directionality = GraphRelationshipType.Bidirectional,
            Confidence = r.Confidence,
            EvidenceTier = r.Tier,
            SourceRefsJson = JsonSerializer.Serialize(new[] { "source:pubmed-1" }),
            Reason = "test relationship",
            SafetyConcernLevel = GraphRelationshipType.SafetyConcern.None,
            ReviewState = "reviewed",
        }).ToList();

        var findings = new List<CompoundGraphFinding>
        {
            new()
            {
                FindingType = "SharedPathwayAdditiveRisk",
                Severity = "Moderate",
                SubjectCompound = relationships.Length > 0 ? relationships[0].Subject : null,
                ObjectCompound = relationships.Length > 0 ? relationships[0].Object : null,
                Reason = "test finding",
                EvidenceRefsJson = "[]",
            },
        };

        await store.PublishAsync(artifact, rels, findings);
    }
}
