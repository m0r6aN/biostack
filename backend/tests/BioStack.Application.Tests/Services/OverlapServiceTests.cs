namespace BioStack.Application.Tests.Services;

using Xunit;
using Moq;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public class OverlapServiceTests
{
    [Fact]
    public async Task CheckOverlapAsync_WithCompoundsHavingPathwayOverlap_ReturnsFlagsWithOverlappingPathways()
    {
        var bpc157 = new KnowledgeEntry
        {
            CanonicalName = "BPC-157",
            Aliases = new List<string>(),
            Pathways = new List<string> { "tissue-repair", "gi-protective" }
        };

        var tb500 = new KnowledgeEntry
        {
            CanonicalName = "TB-500",
            Aliases = new List<string>(),
            Pathways = new List<string> { "tissue-repair", "anti-inflammatory" }
        };

        var mockInteractionService = new Mock<IInteractionIntelligenceService>();
        mockInteractionService
            .Setup(service => service.EvaluateByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InteractionIntelligenceResponse(
                new InteractionSummaryResponse(0, 1, 0),
                new ProtocolInteractionScoreResponse(0, 0.58, 0),
                41.88,
                new List<InteractionFindingResponse>(),
                new List<InteractionResultResponse>
                {
                    new(bpc157.CanonicalName, tb500.CanonicalName, Domain.Enums.InteractionType.Redundant, 0.58, new List<string> { "tissue-repair" }, "Shared pathway overlap detected: tissue-repair.", false)
                },
                new List<InteractionCounterfactualResponse>(),
                new List<InteractionSwapRecommendationResponse>()));
        var mockFlagRepository = new Mock<IInteractionFlagRepository>();

        var service = new OverlapService(mockInteractionService.Object, mockFlagRepository.Object);
        var request = new OverlapCheckRequest(new List<string> { "BPC-157", "TB-500" });

        var result = await service.CheckOverlapAsync(request, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal("tissue-repair", result[0].PathwayTag);
        Assert.Contains("BPC-157", result[0].CompoundNames);
        Assert.Contains("TB-500", result[0].CompoundNames);
        Assert.Contains("Shared pathway overlap", result[0].Description);
    }

    [Fact]
    public async Task CheckOverlapAsync_WithNoPathwayOverlap_ReturnsEmptyList()
    {
        var bpc157 = new KnowledgeEntry
        {
            CanonicalName = "BPC-157",
            Aliases = new List<string>(),
            Pathways = new List<string> { "tissue-repair" }
        };

        var nad = new KnowledgeEntry
        {
            CanonicalName = "NAD+",
            Aliases = new List<string>(),
            Pathways = new List<string> { "cellular-energy" }
        };

        var mockInteractionService = new Mock<IInteractionIntelligenceService>();
        mockInteractionService
            .Setup(service => service.EvaluateByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InteractionIntelligenceResponse(
                new InteractionSummaryResponse(0, 0, 0),
                new ProtocolInteractionScoreResponse(0, 0, 0),
                50,
                new List<InteractionFindingResponse>(),
                new List<InteractionResultResponse>
                {
                    new(bpc157.CanonicalName, nad.CanonicalName, Domain.Enums.InteractionType.Neutral, 0.30, new List<string>(), "No significant overlap detected from the current rule set.", false)
                },
                new List<InteractionCounterfactualResponse>(),
                new List<InteractionSwapRecommendationResponse>()));
        var mockFlagRepository = new Mock<IInteractionFlagRepository>();

        var service = new OverlapService(mockInteractionService.Object, mockFlagRepository.Object);
        var request = new OverlapCheckRequest(new List<string> { "BPC-157", "NAD+" });

        var result = await service.CheckOverlapAsync(request, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckOverlapAsync_WithLessThanTwoCompounds_ReturnsEmptyList()
    {
        var mockInteractionService = new Mock<IInteractionIntelligenceService>();
        var mockFlagRepository = new Mock<IInteractionFlagRepository>();

        var service = new OverlapService(mockInteractionService.Object, mockFlagRepository.Object);
        var request = new OverlapCheckRequest(new List<string> { "BPC-157" });

        var result = await service.CheckOverlapAsync(request, CancellationToken.None);

        Assert.Empty(result);
    }
}
