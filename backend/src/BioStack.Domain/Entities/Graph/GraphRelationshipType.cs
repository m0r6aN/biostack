namespace BioStack.Domain.Entities.Graph;

/// <summary>
/// Canon relationship vocabulary for persisted compound-graph edges (Lane C).
///
/// These are the stable, reviewed relationship-type strings that runtime intelligence reads
/// from <see cref="CompoundGraphRelationship"/>. They mirror the canon §Relationship Semantics
/// families and are deliberately observational/educational — none imply prescribing, dosing,
/// or effect-bearing guidance. The offline <c>CompoundGraphBuilder</c> edge-type enum is mapped
/// into this vocabulary at persistence time so that Lane B can later normalize into a full
/// <c>RelationshipEdge</c> schema without renaming the canonical types.
/// </summary>
public static class GraphRelationshipType
{
    public const string SynergizesWith = "synergizes_with";
    public const string PairsWellWith = "pairs_well_with";
    public const string ConflictsWith = "conflicts_with";
    public const string AvoidWith = "avoid_with";
    public const string RedundantWith = "redundant_with";
    public const string OpposesEffect = "opposes_effect";
    public const string SharedPathwayAdditiveRisk = "shared_pathway_additive_risk";
    public const string RequiresSupport = "requires_support";
    public const string RequiresMonitoring = "requires_monitoring";
    public const string UnknownOrInsufficientEvidence = "unknown_or_insufficient_evidence";

    /// <summary>Directionality marker: the relationship reads the same in both directions.</summary>
    public const string Bidirectional = "bidirectional";

    /// <summary>Directionality marker: subject → object is meaningful and not symmetric.</summary>
    public const string Directed = "directed";

    /// <summary>
    /// Safety-concern tiers for a relationship. Caution/avoidance families warrant warning-first
    /// framing on user surfaces; positive/neutral families do not.
    /// </summary>
    public static class SafetyConcern
    {
        public const string None = "none";
        public const string Low = "low";
        public const string Caution = "caution";
        public const string High = "high";
        public const string Unknown = "unknown";
    }
}
