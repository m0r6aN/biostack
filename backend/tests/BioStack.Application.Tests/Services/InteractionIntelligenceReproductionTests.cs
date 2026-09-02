namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Entities.Graph;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public sealed class InteractionIntelligenceReproductionTests
{
    [Fact]
    public async Task EvaluateAsync_AvoidWithSafetySignal_OutranksPositiveGraphEdge()
    {
        var compoundA = Entry("Synthetic Alpha", avoidWith: ["Synthetic Beta"]);
        var compoundB = Entry("Synthetic Beta");
        var graph = GraphReturning(new CompoundGraphRelationship
        {
            SubjectCompound = compoundA.CanonicalName,
            ObjectCompound = compoundB.CanonicalName,
            RelationshipType = GraphRelationshipType.SynergizesWith,
            Confidence = "high",
            ReviewState = "reviewed",
            NeedsReview = false,
            Reason = "Synthetic positive edge."
        });

        var result = await CreateService(graphStore: graph.Object)
            .EvaluateAsync([compoundA, compoundB]);

        var interaction = Assert.Single(result.Interactions);
        Assert.True(
            interaction.Type == InteractionType.Interfering,
            $"Avoid-with safety metadata must outrank a positive graph edge, but the service returned {interaction.Type} from {interaction.Source}.");
    }

    [Fact]
    public async Task EvaluateAsync_NeedsReviewGraphEdge_IsNotServedAsReviewedIntelligence()
    {
        var compoundA = Entry("Synthetic Gamma");
        var compoundB = Entry("Synthetic Delta");
        var graph = GraphReturning(new CompoundGraphRelationship
        {
            SubjectCompound = compoundA.CanonicalName,
            ObjectCompound = compoundB.CanonicalName,
            RelationshipType = GraphRelationshipType.SynergizesWith,
            Confidence = "high",
            ReviewState = "needs-review",
            NeedsReview = true,
            Reason = "Synthetic provisional edge."
        });

        var result = await CreateService(graphStore: graph.Object)
            .EvaluateAsync([compoundA, compoundB]);

        var interaction = Assert.Single(result.Interactions);
        Assert.True(
            interaction.Source != IntelligenceSource.Graph,
            "A NeedsReview graph edge must not be emitted as graph-backed reviewed intelligence.");
    }

    [Fact]
    public async Task EvaluateByNamesAsync_CanonicalNameAndAlias_DoNotCreateSelfPair()
    {
        var canonical = Entry("Synthetic Epsilon", aliases: ["Epsilon Alias"]);
        var knowledgeSource = new Mock<IKnowledgeSource>();
        knowledgeSource
            .Setup(source => source.GetCompoundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(canonical);
        knowledgeSource
            .Setup(source => source.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateService(knowledgeSource.Object)
            .EvaluateByNamesAsync([canonical.CanonicalName, canonical.Aliases[0]]);

        Assert.True(
            result.Interactions.Count == 0,
            $"Canonical and alias inputs resolved to the same knowledge entry but produced {result.Interactions.Count} self-interaction(s).");
    }

    [Fact]
    public async Task EvaluateAsync_NonFiniteGraphConfidence_IsRejectedOrNormalized()
    {
        var compoundA = Entry("Synthetic Zeta");
        var compoundB = Entry("Synthetic Eta");
        var graph = GraphReturning(new CompoundGraphRelationship
        {
            SubjectCompound = compoundA.CanonicalName,
            ObjectCompound = compoundB.CanonicalName,
            RelationshipType = GraphRelationshipType.SynergizesWith,
            Confidence = "NaN",
            ReviewState = "reviewed",
            NeedsReview = false,
            Reason = "Synthetic edge with invalid confidence."
        });

        var result = await CreateService(graphStore: graph.Object)
            .EvaluateAsync([compoundA, compoundB]);

        var interaction = Assert.Single(result.Interactions);
        Assert.True(
            double.IsFinite(interaction.Confidence)
            && interaction.Confidence >= 0d
            && interaction.Confidence <= 1d,
            $"Interaction confidence must be finite and within [0,1], but was {interaction.Confidence}.");
    }

    private static InteractionIntelligenceService CreateService(
        IKnowledgeSource? knowledgeSource = null,
        ICompoundGraphStore? graphStore = null)
    {
        var hints = new Mock<ICompoundInteractionHintRepository>();
        hints
            .Setup(repository => repository.FindPairAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompoundInteractionHint?)null);

        knowledgeSource ??= EmptyKnowledgeSource().Object;
        return new InteractionIntelligenceService(knowledgeSource, hints.Object, graphStore);
    }

    private static Mock<IKnowledgeSource> EmptyKnowledgeSource()
    {
        var source = new Mock<IKnowledgeSource>();
        source
            .Setup(service => service.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return source;
    }

    private static Mock<ICompoundGraphStore> GraphReturning(CompoundGraphRelationship relationship)
    {
        var graph = new Mock<ICompoundGraphStore>();
        graph
            .Setup(store => store.FindRelationshipAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationship);
        graph
            .Setup(store => store.GetActiveArtifactAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompoundGraphArtifact
            {
                ArtifactHash = "sha256:synthetic-reviewed-graph",
                ReviewState = "reviewed",
                IsActive = true
            });
        return graph;
    }

    private static KnowledgeEntry Entry(
        string canonicalName,
        List<string>? aliases = null,
        List<string>? avoidWith = null)
    {
        return new KnowledgeEntry
        {
            CanonicalName = canonicalName,
            Aliases = aliases ?? [],
            AvoidWith = avoidWith ?? []
        };
    }
}
