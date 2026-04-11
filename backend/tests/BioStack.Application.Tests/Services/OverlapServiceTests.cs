namespace BioStack.Application.Tests.Services;

using Xunit;
using Moq;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;

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

        var mockKnowledgeSource = new Mock<IKnowledgeSource>();
        mockKnowledgeSource
            .Setup(k => k.GetCompoundAsync("BPC-157", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bpc157);
        mockKnowledgeSource
            .Setup(k => k.GetCompoundAsync("TB-500", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tb500);

        var mockFlagRepository = new Mock<IInteractionFlagRepository>();

        var service = new OverlapService(mockKnowledgeSource.Object, mockFlagRepository.Object);
        var request = new OverlapCheckRequest(new List<string> { "BPC-157", "TB-500" });

        var result = await service.CheckOverlapAsync(request, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal("tissue-repair", result[0].PathwayTag);
        Assert.Contains("BPC-157", result[0].CompoundNames);
        Assert.Contains("TB-500", result[0].CompoundNames);
        Assert.Contains("Educational reference only", result[0].Description);
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

        var mockKnowledgeSource = new Mock<IKnowledgeSource>();
        mockKnowledgeSource
            .Setup(k => k.GetCompoundAsync("BPC-157", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bpc157);
        mockKnowledgeSource
            .Setup(k => k.GetCompoundAsync("NAD+", It.IsAny<CancellationToken>()))
            .ReturnsAsync(nad);

        var mockFlagRepository = new Mock<IInteractionFlagRepository>();

        var service = new OverlapService(mockKnowledgeSource.Object, mockFlagRepository.Object);
        var request = new OverlapCheckRequest(new List<string> { "BPC-157", "NAD+" });

        var result = await service.CheckOverlapAsync(request, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckOverlapAsync_WithLessThanTwoCompounds_ReturnsEmptyList()
    {
        var mockKnowledgeSource = new Mock<IKnowledgeSource>();
        var mockFlagRepository = new Mock<IInteractionFlagRepository>();

        var service = new OverlapService(mockKnowledgeSource.Object, mockFlagRepository.Object);
        var request = new OverlapCheckRequest(new List<string> { "BPC-157" });

        var result = await service.CheckOverlapAsync(request, CancellationToken.None);

        Assert.Empty(result);
    }
}
