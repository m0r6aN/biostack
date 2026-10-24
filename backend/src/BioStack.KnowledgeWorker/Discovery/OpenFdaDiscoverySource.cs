namespace BioStack.KnowledgeWorker.Discovery;

using System.Globalization;
using System.Text.Json.Nodes;

/// <summary>
/// openFDA Drugs@FDA: original NDA/BLA approvals inside the window (new drugs and CDER
/// biologics). openFDA terms (https://open.fda.gov/terms/): public data, automated access
/// permitted; 240 requests/min and 1,000/day per IP without a key, 120,000/day with one.
/// Not covered: CBER biologics such as most gene and cell therapies, and IND clearances
/// (FDA does not publish INDs) — those surface through ClinicalTrials.gov instead.
/// </summary>
public sealed class OpenFdaDiscoverySource : IDiscoverySource
{
    public const string Id = "openfda-drugsfda";
    public const string Endpoint = "https://api.fda.gov/drug/drugsfda.json";
    private const int PageSize = 100;

    private readonly DiscoveryHttp _http;
    private readonly DiscoveryOptions _options;

    public OpenFdaDiscoverySource(DiscoveryHttp http, DiscoveryOptions options)
    {
        _http = http;
        _options = options;
    }

    public string SourceId => Id;

    public string BuildUrl(DiscoveryWindow window, int skip)
    {
        var search = "(application_number:NDA*+OR+application_number:BLA*)+AND+submissions.submission_type:ORIG"
                     + $"+AND+submissions.submission_status_date:%5B{window.From:yyyyMMdd}+TO+{window.To:yyyyMMdd}%5D";
        var url = $"{Endpoint}?search={search}&limit={PageSize}&skip={skip}";
        if (!string.IsNullOrWhiteSpace(_options.OpenFdaApiKey)) url += $"&api_key={Uri.EscapeDataString(_options.OpenFdaApiKey)}";
        return url;
    }

    public async Task<DiscoverySourceResult> DiscoverAsync(DiscoveryScope scope, DiscoveryWindow window, CancellationToken ct)
    {
        var hits = new List<DiscoveryHit>();
        var queries = new List<string>();
        var errors = new List<string>();
        for (var page = 0; page < Math.Max(1, _options.OpenFdaMaxPages); page++)
        {
            var url = BuildUrl(window, page * PageSize);
            queries.Add(DiscoveryHttp.Redact(url));
            JsonNode? root;
            try { root = await _http.GetJsonAsync(url, ct); }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                errors.Add(DiscoveryHttp.Redact(ex.Message));
                break;
            }
            var results = root?["results"] as JsonArray ?? new JsonArray();
            hits.AddRange(ParseResults(results, window, scope));
            var total = root?["meta"]?["results"]?["total"]?.GetValue<int>() ?? 0;
            if ((page + 1) * PageSize >= total || results.Count == 0) break;
        }
        return new DiscoverySourceResult(Id, hits, queries, errors);
    }

    public static IEnumerable<DiscoveryHit> ParseResults(JsonArray results, DiscoveryWindow window, DiscoveryScope scope)
    {
        foreach (var app in results.OfType<JsonObject>())
        {
            var appNo = app["application_number"]?.GetValue<string>() ?? "";
            // The search matches any submission in the window; keep only original approvals that are themselves in it.
            var orig = (app["submissions"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
                .FirstOrDefault(s => s["submission_type"]?.GetValue<string>() == "ORIG" && s["submission_status"]?.GetValue<string>() == "AP");
            if (orig is null) continue;
            if (!DateOnly.TryParseExact(orig["submission_status_date"]?.GetValue<string>(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var approved)) continue;
            if (approved < window.From || approved > window.To) continue;
            // New molecular entities only: NDA chemical type 1 (and 1/4 combos) or any original BLA.
            var classCode = orig["submission_class_code"]?.GetValue<string>() ?? "";
            if (appNo.StartsWith("NDA", StringComparison.OrdinalIgnoreCase) && !classCode.StartsWith("TYPE 1", StringComparison.OrdinalIgnoreCase)) continue;

            var products = (app["products"] as JsonArray ?? new JsonArray()).OfType<JsonObject>().ToList();
            var brands = products.Select(p => p["brand_name"]?.GetValue<string>()).Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => Title(s!)).Distinct().ToList();
            var ingredients = products.SelectMany(p => (p["active_ingredients"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
                .Select(i => i["name"]?.GetValue<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => Title(StripSalt(s!))).Distinct().ToList();
            var generic = (app["openfda"]?["generic_name"] as JsonArray ?? new JsonArray()).Select(n => n?.GetValue<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => Title(StripSalt(s!))).Distinct().ToList();
            var names = ingredients.Count > 0 ? ingredients : generic;
            if (names.Count == 0) continue;
            var sponsor = app["sponsor_name"]?.GetValue<string>();
            var digits = new string(appNo.Where(char.IsDigit).ToArray());
            var evidence = new DiscoveryEvidence(Id, appNo,
                $"Drugs@FDA {appNo}: {string.Join(", ", brands.DefaultIfEmpty(names[0]))} original approval",
                $"https://www.accessdata.fda.gov/scripts/cder/daf/index.cfm?event=overview.process&ApplNo={digits}",
                approved, $"approved {approved:yyyy-MM-dd}; sponsor {sponsor ?? "unknown"}");

            var isBla = appNo.StartsWith("BLA", StringComparison.OrdinalIgnoreCase);
            foreach (var name in names.Take(3))
            {
                // Biosimilars carry a four-letter FDA suffix ("Denosumab-Adet"); the molecule is not new.
                if (System.Text.RegularExpressions.Regex.IsMatch(name, @"-[A-Za-z]{4}$")) continue;
                var cls = CandidateNames.StemClassification(name, scope.InnStems) ?? (isBla ? "Biologic" : "Pharmaceutical");
                yield return new DiscoveryHit(name, brands, cls, "fda-approvals", evidence, Phase: "FDA-approved", Sponsor: sponsor);
            }
        }
    }

    private static readonly string[] SaltWords =
    [
        " sodium", " potassium", " calcium", " magnesium", " hydrochloride", " dihydrochloride", " hydrobromide",
        " acetate", " tartrate", " bitartrate", " citrate", " maleate", " mesylate", " besylate", " tosylate",
        " fumarate", " succinate", " sulfate", " phosphate", " meglumine", " tromethamine", " monohydrate", " dihydrate",
    ];

    /// <summary>"ZILGANERSEN SODIUM" → "Zilganersen".</summary>
    public static string StripSalt(string name)
    {
        var n = name.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (var salt in SaltWords)
                if (n.EndsWith(salt, StringComparison.OrdinalIgnoreCase)) { n = n[..^salt.Length].TrimEnd(); changed = true; }
        } while (changed);
        return n;
    }

    private static string Title(string s)
        => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.Trim().ToLowerInvariant());
}
