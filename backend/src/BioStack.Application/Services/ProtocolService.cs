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
    private readonly IProtocolRunRepository _protocolRunRepository;
    private readonly IKnowledgeSource _knowledgeSource;

    public ProtocolService(
        IProtocolRepository protocolRepository,
        IPersonProfileRepository profileRepository,
        ICompoundRecordRepository compoundRepository,
        ICheckInRepository checkInRepository,
        IProtocolRunRepository protocolRunRepository,
        IKnowledgeSource knowledgeSource)
    {
        _protocolRepository = protocolRepository;
        _profileRepository = profileRepository;
        _compoundRepository = compoundRepository;
        _checkInRepository = checkInRepository;
        _protocolRunRepository = protocolRunRepository;
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
                Notes = compound.Notes,
                CompoundNameSnapshot = compound.Name,
                CompoundCategorySnapshot = compound.Category.ToString(),
                CompoundStartDateSnapshot = compound.StartDate,
                CompoundEndDateSnapshot = compound.EndDate,
                CompoundStatusSnapshot = compound.Status.ToString(),
                CompoundNotesSnapshot = compound.Notes,
                CompoundGoalSnapshot = compound.Goal,
                CompoundSourceSnapshot = compound.Source,
                CompoundPricePaidSnapshot = compound.PricePaid
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

    public async Task<ProtocolRunResponse> StartRunAsync(Guid protocolId, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(protocolId, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {protocolId} not found");

        var now = DateTime.UtcNow;
        var activeRun = await _protocolRunRepository.GetActiveByPersonIdAsync(protocol.PersonId, cancellationToken);
        if (activeRun is not null)
        {
            activeRun.Status = ProtocolRunStatus.Abandoned;
            activeRun.EndedAtUtc = now;
            activeRun.Notes = string.IsNullOrWhiteSpace(activeRun.Notes)
                ? "Ended when another protocol run started."
                : activeRun.Notes;
            await _protocolRunRepository.UpdateAsync(activeRun, cancellationToken);
        }

        var run = new ProtocolRun
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            PersonId = protocol.PersonId,
            StartedAtUtc = now,
            Status = ProtocolRunStatus.Active
        };

        await _protocolRunRepository.AddAsync(run, cancellationToken);
        await _protocolRunRepository.SaveChangesAsync(cancellationToken);

        run.Protocol = protocol;
        return MapRun(run);
    }

    public async Task<ProtocolRunResponse?> GetActiveRunAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var run = await _protocolRunRepository.GetActiveByPersonIdAsync(personId, cancellationToken);
        return run is null ? null : MapRun(run);
    }

    public async Task<ProtocolRunResponse> CompleteRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _protocolRunRepository.GetWithProtocolAsync(runId, cancellationToken);
        if (run is null)
            throw new InvalidOperationException($"Protocol run with ID {runId} not found");

        if (run.Status == ProtocolRunStatus.Active)
        {
            run.Status = ProtocolRunStatus.Completed;
            run.EndedAtUtc = DateTime.UtcNow;
            await _protocolRunRepository.UpdateAsync(run, cancellationToken);
            await _protocolRunRepository.SaveChangesAsync(cancellationToken);
        }

        return MapRun(run);
    }

    public async Task<ProtocolRunResponse> AbandonRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _protocolRunRepository.GetWithProtocolAsync(runId, cancellationToken);
        if (run is null)
            throw new InvalidOperationException($"Protocol run with ID {runId} not found");

        if (run.Status == ProtocolRunStatus.Active)
        {
            run.Status = ProtocolRunStatus.Abandoned;
            run.EndedAtUtc = DateTime.UtcNow;
            run.Notes = string.IsNullOrWhiteSpace(run.Notes)
                ? "Marked abandoned from protocol detail."
                : run.Notes;
            await _protocolRunRepository.UpdateAsync(run, cancellationToken);
            await _protocolRunRepository.SaveChangesAsync(cancellationToken);
        }

        return MapRun(run);
    }

    public async Task<ProtocolResponse> EvolveFromRunAsync(Guid runId, EvolveProtocolFromRunRequest request, CancellationToken cancellationToken = default)
    {
        var run = await _protocolRunRepository.GetWithProtocolAsync(runId, cancellationToken);
        if (run?.Protocol is null)
            throw new InvalidOperationException($"Protocol run with ID {runId} not found");

        if (run.Status is not (ProtocolRunStatus.Completed or ProtocolRunStatus.Abandoned))
            throw new InvalidOperationException("Only completed or abandoned runs can be evolved into a new protocol draft.");

        var source = run.Protocol;
        var maxVersion = await _protocolRepository.GetMaxVersionInLineageAsync(source, cancellationToken);
        var nextVersion = maxVersion + 1;
        var now = DateTime.UtcNow;
        var sourceCompounds = source.Items.Select(SnapshotCompoundFromItem).ToList();
        var simulation = Simulate(await LoadKnowledgeEntriesAsync(sourceCompounds, cancellationToken));
        var comparison = await CompareActualAsync(source.PersonId, sourceCompounds, simulation, run, source, cancellationToken);

        var draft = new Protocol
        {
            Id = Guid.NewGuid(),
            PersonId = source.PersonId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{source.Name} v{nextVersion} draft" : request.Name.Trim(),
            Version = nextVersion,
            ParentProtocolId = source.Id,
            OriginProtocolId = source.OriginProtocolId ?? source.Id,
            EvolvedFromRunId = run.Id,
            IsDraft = true,
            EvolutionContext = BuildEvolutionContext(run, comparison),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = source.Items.Select(CloneItemForDraft).ToList()
        };

        await _protocolRepository.AddAsync(draft, cancellationToken);
        await _protocolRepository.SaveChangesAsync(cancellationToken);

        var saved = await _protocolRepository.GetWithItemsAsync(draft.Id, cancellationToken);
        return await MapProtocolAsync(saved ?? draft, includeComparison: true, cancellationToken);
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
            .Select(SnapshotCompoundFromItem)
            .ToList();

        var knowledgeEntries = await LoadKnowledgeEntriesAsync(compounds, cancellationToken);
        var score = CalculateStackScore(knowledgeEntries);
        var simulation = Simulate(knowledgeEntries);
        var activeRun = await _protocolRunRepository.GetActiveByProtocolIdAsync(protocol.Id, cancellationToken);
        var lineage = (await _protocolRepository.GetLineageAsync(protocol, cancellationToken)).ToList();
        var maxVersion = lineage.Count == 0 ? protocol.Version : lineage.Max(version => version.Version);
        var parent = protocol.ParentProtocolId is null
            ? null
            : lineage.FirstOrDefault(version => version.Id == protocol.ParentProtocolId)
                ?? await _protocolRepository.GetWithItemsAsync(protocol.ParentProtocolId.Value, cancellationToken);
        var diff = parent is null ? null : BuildVersionDiff(parent, protocol);
        var comparison = includeComparison
            ? await CompareActualAsync(protocol.PersonId, compounds, simulation, activeRun, protocol, cancellationToken)
            : null;

        return new ProtocolResponse(
            protocol.Id,
            protocol.PersonId,
            protocol.Name,
            protocol.Version,
            protocol.ParentProtocolId,
            protocol.OriginProtocolId,
            protocol.EvolvedFromRunId,
            protocol.IsDraft,
            protocol.EvolutionContext,
            protocol.Version == maxVersion,
            lineage
                .Where(version => version.Id != protocol.Id && version.Version < protocol.Version)
                .OrderByDescending(version => version.Version)
                .Select(version => new ProtocolVersionSummaryResponse(
                    version.Id,
                    version.Name,
                    version.Version,
                    version.IsDraft,
                    version.CreatedAtUtc))
                .ToList(),
            protocol.CreatedAtUtc,
            protocol.UpdatedAtUtc,
            protocol.Items.Select(MapItem).ToList(),
            score,
            simulation,
            activeRun is null ? null : MapRun(activeRun),
            diff,
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
        ProtocolRun? activeRun,
        Protocol protocol,
        CancellationToken cancellationToken)
    {
        var run = activeRun ?? await _protocolRunRepository.GetLatestByProtocolIdAsync(protocol.Id, cancellationToken);
        var checkIns = (await _checkInRepository.GetByPersonIdAsync(personId, cancellationToken))
            .OrderBy(checkIn => checkIn.Date)
            .ToList();
        var protocolStart = run?.StartedAtUtc ?? compounds
            .Where(compound => compound.StartDate.HasValue)
            .Select(compound => compound.StartDate!.Value)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Min();

        var before = checkIns.Where(checkIn => checkIn.Date < protocolStart).TakeLast(7).ToList();
        var after = run is null
            ? checkIns.Where(checkIn => checkIn.Date >= protocolStart).Take(14).ToList()
            : checkIns.Where(checkIn => checkIn.ProtocolRunId == run.Id).OrderBy(checkIn => checkIn.Date).ToList();
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

        if (run is not null)
        {
            highlights.Insert(0, $"Protocol run started {run.StartedAtUtc:MMM d}; attached check-ins are compared as observations, not causal proof.");
        }

        if (checkIns.Count > 0)
        {
            highlights.Add($"{after.Count} check-in{(after.Count == 1 ? string.Empty : "s")} fall inside the first 14 days of this protocol.");
        }

        var observations = run is null
            ? new List<ProtocolRunObservationResponse>()
            : after.Select(checkIn => new ProtocolRunObservationResponse(
                checkIn.Id,
                checkIn.Date,
                Math.Max(1, (int)Math.Floor((checkIn.Date.Date - run.StartedAtUtc.Date).TotalDays) + 1),
                checkIn.Energy,
                checkIn.SleepQuality,
                checkIn.Appetite,
                checkIn.Recovery
            )).ToList();
        var insights = BuildRunInsights(simulation, observations);
        var runSummary = run is null
            ? null
            : BuildRunSummary(run, trends, insights);

        return new ProtocolActualComparisonResponse(
            simulation,
            run is null ? null : MapRun(run),
            runSummary,
            observations,
            trends,
            insights,
            highlights);
    }

    private static ProtocolRunSummaryResponse BuildRunSummary(
        ProtocolRun run,
        List<ActualTrendResponse> trends,
        List<ProtocolRunInsightResponse> insights)
    {
        return new ProtocolRunSummaryResponse(
            MapRun(run),
            Math.Max(1, (int)Math.Floor(((run.EndedAtUtc ?? DateTime.UtcNow).Date - run.StartedAtUtc.Date).TotalDays) + 1),
            trends.Select(trend => new ProtocolRunSignalSummaryResponse(
                trend.Metric,
                trend.Direction,
                TrendMagnitude(trend.BeforeAverage, trend.AfterAverage))).ToList(),
            insights.Count(insight => insight.Type == "alignment"),
            insights.Count(insight => insight.Type == "divergence"));
    }

    private static List<ProtocolRunInsightResponse> BuildRunInsights(
        SimulationResultResponse simulation,
        List<ProtocolRunObservationResponse> observations)
    {
        if (observations.Count < 2)
        {
            return new List<ProtocolRunInsightResponse>
            {
                new("neutral", "More attached check-ins are needed before projected and observed signals can be compared.", new List<string>())
            };
        }

        var insights = new List<ProtocolRunInsightResponse>();
        foreach (var bucket in simulation.Timeline)
        {
            var (start, end) = ParseDayRange(bucket.DayRange);
            var window = observations
                .Where(observation => observation.Day >= start && observation.Day <= end)
                .OrderBy(observation => observation.Day)
                .ToList();

            foreach (var signal in bucket.Signals)
            {
                var metric = SignalToMetric(signal);
                if (metric is null)
                {
                    continue;
                }

                if (window.Count < 2)
                {
                    insights.Add(new ProtocolRunInsightResponse(
                        "neutral",
                        $"{metric} does not have enough attached check-ins to compare against projected signals for days {bucket.DayRange}.",
                        new List<string> { signal }));
                    continue;
                }

                var direction = MetricDirection(metric, window);
                if (direction == "up")
                {
                    insights.Add(new ProtocolRunInsightResponse(
                        "alignment",
                        $"{metric} increase aligns with projected signal timing in days {bucket.DayRange}.",
                        new List<string> { signal }));
                }
                else
                {
                    insights.Add(new ProtocolRunInsightResponse(
                        "divergence",
                        $"{metric} was {direction} while the projected signal was expected in days {bucket.DayRange}.",
                        new List<string> { signal }));
                }
            }
        }

        return insights.Count == 0
            ? new List<ProtocolRunInsightResponse>
            {
                new("neutral", "No projected signals map cleanly to tracked check-in metrics yet.", new List<string>())
            }
            : insights.Take(6).ToList();
    }

    private static (int Start, int End) ParseDayRange(string dayRange)
    {
        var parts = dayRange.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 && int.TryParse(parts[0], out var start) && int.TryParse(parts[1], out var end)
            ? (start, end)
            : (1, 14);
    }

    private static string? SignalToMetric(string signal)
    {
        var normalized = signal.ToLowerInvariant();
        if (normalized.Contains("energy") || normalized.Contains("mitochond"))
            return "Energy";
        if (normalized.Contains("sleep"))
            return "Sleep";
        if (normalized.Contains("appetite") || normalized.Contains("glp"))
            return "Appetite";
        if (normalized.Contains("recovery") || normalized.Contains("inflamm"))
            return "Recovery";
        return null;
    }

    private static string MetricDirection(string metric, List<ProtocolRunObservationResponse> observations)
    {
        var first = MetricValue(metric, observations.First());
        var last = MetricValue(metric, observations.Last());
        return last > first ? "up" : last < first ? "down" : "flat";
    }

    private static int MetricValue(string metric, ProtocolRunObservationResponse observation)
    {
        return metric switch
        {
            "Energy" => observation.Energy,
            "Sleep" => observation.SleepQuality,
            "Appetite" => observation.Appetite,
            "Recovery" => observation.Recovery,
            _ => 0
        };
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

    private static string TrendMagnitude(decimal? beforeAverage, decimal? afterAverage)
    {
        if (beforeAverage is null || afterAverage is null)
        {
            return "insufficient data";
        }

        var delta = Math.Abs(afterAverage.Value - beforeAverage.Value);
        return delta switch
        {
            < 0.5m => "no change",
            < 1.5m => "slight",
            < 3m => "moderate",
            _ => "strong"
        };
    }

    private static string BuildEvolutionContext(ProtocolRun run, ProtocolActualComparisonResponse comparison)
    {
        var lines = new List<string>
        {
            $"Based on this run's observations, draft created from {comparison.Run?.ProtocolName ?? "protocol"} v{comparison.Run?.ProtocolVersion ?? 1}.",
            $"Run status: {run.Status.ToString().ToLowerInvariant()}; observed days: {comparison.RunSummary?.DaysActive ?? 0}."
        };

        lines.AddRange(comparison.Insights
            .Select(insight => insight.Message)
            .Distinct()
            .Take(4));

        return string.Join(Environment.NewLine, lines);
    }

    private static ProtocolItem CloneItemForDraft(ProtocolItem source)
    {
        var compound = SnapshotCompoundFromItem(source);

        return new ProtocolItem
        {
            Id = Guid.NewGuid(),
            CompoundRecordId = source.CompoundRecordId,
            CalculatorResultId = source.CalculatorResultId,
            Notes = source.Notes,
            CompoundNameSnapshot = compound.Name,
            CompoundCategorySnapshot = compound.Category.ToString(),
            CompoundStartDateSnapshot = compound.StartDate,
            CompoundEndDateSnapshot = compound.EndDate,
            CompoundStatusSnapshot = compound.Status.ToString(),
            CompoundNotesSnapshot = compound.Notes,
            CompoundGoalSnapshot = compound.Goal,
            CompoundSourceSnapshot = compound.Source,
            CompoundPricePaidSnapshot = compound.PricePaid
        };
    }

    private static CompoundRecord SnapshotCompoundFromItem(ProtocolItem item)
    {
        var compound = item.CompoundRecord;

        return new CompoundRecord
        {
            Id = item.CompoundRecordId,
            PersonId = item.Protocol?.PersonId ?? compound?.PersonId ?? Guid.Empty,
            Name = string.IsNullOrWhiteSpace(item.CompoundNameSnapshot) ? compound?.Name ?? "Compound snapshot" : item.CompoundNameSnapshot,
            Category = ParseEnumOrDefault(item.CompoundCategorySnapshot, compound?.Category ?? CompoundCategory.Unknown),
            StartDate = item.CompoundStartDateSnapshot ?? compound?.StartDate,
            EndDate = item.CompoundEndDateSnapshot ?? compound?.EndDate,
            Status = ParseEnumOrDefault(item.CompoundStatusSnapshot, compound?.Status ?? CompoundStatus.Planned),
            Notes = string.IsNullOrWhiteSpace(item.CompoundNotesSnapshot) ? compound?.Notes ?? item.Notes : item.CompoundNotesSnapshot,
            SourceType = compound?.SourceType ?? SourceType.Manual,
            Goal = string.IsNullOrWhiteSpace(item.CompoundGoalSnapshot) ? compound?.Goal ?? string.Empty : item.CompoundGoalSnapshot,
            Source = string.IsNullOrWhiteSpace(item.CompoundSourceSnapshot) ? compound?.Source ?? string.Empty : item.CompoundSourceSnapshot,
            PricePaid = item.CompoundPricePaidSnapshot ?? compound?.PricePaid,
            CreatedAtUtc = compound?.CreatedAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = compound?.UpdatedAtUtc ?? DateTime.UtcNow
        };
    }

    private static TEnum ParseEnumOrDefault<TEnum>(string value, TEnum fallback) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
    }

    private static ProtocolVersionDiffResponse BuildVersionDiff(Protocol before, Protocol after)
    {
        var changes = new List<ProtocolVersionChangeResponse>();

        if (!string.Equals(before.Name, after.Name, StringComparison.Ordinal))
        {
            changes.Add(new ProtocolVersionChangeResponse("edited", "structure", "Protocol name", before.Name, after.Name));
        }

        var beforeItems = before.Items.Select(SnapshotCompoundFromItem).OrderBy(compound => compound.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var afterItems = after.Items.Select(SnapshotCompoundFromItem).OrderBy(compound => compound.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var beforeById = beforeItems.ToDictionary(compound => compound.Id);
        var afterById = afterItems.ToDictionary(compound => compound.Id);

        foreach (var removed in beforeItems.Where(compound => !afterById.ContainsKey(compound.Id)))
        {
            changes.Add(new ProtocolVersionChangeResponse("removed", "compound", removed.Name, DescribeCompound(removed), string.Empty));
        }

        foreach (var added in afterItems.Where(compound => !beforeById.ContainsKey(compound.Id)))
        {
            changes.Add(new ProtocolVersionChangeResponse("added", "compound", added.Name, string.Empty, DescribeCompound(added)));
        }

        foreach (var beforeItem in beforeItems.Where(compound => afterById.ContainsKey(compound.Id)))
        {
            var afterItem = afterById[beforeItem.Id];
            AddChangeIfDifferent(changes, "compound", beforeItem.Name, "Name", beforeItem.Name, afterItem.Name);
            AddChangeIfDifferent(changes, "compound", afterItem.Name, "Category", beforeItem.Category.ToString(), afterItem.Category.ToString());
            AddChangeIfDifferent(changes, "schedule", afterItem.Name, "Start date", FormatDate(beforeItem.StartDate), FormatDate(afterItem.StartDate));
            AddChangeIfDifferent(changes, "schedule", afterItem.Name, "End date", FormatDate(beforeItem.EndDate), FormatDate(afterItem.EndDate));
            AddChangeIfDifferent(changes, "structure", afterItem.Name, "Status", beforeItem.Status.ToString(), afterItem.Status.ToString());
            AddChangeIfDifferent(changes, "structure", afterItem.Name, "Notes", beforeItem.Notes, afterItem.Notes);
        }

        if (changes.Count == 0)
        {
            changes.Add(new ProtocolVersionChangeResponse("unchanged", "structure", "Protocol snapshot", "No deterministic changes detected", "No deterministic changes detected"));
        }

        return new ProtocolVersionDiffResponse(before.Id, after.Id, changes);
    }

    private static void AddChangeIfDifferent(
        List<ProtocolVersionChangeResponse> changes,
        string scope,
        string subject,
        string field,
        string before,
        string after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            return;
        }

        changes.Add(new ProtocolVersionChangeResponse("edited", scope, $"{subject}: {field}", before, after));
    }

    private static string DescribeCompound(CompoundRecord compound)
    {
        return JoinNonEmpty(
            compound.Name,
            compound.Category.ToString(),
            compound.Status.ToString(),
            FormatDate(compound.StartDate));
    }

    private static string FormatDate(DateTime? value)
    {
        return value?.ToString("yyyy-MM-dd") ?? string.Empty;
    }

    private static ProtocolRunResponse MapRun(ProtocolRun run)
    {
        return new ProtocolRunResponse(
            run.Id,
            run.ProtocolId,
            run.PersonId,
            run.Protocol?.Name ?? "Protocol",
            run.Protocol?.Version ?? 1,
            run.StartedAtUtc,
            run.EndedAtUtc,
            run.Status.ToString().ToLowerInvariant(),
            run.Notes
        );
    }

    private static ProtocolItemResponse MapItem(ProtocolItem item)
    {
        return new ProtocolItemResponse(
            item.Id,
            item.ProtocolId,
            item.CompoundRecordId,
            item.CalculatorResultId,
            item.Notes,
            MapCompound(SnapshotCompoundFromItem(item))
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
    Task<ProtocolRunResponse> StartRunAsync(Guid protocolId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse?> GetActiveRunAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse> CompleteRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse> AbandonRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<ProtocolResponse> EvolveFromRunAsync(Guid runId, EvolveProtocolFromRunRequest request, CancellationToken cancellationToken = default);
    Task<CurrentStackIntelligenceResponse> GetCurrentStackIntelligenceAsync(Guid personId, CancellationToken cancellationToken = default);
}
