namespace BioStack.Infrastructure.Knowledge;

using BioStack.Domain.Entities.Graph;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class CompoundGraphStore : ICompoundGraphStore
{
    private readonly BioStackDbContext _dbContext;

    public CompoundGraphStore(BioStackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CompoundGraphArtifact> PublishAsync(
        CompoundGraphArtifact artifact,
        IReadOnlyList<CompoundGraphRelationship> relationships,
        IReadOnlyList<CompoundGraphFinding> findings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        // An artifact hash uniquely identifies a build. Re-publishing the same hash is a no-op
        // beyond ensuring it is the active artifact (idempotent worker re-runs).
        var existing = await _dbContext.CompoundGraphArtifacts
            .FirstOrDefaultAsync(a => a.ArtifactHash == artifact.ArtifactHash, cancellationToken);
        if (existing is not null)
        {
            await SetActiveAsync(existing.Id, cancellationToken);
            return existing;
        }

        if (artifact.Id == Guid.Empty) artifact.Id = Guid.NewGuid();
        if (artifact.CreatedAtUtc == default) artifact.CreatedAtUtc = DateTime.UtcNow;
        artifact.RelationshipCount = relationships.Count;
        artifact.FindingCount = findings.Count;

        foreach (var relationship in relationships)
        {
            if (relationship.Id == Guid.Empty) relationship.Id = Guid.NewGuid();
            relationship.GraphArtifactId = artifact.Id;
            relationship.GraphArtifact = null;
            if (relationship.CreatedAtUtc == default) relationship.CreatedAtUtc = artifact.CreatedAtUtc;
        }

        foreach (var finding in findings)
        {
            if (finding.Id == Guid.Empty) finding.Id = Guid.NewGuid();
            finding.GraphArtifactId = artifact.Id;
            finding.GraphArtifact = null;
            if (finding.CreatedAtUtc == default) finding.CreatedAtUtc = artifact.CreatedAtUtc;
        }

        // New publish becomes the sole active artifact.
        await _dbContext.CompoundGraphArtifacts
            .Where(a => a.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, false), cancellationToken);

        artifact.IsActive = true;
        artifact.Relationships = relationships.ToList();
        artifact.Findings = findings.ToList();

        await _dbContext.CompoundGraphArtifacts.AddAsync(artifact, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return artifact;
    }

    public Task<CompoundGraphArtifact?> GetActiveArtifactAsync(CancellationToken cancellationToken = default)
        => _dbContext.CompoundGraphArtifacts
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.GeneratedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CompoundGraphRelationship>> GetRelationshipsForCompoundAsync(
        string compoundSlug,
        CancellationToken cancellationToken = default)
    {
        var slug = CompoundSlug.From(compoundSlug);
        if (slug.Length == 0) return Array.Empty<CompoundGraphRelationship>();

        var active = await GetActiveArtifactAsync(cancellationToken);
        if (active is null) return Array.Empty<CompoundGraphRelationship>();

        return await _dbContext.CompoundGraphRelationships
            .AsNoTracking()
            .Where(r => r.GraphArtifactId == active.Id
                        && (r.SubjectSlug == slug || r.ObjectSlug == slug))
            .OrderBy(r => r.SubjectSlug)
            .ThenBy(r => r.ObjectSlug)
            .ToListAsync(cancellationToken);
    }

    public async Task<CompoundGraphRelationship?> FindRelationshipAsync(
        string slugA,
        string slugB,
        CancellationToken cancellationToken = default)
    {
        var a = CompoundSlug.From(slugA);
        var b = CompoundSlug.From(slugB);
        if (a.Length == 0 || b.Length == 0) return null;

        var active = await GetActiveArtifactAsync(cancellationToken);
        if (active is null) return null;

        return await _dbContext.CompoundGraphRelationships
            .AsNoTracking()
            .Where(r => r.GraphArtifactId == active.Id
                        && ((r.SubjectSlug == a && r.ObjectSlug == b)
                            || (r.SubjectSlug == b && r.ObjectSlug == a)))
            // Prefer the strongest safety signal when multiple edges exist for a pair: caution/
            // conflict families sort ahead of positive/neutral ones so runtime never under-warns.
            .OrderByDescending(r => r.SafetyConcernLevel == "high")
            .ThenByDescending(r => r.SafetyConcernLevel == "caution")
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SetActiveAsync(Guid artifactId, CancellationToken cancellationToken)
    {
        await _dbContext.CompoundGraphArtifacts
            .Where(a => a.IsActive && a.Id != artifactId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, false), cancellationToken);
        await _dbContext.CompoundGraphArtifacts
            .Where(a => a.Id == artifactId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, true), cancellationToken);
    }
}
