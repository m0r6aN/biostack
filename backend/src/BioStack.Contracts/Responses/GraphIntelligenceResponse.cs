namespace BioStack.Contracts.Responses;

/// <summary>
/// Source of a relationship result, so callers can distinguish a reviewed graph-backed answer
/// from a weaker fallback inference (Lane C, design principle "one reviewed truth source").
/// </summary>
public static class IntelligenceSource
{
    /// <summary>Came from the reviewed/materialized compound graph.</summary>
    public const string Graph = "graph";

    /// <summary>Derived from denormalized KnowledgeEntry fields — disclose weaker confidence.</summary>
    public const string Fallback = "fallback";
}

/// <summary>
/// One graph-backed (or fallback) relationship between two compounds, with full provenance so a
/// user can see which graph artifact and evidence supported it.
/// </summary>
public sealed record GraphRelationshipResponse(
    string SubjectCompound,
    string ObjectCompound,
    string RelationshipType,
    string? Confidence,
    string? EvidenceTier,
    IReadOnlyList<string> SourceRefs,
    string? Reason,
    string SafetyConcernLevel,
    string Directionality,
    bool NeedsReview,
    string? GraphArtifactHash,
    DateTime? GeneratedAtUtc,
    string Source);

/// <summary>
/// All reviewed relationships touching a single compound, with the graph artifact provenance and
/// whether the result is graph-backed or fallback.
/// </summary>
public sealed record CompoundRelationshipsResponse(
    string Compound,
    string Source,
    string? GraphArtifactHash,
    DateTime? GeneratedAtUtc,
    IReadOnlyList<GraphRelationshipResponse> Relationships,
    // Lane H safety-gate metadata (additive). Set when the response passes the user-facing
    // intelligence gate; SafetyStatus is "allowed" until the gate evaluates the output.
    string SafetyStatus = Responses.SafetyStatus.Allowed,
    IReadOnlyList<string>? Warnings = null,
    IReadOnlyList<string>? PolicyRefs = null,
    string? SafetyReceiptId = null);

/// <summary>
/// Pairwise compatibility across a set of compounds, assembled from the reviewed graph where edges
/// exist (graph-backed) and disclosed as fallback/missing-evidence otherwise.
/// </summary>
public sealed record CompoundCompatibilityResponse(
    IReadOnlyList<string> Compounds,
    string Source,
    string? GraphArtifactHash,
    DateTime? GeneratedAtUtc,
    IReadOnlyList<GraphRelationshipResponse> Relationships,
    // Lane H safety-gate metadata (additive). Set when the response passes the user-facing
    // intelligence gate; SafetyStatus is "allowed" until the gate evaluates the output.
    string SafetyStatus = Responses.SafetyStatus.Allowed,
    IReadOnlyList<string>? Warnings = null,
    IReadOnlyList<string>? PolicyRefs = null,
    string? SafetyReceiptId = null);
