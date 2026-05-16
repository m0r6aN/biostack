namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline.Graph;
using Xunit;

public class RelationshipPacketAuthorizerTests
{
    private static JsonNode BuildRegistry(params (string id, string tier)[] sources)
    {
        var arr = new JsonArray();
        foreach (var (id, tier) in sources)
        {
            arr.Add(new JsonObject
            {
                ["sourceId"] = id,
                ["authorityTier"] = tier,
            });
        }
        return new JsonObject { ["sources"] = arr };
    }

    private static JsonArray BuildPacketSources(params (string id, string tier)[] sources)
    {
        var arr = new JsonArray();
        foreach (var (id, tier) in sources)
        {
            arr.Add(new JsonObject
            {
                ["sourceId"] = id,
                ["authorityTier"] = tier,
            });
        }
        return arr;
    }

    private static CompoundGraphEdge ProvisionalEdge(
        string relationshipType,
        IReadOnlyList<string> sourceRefs,
        string? evidenceTier = "Limited",
        string? confidence = "moderate")
    {
        return new CompoundGraphEdge(
            EdgeId: $"relationship:a:b:{relationshipType}",
            From: "compound:a",
            To: "compound:b",
            EdgeType: CompoundGraphEdgeType.SynergizesWith,
            RelationshipType: relationshipType,
            AssertedRelationshipType: relationshipType,
            EffectDomain: "cognitive",
            EvidenceTier: evidenceTier,
            Confidence: confidence,
            SourceRefs: sourceRefs,
            ClaimRefs: Array.Empty<string>(),
            ReviewFlags: Array.Empty<string>(),
            NeedsReview: false,
            CommunitySignal: null,
            SourceAuthorityMix: SourceAuthorityMix.Empty);
    }

    [Fact]
    public void ComputeAuthorityMix_Resolves_Sources_Via_Packet_Sources_First()
    {
        var auth = new RelationshipPacketAuthorizer();
        var packetSources = BuildPacketSources(("src-a", "A"), ("src-d", "D"));
        var registry = BuildRegistry(("src-a", "B")); // registry disagrees, packet wins for present id

        var mix = auth.ComputeAuthorityMix(
            new[] { "src-a", "src-d" },
            packetSources,
            registry,
            out var unmapped);

        Assert.Empty(unmapped);
        Assert.Contains("A", mix.AuthorityTiers);
        Assert.Contains("D", mix.AuthorityTiers);
        Assert.DoesNotContain("B", mix.AuthorityTiers);
    }

    [Fact]
    public void ComputeAuthorityMix_Falls_Through_To_Registry_For_Missing_Packet_Entries()
    {
        var auth = new RelationshipPacketAuthorizer();
        var packetSources = BuildPacketSources(("src-a", "A"));
        var registry = BuildRegistry(("src-c", "C1"));

        var mix = auth.ComputeAuthorityMix(
            new[] { "src-a", "src-c" },
            packetSources,
            registry,
            out var unmapped);

        Assert.Empty(unmapped);
        Assert.Contains("A", mix.AuthorityTiers);
        Assert.Contains("C1", mix.AuthorityTiers);
    }

    [Fact]
    public void ComputeAuthorityMix_Flags_Unmapped_Sources()
    {
        var auth = new RelationshipPacketAuthorizer();
        var registry = BuildRegistry(("src-known", "B"));

        var mix = auth.ComputeAuthorityMix(
            new[] { "src-known", "src-mystery", "src-ghost" },
            packetSources: null,
            registry,
            out var unmapped);

        Assert.Equal(new[] { "B" }, mix.AuthorityTiers);
        Assert.Equal(2, unmapped.Count);
        Assert.Contains("src-mystery", unmapped);
        Assert.Contains("src-ghost", unmapped);
    }

    [Fact]
    public void IsLowAuthorityOnly_Is_True_For_D_Only_And_X_Only_Mixes()
    {
        Assert.True(new SourceAuthorityMix(new[] { "D" }).IsLowAuthorityOnly);
        Assert.True(new SourceAuthorityMix(new[] { "X" }).IsLowAuthorityOnly);
        Assert.True(new SourceAuthorityMix(new[] { "D", "X" }).IsLowAuthorityOnly);
        Assert.False(new SourceAuthorityMix(new[] { "D", "B" }).IsLowAuthorityOnly);
        Assert.False(new SourceAuthorityMix(new[] { "A1" }).IsLowAuthorityOnly);
        Assert.False(SourceAuthorityMix.Empty.IsLowAuthorityOnly);
    }

    [Fact]
    public void EnforcePolicy_Quarantines_D_Only_Non_Community_Relationship()
    {
        var auth = new RelationshipPacketAuthorizer();
        var edge = ProvisionalEdge("synergy", new[] { "src-d" }, evidenceTier: "Limited", confidence: "moderate");
        var computed = new SourceAuthorityMix(new[] { "D" });

        var enforced = auth.EnforcePolicy(edge, computed, packetProvidedMix: null, unmappedSourceRefs: Array.Empty<string>());

        Assert.True(enforced.NeedsReview);
        Assert.Equal(CompoundGraphEdgeType.HasCommunitySignal, enforced.EdgeType);
        Assert.Equal("synergy", enforced.AssertedRelationshipType);
        Assert.Equal("synergy", enforced.RelationshipType);
        Assert.Equal("Anecdotal", enforced.EvidenceTier);
        Assert.Equal("low", enforced.Confidence);
        Assert.Contains("low-authority-relationship-claim", enforced.ReviewFlags);
    }

    [Fact]
    public void EnforcePolicy_Preserves_Unknown_Confidence_When_Quarantining()
    {
        var auth = new RelationshipPacketAuthorizer();
        var edge = ProvisionalEdge("conflict", new[] { "src-d" }, evidenceTier: null, confidence: "unknown");
        var computed = new SourceAuthorityMix(new[] { "D" });

        var enforced = auth.EnforcePolicy(edge, computed, null, Array.Empty<string>());

        Assert.True(enforced.NeedsReview);
        Assert.Equal("unknown", enforced.Confidence);
        Assert.Equal("Unknown", enforced.EvidenceTier);
    }

    [Theory]
    [InlineData("community-stack")]
    [InlineData("popular-but-unsupported")]
    [InlineData("vendor-claimed")]
    [InlineData("misinformation-pattern")]
    public void EnforcePolicy_Does_Not_Quarantine_Community_Type_Even_With_D_Only_Sources(string relType)
    {
        var auth = new RelationshipPacketAuthorizer();
        var edge = ProvisionalEdge(relType, new[] { "src-d" }, evidenceTier: "Limited", confidence: "moderate");
        var computed = new SourceAuthorityMix(new[] { "D" });

        var enforced = auth.EnforcePolicy(edge, computed, null, Array.Empty<string>());

        Assert.False(enforced.NeedsReview);
        Assert.DoesNotContain("low-authority-relationship-claim", enforced.ReviewFlags);
        // Original EdgeType (set by caller) preserved when no quarantine.
        Assert.Equal(CompoundGraphEdgeType.SynergizesWith, enforced.EdgeType);
        Assert.Equal("Limited", enforced.EvidenceTier);
        Assert.Equal("moderate", enforced.Confidence);
    }

    [Fact]
    public void EnforcePolicy_Detects_Mix_Mismatch_When_Packet_Differs_From_Computed()
    {
        var auth = new RelationshipPacketAuthorizer();
        var edge = ProvisionalEdge("synergy", new[] { "src-b" });
        var computed = new SourceAuthorityMix(new[] { "B" });
        var packetProvided = new SourceAuthorityMix(new[] { "A1" }); // mismatch

        var enforced = auth.EnforcePolicy(edge, computed, packetProvided, Array.Empty<string>());

        Assert.Contains("source-authority-mix-mismatch", enforced.ReviewFlags);
        Assert.Equal(new[] { "B" }, enforced.SourceAuthorityMix.AuthorityTiers);
    }

    [Fact]
    public void EnforcePolicy_Does_Not_Flag_Mismatch_When_Packet_Matches_Computed()
    {
        var auth = new RelationshipPacketAuthorizer();
        var edge = ProvisionalEdge("synergy", new[] { "src-b" });
        var computed = new SourceAuthorityMix(new[] { "B" });
        var packetProvided = new SourceAuthorityMix(new[] { "B" });

        var enforced = auth.EnforcePolicy(edge, computed, packetProvided, Array.Empty<string>());

        Assert.DoesNotContain("source-authority-mix-mismatch", enforced.ReviewFlags);
    }

    [Fact]
    public void EnforcePolicy_Flags_Unmapped_Sources_And_Forces_Review()
    {
        var auth = new RelationshipPacketAuthorizer();
        var edge = ProvisionalEdge("synergy", new[] { "src-mystery" });
        var computed = new SourceAuthorityMix(Array.Empty<string>());

        var enforced = auth.EnforcePolicy(edge, computed, null, new[] { "src-mystery" });

        Assert.True(enforced.NeedsReview);
        Assert.Contains("unmapped-source-ref", enforced.ReviewFlags);
    }
}
