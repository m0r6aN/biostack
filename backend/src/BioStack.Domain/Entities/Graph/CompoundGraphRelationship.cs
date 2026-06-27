namespace BioStack.Domain.Entities.Graph;

/// <summary>
/// One reviewed pairwise relationship between two compounds, materialized from a graph edge
/// (Lane C). This is the truth source runtime intelligence prefers over ad-hoc inference from
/// denormalized <c>KnowledgeEntry</c> string lists.
///
/// Provenance is preserved per the Lane C design principle: which substances, what relationship
/// type, what evidence/source refs supported it, what confidence/review status applies, and which
/// <see cref="CompoundGraphArtifact"/> (hash) produced it.
/// </summary>
public sealed class CompoundGraphRelationship
{
    public Guid Id { get; set; }

    public Guid GraphArtifactId { get; set; }
    public CompoundGraphArtifact? GraphArtifact { get; set; }

    /// <summary>Subject compound canonical/display name.</summary>
    public string SubjectCompound { get; set; } = string.Empty;

    /// <summary>Subject compound slug (deterministic lower-kebab) — the stable lookup key.</summary>
    public string SubjectSlug { get; set; } = string.Empty;

    public string ObjectCompound { get; set; } = string.Empty;
    public string ObjectSlug { get; set; } = string.Empty;

    /// <summary>Canon relationship type from <see cref="GraphRelationshipType"/>.</summary>
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary><c>bidirectional</c> or <c>directed</c>.</summary>
    public string Directionality { get; set; } = GraphRelationshipType.Bidirectional;

    /// <summary>Builder-provided confidence label (e.g. <c>high</c>/<c>moderate</c>), preserved verbatim.</summary>
    public string? Confidence { get; set; }

    /// <summary>Evidence tier from the source edge (e.g. <c>Strong</c>/<c>Anecdotal</c>).</summary>
    public string? EvidenceTier { get; set; }

    /// <summary>Source/evidence refs that support the relationship, as a JSON string array.</summary>
    public string SourceRefsJson { get; set; } = "[]";

    /// <summary>Human-readable, observational rationale for the relationship.</summary>
    public string? Reason { get; set; }

    /// <summary>Safety-concern tier from <see cref="GraphRelationshipType.SafetyConcern"/>.</summary>
    public string SafetyConcernLevel { get; set; } = GraphRelationshipType.SafetyConcern.None;

    /// <summary>Review state inherited/derived from the edge (e.g. <c>reviewed</c>, <c>needs-review</c>).</summary>
    public string ReviewState { get; set; } = "reviewed";

    /// <summary>Whether the source edge was flagged as requiring human review.</summary>
    public bool NeedsReview { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
