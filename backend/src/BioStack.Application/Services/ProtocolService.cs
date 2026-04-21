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
    private readonly IProtocolComputationRecordRepository _computationRepository;
    private readonly IProtocolReviewCompletedEventRepository _reviewCompletedEventRepository;
    private readonly IKnowledgeSource _knowledgeSource;
    private readonly IInteractionIntelligenceService _interactionIntelligenceService;
    private readonly IOwnershipGuard _ownershipGuard;

    public ProtocolService(
        IProtocolRepository protocolRepository,
        IPersonProfileRepository profileRepository,
        ICompoundRecordRepository compoundRepository,
        ICheckInRepository checkInRepository,
        IProtocolRunRepository protocolRunRepository,
        IProtocolComputationRecordRepository computationRepository,
        IProtocolReviewCompletedEventRepository reviewCompletedEventRepository,
        IKnowledgeSource knowledgeSource,
        IInteractionIntelligenceService interactionIntelligenceService,
        IOwnershipGuard ownershipGuard)
    {
        _protocolRepository = protocolRepository;
        _profileRepository = profileRepository;
        _compoundRepository = compoundRepository;
        _checkInRepository = checkInRepository;
        _protocolRunRepository = protocolRunRepository;
        _computationRepository = computationRepository;
        _reviewCompletedEventRepository = reviewCompletedEventRepository;
        _knowledgeSource = knowledgeSource;
        _interactionIntelligenceService = interactionIntelligenceService;
        _ownershipGuard = ownershipGuard;
    }

    public async Task<ProtocolResponse> SaveCurrentStackAsync(Guid personId, SaveProtocolRequest request, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);

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
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
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
        await _ownershipGuard.EnsureProfileOwnedAsync(protocol.PersonId, cancellationToken);

        return await MapProtocolAsync(protocol, includeComparison: true, cancellationToken);
    }

    public async Task<ProtocolReviewResponse> GetProtocolReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(id, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {id} not found");
        await _ownershipGuard.EnsureProfileOwnedAsync(protocol.PersonId, cancellationToken);

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
        var computations = (await _computationRepository.GetByProtocolIdsAsync(lineage.Select(version => version.Id), cancellationToken))
            .GroupBy(record => record.ProtocolId)
            .ToDictionary(group => group.Key, group => group.OrderBy(record => record.TimestampUtc).ToList());
        var reviewCompletedEvents = (await _reviewCompletedEventRepository.GetByProtocolIdsAsync(lineage.Select(version => version.Id), cancellationToken))
            .GroupBy(@event => @event.ProtocolId)
            .ToDictionary(group => group.Key, group => group.OrderBy(@event => @event.CompletedAtUtc).ToList());
        var reviewVersions = new List<ProtocolReviewVersionResponse>();

        foreach (var version in lineage)
        {
            var compounds = version.Items.Select(SnapshotCompoundFromItem).ToList();
            var knowledgeEntries = await LoadKnowledgeEntriesAsync(compounds, cancellationToken);
            var interactionIntelligence = await _interactionIntelligenceService.EvaluateAsync(knowledgeEntries, cancellationToken);
            var simulation = Simulate(knowledgeEntries, interactionIntelligence);
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
            BuildReviewTimeline(reviewVersions, computations, reviewCompletedEvents),
            new List<string>
            {
                "Protocol Intelligence Review is observational and rule-based.",
                "Signals describe attached check-ins and structural lineage only.",
                "No section provides medical advice, dosage guidance, efficacy claims, or compound ranking."
            });
    }

    public async Task<ProtocolPatternSnapshot> GetPatternSnapshotAsync(Guid protocolId, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(protocolId, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {protocolId} not found");
        await _ownershipGuard.EnsureProfileOwnedAsync(protocol.PersonId, cancellationToken);

        var lineage = (await _protocolRepository.GetLineageAsync(protocol, cancellationToken))
            .OrderBy(version => version.Version)
            .ThenBy(version => version.CreatedAtUtc)
            .ToList();
        if (lineage.Count == 0)
        {
            lineage.Add(protocol);
        }

        var protocolIds = lineage.Select(version => version.Id).ToList();
        var runs = (await _protocolRunRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(run => run.StartedAtUtc)
            .ToList();
        var completedRuns = runs
            .Where(run => run.Status == ProtocolRunStatus.Completed)
            .OrderBy(run => run.StartedAtUtc)
            .ToList();
        var checkIns = (await _checkInRepository.GetByPersonIdAsync(protocol.PersonId, cancellationToken))
            .OrderBy(checkIn => checkIn.Date)
            .ToList();
        var computations = (await _computationRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(record => record.TimestampUtc)
            .ToList();
        var reviewCompletedEvents = (await _reviewCompletedEventRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(@event => @event.CompletedAtUtc)
            .ToList();
        var contexts = completedRuns
            .Select(run => BuildPatternRunContext(run, checkIns, computations, reviewCompletedEvents))
            .ToList();

        if (completedRuns.Count == 0)
        {
            return new ProtocolPatternSnapshot
            {
                ProtocolId = protocol.Id,
                HistoricalRunCount = 0,
                PatternConfidence = "none"
            };
        }

        var metricPatterns = completedRuns.Count < 2 ? new List<MetricPatternSummary>() : BuildMetricPatterns(contexts);
        var eventPatterns = completedRuns.Count < 2 ? new List<EventPatternSummary>() : BuildEventPatterns(contexts);
        var sequencePatterns = completedRuns.Count < 2 ? new List<SequencePatternSummary>() : BuildSequencePatterns(contexts);
        var confidence = DeterminePatternConfidence(completedRuns.Count, contexts, metricPatterns, eventPatterns, sequencePatterns);
        var activeRun = runs
            .Where(run => run.Status == ProtocolRunStatus.Active)
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefault();

        return new ProtocolPatternSnapshot
        {
            ProtocolId = protocol.Id,
            HistoricalRunCount = completedRuns.Count,
            PatternConfidence = confidence,
            MetricPatterns = metricPatterns,
            EventPatterns = eventPatterns,
            SequencePatterns = sequencePatterns,
            CurrentRunComparison = activeRun is null || completedRuns.Count < 2
                ? null
                : BuildCurrentRunComparison(activeRun, contexts, checkIns, computations)
        };
    }

    public async Task<ProtocolDriftSnapshot> GetDriftSnapshotAsync(Guid protocolId, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(protocolId, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {protocolId} not found");
        await _ownershipGuard.EnsureProfileOwnedAsync(protocol.PersonId, cancellationToken);

        var lineage = (await _protocolRepository.GetLineageAsync(protocol, cancellationToken))
            .OrderBy(version => version.Version)
            .ThenBy(version => version.CreatedAtUtc)
            .ToList();
        if (lineage.Count == 0)
        {
            lineage.Add(protocol);
        }

        var protocolIds = lineage.Select(version => version.Id).ToList();
        var runs = (await _protocolRunRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(run => run.StartedAtUtc)
            .ToList();
        var checkIns = (await _checkInRepository.GetByPersonIdAsync(protocol.PersonId, cancellationToken))
            .OrderBy(checkIn => checkIn.Date)
            .ToList();
        var computations = (await _computationRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(record => record.TimestampUtc)
            .ToList();
        var reviewCompletedEvents = (await _reviewCompletedEventRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(@event => @event.CompletedAtUtc)
            .ToList();

        var targetRun = runs
            .Where(run => run.Status == ProtocolRunStatus.Active)
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefault()
            ?? runs
                .OrderByDescending(run => run.EndedAtUtc ?? run.StartedAtUtc)
                .FirstOrDefault();
        var historicalRuns = runs
            .Where(run => run.Status == ProtocolRunStatus.Completed && run.Id != targetRun?.Id)
            .OrderBy(run => run.StartedAtUtc)
            .ToList();

        if (historicalRuns.Count < 2)
        {
            return new ProtocolDriftSnapshot
            {
                ProtocolId = protocol.Id,
                DriftState = "none",
                BaselineSource = "insufficient_history",
                RegimeClassification = new RegimeClassificationSummary { State = "stable" }
            };
        }

        if (targetRun is null)
        {
            return new ProtocolDriftSnapshot
            {
                ProtocolId = protocol.Id,
                DriftState = "none",
                BaselineSource = "historical_runs",
                RegimeClassification = new RegimeClassificationSummary { State = "stable" }
            };
        }

        var historicalContexts = historicalRuns
            .Select(run => BuildPatternRunContext(run, checkIns, computations, reviewCompletedEvents))
            .ToList();
        var targetContext = BuildPatternRunContext(targetRun, checkIns, computations, reviewCompletedEvents);
        var patternSnapshot = await GetPatternSnapshotAsync(protocolId, cancellationToken);
        var observationSignals = BuildObservationSignals(targetRun.Status == ProtocolRunStatus.Active ? targetRun : null, runs, checkIns).ToList();
        var signals = BuildDriftSignals(targetContext, historicalContexts, observationSignals);
        var driftState = ClassifyDrift(signals, patternSnapshot, targetContext.AttachedCheckIns.Count < 2);

        return new ProtocolDriftSnapshot
        {
            ProtocolId = protocol.Id,
            DriftState = driftState,
            BaselineSource = "historical_runs",
            Signals = signals,
            RegimeClassification = new RegimeClassificationSummary
            {
                State = MapRegimeState(driftState),
                ContributingFactors = signals
                    .Select(signal => signal.Type)
                    .Distinct(StringComparer.Ordinal)
                    .ToList()
            }
        };
    }

    public async Task<ProtocolSequenceExpectationSnapshot> GetSequenceExpectationSnapshotAsync(Guid protocolId, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(protocolId, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {protocolId} not found");
        await _ownershipGuard.EnsureProfileOwnedAsync(protocol.PersonId, cancellationToken);

        var lineage = (await _protocolRepository.GetLineageAsync(protocol, cancellationToken))
            .OrderBy(version => version.Version)
            .ThenBy(version => version.CreatedAtUtc)
            .ToList();
        if (lineage.Count == 0)
        {
            lineage.Add(protocol);
        }

        var protocolIds = lineage.Select(version => version.Id).ToList();
        var runs = (await _protocolRunRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(run => run.StartedAtUtc)
            .ToList();
        var completedRuns = runs
            .Where(run => run.Status == ProtocolRunStatus.Completed)
            .OrderBy(run => run.StartedAtUtc)
            .ToList();
        var checkIns = (await _checkInRepository.GetByPersonIdAsync(protocol.PersonId, cancellationToken))
            .OrderBy(checkIn => checkIn.Date)
            .ToList();
        var computations = (await _computationRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(record => record.TimestampUtc)
            .ToList();
        var reviewCompletedEvents = (await _reviewCompletedEventRepository.GetByProtocolIdsAsync(protocolIds, cancellationToken))
            .OrderBy(@event => @event.CompletedAtUtc)
            .ToList();

        if (completedRuns.Count < 2)
        {
            return new ProtocolSequenceExpectationSnapshot
            {
                ProtocolId = protocol.Id,
                HistoricalRunCount = completedRuns.Count,
                CurrentStatus = new CurrentSequenceStatusSummary
                {
                    State = "unknown",
                    Notes = new List<string> { "Sequence patterns will appear after multiple completed runs." }
                }
            };
        }

        var historicalSequences = completedRuns
            .Select(run => BuildSequenceRunContext(run, checkIns, computations, reviewCompletedEvents, lineage))
            .ToList();
        var commonTransitions = BuildCommonSequenceTransitions(historicalSequences);
        if (commonTransitions.Count == 0)
        {
            return new ProtocolSequenceExpectationSnapshot
            {
                ProtocolId = protocol.Id,
                HistoricalRunCount = completedRuns.Count,
                BaselineSource = "insufficient_history",
                CurrentStatus = new CurrentSequenceStatusSummary
                {
                    State = "unknown",
                    Notes = new List<string> { "Completed runs do not yet share repeated next-event transitions." }
                }
            };
        }

        var targetRun = runs
            .Where(run => run.Status == ProtocolRunStatus.Active)
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefault()
            ?? runs
                .OrderByDescending(run => run.EndedAtUtc ?? run.StartedAtUtc)
                .FirstOrDefault();
        var currentSequence = targetRun is null
            ? null
            : BuildSequenceRunContext(targetRun, checkIns, computations, reviewCompletedEvents, lineage);
        var expectation = currentSequence is null
            ? null
            : ResolveExpectedNextEvent(currentSequence, commonTransitions, completedRuns.Count);

        return new ProtocolSequenceExpectationSnapshot
        {
            ProtocolId = protocol.Id,
            BaselineSource = "historical_runs",
            HistoricalRunCount = completedRuns.Count,
            ExpectedNextEvent = expectation?.ExpectedNextEvent,
            CommonTransitions = commonTransitions
                .Select(transition => new ExpectedTransitionSummary
                {
                    FromState = transition.FromState,
                    ToEventType = transition.ToEventType,
                    TimingPattern = $"Usually observed within {FormatRange(transition.Offsets)} of {FormatSequenceEventName(transition.FromState).ToLowerInvariant()}.",
                    ObservedCount = transition.ObservedCount
                })
                .Take(4)
                .ToList(),
            CurrentStatus = expectation?.CurrentStatus ?? new CurrentSequenceStatusSummary
            {
                State = "unknown",
                Notes = new List<string> { "Current protocol state does not match a repeated historical transition." }
            }
        };
    }

    public async Task<ProtocolRunResponse> StartRunAsync(Guid protocolId, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(protocolId, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {protocolId} not found");
        await _ownershipGuard.EnsureProfileOwnedAsync(protocol.PersonId, cancellationToken);

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
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var run = await _protocolRunRepository.GetActiveByPersonIdAsync(personId, cancellationToken);
        return run is null ? null : MapRun(run);
    }

    public async Task<MissionControlResponse> GetMissionControlAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
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
        var latestReviewCompleted = protocols.Count == 0
            ? null
            : (await _reviewCompletedEventRepository.GetByProtocolIdsAsync(protocols.Select(protocol => protocol.Id), cancellationToken))
                .OrderByDescending(@event => @event.CompletedAtUtc)
                .FirstOrDefault();

        var reviewProtocolId = activeRun?.ProtocolId
            ?? latestEvolved?.Id
            ?? latestClosedRun?.ProtocolId
            ?? protocols.OrderByDescending(protocol => protocol.CreatedAtUtc).FirstOrDefault()?.Id;
        ProtocolReviewResponse? review = reviewProtocolId is null
            ? null
            : await GetProtocolReviewAsync(reviewProtocolId.Value, cancellationToken);
        var patternProtocolId = activeRun?.ProtocolId ?? reviewProtocolId;
        var patternSnapshot = patternProtocolId is null
            ? null
            : await GetPatternSnapshotAsync(patternProtocolId.Value, cancellationToken);
        var driftSnapshot = patternProtocolId is null
            ? null
            : await GetDriftSnapshotAsync(patternProtocolId.Value, cancellationToken);
        var sequenceExpectationSnapshot = patternProtocolId is null
            ? null
            : await GetSequenceExpectationSnapshotAsync(patternProtocolId.Value, cancellationToken);
        var pendingReview = HasPendingReview(latestClosedRun, latestEvolved, latestReviewCompleted);
        var observationSignals = BuildObservationSignals(activeRun, runs, checkIns);

        return new MissionControlResponse(
            activeRun is null ? null : MapRun(activeRun),
            latestClosedRun is null ? null : MapRun(latestClosedRun),
            review is null || !pendingReview ? null : BuildMissionReviewSummary(review),
            latestEvolved is null ? null : BuildMissionEvolution(latestEvolved, protocols),
            BuildCheckInSignal(latestCheckIn, activeRun, runs, checkIns),
            observationSignals,
            patternSnapshot,
            driftSnapshot,
            sequenceExpectationSnapshot,
            review?.Timeline
                .Select(@event => AnnotateTimelineEvent(@event, patternSnapshot, sequenceExpectationSnapshot))
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
        await _ownershipGuard.EnsureProfileOwnedAsync(run.PersonId, cancellationToken);

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
        await _ownershipGuard.EnsureProfileOwnedAsync(run.PersonId, cancellationToken);

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

    public async Task<ProtocolComputationRecordResponse> RecordComputationAsync(Guid protocolId, CreateProtocolComputationRequest request, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(protocolId, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {protocolId} not found");
        await _ownershipGuard.EnsureProfileOwnedAsync(protocol.PersonId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Type))
            throw new InvalidOperationException("Computation type is required.");

        if (request.RunId is not null)
        {
            var run = await _protocolRunRepository.GetWithProtocolAsync(request.RunId.Value, cancellationToken);
            if (run is null || run.ProtocolId != protocol.Id)
                throw new InvalidOperationException("Computation run must belong to the target protocol.");
        }

        var record = new ProtocolComputationRecord
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            ProtocolRunId = request.RunId,
            Type = request.Type.Trim(),
            InputSnapshot = request.InputSnapshot,
            OutputResult = request.OutputResult,
            TimestampUtc = DateTime.UtcNow
        };

        await _computationRepository.AddAsync(record, cancellationToken);
        await _computationRepository.SaveChangesAsync(cancellationToken);

        return MapComputation(record);
    }

    public async Task<ProtocolReviewCompletedEventResponse> CompleteReviewAsync(Guid protocolId, CompleteProtocolReviewRequest request, CancellationToken cancellationToken = default)
    {
        var protocol = await _protocolRepository.GetWithItemsAsync(protocolId, cancellationToken);
        if (protocol is null)
            throw new InvalidOperationException($"Protocol with ID {protocolId} not found");
        await _ownershipGuard.EnsureProfileOwnedAsync(protocol.PersonId, cancellationToken);

        if (request.RunId is not null)
        {
            var run = await _protocolRunRepository.GetWithProtocolAsync(request.RunId.Value, cancellationToken);
            if (run is null || run.ProtocolId != protocol.Id)
                throw new InvalidOperationException("Review run must belong to the target protocol.");
        }

        var @event = new ProtocolReviewCompletedEvent
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            ProtocolRunId = request.RunId,
            CompletedAtUtc = DateTime.UtcNow,
            Notes = request.Notes?.Trim() ?? string.Empty
        };

        await _reviewCompletedEventRepository.AddAsync(@event, cancellationToken);
        await _reviewCompletedEventRepository.SaveChangesAsync(cancellationToken);

        return MapReviewCompletedEvent(@event);
    }

    public async Task<ProtocolResponse> EvolveFromRunAsync(Guid runId, EvolveProtocolFromRunRequest request, CancellationToken cancellationToken = default)
    {
        var run = await _protocolRunRepository.GetWithProtocolAsync(runId, cancellationToken);
        if (run?.Protocol is null)
            throw new InvalidOperationException($"Protocol run with ID {runId} not found");
        await _ownershipGuard.EnsureProfileOwnedAsync(run.PersonId, cancellationToken);

        if (run.Status is not (ProtocolRunStatus.Completed or ProtocolRunStatus.Abandoned))
            throw new InvalidOperationException("Only completed or abandoned runs can be evolved into a new protocol draft.");

        var source = run.Protocol;
        var maxVersion = await _protocolRepository.GetMaxVersionInLineageAsync(source, cancellationToken);
        var nextVersion = maxVersion + 1;
        var now = DateTime.UtcNow;
        var sourceCompounds = source.Items.Select(SnapshotCompoundFromItem).ToList();
        var sourceKnowledgeEntries = await LoadKnowledgeEntriesAsync(sourceCompounds, cancellationToken);
        var sourceInteractionIntelligence = await _interactionIntelligenceService.EvaluateAsync(sourceKnowledgeEntries, cancellationToken);
        var simulation = Simulate(sourceKnowledgeEntries, sourceInteractionIntelligence);
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
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var compounds = (await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken))
            .Where(compound => compound.Status == CompoundStatus.Active)
            .ToList();
        var knowledgeEntries = await LoadKnowledgeEntriesAsync(compounds, cancellationToken);
        var interactionIntelligence = await _interactionIntelligenceService.EvaluateAsync(knowledgeEntries, cancellationToken);

        return new CurrentStackIntelligenceResponse(
            CalculateStackScore(knowledgeEntries, interactionIntelligence),
            Simulate(knowledgeEntries, interactionIntelligence),
            interactionIntelligence
        );
    }

    private async Task<ProtocolResponse> MapProtocolAsync(Protocol protocol, bool includeComparison, CancellationToken cancellationToken)
    {
        var compounds = protocol.Items
            .Select(SnapshotCompoundFromItem)
            .ToList();

        var knowledgeEntries = await LoadKnowledgeEntriesAsync(compounds, cancellationToken);
        var interactionIntelligence = await _interactionIntelligenceService.EvaluateAsync(knowledgeEntries, cancellationToken);
        var score = CalculateStackScore(knowledgeEntries, interactionIntelligence);
        var simulation = Simulate(knowledgeEntries, interactionIntelligence);
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
            interactionIntelligence,
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

    private static StackScoreResponse CalculateStackScore(
        List<KnowledgeEntry> entries,
        InteractionIntelligenceResponse interactionIntelligence)
    {
        if (entries.Count == 0)
        {
            return new StackScoreResponse(
                0,
                new StackScoreBreakdownResponse(0, 0, 0, 0),
                new List<string> { "No active compounds", "No simulation inputs" }
            );
        }

        var synergyHits = interactionIntelligence.Summary.Synergies;
        var redundancyHits = interactionIntelligence.Summary.Redundancies;
        var interferenceHits = interactionIntelligence.Summary.Interferences;
        var avoidHits = CountNameMatches(entries, entry => entry.AvoidWith);
        var drugInteractionFlags = entries.Sum(entry => entry.DrugInteractions.Count);
        var strongEvidence = entries.Count(entry => entry.EvidenceTier is EvidenceTier.Strong or EvidenceTier.Mechanistic);
        var moderateEvidence = entries.Count(entry => entry.EvidenceTier == EvidenceTier.Moderate);
        var synergyScore = interactionIntelligence.Score.SynergyScore;
        var redundancyPenalty = interactionIntelligence.Score.RedundancyPenalty;
        var interferencePenalty = interactionIntelligence.Score.InterferencePenalty;

        var score = 60
            + Math.Min(20, (int)Math.Round(synergyScore * 10))
            + Math.Min(16, strongEvidence * 5 + moderateEvidence * 2)
            - Math.Min(14, (int)Math.Round(redundancyPenalty * 7))
            - Math.Min(38, (int)Math.Round(interferencePenalty * 10) + avoidHits * 8 + drugInteractionFlags * 3);

        score = Math.Clamp(score, 0, 100);

        var chips = new List<string>
        {
            interferenceHits + avoidHits + drugInteractionFlags == 0
                ? "No interaction flags"
                : $"{interferenceHits + avoidHits + drugInteractionFlags} review-first interaction signal{(interferenceHits + avoidHits + drugInteractionFlags == 1 ? string.Empty : "s")}",
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
                Math.Min(100, (int)Math.Round(synergyScore * 35)),
                Math.Min(100, (int)Math.Round(redundancyPenalty * 30)),
                Math.Min(100, (int)Math.Round(interferencePenalty * 30) + Math.Min(40, (avoidHits + drugInteractionFlags) * 10)),
                Math.Min(100, strongEvidence * 35 + moderateEvidence * 15)
            ),
            chips
        );
    }

    private static SimulationResultResponse Simulate(
        List<KnowledgeEntry> entries,
        InteractionIntelligenceResponse interactionIntelligence)
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

        foreach (var finding in interactionIntelligence.TopFindings.Take(3))
        {
            insights.Add(finding.Message);
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

    private static List<ProtocolReviewTimelineEventResponse> BuildReviewTimeline(
        List<ProtocolReviewVersionResponse> versions,
        Dictionary<Guid, List<ProtocolComputationRecord>> computations,
        Dictionary<Guid, List<ProtocolReviewCompletedEvent>> reviewCompletedEvents)
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
                    null,
                    null,
                    "Observed after prior run; historical source remains unchanged."));
            }

            foreach (var computation in computations.GetValueOrDefault(version.ProtocolId, new List<ProtocolComputationRecord>()))
            {
                events.Add(new ProtocolReviewTimelineEventResponse(
                    computation.TimestampUtc,
                    "computation",
                    ComputationLabel(computation.Type),
                    version.ProtocolId,
                    computation.ProtocolRunId,
                    null,
                    computation.Id,
                    null,
                    ComputationDetail(computation)));
            }

            foreach (var completed in reviewCompletedEvents.GetValueOrDefault(version.ProtocolId, new List<ProtocolReviewCompletedEvent>()))
            {
                events.Add(new ProtocolReviewTimelineEventResponse(
                    completed.CompletedAtUtc,
                    "review_completed",
                    "Review completed",
                    version.ProtocolId,
                    completed.ProtocolRunId,
                    null,
                    null,
                    completed.Id,
                    string.IsNullOrWhiteSpace(completed.Notes) ? "Review state transition recorded." : completed.Notes));
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
                    null,
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
                        null,
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
                    null,
                    null,
                    $"Energy {observation.Energy}/10, Sleep {observation.SleepQuality}/10, Appetite {observation.Appetite}/10, Recovery {observation.Recovery}/10.")));
            }
        }

        return events
            .OrderBy(@event => @event.OccurredAtUtc)
            .ThenBy(@event => @event.EventType)
            .ToList();
    }

    private static PatternRunContext BuildPatternRunContext(
        ProtocolRun run,
        List<CheckIn> checkIns,
        List<ProtocolComputationRecord> computations,
        List<ProtocolReviewCompletedEvent> reviewCompletedEvents)
    {
        var attachedCheckIns = checkIns
            .Where(checkIn => checkIn.ProtocolRunId == run.Id)
            .OrderBy(checkIn => checkIn.Date)
            .ToList();
        var runEnd = run.EndedAtUtc ?? run.StartedAtUtc;
        var runComputations = computations
            .Where(record => record.ProtocolRunId == run.Id ||
                (record.ProtocolRunId is null && record.ProtocolId == run.ProtocolId && record.TimestampUtc >= run.StartedAtUtc && record.TimestampUtc <= runEnd))
            .OrderBy(record => record.TimestampUtc)
            .ToList();
        var runReviews = reviewCompletedEvents
            .Where(@event => @event.ProtocolRunId == run.Id ||
                (@event.ProtocolRunId is null && @event.ProtocolId == run.ProtocolId && @event.CompletedAtUtc >= runEnd))
            .OrderBy(@event => @event.CompletedAtUtc)
            .ToList();
        var sequence = new List<string> { "RunStart" };
        if (runComputations.Count > 0)
        {
            sequence.Add("Computation");
        }

        if (HasRunTrendShift(attachedCheckIns))
        {
            sequence.Add("TrendShift");
        }

        if (runReviews.Count > 0)
        {
            sequence.Add("ReviewCompleted");
        }

        return new PatternRunContext(
            run,
            attachedCheckIns,
            runComputations,
            runReviews,
            attachedCheckIns.FirstOrDefault() is null ? null : attachedCheckIns.First().Date - run.StartedAtUtc,
            BuildCheckInIntervals(attachedCheckIns),
            run.EndedAtUtc is null ? null : run.EndedAtUtc.Value - run.StartedAtUtc,
            runComputations.FirstOrDefault() is null ? null : runComputations.First().TimestampUtc - run.StartedAtUtc,
            run.EndedAtUtc is null || runReviews.FirstOrDefault() is null ? null : runReviews.First().CompletedAtUtc - run.EndedAtUtc.Value,
            sequence);
    }

    private static List<MetricPatternSummary> BuildMetricPatterns(List<PatternRunContext> contexts)
    {
        var patterns = new List<MetricPatternSummary>();
        var firstCheckIns = PresentTimeSpans(contexts.Select(context => context.FirstCheckInOffset));
        if (firstCheckIns.Count >= 2)
        {
            patterns.Add(new MetricPatternSummary
            {
                Metric = "First check-in",
                Observation = $"Check-ins typically begin within {FormatRange(firstCheckIns)} of run start."
            });
        }

        var intervals = contexts.SelectMany(context => context.CheckInIntervals).ToList();
        if (intervals.Count >= 2)
        {
            patterns.Add(new MetricPatternSummary
            {
                Metric = "Check-in cadence",
                Observation = $"Attached check-ins recur about every {FormatTypical(AverageTimeSpan(intervals))} in prior runs."
            });
        }

        var durations = PresentTimeSpans(contexts.Select(context => context.Duration));
        if (durations.Count >= 2)
        {
            patterns.Add(new MetricPatternSummary
            {
                Metric = "Run duration",
                Observation = $"Completed runs lasted {FormatRange(durations)}, with a typical duration near {FormatTypical(AverageTimeSpan(durations))}."
            });
        }

        return patterns;
    }

    private static List<EventPatternSummary> BuildEventPatterns(List<PatternRunContext> contexts)
    {
        var patterns = new List<EventPatternSummary>();
        var computations = PresentTimeSpans(contexts.Select(context => context.FirstComputationOffset));
        if (computations.Count >= 2)
        {
            patterns.Add(new EventPatternSummary
            {
                EventType = "Computation",
                TimingPattern = $"Computations appear within {FormatRange(computations)} of run start in prior runs."
            });
        }

        var reviews = PresentTimeSpans(contexts.Select(context => context.ReviewCompletionOffset));
        if (reviews.Count >= 2)
        {
            patterns.Add(new EventPatternSummary
            {
                EventType = "Review completed",
                TimingPattern = $"Review completion appears within {FormatRange(reviews)} after run end."
            });
        }

        return patterns;
    }

    private static List<SequencePatternSummary> BuildSequencePatterns(List<PatternRunContext> contexts)
    {
        return contexts
            .GroupBy(context => string.Join(">", context.Sequence), StringComparer.Ordinal)
            .Where(group => group.Count() >= 2 && group.First().Sequence.Count > 1)
            .Select(group => new SequencePatternSummary
            {
                Sequence = group.First().Sequence,
                Description = $"Observed in {group.Count()} completed runs."
            })
            .Take(4)
            .ToList();
    }

    private static string DeterminePatternConfidence(
        int completedRunCount,
        List<PatternRunContext> contexts,
        List<MetricPatternSummary> metricPatterns,
        List<EventPatternSummary> eventPatterns,
        List<SequencePatternSummary> sequencePatterns)
    {
        if (completedRunCount < 2)
        {
            return "none";
        }

        if (completedRunCount == 2)
        {
            return "low";
        }

        var patternCount = metricPatterns.Count + eventPatterns.Count + sequencePatterns.Count;
        var observedRunCount = contexts.Count(context => context.AttachedCheckIns.Count >= 2);
        return patternCount > 0 && observedRunCount >= 2 ? "moderate" : "low";
    }

    private static PatternComparisonSummary BuildCurrentRunComparison(
        ProtocolRun activeRun,
        List<PatternRunContext> historicalContexts,
        List<CheckIn> checkIns,
        List<ProtocolComputationRecord> computations)
    {
        var matching = new List<string>();
        var divergent = new List<string>();
        var activeCheckIns = checkIns
            .Where(checkIn => checkIn.ProtocolRunId == activeRun.Id)
            .OrderBy(checkIn => checkIn.Date)
            .ToList();
        var historicalFirstOffsets = PresentTimeSpans(historicalContexts.Select(context => context.FirstCheckInOffset));
        if (historicalFirstOffsets.Count >= 2)
        {
            var typicalFirst = AverageTimeSpan(historicalFirstOffsets);
            var tolerance = TimeSpan.FromTicks(Math.Max(TimeSpan.FromHours(12).Ticks, typicalFirst.Ticks / 2));
            var activeFirst = activeCheckIns.FirstOrDefault();
            if (activeFirst is not null)
            {
                var currentOffset = activeFirst.Date - activeRun.StartedAtUtc;
                if (Math.Abs((currentOffset - typicalFirst).Ticks) <= tolerance.Ticks)
                {
                    matching.Add("Check-in timing aligns with prior runs");
                }
                else if (currentOffset > typicalFirst)
                {
                    divergent.Add("First check-in later than typical");
                }
                else
                {
                    divergent.Add("First check-in earlier than typical");
                }
            }
            else if (DateTime.UtcNow - activeRun.StartedAtUtc > typicalFirst + tolerance)
            {
                divergent.Add("Current run is later than prior runs to first observation");
            }
        }

        var historicalComputationOffsets = PresentTimeSpans(historicalContexts.Select(context => context.FirstComputationOffset));
        if (historicalComputationOffsets.Count >= 2)
        {
            var typicalComputation = AverageTimeSpan(historicalComputationOffsets);
            var tolerance = TimeSpan.FromTicks(Math.Max(TimeSpan.FromHours(12).Ticks, typicalComputation.Ticks / 2));
            var activeComputation = computations
                .Where(record => record.ProtocolRunId == activeRun.Id)
                .OrderBy(record => record.TimestampUtc)
                .FirstOrDefault();
            if (activeComputation is not null)
            {
                var currentOffset = activeComputation.TimestampUtc - activeRun.StartedAtUtc;
                if (Math.Abs((currentOffset - typicalComputation).Ticks) <= tolerance.Ticks)
                {
                    matching.Add("Computation timing matches prior pattern");
                }
                else if (currentOffset > typicalComputation)
                {
                    divergent.Add("Computation appears later than usual");
                }
                else
                {
                    divergent.Add("Computation appears earlier than typical");
                }
            }
        }

        var similarity = matching.Count == 0 && divergent.Count == 0
            ? "none"
            : matching.Count > 0 && divergent.Count == 0 ? "moderate" : "low";

        return new PatternComparisonSummary
        {
            Similarity = similarity,
            MatchingSignals = matching,
            DivergentSignals = divergent
        };
    }

    private static List<DriftSignalSummary> BuildDriftSignals(
        PatternRunContext current,
        List<PatternRunContext> historical,
        List<MissionControlObservationSignalResponse> observationSignals)
    {
        var signals = new List<DriftSignalSummary>();
        AddCheckInTimingDrift(signals, current, historical);
        AddRunDurationDrift(signals, current, historical);
        AddComputationTimingDrift(signals, current, historical);
        AddReviewTimingDrift(signals, current, historical);
        AddSequenceDrift(signals, current, historical);
        AddSignalDensityDrift(signals, current, historical, observationSignals);

        return signals
            .GroupBy(signal => signal.Type, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(signal => signal.Severity == "moderate")
                .ThenBy(signal => signal.Description, StringComparer.Ordinal)
                .First())
            .OrderBy(signal => signal.Type, StringComparer.Ordinal)
            .ToList();
    }

    private static void AddCheckInTimingDrift(
        List<DriftSignalSummary> signals,
        PatternRunContext current,
        List<PatternRunContext> historical)
    {
        var historicalFirst = PresentTimeSpans(historical.Select(context => context.FirstCheckInOffset));
        var currentFirst = current.FirstCheckInOffset;
        var firstLate = currentFirst is not null && historicalFirst.Count >= 2 && currentFirst.Value > historicalFirst.Max();
        var missingFirst = currentFirst is null && historicalFirst.Count >= 2 && DateTime.UtcNow - current.Run.StartedAtUtc > historicalFirst.Max();

        var historicalAverageIntervals = historical
            .Select(context => context.CheckInIntervals.Count == 0 ? (TimeSpan?)null : AverageTimeSpan(context.CheckInIntervals))
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .ToList();
        var currentAverageInterval = current.CheckInIntervals.Count == 0 ? (TimeSpan?)null : AverageTimeSpan(current.CheckInIntervals);
        var intervalLate = currentAverageInterval is not null && historicalAverageIntervals.Count >= 2 && currentAverageInterval.Value > historicalAverageIntervals.Max();

        if (firstLate && intervalLate)
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "checkin_timing",
                Severity = "moderate",
                Description = "Check-in cadence later than historical pattern"
            });
        }
        else if (firstLate || missingFirst)
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "checkin_timing",
                Severity = "mild",
                Description = "First check-in later than historical range"
            });
        }
        else if (intervalLate)
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "checkin_timing",
                Severity = "mild",
                Description = "Average check-in interval exceeds historical range"
            });
        }
    }

    private static void AddRunDurationDrift(
        List<DriftSignalSummary> signals,
        PatternRunContext current,
        List<PatternRunContext> historical)
    {
        var historicalDurations = PresentTimeSpans(historical.Select(context => context.Duration));
        if (historicalDurations.Count < 2)
        {
            return;
        }

        var currentDuration = current.Duration ?? DateTime.UtcNow - current.Run.StartedAtUtc;
        if (currentDuration > historicalDurations.Max())
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "run_duration",
                Severity = "moderate",
                Description = "Current run duration exceeds historical range"
            });
        }
    }

    private static void AddComputationTimingDrift(
        List<DriftSignalSummary> signals,
        PatternRunContext current,
        List<PatternRunContext> historical)
    {
        var historicalComputationOffsets = PresentTimeSpans(historical.Select(context => context.FirstComputationOffset));
        if (historicalComputationOffsets.Count >= 2 && current.FirstComputationOffset is not null)
        {
            if (current.FirstComputationOffset.Value < historicalComputationOffsets.Min() ||
                current.FirstComputationOffset.Value > historicalComputationOffsets.Max())
            {
                signals.Add(new DriftSignalSummary
                {
                    Type = "computation_timing",
                    Severity = "mild",
                    Description = "Computation occurred outside historical timing range"
                });
            }
        }

        var historicalComputationCounts = historical.Select(context => context.Computations.Count).ToList();
        if (historicalComputationCounts.Count >= 2 &&
            current.Computations.Count >= 2 &&
            current.Computations.Count > historicalComputationCounts.Max())
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "computation_timing",
                Severity = "moderate",
                Description = "Repeated computation behavior exceeds historical range"
            });
        }
    }

    private static void AddReviewTimingDrift(
        List<DriftSignalSummary> signals,
        PatternRunContext current,
        List<PatternRunContext> historical)
    {
        var historicalReviewOffsets = PresentTimeSpans(historical.Select(context => context.ReviewCompletionOffset));
        if (historicalReviewOffsets.Count < 2 || current.Run.EndedAtUtc is null)
        {
            return;
        }

        if (current.ReviewCompletionOffset is null)
        {
            if (DateTime.UtcNow - current.Run.EndedAtUtc.Value > historicalReviewOffsets.Max())
            {
                signals.Add(new DriftSignalSummary
                {
                    Type = "review_timing",
                    Severity = "mild",
                    Description = "Review not completed within historical timing range"
                });
            }

            return;
        }

        if (current.ReviewCompletionOffset.Value > historicalReviewOffsets.Max())
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "review_timing",
                Severity = "moderate",
                Description = "Review completion later than historical range"
            });
        }
    }

    private static void AddSequenceDrift(
        List<DriftSignalSummary> signals,
        PatternRunContext current,
        List<PatternRunContext> historical)
    {
        var recurringSequences = historical
            .GroupBy(context => string.Join(">", context.Sequence), StringComparer.Ordinal)
            .Where(group => group.Count() >= 2 && group.First().Sequence.Count > 1)
            .Select(group => group.First().Sequence)
            .ToList();

        if (recurringSequences.Count == 0)
        {
            return;
        }

        var currentSequence = string.Join(">", current.Sequence);
        if (!recurringSequences.Any(sequence => string.Equals(string.Join(">", sequence), currentSequence, StringComparison.Ordinal)))
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "sequence_break",
                Severity = "moderate",
                Description = "Recurring sequence not observed"
            });
        }
    }

    private static void AddSignalDensityDrift(
        List<DriftSignalSummary> signals,
        PatternRunContext current,
        List<PatternRunContext> historical,
        List<MissionControlObservationSignalResponse> observationSignals)
    {
        var historicalCheckInCounts = historical.Select(context => context.AttachedCheckIns.Count).ToList();
        if (historicalCheckInCounts.Count >= 2 &&
            current.AttachedCheckIns.Count < historicalCheckInCounts.Min() &&
            observationSignals.Any(signal => signal.Type == "gap"))
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "signal_density",
                Severity = "mild",
                Description = "Observation gaps increased relative to historical runs"
            });
        }

        var historicalTrendShiftCounts = historical.Select(context => TrendShiftMetricCount(context.AttachedCheckIns)).ToList();
        var currentTrendShiftCount = TrendShiftMetricCount(current.AttachedCheckIns);
        if (historicalTrendShiftCounts.Count >= 2 &&
            currentTrendShiftCount > historicalTrendShiftCounts.Max())
        {
            signals.Add(new DriftSignalSummary
            {
                Type = "signal_density",
                Severity = "moderate",
                Description = "Trend-shift signal density exceeds historical range"
            });
        }
    }

    private static string ClassifyDrift(
        List<DriftSignalSummary> signals,
        ProtocolPatternSnapshot patternSnapshot,
        bool sparseCurrentData)
    {
        if (signals.Count == 0)
        {
            return "none";
        }

        if (sparseCurrentData && signals.All(signal => signal.Type is "sequence_break" or "signal_density"))
        {
            return "mild";
        }

        var dimensions = signals.Select(signal => signal.Type).Distinct(StringComparer.Ordinal).Count();
        var hasStrongPersistentDeviation = signals.Count == 1 && signals[0].Severity == "moderate";
        var similarityDegraded = patternSnapshot.CurrentRunComparison?.Similarity == "low" ||
            patternSnapshot.CurrentRunComparison?.DivergentSignals.Count >= 2;
        var recurringSequenceMissing = signals.Any(signal => signal.Type == "sequence_break");

        if (dimensions >= 2 && (similarityDegraded || recurringSequenceMissing))
        {
            return "regime_shift";
        }

        if (signals.Count >= 3 || dimensions >= 2 || hasStrongPersistentDeviation)
        {
            return "moderate";
        }

        return "mild";
    }

    private static string MapRegimeState(string driftState)
    {
        return driftState switch
        {
            "regime_shift" => "shifted",
            "mild" or "moderate" => "drifting",
            _ => "stable"
        };
    }

    private static SequenceRunContext BuildSequenceRunContext(
        ProtocolRun run,
        List<CheckIn> checkIns,
        List<ProtocolComputationRecord> computations,
        List<ProtocolReviewCompletedEvent> reviewCompletedEvents,
        List<Protocol> lineage)
    {
        var runEnd = run.EndedAtUtc ?? run.StartedAtUtc;
        var events = new List<SequenceEvent>
        {
            new("RunStarted", run.StartedAtUtc)
        };

        var firstCheckIn = checkIns
            .Where(checkIn => checkIn.ProtocolRunId == run.Id)
            .OrderBy(checkIn => checkIn.Date)
            .FirstOrDefault();
        if (firstCheckIn is not null)
        {
            events.Add(new SequenceEvent("FirstCheckIn", firstCheckIn.Date));
        }

        var firstComputation = computations
            .Where(record => record.ProtocolRunId == run.Id ||
                (record.ProtocolRunId is null && record.ProtocolId == run.ProtocolId && record.TimestampUtc >= run.StartedAtUtc && record.TimestampUtc <= runEnd))
            .OrderBy(record => record.TimestampUtc)
            .FirstOrDefault();
        if (firstComputation is not null)
        {
            events.Add(new SequenceEvent("ComputationRecorded", firstComputation.TimestampUtc));
        }

        if (run.EndedAtUtc is not null)
        {
            events.Add(new SequenceEvent("RunClosed", run.EndedAtUtc.Value));
        }

        var firstReview = reviewCompletedEvents
            .Where(@event => @event.ProtocolRunId == run.Id ||
                (@event.ProtocolRunId is null && @event.ProtocolId == run.ProtocolId && @event.CompletedAtUtc >= runEnd))
            .OrderBy(@event => @event.CompletedAtUtc)
            .FirstOrDefault();
        if (firstReview is not null)
        {
            events.Add(new SequenceEvent("ReviewCompleted", firstReview.CompletedAtUtc));
        }

        var evolution = lineage
            .Where(candidate => candidate.EvolvedFromRunId == run.Id)
            .OrderBy(candidate => candidate.CreatedAtUtc)
            .FirstOrDefault();
        if (evolution is not null)
        {
            events.Add(new SequenceEvent("EvolutionEvent", evolution.CreatedAtUtc));
        }

        return new SequenceRunContext(run, events.OrderBy(@event => @event.OccurredAtUtc).ToList());
    }

    private static List<SequenceTransitionPattern> BuildCommonSequenceTransitions(List<SequenceRunContext> contexts)
    {
        return contexts
            .SelectMany(context => BuildSequenceTransitions(context.Events))
            .GroupBy(transition => $"{transition.From.EventType}>{transition.To.EventType}", StringComparer.Ordinal)
            .Where(group => group.Count() >= 2)
            .Select(group => new SequenceTransitionPattern(
                group.First().From.EventType,
                group.First().To.EventType,
                group.Select(transition => transition.To.OccurredAtUtc - transition.From.OccurredAtUtc).ToList(),
                group.Count()))
            .OrderByDescending(pattern => pattern.ObservedCount)
            .ThenBy(pattern => pattern.FromState, StringComparer.Ordinal)
            .ThenBy(pattern => pattern.ToEventType, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<(SequenceEvent From, SequenceEvent To)> BuildSequenceTransitions(List<SequenceEvent> events)
    {
        for (var index = 1; index < events.Count; index++)
        {
            yield return (events[index - 1], events[index]);
        }
    }

    private static SequenceExpectationResolution? ResolveExpectedNextEvent(
        SequenceRunContext current,
        List<SequenceTransitionPattern> commonTransitions,
        int historicalRunCount)
    {
        var orderedEvents = current.Events.OrderBy(@event => @event.OccurredAtUtc).ToList();
        for (var index = orderedEvents.Count - 1; index >= 0; index--)
        {
            var anchor = orderedEvents[index];
            var transition = SelectCommonTransition(anchor.EventType, commonTransitions);
            if (transition is null)
            {
                continue;
            }

            var nextActual = orderedEvents.Skip(index + 1).FirstOrDefault();
            var expected = new ExpectedNextEventSummary
            {
                EventType = transition.ToEventType,
                Description = $"Based on prior runs, the next commonly observed event from {FormatSequenceEventName(anchor.EventType).ToLowerInvariant()} is {FormatSequenceEventName(transition.ToEventType).ToLowerInvariant()}.",
                TimingWindow = $"Usually observed within {FormatRange(transition.Offsets)} of {FormatSequenceEventName(anchor.EventType).ToLowerInvariant()}.",
                Confidence = SequenceConfidence(historicalRunCount, transition, commonTransitions)
            };

            if (nextActual is null)
            {
                var elapsed = DateTime.UtcNow - anchor.OccurredAtUtc;
                var state = elapsed <= transition.Offsets.Max() ? "pending" : "late";
                var note = state == "pending"
                    ? $"The next commonly observed event has not yet occurred and remains inside the common sequence window."
                    : $"The next commonly observed event has not yet occurred and the current run is beyond the common sequence window.";

                return new SequenceExpectationResolution(
                    expected,
                    new CurrentSequenceStatusSummary
                    {
                        State = state,
                        Notes = new List<string> { note }
                    });
            }

            if (string.Equals(nextActual.EventType, transition.ToEventType, StringComparison.Ordinal))
            {
                var timingState = nextActual.OccurredAtUtc - anchor.OccurredAtUtc <= transition.Offsets.Max()
                    ? "occurred within common sequence window"
                    : "occurred outside common sequence window";
                return new SequenceExpectationResolution(
                    expected,
                    new CurrentSequenceStatusSummary
                    {
                        State = timingState.StartsWith("occurred within", StringComparison.Ordinal) ? "aligned" : "late",
                        Notes = new List<string> { $"{FormatSequenceEventName(nextActual.EventType)} {timingState}." }
                    });
            }

            return new SequenceExpectationResolution(
                expected,
                new CurrentSequenceStatusSummary
                {
                    State = "diverging",
                    Notes = new List<string>
                    {
                        $"{FormatSequenceEventName(nextActual.EventType)} occurred where prior runs commonly showed {FormatSequenceEventName(transition.ToEventType).ToLowerInvariant()}."
                    }
                });
        }

        return null;
    }

    private static SequenceTransitionPattern? SelectCommonTransition(string fromState, List<SequenceTransitionPattern> commonTransitions)
    {
        return commonTransitions
            .Where(transition => transition.FromState == fromState)
            .OrderByDescending(transition => transition.ObservedCount)
            .ThenBy(transition => AverageTimeSpan(transition.Offsets))
            .FirstOrDefault();
    }

    private static string SequenceConfidence(int historicalRunCount, SequenceTransitionPattern transition, List<SequenceTransitionPattern> commonTransitions)
    {
        var pathsFromState = commonTransitions.Count(candidate => candidate.FromState == transition.FromState);
        if (historicalRunCount < 2 || transition.ObservedCount < 2)
        {
            return "none";
        }

        if (historicalRunCount < 3 || transition.ObservedCount == 2 || pathsFromState > 1)
        {
            return "low";
        }

        return "moderate";
    }

    private static string FormatSequenceEventName(string eventType)
    {
        return eventType switch
        {
            "RunStarted" => "run start",
            "FirstCheckIn" => "first check-in",
            "ComputationRecorded" => "computation recorded",
            "RunClosed" => "run close",
            "ReviewCompleted" => "review completion",
            "EvolutionEvent" => "evolution event",
            _ => eventType
        };
    }

    private static ProtocolReviewTimelineEventResponse AnnotateTimelineEvent(
        ProtocolReviewTimelineEventResponse @event,
        ProtocolPatternSnapshot? patternSnapshot,
        ProtocolSequenceExpectationSnapshot? sequenceExpectationSnapshot)
    {
        var comparison = patternSnapshot?.CurrentRunComparison;
        var sequenceAnnotation = SequenceAnnotationForTimeline(@event, sequenceExpectationSnapshot);
        if ((comparison is null || @event.RunId is null) && sequenceAnnotation is null)
        {
            return @event;
        }

        var annotation = comparison is null ? null : @event.EventType switch
        {
            "check_in" when comparison.MatchingSignals.Any(signal => signal.Contains("Check-in timing aligns", StringComparison.OrdinalIgnoreCase)) => "matches prior pattern",
            "check_in" when comparison.DivergentSignals.Any(signal => signal.Contains("later", StringComparison.OrdinalIgnoreCase)) => "later than usual",
            "check_in" when comparison.DivergentSignals.Any(signal => signal.Contains("earlier", StringComparison.OrdinalIgnoreCase)) => "earlier than typical",
            "computation" when comparison.MatchingSignals.Any(signal => signal.Contains("Computation timing", StringComparison.OrdinalIgnoreCase)) => "matches prior pattern",
            "computation" when comparison.DivergentSignals.Any(signal => signal.Contains("later", StringComparison.OrdinalIgnoreCase)) => "later than usual",
            "computation" when comparison.DivergentSignals.Any(signal => signal.Contains("earlier", StringComparison.OrdinalIgnoreCase)) => "earlier than typical",
            _ => null
        };

        var annotations = new[] { annotation, sequenceAnnotation }
            .Where(value => !string.IsNullOrWhiteSpace(value) && !@event.Detail.Contains(value!, StringComparison.OrdinalIgnoreCase))
            .Select(value => value!)
            .ToList();
        if (annotations.Count == 0)
        {
            return @event;
        }

        return @event with { Detail = $"{@event.Detail} {string.Join(" ", annotations)}" };
    }

    private static string? SequenceAnnotationForTimeline(
        ProtocolReviewTimelineEventResponse @event,
        ProtocolSequenceExpectationSnapshot? sequenceExpectationSnapshot)
    {
        if (sequenceExpectationSnapshot?.ExpectedNextEvent is null || @event.RunId is null)
        {
            return null;
        }

        var eventType = @event.EventType switch
        {
            "check_in" => "FirstCheckIn",
            "computation" => "ComputationRecorded",
            "review_completed" => "ReviewCompleted",
            "evolution" => "EvolutionEvent",
            "run_completed" or "run_abandoned" => "RunClosed",
            _ => null
        };

        if (eventType == sequenceExpectationSnapshot.ExpectedNextEvent.EventType)
        {
            return sequenceExpectationSnapshot.CurrentStatus?.State switch
            {
                "aligned" => "Sequence expectation: occurred within common sequence window.",
                "late" => "Sequence expectation: occurred outside common sequence window.",
                _ => "Sequence expectation: usual next event from prior runs."
            };
        }

        return sequenceExpectationSnapshot.CurrentStatus?.State == "diverging"
            ? "Sequence expectation: sequence diverging from prior runs."
            : null;
    }

    private static List<TimeSpan> BuildCheckInIntervals(List<CheckIn> attachedCheckIns)
    {
        var intervals = new List<TimeSpan>();
        for (var index = 1; index < attachedCheckIns.Count; index++)
        {
            intervals.Add(attachedCheckIns[index].Date - attachedCheckIns[index - 1].Date);
        }

        return intervals;
    }

    private static bool HasRunTrendShift(List<CheckIn> attachedCheckIns)
    {
        if (attachedCheckIns.Count < 2)
        {
            return false;
        }

        return new[] { "energy", "recovery", "sleep", "appetite" }
            .Any(metric =>
            {
                var values = attachedCheckIns.Select(checkIn => MetricValue(metric, checkIn)).ToList();
                return values.Max() - values.Min() >= 2;
            });
    }

    private static int TrendShiftMetricCount(List<CheckIn> attachedCheckIns)
    {
        if (attachedCheckIns.Count < 2)
        {
            return 0;
        }

        return new[] { "energy", "recovery", "sleep", "appetite" }
            .Count(metric =>
            {
                var values = attachedCheckIns.Select(checkIn => MetricValue(metric, checkIn)).ToList();
                return values.Max() - values.Min() >= 2;
            });
    }

    private static TimeSpan AverageTimeSpan(List<TimeSpan> values)
    {
        return TimeSpan.FromTicks((long)values.Average(value => value.Ticks));
    }

    private static List<TimeSpan> PresentTimeSpans(IEnumerable<TimeSpan?> values)
    {
        return values
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
    }

    private static string FormatRange(List<TimeSpan> values)
    {
        var min = values.Min();
        var max = values.Max();
        return FormatTypical(min) == FormatTypical(max)
            ? FormatTypical(min)
            : $"{FormatTypical(min)}-{FormatTypical(max)}";
    }

    private static string FormatTypical(TimeSpan value)
    {
        if (value.TotalHours < 48)
        {
            var hours = Math.Max(1, (int)Math.Round(value.TotalHours));
            return $"{hours} hour{(hours == 1 ? string.Empty : "s")}";
        }

        var days = Math.Max(1, (int)Math.Round(value.TotalDays));
        return $"{days} day{(days == 1 ? string.Empty : "s")}";
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

    private static bool HasPendingReview(
        ProtocolRun? latestClosedRun,
        Protocol? latestEvolved,
        ProtocolReviewCompletedEvent? latestReviewCompleted)
    {
        var latestStateChange = new[]
            {
                latestClosedRun?.EndedAtUtc ?? latestClosedRun?.StartedAtUtc,
                latestEvolved?.CreatedAtUtc
            }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        return latestStateChange > DateTime.MinValue &&
            (latestReviewCompleted is null || latestReviewCompleted.CompletedAtUtc < latestStateChange);
    }

    private static List<MissionControlObservationSignalResponse> BuildObservationSignals(
        ProtocolRun? activeRun,
        List<ProtocolRun> runs,
        List<CheckIn> checkIns)
    {
        var signals = new List<MissionControlObservationSignalResponse>();
        var orderedCheckIns = checkIns.OrderBy(checkIn => checkIn.Date).ToList();
        var referenceRun = activeRun ?? runs
            .OrderByDescending(run => run.EndedAtUtc ?? run.StartedAtUtc)
            .FirstOrDefault();

        if (referenceRun is not null)
        {
            var attached = orderedCheckIns
                .Where(checkIn => checkIn.ProtocolRunId == referenceRun.Id)
                .OrderBy(checkIn => checkIn.Date)
                .ToList();
            var lastObservation = attached.LastOrDefault()?.Date ?? referenceRun.StartedAtUtc;
            var daysSince = Math.Floor((DateTime.UtcNow.Date - lastObservation.Date).TotalDays);

            if (attached.Count == 0)
            {
                signals.Add(new MissionControlObservationSignalResponse(
                    "gap",
                    "medium",
                    null,
                    "No check-ins are attached to the current protocol loop."));
            }
            else if (daysSince >= 3)
            {
                signals.Add(new MissionControlObservationSignalResponse(
                    "gap",
                    daysSince >= 7 ? "high" : "medium",
                    null,
                    $"Last attached check-in was {daysSince:0} day{(daysSince == 1 ? string.Empty : "s")} ago."));
            }
        }
        else if (orderedCheckIns.Count == 0)
        {
            signals.Add(new MissionControlObservationSignalResponse(
                "gap",
                "low",
                null,
                "No observations have been captured yet."));
        }

        signals.AddRange(BuildTrendShiftSignals(orderedCheckIns));

        return signals
            .GroupBy(signal => $"{signal.Type}:{signal.Metric}:{signal.Detail}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6)
            .ToList();
    }

    private static IEnumerable<MissionControlObservationSignalResponse> BuildTrendShiftSignals(List<CheckIn> checkIns)
    {
        if (checkIns.Count < 4)
        {
            yield break;
        }

        foreach (var metric in new[] { "weight", "energy", "recovery", "sleep", "appetite" })
        {
            var values = checkIns.Select(checkIn => MetricValue(metric, checkIn)).ToList();
            var prior = values.Take(values.Count - 1).ToList();
            var last = values.Last();
            var priorAverage = prior.Average();
            var meanDeviation = prior.Average(value => Math.Abs(value - priorAverage));
            var threshold = metric == "weight" ? Math.Max(3m, meanDeviation * 2m) : Math.Max(2m, meanDeviation * 2m);

            if (Math.Abs(last - priorAverage) >= threshold)
            {
                yield return new MissionControlObservationSignalResponse(
                    "trend_shift",
                    Math.Abs(last - priorAverage) >= threshold * 1.5m ? "high" : "medium",
                    metric,
                    $"{FormatMetricName(metric)} moved outside the recent observation band.");
            }
        }
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

    private static string ComputationLabel(string type)
    {
        return NormalizeComputationType(type) switch
        {
            "reconstitution" => "Reconstitution calculated",
            "volume" => "Dosage adjusted (math only)",
            "dosage" => "Dosage adjusted (math only)",
            "conversion" => "Unit conversion calculated",
            _ => "Protocol computation recorded"
        };
    }

    private static string ComputationDetail(ProtocolComputationRecord computation)
    {
        return string.IsNullOrWhiteSpace(computation.OutputResult)
            ? "Mathematical trace recorded for this protocol."
            : computation.OutputResult;
    }

    private static string NormalizeComputationType(string type)
    {
        return type.Trim().ToLowerInvariant().Replace("_", "-");
    }

    private static decimal MetricValue(string metric, CheckIn checkIn)
    {
        return metric switch
        {
            "weight" => checkIn.Weight,
            "energy" => checkIn.Energy,
            "recovery" => checkIn.Recovery,
            "sleep" => checkIn.SleepQuality,
            "appetite" => checkIn.Appetite,
            _ => 0
        };
    }

    private static string FormatMetricName(string metric)
    {
        return metric switch
        {
            "sleep" => "Sleep",
            _ => char.ToUpperInvariant(metric[0]) + metric[1..]
        };
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

    private sealed record PatternRunContext(
        ProtocolRun Run,
        List<CheckIn> AttachedCheckIns,
        List<ProtocolComputationRecord> Computations,
        List<ProtocolReviewCompletedEvent> ReviewCompletedEvents,
        TimeSpan? FirstCheckInOffset,
        List<TimeSpan> CheckInIntervals,
        TimeSpan? Duration,
        TimeSpan? FirstComputationOffset,
        TimeSpan? ReviewCompletionOffset,
        List<string> Sequence);

    private sealed record SequenceEvent(string EventType, DateTime OccurredAtUtc);

    private sealed record SequenceRunContext(ProtocolRun Run, List<SequenceEvent> Events);

    private sealed record SequenceTransitionPattern(string FromState, string ToEventType, List<TimeSpan> Offsets, int ObservedCount);

    private sealed record SequenceExpectationResolution(ExpectedNextEventSummary ExpectedNextEvent, CurrentSequenceStatusSummary CurrentStatus);

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

    private static ProtocolComputationRecordResponse MapComputation(ProtocolComputationRecord record)
    {
        return new ProtocolComputationRecordResponse(
            record.Id,
            record.ProtocolId,
            record.ProtocolRunId,
            record.Type,
            record.InputSnapshot,
            record.OutputResult,
            record.TimestampUtc);
    }

    private static ProtocolReviewCompletedEventResponse MapReviewCompletedEvent(ProtocolReviewCompletedEvent @event)
    {
        return new ProtocolReviewCompletedEventResponse(
            @event.Id,
            @event.ProtocolId,
            @event.ProtocolRunId,
            @event.CompletedAtUtc,
            @event.Notes);
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
    Task<ProtocolPatternSnapshot> GetPatternSnapshotAsync(Guid protocolId, CancellationToken cancellationToken = default);
    Task<ProtocolDriftSnapshot> GetDriftSnapshotAsync(Guid protocolId, CancellationToken cancellationToken = default);
    Task<ProtocolSequenceExpectationSnapshot> GetSequenceExpectationSnapshotAsync(Guid protocolId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse> StartRunAsync(Guid protocolId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse?> GetActiveRunAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<MissionControlResponse> GetMissionControlAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse> CompleteRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<ProtocolRunResponse> AbandonRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<ProtocolComputationRecordResponse> RecordComputationAsync(Guid protocolId, CreateProtocolComputationRequest request, CancellationToken cancellationToken = default);
    Task<ProtocolReviewCompletedEventResponse> CompleteReviewAsync(Guid protocolId, CompleteProtocolReviewRequest request, CancellationToken cancellationToken = default);
    Task<ProtocolResponse> EvolveFromRunAsync(Guid runId, EvolveProtocolFromRunRequest request, CancellationToken cancellationToken = default);
    Task<CurrentStackIntelligenceResponse> GetCurrentStackIntelligenceAsync(Guid personId, CancellationToken cancellationToken = default);
}
