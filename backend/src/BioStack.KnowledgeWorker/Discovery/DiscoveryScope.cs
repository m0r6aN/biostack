namespace BioStack.KnowledgeWorker.Discovery;

using System.Text.Json.Nodes;

public sealed record DiscoveryArea(
    string AreaId,
    string Label,
    string DefaultClassification,
    IReadOnlyList<string> ClinicalTrialsTerms,
    string PubMedQuery,
    IReadOnlyList<string> PreprintKeywords);

public sealed record InnStem(string Suffix, string Classification);

/// <summary>
/// Scope taxonomy for open-ended discovery (<c>Seeds/discovery-scope.json</c>). Areas say
/// what to search for; INN stems and code-name rules say what a compound or therapy name
/// looks like. Gene, epigenetic, cell and oligonucleotide therapies are first-class.
/// </summary>
public sealed record DiscoveryScope(
    IReadOnlyList<DiscoveryArea> Areas,
    IReadOnlyList<string> ClinicalTrialsInterventionTypes,
    IReadOnlyList<InnStem> InnStems,
    IReadOnlyList<string> StopNames,
    IReadOnlyList<string> CodeNamePrefixDenylist,
    IReadOnlyList<string>? ExcludeKeywords = null)
{
    /// <summary>True when the text (title, conditions) is outside scope, e.g. oncology.</summary>
    public bool IsExcluded(params string?[] texts)
    {
        if (ExcludeKeywords is null || ExcludeKeywords.Count == 0) return false;
        foreach (var t in texts)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;
            var lower = t.ToLowerInvariant();
            if (ExcludeKeywords.Any(k => System.Text.RegularExpressions.Regex.IsMatch(lower, $@"\b{System.Text.RegularExpressions.Regex.Escape(k)}"))) return true;
        }
        return false;
    }

    public static DiscoveryScope Parse(JsonNode? node)
    {
        if (node is not JsonObject root) throw new InvalidOperationException("Discovery scope must be a JSON object.");
        var areas = (root["areas"] as JsonArray ?? new JsonArray())
            .OfType<JsonObject>()
            .Select(a => new DiscoveryArea(
                Str(a, "areaId"),
                Str(a, "label"),
                Str(a, "defaultClassification", "Research Compound"),
                Strings(a["clinicalTrialsTerms"]),
                Str(a, "pubmedQuery", string.Empty),
                Strings(a["preprintKeywords"])))
            .Where(a => a.AreaId.Length > 0)
            .ToList();
        if (areas.Count == 0) throw new InvalidOperationException("Discovery scope has no areas.");

        var stems = (root["innStems"] as JsonArray ?? new JsonArray())
            .OfType<JsonObject>()
            .Select(s => new InnStem(Str(s, "suffix").ToLowerInvariant(), Str(s, "classification", "Research Compound")))
            .Where(s => s.Suffix.Length > 0)
            .OrderByDescending(s => s.Suffix.Length)
            .ToList();

        return new DiscoveryScope(
            areas,
            Strings(root["clinicalTrialsInterventionTypes"]),
            stems,
            Strings(root["stopNames"]).Select(s => s.ToLowerInvariant()).ToList(),
            Strings(root["codeNamePrefixDenylist"]).Select(s => s.ToUpperInvariant()).ToList(),
            Strings(root["excludeKeywords"]).Select(s => s.ToLowerInvariant()).ToList());
    }

    public static DiscoveryScope Load(string path) => Parse(JsonNode.Parse(File.ReadAllText(path)));

    private static string Str(JsonObject o, string key, string fallback = "")
        => o[key] is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s.Trim() : fallback;

    private static IReadOnlyList<string> Strings(JsonNode? node)
        => (node as JsonArray ?? new JsonArray())
            .Select(n => n is JsonValue v && v.TryGetValue<string>(out var s) ? s.Trim() : null)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToList();
}
