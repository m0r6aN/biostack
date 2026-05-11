namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using Moq;
using Xunit;

[Trait("Category", "Unit")]
public class TrustLedgerServiceTests
{
    private static KnowledgeEntry MakeEntry(string name, EvidenceTier tier = EvidenceTier.Moderate) =>
        new()
        {
            CanonicalName = name,
            EvidenceTier = tier,
            RegulatoryStatus = "Not FDA approved for therapeutic use",
            MechanismSummary = "Test mechanism",
            SourceReferences = ["PubMed:12345"],
            Notes = string.Empty,
            Benefits = ["Benefit A"],
            Pathways = ["mTOR"],
            DrugInteractions = [],
        };

    [Fact]
    public async Task GetTrustLedger_WhenCompoundExists_ReturnsNonNullResult()
    {
        var mockSource = new Mock<IKnowledgeSource>();
        mockSource.Setup(s => s.GetCompoundAsync(It.IsAny<string>(), default))
                  .ReturnsAsync(MakeEntry("BPC-157", EvidenceTier.Moderate));

        var sut = new TrustLedgerService(mockSource.Object);
        var result = await sut.GetTrustLedgerAsync("bpc-157");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTrustLedger_WhenCompoundNotFound_ReturnsNull()
    {
        var mockSource = new Mock<IKnowledgeSource>();
        mockSource.Setup(s => s.GetCompoundAsync(It.IsAny<string>(), default))
                  .ReturnsAsync((KnowledgeEntry?)null);

        var sut = new TrustLedgerService(mockSource.Object);
        var result = await sut.GetTrustLedgerAsync("nonexistent-compound");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTrustLedger_WhenEvidenceTierUnknown_SetsNeedsReviewTrue()
    {
        var mockSource = new Mock<IKnowledgeSource>();
        mockSource.Setup(s => s.GetCompoundAsync(It.IsAny<string>(), default))
                  .ReturnsAsync(MakeEntry("Mystery", EvidenceTier.Unknown));

        var sut = new TrustLedgerService(mockSource.Object);
        var result = await sut.GetTrustLedgerAsync("mystery");

        Assert.NotNull(result);
        Assert.True(result!.NeedsReview);
    }

    [Fact]
    public async Task GetTrustLedger_WhenSourceRefsEmpty_AddsQualityFlag()
    {
        var entry = MakeEntry("NoRefs", EvidenceTier.Limited);
        entry.SourceReferences = [];

        var mockSource = new Mock<IKnowledgeSource>();
        mockSource.Setup(s => s.GetCompoundAsync(It.IsAny<string>(), default)).ReturnsAsync(entry);

        var sut = new TrustLedgerService(mockSource.Object);
        var result = await sut.GetTrustLedgerAsync("norefs");

        Assert.Contains("no-source-references", result!.QualityFlags);
    }

    [Fact]
    public async Task GetTrustLedger_SlugConvertedToNameForLookup()
    {
        var mockSource = new Mock<IKnowledgeSource>();
        mockSource.Setup(s => s.GetCompoundAsync(It.IsAny<string>(), default))
                  .ReturnsAsync((KnowledgeEntry?)null);

        var sut = new TrustLedgerService(mockSource.Object);
        await sut.GetTrustLedgerAsync("bpc-157");

        // Slug "bpc-157" should become "bpc 157" for lookup
        mockSource.Verify(s => s.GetCompoundAsync("bpc 157", default), Times.Once);
    }
}
