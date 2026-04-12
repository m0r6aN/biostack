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

    public async Task<ProtocolReviewResponse> GetProtocolReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(id, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {id} not found");

        var lineage = (await _protocolRepository.GetLineageAsync(protocol, cancellationToken))
            .OrderBy(version => version.Version)
            .ThenBy(version => version.CreatedAtUtc)
            .ToList();
        if (lineage.Count == 0)
        {
            lineage.Add(protocol);
        }

        var runs = (await _protocolRunRepository.GetByProtocolIdsAsync(lineage.Select(version => version.Id), cancellationToken))
            .GroupBy(run => run.ProtocolId)
            .ToDictionary(group => group.Key, group => group.OrderBy(run => run.StartedAtUtc).ToList());
        var reviewVersions = new List<ProtocolReviewVersionResponse>();

        foreach (var version in lineage)
        {
            var compounds = version.Items.Select(SnapshotCompoundFromItem).ToList();
            var simulation = Simulate(await LoadKnowledgeEntriesAsync(compounds, cancellationToken));
            var versionRuns = new List<ProtocolReviewRunResponse>();

            foreach (var run in runs.GetValueOrDefault(version.Id, new List<ProtocolRun>()))
            {
                run.Protocol ??= version;
                var comparison = await CompareActualAsync(version.PersonId, compounds, simulation, run, version, cancellationToken);
                if (comparison.Run is null || comparison.RunSummary is null)
                {
                    continue;
                }

                versionRuns.Add(new ProtocolReviewRunResponse(
                    comparison.Run,
                    comparison.RunSummary,
                    comparison.Observations,
                    comparison.ActualTrends,
                    comparison.Insights));
            }

            var parent = version.ParentProtocolId is null
                ? null
                : lineage.FirstOrDefault(candidate => candidate.Id == version.ParentProtocolId);

            reviewVersions.Add(new ProtocolReviewVersionResponse(
                version.Id,
                version.Name,
                version.Version,
                version.IsDraft,
                version.ParentProtocolId,
                version.EvolvedFromRunId,
                version.EvolutionContext,
                version.CreatedAtUtc,
                parent is null ? null : BuildVersionDiff(parent, version),
                versionRuns));
        }

        var root = lineage.First();
        return new ProtocolReviewResponse(
            root.Id,
            protocol.Id,
            root.Name,
            reviewVersions,
            BuildReviewSections(reviewVersions),
            BuildReviewTimeline(reviewVersions),
            new List<string>
            {
                "Protocol Intelligence Review is observational and rule-based.",
                "Signals describe attached check-ins and structural lineage only.",
                "No section provides medical advice, dosage guidance, efficacy claims, or compound ranking."
            });
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

    public async Task<MissionControlResponse> GetMissionControlAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var protocols = (await _protocolRepository.GetByPersonIdAsync(personId, cancellationToken)).ToList();
        var activeRun = await _protocolRunRepository.GetActiveByPersonIdAsync(personId, cancellationToken);
        var runs = protocols.Count == 0
            ? new List<ProtocolRun>()
            : (await _protocolRunRepository.GetByProtocolIdsAsync(protocols.Select(protocol => protocol.Id), cancellationToken)).ToList();
        var latestClosedRun = runs
            .Where(run => run.Status is ProtocolRunStatus.Completed or ProtocolRunStatus.Abandoned)
            .OrderByDescending(run => run.EndedAtUtc ?? run.StartedAtUtc)
            .FirstOrDefault();
        var latestEvolved = await _protocolRepository.GetLatestEvolvedByPersonIdAsync(personId, cancellationToken);
        var checkIns = (await _checkInRepository.GetByPersonIdAsync(personId, cancellationToken))
            .OrderByDescending(checkIn => checkIn.Date)
            .ToList();
        var latestCheckIn = checkIns.FirstOrDefault();

        var reviewProtocolId = activeRun?.ProtocolId
            ?? latestEvolved?.Id
            ?? latestClosedRun?.ProtocolId
            ?? protocols.OrderByDescending(protocol => protocol.CreatedAtUtc).FirstOrDefault()?.Id;
        ProtocolReviewResponse? review = reviewProtocolId is null
            ? null
            : await GetProtocolReviewAsync(reviewProtocolId.Value, cancellationToken);

        return new MissionControlResponse(
            activeRun is null ? null : MapRun(activeRun),
            latestClosedRun is null ? null : MapRun(latestClosedRun),
            review is null ? null : BuildMissionReviewSummary(review),
            latestEvolved is null ? null : BuildMissionEvolution(latestEvolved, protocols),
            BuildCheckInSignal(latestCheckIn, activeRun, runs, checkIns),
            review?.Timeline
                .OrderByDescending(@event => @event.OccurredAtUtc)
                .Take(8)
                .OrderBy(@event => @event.OccurredAtUtc)
                .ToList() ?? new List<ProtocolReviewTimelineEventResponse>());
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

    private static List<ProtocolReviewSectionResponse> BuildReviewSections(List<ProtocolReviewVersionResponse> versions)
    {
        var sections = new List<ProtocolReviewSectionResponse>();
        var runContexts = versions
            .SelectMany(version => version.Runs.Select(run => new ReviewRunContext(version, run)))
            .ToList();

        sections.AddRange(BuildRecurringTrendSections(runContexts));
        sections.AddRange(BuildDivergenceSections(versions));
        sections.Add(BuildNeutralSection(runContexts));
        sections.Add(BuildChangeSection(versions));
        sections.Add(BuildDataGapSection(versions));

        return sections
            .Where(section => section.Evidence.Count > 0 || section.Type is "neutral" or "gap")
            .ToList();
    }

    private static IEnumerable<ProtocolReviewSectionResponse> BuildRecurringTrendSections(List<ReviewRunContext> runContexts)
    {
        return runContexts
            .SelectMany(context => context.Run.Trends
                .Where(trend => TrendHasObservationData(trend) && trend.Direction is "up" or "down" or "flat")
                .Select(trend => new { context.Version, context.Run, Trend = trend }))
            .GroupBy(item => new { item.Trend.Metric, item.Trend.Direction })
            .Where(group => group.Count() >= 2)
            .OrderBy(group => group.Key.Metric)
            .Take(4)
            .Select(group => new ProtocolReviewSectionResponse(
                group.Key.Direction == "flat" ? "neutral" : "alignment",
                group.Key.Direction == "flat"
                    ? $"Repeated stable {group.Key.Metric.ToLowerInvariant()} signal"
                    : $"Repeated {group.Key.Metric.ToLowerInvariant()} movement",
                group.Key.Direction == "flat"
                    ? $"{group.Key.Metric} stayed flat in {group.Count()} runs across the lineage."
                    : $"{group.Key.Metric} trend moved {DescribeDirection(group.Key.Direction)} in {group.Count()} runs across the lineage.",
                group.Select(item => FormatTrendEvidence(item.Version, item.Run.Run, item.Trend)).Take(4).ToList()));
    }

    private static IEnumerable<ProtocolReviewSectionResponse> BuildDivergenceSections(List<ProtocolReviewVersionResponse> versions)
    {
        var sections = new List<ProtocolReviewSectionResponse>();
        foreach (var version in versions.Where(version => version.VersionDiff is not null && version.Runs.Count > 0))
        {
            var prior = versions.LastOrDefault(candidate => candidate.Version < version.Version && candidate.Runs.Count > 0);
            if (prior is null)
            {
                continue;
            }

            var priorRun = prior.Runs.Last();
            var currentRun = version.Runs.First();
            foreach (var priorTrend in priorRun.Trends.Where(TrendHasObservationData))
            {
                var currentTrend = currentRun.Trends.FirstOrDefault(trend => trend.Metric == priorTrend.Metric);
                if (currentTrend is null || !TrendHasObservationData(currentTrend) || currentTrend.Direction == priorTrend.Direction)
                {
                    continue;
                }

                var changeScope = version.VersionDiff!.Changes.Any(change => change.Scope == "schedule")
                    ? "schedule structure"
                    : version.VersionDiff.Changes.Any(change => change.Scope == "compound")
                        ? "compound structure"
                        : "protocol structure";

                sections.Add(new ProtocolReviewSectionResponse(
                    "divergence",
                    $"{priorTrend.Metric} diverged after v{version.Version} changes",
                    $"{priorTrend.Metric} observations moved {DescribeDirection(priorTrend.Direction)} in v{prior.Version} and {DescribeDirection(currentTrend.Direction)} after {changeScope} changed in v{version.Version}.",
                    new List<string>
                    {
                        FormatChangeEvidence(version),
                        FormatTrendEvidence(prior, priorRun.Run, priorTrend),
                        FormatTrendEvidence(version, currentRun.Run, currentTrend)
                    }));
            }
        }

        return sections.Take(4);
    }

    private static ProtocolReviewSectionResponse BuildNeutralSection(List<ReviewRunContext> runContexts)
    {
        var stable = runContexts
            .SelectMany(context => context.Run.Trends
                .Where(trend => TrendHasObservationData(trend) && trend.Direction == "flat")
                .Select(trend => FormatTrendEvidence(context.Version, context.Run.Run, trend)))
            .Distinct()
            .Take(4)
            .ToList();

        if (stable.Count > 0)
        {
            return new ProtocolReviewSectionResponse(
                "neutral",
                "Neutral or stable signals",
                "Some tracked metrics stayed flat within attached run observations.",
                stable);
        }

        return new ProtocolReviewSectionResponse(
            "neutral",
            "No clear stable pattern detected",
            "Attached observations did not produce a repeated flat signal across runs.",
            new List<string>());
    }

    private static ProtocolReviewSectionResponse BuildChangeSection(List<ProtocolReviewVersionResponse> versions)
    {
        var evidence = versions
            .Where(version => version.VersionDiff is not null)
            .Select(FormatChangeEvidence)
            .Distinct()
            .Take(6)
            .ToList();

        return new ProtocolReviewSectionResponse(
            "change",
            "Notable changes between versions",
            evidence.Count == 0
                ? "No version-to-version structural changes are available for this lineage yet."
                : "Version changes are listed as lineage facts, not recommendations.",
            evidence);
    }

    private static ProtocolReviewSectionResponse BuildDataGapSection(List<ProtocolReviewVersionResponse> versions)
    {
        var evidence = new List<string>();
        foreach (var version in versions)
        {
            if (version.Runs.Count == 0)
            {
                evidence.Add($"v{version.Version}: no protocol runs are available for review.");
                continue;
            }

            foreach (var run in version.Runs.Where(run => run.Observations.Count < 2))
            {
                evidence.Add($"v{version.Version} run {ShortId(run.Run.Id)}: insufficient attached check-ins to compare run trends.");
            }
        }

        foreach (var metric in new[] { "Energy", "Recovery", "Sleep", "Appetite" })
        {
            var versionsWithMetricData = versions.Count(version => version.Runs.Any(run =>
                run.Trends.Any(trend => trend.Metric == metric && TrendHasObservationData(trend))));
            if (versions.Count > 1 && versionsWithMetricData < 2)
            {
                evidence.Add($"Insufficient observations to compare {metric.ToLowerInvariant()}-related outcomes across versions.");
            }
        }

        return new ProtocolReviewSectionResponse(
            "gap",
            "Observation gaps",
            evidence.Count == 0
                ? "No major observation gaps were detected for the available runs."
                : "Gaps identify where BioStack cannot compare observations without adding interpretation.",
            evidence.Distinct().Take(8).ToList());
    }

    private static List<ProtocolReviewTimelineEventResponse> BuildReviewTimeline(List<ProtocolReviewVersionResponse> versions)
    {
        var events = new List<ProtocolReviewTimelineEventResponse>();
        foreach (var version in versions)
        {
            events.Add(new ProtocolReviewTimelineEventResponse(
                version.CreatedAtUtc,
                "version_created",
                $"v{version.Version} created",
                version.ProtocolId,
                null,
                null,
                version.IsDraft ? "Protocol draft snapshot created." : "Protocol snapshot created."));

            if (version.EvolvedFromRunId is not null)
            {
                events.Add(new ProtocolReviewTimelineEventResponse(
                    version.CreatedAtUtc,
                    "evolution",
                    $"v{version.Version} evolved from run",
                    version.ProtocolId,
                    version.EvolvedFromRunId,
                    null,
                    "Observed after prior run; historical source remains unchanged."));
            }

            foreach (var run in version.Runs)
            {
                events.Add(new ProtocolReviewTimelineEventResponse(
                    run.Run.StartedAtUtc,
                    "run_started",
                    $"v{version.Version} run started",
                    version.ProtocolId,
                    run.Run.Id,
                    null,
                    $"{run.Observations.Count} attached check-in{(run.Observations.Count == 1 ? string.Empty : "s")} in this run."));

                if (run.Run.EndedAtUtc is not null)
                {
                    events.Add(new ProtocolReviewTimelineEventResponse(
                        run.Run.EndedAtUtc.Value,
                        $"run_{run.Run.Status}",
                        $"v{version.Version} run {run.Run.Status}",
                        version.ProtocolId,
                        run.Run.Id,
                        null,
                        "Run boundary preserved for review."));
                }

                events.AddRange(run.Observations.Select(observation => new ProtocolReviewTimelineEventResponse(
                    observation.Date,
                    "check_in",
                    $"v{version.Version} day {observation.Day} check-in",
                    version.ProtocolId,
                    run.Run.Id,
                    observation.CheckInId,
                    $"Energy {observation.Energy}/10, Sleep {observation.SleepQuality}/10, Appetite {observation.Appetite}/10, Recovery {observation.Recovery}/10.")));
            }
        }

        return events
            .OrderBy(@event => @event.OccurredAtUtc)
            .ThenBy(@event => @event.EventType)
            .ToList();
    }

    private static MissionControlReviewSummaryResponse BuildMissionReviewSummary(ProtocolReviewResponse review)
    {
        var runCount = review.Versions.Sum(version => version.Runs.Count);
        var checkInCount = review.Versions.Sum(version => version.Runs.Sum(run => run.Observations.Count));
        var primarySection = review.Sections.FirstOrDefault(section => section.Type != "gap")
            ?? review.Sections.FirstOrDefault();
        var cue = primarySection?.Summary
            ?? (runCount < 2
                ? "Review available after multiple runs in this lineage."
                : "Review is available for this lineage.");

        return new MissionControlReviewSummaryResponse(
            review.RequestedProtocolId,
            review.LineageRootProtocolId,
            review.LineageName,
            cue,
            primarySection?.Type ?? "gap",
            review.Versions.Count,
            runCount,
            checkInCount);
    }

    private static MissionControlEvolutionResponse BuildMissionEvolution(Protocol latestEvolved, List<Protocol> protocols)
    {
        var parent = latestEvolved.ParentProtocolId is null
            ? null
            : protocols.FirstOrDefault(protocol => protocol.Id == latestEvolved.ParentProtocolId);
        var diff = parent is null ? null : BuildVersionDiff(parent, latestEvolved);
        var changes = diff?.Changes.Take(4).ToList() ?? new List<ProtocolVersionChangeResponse>();
        var summary = changes.Count == 0
            ? "No deterministic structural change detected."
            : string.Join("; ", changes.Take(2).Select(change => $"{change.ChangeType} {change.Scope}: {change.Subject}"));

        return new MissionControlEvolutionResponse(
            latestEvolved.Id,
            latestEvolved.ParentProtocolId,
            latestEvolved.EvolvedFromRunId,
            $"v{latestEvolved.Version} draft evolved",
            summary,
            latestEvolved.CreatedAtUtc,
            changes);
    }

    private static MissionControlCheckInSignalResponse BuildCheckInSignal(
        CheckIn? latestCheckIn,
        ProtocolRun? activeRun,
        List<ProtocolRun> runs,
        List<CheckIn> checkIns)
    {
        var referenceRun = activeRun ?? runs
            .OrderByDescending(run => run.EndedAtUtc ?? run.StartedAtUtc)
            .FirstOrDefault();
        var attachedCount = referenceRun is null
            ? 0
            : checkIns.Count(checkIn => checkIn.ProtocolRunId == referenceRun.Id);

        if (referenceRun is not null && attachedCount == 0)
        {
            return new MissionControlCheckInSignalResponse(
                latestCheckIn?.Id,
                referenceRun.Id,
                latestCheckIn?.Date,
                $"No check-ins attached to {referenceRun.Protocol?.Name ?? "this run"} v{referenceRun.Protocol?.Version ?? 1} yet.",
                0,
                true);
        }

        if (latestCheckIn is null)
        {
            return new MissionControlCheckInSignalResponse(
                null,
                referenceRun?.Id,
                null,
                "No check-ins captured yet.",
                0,
                true);
        }

        return new MissionControlCheckInSignalResponse(
            latestCheckIn.Id,
            referenceRun?.Id ?? latestCheckIn.ProtocolRunId,
            latestCheckIn.Date,
            latestCheckIn.ProtocolRunId is null
                ? "Latest check-in is not attached to a protocol run."
                : latestCheckIn.ProtocolRunId == referenceRun?.Id
                    ? "Latest check-in is attached to the current observation loop."
                    : $"{attachedCount} check-in{(attachedCount == 1 ? string.Empty : "s")} attached to this run; latest check-in belongs elsewhere.",
            attachedCount,
            latestCheckIn.ProtocolRunId is null);
    }

    private static bool TrendHasObservationData(ActualTrendResponse trend)
    {
        return trend.BeforeAverage is not null && trend.AfterAverage is not null && trend.Direction != "not enough data";
    }

    private static string DescribeDirection(string direction)
    {
        return direction switch
        {
            "up" => "up",
            "down" => "down",
            "flat" => "flat",
            _ => direction
        };
    }

    private static string FormatTrendEvidence(ProtocolReviewVersionResponse version, ProtocolRunResponse run, ActualTrendResponse trend)
    {
        return $"v{version.Version} run {ShortId(run.Id)}: {trend.Metric} before {FormatAverage(trend.BeforeAverage)}, after {FormatAverage(trend.AfterAverage)}, direction {trend.Direction}.";
    }

    private static string FormatChangeEvidence(ProtocolReviewVersionResponse version)
    {
        var changes = version.VersionDiff?.Changes
            .Take(3)
            .Select(change => $"{change.ChangeType} {change.Scope}: {change.Subject}")
            .ToList() ?? new List<string>();

        return changes.Count == 0
            ? $"v{version.Version}: no deterministic structural change detected."
            : $"v{version.Version}: {string.Join("; ", changes)}.";
    }

    private static string FormatAverage(decimal? average)
    {
        return average?.ToString("0.#") ?? "n/a";
    }

    private static string ShortId(Guid id)
    {
        return id.ToString("N")[..8];
    }

    private sealed record ReviewRunContext(ProtocolReviewVersionResponse Version, ProtocolReviewRunResponse Run);

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
    Task<ProtocolReviewResponse> GetProtocolReviewAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse> StartRunAsync(Guid protocolId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse?> GetActiveRunAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<MissionControlResponse> GetMissionControlAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse> CompleteRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse> AbandonRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<ProtocolResponse> EvolveFromRunAsync(Guid runId, EvolveProtocolFromRunRequest request, CancellationToken cancellationToken = default);
    Task<CurrentStackIntelligenceResponse> GetCurrentStackIntelligenceAsync(Guid personId, CancellationToken cancellationToken = default);
}
