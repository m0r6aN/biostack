namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.Domain.Entities.Graph;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using BioStack.KnowledgeWorker.Pipeline.Graph;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Lane C: the worker-side mapper projects a built <see cref="CompoundGraph"/> into the persisted
/// graph entities, and the persistence method (<c>ICompoundGraphStore.PublishAsync</c>) round-trips
/// the materialized graph so a DB-connected worker run can publish it for runtime to read.
/// </summary>
public class CompoundGraphPersistenceMapperTests
{
    [Fact]
    public void Map_ProjectsCompoundEdges_IntoCanonRelationships()
    {
        var graph = BuildGraph();

        var payload = CompoundGraphPersistenceMapper.Map(graph, sourceManifestHash: "manifest-1");

        Assert.Equal("1.0.0", payload.Artifact.BuilderVersion);
        Assert.Equal("manifest-1", payload.Artifact.SourceManifestHash);
        Assert.StartsWith("sha256:", payload.Artifact.ArtifactHash);

        var rel = Assert.Single(payload.Relationships);
        Assert.Equal("creatine", rel.SubjectSlug);
        Assert.Equal("beta-alanine", rel.ObjectSlug);
        Assert.Equal("Creatine", rel.SubjectCompound);
        Assert.Equal(GraphRelationshipType.SynergizesWith, rel.RelationshipType);
        Assert.Equal("Strong", rel.EvidenceTier);
        Assert.Equal("high", rel.Confidence);
        var refs = JsonSerializer.Deserialize<List<string>>(rel.SourceRefsJson)!;
        Assert.Contains("src-1", refs);

        var finding = Assert.Single(payload.Findings);
        Assert.Equal("SharedPathwayAdditiveRisk", finding.FindingType);
        Assert.Equal("Creatine", finding.SubjectCompound);
    }

    [Fact]
    public void Map_IsDeterministic_ForSameGraph()
    {
        var a = CompoundGraphPersistenceMapper.Map(BuildGraph());
        var b = CompoundGraphPersistenceMapper.Map(BuildGraph());

        Assert.Equal(a.Artifact.ArtifactHash, b.Artifact.ArtifactHash);
    }

    [Fact]
    public void Map_SkipsNonCompoundEdges()
    {
        var graph = BuildGraph();
        // Category/structural edges must not become pairwise relationships.
        Assert.DoesNotContain(
            CompoundGraphPersistenceMapper.Map(graph).Relationships,
            r => r.RelationshipType == "belongs_to_category");
    }

    [Fact]
    public async Task PublishAsync_RoundTripsMappedGraph()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite(connection).Options;

        await using (var seed = new BioStackDbContext(options))
        {
            seed.Database.EnsureCreated();
            var payload = CompoundGraphPersistenceMapper.Map(BuildGraph());
            var store = new CompoundGraphStore(seed);
            await store.PublishAsync(payload.Artifact, payload.Relationships, payload.Findings);
        }

        await using var read = new BioStackDbContext(options);
        var readStore = new CompoundGraphStore(read);
        var active = await readStore.GetActiveArtifactAsync();
        Assert.NotNull(active);
        Assert.Equal(1, active!.RelationshipCount);

        var edge = await readStore.FindRelationshipAsync("Creatine", "Beta-Alanine");
        Assert.NotNull(edge);
        Assert.Equal(GraphRelationshipType.SynergizesWith, edge!.RelationshipType);
    }

    private static CompoundGraph BuildGraph()
    {
        var nodes = new List<CompoundGraphNode>
        {
            new("compound:creatine", CompoundGraphNodeType.Compound, "Creatine",
                Array.Empty<string>(), new Dictionary<string, JsonNode?>()),
            new("compound:beta-alanine", CompoundGraphNodeType.Compound, "Beta-Alanine",
                Array.Empty<string>(), new Dictionary<string, JsonNode?>()),
            new("category:performance", CompoundGraphNodeType.Category, "performance",
                Array.Empty<string>(), new Dictionary<string, JsonNode?>()),
        };

        var edges = new List<CompoundGraphEdge>
        {
            new(
                EdgeId: "relationship:creatine:beta-alanine:synergy",
                From: "compound:creatine",
                To: "compound:beta-alanine",
                EdgeType: CompoundGraphEdgeType.SynergizesWith,
                RelationshipType: "synergy",
                AssertedRelationshipType: "synergy",
                EffectDomain: null,
                EvidenceTier: "Strong",
                Confidence: "high",
                SourceRefs: new[] { "src-1" },
                ClaimRefs: Array.Empty<string>(),
                ReviewFlags: Array.Empty<string>(),
                NeedsReview: false,
                CommunitySignal: null,
                SourceAuthorityMix: SourceAuthorityMix.Empty),
            // Structural edge (compound → category) that must be skipped.
            new(
                EdgeId: "belongs-to-category:creatine:performance",
                From: "compound:creatine",
                To: "category:performance",
                EdgeType: CompoundGraphEdgeType.BelongsToCategory,
                RelationshipType: null,
                AssertedRelationshipType: null,
                EffectDomain: null,
                EvidenceTier: null,
                Confidence: null,
                SourceRefs: Array.Empty<string>(),
                ClaimRefs: Array.Empty<string>(),
                ReviewFlags: Array.Empty<string>(),
                NeedsReview: false,
                CommunitySignal: null,
                SourceAuthorityMix: SourceAuthorityMix.Empty),
        };

        var findings = new List<CompoundGraphReviewFinding>
        {
            new(
                FindingId: "finding:shared-pathway-additive-risk:beta-alanine+creatine:atp",
                FindingType: CompoundGraphFindingType.SharedPathwayAdditiveRisk,
                Severity: CompoundGraphFindingSeverity.Moderate,
                CompoundRefs: new[] { "compound:creatine", "compound:beta-alanine" },
                EdgeRefs: new[] { "relationship:creatine:beta-alanine:synergy" },
                Summary: "Shared pathway additive risk.",
                RecommendedAction: "human-review-additive-risk",
                NeedsHumanReview: true),
        };

        var counts = new CompoundGraphCounts(
            Nodes: nodes.Count, Edges: edges.Count,
            ReviewRequiredEdges: 0, CommunitySignalEdges: 0, ConflictEdges: 0);

        return new CompoundGraph("1.0.0", DateTimeOffset.UtcNow, counts, nodes, edges, findings);
    }
}
