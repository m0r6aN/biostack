namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public sealed class InteractionIntelligencePublicBoundaryTests
{
    [Fact]
    public async Task EvaluatePublicByNamesAsync_TreatsLegacyPairingMetadataAsUnknown_AndOmitsActionScenarios()
    {
        var compoundA = new KnowledgeEntry
        {
            CanonicalName = "Compound A",
            PairsWellWith = new List<string> { "Compound B" }
        };
        var compoundB = new KnowledgeEntry
        {
            CanonicalName = "Compound B",
            CompatibleBlends = new List<string> { "Compound A" }
        };
        var source = new Mock<IKnowledgeSource>();
        source
            .Setup(service => service.GetCompoundAsync("Compound A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(compoundA);
        source
            .Setup(service => service.GetCompoundAsync("Compound B", It.IsAny<CancellationToken>()))
            .ReturnsAsync(compoundB);

        var hints = new Mock<ICompoundInteractionHintRepository>();
        hints
            .Setup(repository => repository.FindPairAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompoundInteractionHint?)null);

        var result = await new InteractionIntelligenceService(source.Object, hints.Object)
            .EvaluatePublicByNamesAsync(new[] { "Compound A", "Compound B" });

        var interaction = Assert.Single(result.Interactions);
        Assert.Equal(InteractionType.Unknown, interaction.Type);
        Assert.Equal("Source data reports this pairing, but does not establish compatibility or safety.", interaction.Reason);
        Assert.Equal(0, result.Summary.Synergies);
        Assert.Empty(result.Counterfactuals);
        Assert.Empty(result.Swaps);
        source.Verify(
            service => service.GetAllCompoundsAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluatePublicByNamesAsync_NoFindingRemainsUnknown()
    {
        var source = new Mock<IKnowledgeSource>();
        source
            .Setup(service => service.GetCompoundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => new KnowledgeEntry { CanonicalName = name });

        var hints = new Mock<ICompoundInteractionHintRepository>();
        hints
            .Setup(repository => repository.FindPairAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompoundInteractionHint?)null);

        var result = await new InteractionIntelligenceService(source.Object, hints.Object)
            .EvaluatePublicByNamesAsync(new[] { "Compound A", "Compound B" });

        var interaction = Assert.Single(result.Interactions);
        Assert.Equal(InteractionType.Unknown, interaction.Type);
        Assert.Contains("compatibility and safety remain unknown", interaction.Reason);
    }
}
