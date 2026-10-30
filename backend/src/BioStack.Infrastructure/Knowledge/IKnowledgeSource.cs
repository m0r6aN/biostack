namespace BioStack.Infrastructure.Knowledge;

using BioStack.Domain.Entities;

public interface IKnowledgeSource
{
    Task<KnowledgeEntry?> GetCompoundAsync(string name, CancellationToken cancellationToken = default);
    Task<List<KnowledgeEntry>> GetAllCompoundsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Public compound list. Returns <see cref="KnowledgeEntry.Id"/> and the public response
    /// columns only. Dose, schedule, pairing, and optimization columns stay on
    /// <see cref="GetAllCompoundsAsync"/>.
    /// </summary>
    Task<List<KnowledgeEntry>> ListPublicCompoundsAsync(CancellationToken cancellationToken = default);
    Task<List<KnowledgeEntry>> SearchCompoundsByPathwayAsync(string pathway, CancellationToken cancellationToken = default);
    Task<KnowledgeUpsertDisposition> UpsertCompoundAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default);
    Task<int> IngestBulkAsync(List<KnowledgeEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only equivalent of <see cref="UpsertCompoundAsync"/>: returns the disposition
    /// (Created/Updated/Unchanged) that a real upsert would produce, without adding,
    /// tracking-for-update, or saving any change. Used by DryRun so its summary counters
    /// and per-record table reflect a real diff instead of always reading zero.
    /// </summary>
    Task<KnowledgeUpsertDisposition> PreviewUpsertAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that are currently stored and not retired (public or hidden draft), as canonical name plus aliases.
    /// Refresh uses this to find records that are no longer in the seed. The default returns
    /// nothing, so sources without retirement support never retire anything.
    /// </summary>
    Task<IReadOnlyList<KnowledgeEntryIdentity>> ListActiveIdentitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnowledgeEntryIdentity>>(Array.Empty<KnowledgeEntryIdentity>());

    /// <summary>
    /// Auto-discovered drafts that are hidden from every public read until the AI council approves them
    /// (<see cref="KnowledgeEntry.IsPublic"/> = false). Admin review queue only. The default returns nothing.
    /// </summary>
    Task<IReadOnlyList<KnowledgeEntry>> ListUnreviewedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnowledgeEntry>>(Array.Empty<KnowledgeEntry>());

    /// <summary>
    /// Soft-retires a published record: it stays in storage (for audit and redirects) but is
    /// excluded from every public read. <paramref name="supersededBy"/> names the record that
    /// absorbed it, so a lookup by the retired name resolves to the successor.
    /// </summary>
    Task<bool> RetireCompoundAsync(string canonicalName, string? supersededBy, DateTime retiredAtUtc, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This knowledge source does not support retirement.");
}

/// <summary>Canonical name and aliases of a stored knowledge entry.</summary>
public sealed record KnowledgeEntryIdentity(string CanonicalName, IReadOnlyList<string> Aliases);
