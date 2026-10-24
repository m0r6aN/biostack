namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>Planner settings for one scheduled research-planning pass.</summary>
public sealed record ResearchScheduleSettings(
    bool IncludeUpdates,
    bool IncludeDiscovery,
    int UpdateCheckStaleDays,
    int MaxUpdateRequests,
    int MaxDiscoveryRequests);

/// <summary>One compound the owner wants BioStack to look for (from the discovery watchlist).</summary>
public sealed record DiscoveryCandidate(
    string CompoundName,
    IReadOnlyList<string> Aliases,
    string Classification,
    IReadOnlyList<string> Categories,
    string? Rationale);

/// <summary>
/// Turns the current corpus and the discovery watchlist into a schema-valid
/// <c>research-request-batch</c> (owner ruling 7, 2026-10-24: schedules for all data tasks).
/// Update checks cover every existing entry whose <c>provenance.lastReviewedAt</c> is older
/// than the stale window, oldest first; discovery covers watchlist compounds not already in
/// the corpus. Deterministic for a given <paramref name="now"/>: no clock, no I/O.
/// </summary>
public static class ResearchSchedulePlanner
{
    public const string RequesterId = "biostack-scheduler";

    private static readonly HashSet<string> WeakTiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Plausible Theory", "Anecdotal Signal", "Unsupported", "Hype Exceeds Evidence", "Contradicted", "Not graded",
    };

    public static JsonObject? Plan(
        JsonArray seed,
        IReadOnlyList<DiscoveryCandidate> watchlist,
        ResearchScheduleSettings settings,
        DateTimeOffset now,
        IReadOnlyDictionary<string, DateTimeOffset>? recentlyRequested = null)
    {
        recentlyRequested ??= new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(watchlist);
        ArgumentNullException.ThrowIfNull(settings);

        var stamp = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var requestedAt = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var requests = new JsonArray();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int updates = 0, discoveries = 0;

        foreach (var record in seed.OfType<JsonObject>())
        {
            foreach (var name in Names(record)) seen.Add(name);
        }

        if (settings.IncludeUpdates)
        {
            var cutoff = now.AddDays(-Math.Max(0, settings.UpdateCheckStaleDays));
            var due = seed.OfType<JsonObject>()
                .Select(r => (Record: r, Name: Str(r, "identity", "canonicalName"), Reviewed: LastReviewed(r)))
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => (x.Record, x.Name, x.Reviewed,
                    Touched: Max(x.Reviewed, recentlyRequested.TryGetValue(x.Name!, out var asked) ? asked : null)))
                .Where(x => x.Touched is null || x.Touched <= cutoff)
                .OrderBy(x => x.Touched ?? DateTimeOffset.MinValue)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(0, settings.MaxUpdateRequests));

            foreach (var (record, name, reviewed, _) in due)
            {
                var tier = Str(record, "evidence", "overallTier");
                var regulatory = Str(record, "regulatory", "regulatoryStatus");
                var weak = WeakTiers.Contains(tier ?? "Not graded") || string.IsNullOrWhiteSpace(regulatory);
                requests.Add(Request(
                    $"update-{Slug(name!)}-{stamp}",
                    name!,
                    Aliases(record),
                    Str(record, "identity", "classification") ?? "Unknown",
                    new[] { "scheduled-update-check" },
                    weak ? "high" : "normal",
                    requestedAt,
                    $"Scheduled update check. Last reviewed {(reviewed?.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "never")}. " +
                    "Look for new trials and results, label or regulatory changes, safety signals, and doses reported in studies and online, each with its source and grade.",
                    weak
                        ? new[] { "Prioritized: weak overall grade or missing regulatory status." }
                        : Array.Empty<string>()));
                updates++;
            }
        }

        if (settings.IncludeDiscovery)
        {
            foreach (var candidate in watchlist)
            {
                if (discoveries >= Math.Max(0, settings.MaxDiscoveryRequests)) break;
                if (string.IsNullOrWhiteSpace(candidate.CompoundName)) continue;
                if (seen.Contains(candidate.CompoundName) || candidate.Aliases.Any(seen.Contains)) continue;
                if (recentlyRequested.ContainsKey("discover:" + candidate.CompoundName)) continue;
                seen.Add(candidate.CompoundName);
                requests.Add(Request(
                    $"discover-{Slug(candidate.CompoundName)}-{stamp}",
                    candidate.CompoundName.Trim(),
                    candidate.Aliases,
                    string.IsNullOrWhiteSpace(candidate.Classification) ? "Unknown" : candidate.Classification,
                    candidate.Categories.Append("scheduled-discovery").Distinct(StringComparer.Ordinal).ToArray(),
                    "normal",
                    requestedAt,
                    string.IsNullOrWhiteSpace(candidate.Rationale)
                        ? "Scheduled new-compound research from the discovery watchlist."
                        : candidate.Rationale!,
                    Array.Empty<string>()));
                discoveries++;
            }
        }

        if (requests.Count == 0) return null;

        return new JsonObject
        {
            ["schemaVersion"] = "1.0.0",
            ["recordType"] = "research-request-batch",
            ["batch"] = new JsonObject
            {
                ["batchId"] = $"scheduled-{now.UtcDateTime.ToString("yyyyMMdd'T'HHmm'Z'", CultureInfo.InvariantCulture)}",
                ["requesterId"] = RequesterId,
                ["requestedAt"] = requestedAt,
                ["notes"] = new JsonArray(
                    $"Generated by RunMode=ResearchPlanning: {updates} update check(s), {discoveries} discovery request(s).",
                    "Results publish labeled pending review until the AI council reaches consensus; disagreements escalate to the owner."),
            },
            ["requests"] = requests,
        };
    }

    /// <summary>Latest <c>requestedAt</c> per compound across earlier scheduled update batches,
    /// so successive passes rotate through the corpus instead of re-asking the same entries.</summary>
    public static IReadOnlyDictionary<string, DateTimeOffset> RecentUpdateRequests(IEnumerable<JsonNode?> batches)
    {
        var latest = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        foreach (var request in batches.SelectMany(b => (b?["requests"] as JsonArray) ?? new JsonArray()).OfType<JsonObject>())
        {
            var id = request["requestId"]?.GetValue<string>();
            var name = request["compoundName"]?.GetValue<string>();
            if (id is null || name is null) continue;
            if (id.StartsWith("discover-", StringComparison.Ordinal)) name = "discover:" + name;
            else if (!id.StartsWith("update-", StringComparison.Ordinal)) continue;
            if (!DateTimeOffset.TryParse(request["requestedAt"]?.GetValue<string>(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var at)) continue;
            if (!latest.TryGetValue(name, out var existing) || at > existing) latest[name] = at;
        }
        return latest;
    }

    private static DateTimeOffset? Max(DateTimeOffset? a, DateTimeOffset? b)
        => a is null ? b : b is null ? a : (a > b ? a : b);

    public static IReadOnlyList<DiscoveryCandidate> ParseWatchlist(JsonNode? node)
    {
        var entries = node?["entries"] as JsonArray ?? node as JsonArray;
        if (entries is null) return Array.Empty<DiscoveryCandidate>();
        return entries.OfType<JsonObject>()
            .Select(e => new DiscoveryCandidate(
                e["compoundName"]?.GetValue<string>() ?? string.Empty,
                Strings(e["aliases"]),
                e["classification"]?.GetValue<string>() ?? "Unknown",
                Strings(e["categories"]),
                e["rationale"]?.GetValue<string>()))
            .ToList();
    }

    private static JsonObject Request(string id, string name, IReadOnlyList<string> aliases, string classification,
        IReadOnlyList<string> categories, string priority, string requestedAt, string rationale, IReadOnlyList<string> notes)
        => new()
        {
            ["requestId"] = id,
            ["compoundName"] = name,
            ["aliases"] = new JsonArray(aliases.Distinct(StringComparer.OrdinalIgnoreCase).Select(a => (JsonNode?)a).ToArray()),
            ["categories"] = new JsonArray(categories.Select(c => (JsonNode?)c).ToArray()),
            ["classification"] = classification,
            ["priority"] = priority,
            ["requesterId"] = RequesterId,
            ["requestedAt"] = requestedAt,
            ["rationale"] = rationale,
            ["notes"] = new JsonArray(notes.Select(n => (JsonNode?)n).ToArray()),
        };

    private static IEnumerable<string> Names(JsonObject record)
    {
        var name = Str(record, "identity", "canonicalName");
        if (!string.IsNullOrWhiteSpace(name)) yield return name!;
        foreach (var alias in Aliases(record)) yield return alias;
    }

    private static IReadOnlyList<string> Aliases(JsonObject record)
    {
        var identity = record["identity"] as JsonObject;
        return Strings(identity?["aliases"]).Concat(Strings(identity?["brandNames"]))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IReadOnlyList<string> Strings(JsonNode? node)
        => node is JsonArray a
            ? a.Select(x => x?.GetValue<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToList()
            : Array.Empty<string>();

    private static DateTimeOffset? LastReviewed(JsonObject record)
        => DateTimeOffset.TryParse(Str(record, "provenance", "lastReviewedAt"), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var value) ? value : null;

    private static string? Str(JsonObject record, string section, string field)
        => (record[section] as JsonObject)?[field] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private static string Slug(string value)
        => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}
