namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public sealed class InteractionNumericAliasTests
{
    [Theory]
    [InlineData("1", "Unrelated note from phase 1.")]
    [InlineData("3", "Unrelated note from phase 3.")]
    [InlineData("500", "Unrelated note about 500 records.")]
    [InlineData("2.5", "Unrelated note for version 2.5.")]
    [InlineData("+", "Unrelated note with a + marker.")]
    [InlineData("", "Unrelated note.")]
    [InlineData(" ", "Unrelated note.")]
    public async Task NumericOrEmptyAlias_DoesNotTurnUnrelatedNotesIntoAnInteraction(string alias, string note)
    {
        var alpha = new KnowledgeEntry { CanonicalName = "Synthetic Alpha", DrugInteractions = [note] };
        var beta = new KnowledgeEntry { CanonicalName = "Synthetic Beta", Aliases = [alias, "Beta-12"] };

        var result = await CreateService().EvaluateAsync([alpha, beta]);

        Assert.Equal(InteractionType.Unknown, Assert.Single(result.Interactions).Type);
        Assert.Equal(0, result.Summary.Interferences);
        Assert.Equal(0d, result.Score.InterferencePenalty);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("3")]
    public async Task NumericAlias_DoesNotIdentifyAnAvoidWithCompound(string alias)
    {
        var alpha = new KnowledgeEntry { CanonicalName = "Synthetic Alpha", AvoidWith = [alias] };
        var beta = new KnowledgeEntry { CanonicalName = "Synthetic Beta", Aliases = [alias] };

        var result = await CreateService().EvaluateAsync([alpha, beta]);

        Assert.Equal(InteractionType.Unknown, Assert.Single(result.Interactions).Type);
        Assert.Equal(0, result.Summary.Interferences);
    }

    [Theory]
    [InlineData("synthetic beta")]
    [InlineData("beta-12")]
    public async Task NamedInteraction_StillMatchesCanonicalAndAlphanumericNames(string name)
    {
        var alpha = new KnowledgeEntry { CanonicalName = "Synthetic Alpha", DrugInteractions = [$"Review pairing with {name}."] };
        var beta = new KnowledgeEntry { CanonicalName = "Synthetic Beta", Aliases = ["1", "3", "Beta-12"] };

        var result = await CreateService().EvaluateAsync([alpha, beta]);

        var interaction = Assert.Single(result.Interactions);
        Assert.Equal(InteractionType.Interfering, interaction.Type);
        Assert.Equal(0.64d, interaction.Confidence);
    }

    [Theory]
    [InlineData("synthetic beta")]
    [InlineData("beta-12")]
    public async Task NamedAvoidWith_StillMatchesCanonicalAndAlphanumericNames(string name)
    {
        var alpha = new KnowledgeEntry { CanonicalName = "Synthetic Alpha", AvoidWith = [name] };
        var beta = new KnowledgeEntry { CanonicalName = "Synthetic Beta", Aliases = ["1", "3", "Beta-12"] };

        var result = await CreateService().EvaluateAsync([alpha, beta]);

        var interaction = Assert.Single(result.Interactions);
        Assert.Equal(InteractionType.Interfering, interaction.Type);
        Assert.Equal(0.72d, interaction.Confidence);
    }

    private static InteractionIntelligenceService CreateService()
    {
        var knowledge = new Mock<IKnowledgeSource>();
        knowledge.Setup(source => source.GetAllCompoundsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var hints = new Mock<ICompoundInteractionHintRepository>();
        hints.Setup(repository => repository.FindPairAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompoundInteractionHint?)null);
        return new InteractionIntelligenceService(knowledge.Object, hints.Object);
    }
}
