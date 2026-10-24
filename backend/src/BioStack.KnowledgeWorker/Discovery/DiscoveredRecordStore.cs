namespace BioStack.KnowledgeWorker.Discovery;

using System.Text.Json.Nodes;

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
            foreach (var record in records) Add(record, combined, seen);
        }
        return combined;
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
