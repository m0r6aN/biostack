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

    private static ProtocolService CreateService(
        IProtocolRepository protocolRepository,
        IProtocolRunRepository runRepository,
        ICheckInRepository checkInRepository)
    {
        return new ProtocolService(
            protocolRepository,
            new Mock<IPersonProfileRepository>().Object,
            new Mock<ICompoundRecordRepository>().Object,
            checkInRepository,
            runRepository,
            new Mock<IKnowledgeSource>().Object);
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
        return new ProtocolRun
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ProtocolId = protocol.Id,
            Protocol = protocol,
            StartedAtUtc = startedAt,
            EndedAtUtc = startedAt.AddDays(4),
            Status = ProtocolRunStatus.Completed
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
