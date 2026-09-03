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

    [Fact]
    public async Task EvaluateAsync_AvoidWithSafetySignal_OutranksPositiveStoredHint()
    {
        var compoundA = Entry("Synthetic Theta", avoidWith: ["Synthetic Iota"]);
        var compoundB = Entry("Synthetic Iota");
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
        var hints = HintReturning(new CompoundInteractionHint
        {
            CompoundA = compoundA.CanonicalName,
            CompoundB = compoundB.CanonicalName,
            InteractionType = InteractionType.Synergistic,
            Strength = 0.91m,
            Notes = "Synthetic positive stored hint."
        });

        var result = await CreateService(graphStore: graph.Object, hintRepository: hints.Object)
            .EvaluateAsync([compoundA, compoundB]);

        var interaction = Assert.Single(result.Interactions);
        Assert.Equal(InteractionType.Interfering, interaction.Type);
        graph.Verify(
            store => store.GetActiveArtifactAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        graph.Verify(
            store => store.FindRelationshipAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        hints.Verify(
            repository => repository.FindPairAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(false, false, "reviewed", true)]
    [InlineData(true, false, "reviewed", false)]
    [InlineData(true, true, "provisional", false)]
    public async Task EvaluateAsync_IneligibleGraphArtifact_DoesNotAuthorizeGraphIntelligence(
        bool hasArtifact,
        bool isActive,
        string reviewState,
        bool includeStoredHint)
    {
        var compoundA = Entry("Synthetic Kappa");
        var compoundB = Entry("Synthetic Lambda");
        var edge = new CompoundGraphRelationship
        {
            SubjectCompound = compoundA.CanonicalName,
            ObjectCompound = compoundB.CanonicalName,
            RelationshipType = GraphRelationshipType.SynergizesWith,
            Confidence = "high",
            ReviewState = "reviewed",
            NeedsReview = false,
            Reason = "Synthetic reviewed edge."
        };
        var artifact = hasArtifact
            ? new CompoundGraphArtifact
            {
                ArtifactHash = "sha256:synthetic-artifact",
                ReviewState = reviewState,
                IsActive = isActive
            }
            : null;
        var graph = GraphReturning(edge, artifact);
        var hints = HintReturning(includeStoredHint
            ? new CompoundInteractionHint
            {
                CompoundA = compoundA.CanonicalName,
                CompoundB = compoundB.CanonicalName,
                InteractionType = InteractionType.Synergistic,
                Strength = 0.83m,
                Notes = "Synthetic eligible fallback hint."
            }
            : null);

        var result = await CreateService(graphStore: graph.Object, hintRepository: hints.Object)
            .EvaluateAsync([compoundA, compoundB]);

        var interaction = Assert.Single(result.Interactions);
        Assert.NotEqual(IntelligenceSource.Graph, interaction.Source);
        Assert.Null(interaction.GraphArtifactHash);
        if (includeStoredHint)
        {
            Assert.True(interaction.HintBacked);
            Assert.Equal(InteractionType.Synergistic, interaction.Type);
        }
    }

    [Fact]
    public async Task EvaluateAsync_NonReviewedGraphEdge_DoesNotAuthorizeGraphIntelligence()
    {
        var compoundA = Entry("Synthetic Mu");
        var compoundB = Entry("Synthetic Nu");
        var graph = GraphReturning(new CompoundGraphRelationship
        {
            SubjectCompound = compoundA.CanonicalName,
            ObjectCompound = compoundB.CanonicalName,
            RelationshipType = GraphRelationshipType.SynergizesWith,
            Confidence = "high",
            ReviewState = "Reviewed",
            NeedsReview = false,
            Reason = "Synthetic case-variant edge."
        });

        var result = await CreateService(graphStore: graph.Object)
            .EvaluateAsync([compoundA, compoundB]);

        var interaction = Assert.Single(result.Interactions);
        Assert.NotEqual(IntelligenceSource.Graph, interaction.Source);
        Assert.Null(interaction.GraphArtifactHash);
    }

    [Fact]
    public async Task EvaluateAsync_NeedsReviewFlag_IsIneligible_WhenArtifactAndEdgeAreReviewed()
    {
        var compoundA = Entry("Synthetic Xi");
        var compoundB = Entry("Synthetic Omicron");
        var graph = GraphReturning(new CompoundGraphRelationship
        {
            SubjectCompound = compoundA.CanonicalName,
            ObjectCompound = compoundB.CanonicalName,
            RelationshipType = GraphRelationshipType.SynergizesWith,
            Confidence = "high",
            ReviewState = "reviewed",
            NeedsReview = true,
            Reason = "Synthetic flagged edge."
        });

        var result = await CreateService(graphStore: graph.Object)
            .EvaluateAsync([compoundA, compoundB]);

        var interaction = Assert.Single(result.Interactions);
        Assert.NotEqual(IntelligenceSource.Graph, interaction.Source);
        Assert.Null(interaction.GraphArtifactHash);
    }

    [Fact]
    public async Task EvaluateAsync_GraphEdgeFromDifferentArtifact_IsNotAttributedToReviewedArtifact()
    {
        var compoundA = Entry("Synthetic Pi");
        var compoundB = Entry("Synthetic Rho");
        var reviewedArtifact = new CompoundGraphArtifact
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ArtifactHash = "sha256:synthetic-reviewed-artifact-a",
            ReviewState = "reviewed",
            IsActive = true
        };
        var graph = GraphReturning(
            new CompoundGraphRelationship
            {
                GraphArtifactId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                SubjectCompound = compoundA.CanonicalName,
                ObjectCompound = compoundB.CanonicalName,
                RelationshipType = GraphRelationshipType.SynergizesWith,
                Confidence = "high",
                ReviewState = "reviewed",
                NeedsReview = false,
                Reason = "Synthetic reviewed edge from artifact B."
            },
            reviewedArtifact);
        var hints = HintReturning(new CompoundInteractionHint
        {
            CompoundA = compoundA.CanonicalName,
            CompoundB = compoundB.CanonicalName,
            InteractionType = InteractionType.Synergistic,
            Strength = 0.79m,
            Notes = "Synthetic eligible fallback hint."
        });

        var result = await CreateService(graphStore: graph.Object, hintRepository: hints.Object)
            .EvaluateAsync([compoundA, compoundB]);

        var interaction = Assert.Single(result.Interactions);
        Assert.NotEqual(IntelligenceSource.Graph, interaction.Source);
        Assert.Null(interaction.GraphArtifactHash);
        Assert.True(interaction.HintBacked);
        Assert.Equal(InteractionType.Synergistic, interaction.Type);
    }

    private static InteractionIntelligenceService CreateService(
        IKnowledgeSource? knowledgeSource = null,
        ICompoundGraphStore? graphStore = null,
        ICompoundInteractionHintRepository? hintRepository = null)
    {
        hintRepository ??= HintReturning(null).Object;

        knowledgeSource ??= EmptyKnowledgeSource().Object;
        return new InteractionIntelligenceService(knowledgeSource, hintRepository, graphStore);
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
        return GraphReturning(
            relationship,
            new CompoundGraphArtifact
            {
                ArtifactHash = "sha256:synthetic-reviewed-graph",
                ReviewState = "reviewed",
                IsActive = true
            });
    }

    private static Mock<ICompoundGraphStore> GraphReturning(
        CompoundGraphRelationship relationship,
        CompoundGraphArtifact? artifact)
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
            .ReturnsAsync(artifact);
        return graph;
    }

    private static Mock<ICompoundInteractionHintRepository> HintReturning(CompoundInteractionHint? hint)
    {
        var hints = new Mock<ICompoundInteractionHintRepository>();
        hints
            .Setup(repository => repository.FindPairAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hint);
        return hints;
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
