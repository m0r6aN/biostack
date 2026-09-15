namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Contracts.Requests;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

/// <summary>
/// Covers the owner-reported bug where an accidental Enter mid-form created a
/// compound with a blank name (see CompoundForm.tsx). The name must be
/// rejected server-side on both create and update, regardless of what the
/// client sends.
/// </summary>
public sealed class CompoundNameValidationTests
{
    private static CreateCompoundRequest CreateRequest(string name) =>
        new(name, CompoundCategory.Peptide, DateTime.UtcNow.Date, null, CompoundStatus.Active, "", SourceType.Manual);

    private static UpdateCompoundRequest UpdateRequest(string name) =>
        new(name, CompoundCategory.Peptide, DateTime.UtcNow.Date, null, CompoundStatus.Active, "", SourceType.Manual);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task CreateCompoundAsync_RejectsBlankName(string name)
    {
        var repository = new Mock<ICompoundRecordRepository>();
        var guard = new Mock<IOwnershipGuard>();
        var service = new CompoundService(
            repository.Object,
            Mock.Of<ITimelineEventRepository>(),
            guard.Object,
            Mock.Of<IFeatureGate>());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCompoundAsync(Guid.NewGuid(), CreateRequest(name)));

        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        guard.Verify(x => x.EnsureProfileOwnedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddAsync(It.IsAny<CompoundRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCompoundAsync_TrimsSurroundingWhitespaceFromName()
    {
        var repository = new Mock<ICompoundRecordRepository>();
        var feature = new Mock<IFeatureGate>();
        feature.Setup(x => x.GetLimitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
        CompoundRecord? saved = null;
        repository.Setup(x => x.AddAsync(It.IsAny<CompoundRecord>(), It.IsAny<CancellationToken>()))
            .Callback<CompoundRecord, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);

        var service = new CompoundService(
            repository.Object,
            Mock.Of<ITimelineEventRepository>(),
            Mock.Of<IOwnershipGuard>(),
            feature.Object);

        await service.CreateCompoundAsync(Guid.NewGuid(), CreateRequest("  BPC-157  "));

        Assert.NotNull(saved);
        Assert.Equal("BPC-157", saved!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateCompoundAsync_RejectsBlankName(string name)
    {
        var repository = new Mock<ICompoundRecordRepository>();
        var guard = new Mock<IOwnershipGuard>();
        var service = new CompoundService(
            repository.Object,
            Mock.Of<ITimelineEventRepository>(),
            guard.Object,
            Mock.Of<IFeatureGate>());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateCompoundAsync(Guid.NewGuid(), Guid.NewGuid(), UpdateRequest(name)));

        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        guard.Verify(x => x.EnsureProfileOwnedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCompoundAsync_TrimsSurroundingWhitespaceFromName()
    {
        var repository = new Mock<ICompoundRecordRepository>();
        var profileId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var existing = new CompoundRecord { Id = id, PersonId = profileId, Name = "Old Name" };
        repository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var service = new CompoundService(
            repository.Object,
            Mock.Of<ITimelineEventRepository>(),
            Mock.Of<IOwnershipGuard>(),
            Mock.Of<IFeatureGate>());

        await service.UpdateCompoundAsync(profileId, id, UpdateRequest("  Creatine  "));

        Assert.Equal("Creatine", existing.Name);
    }
}
