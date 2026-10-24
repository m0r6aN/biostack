namespace BioStack.KnowledgeWorker.Discovery;

using System.Text.Json.Nodes;

/// <summary>
/// Every name the corpus already knows (canonical names, aliases, brands, synonyms, moieties,
/// slugs), normalized, plus text mentions that sit next to a record's own name — the
/// "enobosarm in the Ostarine record" case, where a compound is present but unfindable by name.
/// </summary>
public sealed class CorpusNameIndex
{
    private const int MentionWindow = 40;
    private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
    private readonly List<(string Record, string Text, IReadOnlyList<string> OwnNames)> _texts = new();

    public int RecordCount { get; private set; }

    public static CorpusNameIndex Build(IEnumerable<JsonNode?> records)
    {
        var index = new CorpusNameIndex();
        foreach (var node in records)
        {
            if (node is not JsonObject record || record["identity"] is not JsonObject identity) continue;
            var name = identity["canonicalName"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name)) continue;
            index.RecordCount++;

            var own = new List<string> { name };
            if (identity["slug"] is JsonValue slug && slug.TryGetValue<string>(out var s)) own.Add(s);
            foreach (var key in new[] { "aliases", "brandNames", "synonyms", "activeMoieties" })
                own.AddRange((identity[key] as JsonArray ?? new JsonArray())
                    .Select(n => n is JsonValue v && v.TryGetValue<string>(out var t) ? t : null)
                    .Where(t => !string.IsNullOrWhiteSpace(t))!);

            foreach (var n in own)
            {
                var key = CandidateNames.Normalize(n);
                if (key.Length >= 2) index._names.TryAdd(key, name);
            }
            index._texts.Add((name, record.ToJsonString(), own));
        }
        return index;
    }

    /// <summary>The record that already carries this name (or any alias), or null.</summary>
    public string? FindRecord(IEnumerable<string> names)
    {
        foreach (var n in names)
        {
            var key = CandidateNames.Normalize(n);
            if (key.Length >= 2 && _names.TryGetValue(key, out var record)) return record;
        }
        return null;
    }

    /// <summary>
    /// The single record whose text mentions one of <paramref name="names"/> within a few
    /// characters of that record's own name or alias. Null when none or more than one.
    /// </summary>
    public string? FindPossibleAliasOwner(IEnumerable<string> names)
    {
        var owners = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in names.Where(n => n.Trim().Length >= 4))
        {
            var needle = raw.Trim();
            foreach (var (record, text, own) in _texts)
            {
                var at = text.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                while (at >= 0)
                {
                    var lo = Math.Max(0, at - MentionWindow);
                    var hi = Math.Min(text.Length, at + needle.Length + MentionWindow);
                    var window = text[lo..hi];
                    if (own.Any(o => o.Length >= 3
                                     && !o.Equals(needle, StringComparison.OrdinalIgnoreCase)
                                     && window.Contains(o, StringComparison.OrdinalIgnoreCase)))
                    {
                        owners.Add(record);
                        break;
                    }
                    at = text.IndexOf(needle, at + needle.Length, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        return owners.Count == 1 ? owners.First() : null;
    }
}
