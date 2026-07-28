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
        return entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.CanonicalName))
            .GroupBy(entry => entry.CanonicalName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(MapPublicGroupToResponse);
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

    private static int PublicEvidenceCompleteness(KnowledgeEntry entry)
    {
        var score = entry.SourceReferences.Count * 8
            + entry.Pathways.Count * 4
            + entry.Benefits.Count * 2
            + entry.AvoidWith.Count * 2
            + entry.DrugInteractions.Count * 2;

        if (!string.IsNullOrWhiteSpace(entry.MechanismSummary))
        {
            score += 4;
        }

        if (!string.IsNullOrWhiteSpace(entry.RegulatoryStatus))
        {
            score += 1;
        }

        return score;
    }

    private static KnowledgeEntryResponse MapPublicGroupToResponse(
        IGrouping<string, KnowledgeEntry> group)
    {
        var entries = group.ToList();
        var primary = entries
            .OrderByDescending(PublicEvidenceCompleteness)
            .ThenBy(entry => entry.Id)
            .First();
        var response = MapToResponse(primary);

        return response with
        {
            CanonicalName = primary.CanonicalName.Trim(),
            Aliases = MergePublicLists(entries, entry => entry.Aliases),
            SourceReferences = MergePublicLists(entries, entry => entry.SourceReferences),
            Pathways = MergePublicLists(entries, entry => entry.Pathways),
            Benefits = MergePublicLists(entries, entry => entry.Benefits),
            AvoidWith = MergePublicLists(entries, entry => entry.AvoidWith),
            DrugInteractions = MergePublicLists(entries, entry => entry.DrugInteractions),
        };
    }

    private static List<string> MergePublicLists(
        IEnumerable<KnowledgeEntry> entries,
        Func<KnowledgeEntry, IEnumerable<string>> selector)
    {
        return entries
            .SelectMany(selector)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public interface IKnowledgeService
{
    Task<KnowledgeEntryResponse?> GetCompoundAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeEntryResponse>> GetAllCompoundsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeEntryResponse>> SearchByPathwayAsync(string pathway, CancellationToken cancellationToken = default);
}
