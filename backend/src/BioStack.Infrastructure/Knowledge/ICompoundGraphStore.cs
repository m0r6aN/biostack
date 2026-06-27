namespace BioStack.Infrastructure.Knowledge;

using BioStack.Domain.Entities.Graph;

/// <summary>
/// Read/write access to the materialized compound graph (Lane C).
///
/// Writes come from the offline worker (publishing a new <see cref="CompoundGraphArtifact"/>
/// and its relationships/findings); reads come from runtime intelligence, which prefers reviewed
/// graph relationships over ad-hoc inference from denormalized <c>KnowledgeEntry</c> strings.
/// Reads always resolve against the single active artifact.
/// </summary>
public interface ICompoundGraphStore
{
    /// <summary>
    /// Persist a new graph artifact with its relationships and findings, and make it the active
    /// artifact (deactivating any prior active one). Returns the saved artifact.
    /// </summary>
    Task<CompoundGraphArtifact> PublishAsync(
        CompoundGraphArtifact artifact,
        IReadOnlyList<CompoundGraphRelationship> relationships,
        IReadOnlyList<CompoundGraphFinding> findings,
        CancellationToken cancellationToken = default);

    /// <summary>The currently active artifact, or null if no graph has been published.</summary>
    Task<CompoundGraphArtifact?> GetActiveArtifactAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// All relationships in the active artifact that touch the given compound slug (as subject or
    /// object). Empty when no active graph or no edges for the compound.
    /// </summary>
    Task<IReadOnlyList<CompoundGraphRelationship>> GetRelationshipsForCompoundAsync(
        string compoundSlug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The reviewed relationship between two compound slugs in the active artifact, or null if the
    /// graph has no edge for the pair. Order-independent.
    /// </summary>
    Task<CompoundGraphRelationship?> FindRelationshipAsync(
        string slugA,
        string slugB,
        CancellationToken cancellationToken = default);
}
