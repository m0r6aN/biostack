namespace BioStack.Application.Services;

using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;

public sealed class ProtocolService : IProtocolService
{
    private readonly IProtocolRepository _protocolRepository;
    private readonly IPersonProfileRepository _profileRepository;
    private readonly ICompoundRecordRepository _compoundRepository;
    private readonly ICheckInRepository _checkInRepository;
    private readonly IKnowledgeSource _knowledgeSource;

    public ProtocolService(
        IProtocolRepository protocolRepository,
        IPersonProfileRepository profileRepository,
        ICompoundRecordRepository compoundRepository,
        ICheckInRepository checkInRepository,
        IKnowledgeSource knowledgeSource)
    {
        _protocolRepository = protocolRepository;
        _profileRepository = profileRepository;
        _compoundRepository = compoundRepository;
        _checkInRepository = checkInRepository;
        _knowledgeSource = knowledgeSource;
    }

    public async Task<ProtocolResponse> SaveCurrentStackAsync(Guid personId, SaveProtocolRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(personId, cancellationToken);
        if (profile is null)
            throw new InvalidOperationException($"Profile with ID {personId} not found");

        var activeCompounds = (await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken))
            .Where(compound => compound.Status == CompoundStatus.Active)
            .ToList();

        if (activeCompounds.Count == 0)
            throw new InvalidOperationException("No active compounds to save as a protocol");

        var now = DateTime.UtcNow;
        var protocol = new Protocol
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"Protocol {now:yyyy-MM-dd}" : request.Name.Trim(),
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = activeCompounds.Select(compound => new ProtocolItem
            {
                Id = Guid.NewGuid(),
                CompoundRecordId = compound.Id,
                Notes = compound.Notes
            }).ToList()
        };

        await _protocolRepository.AddAsync(protocol, cancellationToken);
        await _protocolRepository.SaveChangesAsync(cancellationToken);

        var saved = await _protocolRepository.GetWithItemsAsync(protocol.Id, cancellationToken);
        return await MapProtocolAsync(saved ?? protocol, includeComparison: true, cancellationToken);
    }

    public async Task<IEnumerable<ProtocolResponse>> GetProtocolsByProfileAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var protocols = await _protocolRepository.GetByPersonIdAsync(personId, cancellationToken);
        var responses = new List<ProtocolResponse>();

        foreach (var protocol in protocols)
        {
            responses.Add(await MapProtocolAsync(protocol, includeComparison: false, cancellationToken));
        }

        return responses;
    }

    public async Task<ProtocolResponse> GetProtocolAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(id, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {id} not found");

        return await MapProtocolAsync(protocol, includeComparison: true, cancellationToken);
    }

    public async Task<CurrentStackIntelligenceResponse> GetCurrentStackIntelligenceAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var compounds = (await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken))
            .Where(compound => compound.Status == CompoundStatus.Active)
            .ToList();
        var knowledgeEntries = await LoadKnowledgeEntriesAsync(compounds, cancellationToken);

        return new CurrentStackIntelligenceResponse(
            CalculateStackScore(knowledgeEntries),
            Simulate(knowledgeEntries)
        );
    }

    private async Task<ProtocolResponse> MapProtocolAsync(Protocol protocol, bool includeComparison, CancellationToken cancellationToken)
    {
        var compounds = protocol.Items
            .Select(item => item.CompoundRecord)
            .Where(compound => compound is not null)
            .Cast<CompoundRecord>()
            .ToList();

        var knowledgeEntries = await LoadKnowledgeEntriesAsync(compounds, cancellationToken);
        var score = CalculateStackScore(knowledgeEntries);
        var simulation = Simulate(knowledgeEntries);
        var comparison = includeComparison
            ? await CompareActualAsync(protocol.PersonId, compounds, simulation, cancellationToken)
            : null;

        return new ProtocolResponse(
            protocol.Id,
            protocol.PersonId,
            protocol.Name,
            protocol.Version,
            protocol.CreatedAtUtc,
            protocol.UpdatedAtUtc,
            protocol.Items.Select(MapItem).ToList(),
            score,
            simulation,
            comparison
        );
    }

    private async Task<List<KnowledgeEntry>> LoadKnowledgeEntriesAsync(IEnumerable<CompoundRecord> compounds, CancellationToken cancellationToken)
    {
        var entries = new List<KnowledgeEntry>();
        foreach (var compound in compounds)
        {
            var entry = await _knowledgeSource.GetCompoundAsync(compound.Name, cancellationToken);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static StackScoreResponse CalculateStackScore(List<KnowledgeEntry> entries)
    {
        if (entries.Count == 0)
        {
            return new StackScoreResponse(
                0,
                new StackScoreBreakdownResponse(0, 0, 0, 0),
                new List<string> { "No active compounds", "No simulation inputs" }
            );
        }

        var synergyHits = CountNameMatches(entries, entry => entry.PairsWellWith);
        var avoidHits = CountNameMatches(entries, entry => entry.AvoidWith);
        var drugInteractionFlags = entries.Sum(entry => entry.DrugInteractions.Count);
        var redundancyHits = CountOverlappingPathways(entries);
        var strongEvidence = entries.Count(entry => entry.EvidenceTier is EvidenceTier.Strong or EvidenceTier.Mechanistic);
        var moderateEvidence = entries.Count(entry => entry.EvidenceTier == EvidenceTier.Moderate);

        var score = 60
            + Math.Min(18, synergyHits * 6)
            + Math.Min(16, strongEvidence * 5 + moderateEvidence * 2)
            - Math.Min(12, redundancyHits * 2)
            - Math.Min(36, avoidHits * 14 + drugInteractionFlags * 5);

        score = Math.Clamp(score, 0, 100);

        var chips = new List<string>
        {
            avoidHits + drugInteractionFlags == 0
                ? "No interaction flags"
                : $"{avoidHits + drugInteractionFlags} interaction flag{(avoidHits + drugInteractionFlags == 1 ? string.Empty : "s")}",
            redundancyHits == 0
                ? "Low redundancy"
                : redundancyHits <= 2 ? "Moderate redundancy" : "High redundancy",
            strongEvidence > 0
                ? "Strong evidence base"
                : moderateEvidence > 0 ? "Moderate evidence base" : "Limited evidence base"
        };

        if (synergyHits > 0)
        {
            chips.Add($"{synergyHits} synergy signal{(synergyHits == 1 ? string.Empty : "s")}");
        }

        return new StackScoreResponse(
            score,
            new StackScoreBreakdownResponse(
                Math.Min(100, synergyHits * 25),
                Math.Min(100, redundancyHits * 20),
                Math.Min(100, (avoidHits + drugInteractionFlags) * 25),
                Math.Min(100, strongEvidence * 35 + moderateEvidence * 15)
            ),
            chips
        );
    }

    private static SimulationResultResponse Simulate(List<KnowledgeEntry> entries)
    {
        var earlySignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var midSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var laterSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var insights = new List<string>();

        foreach (var entry in entries)
        {
            foreach (var pathway in entry.Pathways.Take(3))
            {
                var signal = PathwayToSignal(pathway);
                earlySignals.Add(signal);
                midSignals.Add(signal);
            }

            foreach (var benefit in entry.Benefits.Take(2))
            {
                midSignals.Add($"{NormalizeSignalFragment(benefit)} signal may become easier to observe");
            }

            if (!string.IsNullOrWhiteSpace(entry.Frequency) || !string.IsNullOrWhiteSpace(entry.PreferredTimeOfDay))
            {
                insights.Add($"{entry.CanonicalName}: schedule reference is {JoinNonEmpty(entry.Frequency, entry.PreferredTimeOfDay)}.");
            }

            if (entry.WeeklyDosageSchedule.Count > 0 || entry.IncrementalEscalationSteps.Count > 0)
            {
                laterSignals.Add($"{entry.CanonicalName} schedule phase should be reviewed before week 3");
            }
        }

        if (entries.SelectMany(entry => entry.Pathways).GroupBy(p => p, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            insights.Add("Shared pathways exist, so compare observations by phase rather than assuming one compound explains a change.");
        }

        if (entries.Any(entry => entry.DrugInteractions.Count > 0 || entry.AvoidWith.Count > 0))
        {
            insights.Add("Interaction and avoid-with flags are educational warnings; review them before interpreting the protocol.");
        }

        laterSignals.Add("plateau or adaptation signals are most useful to review after week 3");

        return new SimulationResultResponse(
            new List<SimulationTimelineEntryResponse>
            {
                new("1-3", earlySignals.DefaultIfEmpty("initial observation baseline").Take(5).ToList()),
                new("4-7", midSignals.DefaultIfEmpty("early trend signals may begin separating from baseline").Take(5).ToList()),
                new("7-14", laterSignals.DefaultIfEmpty("review consistency before changing the stack").Take(5).ToList())
            },
            insights.Distinct().Take(6).ToList()
        );
    }

    private async Task<ProtocolActualComparisonResponse> CompareActualAsync(
        Guid personId,
        List<CompoundRecord> compounds,
        SimulationResultResponse simulation,
        CancellationToken cancellationToken)
    {
        var checkIns = (await _checkInRepository.GetByPersonIdAsync(personId, cancellationToken))
            .OrderBy(checkIn => checkIn.Date)
            .ToList();
        var protocolStart = compounds
            .Where(compound => compound.StartDate.HasValue)
            .Select(compound => compound.StartDate!.Value)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Min();

        var before = checkIns.Where(checkIn => checkIn.Date < protocolStart).TakeLast(7).ToList();
        var after = checkIns.Where(checkIn => checkIn.Date >= protocolStart).Take(14).ToList();
        var trends = new List<ActualTrendResponse>
        {
            BuildTrend("Energy", before.Select(c => (decimal)c.Energy), after.Select(c => (decimal)c.Energy)),
            BuildTrend("Recovery", before.Select(c => (decimal)c.Recovery), after.Select(c => (decimal)c.Recovery)),
            BuildTrend("Sleep", before.Select(c => (decimal)c.SleepQuality), after.Select(c => (decimal)c.SleepQuality)),
            BuildTrend("Appetite", before.Select(c => (decimal)c.Appetite), after.Select(c => (decimal)c.Appetite))
        };

        var highlights = compounds
            .Where(compound => compound.StartDate.HasValue)
            .OrderBy(compound => compound.StartDate)
            .Select(compound => $"{compound.Name} starts {compound.StartDate!.Value:MMM d}; compare check-ins before and after without assuming causation.")
            .Take(4)
            .ToList();

        if (checkIns.Count > 0)
        {
            highlights.Add($"{after.Count} check-in{(after.Count == 1 ? string.Empty : "s")} fall inside the first 14 days of this protocol.");
        }

        return new ProtocolActualComparisonResponse(simulation, trends, highlights);
    }

    private static int CountNameMatches(List<KnowledgeEntry> entries, Func<KnowledgeEntry, List<string>> selector)
    {
        var names = entries
            .SelectMany(entry => entry.Aliases.Append(entry.CanonicalName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return entries.Sum(entry => selector(entry).Count(names.Contains));
    }

    private static int CountOverlappingPathways(List<KnowledgeEntry> entries)
    {
        return entries
            .SelectMany(entry => entry.Pathways)
            .GroupBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Sum(group => group.Count() - 1);
    }

    private static string PathwayToSignal(string pathway)
    {
        var normalized = pathway.ToLowerInvariant();
        if (normalized.Contains("appetite") || normalized.Contains("glp"))
            return "appetite regulation increasing";
        if (normalized.Contains("mitochond"))
            return "mitochondrial activity increasing";
        if (normalized.Contains("sleep"))
            return "sleep rhythm signal may shift";
        if (normalized.Contains("inflamm") || normalized.Contains("recovery"))
            return "recovery and inflammatory load signals may change";
        if (normalized.Contains("focus") || normalized.Contains("cogn"))
            return "focus and clarity signals may change";

        return $"{NormalizeSignalFragment(pathway)} signal increasing";
    }

    private static string NormalizeSignalFragment(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string JoinNonEmpty(params string[] values)
    {
        return string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static ActualTrendResponse BuildTrend(string metric, IEnumerable<decimal> before, IEnumerable<decimal> after)
    {
        var beforeAverage = AverageOrNull(before);
        var afterAverage = AverageOrNull(after);
        var direction = beforeAverage is null || afterAverage is null
            ? "not enough data"
            : afterAverage > beforeAverage ? "up" : afterAverage < beforeAverage ? "down" : "flat";

        return new ActualTrendResponse(metric, beforeAverage, afterAverage, direction);
    }

    private static decimal? AverageOrNull(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 1);
    }

    private static ProtocolItemResponse MapItem(ProtocolItem item)
    {
        return new ProtocolItemResponse(
            item.Id,
            item.ProtocolId,
            item.CompoundRecordId,
            item.CalculatorResultId,
            item.Notes,
            item.CompoundRecord is null ? null : MapCompound(item.CompoundRecord)
        );
    }

    private static CompoundResponse MapCompound(CompoundRecord compound)
    {
        return new CompoundResponse(
            compound.Id,
            compound.PersonId,
            compound.Name,
            compound.Category,
            compound.StartDate,
            compound.EndDate,
            compound.Status,
            compound.Notes,
            compound.SourceType,
            compound.CreatedAtUtc,
            compound.UpdatedAtUtc,
            compound.Goal,
            compound.Source,
            compound.PricePaid
        );
    }
}

public interface IProtocolService
{
    Task<ProtocolResponse> SaveCurrentStackAsync(Guid personId, SaveProtocolRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProtocolResponse>> GetProtocolsByProfileAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<ProtocolResponse> GetProtocolAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CurrentStackIntelligenceResponse> GetCurrentStackIntelligenceAsync(Guid personId, CancellationToken cancellationToken = default);
}
