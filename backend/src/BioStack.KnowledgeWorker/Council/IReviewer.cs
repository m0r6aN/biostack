namespace BioStack.KnowledgeWorker.Council;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;

/// <summary>A provider-agnostic council reviewer.</summary>
public interface IReviewer
{
    string Id { get; }
    string Provider { get; }
    string Model { get; }
    Task<ReviewerVerdict> ReviewAsync(CouncilReviewItem item, CancellationToken cancellationToken);
}

public sealed class ReviewerResponseException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Shared prompt and verdict parsing for every provider.</summary>
public static class ReviewerPrompt
{
    public static readonly string System =
        "You are one of two independent reviewers on the BioStack evidence council. BioStack shows what is known " +
        "and unknown about a compound with sources; it never prescribes. Doses from studies and from online " +
        "communities may be shown, each with its source and grade, or 'n/a'. Check the record against the BioStack " +
        "7-tier methodology: Established, Plausible Theory, Early Evidence, Anecdotal Signal, Contradicted, " +
        "Unsupported, Hype Exceeds Evidence (or 'Not graded'). Check that every claim is supported by its cited " +
        "sources, that grades are not rounded up, that dangers and unknowns are stated, and that no text tells the " +
        "reader what they should take. List any legal or compliance concern separately in legalRisks; legal items " +
        "never decide the outcome. Reply with ONLY a JSON object: {\"decision\": \"approve\" | \"request-changes\" | " +
        "\"reject\", \"proposedTier\": <overall tier label>, \"concerns\": [string], \"legalRisks\": [string], " +
        "\"rationale\": string, \"claims\": [...]}. If the record has evidence.publicClaims, add one entry to " +
        "\"claims\" per public claim: {\"id\": <claim id>, \"decision\": \"approve\" | \"request-changes\" | \"reject\", " +
        "\"proposedTier\": <tier the cited sources support for that claim>, \"rationale\": string}. Approve a claim only " +
        "when its tier is supported by its own cited sources, its note says nothing the sources do not, and it carries " +
        "no dose and no advice; otherwise request changes with the tier you would give.";

    public static string User(CouncilReviewItem item)
        => "Review this BioStack compound record. Current overall tier: " + item.CurrentOverallTier + ".\n" +
           item.Payload.ToJsonString();

    public static ReviewerVerdict Parse(string reviewerId, string provider, string model, string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) throw new ReviewerResponseException("Reviewer reply contained no JSON object.");
        JsonObject obj;
        try { obj = JsonNode.Parse(text[start..(end + 1)]) as JsonObject ?? throw new ReviewerResponseException("Reviewer reply was not a JSON object."); }
        catch (JsonException ex) { throw new ReviewerResponseException("Reviewer reply was not valid JSON.", ex); }

        var decision = (obj["decision"]?.GetValue<string>() ?? string.Empty).Trim().ToLowerInvariant();
        if (!CouncilDecisions.All.Contains(decision))
            throw new ReviewerResponseException($"Reviewer decision '{decision}' is not one of {string.Join(", ", CouncilDecisions.All)}.");
        var rawTier = (obj["proposedTier"]?.GetValue<string>() ?? string.Empty).Trim();
        var tier = RecordEvidenceTiers.All.FirstOrDefault(t => t.Equals(rawTier, StringComparison.OrdinalIgnoreCase))
            ?? throw new ReviewerResponseException($"Reviewer proposedTier '{rawTier}' is not on the 7-tier scale.");
        return new ReviewerVerdict(reviewerId, provider, model, decision, tier,
            Strings(obj["concerns"]), Strings(obj["legalRisks"]),
            obj["rationale"]?.GetValue<string>()?.Trim() ?? string.Empty)
        {
            Claims = ParseClaims(obj["claims"]),
        };
    }

    /// <summary>
    /// Per-claim verdicts. An entry with an unknown decision or an off-scale tier is dropped,
    /// so that claim is treated as unreviewed (incomplete) rather than failing the whole record.
    /// </summary>
    private static IReadOnlyList<ClaimReviewVerdict> ParseClaims(JsonNode? node)
    {
        if (node is not JsonArray items) return Array.Empty<ClaimReviewVerdict>();
        var list = new List<ClaimReviewVerdict>();
        foreach (var item in items.OfType<JsonObject>())
        {
            string Read(string key) => item[key] is JsonValue v && v.TryGetValue<string>(out var str) ? str.Trim() : string.Empty;
            var id = Read("id");
            var decision = Read("decision").ToLowerInvariant();
            var tier = RecordEvidenceTiers.All.FirstOrDefault(t => t.Equals(Read("proposedTier"), StringComparison.OrdinalIgnoreCase));
            if (id.Length == 0 || !CouncilDecisions.All.Contains(decision) || tier is null) continue;
            if (list.Any(c => c.ClaimId == id)) continue;
            list.Add(new ClaimReviewVerdict(id, decision, tier, Read("rationale")));
        }
        return list;
    }

    private static IReadOnlyList<string> Strings(JsonNode? node)
        => node is JsonArray a
            ? a.Select(x => x?.ToString().Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).Distinct().ToList()
            : Array.Empty<string>();
}

/// <summary>OpenAI Chat Completions–compatible reviewer (OpenAI, Azure OpenAI, xAI, OpenRouter, vLLM…).</summary>
public sealed class OpenAiCompatibleReviewer(CouncilReviewerOptions options, string id, HttpClient http) : IReviewer
{
    public string Id => id;
    public string Provider => CouncilProviders.OpenAiCompatible;
    public string Model => options.Model;

    public async Task<ReviewerVerdict> ReviewAsync(CouncilReviewItem item, CancellationToken cancellationToken)
    {
        var endpoint = (string.IsNullOrWhiteSpace(options.Endpoint) ? "https://api.openai.com/v1" : options.Endpoint!).TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint + "/chat/completions");
        if (string.Equals(options.AuthHeader, "api-key", StringComparison.OrdinalIgnoreCase))
            request.Headers.Add("api-key", options.ApiKey);
        else
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        var body = new JsonObject
        {
            ["model"] = options.Model,
            ["temperature"] = 0,
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = ReviewerPrompt.System },
                new JsonObject { ["role"] = "user", ["content"] = ReviewerPrompt.User(item) }),
        };
        request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ReviewerResponseException($"{Provider} {Model} returned HTTP {(int)response.StatusCode}.");
        var content = JsonNode.Parse(text)?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
                      ?? throw new ReviewerResponseException($"{Provider} {Model} reply had no message content.");
        return ReviewerPrompt.Parse(Id, Provider, Model, content);
    }
}

/// <summary>Anthropic Messages API reviewer.</summary>
public sealed class AnthropicReviewer(CouncilReviewerOptions options, string id, HttpClient http) : IReviewer
{
    public string Id => id;
    public string Provider => CouncilProviders.Anthropic;
    public string Model => options.Model;

    public async Task<ReviewerVerdict> ReviewAsync(CouncilReviewItem item, CancellationToken cancellationToken)
    {
        var endpoint = (string.IsNullOrWhiteSpace(options.Endpoint) ? "https://api.anthropic.com" : options.Endpoint!).TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint + "/v1/messages");
        request.Headers.Add("x-api-key", options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        var body = new JsonObject
        {
            ["model"] = options.Model,
            ["max_tokens"] = 2000,
            ["temperature"] = 0,
            ["system"] = ReviewerPrompt.System,
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = ReviewerPrompt.User(item) }),
        };
        request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ReviewerResponseException($"{Provider} {Model} returned HTTP {(int)response.StatusCode}.");
        var content = JsonNode.Parse(text)?["content"] as JsonArray;
        var joined = string.Concat(content?.OfType<JsonObject>()
            .Where(c => c["type"]?.GetValue<string>() == "text")
            .Select(c => c["text"]?.GetValue<string>()) ?? Array.Empty<string?>());
        if (string.IsNullOrWhiteSpace(joined))
            throw new ReviewerResponseException($"{Provider} {Model} reply had no text content.");
        return ReviewerPrompt.Parse(Id, Provider, Model, joined);
    }
}

public static class ReviewerFactory
{
    public static IReadOnlyList<IReviewer> Create(CouncilOptions options, HttpMessageHandler? handler = null)
    {
        var missing = options.MissingSettings();
        if (missing.Count > 0)
            throw new InvalidOperationException("AI council is not configured. Set: " + string.Join("; ", missing));
        var reviewers = new List<IReviewer>();
        for (var i = 0; i < options.Reviewers.Count; i++)
        {
            var r = options.Reviewers[i];
            var id = string.IsNullOrWhiteSpace(r.Id) ? $"reviewer-{i}" : r.Id!.Trim();
            var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            http.Timeout = TimeSpan.FromSeconds(Math.Max(10, options.RequestTimeoutSeconds));
            reviewers.Add(r.Provider.Trim().ToLowerInvariant() switch
            {
                CouncilProviders.Anthropic => new AnthropicReviewer(r, id, http),
                _ => new OpenAiCompatibleReviewer(r, id, http),
            });
        }
        return reviewers;
    }
}
