using System.Globalization;
using System.Text.Json.Nodes;

namespace BioStack.KnowledgeWorker.Pipeline;

/// <summary>One council result for one public claim, read from a review-decision batch.</summary>
public sealed record ClaimSignOff(
    string CompoundName,
    string ClaimId,
    string DecisionId,
    string Outcome,
    string? Decision,
    string? Tier,
    IReadOnlyList<string> ReviewerModels,
    DateTimeOffset ReviewedAt);

/// <summary>
/// Claim-level sign-offs from the two-reviewer AI council (<c>council.claims</c> in
/// review-decision batches). Owner ruling: no human approval gates; consensus publishes,
/// disagreement escalates to the owner.
/// </summary>
public sealed class ClaimSignOffIndex
{
    private readonly Dictionary<(string, string), List<ClaimSignOff>> _byClaim;

    public static ClaimSignOffIndex Empty { get; } = new(new());

    private ClaimSignOffIndex(Dictionary<(string, string), List<ClaimSignOff>> byClaim) => _byClaim = byClaim;

    public static ClaimSignOffIndex FromBatches(IEnumerable<JsonNode> batches)
    {
        var map = new Dictionary<(string, string), List<ClaimSignOff>>();
        foreach (var batch in batches)
        {
            foreach (var decision in (batch["decisions"] as JsonArray)?.OfType<JsonObject>() ?? Enumerable.Empty<JsonObject>())
            {
                var compound = Str(decision["compoundName"]);
                if (compound.Length == 0 || decision["council"]?["claims"] is not JsonArray claims) continue;
                var reviewedAt = Date(decision["reviewedAt"]);
                if (reviewedAt is null) continue;
                foreach (var c in claims.OfType<JsonObject>())
                {
                    var id = Str(c["claimId"]);
                    if (id.Length == 0) continue;
                    var models = (c["reviewers"] as JsonArray)?.OfType<JsonObject>()
                        .Select(r => $"{Str(r["provider"])}/{Str(r["model"])}").ToList() ?? new List<string>();
                    var signOff = new ClaimSignOff(compound, id, Str(decision["decisionId"]), Str(c["outcome"]),
                        NullableStr(c["decision"]), NullableStr(c["tier"]), models, reviewedAt.Value);
                    var key = (Key(compound), id);
                    if (!map.TryGetValue(key, out var list)) map[key] = list = new List<ClaimSignOff>();
                    list.Add(signOff);
                }
            }
        }
        foreach (var list in map.Values) list.Sort((a, b) => a.ReviewedAt.CompareTo(b.ReviewedAt));
        return new ClaimSignOffIndex(map);
    }

    /// <summary>Best effort: reads every review-decision batch in the directories; missing directories are skipped.</summary>
    public static ClaimSignOffIndex FromDirectories(IEnumerable<string?> directories)
    {
        var batches = new List<JsonNode>();
        foreach (var dir in directories.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => Path.GetFullPath(d!)).Distinct())
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "review-decision-batch-*.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                try { if (JsonNode.Parse(File.ReadAllText(file)) is { } node) batches.Add(node); }
                catch (System.Text.Json.JsonException) { /* unreadable batch: ignored here; Refresh's gate reports it */ }
            }
        }
        return FromBatches(batches);
    }

    /// <summary>All sign-offs for a claim, oldest first.</summary>
    public IReadOnlyList<ClaimSignOff> For(string compoundName, string claimId)
        => _byClaim.TryGetValue((Key(compoundName), claimId), out var list) ? list : Array.Empty<ClaimSignOff>();

    private static string Key(string compound) => compound.Trim().ToLowerInvariant();
    private static string Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s.Trim() : string.Empty;
    private static string? NullableStr(JsonNode? n) => Str(n) is { Length: > 0 } s ? s : null;
    private static DateTimeOffset? Date(JsonNode? n)
        => DateTimeOffset.TryParse(Str(n), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d) ? d : null;
}

/// <summary>Supplies the current claim sign-offs to the canonicalizer.</summary>
public interface IClaimSignOffSource
{
    ClaimSignOffIndex Current { get; }
}

public sealed class EmptyClaimSignOffSource : IClaimSignOffSource
{
    public static readonly EmptyClaimSignOffSource Instance = new();
    public ClaimSignOffIndex Current => ClaimSignOffIndex.Empty;
}

/// <summary>Reads council sign-offs from the review-decision directories once, at first use.</summary>
public sealed class DirectoryClaimSignOffSource(IEnumerable<string?> directories) : IClaimSignOffSource
{
    private readonly Lazy<ClaimSignOffIndex> _index = new(() => ClaimSignOffIndex.FromDirectories(directories));
    public ClaimSignOffIndex Current => _index.Value;
}
