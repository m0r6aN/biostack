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

    public static Mock<ICompoundInteractionHintRepository> WithHints(params CompoundInteractionHint[] hints)
    {
        var repository = Empty();
        var storedHints = hints.ToList();

        repository
            .Setup(store => store.FindPairAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string compoundA, string compoundB, CancellationToken _) =>
            {
                var normalized = CompoundInteractionHintRepository.NormalizePair(compoundA, compoundB);
                return storedHints.FirstOrDefault(hint =>
                    string.Equals(hint.CompoundA, normalized.CompoundA, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(hint.CompoundB, normalized.CompoundB, StringComparison.OrdinalIgnoreCase));
            });

        repository
            .Setup(store => store.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedHints);

        return repository;
    }
}
