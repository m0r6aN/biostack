namespace BioStack.KnowledgeWorker.Discovery;

using System.Globalization;
using System.Text.Json.Nodes;

/// <summary>
/// Open-ended ClinicalTrials.gov API v2 search: every scope area's terms, restricted to studies
/// first posted inside the window, then every drug/biologic/genetic/supplement intervention name
/// is turned into a candidate. Unlike <c>ClinicalTrialsGovV2AcquisitionAdapter</c> (which looks
/// up names already requested), this finds names nobody has typed in yet.
/// Terms: https://clinicaltrials.gov/about-site/terms-conditions (public-domain registry data;
/// API use permitted; no rate limit is published, so requests are spaced).
/// </summary>
public sealed partial class ClinicalTrialsGovDiscoverySource : IDiscoverySource
{
    public const string Id = "clinicaltrials-gov";
    public const string Endpoint = "https://clinicaltrials.gov/api/v2/studies";
    private const string Fields =
        "NCTId,BriefTitle,OverallStatus,StudyType,Phase,LeadSponsorName,Condition,"
        + "InterventionName,InterventionType,InterventionOtherName,StudyFirstPostDate";

    private readonly DiscoveryHttp _http;
    private readonly DiscoveryOptions _options;

    public ClinicalTrialsGovDiscoverySource(DiscoveryHttp http, DiscoveryOptions options)
    {
        _http = http;
        _options = options;
    }

    public string SourceId => Id;

    public static string BuildUrl(DiscoveryArea area, DiscoveryWindow window, int pageSize, string? pageToken)
    {
        var term = string.Join(" OR ", area.ClinicalTrialsTerms.Select(t => t.Contains(' ') ? $"\"{t}\"" : t));
        var range = $"AREA[StudyFirstPostDate]RANGE[{window.From:yyyy-MM-dd},{window.To:yyyy-MM-dd}]";
        var url = $"{Endpoint}?format=json&query.term={Uri.EscapeDataString(term)}"
                  + $"&filter.advanced={Uri.EscapeDataString(range)}"
                  + $"&fields={Uri.EscapeDataString(Fields)}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(pageToken)) url += $"&pageToken={Uri.EscapeDataString(pageToken)}";
        return url;
    }

    public async Task<DiscoverySourceResult> DiscoverAsync(DiscoveryScope scope, DiscoveryWindow window, CancellationToken ct)
    {
        var hits = new List<DiscoveryHit>();
        var queries = new List<string>();
        var errors = new List<string>();
        var types = new HashSet<string>(scope.ClinicalTrialsInterventionTypes, StringComparer.OrdinalIgnoreCase);

        foreach (var area in scope.Areas.Where(a => a.ClinicalTrialsTerms.Count > 0))
        {
            string? token = null;
            for (var page = 0; page < Math.Max(1, _options.MaxPagesPerQuery); page++)
            {
                var url = BuildUrl(area, window, Math.Clamp(_options.ClinicalTrialsPageSize, 1, 1000), token);
                queries.Add(url);
                JsonNode? root;
                try { root = await _http.GetJsonAsync(url, ct); }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
                {
                    errors.Add($"{area.AreaId}: {ex.Message}");
                    break;
                }
                foreach (var study in (root?["studies"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
                    hits.AddRange(ParseStudy(study, area, scope, types));
                token = root?["nextPageToken"]?.GetValue<string>();
                if (string.IsNullOrEmpty(token)) break;
            }
        }
        return new DiscoverySourceResult(Id, hits, queries, errors);
    }

    public static IEnumerable<DiscoveryHit> ParseStudy(JsonObject study, DiscoveryArea area, DiscoveryScope scope, ISet<string> types)
    {
        var p = study["protocolSection"] as JsonObject;
        if (p is null) yield break;
        var nct = p["identificationModule"]?["nctId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(nct)) yield break;
        var title = p["identificationModule"]?["briefTitle"]?.GetValue<string>() ?? nct;
        var studyType = p["designModule"]?["studyType"]?.GetValue<string>();
        if (studyType is not null && !studyType.Equals("INTERVENTIONAL", StringComparison.OrdinalIgnoreCase)) yield break;
        var phases = (p["designModule"]?["phases"] as JsonArray ?? new JsonArray())
            .Select(n => n?.GetValue<string>()).Where(s => !string.IsNullOrEmpty(s) && s != "NA").Select(s => s!).ToList();
        if (phases.Count == 0) yield break; // device/behavioral/"NA" studies are not compound or therapy trials
        if (phases.All(ph => ph == "PHASE4")) yield break; // post-marketing studies of drugs that are already approved
        var phase = string.Join("/", phases);
        var sponsor = p["sponsorCollaboratorsModule"]?["leadSponsor"]?["name"]?.GetValue<string>();
        var status = p["statusModule"]?["overallStatus"]?.GetValue<string>();
        var posted = p["statusModule"]?["studyFirstPostDateStruct"]?["date"]?.GetValue<string>();
        DateOnly? date = DateOnly.TryParse(posted, CultureInfo.InvariantCulture, out var d) ? d : null;
        var conditions = (p["conditionsModule"]?["conditions"] as JsonArray ?? new JsonArray())
            .Select(n => n?.GetValue<string>()).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList();
        if (scope.IsExcluded(new[] { title }.Concat(conditions).ToArray())) yield break;

        foreach (var intervention in (p["armsInterventionsModule"]?["interventions"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
        {
            var type = intervention["type"]?.GetValue<string>() ?? "";
            if (types.Count > 0 && !types.Contains(type)) continue;
            var name = intervention["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name)) continue;
            var otherNames = (intervention["otherNames"] as JsonArray ?? new JsonArray())
                .Select(n => n?.GetValue<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToList();

            var evidence = new DiscoveryEvidence(Id, nct, title, $"https://clinicaltrials.gov/study/{nct}", date,
                $"{phase}; {status}; sponsor {sponsor ?? "unknown"}; intervention \"{name}\" ({type})");
            foreach (var candidate in CandidatesFromIntervention(name, otherNames, type, area, scope))
                yield return candidate with { Evidence = evidence, Phase = phase, Sponsor = sponsor, Conditions = conditions };
        }
    }

    /// <summary>
    /// Turns one intervention entry into candidates. "ER-100 epigenetic therapy" (GENETIC, other
    /// names "OSK epigenetic therapy") → ER-100, Epigenetic Therapy, aliases the full strings.
    /// Combination arms ("X + Y") are split; placebo/standard-of-care entries are dropped.
    /// </summary>
    public static IEnumerable<DiscoveryHit> CandidatesFromIntervention(
        string name, IReadOnlyList<string> otherNames, string type, DiscoveryArea area, DiscoveryScope scope)
    {
        var parts = name.Split(new[] { " + ", "+", " plus ", " and ", ";", "," }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var cleanPart = Clean(part.Split('(')[0]);
            var lower = cleanPart.ToLowerInvariant();
            if (lower.Length < 2) continue;
            if (scope.StopNames.Any(s => lower == s || lower.StartsWith(s + " ", StringComparison.Ordinal))) continue;
            if (StopSubstrings.Any(s => lower.Contains(s, StringComparison.Ordinal))) continue;

            var texts = new[] { cleanPart }.Concat(parts.Length == 1 ? otherNames : Array.Empty<string>()).ToList();
            var codes = texts.SelectMany(t => CandidateNames.ExtractCodeNames(t, scope.CodeNamePrefixDenylist)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var inns = texts.SelectMany(t => CandidateNames.ExtractInnNames(t, scope.InnStems)).DistinctBy(i => i.Name).ToList();

            string primary;
            if (inns.Count > 0) primary = inns[0].Name;
            else if (codes.Count > 0) primary = codes[0];
            // Without an INN stem or a code name, only therapy and supplement interventions keep their
            // raw name (e.g. "Fisetin", "Calcium AKG"); plain DRUG entries are usually generics or doses.
            else if ((type is "GENETIC" or "BIOLOGICAL" or "DIETARY_SUPPLEMENT") && WordCount(cleanPart) <= 4 && !DoseFragment().IsMatch(cleanPart)) primary = cleanPart;
            else continue;

            var aliases = inns.Select(i => i.Name).Concat(codes).Concat(texts)
                .Select(Clean)
                .Where(a => a.Length >= 2 && WordCount(a) <= 5 && CandidateNames.Normalize(a) != CandidateNames.Normalize(primary))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            yield return new DiscoveryHit(primary, aliases, Classify(type, string.Join(" ", texts), inns.Select(i => i.Classification).FirstOrDefault(), area),
                area.AreaId, new DiscoveryEvidence(Id, "", "", "", null, null));
        }
    }

    public static string Classify(string type, string text, string? stemClassification, DiscoveryArea area)
    {
        var lower = text.ToLowerInvariant();
        switch (type.ToUpperInvariant())
        {
            case "GENETIC":
                if (lower.Contains("epigenetic") || lower.Contains("reprogramming")) return "Epigenetic Therapy";
                if (lower.Contains("cell")) return "Cell Therapy";
                if (lower.Contains("antisense") || lower.Contains("sirna") || lower.Contains("oligonucleotide")) return "Oligonucleotide";
                return "Gene Therapy";
            case "BIOLOGICAL":
                if (lower.Contains("cell therapy") || lower.Contains("car-t") || lower.Contains("stem cell") || stemClassification == "Cell Therapy") return "Cell Therapy";
                if (lower.Contains("gene therapy") || lower.Contains("aav")) return "Gene Therapy";
                return stemClassification ?? "Biologic";
            case "DIETARY_SUPPLEMENT":
                return "Supplement";
            default:
                return stemClassification ?? "Research Compound";
        }
    }

    private static readonly string[] StopSubstrings =
    [
        "placebo", "placabo", "vaccine", "chemotherapy", "saline", "membrane", "allograft", "infusion", "standard",
        "control", "sham", "alone", "anesthe", "usual care", "comparator", "immunoglobulin", "transfusion", "plasma",
    ];

    [System.Text.RegularExpressions.GeneratedRegex(@"\d\s*(mg|mcg|µg|ml|%|vg|iu|cells|x\s*10)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex DoseFragment();

    private static int WordCount(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private static string Clean(string s) => s.Trim().Trim('"', '\'', '(', ')', '.').Trim();
}
