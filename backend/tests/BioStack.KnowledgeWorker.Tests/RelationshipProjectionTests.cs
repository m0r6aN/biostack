namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.Domain.Entities.Graph;
using BioStack.KnowledgeWorker.Pipeline;
using BioStack.KnowledgeWorker.Pipeline.Graph;
using Xunit;

/// <summary>
/// BIO-PAIRWISE-003: wiring assertions, inert-path assertions, and P1 publication-bar
/// enforcement (amendment PW-003-A1) for the relationship projection path:
/// <c>research/input/relationships/</c> -&gt; <see cref="CompoundGraphBuilder"/> -&gt;
/// <see cref="CompoundGraphPersistenceMapper"/> -&gt; the compound graph substrate that
/// <c>CompoundGraphStore</c> / <c>GraphIntelligenceService</c> read (BioStack.Infrastructure /
/// BioStack.Application are not referenced by this test project; the mapped
/// <see cref="CompoundGraphRelationship"/> payload this project can already produce is the exact
/// shape those two read-only layers persist/serve unfiltered, per the doc comments added to both
/// in this parcel).
///
/// Per docs/guidance/pairwise-relationship-publication-contract.v1.md §3, a relationship record is
/// publishable in v1 iff ALL of:
///   1. relationshipType ∈ {contraindicated, caution, conflict}
///   2. assertionClass ∈ {direct-evidence, authoritative-caution}
///   3. evidenceTier is present and not "Unknown"
///   4. relationshipReviewStatus == "accepted-as-evidence-backed"
///   5. at least one sourceRef resolves to an A1/A2 authority tier
/// </summary>
public class RelationshipProjectionTests
{
    // ──────────────────────────────────────────────────────────────────────
    // AC1 — a bar-passing record reaches the projection, retrievable by compound
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void PassingNegativeRelationship_ProjectsToGraph_RetrievableByCompound()
    {
        var packet = BuildPacket(MakeRelationship(
            relationshipId: "rel-pass-001",
            subject: "Lisinopril",
            obj: "Spironolactone",
            relationshipType: "conflict",
            assertionClass: "direct-evidence",
            evidenceTier: "Strong",
            relationshipReviewStatus: "accepted-as-evidence-backed",
            sourceRefs: new[] { "src-a1-label" }),
            sources: new[] { ("src-a1-label", "A1") });

        var payload = BuildPayload(packet);

        // "Retrievable by the compound it concerns" — the same slug-based lookup
        // CompoundGraphStore.GetRelationshipsForCompoundAsync performs.
        var forLisinopril = payload.Relationships.Where(r =>
            r.SubjectSlug == "lisinopril" || r.ObjectSlug == "lisinopril").ToList();
        var relationship = Assert.Single(forLisinopril);

        Assert.Equal(GraphRelationshipType.ConflictsWith, relationship.RelationshipType);
        Assert.Equal("Strong", relationship.EvidenceTier);
        Assert.Contains("src-a1-label", JsonSerializer.Deserialize<List<string>>(relationship.SourceRefsJson)!);
        Assert.Equal(GraphRelationshipType.SafetyConcern.High, relationship.SafetyConcernLevel);
    }

    // ──────────────────────────────────────────────────────────────────────
    // AC2 — one test per clause: a record failing exactly one clause is excluded
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Clause1_RelationshipTypeOutsideNegativeFamily_NotProjectedAsSafetyRelationship()
    {
        // Every other clause is bar-perfect; only relationshipType fails clause 1 (it is a
        // deferred positive type, not one of contraindicated/caution/conflict). P1 never governs
        // this record, so no AvoidWith/ConflictsWith edge is produced for it — admitting positive
        // relationship types into the public safety lane remains explicitly out of this parcel's
        // scope (spec "Out of Scope": Positive / synergy admission).
        var packet = BuildPacket(MakeRelationship(
            relationshipId: "rel-clause1-001",
            subject: "AlphaX",
            obj: "BetaX",
            relationshipType: "synergy",
            assertionClass: "direct-evidence",
            evidenceTier: "Strong",
            relationshipReviewStatus: "accepted-as-evidence-backed",
            sourceRefs: new[] { "src-a1-label" }),
            sources: new[] { ("src-a1-label", "A1") });

        var payload = BuildPayload(packet);

        Assert.DoesNotContain(payload.Relationships, r =>
            r.RelationshipType is GraphRelationshipType.ConflictsWith or GraphRelationshipType.AvoidWith);
    }

    [Fact]
    public void Clause2_AssertionClassNotPublishable_Excluded()
    {
        var packet = BuildPacket(MakeRelationship(
            relationshipId: "rel-clause2-001",
            subject: "GammaX",
            obj: "DeltaX",
            relationshipType: "caution",
            assertionClass: "community-signal", // valid enum value, but never publishable (§2b)
            evidenceTier: "Strong",
            relationshipReviewStatus: "accepted-as-evidence-backed",
            sourceRefs: new[] { "src-a1-label" }),
            sources: new[] { ("src-a1-label", "A1") });

        var payload = BuildPayload(packet);

        AssertNoRelationshipEdge(payload, "gammax", "deltax");
    }

    [Fact]
    public void Clause3_EvidenceTierUnknown_Excluded()
    {
        var packet = BuildPacket(MakeRelationship(
            relationshipId: "rel-clause3-001",
            subject: "EpsilonX",
            obj: "ZetaX",
            relationshipType: "caution",
            assertionClass: "direct-evidence",
            evidenceTier: "Unknown",
            relationshipReviewStatus: "accepted-as-evidence-backed",
            sourceRefs: new[] { "src-a1-label" }),
            sources: new[] { ("src-a1-label", "A1") });

        var payload = BuildPayload(packet);

        AssertNoRelationshipEdge(payload, "epsilonx", "zetax");
    }

    [Fact]
    public void Clause4_ReviewStatusNotAcceptedAsEvidenceBacked_Excluded()
    {
        // "human-reviewed" is a valid enum value but does not state the review's outcome; only
        // "accepted-as-evidence-backed" clears clause 4 (ratification §3 clause 4 rationale).
        var packet = BuildPacket(MakeRelationship(
            relationshipId: "rel-clause4-001",
            subject: "EtaX",
            obj: "ThetaX",
            relationshipType: "conflict",
            assertionClass: "direct-evidence",
            evidenceTier: "Strong",
            relationshipReviewStatus: "human-reviewed",
            sourceRefs: new[] { "src-a1-label" }),
            sources: new[] { ("src-a1-label", "A1") });

        var payload = BuildPayload(packet);

        AssertNoRelationshipEdge(payload, "etax", "thetax");
    }

    [Fact]
    public void Clause5_NoAuthoritativeTierSource_Excluded()
    {
        var packet = BuildPacket(MakeRelationship(
            relationshipId: "rel-clause5-001",
            subject: "IotaX",
            obj: "KappaX",
            relationshipType: "contraindicated",
            assertionClass: "authoritative-caution",
            evidenceTier: "Moderate",
            relationshipReviewStatus: "accepted-as-evidence-backed",
            sourceRefs: new[] { "src-b-tier-review" }),
            sources: new[] { ("src-b-tier-review", "B") });

        var payload = BuildPayload(packet);

        AssertNoRelationshipEdge(payload, "iotax", "kappax");
    }

    // ──────────────────────────────────────────────────────────────────────
    // AC3 — malformed records fail loudly, naming relationshipId; not silently skipped
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MalformedAssertionClass_FailsValidation_NamingRelationshipId()
    {
        var packet = BuildPacket(MakeRelationship(
            relationshipId: "rel-malformed-001",
            subject: "LambdaX",
            obj: "MuX",
            relationshipType: "caution",
            assertionClass: "not-a-real-assertion-class",
            evidenceTier: "Strong",
            relationshipReviewStatus: "accepted-as-evidence-backed",
            sourceRefs: new[] { "src-a1-label" }),
            sources: new[] { ("src-a1-label", "A1") });

        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { (JsonNode)packet }, null));

        Assert.Contains("rel-malformed-001", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedRelationshipReviewStatus_FailsValidation_NamingRelationshipId()
    {
        var packet = BuildPacket(MakeRelationship(
            relationshipId: "rel-malformed-002",
            subject: "NuX",
            obj: "XiX",
            relationshipType: "conflict",
            assertionClass: "direct-evidence",
            evidenceTier: "Strong",
            relationshipReviewStatus: "not-a-real-review-status",
            sourceRefs: new[] { "src-a1-label" }),
            sources: new[] { ("src-a1-label", "A1") });

        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { (JsonNode)packet }, null));

        Assert.Contains("rel-malformed-002", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingRelationshipId_FailsValidation_RatherThanSilentSkip()
    {
        var rel = MakeRelationship(
            relationshipId: "placeholder",
            subject: "OmicronX",
            obj: "PiX",
            relationshipType: "conflict",
            assertionClass: "direct-evidence",
            evidenceTier: "Strong",
            relationshipReviewStatus: "accepted-as-evidence-backed",
            sourceRefs: new[] { "src-a1-label" });
        rel["relationshipId"] = "";
        var packet = BuildPacket(rel, sources: new[] { ("src-a1-label", "A1") });

        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { (JsonNode)packet }, null));

        Assert.Contains("relationshipId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────────────────────────────
    // AC4 — no relationship records projects empty collections, no error
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void NoRelationshipRecords_ProjectsEmptyCollections_NoError()
    {
        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());

        var graph = builder.Build(new JsonArray(), Array.Empty<JsonNode>(), Array.Empty<JsonNode>(), null);
        var payload = CompoundGraphPersistenceMapper.Map(graph);

        Assert.Empty(payload.Relationships);
        Assert.Empty(payload.Findings);
        Assert.Equal(0, payload.Artifact.RelationshipCount);
    }

    // ──────────────────────────────────────────────────────────────────────
    // AC5 — the non-authoritative path (EvidencePacketSubstanceRecordCompiler) stays inert
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void EvidencePacketCompiler_RelationshipShapedFields_StayEmpty_RegardlessOfEvidenceContent()
    {
        var evidencePacket = JsonNode.Parse(
            File.ReadAllText(TestPaths.FixturePath("evidence-packet.sample.json")))!;

        var compiler = new EvidencePacketSubstanceRecordCompiler();
        var draft = compiler.CompileDraft(evidencePacket)!.AsObject();

        // The inert path: no relationship packet is even accepted as input to this compiler, so
        // these fields can never be populated from here — they are not merely unused today.
        Assert.Empty(draft["interactions"]!.AsArray());
        Assert.Empty(draft["compatibility"]!["compatibleBlends"]!.AsArray());
        Assert.Empty(draft["compatibility"]!["incompatibleBlends"]!.AsArray());
        Assert.Empty(draft["compatibility"]!["unknownCompatibility"]!.AsArray());
        Assert.Empty(draft["stackIntelligence"]!["pairsWellWith"]!.AsArray());
        Assert.Empty(draft["stackIntelligence"]!["avoidWith"]!.AsArray());
        Assert.Empty(draft["stackIntelligence"]!["conflictRules"]!.AsArray());
        Assert.Empty(draft["stackIntelligence"]!["synergyRules"]!.AsArray());
        Assert.Empty(draft["stackIntelligence"]!["redundancyRules"]!.AsArray());
    }

    // ──────────────────────────────────────────────────────────────────────
    // AC6 — provider round-trip stability: the new exclusion logic introduces no non-determinism
    // or ordering dependency into the persisted shape (the governed persisted fields themselves —
    // string/bool/int plus the pre-existing CreatedAtUtc — are unchanged by this parcel; their
    // dual-provider DateTimeKind/precision handling is already covered by migration
    // RepairCompoundGraphPostgresTypes + CompoundGraphPostgresTypeRepairMigrationTests, which this
    // parcel does not touch, per D-B no-migration).
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void PassingNegativeRelationship_MapsToIdenticalPersistableShape_AcrossRepeatedBuilds()
    {
        var packet1 = BuildPacket(MakeRelationship(
            relationshipId: "rel-stable-001",
            subject: "RhoX",
            obj: "SigmaX",
            relationshipType: "caution",
            assertionClass: "authoritative-caution",
            evidenceTier: "Moderate",
            relationshipReviewStatus: "accepted-as-evidence-backed",
            sourceRefs: new[] { "src-a2-label" }),
            sources: new[] { ("src-a2-label", "A2") });
        // A fresh, independently-parsed copy — same bytes, different object graph/dictionary
        // enumeration instances, to flush out any accidental ordering dependency.
        var packet2 = JsonNode.Parse(packet1.ToJsonString())!;

        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());
        var g1 = builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { (JsonNode)packet1 }, null);
        var g2 = builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { packet2 }, null);

        var p1 = CompoundGraphPersistenceMapper.Map(g1 with { GeneratedAtUtc = DateTimeOffset.UnixEpoch });
        var p2 = CompoundGraphPersistenceMapper.Map(g2 with { GeneratedAtUtc = DateTimeOffset.UnixEpoch });

        Assert.Equal(p1.Artifact.ArtifactHash, p2.Artifact.ArtifactHash);
        var r1 = Assert.Single(p1.Relationships);
        var r2 = Assert.Single(p2.Relationships);
        Assert.Equal(r1.SubjectSlug, r2.SubjectSlug);
        Assert.Equal(r1.ObjectSlug, r2.ObjectSlug);
        Assert.Equal(r1.RelationshipType, r2.RelationshipType);
        Assert.Equal(r1.EvidenceTier, r2.EvidenceTier);
        Assert.Equal(r1.SourceRefsJson, r2.SourceRefsJson);
        Assert.Equal(r1.SafetyConcernLevel, r2.SafetyConcernLevel);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private static void AssertNoRelationshipEdge(CompoundGraphPersistenceMapper.Payload payload, string subjectSlug, string objectSlug)
    {
        Assert.DoesNotContain(payload.Relationships, r =>
            (r.SubjectSlug == subjectSlug && r.ObjectSlug == objectSlug)
            || (r.SubjectSlug == objectSlug && r.ObjectSlug == subjectSlug));
    }

    private static CompoundGraphPersistenceMapper.Payload BuildPayload(JsonObject packet)
    {
        var builder = new CompoundGraphBuilder(new RelationshipPacketAuthorizer());
        var graph = builder.Build(new JsonArray(), Array.Empty<JsonNode>(), new[] { (JsonNode)packet }, null);
        return CompoundGraphPersistenceMapper.Map(graph);
    }

    private static JsonObject MakeRelationship(
        string relationshipId,
        string subject,
        string obj,
        string relationshipType,
        string assertionClass,
        string evidenceTier,
        string relationshipReviewStatus,
        IReadOnlyList<string> sourceRefs)
    {
        var sourceRefsArr = new JsonArray();
        foreach (var s in sourceRefs) sourceRefsArr.Add(s);

        return new JsonObject
        {
            ["relationshipId"] = relationshipId,
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
            ["confidence"] = "moderate",
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
            ["resolutionStatus"] = "resolved",
            ["assertionClass"] = assertionClass,
            ["relationshipReviewStatus"] = relationshipReviewStatus,
        };
    }

    private static JsonObject BuildPacket(
        JsonObject relationship,
        IEnumerable<(string id, string tier)> sources)
    {
        var srcs = new JsonArray();
        foreach (var (id, tier) in sources)
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
                ["packetId"] = "rel-projection-tests",
                ["agentId"] = "test-agent",
                ["generatedAt"] = "2026-05-01T00:00:00Z",
                ["sourceRegistryVersion"] = "2026.05.01",
            },
            ["relationships"] = new JsonArray(relationship),
            ["sources"] = srcs,
        };
    }
}
