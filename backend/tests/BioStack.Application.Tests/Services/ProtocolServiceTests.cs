namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public class ProtocolServiceTests
{
    [Fact]
    public async Task EvolveFromRunAsync_WithCompletedRun_CreatesNewDraftVersionWithoutMutatingSource()
    {
        var personId = Guid.NewGuid();
        var sourceProtocolId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var compoundId = Guid.NewGuid();
        var source = new Protocol
        {
            Id = sourceProtocolId,
            PersonId = personId,
            Name = "Observation stack",
            Version = 2,
            Items = new List<ProtocolItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProtocolId = sourceProtocolId,
                    CompoundRecordId = compoundId,
                    Notes = "Original item note",
                    CompoundNameSnapshot = "BPC-157",
                    CompoundCategorySnapshot = CompoundCategory.Peptide.ToString(),
                    CompoundStatusSnapshot = CompoundStatus.Active.ToString(),
                    CompoundNotesSnapshot = "Original compound note"
                }
            }
        };
        source.Items.First().Protocol = source;

        var run = new ProtocolRun
        {
            Id = runId,
            ProtocolId = sourceProtocolId,
            PersonId = personId,
            Status = ProtocolRunStatus.Completed,
            StartedAtUtc = DateTime.UtcNow.AddDays(-5),
            EndedAtUtc = DateTime.UtcNow,
            Protocol = source
        };

        Protocol? capturedDraft = null;
        var protocolRepository = new Mock<IProtocolRepository>();
        protocolRepository.Setup(repository => repository.GetMaxVersionInLineageAsync(source, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        protocolRepository.Setup(repository => repository.AddAsync(It.IsAny<Protocol>(), It.IsAny<CancellationToken>()))
            .Callback<Protocol, CancellationToken>((protocol, _) => capturedDraft = protocol)
            .Returns(Task.CompletedTask);
        protocolRepository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        protocolRepository.Setup(repository => repository.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedDraft);
        protocolRepository.Setup(repository => repository.GetLineageAsync(It.IsAny<Protocol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new List<Protocol> { source, capturedDraft! });

        var runRepository = new Mock<IProtocolRunRepository>();
        runRepository.Setup(repository => repository.GetWithProtocolAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        runRepository.Setup(repository => repository.GetActiveByProtocolIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProtocolRun?)null);
        runRepository.Setup(repository => repository.GetLatestByProtocolIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var checkInRepository = new Mock<ICheckInRepository>();
        checkInRepository.Setup(repository => repository.GetByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CheckIn>());

        var service = CreateService(
            protocolRepository.Object,
            runRepository.Object,
            checkInRepository.Object);

        var response = await service.EvolveFromRunAsync(runId, new EvolveProtocolFromRunRequest(null), CancellationToken.None);

        Assert.NotNull(capturedDraft);
        Assert.Equal(sourceProtocolId, capturedDraft!.ParentProtocolId);
        Assert.Equal(sourceProtocolId, capturedDraft.OriginProtocolId);
        Assert.Equal(runId, capturedDraft.EvolvedFromRunId);
        Assert.True(capturedDraft.IsDraft);
        Assert.Equal(3, capturedDraft.Version);
        Assert.Equal(2, source.Version);
        Assert.Equal("BPC-157", response.Items.Single().Compound?.Name);
        Assert.Contains("Based on this run's observations", response.EvolutionContext);
    }

    [Fact]
    public async Task EvolveFromRunAsync_WithActiveRun_RejectsEvolution()
    {
        var runId = Guid.NewGuid();
        var source = new Protocol { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), Name = "Active stack" };
        var run = new ProtocolRun
        {
            Id = runId,
            ProtocolId = source.Id,
            PersonId = source.PersonId,
            Status = ProtocolRunStatus.Active,
            Protocol = source
        };

        var runRepository = new Mock<IProtocolRunRepository>();
        runRepository.Setup(repository => repository.GetWithProtocolAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var service = CreateService(new Mock<IProtocolRepository>().Object, runRepository.Object, new Mock<ICheckInRepository>().Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EvolveFromRunAsync(runId, new EvolveProtocolFromRunRequest(null), CancellationToken.None));

        Assert.Contains("completed or abandoned", ex.Message);
    }

    [Fact]
    public async Task GetProtocolReviewAsync_ReturnsDeterministicLineageSectionsWithoutMutatingHistory()
    {
        var personId = Guid.NewGuid();
        var compoundId = Guid.NewGuid();
        var version1 = CreateProtocolVersion(personId, Guid.NewGuid(), compoundId, 1, null, "Recovery baseline", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var version2 = CreateProtocolVersion(personId, Guid.NewGuid(), compoundId, 2, version1.Id, "Recovery schedule changed", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        version2.EvolvedFromRunId = Guid.NewGuid();
        version2.OriginProtocolId = version1.Id;

        var run1 = CreateRun(personId, version1, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc));
        var run2 = CreateRun(personId, version2, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc));
        version2.EvolvedFromRunId = run1.Id;

        var checkIns = new List<CheckIn>
        {
            CreateCheckIn(personId, null, new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc), energy: 5, recovery: 4, sleep: 6, appetite: 5),
            CreateCheckIn(personId, run1.Id, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), energy: 6, recovery: 5, sleep: 6, appetite: 5),
            CreateCheckIn(personId, run1.Id, new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc), energy: 7, recovery: 7, sleep: 6, appetite: 5),
            CreateCheckIn(personId, null, new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc), energy: 5, recovery: 7, sleep: 6, appetite: 5),
            CreateCheckIn(personId, run2.Id, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), energy: 7, recovery: 7, sleep: 6, appetite: 5),
            CreateCheckIn(personId, run2.Id, new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), energy: 8, recovery: 4, sleep: 6, appetite: 5)
        };

        var protocolRepository = new Mock<IProtocolRepository>();
        protocolRepository.Setup(repository => repository.GetWithItemsAsync(version2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version2);
        protocolRepository.Setup(repository => repository.GetLineageAsync(version2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Protocol> { version1, version2 });

        var runRepository = new Mock<IProtocolRunRepository>();
        runRepository.Setup(repository => repository.GetByProtocolIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProtocolRun> { run1, run2 });

        var checkInRepository = new Mock<ICheckInRepository>();
        checkInRepository.Setup(repository => repository.GetByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkIns);

        var service = CreateService(protocolRepository.Object, runRepository.Object, checkInRepository.Object);

        var review = await service.GetProtocolReviewAsync(version2.Id, CancellationToken.None);

        Assert.Equal(version1.Id, review.LineageRootProtocolId);
        Assert.Equal(2, review.Versions.Count);
        Assert.Equal(2, review.Versions.Sum(version => version.Runs.Count));
        Assert.Contains(review.Sections, section => section.Type == "alignment" && section.Summary.Contains("Energy trend moved up", StringComparison.Ordinal));
        Assert.Contains(review.Sections, section => section.Type == "divergence" && section.Summary.Contains("Recovery observations moved", StringComparison.Ordinal));
        Assert.Contains(review.Sections, section => section.Type == "neutral" && section.Summary.Contains("flat", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(review.Timeline, entry => entry.EventType == "evolution" && entry.RunId == run1.Id);
        Assert.Contains(review.Timeline, entry => entry.EventType == "check_in" && entry.CheckInId == checkIns[1].Id);
        Assert.Equal("Recovery baseline", version1.Items.Single().CompoundNotesSnapshot);
    }

    [Fact]
    public async Task GetProtocolReviewAsync_WithSparseRuns_ReportsObservationGaps()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Sparse", DateTime.UtcNow.AddDays(-5));
        var run = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-3));
        var checkIns = new List<CheckIn>
        {
            CreateCheckIn(personId, run.Id, DateTime.UtcNow.AddDays(-2), energy: 5, recovery: 5, sleep: 5, appetite: 5)
        };

        var protocolRepository = new Mock<IProtocolRepository>();
        protocolRepository.Setup(repository => repository.GetWithItemsAsync(protocol.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(protocol);
        protocolRepository.Setup(repository => repository.GetLineageAsync(protocol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Protocol> { protocol });

        var runRepository = new Mock<IProtocolRunRepository>();
        runRepository.Setup(repository => repository.GetByProtocolIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProtocolRun> { run });

        var checkInRepository = new Mock<ICheckInRepository>();
        checkInRepository.Setup(repository => repository.GetByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkIns);

        var service = CreateService(protocolRepository.Object, runRepository.Object, checkInRepository.Object);

        var review = await service.GetProtocolReviewAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(review.Sections, section => section.Type == "gap" && section.Evidence.Any(evidence => evidence.Contains("insufficient attached check-ins", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(review.SafetyNotes, note => note.Contains("observational", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetMissionControlAsync_WithNoRuns_ReturnsEmptyLoopSignals()
    {
        var personId = Guid.NewGuid();
        var protocolRepository = new Mock<IProtocolRepository>();
        protocolRepository.Setup(repository => repository.GetByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Protocol>());

        var runRepository = new Mock<IProtocolRunRepository>();
        runRepository.Setup(repository => repository.GetActiveByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProtocolRun?)null);

        var checkInRepository = new Mock<ICheckInRepository>();
        checkInRepository.Setup(repository => repository.GetByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CheckIn>());

        var service = CreateService(protocolRepository.Object, runRepository.Object, checkInRepository.Object);

        var mission = await service.GetMissionControlAsync(personId, CancellationToken.None);

        Assert.Null(mission.ActiveRun);
        Assert.Null(mission.LatestClosedRun);
        Assert.Null(mission.LatestReviewSummary);
        Assert.Contains(mission.ObservationSignals, signal => signal.Type == "gap" && signal.Severity == "low");
    }

    [Fact]
    public async Task GetMissionControlAsync_WithActiveRunOnly_ReportsAttachedObservationGap()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Active", DateTime.UtcNow.AddDays(-2));
        var run = new ProtocolRun
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ProtocolId = protocol.Id,
            Protocol = protocol,
            StartedAtUtc = DateTime.UtcNow.AddDays(-2),
            Status = ProtocolRunStatus.Active
        };

        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run }, new List<CheckIn>(), activeRun: run);

        var mission = await service.GetMissionControlAsync(personId, CancellationToken.None);

        Assert.Equal(run.Id, mission.ActiveRun?.Id);
        Assert.Null(mission.LatestReviewSummary);
        Assert.True(mission.LatestCheckInSignal.HasObservationGap);
        Assert.Contains(mission.ObservationSignals, signal => signal.Type == "gap" && signal.Severity == "medium");
    }

    [Fact]
    public async Task GetMissionControlAsync_WithClosedRunAndNoReviewCompletion_ReturnsPendingReview()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Closed", DateTime.UtcNow.AddDays(-10));
        var run = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-5));
        var checkIns = new List<CheckIn>
        {
            CreateCheckIn(personId, run.Id, run.StartedAtUtc.AddDays(1), energy: 5, recovery: 5, sleep: 5, appetite: 5),
            CreateCheckIn(personId, run.Id, run.StartedAtUtc.AddDays(2), energy: 6, recovery: 6, sleep: 5, appetite: 5)
        };

        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run }, checkIns);

        var mission = await service.GetMissionControlAsync(personId, CancellationToken.None);

        Assert.Equal(run.Id, mission.LatestClosedRun?.Id);
        Assert.NotNull(mission.LatestReviewSummary);
        Assert.Equal(protocol.Id, mission.LatestReviewSummary?.ProtocolId);
    }

    [Fact]
    public async Task GetMissionControlAsync_WithCompletedReview_ClearsPendingReviewCue()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Closed", DateTime.UtcNow.AddDays(-10));
        var run = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-5));
        var completedReview = new ProtocolReviewCompletedEvent
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            ProtocolRunId = run.Id,
            CompletedAtUtc = run.EndedAtUtc!.Value.AddMinutes(1)
        };

        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { protocol },
            new List<ProtocolRun> { run },
            new List<CheckIn>(),
            reviewCompletedEvents: new List<ProtocolReviewCompletedEvent> { completedReview });

        var mission = await service.GetMissionControlAsync(personId, CancellationToken.None);

        Assert.Null(mission.LatestReviewSummary);
        Assert.Contains(mission.CohesionTimeline, @event => @event.EventType == "review_completed" && @event.ReviewCompletedEventId == completedReview.Id);
    }

    [Fact]
    public async Task GetMissionControlAsync_WithEvolvedProtocolAndParent_ReturnsRecentEvolution()
    {
        var personId = Guid.NewGuid();
        var parent = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Parent", DateTime.UtcNow.AddDays(-12));
        var run = CreateRun(personId, parent, DateTime.UtcNow.AddDays(-8));
        var evolved = CreateProtocolVersion(personId, Guid.NewGuid(), parent.Items.Single().CompoundRecordId, 2, parent.Id, "Child", DateTime.UtcNow.AddDays(-1));
        evolved.OriginProtocolId = parent.Id;
        evolved.EvolvedFromRunId = run.Id;

        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { evolved, parent },
            new List<ProtocolRun> { run },
            new List<CheckIn>(),
            latestEvolved: evolved);

        var mission = await service.GetMissionControlAsync(personId, CancellationToken.None);

        Assert.Equal(evolved.Id, mission.RecentEvolution?.ProtocolId);
        Assert.Equal(parent.Id, mission.RecentEvolution?.ParentProtocolId);
        Assert.Contains(mission.CohesionTimeline, @event => @event.EventType == "evolution" && @event.RunId == run.Id);
    }

    [Fact]
    public async Task GetMissionControlAsync_WithRecentAttachedObservation_DoesNotReportGap()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Observed", DateTime.UtcNow.AddDays(-2));
        var run = new ProtocolRun
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ProtocolId = protocol.Id,
            Protocol = protocol,
            StartedAtUtc = DateTime.UtcNow.AddDays(-2),
            Status = ProtocolRunStatus.Active
        };
        var checkIns = new List<CheckIn>
        {
            CreateCheckIn(personId, run.Id, DateTime.UtcNow, energy: 5, recovery: 5, sleep: 5, appetite: 5)
        };

        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run }, checkIns, activeRun: run);

        var mission = await service.GetMissionControlAsync(personId, CancellationToken.None);

        Assert.False(mission.LatestCheckInSignal.HasObservationGap);
        Assert.DoesNotContain(mission.ObservationSignals, signal => signal.Type == "gap");
    }

    [Fact]
    public async Task GetMissionControlAsync_WithComputationRecord_OverlaysMathInTimeline()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Math", DateTime.UtcNow.AddDays(-5));
        var computation = new ProtocolComputationRecord
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            Type = "reconstitution",
            InputSnapshot = "{}",
            OutputResult = "1000 mcg/mL",
            TimestampUtc = DateTime.UtcNow.AddDays(-1)
        };

        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { protocol },
            new List<ProtocolRun>(),
            new List<CheckIn>(),
            computations: new List<ProtocolComputationRecord> { computation });

        var mission = await service.GetMissionControlAsync(personId, CancellationToken.None);

        Assert.Contains(mission.CohesionTimeline, @event => @event.EventType == "computation" && @event.ComputationId == computation.Id);
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_WithNoRuns_ReturnsNoPattern()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "No runs", DateTime.UtcNow.AddDays(-10));
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun>(), new List<CheckIn>());

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal(0, snapshot.HistoricalRunCount);
        Assert.Equal("none", snapshot.PatternConfidence);
        Assert.Empty(snapshot.MetricPatterns);
        Assert.Null(snapshot.CurrentRunComparison);
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_WithOneRun_ReturnsInsufficientPattern()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "One run", DateTime.UtcNow.AddDays(-10));
        var run = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-8));
        var checkIns = PatternCheckIns(personId, run, 1, 2);
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run }, checkIns);

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal(1, snapshot.HistoricalRunCount);
        Assert.Equal("none", snapshot.PatternConfidence);
        Assert.Empty(snapshot.MetricPatterns);
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_WithTwoRuns_ReturnsLowConfidence()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Two runs", DateTime.UtcNow.AddDays(-20));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-18));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10));
        var checkIns = PatternCheckIns(personId, run1, 1, 2).Concat(PatternCheckIns(personId, run2, 1, 2)).ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2 }, checkIns);

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal(2, snapshot.HistoricalRunCount);
        Assert.Equal("low", snapshot.PatternConfidence);
        Assert.Contains(snapshot.MetricPatterns, pattern => pattern.Metric == "First check-in");
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_WithThreeConsistentRuns_ReturnsModerateConfidence()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Three runs", DateTime.UtcNow.AddDays(-35));
        var runs = new List<ProtocolRun>
        {
            CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-30)),
            CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-20)),
            CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10))
        };
        var checkIns = runs.SelectMany(run => PatternCheckIns(personId, run, 1, 2)).ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, runs, checkIns);

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("moderate", snapshot.PatternConfidence);
        Assert.Contains(snapshot.MetricPatterns, pattern => pattern.Metric == "Check-in cadence");
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_WithCurrentRunMatchingPattern_ReturnsMatchingComparison()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Matching", DateTime.UtcNow.AddDays(-25));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-22));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-14));
        var activeRun = CreateActiveRun(personId, protocol, DateTime.UtcNow.AddDays(-2));
        var checkIns = PatternCheckIns(personId, run1, 1, 2)
            .Concat(PatternCheckIns(personId, run2, 1, 2))
            .Append(CreateCheckIn(personId, activeRun.Id, activeRun.StartedAtUtc.AddDays(1), energy: 5, recovery: 5, sleep: 5, appetite: 5))
            .ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2, activeRun }, checkIns, activeRun);

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("moderate", snapshot.CurrentRunComparison?.Similarity);
        Assert.Contains(snapshot.CurrentRunComparison!.MatchingSignals, signal => signal == "Check-in timing aligns with prior runs");
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_WithCurrentRunDivergingFromPattern_ReturnsDivergentComparison()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Diverging", DateTime.UtcNow.AddDays(-25));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-22));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-14));
        var activeRun = CreateActiveRun(personId, protocol, DateTime.UtcNow.AddDays(-4));
        var checkIns = PatternCheckIns(personId, run1, 1, 2)
            .Concat(PatternCheckIns(personId, run2, 1, 2))
            .ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2, activeRun }, checkIns, activeRun);

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("low", snapshot.CurrentRunComparison?.Similarity);
        Assert.Contains(snapshot.CurrentRunComparison!.DivergentSignals, signal => signal == "Current run is later than prior runs to first observation");
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_WithSparseData_DowngradesConfidence()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Sparse", DateTime.UtcNow.AddDays(-35));
        var runs = new List<ProtocolRun>
        {
            CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-30)),
            CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-20)),
            CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10))
        };
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, runs, new List<CheckIn>());

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("low", snapshot.PatternConfidence);
        Assert.Contains(snapshot.MetricPatterns, pattern => pattern.Metric == "Run duration");
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_IncludesComputationsInPatterns()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Computations", DateTime.UtcNow.AddDays(-20));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-18));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10));
        var computations = new List<ProtocolComputationRecord>
        {
            CreateComputation(protocol, run1, run1.StartedAtUtc.AddDays(1)),
            CreateComputation(protocol, run2, run2.StartedAtUtc.AddDays(1))
        };
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2 }, new List<CheckIn>(), computations: computations);

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.EventPatterns, pattern => pattern.EventType == "Computation");
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_IncludesReviewEventsInPatterns()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Reviews", DateTime.UtcNow.AddDays(-20));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-18));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10));
        var reviewEvents = new List<ProtocolReviewCompletedEvent>
        {
            CreateReviewCompleted(protocol, run1, run1.EndedAtUtc!.Value.AddDays(1)),
            CreateReviewCompleted(protocol, run2, run2.EndedAtUtc!.Value.AddDays(1))
        };
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2 }, new List<CheckIn>(), reviewCompletedEvents: reviewEvents);

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.EventPatterns, pattern => pattern.EventType == "Review completed");
    }

    [Fact]
    public async Task GetPatternSnapshotAsync_DetectsRecurringSequences()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Sequences", DateTime.UtcNow.AddDays(-20));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-18));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10));
        var checkIns = PatternCheckIns(personId, run1, 1, 2).Concat(PatternCheckIns(personId, run2, 1, 2)).ToList();
        var computations = new List<ProtocolComputationRecord>
        {
            CreateComputation(protocol, run1, run1.StartedAtUtc.AddDays(1)),
            CreateComputation(protocol, run2, run2.StartedAtUtc.AddDays(1))
        };
        var reviewEvents = new List<ProtocolReviewCompletedEvent>
        {
            CreateReviewCompleted(protocol, run1, run1.EndedAtUtc!.Value.AddDays(1)),
            CreateReviewCompleted(protocol, run2, run2.EndedAtUtc!.Value.AddDays(1))
        };
        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { protocol },
            new List<ProtocolRun> { run1, run2 },
            checkIns,
            computations: computations,
            reviewCompletedEvents: reviewEvents);

        var snapshot = await service.GetPatternSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.SequencePatterns, pattern => pattern.Sequence.SequenceEqual(new[] { "RunStart", "Computation", "TrendShift", "ReviewCompleted" }));
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithInsufficientHistory_ReturnsNoBaseline()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Drift", DateTime.UtcNow.AddDays(-10));
        var historical = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-8));
        var active = CreateActiveRun(personId, protocol, DateTime.UtcNow.AddDays(-1));
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { historical, active }, new List<CheckIn>(), active);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("none", snapshot.DriftState);
        Assert.Equal("insufficient_history", snapshot.BaselineSource);
        Assert.Equal("stable", snapshot.RegimeClassification?.State);
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithStableRun_ReturnsNoDrift()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture();
        var checkIns = historicalRuns.SelectMany(run => StableCheckIns(personId, run, 1, 2))
            .Concat(StableCheckIns(personId, activeRun, 1, 2))
            .ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, historicalRuns.Append(activeRun).ToList(), checkIns, activeRun);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("none", snapshot.DriftState);
        Assert.Equal("historical_runs", snapshot.BaselineSource);
        Assert.Empty(snapshot.Signals);
        Assert.Equal("stable", snapshot.RegimeClassification?.State);
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithLateFirstCheckIn_ReturnsMildDrift()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture();
        var checkIns = historicalRuns.SelectMany(run => StableCheckIns(personId, run, 1, 2))
            .Append(CreateCheckIn(personId, activeRun.Id, activeRun.StartedAtUtc.AddDays(3), energy: 5, recovery: 5, sleep: 5, appetite: 5))
            .ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, historicalRuns.Append(activeRun).ToList(), checkIns, activeRun);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("mild", snapshot.DriftState);
        Assert.Contains(snapshot.Signals, signal => signal.Type == "checkin_timing" && signal.Severity == "mild");
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithRepeatedTimingDrift_ReturnsModerateDrift()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(historicalDurationDays: 10, activeStartedDaysAgo: 4);
        var checkIns = historicalRuns.SelectMany(run => StableCheckIns(personId, run, 1, 2))
            .Concat(new[]
            {
                CreateCheckIn(personId, activeRun.Id, activeRun.StartedAtUtc.AddDays(2), energy: 5, recovery: 5, sleep: 5, appetite: 5),
                CreateCheckIn(personId, activeRun.Id, activeRun.StartedAtUtc.AddDays(4), energy: 5, recovery: 5, sleep: 5, appetite: 5)
            })
            .ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, historicalRuns.Append(activeRun).ToList(), checkIns, activeRun);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("moderate", snapshot.DriftState);
        Assert.Contains(snapshot.Signals, signal => signal.Type == "checkin_timing" && signal.Severity == "moderate");
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithMissingRecurringSequence_DetectsSequenceBreak()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture();
        var checkIns = historicalRuns.SelectMany(run => PatternCheckIns(personId, run, 1, 2))
            .Concat(PatternCheckIns(personId, activeRun, 1, 2))
            .ToList();
        var computations = historicalRuns.Select(run => CreateComputation(protocol, run, run.StartedAtUtc.AddDays(1))).ToList();
        var reviews = historicalRuns.Select(run => CreateReviewCompleted(protocol, run, run.EndedAtUtc!.Value.AddDays(1))).ToList();
        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { protocol },
            historicalRuns.Append(activeRun).ToList(),
            checkIns,
            activeRun,
            computations: computations,
            reviewCompletedEvents: reviews);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.Signals, signal => signal.Type == "sequence_break");
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithMultipleSignals_ClassifiesRegimeShift()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(historicalDurationDays: 10, activeStartedDaysAgo: 4);
        var checkIns = historicalRuns.SelectMany(run => PatternCheckIns(personId, run, 1, 2))
            .Concat(new[]
            {
                CreateCheckIn(personId, activeRun.Id, activeRun.StartedAtUtc.AddDays(3), energy: 5, recovery: 5, sleep: 5, appetite: 5),
                CreateCheckIn(personId, activeRun.Id, activeRun.StartedAtUtc.AddDays(4), energy: 8, recovery: 5, sleep: 5, appetite: 5)
            })
            .ToList();
        var computations = historicalRuns.Select(run => CreateComputation(protocol, run, run.StartedAtUtc.AddDays(1)))
            .Append(CreateComputation(protocol, activeRun, activeRun.StartedAtUtc.AddDays(3)))
            .ToList();
        var reviews = historicalRuns.Select(run => CreateReviewCompleted(protocol, run, run.EndedAtUtc!.Value.AddDays(1))).ToList();
        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { protocol },
            historicalRuns.Append(activeRun).ToList(),
            checkIns,
            activeRun,
            computations: computations,
            reviewCompletedEvents: reviews);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("regime_shift", snapshot.DriftState);
        Assert.Equal("shifted", snapshot.RegimeClassification?.State);
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithSparseCurrentData_ReducesSeverity()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(activeStartedDaysAgo: 0);
        var checkIns = historicalRuns.SelectMany(run => PatternCheckIns(personId, run, 1, 2)).ToList();
        var computations = historicalRuns.Select(run => CreateComputation(protocol, run, run.StartedAtUtc.AddDays(1))).ToList();
        var reviews = historicalRuns.Select(run => CreateReviewCompleted(protocol, run, run.EndedAtUtc!.Value.AddDays(1))).ToList();
        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { protocol },
            historicalRuns.Append(activeRun).ToList(),
            checkIns,
            activeRun,
            computations: computations,
            reviewCompletedEvents: reviews);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("mild", snapshot.DriftState);
        Assert.NotEqual("shifted", snapshot.RegimeClassification?.State);
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithComputationTimingDrift_DetectsSignal()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture();
        var computations = historicalRuns.Select(run => CreateComputation(protocol, run, run.StartedAtUtc.AddDays(1)))
            .Append(CreateComputation(protocol, activeRun, activeRun.StartedAtUtc.AddDays(3)))
            .ToList();
        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { protocol },
            historicalRuns.Append(activeRun).ToList(),
            new List<CheckIn>(),
            activeRun,
            computations: computations);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.Signals, signal => signal.Type == "computation_timing");
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithReviewTimingDrift_DetectsSignal()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Review drift", DateTime.UtcNow.AddDays(-30));
        var run1 = CreateRunWithDuration(personId, protocol, DateTime.UtcNow.AddDays(-24), 4);
        var run2 = CreateRunWithDuration(personId, protocol, DateTime.UtcNow.AddDays(-16), 4);
        var current = CreateRunWithDuration(personId, protocol, DateTime.UtcNow.AddDays(-8), 4);
        var reviews = new List<ProtocolReviewCompletedEvent>
        {
            CreateReviewCompleted(protocol, run1, run1.EndedAtUtc!.Value.AddDays(1)),
            CreateReviewCompleted(protocol, run2, run2.EndedAtUtc!.Value.AddDays(1)),
            CreateReviewCompleted(protocol, current, current.EndedAtUtc!.Value.AddDays(4))
        };
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2, current }, new List<CheckIn>(), reviewCompletedEvents: reviews);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.Signals, signal => signal.Type == "review_timing");
    }

    [Fact]
    public async Task GetDriftSnapshotAsync_WithSignalDensityDrift_DetectsSignal()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(historicalDurationDays: 10, activeStartedDaysAgo: 2);
        var checkIns = historicalRuns.SelectMany(run => PatternCheckIns(personId, run, 1, 2))
            .Concat(new[]
            {
                CreateCheckIn(personId, activeRun.Id, activeRun.StartedAtUtc.AddDays(1), energy: 4, recovery: 4, sleep: 4, appetite: 4),
                CreateCheckIn(personId, activeRun.Id, activeRun.StartedAtUtc.AddDays(2), energy: 8, recovery: 8, sleep: 8, appetite: 8)
            })
            .ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, historicalRuns.Append(activeRun).ToList(), checkIns, activeRun);

        var snapshot = await service.GetDriftSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.Signals, signal => signal.Type == "signal_density");
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_WithInsufficientHistory_ReturnsNoExpectedNextEvent()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Sequence", DateTime.UtcNow.AddDays(-10));
        var run = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-8));
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run }, new List<CheckIn>());

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("insufficient_history", snapshot.BaselineSource);
        Assert.Null(snapshot.ExpectedNextEvent);
        Assert.Empty(snapshot.CommonTransitions);
        Assert.Equal("unknown", snapshot.CurrentStatus?.State);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_DetectsRepeatedRunStartToFirstCheckIn()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Sequence", DateTime.UtcNow.AddDays(-20));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-18));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10));
        var checkIns = StableCheckIns(personId, run1, 1, 2).Concat(StableCheckIns(personId, run2, 1, 2)).ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2 }, checkIns);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.CommonTransitions, transition => transition.FromState == "RunStarted" && transition.ToEventType == "FirstCheckIn" && transition.ObservedCount == 2);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_DetectsRepeatedRunClosedToReviewCompleted()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Sequence", DateTime.UtcNow.AddDays(-20));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-18));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10));
        var reviews = new List<ProtocolReviewCompletedEvent>
        {
            CreateReviewCompleted(protocol, run1, run1.EndedAtUtc!.Value.AddDays(1)),
            CreateReviewCompleted(protocol, run2, run2.EndedAtUtc!.Value.AddDays(1))
        };
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2 }, new List<CheckIn>(), reviewCompletedEvents: reviews);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Contains(snapshot.CommonTransitions, transition => transition.FromState == "RunClosed" && transition.ToEventType == "ReviewCompleted" && transition.ObservedCount == 2);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_WithActiveRunBeforeFirstCheckIn_ReturnsFirstCheckIn()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(activeStartedDaysAgo: 0);
        var checkIns = historicalRuns.SelectMany(run => StableCheckIns(personId, run, 1, 2)).ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, historicalRuns.Append(activeRun).ToList(), checkIns, activeRun);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("FirstCheckIn", snapshot.ExpectedNextEvent?.EventType);
        Assert.Equal("pending", snapshot.CurrentStatus?.State);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_WithClosedRunPendingReview_ReturnsReviewCompletion()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Sequence", DateTime.UtcNow.AddDays(-30));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-24));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-16));
        var current = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-4));
        var reviews = new List<ProtocolReviewCompletedEvent>
        {
            CreateReviewCompleted(protocol, run1, run1.EndedAtUtc!.Value.AddDays(1)),
            CreateReviewCompleted(protocol, run2, run2.EndedAtUtc!.Value.AddDays(1))
        };
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2, current }, new List<CheckIn>(), reviewCompletedEvents: reviews);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("ReviewCompleted", snapshot.ExpectedNextEvent?.EventType);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_WithinTimingWindow_ReturnsPending()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(activeStartedDaysAgo: 1);
        var checkIns = historicalRuns.SelectMany(run => StableCheckIns(personId, run, 2, 3)).ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, historicalRuns.Append(activeRun).ToList(), checkIns, activeRun);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("pending", snapshot.CurrentStatus?.State);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_BeyondTimingWindow_ReturnsLate()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(activeStartedDaysAgo: 3);
        var checkIns = historicalRuns.SelectMany(run => StableCheckIns(personId, run, 1, 2)).ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, historicalRuns.Append(activeRun).ToList(), checkIns, activeRun);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("late", snapshot.CurrentStatus?.State);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_WithAlternateEventPath_ReturnsDiverging()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(activeStartedDaysAgo: 1);
        var checkIns = historicalRuns.SelectMany(run => StableCheckIns(personId, run, 1, 2)).ToList();
        var computations = new List<ProtocolComputationRecord>
        {
            CreateComputation(protocol, activeRun, activeRun.StartedAtUtc.AddHours(12))
        };
        var service = CreateMissionControlService(
            personId,
            new List<Protocol> { protocol },
            historicalRuns.Append(activeRun).ToList(),
            checkIns,
            activeRun,
            computations: computations);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("diverging", snapshot.CurrentStatus?.State);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_WithSparseHistory_LowersConfidence()
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Sequence", DateTime.UtcNow.AddDays(-20));
        var run1 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-18));
        var run2 = CreateRun(personId, protocol, DateTime.UtcNow.AddDays(-10));
        var active = CreateActiveRun(personId, protocol, DateTime.UtcNow.AddDays(-1));
        var checkIns = StableCheckIns(personId, run1, 1, 2).Concat(StableCheckIns(personId, run2, 1, 2)).ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, new List<ProtocolRun> { run1, run2, active }, checkIns, active);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(protocol.Id, CancellationToken.None);

        Assert.Equal("low", snapshot.ExpectedNextEvent?.Confidence);
    }

    [Fact]
    public async Task GetSequenceExpectationSnapshotAsync_WithCommonEvolutionEvent_ReturnsEvolutionNext()
    {
        var personId = Guid.NewGuid();
        var root = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Root", DateTime.UtcNow.AddDays(-40));
        var run1 = CreateRun(personId, root, DateTime.UtcNow.AddDays(-34));
        var run2 = CreateRun(personId, root, DateTime.UtcNow.AddDays(-24));
        var current = CreateRun(personId, root, DateTime.UtcNow.AddDays(-8));
        var child1 = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 2, root.Id, "Evolved", run1.EndedAtUtc!.Value.AddDays(2));
        child1.EvolvedFromRunId = run1.Id;
        var child2 = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 3, root.Id, "Evolved", run2.EndedAtUtc!.Value.AddDays(2));
        child2.EvolvedFromRunId = run2.Id;
        var reviews = new List<ProtocolReviewCompletedEvent>
        {
            CreateReviewCompleted(root, run1, run1.EndedAtUtc!.Value.AddDays(1)),
            CreateReviewCompleted(root, run2, run2.EndedAtUtc!.Value.AddDays(1)),
            CreateReviewCompleted(root, current, current.EndedAtUtc!.Value.AddDays(1))
        };
        var service = CreateMissionControlService(personId, new List<Protocol> { root, child1, child2 }, new List<ProtocolRun> { run1, run2, current }, new List<CheckIn>(), reviewCompletedEvents: reviews);

        var snapshot = await service.GetSequenceExpectationSnapshotAsync(root.Id, CancellationToken.None);

        Assert.Equal("EvolutionEvent", snapshot.ExpectedNextEvent?.EventType);
    }

    [Fact]
    public async Task GetMissionControlAsync_IncludesSequenceExpectationWhenEligible()
    {
        var (personId, protocol, historicalRuns, activeRun) = CreateDriftFixture(activeStartedDaysAgo: 0);
        var checkIns = historicalRuns.SelectMany(run => StableCheckIns(personId, run, 1, 2)).ToList();
        var service = CreateMissionControlService(personId, new List<Protocol> { protocol }, historicalRuns.Append(activeRun).ToList(), checkIns, activeRun);

        var mission = await service.GetMissionControlAsync(personId, CancellationToken.None);

        Assert.NotNull(mission.SequenceExpectationSnapshot);
        Assert.Equal("FirstCheckIn", mission.SequenceExpectationSnapshot?.ExpectedNextEvent?.EventType);
    }

    private static ProtocolService CreateService(
        IProtocolRepository protocolRepository,
        IProtocolRunRepository runRepository,
        ICheckInRepository checkInRepository,
        IProtocolComputationRecordRepository? computationRepository = null,
        IProtocolReviewCompletedEventRepository? reviewCompletedEventRepository = null)
    {
        return new ProtocolService(
            protocolRepository,
            new Mock<IPersonProfileRepository>().Object,
            new Mock<ICompoundRecordRepository>().Object,
            checkInRepository,
            runRepository,
            computationRepository ?? EmptyComputationRepository(),
            reviewCompletedEventRepository ?? EmptyReviewCompletedEventRepository(),
            new Mock<IKnowledgeSource>().Object);
    }

    private static IProtocolComputationRecordRepository EmptyComputationRepository()
    {
        var repository = new Mock<IProtocolComputationRecordRepository>();
        repository.Setup(item => item.GetByProtocolIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProtocolComputationRecord>());
        return repository.Object;
    }

    private static IProtocolReviewCompletedEventRepository EmptyReviewCompletedEventRepository()
    {
        var repository = new Mock<IProtocolReviewCompletedEventRepository>();
        repository.Setup(item => item.GetByProtocolIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProtocolReviewCompletedEvent>());
        return repository.Object;
    }

    private static ProtocolService CreateMissionControlService(
        Guid personId,
        List<Protocol> protocols,
        List<ProtocolRun> runs,
        List<CheckIn> checkIns,
        ProtocolRun? activeRun = null,
        Protocol? latestEvolved = null,
        List<ProtocolComputationRecord>? computations = null,
        List<ProtocolReviewCompletedEvent>? reviewCompletedEvents = null)
    {
        var protocolRepository = new Mock<IProtocolRepository>();
        protocolRepository.Setup(repository => repository.GetByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(protocols);
        protocolRepository.Setup(repository => repository.GetLatestEvolvedByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestEvolved);
        protocolRepository.Setup(repository => repository.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => protocols.FirstOrDefault(protocol => protocol.Id == id));
        protocolRepository.Setup(repository => repository.GetLineageAsync(It.IsAny<Protocol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Protocol protocol, CancellationToken _) =>
            {
                var rootId = protocol.OriginProtocolId ?? protocol.Id;
                return protocols
                    .Where(candidate => candidate.Id == rootId || candidate.OriginProtocolId == rootId)
                    .OrderBy(candidate => candidate.Version)
                    .ToList();
            });

        var runRepository = new Mock<IProtocolRunRepository>();
        runRepository.Setup(repository => repository.GetActiveByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeRun);
        runRepository.Setup(repository => repository.GetByProtocolIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => runs.Where(run => ids.Contains(run.ProtocolId)).ToList());
        runRepository.Setup(repository => repository.GetLatestByProtocolIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => runs.Where(run => run.ProtocolId == id).OrderByDescending(run => run.StartedAtUtc).FirstOrDefault());

        var checkInRepository = new Mock<ICheckInRepository>();
        checkInRepository.Setup(repository => repository.GetByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkIns);

        var computationRepository = new Mock<IProtocolComputationRecordRepository>();
        computationRepository.Setup(repository => repository.GetByProtocolIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => (computations ?? new List<ProtocolComputationRecord>()).Where(record => ids.Contains(record.ProtocolId)).ToList());

        var reviewCompletedEventRepository = new Mock<IProtocolReviewCompletedEventRepository>();
        reviewCompletedEventRepository.Setup(repository => repository.GetByProtocolIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => (reviewCompletedEvents ?? new List<ProtocolReviewCompletedEvent>()).Where(@event => ids.Contains(@event.ProtocolId)).ToList());

        return CreateService(
            protocolRepository.Object,
            runRepository.Object,
            checkInRepository.Object,
            computationRepository.Object,
            reviewCompletedEventRepository.Object);
    }

    private static Protocol CreateProtocolVersion(Guid personId, Guid protocolId, Guid compoundId, int version, Guid? parentProtocolId, string notes, DateTime createdAt)
    {
        var protocol = new Protocol
        {
            Id = protocolId,
            PersonId = personId,
            Name = $"Protocol v{version}",
            Version = version,
            ParentProtocolId = parentProtocolId,
            OriginProtocolId = parentProtocolId is null ? null : parentProtocolId,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
            Items = new List<ProtocolItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProtocolId = protocolId,
                    CompoundRecordId = compoundId,
                    CompoundNameSnapshot = "Test compound",
                    CompoundCategorySnapshot = CompoundCategory.Peptide.ToString(),
                    CompoundStatusSnapshot = CompoundStatus.Active.ToString(),
                    CompoundStartDateSnapshot = createdAt,
                    CompoundNotesSnapshot = notes
                }
            }
        };
        protocol.Items.First().Protocol = protocol;

        return protocol;
    }

    private static ProtocolRun CreateRun(Guid personId, Protocol protocol, DateTime startedAt)
    {
        return CreateRunWithDuration(personId, protocol, startedAt, 4);
    }

    private static ProtocolRun CreateRunWithDuration(Guid personId, Protocol protocol, DateTime startedAt, int durationDays)
    {
        return new ProtocolRun
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ProtocolId = protocol.Id,
            Protocol = protocol,
            StartedAtUtc = startedAt,
            EndedAtUtc = startedAt.AddDays(durationDays),
            Status = ProtocolRunStatus.Completed
        };
    }

    private static ProtocolRun CreateActiveRun(Guid personId, Protocol protocol, DateTime startedAt)
    {
        return new ProtocolRun
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ProtocolId = protocol.Id,
            Protocol = protocol,
            StartedAtUtc = startedAt,
            Status = ProtocolRunStatus.Active
        };
    }

    private static List<CheckIn> PatternCheckIns(Guid personId, ProtocolRun run, int firstDay, int secondDay)
    {
        return new List<CheckIn>
        {
            CreateCheckIn(personId, run.Id, run.StartedAtUtc.AddDays(firstDay), energy: 5, recovery: 5, sleep: 5, appetite: 5),
            CreateCheckIn(personId, run.Id, run.StartedAtUtc.AddDays(secondDay), energy: 8, recovery: 7, sleep: 5, appetite: 5)
        };
    }

    private static List<CheckIn> StableCheckIns(Guid personId, ProtocolRun run, int firstDay, int secondDay)
    {
        return new List<CheckIn>
        {
            CreateCheckIn(personId, run.Id, run.StartedAtUtc.AddDays(firstDay), energy: 5, recovery: 5, sleep: 5, appetite: 5),
            CreateCheckIn(personId, run.Id, run.StartedAtUtc.AddDays(secondDay), energy: 5, recovery: 5, sleep: 5, appetite: 5)
        };
    }

    private static (Guid PersonId, Protocol Protocol, List<ProtocolRun> HistoricalRuns, ProtocolRun ActiveRun) CreateDriftFixture(
        int historicalDurationDays = 4,
        int activeStartedDaysAgo = 2)
    {
        var personId = Guid.NewGuid();
        var protocol = CreateProtocolVersion(personId, Guid.NewGuid(), Guid.NewGuid(), 1, null, "Drift", DateTime.UtcNow.AddDays(-30));
        var historicalRuns = new List<ProtocolRun>
        {
            CreateRunWithDuration(personId, protocol, DateTime.UtcNow.AddDays(-24), historicalDurationDays),
            CreateRunWithDuration(personId, protocol, DateTime.UtcNow.AddDays(-14), historicalDurationDays)
        };
        var activeRun = CreateActiveRun(personId, protocol, DateTime.UtcNow.AddDays(-activeStartedDaysAgo));

        return (personId, protocol, historicalRuns, activeRun);
    }

    private static ProtocolComputationRecord CreateComputation(Protocol protocol, ProtocolRun run, DateTime timestamp)
    {
        return new ProtocolComputationRecord
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            ProtocolRunId = run.Id,
            Type = "reconstitution",
            InputSnapshot = "{}",
            OutputResult = "1000 mcg/mL",
            TimestampUtc = timestamp
        };
    }

    private static ProtocolReviewCompletedEvent CreateReviewCompleted(Protocol protocol, ProtocolRun run, DateTime completedAt)
    {
        return new ProtocolReviewCompletedEvent
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            ProtocolRunId = run.Id,
            CompletedAtUtc = completedAt,
            Notes = "Review recorded."
        };
    }

    private static CheckIn CreateCheckIn(Guid personId, Guid? runId, DateTime date, int energy, int recovery, int sleep, int appetite)
    {
        return new CheckIn
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ProtocolRunId = runId,
            Date = date,
            Weight = 180,
            Energy = energy,
            Recovery = recovery,
            SleepQuality = sleep,
            Appetite = appetite
        };
    }
}
