namespace BioStack.KnowledgeWorker.Discovery;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// Discovered records live beside the curated seed, never inside it: curated discovered records
/// ship in <c>Seeds/discovered-records/</c>, and RunMode=Discovery writes drafts to the runtime
/// directory. Refresh, CouncilReview and ResearchPlanning read seed + both directories. The seed
/// always wins on a name collision, then curated, then runtime drafts (oldest file first).
/// </summary>
public static class DiscoveredRecordStore
{
    public static IReadOnlyList<string> Files(IEnumerable<string?> directories)
        => directories
            .Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d))
            .SelectMany(d => Directory.GetFiles(d!, "*.json").OrderBy(f => f, StringComparer.Ordinal))
            .ToList();

    /// <summary>Seed records followed by discovered records whose names are not already present.</summary>
    public static JsonArray Combine(JsonArray seed, IEnumerable<string?> directories)
    {
        var combined = new JsonArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in seed) Add(record, combined, seen);
        foreach (var file in Files(directories))
        {
            var node = JsonNode.Parse(File.ReadAllText(file));
            var records = node as JsonArray ?? (node is JsonObject o ? new JsonArray(o.DeepClone()) : new JsonArray());
            foreach (var record in records)
            {
                if (IsScreenedOut(record)) continue;
                Add(record, combined, seen);
            }
        }
        return combined;
    }

    /// <summary>
    /// True for an auto-discovered draft whose name is certainly not a substance (a procedure, an ATC code, an
    /// excipient code). Drafts written before screening existed ("blood sampling", "L01A", "F-127") are dropped
    /// here, so Refresh no longer publishes them (it soft-retires the stored rows) and the council never reviews them.
    /// Curated records are never screened out.
    /// </summary>
    public static bool IsScreenedOut(JsonNode? record)
    {
        if (record is not JsonObject obj) return false;
        var ops = obj["ops"] as JsonObject;
        var autoDiscovered =
            (ops?["ingestionSource"] is JsonValue src && src.TryGetValue<string>(out var s) && s.StartsWith(KnowledgeVisibility.DiscoveryIngestionPrefix, StringComparison.OrdinalIgnoreCase))
            || ((ops?["qualityFlags"] as JsonArray)?.Any(f => f is JsonValue v && v.TryGetValue<string>(out var t) && string.Equals(t, KnowledgeVisibility.AutoDiscoveredFlag, StringComparison.OrdinalIgnoreCase)) ?? false);
        if (!autoDiscovered) return false;
        var name = obj["identity"]?["canonicalName"]?.GetValue<string>();
        return CandidateScreening.Screen(name).Verdict == ScreenVerdict.Reject;
    }

    /// <summary>Normalized canonical names and aliases of every record in the array.</summary>
    public static HashSet<string> NameKeys(IEnumerable<JsonNode?> records)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in records.OfType<JsonObject>())
        {
            var identity = r["identity"] as JsonObject;
            var name = identity?["canonicalName"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(name)) keys.Add(CandidateNames.Normalize(name));
        }
        return keys;
    }

    private static void Add(JsonNode? record, JsonArray into, HashSet<string> seen)
    {
        var name = record?["identity"]?["canonicalName"]?.GetValue<string>();
        if (record is null || string.IsNullOrWhiteSpace(name)) return;
        if (!seen.Add(CandidateNames.Normalize(name))) return;
        into.Add(record.DeepClone());
    }
}
