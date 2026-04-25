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
            new List<string>());

        await cache.SetAnalysisAsync("key-2", dto, TimeSpan.FromMinutes(5), CancellationToken.None);
        var roundTrip = await cache.GetAnalysisAsync("key-2", CancellationToken.None);

        Assert.Equal(72, roundTrip?.Score);
    }

    private static IProtocolAnalysisCache CreateCache()
    {
        var memory = new MemoryCache(new MemoryCacheOptions());
        var distributed = new MemoryDistributedCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));
        return new ProtocolAnalysisCache(memory, distributed, NullLogger<ProtocolAnalysisCache>.Instance);
    }
}
