namespace BioStack.Application.Tests.Services;

using System.Globalization;
using BioStack.Application.Services;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

public sealed class ProtocolParserReproductionTests
{
    [Fact]
    public async Task ParseAsync_AliasInsideUnrelatedTokens_DoesNotCreateCompound()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var parser = CreateParser(cache);

        var result = await parser.ParseAsync("At 4000 steps nightly");

        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task ParseAsync_CommaSeparatedCompounds_PreservesEveryCompound()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var parser = CreateParser(cache);

        var result = await parser.ParseAsync("BPC-157 500mcg daily, TB-500 2mg twice weekly");

        Assert.Collection(
            result.Entries.OrderBy(entry => entry.CompoundName, StringComparer.Ordinal),
            entry =>
            {
                Assert.Equal("BPC-157", entry.CompoundName);
                Assert.Equal(500d, entry.Dose);
                Assert.Equal("mcg", entry.Unit);
            },
            entry =>
            {
                Assert.Equal("TB-500", entry.CompoundName);
                Assert.Equal(2d, entry.Dose);
                Assert.Equal("mg", entry.Unit);
            });
    }

    [Fact]
    public async Task ParseAsync_DotDecimalDose_IsCultureInvariant()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var parser = CreateParser(cache);
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            var result = await parser.ParseAsync("BPC-157 0.5mg daily");

            var entry = Assert.Single(result.Entries);
            Assert.Equal(0.5d, entry.Dose);
            Assert.Equal("mg", entry.Unit);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task ParseAsync_UnicodeMicroSign_PreservesDoseAndNormalizesUnit()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var parser = CreateParser(cache);

        var result = await parser.ParseAsync("BPC-157 500\u00b5g daily");

        var entry = Assert.Single(result.Entries);
        Assert.Equal(500d, entry.Dose);
        Assert.Equal("mcg", entry.Unit);
    }

    [Fact]
    public async Task ParseAsync_LeadingDigitCompoundName_PreservesFullName()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var parser = CreateParser(cache);

        var result = await parser.ParseAsync("5-Amino-1MQ 50mg daily");

        var entry = Assert.Single(result.Entries);
        Assert.Equal("5-Amino-1MQ", entry.CompoundName);
        Assert.Equal(50d, entry.Dose);
        Assert.Equal("mg", entry.Unit);
    }

    [Fact]
    public async Task ParseAsync_LeadingDecimalDose_PreservesPrecision()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var parser = CreateParser(cache);

        var result = await parser.ParseAsync("BPC-157 .25mg daily");

        var entry = Assert.Single(result.Entries);
        Assert.Equal(0.25d, entry.Dose);
        Assert.Equal("mg", entry.Unit);
    }

    private static ProtocolParser CreateParser(IMemoryCache cache) =>
        new(new LocalKnowledgeSource(), new BlendDecomposerService(), cache);
}
