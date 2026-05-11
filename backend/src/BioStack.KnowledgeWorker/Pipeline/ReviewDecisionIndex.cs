namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public sealed record ReviewDecisionInfo(
    string DecisionId,
    string CompoundName,
    string Decision,
    string ReviewerId,
    DateTimeOffset ReviewedAt,
    bool ClearsSoftPromotionBlockers,
    IReadOnlyList<string> ReviewQueueItemIds,
    IReadOnlyList<string> Notes);

public sealed class ReviewDecisionIndex
{
    private readonly IReadOnlyDictionary<string, List<ReviewDecisionInfo>> _byCompound;

    public static ReviewDecisionIndex Empty { get; } = new(new Dictionary<string, List<ReviewDecisionInfo>>(StringComparer.OrdinalIgnoreCase));

    private ReviewDecisionIndex(IReadOnlyDictionary<string, List<ReviewDecisionInfo>> byCompound)
    {
        _byCompound = byCompound;
    }

    public static ReviewDecisionIndex FromBatches(IEnumerable<JsonNode> batches)
    {
        var byCompound = new Dictionary<string, List<ReviewDecisionInfo>>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in batches)
        {
            foreach (var decisionNode in batch["decisions"]?.AsArray() ?? new JsonArray())
            {
                if (decisionNode is null) continue;
                var decision = decisionNode.AsObject();
                var compound = ReadString(decision["compoundName"]);
                if (compound.Length == 0) continue;

                var info = new ReviewDecisionInfo(
                    DecisionId: ReadString(decision["decisionId"]),
                    CompoundName: compound,
                    Decision: ReadString(decision["decision"]),
                    ReviewerId: ReadString(decision["reviewerId"]),
                    ReviewedAt: ReadDate(decision["reviewedAt"]),
                    ClearsSoftPromotionBlockers: ReadBool(decision["clearsSoftPromotionBlockers"]),
                    ReviewQueueItemIds: ReadStringArray(decision["scope"]?["reviewQueueItemIds"]),
                    Notes: ReadStringArray(decision["notes"]));

                if (!byCompound.TryGetValue(compound, out var list))
                {
                    list = new List<ReviewDecisionInfo>();
                    byCompound[compound] = list;
                }
                list.Add(info);
            }
        }

        foreach (var key in byCompound.Keys.ToList())
        {
            byCompound[key] = byCompound[key]
                .OrderByDescending(d => d.ReviewedAt)
                .ThenBy(d => d.DecisionId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new ReviewDecisionIndex(byCompound);
    }

    public IReadOnlyList<ReviewDecisionInfo> ForCompound(string compoundName)
        => _byCompound.TryGetValue(compoundName, out var decisions) ? decisions : Array.Empty<ReviewDecisionInfo>();

    public bool HasPromotionApproval(string compoundName)
        => ForCompound(compoundName).Any(d =>
            d.Decision.Equals("approve-for-promotion", StringComparison.OrdinalIgnoreCase)
            && d.ClearsSoftPromotionBlockers);

    public bool HasPendingRequestedChanges(string compoundName)
    {
        foreach (var decision in ForCompound(compoundName))
        {
            if (decision.Decision.Equals("approve-for-promotion", StringComparison.OrdinalIgnoreCase)
                || decision.Decision.Equals("archive-draft", StringComparison.OrdinalIgnoreCase)
                || decision.Decision.Equals("reject", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (decision.Decision.Equals("request-changes", StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    public bool IsCompoundArchived(string compoundName)
        => ForCompound(compoundName).Any(d =>
            d.Decision.Equals("archive-draft", StringComparison.OrdinalIgnoreCase)
            || d.Decision.Equals("reject", StringComparison.OrdinalIgnoreCase));

    public bool IsReviewQueueItemResolved(string compoundName, string itemId)
        => itemId.Length > 0 && ForCompound(compoundName)
            .SelectMany(d => d.ReviewQueueItemIds)
            .Contains(itemId, StringComparer.OrdinalIgnoreCase);

    private static string ReadString(JsonNode? node) => node?.GetValue<string>()?.Trim() ?? string.Empty;

    private static bool ReadBool(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<bool>(out var result) && result;

    private static DateTimeOffset ReadDate(JsonNode? node)
        => DateTimeOffset.TryParse(ReadString(node), out var parsed) ? parsed : DateTimeOffset.MinValue;

    private static List<string> ReadStringArray(JsonNode? node)
    {
        if (node is not JsonArray arr) return new List<string>();
        return arr.Select(item => item?.GetValue<string>()?.Trim() ?? string.Empty)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}