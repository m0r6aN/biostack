namespace BioStack.Application.Services;

using System.Text.Json;
using BioStack.Contracts.Responses;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public sealed class ProtocolAnalysisCache : IProtocolAnalysisCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<ProtocolAnalysisCache> _logger;

    public ProtocolAnalysisCache(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<ProtocolAnalysisCache> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public Task<ParsedProtocolCacheDto?> GetParsedAsync(string key, CancellationToken ct) =>
        GetAsync<ParsedProtocolCacheDto>(CacheBucket.Parsed, key, ct);

    public Task SetParsedAsync(string key, ParsedProtocolCacheDto value, TimeSpan ttl, CancellationToken ct) =>
        SetAsync(CacheBucket.Parsed, key, value, ttl, ct);

    public Task<IngestionCacheDto?> GetIngestionAsync(string key, CancellationToken ct) =>
        GetAsync<IngestionCacheDto>(CacheBucket.Ingestion, key, ct);

    public Task SetIngestionAsync(string key, IngestionCacheDto value, TimeSpan ttl, CancellationToken ct) =>
        SetAsync(CacheBucket.Ingestion, key, value, ttl, ct);

    public Task<ProtocolAnalysisCacheDto?> GetAnalysisAsync(string key, CancellationToken ct) =>
        GetAsync<ProtocolAnalysisCacheDto>(CacheBucket.Analysis, key, ct);

    public Task SetAnalysisAsync(string key, ProtocolAnalysisCacheDto value, TimeSpan ttl, CancellationToken ct) =>
        SetAsync(CacheBucket.Analysis, key, value, ttl, ct);

    public Task<CounterfactualResultDto?> GetCounterfactualAsync(string key, CancellationToken ct) =>
        GetAsync<CounterfactualResultDto>(CacheBucket.Counterfactual, key, ct);

    public Task SetCounterfactualAsync(string key, CounterfactualResultDto value, TimeSpan ttl, CancellationToken ct) =>
        SetAsync(CacheBucket.Counterfactual, key, value, ttl, ct);

    private async Task<T?> GetAsync<T>(string bucket, string key, CancellationToken ct)
    {
        var memoryKey = $"{bucket}:{key}";
        if (_memoryCache.TryGetValue(memoryKey, out T? hotValue))
        {
            _logger.LogInformation("Analyzer cache hit. Layer=L1 Bucket={Bucket} Key={Key}", bucket, key);
            return hotValue;
        }

        var payload = await _distributedCache.GetStringAsync(key, ct);
        if (string.IsNullOrWhiteSpace(payload))
        {
            _logger.LogInformation("Analyzer cache miss. Layer=L2 Bucket={Bucket} Key={Key}", bucket, key);
            return default;
        }

        var value = JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        if (value is not null)
        {
            _memoryCache.Set(memoryKey, value, TimeSpan.FromHours(1));
            _logger.LogInformation("Analyzer cache hit. Layer=L2 Bucket={Bucket} Key={Key}", bucket, key);
        }

        return value;
    }

    private async Task SetAsync<T>(string bucket, string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var memoryKey = $"{bucket}:{key}";
        _memoryCache.Set(memoryKey, value, TimeSpan.FromHours(1));
        await _distributedCache.SetStringAsync(
            key,
            JsonSerializer.Serialize(value, SerializerOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct);
    }

    private static class CacheBucket
    {
        public const string Ingestion = "ingestion";
        public const string Parsed = "parsed";
        public const string Analysis = "analysis";
        public const string Counterfactual = "counterfactual";
    }
}

public interface IProtocolAnalysisCache
{
    Task<IngestionCacheDto?> GetIngestionAsync(string key, CancellationToken ct);
    Task SetIngestionAsync(string key, IngestionCacheDto value, TimeSpan ttl, CancellationToken ct);
    Task<ParsedProtocolCacheDto?> GetParsedAsync(string key, CancellationToken ct);
    Task SetParsedAsync(string key, ParsedProtocolCacheDto value, TimeSpan ttl, CancellationToken ct);
    Task<ProtocolAnalysisCacheDto?> GetAnalysisAsync(string key, CancellationToken ct);
    Task SetAnalysisAsync(string key, ProtocolAnalysisCacheDto value, TimeSpan ttl, CancellationToken ct);
    Task<CounterfactualResultDto?> GetCounterfactualAsync(string key, CancellationToken ct);
    Task SetCounterfactualAsync(string key, CounterfactualResultDto value, TimeSpan ttl, CancellationToken ct);
}
