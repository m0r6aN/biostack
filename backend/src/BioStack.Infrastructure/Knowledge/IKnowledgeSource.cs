namespace BioStack.Infrastructure.Knowledge;

using BioStack.Domain.Entities;

public interface IKnowledgeSource
{
    Task<KnowledgeEntry?> GetCompoundAsync(string name, CancellationToken cancellationToken = default);
    Task<List<KnowledgeEntry>> GetAllCompoundsAsync(CancellationToken cancellationToken = default);
    Task<List<KnowledgeEntry>> SearchCompoundsByPathwayAsync(string pathway, CancellationToken cancellationToken = default);
    Task UpsertCompoundAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default);
    Task<int> IngestBulkAsync(List<KnowledgeEntry> entries, CancellationToken cancellationToken = default);
}
