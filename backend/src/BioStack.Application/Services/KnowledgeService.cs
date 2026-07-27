namespace BioStack.Application.Services;

using BioStack.Infrastructure.Knowledge;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;

public sealed class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeSource _knowledgeSource;

    public KnowledgeService(IKnowledgeSource knowledgeSource)
    {
        _knowledgeSource = knowledgeSource;
    }

    public async Task<KnowledgeEntryResponse?> GetCompoundAsync(string name, CancellationToken cancellationToken = default)
    {
        var entry = await _knowledgeSource.GetCompoundAsync(name, cancellationToken);
        return entry is null ? null : MapToResponse(entry);
    }

    public async Task<IEnumerable<KnowledgeEntryResponse>> GetAllCompoundsAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _knowledgeSource.GetAllCompoundsAsync(cancellationToken);
        return entries.Select(MapToResponse);
    }

    public async Task<IEnumerable<KnowledgeEntryResponse>> SearchByPathwayAsync(string pathway, CancellationToken cancellationToken = default)
    {
        var entries = await _knowledgeSource.SearchCompoundsByPathwayAsync(pathway, cancellationToken);
        return entries.Select(MapToResponse);
    }

    private static KnowledgeEntryResponse MapToResponse(KnowledgeEntry entry)
    {
        return new KnowledgeEntryResponse(
            entry.CanonicalName,
            entry.Aliases,
            entry.Classification,
            entry.RegulatoryStatus,
            entry.MechanismSummary,
            entry.EvidenceTier,
            entry.SourceReferences,
            entry.Notes,
            entry.Pathways,
            entry.Benefits,
            // The public knowledge contract keeps observational evidence and caution
            // signals, but quarantines legacy fields that imply individualized action.
            new List<string>(),
            entry.AvoidWith,
            new List<string>(),
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            new List<string>(),
            new List<string>(),
            null,
            entry.DrugInteractions,
            string.Empty,
            string.Empty,
            new List<string>(),
            string.Empty,
            string.Empty
        );
    }
}

public interface IKnowledgeService
{
    Task<KnowledgeEntryResponse?> GetCompoundAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeEntryResponse>> GetAllCompoundsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeEntryResponse>> SearchByPathwayAsync(string pathway, CancellationToken cancellationToken = default);
}
