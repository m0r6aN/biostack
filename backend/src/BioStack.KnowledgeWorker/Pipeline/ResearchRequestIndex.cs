namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public sealed record ResearchRequestInfo(
    string RequestId,
    string CompoundName,
    IReadOnlyList<string> Aliases,
    string Classification,
    string Priority,
    string RequesterId,
    DateTimeOffset RequestedAt,
    string Rationale,
    IReadOnlyList<string> Notes);

public sealed class ResearchRequestIndex
{
    public static ResearchRequestIndex Empty { get; } = new(new Dictionary<string, List<ResearchRequestInfo>>(StringComparer.OrdinalIgnoreCase));

    private readonly IReadOnlyDictionary<string, List<ResearchRequestInfo>> _byCompound;

    private ResearchRequestIndex(IReadOnlyDictionary<string, List<ResearchRequestInfo>> byCompound)
    {
        _byCompound = byCompound;
    }

    public static ResearchRequestIndex FromBatches(IEnumerable<JsonNode> batches)
    {
        var byCompound = new Dictionary<string, List<ResearchRequestInfo>>(StringComparer.OrdinalIgnoreCase);
        foreach (var request in batches.SelectMany(batch => batch["requests"]?.AsArray() ?? new JsonArray()))
        {
            var compound = ReadString(request?["compoundName"]);
            if (compound.Length == 0) continue;
            var info = new ResearchRequestInfo(
                RequestId: ReadString(request?["requestId"]),
                CompoundName: compound,
                Aliases: ReadStringArray(request?["aliases"]),
                Classification: ReadString(request?["classification"]),
                Priority: ReadString(request?["priority"]),
                RequesterId: ReadString(request?["requesterId"]),
                RequestedAt: ReadDate(request?["requestedAt"]),
                Rationale: ReadString(request?["rationale"]),
                Notes: ReadStringArray(request?["notes"]));

            if (!byCompound.TryGetValue(compound, out var list))
            {
                list = new List<ResearchRequestInfo>();
                byCompound[compound] = list;
            }
            list.Add(info);
        }

        return new ResearchRequestIndex(byCompound.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.OrderByDescending(r => r.RequestedAt).ToList(),
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ResearchRequestInfo> ForCompound(string compoundName)
        => _byCompound.TryGetValue(compoundName, out var list) ? list : Array.Empty<ResearchRequestInfo>();

    public IEnumerable<ResearchRequestInfo> All()
        => _byCompound.Values.SelectMany(v => v)
            .GroupBy(r => r.RequestId.Length > 0 ? r.RequestId : r.CompoundName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.RequestedAt).First())
            .OrderBy(r => r.CompoundName, StringComparer.OrdinalIgnoreCase);

    private static string ReadString(JsonNode? node) => node?.GetValue<string>()?.Trim() ?? string.Empty;

    private static DateTimeOffset ReadDate(JsonNode? node)
        => DateTimeOffset.TryParse(ReadString(node), out var parsed) ? parsed : DateTimeOffset.MinValue;

    private static IReadOnlyList<string> ReadStringArray(JsonNode? node)
        => node is JsonArray arr
            ? arr.Select(item => item?.GetValue<string>()?.Trim() ?? string.Empty)
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : Array.Empty<string>();
}