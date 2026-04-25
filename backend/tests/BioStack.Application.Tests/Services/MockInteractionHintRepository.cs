namespace BioStack.Application.Tests.Services;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;
using Moq;

internal static class MockInteractionHintRepository
{
    public static Mock<ICompoundInteractionHintRepository> Empty()
    {
        var repository = new Mock<ICompoundInteractionHintRepository>();
        repository
            .Setup(store => store.FindPairAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompoundInteractionHint?)null);

        repository
            .Setup(store => store.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompoundInteractionHint>());

        return repository;
    }
}
