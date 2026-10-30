namespace BioStack.KnowledgeWorker.Discovery;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

public enum NameCheckOutcome
{
    /// <summary>Not checkable (a code name, a phrase, or checks are off): nothing learned.</summary>
    NotChecked,
    /// <summary>The reference list knows this spelling.</summary>
    Known,
    /// <summary>The reference list does not know it but proposes a close spelling (<see cref="NameCheck.Suggestion"/>).</summary>
    Misspelled,
    /// <summary>The reference list knows neither the name nor a close spelling. Common for new investigational names.</summary>
    Unknown,
    /// <summary>The reference service could not be reached or answered badly.</summary>
    Unavailable,
}

public sealed record NameCheck(NameCheckOutcome Outcome, string? Suggestion = null)
{
    public static readonly NameCheck NotChecked = new(NameCheckOutcome.NotChecked);
}

/// <summary>Checks a drug-like name's spelling against a public reference list. Never throws for network failures.</summary>
public interface IDrugNameVerifier
{
    Task<NameCheck> CheckAsync(string name, CancellationToken cancellationToken);
}

/// <summary>
/// NLM RxNorm API (https://rxnav.nlm.nih.gov/REST). No key or license is needed; NLM asks for at most 20
/// requests per second per IP, and the discovery HTTP client already spaces calls. Only a single plain word of
/// at least six letters is checked (an INN-style or brand name); code names ("ER-100") and phrases are not in
/// RxNorm and are left alone. A close spelling (edit distance 2 or less) is reported as a misspelling.
/// </summary>
public sealed partial class RxNormNameVerifier : IDrugNameVerifier
{
    public const string BaseUrl = "https://rxnav.nlm.nih.gov/REST";
    public const int MaxEditDistance = 2;

    private readonly DiscoveryHttp _http;
    private readonly Dictionary<string, NameCheck> _cache = new(StringComparer.Ordinal);

    public RxNormNameVerifier(DiscoveryHttp http) => _http = http ?? throw new ArgumentNullException(nameof(http));

    [GeneratedRegex(@"^[A-Za-z]{6,40}$")]
    private static partial Regex CheckableRegex();

    public static bool IsCheckable(string name) => CheckableRegex().IsMatch(name.Trim());

    public async Task<NameCheck> CheckAsync(string name, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        if (!IsCheckable(trimmed)) return NameCheck.NotChecked;
        var key = trimmed.ToLowerInvariant();
        if (_cache.TryGetValue(key, out var cached)) return cached;

        NameCheck result;
        try
        {
            var exact = await _http.GetJsonAsync($"{BaseUrl}/rxcui.json?name={Uri.EscapeDataString(trimmed)}&search=1", cancellationToken);
            if (exact?["idGroup"]?["rxnormId"] is JsonArray ids && ids.Count > 0)
            {
                result = new NameCheck(NameCheckOutcome.Known);
            }
            else
            {
                var spelling = await _http.GetJsonAsync($"{BaseUrl}/spellingsuggestions.json?name={Uri.EscapeDataString(trimmed)}", cancellationToken);
                var suggestions = (spelling?["suggestionGroup"]?["suggestionList"]?["suggestion"] as JsonArray)?
                    .Select(n => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.Trim())
                    .ToList() ?? new List<string>();
                var close = suggestions
                    .Where(s => !s.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
                                && IsCheckable(s)
                                && CandidateScreening.EditDistance(trimmed, s) is > 0 and <= MaxEditDistance)
                    .OrderBy(s => CandidateScreening.EditDistance(trimmed, s))
                    .FirstOrDefault();
                result = close is null ? new NameCheck(NameCheckOutcome.Unknown) : new NameCheck(NameCheckOutcome.Misspelled, close);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException)
        {
            if (cancellationToken.IsCancellationRequested) throw;
            return new NameCheck(NameCheckOutcome.Unavailable); // not cached: a later candidate may succeed
        }

        _cache[key] = result;
        return result;
    }
}
