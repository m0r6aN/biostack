namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;

public sealed class CurrentStackIntelligenceService : ICurrentStackIntelligenceService
{
    private readonly ICompoundRecordRepository _compoundRepository;
    private readonly IPersonProfileRepository _profileRepository;
    private readonly IKnowledgeSource _knowledgeSource;
    private readonly ISchedulePreviewService _schedulePreviewService;

    public CurrentStackIntelligenceService(
        ICompoundRecordRepository compoundRepository,
        IPersonProfileRepository profileRepository,
        IKnowledgeSource knowledgeSource,
        ISchedulePreviewService schedulePreviewService)
    {
        _compoundRepository = compoundRepository;
        _profileRepository = profileRepository;
        _knowledgeSource = knowledgeSource;
        _schedulePreviewService = schedulePreviewService;
    }

    public async Task<CurrentStackIntelligenceResponse> GetCurrentStackAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(personId, cancellationToken);
        if (profile is null)
            throw new InvalidOperationException($"Profile with ID {personId} not found");

        var compounds = (await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken))
            .Where(compound => compound.Status == CompoundStatus.Active)
            .OrderBy(compound => compound.StartDate ?? compound.CreatedAtUtc)
            .ToList();

        var knowledgeEntries = await _knowledgeSource.GetAllCompoundsAsync(cancellationToken);
        var enriched = compounds
            .Select(compound => EnrichCompound(compound, knowledgeEntries))
            .ToList();

        var linked = enriched
            .Where(item => item.Entry is not null)
            .Select(item => new LinkedCompound(item.Compound, item.Entry!))
            .ToList();

        var signals = new List<StackSignalResponse>();
        signals.AddRange(BuildAvoidSignals(linked));
        signals.AddRange(BuildDrugInteractionSignals(linked));
        signals.AddRange(BuildPositivePairSignals(linked));
        signals.AddRange(BuildBlendSignals(linked));

        var pathwayOverlap = BuildPathwayOverlap(linked);
        foreach (var overlap in pathwayOverlap)
        {
            signals.Add(new StackSignalResponse(
                "pathway_overlap",
                "neutral",
                $"{overlap.Pathway} overlap",
                "Multiple active compounds share this pathway in the knowledge base.",
                overlap.CompoundNames,
                "Pathways"
            ));
        }

        var activeResponses = enriched.Select(item => MapCompound(item.Compound, item.Entry)).ToList();
        var unresolved = enriched
            .Where(item => item.Entry is null)
            .Select(item => MapCompound(item.Compound, null))
            .ToList();

        return new CurrentStackIntelligenceResponse(
            personId,
            activeResponses,
            PrioritizeSignals(signals),
            pathwayOverlap,
            BuildEvidenceSummary(linked),
            unresolved,
            "Educational and observational stack intelligence from linked knowledge entries. Not medical advice."
        );
    }

    private (CompoundRecord Compound, KnowledgeEntry? Entry) EnrichCompound(CompoundRecord compound, List<KnowledgeEntry> knowledgeEntries)
    {
        var entry = compound.KnowledgeEntryId.HasValue
            ? knowledgeEntries.FirstOrDefault(candidate => candidate.Id == compound.KnowledgeEntryId.Value)
            : null;

        entry ??= knowledgeEntries.FirstOrDefault(candidate => IsKnowledgeMatch(candidate, compound.CanonicalName));
        entry ??= knowledgeEntries.FirstOrDefault(candidate => IsKnowledgeMatch(candidate, compound.Name));

        return (compound, entry);
    }

    private static bool IsKnowledgeMatch(KnowledgeEntry entry, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return string.Equals(entry.CanonicalName, name, StringComparison.OrdinalIgnoreCase) ||
            entry.Aliases.Any(alias => string.Equals(alias, name, StringComparison.OrdinalIgnoreCase));
    }

    private List<StackSignalResponse> BuildAvoidSignals(List<LinkedCompound> linked)
    {
        var signals = new List<StackSignalResponse>();
        foreach (var source in linked)
        {
            foreach (var avoid in source.Entry.AvoidWith.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var matches = linked
                    .Where(candidate => candidate.Compound.Id != source.Compound.Id && MatchesKnowledgeToken(candidate.Entry, candidate.Compound, avoid))
                    .ToList();

                foreach (var match in matches)
                {
                    signals.Add(new StackSignalResponse(
                        "avoid_with",
                        "caution",
                        $"{source.DisplayName} avoid-with note",
                        $"Knowledge base lists '{avoid}' as an avoid-with note for {source.DisplayName}.",
                        new List<string> { source.DisplayName, match.DisplayName },
                        "AvoidWith"
                    ));
                }
            }
        }

        return signals;
    }

    private static List<StackSignalResponse> BuildDrugInteractionSignals(List<LinkedCompound> linked)
    {
        return linked
            .SelectMany(source => source.Entry.DrugInteractions
                .Where(note => !string.IsNullOrWhiteSpace(note))
                .Select(note => new StackSignalResponse(
                    "drug_interaction",
                    "caution",
                    $"{source.DisplayName} interaction note",
                    note,
                    new List<string> { source.DisplayName },
                    "DrugInteractions"
                )))
            .ToList();
    }

    private static List<StackSignalResponse> BuildPositivePairSignals(List<LinkedCompound> linked)
    {
        var signals = new List<StackSignalResponse>();
        foreach (var source in linked)
        {
            foreach (var pair in source.Entry.PairsWellWith.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var matches = linked
                    .Where(candidate => candidate.Compound.Id != source.Compound.Id && MatchesKnowledgeToken(candidate.Entry, candidate.Compound, pair))
                    .ToList();

                foreach (var match in matches)
                {
                    signals.Add(new StackSignalResponse(
                        "pairs_well_with",
                        "positive",
                        $"{source.DisplayName} pairing signal",
                        $"Knowledge base lists '{pair}' as pairing context for {source.DisplayName}.",
                        new List<string> { source.DisplayName, match.DisplayName },
                        "PairsWellWith"
                    ));
                }
            }
        }

        return signals;
    }

    private static List<StackSignalResponse> BuildBlendSignals(List<LinkedCompound> linked)
    {
        var signals = new List<StackSignalResponse>();
        foreach (var source in linked)
        {
            foreach (var blend in source.Entry.CompatibleBlends.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var matches = linked
                    .Where(candidate => candidate.Compound.Id != source.Compound.Id && MatchesKnowledgeToken(candidate.Entry, candidate.Compound, blend))
                    .ToList();

                foreach (var match in matches)
                {
                    signals.Add(new StackSignalResponse(
                        "compatible_blend",
                        "neutral",
                        $"{source.DisplayName} blend note",
                        $"Knowledge base lists '{blend}' in compatible blend context for {source.DisplayName}.",
                        new List<string> { source.DisplayName, match.DisplayName },
                        "CompatibleBlends"
                    ));
                }
            }

            if (!string.IsNullOrWhiteSpace(source.Entry.VialCompatibility))
            {
                signals.Add(new StackSignalResponse(
                    "vial_compatibility",
                    "neutral",
                    $"{source.DisplayName} vial compatibility",
                    source.Entry.VialCompatibility,
                    new List<string> { source.DisplayName },
                    "VialCompatibility"
                ));
            }
        }

        return signals;
    }

    private static bool MatchesKnowledgeToken(KnowledgeEntry entry, CompoundRecord compound, string token)
    {
        return NamesFor(entry, compound).Any(name =>
            token.Contains(name, StringComparison.OrdinalIgnoreCase) ||
            name.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> NamesFor(KnowledgeEntry entry, CompoundRecord compound)
    {
        yield return compound.Name;
        if (!string.IsNullOrWhiteSpace(compound.CanonicalName))
        {
            yield return compound.CanonicalName;
        }

        yield return entry.CanonicalName;
        foreach (var alias in entry.Aliases)
        {
            yield return alias;
        }
    }

    private static List<PathwayOverlapResponse> BuildPathwayOverlap(List<LinkedCompound> linked)
    {
        return linked
            .SelectMany(item => item.Entry.Pathways
                .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
                .Select(pathway => new { Pathway = pathway, item.DisplayName }))
            .GroupBy(item => item.Pathway, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(item => item.DisplayName).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(group => new PathwayOverlapResponse(
                group.Key,
                group.Select(item => item.DisplayName).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            ))
            .ToList();
    }

    private static List<EvidenceTierSummaryResponse> BuildEvidenceSummary(List<LinkedCompound> linked)
    {
        return linked
            .GroupBy(item => item.Entry.EvidenceTier)
            .Select(group => new EvidenceTierSummaryResponse(
                group.Key,
                group.Count(),
                group.Select(item => item.DisplayName).ToList()
            ))
            .OrderBy(summary => summary.EvidenceTier)
            .ToList();
    }

    private StackCompoundIntelligenceResponse MapCompound(CompoundRecord compound, KnowledgeEntry? entry)
    {
        return new StackCompoundIntelligenceResponse(
            compound.Id,
            compound.Name,
            compound.Status,
            compound.StartDate,
            compound.EndDate,
            entry is not null,
            entry?.Id ?? compound.KnowledgeEntryId,
            entry?.CanonicalName ?? compound.CanonicalName,
            entry?.Pathways ?? new List<string>(),
            entry?.EvidenceTier ?? EvidenceTier.Unknown,
            entry is null ? null : _schedulePreviewService.BuildPreview(entry)
        );
    }

    private static List<StackSignalResponse> PrioritizeSignals(List<StackSignalResponse> signals)
    {
        return signals
            .GroupBy(signal => $"{signal.Kind}|{signal.Title}|{signal.Detail}|{string.Join(",", signal.CompoundNames.OrderBy(name => name))}")
            .Select(group => group.First())
            .OrderBy(signal => SignalPriority(signal.Kind))
            .ThenBy(signal => signal.Title)
            .ToList();
    }

    private static int SignalPriority(string kind)
    {
        return kind switch
        {
            "avoid_with" => 0,
            "drug_interaction" => 1,
            "pathway_overlap" => 2,
            "compatible_blend" => 3,
            "vial_compatibility" => 4,
            "pairs_well_with" => 5,
            _ => 10
        };
    }

    private sealed record LinkedCompound(CompoundRecord Compound, KnowledgeEntry Entry)
    {
        public string DisplayName => string.IsNullOrWhiteSpace(Entry.CanonicalName) ? Compound.Name : Entry.CanonicalName;
    }
}

public interface ICurrentStackIntelligenceService
{
    Task<CurrentStackIntelligenceResponse> GetCurrentStackAsync(Guid personId, CancellationToken cancellationToken = default);
}
