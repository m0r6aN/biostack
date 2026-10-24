namespace BioStack.KnowledgeWorker.Discovery;

using System.Globalization;
using System.Text.Json.Nodes;

/// <summary>
/// PubMed E-utilities (esearch + esummary) over each scope area's query, limited to clinical
/// or first-in-human papers entered in the window. Code names and INN-stem names in titles
/// become candidates. NCBI policy (https://www.ncbi.nlm.nih.gov/books/NBK25497/): send
/// <c>tool</c> and <c>email</c>, at most 3 requests/s without an API key (10/s with one).
/// Metadata only — abstracts and full text are not stored.
/// </summary>
public sealed class PubMedDiscoverySource : IDiscoverySource
{
    public const string Id = "pubmed";
    public const string Base = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/";
    private const string ClinicalFilter =
        " AND (\"clinical trial\"[pt] OR phase[tiab] OR \"first-in-human\"[tiab] OR randomized[tiab])";

    private readonly DiscoveryHttp _http;
    private readonly DiscoveryOptions _options;

    public PubMedDiscoverySource(DiscoveryHttp http, DiscoveryOptions options)
    {
        _http = http;
        _options = options;
    }

    public string SourceId => Id;

    private string Identity()
    {
        var s = $"&tool={Uri.EscapeDataString(_options.NcbiTool)}&email={Uri.EscapeDataString(_options.NcbiEmail ?? "")}";
        if (!string.IsNullOrWhiteSpace(_options.NcbiApiKey)) s += $"&api_key={Uri.EscapeDataString(_options.NcbiApiKey)}";
        return s;
    }

    public string BuildSearchUrl(DiscoveryArea area, DiscoveryWindow window)
        => $"{Base}esearch.fcgi?db=pubmed&retmode=json&retmax={Math.Clamp(_options.PubMedRetMax, 1, 500)}"
           + $"&datetype=edat&mindate={window.From:yyyy'/'MM'/'dd}&maxdate={window.To:yyyy'/'MM'/'dd}"
           + $"&term={Uri.EscapeDataString(area.PubMedQuery + ClinicalFilter)}" + Identity();

    public async Task<DiscoverySourceResult> DiscoverAsync(DiscoveryScope scope, DiscoveryWindow window, CancellationToken ct)
    {
        var hits = new List<DiscoveryHit>();
        var queries = new List<string>();
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.NcbiEmail))
        {
            errors.Add("skipped: Discovery:NcbiEmail is not set (NCBI usage policy requires tool and email).");
            return new DiscoverySourceResult(Id, hits, queries, errors);
        }

        foreach (var area in scope.Areas.Where(a => !string.IsNullOrWhiteSpace(a.PubMedQuery)))
        {
            try
            {
                var searchUrl = BuildSearchUrl(area, window);
                queries.Add(DiscoveryHttp.Redact(searchUrl));
                var search = await _http.GetJsonAsync(searchUrl, ct);
                var ids = (search?["esearchresult"]?["idlist"] as JsonArray ?? new JsonArray())
                    .Select(n => n?.GetValue<string>()).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList();
                if (ids.Count == 0) continue;

                var summaryUrl = $"{Base}esummary.fcgi?db=pubmed&retmode=json&id={string.Join(",", ids)}" + Identity();
                queries.Add(DiscoveryHttp.Redact(summaryUrl));
                var summary = await _http.GetJsonAsync(summaryUrl, ct);
                hits.AddRange(ParseSummary(summary, area, scope));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException)
            {
                errors.Add($"{area.AreaId}: {DiscoveryHttp.Redact(ex.Message)}");
            }
        }
        return new DiscoverySourceResult(Id, hits, queries, errors);
    }

    public static IEnumerable<DiscoveryHit> ParseSummary(JsonNode? summary, DiscoveryArea area, DiscoveryScope scope)
    {
        var result = summary?["result"] as JsonObject;
        if (result is null) yield break;
        foreach (var uid in (result["uids"] as JsonArray ?? new JsonArray()).Select(n => n?.GetValue<string>()).Where(s => !string.IsNullOrEmpty(s)))
        {
            if (result[uid!] is not JsonObject doc) continue;
            var title = doc["title"]?.GetValue<string>() ?? "";
            if (scope.IsExcluded(title)) continue;
            var journal = doc["fulljournalname"]?.GetValue<string>() ?? doc["source"]?.GetValue<string>();
            DateOnly? date = ParsePubDate(doc["sortpubdate"]?.GetValue<string>() ?? doc["pubdate"]?.GetValue<string>());
            var evidence = new DiscoveryEvidence(Id, $"PMID:{uid}", title, $"https://pubmed.ncbi.nlm.nih.gov/{uid}/", date, journal);
            foreach (var hit in NamesFromText(title, area, scope, evidence)) yield return hit;
        }
    }

    /// <summary>Code names and INN-stem names in free text (titles), each a candidate hit.</summary>
    public static IEnumerable<DiscoveryHit> NamesFromText(string text, DiscoveryArea area, DiscoveryScope scope, DiscoveryEvidence evidence)
    {
        foreach (var (name, cls) in CandidateNames.ExtractInnNames(text, scope.InnStems))
            yield return new DiscoveryHit(name, Array.Empty<string>(), cls, area.AreaId, evidence);
        foreach (var code in CandidateNames.ExtractCodeNames(text, scope.CodeNamePrefixDenylist))
            yield return new DiscoveryHit(code, Array.Empty<string>(), area.DefaultClassification, area.AreaId, evidence);
    }

    private static DateOnly? ParsePubDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var head = s.Length >= 10 ? s[..10].Replace('/', '-') : s;
        if (DateOnly.TryParseExact(head, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
        return int.TryParse(s.AsSpan(0, Math.Min(4, s.Length)), out var y) && y > 1900 ? new DateOnly(y, 1, 1) : null;
    }
}
