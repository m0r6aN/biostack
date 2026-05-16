namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using BioStack.KnowledgeWorker.Pipeline.Graph;
using Microsoft.Extensions.Logging;
using Xunit;

public class CompoundGraphTests
{
    // ──────────────────────────────────────────────────────────────────────
    // Schema validation (1, 2, 3)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Relationship_Packet_Sample_Validates_Against_Schema()
    {
        var loader = new ResearchArtifactLoader();
        var artifact = loader.Load(
            ResearchArtifactKind.RelationshipPacket,
            TestPaths.FixturePath("relationship-packet.sample.json"));
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.RelationshipPacket, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
    }

    [Fact]
    public void Relationship_Packet_With_Invalid_RelationshipType_Fails_Schema()
    {
        var loader = new ResearchArtifactLoader();
        var artifact = loader.Load(
            ResearchArtifactKind.RelationshipPacket,
            TestPaths.FixturePath("relationship-packet.sample.json"));
        artifact.Node["relationships"]![0]!["relationshipType"] = "not-a-real-type";
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.RelationshipPacket, artifact.Node);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Relationship_Packet_With_Empty_SourceRefs_Fails_Schema()
    {
        var loader = new ResearchArtifactLoader();
        var artifact = loader.Load(
            ResearchArtifactKind.RelationshipPacket,
            TestPaths.FixturePath("relationship-packet.sample.json"));
        artifact.Node["relationships"]![0]!["sourceRefs"] = new JsonArray();
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.RelationshipPacket, artifact.Node);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Keyword.Equals("minItems", StringComparison.OrdinalIgnoreCase));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Directory load (4)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResearchJob_Loads_Relationship_Packets_From_Directory()
    {
        var root = CreateTempDirectory();
        try
        {
            var relDir = Path.Combine(root, "relationships");
            var outDir = Path.Combine(root, "output");
            Directory.CreateDirectory(relDir);
            File.Copy(
                TestPaths.FixturePath("relationship-packet.sample.json"),
                Path.Combine(relDir, "packet-a.json"));
            File.Copy(
                TestPaths.FixturePath("relationship-packet.synergy-chain.sample.json"),
                Path.Combine(relDir, "packet-b.json"));

            var options = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchSourceRegistryFilePath = TestPaths.FixturePath("source-registry.sample.json"),
                ResearchEvidencePacketPath = TestPaths.FixturePath("evidence-packet.sample.json"),
                ResearchRelationshipPacketDirectory = relDir,
                ResearchOutputDirectory = outDir,
            };

            var result = await CreateJob(options).RunAsync(new IngestionContext(options, CreateLogger()));

            Assert.True(result.Success, result.ErrorMessage);

            var report = JsonNode.Parse(File.ReadAllText(Path.Combine(outDir, "research-run-report.json")))!;
            var validated = report["Records"]!.AsArray()
                .Count(r => r!["Status"]?.GetValue<string>() == "validated"
                            && r["ArtifactKind"]?.GetValue<string>() == "RelationshipPacket");
            Assert.Equal(2, validated);

            var graph = JsonNode.Parse(File.ReadAllText(Path.Combine(outDir, "compound-graph.json")))!;
            // Both packets' relationship edges should be present.
            var edges = graph["edges"]!.AsArray();
            Assert.Contains(edges, e =>
                e!["edgeId"]!.GetValue<string>().Contains("bpc-157", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(edges, e =>
                e!["edgeId"]!.GetValue<string>().Contains("chaintest-alpha", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Graph emission with/without relationship input (5, 6)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResearchJob_Emits_CompoundGraph_With_No_Relationship_Input()
    {
        var outDir = CreateTempDirectory();
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchSourceRegistryFilePath = TestPaths.FixturePath("source-registry.sample.json"),
                ResearchEvidencePacketPath = TestPaths.FixturePath("evidence-packet.sample.json"),
                ResearchOutputDirectory = outDir,
            };

            var result = await CreateJob(options).RunAsync(new IngestionContext(options, CreateLogger()));
            Assert.True(result.Success, result.ErrorMessage);

            var path = Path.Combine(outDir, "compound-graph.json");
            Assert.True(File.Exists(path));
            var graph = JsonNode.Parse(File.ReadAllText(path))!;

            var nodes = graph["nodes"]!.AsArray();
            var edges = graph["edges"]!.AsArray();

            Assert.Contains(nodes, n => n!["nodeId"]!.GetValue<string>() == "compound:creatine");
            Assert.Contains(nodes, n =>
                n!["nodeType"]!.GetValue<string>().Equals("claim", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(nodes, n =>
                n!["nodeType"]!.GetValue<string>().Equals("source-family", StringComparison.OrdinalIgnoreCase));

            // No relationship-derived edges.
            Assert.DoesNotContain(edges, e =>
                e!["edgeId"]!.GetValue<string>().StartsWith("relationship:", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task ResearchJob_Emits_CompoundGraph_With_Relationship_Input()
    {
        var outDir = CreateTempDirectory();
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchSourceRegistryFilePath = TestPaths.FixturePath("source-registry.sample.json"),
                ResearchEvidencePacketPath = TestPaths.FixturePath("evidence-packet.sample.json"),
                ResearchRelationshipPacketPath = TestPaths.FixturePath("relationship-packet.sample.json"),
                ResearchOutputDirectory = outDir,
            };

            var result = await CreateJob(options).RunAsync(new IngestionContext(options, CreateLogger()));
            Assert.True(result.Success, result.ErrorMessage);

            var graph = JsonNode.Parse(File.ReadAllText(Path.Combine(outDir, "compound-graph.json")))!;
            var edges = graph["edges"]!.AsArray();
            var relEdges = edges
                .Where(e => e!["edgeId"]!.GetValue<string>().StartsWith("relationship:", StringComparison.Ordinal))
                .ToList();
            Assert.Equal(3, relEdges.Count);
        }
        finally
        {
            Directory.Delete(outDir, recursive: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Authority policy & mix-mismatch via builder (7, 8)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Builder_Quarantines_D_Tier_Only_NonCommunity_Synergy_Relationship()
    {
        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());
        var packet = BuildRelationshipPacket(
            packetId: "p-quarantine",
            relationships: new[]
            {
                MakeRelationshipJson(
                    subject: "AlphaCompound",
                    obj: "BetaCompound",
                    relationshipType: "synergy",
                    sourceRefs: new[] { "src-forum-broscience-thread-001" },
                    evidenceTier: "Limited",
                    confidence: "moderate"),
            },
            packetSources: new[] { ("src-forum-broscience-thread-001", "D") });

            var registry = (JsonNode)new JsonObject { ["sources"] = new JsonArray() };
            var graph = builder.Build(
                new JsonArray(),
                Array.Empty<JsonNode>(),
                new[] { (JsonNode)packet },
                registry);

        var edge = graph.Edges.Single(e => e.EdgeId.StartsWith("relationship:alphacompound:betacompound", StringComparison.Ordinal));
        Assert.True(edge.NeedsReview);
        Assert.Equal(CompoundGraphEdgeType.HasCommunitySignal, edge.EdgeType);
        Assert.Equal("synergy", edge.AssertedRelationshipType);
        Assert.Equal("synergy", edge.RelationshipType);
        Assert.Equal("Anecdotal", edge.EvidenceTier);
        Assert.Equal("low", edge.Confidence);
        Assert.Contains("low-authority-relationship-claim", edge.ReviewFlags);
        Assert.Single(edge.SourceAuthorityMix.AuthorityTiers, "D");
    }

    [Fact]
    public void Builder_Flags_Source_Authority_Mix_Mismatch_From_Packet()
    {
        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());

        // Real sources are tier B (peptide-review-2024); but packet asserts mix = ["A1"].
        var rel = MakeRelationshipJson(
            subject: "PacketSubj",
            obj: "PacketObj",
            relationshipType: "complementary",
            sourceRefs: new[] { "src-peptide-review-2024" },
            evidenceTier: "Limited",
            confidence: "moderate");
        rel["sourceAuthorityMix"] = new JsonObject
        {
            ["authorityTiers"] = new JsonArray("A1"),
        };

        var packet = BuildRelationshipPacket(
            packetId: "p-mismatch",
            relationships: new[] { rel },
            packetSources: new[] { ("src-peptide-review-2024", "B") });

        var graph = builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { (JsonNode)packet }, sourceRegistry: null);

        var edge = graph.Edges.Single(e => e.EdgeId.StartsWith("relationship:packetsubj:packetobj", StringComparison.Ordinal));
        Assert.Contains("source-authority-mix-mismatch", edge.ReviewFlags);
        Assert.Equal(new[] { "B" }, edge.SourceAuthorityMix.AuthorityTiers);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Review findings (9, 10)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Builder_Emits_SynergyChainWithConflict_Finding_From_Fixture()
    {
        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());
        var packet = JsonNode.Parse(File.ReadAllText(
            TestPaths.FixturePath("relationship-packet.synergy-chain.sample.json")))!;

        var graph = builder.Build(
            new JsonArray(),
            Array.Empty<JsonNode>(),
            new[] { packet },
            sourceRegistry: null);

        Assert.Contains(graph.ReviewFindings, f =>
            f.FindingType == CompoundGraphFindingType.SynergyChainWithConflict);
    }

    [Fact]
    public void Builder_Emits_PopularStackInsufficientEvidence_Finding_For_BroScience_Pair()
    {
        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());
        var packet = JsonNode.Parse(File.ReadAllText(
            TestPaths.FixturePath("relationship-packet.sample.json")))!;

        var graph = builder.Build(
            new JsonArray(),
            Array.Empty<JsonNode>(),
            new[] { packet },
            sourceRegistry: null);

        var popularFinding = graph.ReviewFindings.SingleOrDefault(f =>
            f.FindingType == CompoundGraphFindingType.PopularStackInsufficientEvidence);
        Assert.NotNull(popularFinding);
        Assert.Contains("compound:caffeine", popularFinding!.CompoundRefs);
        Assert.Contains("compound:yohimbine", popularFinding.CompoundRefs);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Promotion invariance (11)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PromotionManifest_Identical_With_Or_Without_Relationship_Packets()
    {
        var outA = CreateTempDirectory();
        var outB = CreateTempDirectory();
        try
        {
            var baseOptions = new Func<string, string?, WorkerOptions>((dir, relPath) => new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchSourceRegistryFilePath = TestPaths.FixturePath("source-registry.sample.json"),
                ResearchEvidencePacketPath = TestPaths.FixturePath("evidence-packet.sample.json"),
                ResearchRelationshipPacketPath = relPath,
                ResearchOutputDirectory = dir,
            });

            var withoutOpts = baseOptions(outA, null);
            var withOpts = baseOptions(outB, TestPaths.FixturePath("relationship-packet.sample.json"));

            await CreateJob(withoutOpts).RunAsync(new IngestionContext(withoutOpts, CreateLogger()));
            await CreateJob(withOpts).RunAsync(new IngestionContext(withOpts, CreateLogger()));

            var manifestA = JsonNode.Parse(File.ReadAllText(Path.Combine(outA, "promotion-manifest.json")))!;
            var manifestB = JsonNode.Parse(File.ReadAllText(Path.Combine(outB, "promotion-manifest.json")))!;

            // Normalize: GeneratedAtUtc moves between runs and the Outputs paths point
            // at the per-run temp directory; everything else (Counts and per-bucket
            // decisions) must be byte-equal.
            manifestA["Outputs"] = null;
            manifestB["Outputs"] = null;
            manifestA["GeneratedAtUtc"] = null;
            manifestB["GeneratedAtUtc"] = null;

            Assert.Equal(manifestA.ToJsonString(), manifestB.ToJsonString());
        }
        finally
        {
            Directory.Delete(outA, recursive: true);
            Directory.Delete(outB, recursive: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Determinism (12)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Builder_Produces_Byte_Identical_Graph_For_Same_Inputs()
    {
        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());
        var packet = JsonNode.Parse(File.ReadAllText(
            TestPaths.FixturePath("relationship-packet.sample.json")))!;
        var packet2 = JsonNode.Parse(File.ReadAllText(
            TestPaths.FixturePath("relationship-packet.sample.json")))!;

        var jsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        var g1 = builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { packet }, null);
        var g2 = builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { packet2 }, null);

        // GeneratedAtUtc will differ; serialize a normalized projection.
        var s1 = SerializeWithoutTimestamp(g1, jsonOpts);
        var s2 = SerializeWithoutTimestamp(g2, jsonOpts);

        Assert.Equal(s1, s2);
    }

    private static string SerializeWithoutTimestamp(CompoundGraph g, JsonSerializerOptions opts)
    {
        var normalized = g with { GeneratedAtUtc = DateTimeOffset.UnixEpoch };
        return JsonSerializer.Serialize(normalized, opts);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Evidence tier preservation (13)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Builder_Preserves_Anecdotal_Evidence_Tier_Without_Silent_Promotion()
    {
        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());
        var rel = MakeRelationshipJson(
            subject: "AnecCompoundA",
            obj: "AnecCompoundB",
            relationshipType: "community-stack",
            sourceRefs: new[] { "src-forum-broscience-thread-001" },
            evidenceTier: "Anecdotal",
            confidence: "low");
        var packet = BuildRelationshipPacket(
            packetId: "p-anecdotal",
            relationships: new[] { rel },
            packetSources: new[] { ("src-forum-broscience-thread-001", "D") });

        var graph = builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { (JsonNode)packet }, null);

        var edge = graph.Edges.Single(e =>
            e.EdgeId.StartsWith("relationship:aneccompounda:aneccompoundb", StringComparison.Ordinal));
        Assert.Equal("Anecdotal", edge.EvidenceTier);
        // Community-type relationships should not be quarantined.
        Assert.DoesNotContain("low-authority-relationship-claim", edge.ReviewFlags);
        Assert.Equal(CompoundGraphEdgeType.HasCommunitySignal, edge.EdgeType);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private static JsonObject MakeRelationshipJson(
        string subject,
        string obj,
        string relationshipType,
        IReadOnlyList<string> sourceRefs,
        string evidenceTier,
        string confidence)
    {
        var sourceRefsArr = new JsonArray();
        foreach (var s in sourceRefs) sourceRefsArr.Add(s);

        return new JsonObject
        {
            ["relationshipId"] = $"rel-{subject.ToLowerInvariant()}-{obj.ToLowerInvariant()}",
            ["subjectCompound"] = subject,
            ["objectCompound"] = obj,
            ["relationshipType"] = relationshipType,
            ["directionality"] = "symmetric",
            ["effectDomain"] = "test-domain",
            ["mechanismBasis"] = new JsonArray(),
            ["categoryBasis"] = new JsonArray(),
            ["claimRefs"] = new JsonArray(),
            ["sourceRefs"] = sourceRefsArr,
            ["evidenceTier"] = evidenceTier,
            ["confidence"] = confidence,
            ["communitySignal"] = new JsonObject
            {
                ["present"] = false,
                ["signalStrength"] = "none",
                ["signalDirection"] = "unclear",
                ["signalUse"] = "research-priority",
                ["canonicalTruthStatus"] = "unknown",
                ["notes"] = "",
            },
            ["reviewFlags"] = new JsonArray(),
            ["resolutionStatus"] = "unresolved",
        };
    }

    private static JsonObject BuildRelationshipPacket(
        string packetId,
        IEnumerable<JsonObject> relationships,
        IEnumerable<(string id, string tier)> packetSources)
    {
        var rels = new JsonArray();
        foreach (var r in relationships) rels.Add(r);

        var srcs = new JsonArray();
        foreach (var (id, tier) in packetSources)
        {
            srcs.Add(new JsonObject
            {
                ["sourceId"] = id,
                ["authorityTier"] = tier,
            });
        }

        return new JsonObject
        {
            ["schemaVersion"] = "1.0.0",
            ["recordType"] = "compound-relationship-packet",
            ["packet"] = new JsonObject
            {
                ["packetId"] = packetId,
                ["agentId"] = "test-agent",
                ["generatedAt"] = "2026-05-01T00:00:00Z",
                ["sourceRegistryVersion"] = "2026.05.01",
            },
            ["relationships"] = rels,
            ["sources"] = srcs,
        };
    }

    private static ResearchJob CreateJob(WorkerOptions options) => new(
        options,
        new ResearchArtifactLoader(),
        ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory()),
        new EvidencePacketPreprocessor(),
        new SourceRegistryAuthorizer(),
        new EvidencePacketSubstanceRecordCompiler(),
        new ResearchReviewQueueBuilder(),
        new ResearchSummaryBuilder(),
        new ResearchTaskQueueBuilder(),
        new PromotionManifestBuilder(),
        new ReviewResolutionPlanBuilder(),
        new PromotionExporter(),
        new PromotionImportPreviewBuilder(),
        SubstanceRecordValidator.LoadFromFile(Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json")),
        new CompoundGraphBuilder(new RelationshipPacketAuthorizer()));

    private static ILogger CreateLogger()
        => LoggerFactory.Create(_ => { }).CreateLogger("CompoundGraphTests");

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"biostack-graph-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
