namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public class InteractionIntelligenceServiceTests
{
    [Fact]
    public async Task EvaluateAsync_UsesHintWhenPresent()
    {
        var bpc157 = new KnowledgeEntry
        {
            CanonicalName = "BPC-157",
            Pathways = new List<string> { "tissue-repair", "angiogenesis" },
            EvidenceTier = EvidenceTier.Limited
        };
        var tb500 = new KnowledgeEntry
        {
            CanonicalName = "TB-500",
            Pathways = new List<string> { "tissue-repair", "angiogenesis" },
            EvidenceTier = EvidenceTier.Limited
        };

        var hintRepository = new Mock<ICompoundInteractionHintRepository>();
        hintRepository
            .Setup(repository => repository.FindPairAsync("BPC-157", "TB-500", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompoundInteractionHint
            {
                CompoundA = "BPC-157",
                CompoundB = "TB-500",
                InteractionType = InteractionType.Synergistic,
                Strength = 0.85m,
                Notes = "Known repair-stack pairing."
            });

        var service = new InteractionIntelligenceService(new Mock<IKnowledgeSource>().Object, hintRepository.Object);

        var result = await service.EvaluateAsync(new List<KnowledgeEntry> { bpc157, tb500 }, CancellationToken.None);

        var interaction = Assert.Single(result.Interactions);
        Assert.Equal(InteractionType.Synergistic, interaction.Type);
        Assert.True(interaction.HintBacked);
        Assert.Equal(0.85d, interaction.Confidence);
        Assert.Equal(1, result.Summary.Synergies);
        Assert.NotEmpty(result.Counterfactuals);
    }

    [Fact]
    public async Task EvaluateAsync_UsesSharedPathwaysWhenNoHintExists()
    {
        var semaglutide = new KnowledgeEntry
        {
            CanonicalName = "Semaglutide",
            Pathways = new List<string> { "incretin-signaling", "appetite-regulation" },
            EvidenceTier = EvidenceTier.Strong
        };
        var liraglutide = new KnowledgeEntry
        {
            CanonicalName = "Liraglutide",
            Pathways = new List<string> { "incretin-signaling", "glucose-regulation" },
            EvidenceTier = EvidenceTier.Strong
        };

        var hintRepository = new Mock<ICompoundInteractionHintRepository>();
        hintRepository
            .Setup(repository => repository.FindPairAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompoundInteractionHint?)null);

        var service = new InteractionIntelligenceService(new Mock<IKnowledgeSource>().Object, hintRepository.Object);

        var result = await service.EvaluateAsync(new List<KnowledgeEntry> { semaglutide, liraglutide }, CancellationToken.None);

        var interaction = Assert.Single(result.Interactions);
        Assert.Equal(InteractionType.Redundant, interaction.Type);
        Assert.Contains("incretin-signaling", interaction.SharedPathways);
        Assert.False(interaction.HintBacked);
        Assert.Equal(1, result.Summary.Redundancies);
        Assert.True(result.Score.RedundancyPenalty > 0);
        Assert.Equal("Liraglutide", result.Counterfactuals[0].RemovedCompound);
    }
}
