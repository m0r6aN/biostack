namespace BioStack.KnowledgeWorker.Council;

using System.Globalization;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;

/// <summary>Picks which records the council reviews this run and builds what reviewers see.</summary>
public static class CouncilItemSelector
{
    private static readonly string[] Sections =
        { "identity", "regulatory", "indications", "evidence", "safety", "doseReports", "interactions" };

    /// <summary>
    /// A record is due when no council or owner decision is on file that is at least as new as
    /// its <c>provenance.lastReviewedAt</c>. Records whose latest decision is
    /// <c>escalate-to-owner</c> wait for the owner and are not re-sent.
    /// </summary>
    public static IReadOnlyList<CouncilReviewItem> Select(JsonArray seed, ReviewDecisionIndex index, int max)
    {
        var items = new List<CouncilReviewItem>();
        foreach (var record in seed.OfType<JsonObject>())
        {
            if (items.Count >= Math.Max(0, max)) break;
            var name = (record["identity"] as JsonObject)?["canonicalName"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name)) continue;
            var reviewed = ParseDate((record["provenance"] as JsonObject)?["lastReviewedAt"]?.GetValue<string>());
            var decisions = index.ForCompound(name);
            if (decisions.Count > 0)
            {
                var latest = decisions[0];
                if (latest.Decision.Equals("escalate-to-owner", StringComparison.OrdinalIgnoreCase)) continue;
                if (reviewed is null || latest.ReviewedAt >= reviewed) continue;
            }
            items.Add(Build(record, name!, reviewed));
        }
        return items;
    }

    public static CouncilReviewItem Build(JsonObject record, string name, DateTimeOffset? reviewed)
    {
        var payload = new JsonObject();
        foreach (var section in Sections)
            if (record[section] is { } node) payload[section] = node.DeepClone();
        if ((record["provenance"] as JsonObject)?["sourceRecords"] is { } sources)
            payload["sources"] = sources.DeepClone();
        var tier = (record["evidence"] as JsonObject)?["overallTier"]?.GetValue<string>() ?? RecordEvidenceTiers.NotGraded;
        return new CouncilReviewItem(name, RecordEvidenceTiers.Normalize(tier), reviewed, payload);
    }

    private static DateTimeOffset? ParseDate(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d) ? d : null;
}
