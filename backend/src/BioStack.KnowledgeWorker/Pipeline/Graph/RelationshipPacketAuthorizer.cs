namespace BioStack.KnowledgeWorker.Pipeline.Graph;

using System.Text.Json.Nodes;

public interface IRelationshipPacketAuthorizer
{
    SourceAuthorityMix ComputeAuthorityMix(
        IEnumerable<string> sourceRefs,
        JsonArray? packetSources,
        JsonNode? sourceRegistry,
        out IReadOnlyList<string> unmappedSourceRefs);

    CompoundGraphEdge EnforcePolicy(
        CompoundGraphEdge edge,
        SourceAuthorityMix computedMix,
        SourceAuthorityMix? packetProvidedMix,
        IReadOnlyList<string> unmappedSourceRefs);
}

/// <summary>
/// Computes authority mix and enforces quarantine policy for relationship edges.
/// Trusts only the source registry / packet sources[] — never the packet-provided
/// sourceAuthorityMix, which is treated as advisory and cross-checked.
/// </summary>
public sealed class RelationshipPacketAuthorizer : IRelationshipPacketAuthorizer
{
    private static readonly HashSet<string> CommunityRelationshipTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "community-stack",
        "popular-but-unsupported",
        "vendor-claimed",
        "misinformation-pattern",
    };

    private static readonly HashSet<string> DowngradableEvidenceTiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Strong",
        "Moderate",
        "Limited",
    };

    public SourceAuthorityMix ComputeAuthorityMix(
        IEnumerable<string> sourceRefs,
        JsonArray? packetSources,
        JsonNode? sourceRegistry,
        out IReadOnlyList<string> unmappedSourceRefs)
    {
        if (sourceRefs is null) throw new ArgumentNullException(nameof(sourceRefs));

        var tiers = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var unmapped = new List<string>();

        foreach (var rawRef in sourceRefs)
        {
            if (string.IsNullOrWhiteSpace(rawRef)) continue;
            var sourceRef = rawRef.Trim();

            var tier = LookupTier(sourceRef, packetSources)
                       ?? LookupTier(sourceRef, sourceRegistry);

            if (string.IsNullOrWhiteSpace(tier))
            {
                if (!unmapped.Contains(sourceRef, StringComparer.OrdinalIgnoreCase))
                {
                    unmapped.Add(sourceRef);
                }
            }
            else
            {
                tiers.Add(tier!);
            }
        }

        unmappedSourceRefs = unmapped
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        return new SourceAuthorityMix(tiers.ToList());
    }

    public CompoundGraphEdge EnforcePolicy(
        CompoundGraphEdge edge,
        SourceAuthorityMix computedMix,
        SourceAuthorityMix? packetProvidedMix,
        IReadOnlyList<string> unmappedSourceRefs)
    {
        if (edge is null) throw new ArgumentNullException(nameof(edge));
        if (computedMix is null) throw new ArgumentNullException(nameof(computedMix));
        unmappedSourceRefs ??= Array.Empty<string>();

        var reviewFlags = new SortedSet<string>(edge.ReviewFlags, StringComparer.Ordinal);
        var needsReview = edge.NeedsReview;
        var edgeType = edge.EdgeType;
        var confidence = edge.Confidence;
        var evidenceTier = edge.EvidenceTier;
        var assertedRelationshipType = edge.AssertedRelationshipType ?? edge.RelationshipType;

        // Quarantine: low-authority-only mix for non-community relationship types.
        var relType = edge.RelationshipType ?? string.Empty;
        var isCommunityType = CommunityRelationshipTypes.Contains(relType);

        if (computedMix.IsLowAuthorityOnly && !isCommunityType)
        {
            needsReview = true;

            confidence = string.Equals(confidence, "unknown", StringComparison.OrdinalIgnoreCase)
                ? "unknown"
                : "low";

            if (evidenceTier is not null && DowngradableEvidenceTiers.Contains(evidenceTier))
            {
                evidenceTier = "Anecdotal";
            }
            else if (string.IsNullOrWhiteSpace(evidenceTier))
            {
                evidenceTier = "Unknown";
            }

            reviewFlags.Add("low-authority-relationship-claim");
            edgeType = CompoundGraphEdgeType.HasCommunitySignal;
            // Preserve original on AssertedRelationshipType.
            assertedRelationshipType = edge.RelationshipType;
        }

        // Mismatch: packet-provided mix differs from computed.
        if (packetProvidedMix is not null
            && !TierSetsEqual(packetProvidedMix.AuthorityTiers, computedMix.AuthorityTiers))
        {
            reviewFlags.Add("source-authority-mix-mismatch");
        }

        // Unmapped source refs.
        if (unmappedSourceRefs.Count > 0)
        {
            reviewFlags.Add("unmapped-source-ref");
            needsReview = true;
        }

        return edge with
        {
            EdgeType = edgeType,
            Confidence = confidence,
            EvidenceTier = evidenceTier,
            AssertedRelationshipType = assertedRelationshipType,
            ReviewFlags = reviewFlags.ToList(),
            NeedsReview = needsReview,
            SourceAuthorityMix = computedMix,
        };
    }

    private static bool TierSetsEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var lSet = new HashSet<string>(left, StringComparer.OrdinalIgnoreCase);
        var rSet = new HashSet<string>(right, StringComparer.OrdinalIgnoreCase);
        return lSet.SetEquals(rSet);
    }

    private static string? LookupTier(string sourceRef, JsonArray? sources)
    {
        if (sources is null) return null;
        foreach (var node in sources)
        {
            if (node is not JsonObject obj) continue;
            var id = obj["sourceId"]?.GetValue<string>();
            if (string.Equals(id, sourceRef, StringComparison.OrdinalIgnoreCase))
            {
                return obj["authorityTier"]?.GetValue<string>();
            }
        }
        return null;
    }

    private static string? LookupTier(string sourceRef, JsonNode? sourceRegistry)
    {
        if (sourceRegistry is null) return null;
        try
        {
            var registrySources = sourceRegistry["sources"] as JsonArray;
            return LookupTier(sourceRef, registrySources);
        }
        catch
        {
            return null;
        }
    }
}
