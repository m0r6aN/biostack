namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class ProtocolAnalysisCacheTests
{
    [Fact]
    public async Task ParsedCache_RoundTrips()
    {
        var cache = CreateCache();
        var dto = new ParsedProtocolCacheDto(
            new List<ProtocolEntryResponse> { new("BPC-157", 500, "mcg", "daily", string.Empty) },
            new List<ProtocolBlendExpansionResponse>());

        await cache.SetParsedAsync("key-1", dto, TimeSpan.FromMinutes(5), CancellationToken.None);
        var roundTrip = await cache.GetParsedAsync("key-1", CancellationToken.None);

        Assert.NotNull(roundTrip);
        Assert.Single(roundTrip!.Protocol);
    }

    [Fact]
    public async Task AnalysisCache_RoundTrips()
    {
        var cache = CreateCache();
        var dto = new ProtocolAnalysisCacheDto(
            72,
            new ProtocolScoreExplanationResponse(50, 12, -4, -2),
            new List<ProtocolIssueResponse>(),
            new List<string>(),
            new List<KnownPatternResponse>
            {
                new("bpc-157-tb-500-complementary", "BPC-157 + TB-500 Complementary Pairing", new List<string> { "bpc-157", "tb-500" }, "Known repair-stack pairing.")
            },
            new List<EmergentPatternResponse>
            {
                new("emergent-shared-pathway-tissue-repair", "Shared pathway motif · tissue-repair", "motif", new List<string> { "bpc-157", "tb-500", "ghk-cu" }, new List<string> { "tissue-repair" }, new List<string> { "tissue-repair signaling" }, "moderate", "pathway-overlap", "Three or more compounds appear to converge on tissue-repair.", new List<string> { "Evidence mix in this pattern: 1 moderate/strong and 2 limited/mechanistic entries." }, "Inferred from this stack · not canonical")
            });

        await cache.SetAnalysisAsync("key-2", dto, TimeSpan.FromMinutes(5), CancellationToken.None);
        var roundTrip = await cache.GetAnalysisAsync("key-2", CancellationToken.None);

        Assert.Equal(72, roundTrip?.Score);
        Assert.Single(roundTrip?.KnownPatterns ?? new List<KnownPatternResponse>());
        Assert.Single(roundTrip?.EmergentPatterns ?? new List<EmergentPatternResponse>());
    }

    private static IProtocolAnalysisCache CreateCache()
    {
        var memory = new MemoryCache(new MemoryCacheOptions());
        var distributed = new MemoryDistributedCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));
        return new ProtocolAnalysisCache(memory, distributed, NullLogger<ProtocolAnalysisCache>.Instance);
    }
}
