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
}
