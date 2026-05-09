namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public sealed record ResearchReviewQueueItem(
    string ItemId,
    string CompoundName,
    string Severity,
    string Reason,
    IReadOnlyList<string> References);

public interface IResearchReviewQueueBuilder
{
    IReadOnlyList<ResearchReviewQueueItem> BuildFromEvidencePacket(JsonNode evidencePacket);
}

public sealed class ResearchReviewQueueBuilder : IResearchReviewQueueBuilder
{
    public IReadOnlyList<ResearchReviewQueueItem> BuildFromEvidencePacket(JsonNode evidencePacket)
    {
        if (evidencePacket is null) throw new ArgumentNullException(nameof(evidencePacket));

        var root = evidencePacket.AsObject();
        var compoundName = ReadString(root["compound"]?["canonicalName"]);
        var slug = SubstanceRecordNormalizer.Slugify(compoundName);
        var items = new List<ResearchReviewQueueItem>();

        foreach (var reason in ReadStringArray(root["ops"]?["reviewReasons"]))
        {
            items.Add(new ResearchReviewQueueItem(
                ItemId: $"{slug}-ops-review-{items.Count + 1}",
                CompoundName: compoundName,
                Severity: "review",
                Reason: reason,
                References: Array.Empty<string>()));
        }

        foreach (var conflictNode in root["conflicts"]?.AsArray() ?? new JsonArray())
        {
            if (conflictNode is null) continue;
            var conflict = conflictNode.AsObject();
            var status = ReadString(conflict["resolutionStatus"]);
            if (status.Equals("resolved", StringComparison.OrdinalIgnoreCase)) continue;

            items.Add(new ResearchReviewQueueItem(
                ItemId: ReadString(conflict["conflictId"]) is { Length: > 0 } id ? id : $"{slug}-conflict-{items.Count + 1}",
                CompoundName: compoundName,
                Severity: ReadString(conflict["severity"]) is { Length: > 0 } s ? s : "review",
                Reason: ReadString(conflict["summary"]),
                References: ReadStringArray(conflict["claimRefs"])));
        }

        return items;
    }

    private static string ReadString(JsonNode? node)
        => node?.GetValue<string>()?.Trim() ?? string.Empty;

    private static List<string> ReadStringArray(JsonNode? node)
    {
        if (node is not JsonArray arr) return new List<string>();
        return arr.Select(item => item?.GetValue<string>()?.Trim() ?? string.Empty)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}