namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

public sealed class ProtocolParserNumericAliasTests
{
    [Fact]
    public async Task ParseAsync_NumericKnowledgeAliases_PreservesBothUnknownFixtureNames()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var parser = CreateParser(cache);

        var result = await parser.ParseAsync("QA-KEO69-Alpha 1 mg daily\nQA-KEO69-Beta 2 mg daily");

        Assert.Collection(result.Entries,
            entry => { Assert.Equal("QA-KEO69-Alpha", entry.CompoundName); Assert.Equal(1d, entry.Dose); },
            entry => { Assert.Equal("QA-KEO69-Beta", entry.CompoundName); Assert.Equal(2d, entry.Dose); });
        Assert.Empty(result.KnowledgeByCompound);
    }

    [Theory]
    [InlineData("1", 1d)]
    [InlineData("3", 3d)]
    [InlineData("0.1", 0.1d)]
    public async Task ParseAsync_DoseIsNotACompoundAlias(string amount, double expectedDose)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var result = await CreateParser(cache).ParseAsync($"NovelCompound {amount} mg daily");

        var entry = Assert.Single(result.Entries);
        Assert.Equal("NovelCompound", entry.CompoundName);
        Assert.Equal(expectedDose, entry.Dose);
        Assert.Equal("mg", entry.Unit);
        Assert.Equal("daily", entry.Frequency);
        Assert.Empty(result.KnowledgeByCompound);
    }

    [Fact]
    public async Task ParseAsync_BareDoseWithNumericAlias_DoesNotInventACompound()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var result = await CreateParser(cache).ParseAsync("1 mg daily");

        Assert.Empty(result.Entries);
        Assert.Empty(result.KnowledgeByCompound);
    }

    [Theory]
    [InlineData("Caffeine")]
    [InlineData("1,3,7-trimethylxanthine")]
    public async Task ParseAsync_CanonicalAndAlphanumericAlias_RemainRecognized(string name)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var result = await CreateParser(cache).ParseAsync($"{name} 2 mg daily");

        var entry = Assert.Single(result.Entries);
        Assert.Equal("Caffeine", entry.CompoundName);
        Assert.Equal(2d, entry.Dose);
        Assert.True(entry.Recognized);
        Assert.Single(result.KnowledgeByCompound);
    }

    private static ProtocolParser CreateParser(IMemoryCache cache)
    {
        var knowledge = new Mock<IKnowledgeSource>();
        knowledge.Setup(source => source.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntry>
            {
                new()
                {
                    CanonicalName = "Caffeine",
                    // The numeric fragments reproduce the observed public record;
                    // the complete name ensures valid digit-bearing aliases survive.
                    Aliases = ["1", "3", "7-trimethylxanthine", "1,3,7-trimethylxanthine"],
                },
            });
        return new ProtocolParser(knowledge.Object, new BlendDecomposerService(), cache);
    }
}
