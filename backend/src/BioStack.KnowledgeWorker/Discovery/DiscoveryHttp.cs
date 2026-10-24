namespace BioStack.KnowledgeWorker.Discovery;

using System.Net.Http.Headers;
using System.Text.Json.Nodes;

/// <summary>Bounded, polite JSON GETs shared by the discovery sources.</summary>
public sealed class DiscoveryHttp
{
    public const int MaxResponseBytes = 8 * 1024 * 1024;
    private readonly HttpClient _client;
    private readonly TimeSpan _delay;
    private DateTimeOffset _last = DateTimeOffset.MinValue;

    public DiscoveryHttp(HttpClient client, TimeSpan delay, string userAgent)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        if (_client.DefaultRequestHeaders.UserAgent.Count == 0 && ProductInfoHeaderValue.TryParse(userAgent.Split(' ')[0], out var ua))
            _client.DefaultRequestHeaders.UserAgent.Add(ua);
        if (_client.Timeout > TimeSpan.FromSeconds(60)) _client.Timeout = TimeSpan.FromSeconds(60);
    }

    public int RequestCount { get; private set; }

    public async Task<JsonNode?> GetJsonAsync(string url, CancellationToken ct)
    {
        if (_delay > TimeSpan.Zero && _last != DateTimeOffset.MinValue)
        {
            var wait = _last + _delay - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
        }
        _last = DateTimeOffset.UtcNow;
        RequestCount++;

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        // openFDA answers 404 for "no matches"; treat it as an empty result, not a failure.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound && url.Contains("api.fda.gov", StringComparison.Ordinal))
            return new JsonObject { ["results"] = new JsonArray() };
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"GET {Redact(url)} returned {(int)response.StatusCode}.");
        if (response.Content.Headers.ContentLength is > MaxResponseBytes)
            throw new HttpRequestException($"GET {Redact(url)} response too large.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var limited = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (limited.Length + read > MaxResponseBytes) throw new HttpRequestException($"GET {Redact(url)} response too large.");
            limited.Write(buffer, 0, read);
        }
        limited.Position = 0;
        return await JsonNode.ParseAsync(limited, cancellationToken: ct);
    }

    /// <summary>Strips API keys so URLs can be logged and written to reports.</summary>
    public static string Redact(string url)
    {
        foreach (var key in new[] { "api_key=" })
        {
            var at = url.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (at < 0) continue;
            var end = url.IndexOf('&', at);
            url = url[..(at + key.Length)] + "REDACTED" + (end < 0 ? string.Empty : url[end..]);
        }
        return url;
    }
}
