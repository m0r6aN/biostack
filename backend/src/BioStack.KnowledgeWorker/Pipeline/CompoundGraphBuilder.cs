namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline.Graph;

public interface ICompoundGraphBuilder
{
    CompoundGraph Build(
        JsonArray draftSubstances,
        IReadOnlyList<JsonNode> evidencePackets,
        IReadOnlyList<JsonNode> relationshipPackets,
        JsonNode? sourceRegistry);
}

/// <summary>
/// Builds a deterministic cross-compound relationship graph from drafts, evidence
/// packets, and relationship packets. All relationship edges are routed through
/// the <see cref="IRelationshipPacketAuthorizer"/> for authority-tier policy.
/// </summary>
public sealed class CompoundGraphBuilder : ICompoundGraphBuilder
{
    private const string GraphVersion = "1.0.0";

    private readonly IRelationshipPacketAuthorizer _authorizer;

    public CompoundGraphBuilder(IRelationshipPacketAuthorizer authorizer)
    {
        _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
    }

    public CompoundGraph Build(
        JsonArray draftSubstances,
        IReadOnlyList<JsonNode> evidencePackets,
        IReadOnlyList<JsonNode> relationshipPackets,
        JsonNode? sourceRegistry)
    {
        draftSubstances ??= new JsonArray();
        evidencePackets ??= Array.Empty<JsonNode>();
        relationshipPackets ??= Array.Empty<JsonNode>();

        var nodes = new Dictionary<string, CompoundGraphNode>(StringComparer.Ordinal);
        var edges = new Dictionary<string, CompoundGraphEdge>(StringComparer.Ordinal);

        // 1. Compound nodes from drafts.
        foreach (var draft in draftSubstances.OfType<JsonObject>())
        {
            var identity = draft["identity"] as JsonObject;
            if (identity is null) continue;
            var canonical = ReadString(identity["canonicalName"]);
            if (canonical.Length == 0) continue;
            var aliases = ReadStringArray(identity["aliases"]);
            AddCompoundNode(nodes, canonical, aliases);

            // Category nodes + belongs-to-category edges from draft classification.
            var classification = ReadString(identity["classification"]);
            if (classification.Length > 0)
            {
                AddCategoryEdge(nodes, edges, canonical, classification);
            }
        }

        // 2. Compound nodes + category nodes from evidence packets; collect claim nodes/edges.
        foreach (var packet in evidencePackets)
        {
            if (packet is not JsonObject packetObj) continue;
            var compound = packetObj["compound"] as JsonObject;
            if (compound is null) continue;
            var canonical = ReadString(compound["canonicalName"]);
            if (canonical.Length == 0) continue;
            var aliases = ReadStringArray(compound["aliases"]);
            AddCompoundNode(nodes, canonical, aliases);

            // Category from packet.category.
            var category = ReadString(packetObj["packet"]?["category"]);
            if (category.Length > 0)
            {
                AddCategoryEdge(nodes, edges, canonical, category);
            }

            // Claim nodes + edges.
            var claims = packetObj["claims"] as JsonArray;
            if (claims is null) continue;

            var packetSources = packetObj["sources"] as JsonArray;

            foreach (var claimNode in claims.OfType<JsonObject>())
            {
                var claimId = ReadString(claimNode["claimId"]);
                if (claimId.Length == 0) continue;
                var claimType = ReadString(claimNode["claimType"]);
                var statement = ReadString(claimNode["statement"]);

                var compoundSlug = Slug(canonical);
                var claimNodeId = $"claim:{compoundSlug}:{claimId}";

                AddNode(nodes, new CompoundGraphNode(
                    NodeId: claimNodeId,
                    NodeType: CompoundGraphNodeType.Claim,
                    Label: statement.Length > 0 ? statement : claimId,
                    Aliases: Array.Empty<string>(),
                    Metadata: new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
                    {
                        ["claimId"] = claimId,
                        ["claimType"] = claimType.Length > 0 ? claimType : null,
                    }));

                // Compound -> claim
                var isRisk = IsRiskClaimType(claimType);
                var compoundClaimEdgeType = isRisk
                    ? CompoundGraphEdgeType.HasRiskClaim
                    : CompoundGraphEdgeType.HasBenefitClaim;

                var compoundClaimEdgeId = $"{Slug(compoundClaimEdgeType.ToString())}:{compoundSlug}:{claimId}";
                AddSimpleEdge(edges, compoundClaimEdgeId,
                    from: CompoundNodeId(canonical),
                    to: claimNodeId,
                    edgeType: compoundClaimEdgeType,
                    relationshipType: claimType.Length > 0 ? claimType : null);

                // Claim -> source-family edges.
                var claimSourceRefs = ReadStringArray(claimNode["sourceRefs"]);
                var isContradiction = string.Equals(claimType, "controversy", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(claimType, "misinformation-claim", StringComparison.OrdinalIgnoreCase);
                foreach (var sourceRef in claimSourceRefs)
                {
                    var tier = LookupAuthorityTier(sourceRef, packetSources, sourceRegistry);
                    if (string.IsNullOrWhiteSpace(tier)) continue;
                    var sourceFamilyId = SourceFamilyNodeId(tier!);
                    AddNode(nodes, new CompoundGraphNode(
                        NodeId: sourceFamilyId,
                        NodeType: CompoundGraphNodeType.SourceFamily,
                        Label: $"Tier {tier!.ToUpperInvariant()} sources",
                        Aliases: Array.Empty<string>(),
                        Metadata: new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
                        {
                            ["authorityTier"] = tier,
                        }));

                    var supportEdgeType = isContradiction
                        ? CompoundGraphEdgeType.ContradictedBy
                        : CompoundGraphEdgeType.SupportedBy;
                    var supportEdgeId = $"{Slug(supportEdgeType.ToString())}:{claimNodeId}:{sourceFamilyId}";
                    AddSimpleEdge(edges, supportEdgeId,
                        from: claimNodeId,
                        to: sourceFamilyId,
                        edgeType: supportEdgeType,
                        relationshipType: null);
                }
            }
        }

        // 3. Relationship packets — compound + category + effect-domain + mechanism nodes and edges.
        foreach (var packet in relationshipPackets)
        {
            if (packet is not JsonObject packetObj) continue;
            var packetSources = packetObj["sources"] as JsonArray;
            var relationships = packetObj["relationships"] as JsonArray;
            if (relationships is null) continue;

            foreach (var rel in relationships.OfType<JsonObject>())
            {
                BuildRelationship(rel, packetSources, sourceRegistry, nodes, edges);
            }
        }

        // 4. Sort & assemble.
        var sortedNodes = nodes.Values
            .OrderBy(n => n.NodeId, StringComparer.Ordinal)
            .ToList();

        var sortedEdges = edges.Values
            .Select(SortEdgeCollections)
            .OrderBy(e => e.EdgeId, StringComparer.Ordinal)
            .ToList();

        var counts = new CompoundGraphCounts(
            Nodes: sortedNodes.Count,
            Edges: sortedEdges.Count,
            ReviewRequiredEdges: sortedEdges.Count(e => e.NeedsReview),
            CommunitySignalEdges: sortedEdges.Count(e =>
                e.EdgeType == CompoundGraphEdgeType.HasCommunitySignal
                || (e.CommunitySignal is not null && e.CommunitySignal.Present)),
            ConflictEdges: sortedEdges.Count(e =>
                e.EdgeType is CompoundGraphEdgeType.ConflictsWith
                    or CompoundGraphEdgeType.AvoidWith
                    or CompoundGraphEdgeType.OpposesEffect));

        var findings = BuildReviewFindings(sortedEdges, sortedNodes);

        return new CompoundGraph(
            GraphVersion: GraphVersion,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Counts: counts,
            Nodes: sortedNodes,
            Edges: sortedEdges,
            ReviewFindings: findings);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Relationship construction
    // ────────────────────────────────────────────────────────────────────────

    private void BuildRelationship(
        JsonObject rel,
        JsonArray? packetSources,
        JsonNode? sourceRegistry,
        Dictionary<string, CompoundGraphNode> nodes,
        Dictionary<string, CompoundGraphEdge> edges)
    {
        var subject = ReadString(rel["subjectCompound"]);
        var obj = ReadString(rel["objectCompound"]);
        if (subject.Length == 0 || obj.Length == 0) return;

        AddCompoundNode(nodes, subject, Array.Empty<string>());
        AddCompoundNode(nodes, obj, Array.Empty<string>());

        var relationshipType = ReadString(rel["relationshipType"]);
        var edgeType = MapRelationshipType(relationshipType);

        var subjectSlug = Slug(subject);
        var objectSlug = Slug(obj);
        var edgeId = $"relationship:{subjectSlug}:{objectSlug}:{relationshipType}";

        var effectDomain = ReadString(rel["effectDomain"]);
        var evidenceTier = ReadString(rel["evidenceTier"]);
        var confidence = ReadString(rel["confidence"]);
        var sourceRefs = ReadStringArray(rel["sourceRefs"]);
        var claimRefs = ReadStringArray(rel["claimRefs"]);
        var reviewFlags = ReadStringArray(rel["reviewFlags"]);

        // Category basis → category nodes + belongs-to-category edges for both compounds.
        foreach (var cat in ReadStringArray(rel["categoryBasis"]))
        {
            AddCategoryEdge(nodes, edges, subject, cat);
            AddCategoryEdge(nodes, edges, obj, cat);
        }

        // Effect-domain node + affects-pathway edge.
        if (effectDomain.Length > 0)
        {
            var effectNodeId = $"effect-domain:{Slug(effectDomain)}";
            AddNode(nodes, new CompoundGraphNode(
                NodeId: effectNodeId,
                NodeType: CompoundGraphNodeType.EffectDomain,
                Label: effectDomain,
                Aliases: Array.Empty<string>(),
                Metadata: EmptyMetadata()));
            AddSimpleEdge(edges,
                edgeId: $"affects-pathway:{subjectSlug}:{Slug(effectDomain)}",
                from: CompoundNodeId(subject),
                to: effectNodeId,
                edgeType: CompoundGraphEdgeType.AffectsPathway,
                relationshipType: null);
            AddSimpleEdge(edges,
                edgeId: $"affects-pathway:{objectSlug}:{Slug(effectDomain)}",
                from: CompoundNodeId(obj),
                to: effectNodeId,
                edgeType: CompoundGraphEdgeType.AffectsPathway,
                relationshipType: null);
        }

        // Mechanism basis → mechanism nodes + affects-pathway edges (mechanism category).
        foreach (var mech in ReadStringArray(rel["mechanismBasis"]))
        {
            var mechNodeId = $"mechanism:{Slug(mech)}";
            AddNode(nodes, new CompoundGraphNode(
                NodeId: mechNodeId,
                NodeType: CompoundGraphNodeType.Mechanism,
                Label: mech,
                Aliases: Array.Empty<string>(),
                Metadata: EmptyMetadata()));
            AddSimpleEdge(edges,
                edgeId: $"has-target:{subjectSlug}:{Slug(mech)}",
                from: CompoundNodeId(subject),
                to: mechNodeId,
                edgeType: CompoundGraphEdgeType.HasTarget,
                relationshipType: null);
            AddSimpleEdge(edges,
                edgeId: $"has-target:{objectSlug}:{Slug(mech)}",
                from: CompoundNodeId(obj),
                to: mechNodeId,
                edgeType: CompoundGraphEdgeType.HasTarget,
                relationshipType: null);
        }

        // Community signal (if present).
        CommunitySignal? communitySignal = ReadCommunitySignal(rel["communitySignal"]);

        // Authority mix: compute from sources, ignore packet-provided as authoritative.
        var computedMix = _authorizer.ComputeAuthorityMix(
            sourceRefs,
            packetSources,
            sourceRegistry,
            out var unmappedSourceRefs);

        var packetProvidedMix = ReadPacketSourceAuthorityMix(rel["sourceAuthorityMix"]);

        // Build provisional edge, then enforce policy.
        var provisional = new CompoundGraphEdge(
            EdgeId: edgeId,
            From: CompoundNodeId(subject),
            To: CompoundNodeId(obj),
            EdgeType: edgeType,
            RelationshipType: relationshipType.Length > 0 ? relationshipType : null,
            AssertedRelationshipType: relationshipType.Length > 0 ? relationshipType : null,
            EffectDomain: effectDomain.Length > 0 ? effectDomain : null,
            EvidenceTier: evidenceTier.Length > 0 ? evidenceTier : null,
            Confidence: confidence.Length > 0 ? confidence : null,
            SourceRefs: sourceRefs,
            ClaimRefs: claimRefs,
            ReviewFlags: reviewFlags,
            NeedsReview: false,
            CommunitySignal: communitySignal,
            SourceAuthorityMix: computedMix);

        // Pre-policy needsReview: relationshipReviewStatus / resolutionStatus / packet flags.
        var resolution = ReadString(rel["resolutionStatus"]);
        var relReview = ReadString(rel["relationshipReviewStatus"]);
        var pktNeedsReview =
            string.Equals(resolution, "needs-human-review", StringComparison.OrdinalIgnoreCase)
            || string.Equals(relReview, "review-required", StringComparison.OrdinalIgnoreCase);
        if (pktNeedsReview) provisional = provisional with { NeedsReview = true };

        var enforced = _authorizer.EnforcePolicy(
            provisional,
            computedMix,
            packetProvidedMix,
            unmappedSourceRefs);

        // De-dup: last write wins is fine because edgeId encodes the relationship triple.
        edges[enforced.EdgeId] = enforced;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Review findings
    // ────────────────────────────────────────────────────────────────────────

    private static IReadOnlyList<CompoundGraphReviewFinding> BuildReviewFindings(
        IReadOnlyList<CompoundGraphEdge> edges,
        IReadOnlyList<CompoundGraphNode> nodes)
    {
        var findings = new List<CompoundGraphReviewFinding>();
        findings.AddRange(FindSynergyChainsWithConflict(edges));
        findings.AddRange(FindSameCategoryOpposingEffects(edges, nodes));
        findings.AddRange(FindSharedPathwayAdditiveRisk(edges, nodes));
        findings.AddRange(FindPopularStackInsufficientEvidence(edges));
        findings.AddRange(FindCommunityClaimContradictedByAuthority(edges));

        return findings
            .OrderBy(f => f.FindingId, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<CompoundGraphReviewFinding> FindSynergyChainsWithConflict(
        IReadOnlyList<CompoundGraphEdge> edges)
    {
        var synergyEdges = edges
            .Where(e => e.EdgeType is CompoundGraphEdgeType.SynergizesWith or CompoundGraphEdgeType.Complements)
            .ToList();
        var conflictEdges = edges
            .Where(e => e.EdgeType is CompoundGraphEdgeType.ConflictsWith
                                    or CompoundGraphEdgeType.OpposesEffect
                                    or CompoundGraphEdgeType.AvoidWith)
            .ToList();

        var emitted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var ab in synergyEdges)
        foreach (var bc in synergyEdges)
        {
            if (ab.EdgeId == bc.EdgeId) continue;
            // chain A-B-C where B is shared
            var (a, b1) = (ab.From, ab.To);
            var (b2, c) = (bc.From, bc.To);
            // try both orientations
            foreach (var combo in PossibleChains(a, b1, b2, c))
            {
                var aNode = combo.A;
                var cNode = combo.C;
                if (aNode == cNode) continue;

                var conflict = conflictEdges.FirstOrDefault(e =>
                    (e.From == aNode && e.To == cNode) || (e.From == cNode && e.To == aNode));
                if (conflict is null) continue;

                var compounds = new[] { aNode, combo.B, cNode }.OrderBy(s => s, StringComparer.Ordinal).ToList();
                var findingId = $"finding:synergy-chain-with-conflict:{string.Join("+", compounds)}";
                if (!emitted.Add(findingId)) continue;

                yield return new CompoundGraphReviewFinding(
                    FindingId: findingId,
                    FindingType: CompoundGraphFindingType.SynergyChainWithConflict,
                    Severity: CompoundGraphFindingSeverity.High,
                    CompoundRefs: compounds,
                    EdgeRefs: new[] { ab.EdgeId, bc.EdgeId, conflict.EdgeId }
                        .OrderBy(s => s, StringComparer.Ordinal).ToList(),
                    Summary: $"Synergy chain {aNode}-{combo.B}-{cNode} contains a direct conflict edge between {aNode} and {cNode}.",
                    RecommendedAction: "human-review-conflict-vs-synergy",
                    NeedsHumanReview: true);
            }
        }
    }

    private static IEnumerable<(string A, string B, string C)> PossibleChains(
        string a, string b1, string b2, string c)
    {
        // ab is A->B, bc is B->C. We accept undirected chains: shared endpoint becomes B.
        if (b1 == b2) yield return (a, b1, c);
        if (b1 == c) yield return (a, b1, b2);
        if (a == b2) yield return (b1, a, c);
        if (a == c) yield return (b1, a, b2);
    }

    private static IEnumerable<CompoundGraphReviewFinding> FindSameCategoryOpposingEffects(
        IReadOnlyList<CompoundGraphEdge> edges,
        IReadOnlyList<CompoundGraphNode> nodes)
    {
        // Map compound -> categories
        var compoundCategories = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var e in edges.Where(e => e.EdgeType == CompoundGraphEdgeType.BelongsToCategory))
        {
            if (!compoundCategories.TryGetValue(e.From, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                compoundCategories[e.From] = set;
            }
            set.Add(e.To);
        }

        // Track polarity per (compound, effectDomain).
        // We rely on relationship edges' Metadata-free shape; instead, scan relationship edges
        // that contain effectDomain + a polarity hint via review flags or known sentinel relationship types.
        // Simpler approach: look at opposing-effect / conflict relationship edges as direct opposing signal,
        // and skip silently otherwise (per spec best-effort).
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in edges.Where(e => e.EdgeType == CompoundGraphEdgeType.OpposesEffect && e.EffectDomain is not null))
        {
            if (!compoundCategories.TryGetValue(e.From, out var aCats)) continue;
            if (!compoundCategories.TryGetValue(e.To, out var bCats)) continue;
            var shared = aCats.Intersect(bCats, StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToList();
            if (shared.Count == 0) continue;

            var compounds = new[] { e.From, e.To }.OrderBy(s => s, StringComparer.Ordinal).ToList();
            var findingId = $"finding:same-category-opposing-effects:{string.Join("+", compounds)}:{Slug(e.EffectDomain!)}";
            if (!emitted.Add(findingId)) continue;

            yield return new CompoundGraphReviewFinding(
                FindingId: findingId,
                FindingType: CompoundGraphFindingType.SameCategoryOpposingEffects,
                Severity: CompoundGraphFindingSeverity.Moderate,
                CompoundRefs: compounds,
                EdgeRefs: new[] { e.EdgeId },
                Summary: $"Compounds {e.From} and {e.To} share category {shared[0]} but have opposing effects on {e.EffectDomain}.",
                RecommendedAction: "human-review-same-category-opposing",
                NeedsHumanReview: true);
        }
    }

    private static IEnumerable<CompoundGraphReviewFinding> FindSharedPathwayAdditiveRisk(
        IReadOnlyList<CompoundGraphEdge> edges,
        IReadOnlyList<CompoundGraphNode> nodes)
    {
        // Map compound -> pathway/effect-domain targets via HasTarget / AffectsPathway.
        var compoundPathways = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var e in edges.Where(e =>
            e.EdgeType is CompoundGraphEdgeType.AffectsPathway or CompoundGraphEdgeType.HasTarget))
        {
            if (!compoundPathways.TryGetValue(e.From, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                compoundPathways[e.From] = set;
            }
            set.Add(e.To);
        }

        // Find compounds with risk claims.
        var compoundsWithRisk = new HashSet<string>(
            edges.Where(e => e.EdgeType == CompoundGraphEdgeType.HasRiskClaim).Select(e => e.From),
            StringComparer.Ordinal);

        var emitted = new HashSet<string>(StringComparer.Ordinal);
        var compounds = compoundPathways.Keys.ToList();
        for (var i = 0; i < compounds.Count; i++)
        for (var j = i + 1; j < compounds.Count; j++)
        {
            var a = compounds[i];
            var c = compounds[j];
            if (!compoundsWithRisk.Contains(a) || !compoundsWithRisk.Contains(c)) continue;
            var shared = compoundPathways[a].Intersect(compoundPathways[c], StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal).ToList();
            if (shared.Count == 0) continue;

            var pair = new[] { a, c }.OrderBy(s => s, StringComparer.Ordinal).ToList();
            var findingId = $"finding:shared-pathway-additive-risk:{string.Join("+", pair)}:{shared[0]}";
            if (!emitted.Add(findingId)) continue;

            yield return new CompoundGraphReviewFinding(
                FindingId: findingId,
                FindingType: CompoundGraphFindingType.SharedPathwayAdditiveRisk,
                Severity: CompoundGraphFindingSeverity.High,
                CompoundRefs: pair,
                EdgeRefs: Array.Empty<string>(),
                Summary: $"Compounds {a} and {c} share pathway {shared[0]} and both carry risk claims (additive risk potential).",
                RecommendedAction: "human-review-additive-risk",
                NeedsHumanReview: true);
        }
    }

    private static IEnumerable<CompoundGraphReviewFinding> FindPopularStackInsufficientEvidence(
        IReadOnlyList<CompoundGraphEdge> edges)
    {
        var lowTiers = new HashSet<string>(new[] { "Anecdotal", "Insufficient", "Unknown" }, StringComparer.OrdinalIgnoreCase);
        var strongTiers = new HashSet<string>(new[] { "Strong", "Moderate" }, StringComparer.OrdinalIgnoreCase);

        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in edges.Where(e => e.EdgeType == CompoundGraphEdgeType.HasCommunitySignal))
        {
            if (e.EvidenceTier is null || !lowTiers.Contains(e.EvidenceTier)) continue;

            // Look for corroborating edge between same pair with strong/moderate tier from non-D/X sources.
            var hasCorroboration = edges.Any(other =>
                other.EdgeId != e.EdgeId
                && PairMatches(e, other)
                && other.EvidenceTier is not null
                && strongTiers.Contains(other.EvidenceTier)
                && !other.SourceAuthorityMix.IsLowAuthorityOnly);
            if (hasCorroboration) continue;

            var pair = new[] { e.From, e.To }.OrderBy(s => s, StringComparer.Ordinal).ToList();
            var findingId = $"finding:popular-stack-insufficient-evidence:{string.Join("+", pair)}";
            if (!emitted.Add(findingId)) continue;

            yield return new CompoundGraphReviewFinding(
                FindingId: findingId,
                FindingType: CompoundGraphFindingType.PopularStackInsufficientEvidence,
                Severity: CompoundGraphFindingSeverity.Moderate,
                CompoundRefs: pair,
                EdgeRefs: new[] { e.EdgeId },
                Summary: $"Popular stack {e.From}+{e.To} backed only by anecdotal/community evidence.",
                RecommendedAction: "human-review-popular-stack-evidence-gap",
                NeedsHumanReview: true);
        }
    }

    private static IEnumerable<CompoundGraphReviewFinding> FindCommunityClaimContradictedByAuthority(
        IReadOnlyList<CompoundGraphEdge> edges)
    {
        var authoritativeContradictionTypes = new HashSet<CompoundGraphEdgeType>
        {
            CompoundGraphEdgeType.ConflictsWith,
            CompoundGraphEdgeType.AvoidWith,
            CompoundGraphEdgeType.OpposesEffect,
            CompoundGraphEdgeType.ContradictedBy,
        };
        var regulatoryTiers = new HashSet<string>(new[] { "A1", "A2", "B1", "B2" }, StringComparer.OrdinalIgnoreCase);

        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in edges.Where(e => e.EdgeType == CompoundGraphEdgeType.HasCommunitySignal
                                            && e.CommunitySignal is not null
                                            && e.CommunitySignal.SignalDirection == CommunitySignalDirection.Positive))
        {
            var contradiction = edges.FirstOrDefault(other =>
                other.EdgeId != e.EdgeId
                && PairMatches(e, other)
                && authoritativeContradictionTypes.Contains(other.EdgeType)
                && (other.SourceAuthorityMix.IsRegulatoryGrade
                    || other.SourceAuthorityMix.AuthorityTiers.Any(t => regulatoryTiers.Contains(t))));
            if (contradiction is null) continue;

            var pair = new[] { e.From, e.To }.OrderBy(s => s, StringComparer.Ordinal).ToList();
            var findingId = $"finding:community-claim-contradicted-by-authority:{string.Join("+", pair)}";
            if (!emitted.Add(findingId)) continue;

            var tier = contradiction.SourceAuthorityMix.AuthorityTiers.FirstOrDefault() ?? "authoritative";
            yield return new CompoundGraphReviewFinding(
                FindingId: findingId,
                FindingType: CompoundGraphFindingType.CommunityClaimContradictedByAuthority,
                Severity: CompoundGraphFindingSeverity.High,
                CompoundRefs: pair,
                EdgeRefs: new[] { e.EdgeId, contradiction.EdgeId }
                    .OrderBy(s => s, StringComparer.Ordinal).ToList(),
                Summary: $"Community claims positive {e.From}+{e.To} stack, but {tier}-tier source contradicts/cautions.",
                RecommendedAction: "human-review-community-vs-authority",
                NeedsHumanReview: true);
        }
    }

    private static bool PairMatches(CompoundGraphEdge a, CompoundGraphEdge b)
        => (a.From == b.From && a.To == b.To) || (a.From == b.To && a.To == b.From);

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private static CompoundGraphEdgeType MapRelationshipType(string relationshipType)
    {
        return relationshipType switch
        {
            "synergy" => CompoundGraphEdgeType.SynergizesWith,
            "complementary" => CompoundGraphEdgeType.Complements,
            "redundant" => CompoundGraphEdgeType.RedundantWith,
            "conflict" => CompoundGraphEdgeType.ConflictsWith,
            "contraindicated" => CompoundGraphEdgeType.AvoidWith,
            "caution" => CompoundGraphEdgeType.AvoidWith,
            "timing-sensitive" => CompoundGraphEdgeType.PairsWith,
            "dose-dependent" => CompoundGraphEdgeType.PairsWith,
            "mechanism-overlap" => CompoundGraphEdgeType.RedundantWith,
            "opposing-effect" => CompoundGraphEdgeType.OpposesEffect,
            "community-stack" => CompoundGraphEdgeType.HasCommunitySignal,
            "popular-but-unsupported" => CompoundGraphEdgeType.HasCommunitySignal,
            "vendor-claimed" => CompoundGraphEdgeType.HasCommunitySignal,
            "misinformation-pattern" => CompoundGraphEdgeType.HasCommunitySignal,
            _ => CompoundGraphEdgeType.PairsWith,
        };
    }

    private static bool IsRiskClaimType(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType)) return false;
        var lower = claimType.ToLowerInvariant();
        return lower.Contains("risk")
            || lower.Contains("adverse")
            || lower.Contains("warning")
            || lower.Contains("contraindication")
            || lower.Contains("misinformation");
    }

    private static void AddCompoundNode(
        Dictionary<string, CompoundGraphNode> nodes,
        string canonicalName,
        IReadOnlyList<string> aliases)
    {
        var id = CompoundNodeId(canonicalName);
        if (nodes.TryGetValue(id, out var existing))
        {
            // Merge aliases.
            var merged = existing.Aliases
                .Concat(aliases ?? Array.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a, StringComparer.Ordinal)
                .ToList();
            nodes[id] = existing with { Aliases = merged };
            return;
        }

        var sortedAliases = (aliases ?? Array.Empty<string>())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a, StringComparer.Ordinal)
            .ToList();

        nodes[id] = new CompoundGraphNode(
            NodeId: id,
            NodeType: CompoundGraphNodeType.Compound,
            Label: canonicalName,
            Aliases: sortedAliases,
            Metadata: EmptyMetadata());
    }

    private static void AddCategoryEdge(
        Dictionary<string, CompoundGraphNode> nodes,
        Dictionary<string, CompoundGraphEdge> edges,
        string canonicalName,
        string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return;
        var catId = $"category:{Slug(category)}";
        AddNode(nodes, new CompoundGraphNode(
            NodeId: catId,
            NodeType: CompoundGraphNodeType.Category,
            Label: category,
            Aliases: Array.Empty<string>(),
            Metadata: EmptyMetadata()));

        var fromId = CompoundNodeId(canonicalName);
        var edgeId = $"belongs-to-category:{Slug(canonicalName)}:{Slug(category)}";
        AddSimpleEdge(edges, edgeId,
            from: fromId,
            to: catId,
            edgeType: CompoundGraphEdgeType.BelongsToCategory,
            relationshipType: null);
    }

    private static void AddNode(Dictionary<string, CompoundGraphNode> nodes, CompoundGraphNode node)
    {
        if (!nodes.ContainsKey(node.NodeId))
        {
            nodes[node.NodeId] = node;
        }
    }

    private static void AddSimpleEdge(
        Dictionary<string, CompoundGraphEdge> edges,
        string edgeId,
        string from,
        string to,
        CompoundGraphEdgeType edgeType,
        string? relationshipType)
    {
        if (edges.ContainsKey(edgeId)) return;
        edges[edgeId] = new CompoundGraphEdge(
            EdgeId: edgeId,
            From: from,
            To: to,
            EdgeType: edgeType,
            RelationshipType: relationshipType,
            AssertedRelationshipType: relationshipType,
            EffectDomain: null,
            EvidenceTier: null,
            Confidence: null,
            SourceRefs: Array.Empty<string>(),
            ClaimRefs: Array.Empty<string>(),
            ReviewFlags: Array.Empty<string>(),
            NeedsReview: false,
            CommunitySignal: null,
            SourceAuthorityMix: SourceAuthorityMix.Empty);
    }

    private static CompoundGraphEdge SortEdgeCollections(CompoundGraphEdge e)
    {
        return e with
        {
            SourceRefs = e.SourceRefs.OrderBy(s => s, StringComparer.Ordinal).ToList(),
            ClaimRefs = e.ClaimRefs.OrderBy(s => s, StringComparer.Ordinal).ToList(),
            ReviewFlags = e.ReviewFlags.OrderBy(s => s, StringComparer.Ordinal).ToList(),
            SourceAuthorityMix = new SourceAuthorityMix(
                e.SourceAuthorityMix.AuthorityTiers.OrderBy(s => s, StringComparer.Ordinal).ToList()),
        };
    }

    private static string CompoundNodeId(string canonicalName) => $"compound:{Slug(canonicalName)}";

    private static string SourceFamilyNodeId(string tier) => $"source-family:tier-{tier.ToLowerInvariant()}";

    private static string Slug(string input) => SubstanceRecordNormalizer.Slugify(input ?? string.Empty);

    private static IReadOnlyDictionary<string, JsonNode?> EmptyMetadata()
        => new Dictionary<string, JsonNode?>(StringComparer.Ordinal);

    private static string ReadString(JsonNode? node)
    {
        if (node is null) return string.Empty;
        try { return node.GetValue<string>() ?? string.Empty; }
        catch { return string.Empty; }
    }

    private static IReadOnlyList<string> ReadStringArray(JsonNode? node)
    {
        if (node is not JsonArray arr) return Array.Empty<string>();
        var list = new List<string>();
        foreach (var item in arr)
        {
            if (item is null) continue;
            try
            {
                var v = item.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(v)) list.Add(v);
            }
            catch { /* skip non-strings */ }
        }
        return list;
    }

    private static SourceAuthorityMix? ReadPacketSourceAuthorityMix(JsonNode? node)
    {
        if (node is not JsonObject obj) return null;
        var tiers = ReadStringArray(obj["authorityTiers"]);
        return new SourceAuthorityMix(tiers);
    }

    private static CommunitySignal? ReadCommunitySignal(JsonNode? node)
    {
        if (node is not JsonObject obj) return null;
        bool present;
        try { present = obj["present"]?.GetValue<bool>() ?? false; }
        catch { present = false; }

        var strength = ParseEnum<CommunitySignalStrength>(ReadString(obj["signalStrength"])) ?? CommunitySignalStrength.None;
        var direction = ParseEnum<CommunitySignalDirection>(ReadString(obj["signalDirection"])) ?? CommunitySignalDirection.Unclear;
        var use = ParseEnum<CommunitySignalUse>(ReadString(obj["signalUse"]));
        var truth = ParseEnum<CanonicalTruthStatus>(ReadString(obj["canonicalTruthStatus"])) ?? CanonicalTruthStatus.Unknown;
        var notes = ReadString(obj["notes"]);

        return new CommunitySignal(
            Present: present,
            SignalStrength: strength,
            SignalDirection: direction,
            SignalUse: use,
            CanonicalTruthStatus: truth,
            Notes: notes.Length > 0 ? notes : null);
    }

    private static TEnum? ParseEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        // Accept kebab-case (e.g., "plausible-mechanistic") by stripping dashes.
        var normalized = value.Replace("-", "");
        if (Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    private static string? LookupAuthorityTier(string sourceRef, JsonArray? packetSources, JsonNode? sourceRegistry)
    {
        if (string.IsNullOrWhiteSpace(sourceRef)) return null;

        if (packetSources is not null)
        {
            foreach (var node in packetSources)
            {
                if (node is not JsonObject obj) continue;
                var id = obj["sourceId"]?.GetValue<string>();
                if (string.Equals(id, sourceRef, StringComparison.OrdinalIgnoreCase))
                {
                    return obj["authorityTier"]?.GetValue<string>();
                }
            }
        }

        if (sourceRegistry?["sources"] is JsonArray registrySources)
        {
            foreach (var node in registrySources)
            {
                if (node is not JsonObject obj) continue;
                var id = obj["sourceId"]?.GetValue<string>();
                if (string.Equals(id, sourceRef, StringComparison.OrdinalIgnoreCase))
                {
                    return obj["authorityTier"]?.GetValue<string>();
                }
            }
        }
        return null;
    }
}
