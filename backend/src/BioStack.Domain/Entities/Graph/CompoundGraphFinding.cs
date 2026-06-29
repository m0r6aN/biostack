namespace BioStack.Domain.Entities.Graph;

/// <summary>
/// A materialized review finding from the offline graph (Lane C) — e.g. a synergy chain that
/// contains a conflict, or a shared-pathway additive-risk signal. Findings are higher-order
/// observations spanning one or more compounds and are surfaced warning-first.
/// </summary>
public sealed class CompoundGraphFinding
{
    public Guid Id { get; set; }

    public Guid GraphArtifactId { get; set; }
    public CompoundGraphArtifact? GraphArtifact { get; set; }

    /// <summary>Finding type (e.g. <c>SharedPathwayAdditiveRisk</c>), preserved from the builder enum.</summary>
    public string FindingType { get; set; } = string.Empty;

    /// <summary>Severity (e.g. <c>High</c>/<c>Moderate</c>).</summary>
    public string Severity { get; set; } = string.Empty;

    public string? SubjectCompound { get; set; }
    public string? ObjectCompound { get; set; }

    /// <summary>Pathway/effect-domain implicated by the finding, when applicable.</summary>
    public string? Pathway { get; set; }

    /// <summary>Observational summary of the finding.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Supporting edge/evidence refs as a JSON string array.</summary>
    public string EvidenceRefsJson { get; set; } = "[]";

    /// <summary>Recommended human-review action carried from the builder.</summary>
    public string? RecommendedAction { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
