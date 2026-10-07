namespace BioStack.Infrastructure.Knowledge;

using BioStack.Domain.Entities;

public interface IKnowledgeSource
{
    Task<KnowledgeEntry?> GetCompoundAsync(string name, CancellationToken cancellationToken = default);
    Task<List<KnowledgeEntry>> GetAllCompoundsAsync(CancellationToken cancellationToken = default);
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
}
