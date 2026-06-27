namespace BioStack.Application.Services.Intelligence;

using System.Text.Json;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities.Graph;
using BioStack.Infrastructure.Knowledge;

/// <summary>
/// Graph-backed read surface for compound relationships and compatibility (Lane C).
///
/// Prefers the reviewed/materialized compound graph as the single truth source. Where the graph has
/// no edge for a pair, the result is disclosed as fallback / unknown-evidence rather than fabricated
/// — per canon "Unknown beats inference".
/// </summary>
public interface IGraphIntelligenceService
{
    Task<CompoundRelationshipsResponse> GetRelationshipsForCompoundAsync(
        string compound,
        CancellationToken cancellationToken = default);

    Task<CompoundCompatibilityResponse> GetCompatibilityAsync(
        IReadOnlyList<string> compounds,
        CancellationToken cancellationToken = default);
}

public sealed class GraphIntelligenceService : IGraphIntelligenceService
{
    private readonly ICompoundGraphStore _graphStore;

    public GraphIntelligenceService(ICompoundGraphStore graphStore)
    {
        _graphStore = graphStore;
    }

    public async Task<CompoundRelationshipsResponse> GetRelationshipsForCompoundAsync(
        string compound,
        CancellationToken cancellationToken = default)
    {
        var artifact = await _graphStore.GetActiveArtifactAsync(cancellationToken);
        var relationships = await _graphStore.GetRelationshipsForCompoundAsync(compound, cancellationToken);

        var source = artifact is not null && relationships.Count > 0
            ? IntelligenceSource.Graph
            : IntelligenceSource.Fallback;

        var mapped = relationships
            .Select(r => MapRelationship(r, artifact))
            .ToList();

        return new CompoundRelationshipsResponse(
            Compound: compound,
            Source: source,
            GraphArtifactHash: artifact?.ArtifactHash,
            GeneratedAtUtc: artifact?.GeneratedAtUtc,
            Relationships: mapped);
    }

    public async Task<CompoundCompatibilityResponse> GetCompatibilityAsync(
        IReadOnlyList<string> compounds,
        CancellationToken cancellationToken = default)
    {
        var distinct = compounds
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var artifact = await _graphStore.GetActiveArtifactAsync(cancellationToken);
        var results = new List<GraphRelationshipResponse>();
        var anyGraphBacked = false;

        for (var i = 0; i < distinct.Count; i++)
        {
            for (var j = i + 1; j < distinct.Count; j++)
            {
                var a = distinct[i];
                var b = distinct[j];
                var edge = artifact is null
                    ? null
                    : await _graphStore.FindRelationshipAsync(a, b, cancellationToken);

                if (edge is not null)
                {
                    anyGraphBacked = true;
                    results.Add(MapRelationship(edge, artifact));
                }
                else
                {
                    // No reviewed edge — disclose missing graph evidence rather than infer.
                    results.Add(new GraphRelationshipResponse(
                        SubjectCompound: a,
                        ObjectCompound: b,
                        RelationshipType: GraphRelationshipType.UnknownOrInsufficientEvidence,
                        Confidence: null,
                        EvidenceTier: null,
                        SourceRefs: Array.Empty<string>(),
                        Reason: "No reviewed graph relationship exists for this pair.",
                        SafetyConcernLevel: GraphRelationshipType.SafetyConcern.Unknown,
                        Directionality: GraphRelationshipType.Bidirectional,
                        NeedsReview: false,
                        GraphArtifactHash: artifact?.ArtifactHash,
                        GeneratedAtUtc: artifact?.GeneratedAtUtc,
                        Source: IntelligenceSource.Fallback));
                }
            }
        }

        return new CompoundCompatibilityResponse(
            Compounds: distinct,
            Source: anyGraphBacked ? IntelligenceSource.Graph : IntelligenceSource.Fallback,
            GraphArtifactHash: artifact?.ArtifactHash,
            GeneratedAtUtc: artifact?.GeneratedAtUtc,
            Relationships: results);
    }

    private static GraphRelationshipResponse MapRelationship(
        CompoundGraphRelationship r,
        CompoundGraphArtifact? artifact)
        => new(
            SubjectCompound: r.SubjectCompound,
            ObjectCompound: r.ObjectCompound,
            RelationshipType: r.RelationshipType,
            Confidence: r.Confidence,
            EvidenceTier: r.EvidenceTier,
            SourceRefs: DeserializeRefs(r.SourceRefsJson),
            Reason: r.Reason,
            SafetyConcernLevel: r.SafetyConcernLevel,
            Directionality: r.Directionality,
            NeedsReview: r.NeedsReview,
            GraphArtifactHash: artifact?.ArtifactHash,
            GeneratedAtUtc: artifact?.GeneratedAtUtc,
            Source: IntelligenceSource.Graph);

    internal static IReadOnlyList<string> DeserializeRefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
