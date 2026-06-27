namespace BioStack.KnowledgeWorker.Pipeline.Graph;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioStack.Domain.Entities.Graph;

/// <summary>
/// Projects an offline <see cref="CompoundGraph"/> into the persisted Lane C entities
/// (<see cref="CompoundGraphArtifact"/> + relationships + findings) that runtime intelligence reads.
///
/// Pure and deterministic: the same graph always yields the same artifact hash, so re-publishing is
/// idempotent. The worker builds a graph DB-less in Research mode and writes <c>compound-graph.json</c>;
/// this mapper + <c>ICompoundGraphStore.PublishAsync</c> are the persistence path a DB-connected
/// worker run (or an importer) calls to materialize that graph into the application database.
/// </summary>
public static class CompoundGraphPersistenceMapper
{
    private const string CompoundNodePrefix = "compound:";

    /// <summary>The pairwise compound→compound edge types that become persisted relationships.</summary>
    private static readonly IReadOnlyDictionary<CompoundGraphEdgeType, string> RelationshipTypeMap =
        new Dictionary<CompoundGraphEdgeType, string>
        {
            [CompoundGraphEdgeType.SynergizesWith] = GraphRelationshipType.SynergizesWith,
            [CompoundGraphEdgeType.Complements] = GraphRelationshipType.PairsWellWith,
            [CompoundGraphEdgeType.PairsWith] = GraphRelationshipType.PairsWellWith,
            [CompoundGraphEdgeType.RedundantWith] = GraphRelationshipType.RedundantWith,
            [CompoundGraphEdgeType.ConflictsWith] = GraphRelationshipType.ConflictsWith,
            [CompoundGraphEdgeType.AvoidWith] = GraphRelationshipType.AvoidWith,
            [CompoundGraphEdgeType.OpposesEffect] = GraphRelationshipType.OpposesEffect,
            [CompoundGraphEdgeType.HasCommunitySignal] = GraphRelationshipType.UnknownOrInsufficientEvidence,
        };

    public sealed record Payload(
        CompoundGraphArtifact Artifact,
        IReadOnlyList<CompoundGraphRelationship> Relationships,
        IReadOnlyList<CompoundGraphFinding> Findings);

    public static Payload Map(CompoundGraph graph, string? sourceManifestHash = null)
    {
        ArgumentNullException.ThrowIfNull(graph);

        // Compound node id → canonical label, for resolving edge endpoints back to display names.
        var compoundLabels = graph.Nodes
            .Where(n => n.NodeType == CompoundGraphNodeType.Compound)
            .ToDictionary(n => n.NodeId, n => n.Label, StringComparer.Ordinal);

        var generatedAt = graph.GeneratedAtUtc.UtcDateTime;

        var relationships = new List<CompoundGraphRelationship>();
        foreach (var edge in graph.Edges)
        {
            if (!RelationshipTypeMap.TryGetValue(edge.EdgeType, out var relationshipType)) continue;
            if (!IsCompoundNode(edge.From) || !IsCompoundNode(edge.To)) continue;

            var subjectSlug = StripCompoundPrefix(edge.From);
            var objectSlug = StripCompoundPrefix(edge.To);

            relationships.Add(new CompoundGraphRelationship
            {
                SubjectCompound = compoundLabels.GetValueOrDefault(edge.From, subjectSlug),
                SubjectSlug = subjectSlug,
                ObjectCompound = compoundLabels.GetValueOrDefault(edge.To, objectSlug),
                ObjectSlug = objectSlug,
                RelationshipType = relationshipType,
                Directionality = edge.EdgeType == CompoundGraphEdgeType.OpposesEffect
                    ? GraphRelationshipType.Directed
                    : GraphRelationshipType.Bidirectional,
                Confidence = edge.Confidence,
                EvidenceTier = edge.EvidenceTier,
                SourceRefsJson = Serialize(edge.SourceRefs),
                Reason = BuildReason(edge, relationshipType),
                SafetyConcernLevel = MapSafetyConcern(relationshipType),
                ReviewState = edge.NeedsReview ? "needs-review" : "reviewed",
                NeedsReview = edge.NeedsReview,
                CreatedAtUtc = generatedAt,
            });
        }

        var findings = new List<CompoundGraphFinding>();
        foreach (var f in graph.ReviewFindings)
        {
            findings.Add(new CompoundGraphFinding
            {
                FindingType = f.FindingType.ToString(),
                Severity = f.Severity.ToString(),
                SubjectCompound = ResolveCompound(f.CompoundRefs, 0, compoundLabels),
                ObjectCompound = ResolveCompound(f.CompoundRefs, 1, compoundLabels),
                Pathway = null,
                Reason = f.Summary,
                EvidenceRefsJson = Serialize(f.EdgeRefs),
                RecommendedAction = f.RecommendedAction,
                CreatedAtUtc = generatedAt,
            });
        }

        var artifactHash = ComputeArtifactHash(graph.GraphVersion, relationships, findings);

        var artifact = new CompoundGraphArtifact
        {
            ArtifactHash = artifactHash,
            BuilderVersion = graph.GraphVersion,
            GeneratedAtUtc = generatedAt,
            SourceManifestHash = sourceManifestHash,
            ReviewState = "provisional",
            RelationshipCount = relationships.Count,
            FindingCount = findings.Count,
            MetadataJson = Serialize(new Dictionary<string, object?>
            {
                ["nodes"] = graph.Counts.Nodes,
                ["edges"] = graph.Counts.Edges,
                ["reviewRequiredEdges"] = graph.Counts.ReviewRequiredEdges,
                ["communitySignalEdges"] = graph.Counts.CommunitySignalEdges,
                ["conflictEdges"] = graph.Counts.ConflictEdges,
            }),
            CreatedAtUtc = generatedAt,
        };

        return new Payload(artifact, relationships, findings);
    }

    private static bool IsCompoundNode(string nodeId)
        => nodeId.StartsWith(CompoundNodePrefix, StringComparison.Ordinal);

    private static string StripCompoundPrefix(string nodeId)
        => nodeId.StartsWith(CompoundNodePrefix, StringComparison.Ordinal)
            ? nodeId[CompoundNodePrefix.Length..]
            : nodeId;

    private static string? ResolveCompound(
        IReadOnlyList<string> refs,
        int index,
        IReadOnlyDictionary<string, string> compoundLabels)
    {
        if (refs.Count <= index) return null;
        var id = refs[index];
        return compoundLabels.GetValueOrDefault(id, StripCompoundPrefix(id));
    }

    private static string BuildReason(CompoundGraphEdge edge, string relationshipType)
    {
        if (edge.CommunitySignal is { Present: true, Notes: { Length: > 0 } notes })
        {
            return notes;
        }

        return relationshipType switch
        {
            GraphRelationshipType.SynergizesWith => "Reviewed synergy relationship in the compound graph.",
            GraphRelationshipType.PairsWellWith => "Reviewed complementary pairing in the compound graph.",
            GraphRelationshipType.RedundantWith => "Reviewed redundancy (overlapping action) in the compound graph.",
            GraphRelationshipType.ConflictsWith => "Reviewed conflict relationship — review before combining.",
            GraphRelationshipType.AvoidWith => "Reviewed avoid-with relationship — warning-first.",
            GraphRelationshipType.OpposesEffect => "Reviewed opposing-effect relationship in the compound graph.",
            GraphRelationshipType.UnknownOrInsufficientEvidence => "Community-signal pairing with insufficient reviewed evidence.",
            _ => "Reviewed compound-graph relationship.",
        };
    }

    private static string MapSafetyConcern(string relationshipType) => relationshipType switch
    {
        GraphRelationshipType.ConflictsWith => GraphRelationshipType.SafetyConcern.High,
        GraphRelationshipType.AvoidWith => GraphRelationshipType.SafetyConcern.High,
        GraphRelationshipType.OpposesEffect => GraphRelationshipType.SafetyConcern.Caution,
        GraphRelationshipType.RedundantWith => GraphRelationshipType.SafetyConcern.Low,
        GraphRelationshipType.SynergizesWith => GraphRelationshipType.SafetyConcern.None,
        GraphRelationshipType.PairsWellWith => GraphRelationshipType.SafetyConcern.None,
        GraphRelationshipType.UnknownOrInsufficientEvidence => GraphRelationshipType.SafetyConcern.Unknown,
        _ => GraphRelationshipType.SafetyConcern.Unknown,
    };

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);

    /// <summary>
    /// Deterministic content hash over the relationship/finding triples + builder version. Stable
    /// across runs for identical input so the store can treat re-publishes as idempotent and a
    /// receipt can cite a meaningful <c>compound-graph:{hash}</c>.
    /// </summary>
    private static string ComputeArtifactHash(
        string graphVersion,
        IReadOnlyList<CompoundGraphRelationship> relationships,
        IReadOnlyList<CompoundGraphFinding> findings)
    {
        var sb = new StringBuilder();
        sb.Append("v=").Append(graphVersion).Append('\n');

        foreach (var r in relationships
            .OrderBy(r => r.SubjectSlug, StringComparer.Ordinal)
            .ThenBy(r => r.ObjectSlug, StringComparer.Ordinal)
            .ThenBy(r => r.RelationshipType, StringComparer.Ordinal))
        {
            sb.Append("r=").Append(r.SubjectSlug).Append('|').Append(r.ObjectSlug)
              .Append('|').Append(r.RelationshipType)
              .Append('|').Append(r.EvidenceTier ?? string.Empty)
              .Append('|').Append(r.Confidence ?? string.Empty).Append('\n');
        }

        foreach (var f in findings
            .OrderBy(f => f.FindingType, StringComparer.Ordinal)
            .ThenBy(f => f.SubjectCompound ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(f => f.ObjectCompound ?? string.Empty, StringComparer.Ordinal))
        {
            sb.Append("f=").Append(f.FindingType).Append('|').Append(f.Severity)
              .Append('|').Append(f.SubjectCompound ?? string.Empty)
              .Append('|').Append(f.ObjectCompound ?? string.Empty).Append('\n');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return "sha256:" + Convert.ToHexStringLower(bytes);
    }
}
