namespace BioStack.KnowledgeWorker.Hype;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Hype Gap data sources (growth spec 02 §5, README open question 4). v0 hype is editorial;
/// these optional, config-gated sources collect public-volume signals as <b>editor input
/// only</b>. Nothing here sets a claim's hype score or publishes anything.
/// Only sources whose terms permit automated API access are implemented. Reddit's Data API
/// (commercial use needs a separate agreement) and Google Trends (no official API; scraping
/// is against its terms) are deliberately not implemented.
/// </summary>
public sealed class HypeOptions
{
    /// <summary>Where signal reports are written (DB-free, like the council outputs).</summary>
    public string OutputDirectory { get; set; } = "research/hype-signals";
    public int WindowDays { get; set; } = 7;
    public int MaxCompounds { get; set; } = 50;
    public XSourceOptions X { get; set; } = new();
    public YouTubeSourceOptions YouTube { get; set; } = new();
}

public sealed class XSourceOptions
{
    public bool Enabled { get; set; }
    /// <summary>X API v2 app bearer token (Hype__X__BearerToken). Never committed.</summary>
    public string? BearerToken { get; set; }
    public string BaseUrl { get; set; } = "https://api.x.com";
    /// <summary>7-day post-count thresholds for the editor rubric: Niche, Common, Everywhere, Viral.</summary>
    public int[] RubricThresholds { get; set; } = [50, 500, 5000, 50000];
}

public sealed class YouTubeSourceOptions
{
    public bool Enabled { get; set; }
    /// <summary>YouTube Data API v3 key (Hype__YouTube__ApiKey). Never committed.</summary>
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://www.googleapis.com";
}

public sealed record HypeSignal(string Source, string Query, long? Count, string Window, DateTimeOffset RetrievedAt, string? Error = null);

public interface IHypeSignalSource
{
    string Name { get; }
    Task<HypeSignal> CountAsync(string query, DateTimeOffset now, int windowDays, CancellationToken ct);
}

/// <summary>X API v2 recent post counts (GET /2/tweets/counts/recent). Requires a paid API tier.</summary>
public sealed class XRecentCountsSource(HttpClient http, XSourceOptions options) : IHypeSignalSource
{
    public string Name => "x";

    public async Task<HypeSignal> CountAsync(string query, DateTimeOffset now, int windowDays, CancellationToken ct)
    {
        var window = $"{Math.Min(windowDays, 7)}d";
        var start = now.AddDays(-Math.Min(windowDays, 7)).AddMinutes(1).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"{options.BaseUrl.TrimEnd('/')}/2/tweets/counts/recent?query={Uri.EscapeDataString($"{query} -is:retweet")}&granularity=day&start_time={start}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken);
        try
        {
            using var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return new(Name, query, null, window, now, $"HTTP {(int)res.StatusCode}");
            var json = JsonNode.Parse(await res.Content.ReadAsStringAsync(ct));
            return new(Name, query, json?["meta"]?["total_tweet_count"]?.GetValue<long>(), window, now);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(Name, query, null, window, now, ex.GetType().Name);
        }
    }
}

/// <summary>YouTube Data API v3 search total (approximate; 100 quota units per call).</summary>
public sealed class YouTubeSearchSource(HttpClient http, YouTubeSourceOptions options) : IHypeSignalSource
{
    public string Name => "youtube";

    public async Task<HypeSignal> CountAsync(string query, DateTimeOffset now, int windowDays, CancellationToken ct)
    {
        var window = $"{windowDays}d";
        var after = now.AddDays(-windowDays).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"{options.BaseUrl.TrimEnd('/')}/youtube/v3/search?part=id&type=video&maxResults=1&q={Uri.EscapeDataString(query)}&publishedAfter={after}&key={Uri.EscapeDataString(options.ApiKey ?? string.Empty)}";
        try
        {
            using var res = await http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return new(Name, query, null, window, now, $"HTTP {(int)res.StatusCode}");
            var json = JsonNode.Parse(await res.Content.ReadAsStringAsync(ct));
            return new(Name, query, json?["pageInfo"]?["totalResults"]?.GetValue<long>(), window, now);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(Name, query, null, window, now, ex.GetType().Name);
        }
    }
}

public static class HypeSources
{
    /// <summary>Enabled sources with credentials present; an enabled source without its secret is skipped.</summary>
    public static IReadOnlyList<IHypeSignalSource> Create(HypeOptions options, Func<HttpClient> http, Action<string>? warn = null)
    {
        var list = new List<IHypeSignalSource>();
        if (options.X.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.X.BearerToken)) warn?.Invoke("Hype:X is enabled but Hype__X__BearerToken is not set; skipped.");
            else list.Add(new XRecentCountsSource(http(), options.X));
        }
        if (options.YouTube.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.YouTube.ApiKey)) warn?.Invoke("Hype:YouTube is enabled but Hype__YouTube__ApiKey is not set; skipped.");
            else list.Add(new YouTubeSearchSource(http(), options.YouTube));
        }
        return list;
    }

    /// <summary>Editor rubric suggestion from a 7-day X count (02 §5: Rare, Niche, Common, Everywhere, Viral).</summary>
    public static string? SuggestedLevel(long? xCount, int[] thresholds)
    {
        if (xCount is not { } c || thresholds.Length != 4) return null;
        if (c < thresholds[0]) return "Rare";
        if (c < thresholds[1]) return "Niche";
        if (c < thresholds[2]) return "Common";
        if (c < thresholds[3]) return "Everywhere";
        return "Viral";
    }
}
