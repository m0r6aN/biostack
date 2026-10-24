namespace BioStack.KnowledgeWorker.Discovery;

using System.Globalization;
using System.Text.Json.Nodes;

/// <summary>
/// bioRxiv and medRxiv "details" API (https://api.biorxiv.org/): open metadata for preprints
/// posted in the window, 30 per page. Titles and abstracts are matched against scope keywords;
/// code names and INN-stem names in matching titles become candidates. Metadata only — each
/// preprint carries its own license, so no preprint text is stored, and preprints are weighted
/// lowest because they are not peer reviewed.
/// </summary>
public sealed class PreprintDiscoverySource : IDiscoverySource
{
    public const string Id = "biorxiv-medrxiv";
    public const string Base = "https://api.biorxiv.org/details/";
    public static readonly string[] Servers = ["biorxiv", "medrxiv"];

    private readonly DiscoveryHttp _http;
    private readonly DiscoveryOptions _options;

    public PreprintDiscoverySource(DiscoveryHttp http, DiscoveryOptions options)
    {
        _http = http;
        _options = options;
    }

    public string SourceId => Id;

    public static string BuildUrl(string server, DiscoveryWindow window, int cursor)
        => $"{Base}{server}/{window.From:yyyy-MM-dd}/{window.To:yyyy-MM-dd}/{cursor}/json";

    public async Task<DiscoverySourceResult> DiscoverAsync(DiscoveryScope scope, DiscoveryWindow window, CancellationToken ct)
    {
        var hits = new List<DiscoveryHit>();
        var queries = new List<string>();
        var errors = new List<string>();
        foreach (var server in Servers)
        {
            var cursor = 0;
            for (var page = 0; page < Math.Max(1, _options.PreprintMaxPagesPerServer); page++)
            {
                var url = BuildUrl(server, window, cursor);
                queries.Add(url);
                JsonNode? root;
                try { root = await _http.GetJsonAsync(url, ct); }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
                {
                    errors.Add($"{server}: {ex.Message}");
                    break;
                }
                var collection = root?["collection"] as JsonArray ?? new JsonArray();
                hits.AddRange(ParseCollection(collection, scope));
                if (collection.Count == 0) break;
                cursor += collection.Count;
                var total = int.TryParse(root?["messages"]?[0]?["total"]?.ToString(), out var t) ? t : 0;
                if (cursor >= total) break;
            }
        }
        return new DiscoverySourceResult(Id, hits, queries, errors);
    }

    public static IEnumerable<DiscoveryHit> ParseCollection(JsonArray collection, DiscoveryScope scope)
    {
        foreach (var paper in collection.OfType<JsonObject>())
        {
            var title = paper["title"]?.GetValue<string>() ?? "";
            var abstractText = paper["abstract"]?.GetValue<string>() ?? "";
            var haystack = (title + " " + abstractText).ToLowerInvariant();
            var area = scope.Areas.FirstOrDefault(a => a.PreprintKeywords.Any(k => haystack.Contains(k.ToLowerInvariant())));
            if (area is null || scope.IsExcluded(title)) continue;
            var doi = paper["doi"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(doi)) continue;
            DateOnly? date = DateOnly.TryParse(paper["date"]?.GetValue<string>(), CultureInfo.InvariantCulture, out var d) ? d : null;
            var server = paper["server"]?.GetValue<string>() ?? "preprint";
            var evidence = new DiscoveryEvidence(Id, $"doi:{doi}", $"[Preprint, not peer reviewed] {title}", $"https://doi.org/{doi}", date,
                $"{server}; license {paper["license"]?.GetValue<string>() ?? "unknown"}");
            foreach (var hit in PubMedDiscoverySource.NamesFromText(title, area, scope, evidence)) yield return hit;
        }
    }
}
