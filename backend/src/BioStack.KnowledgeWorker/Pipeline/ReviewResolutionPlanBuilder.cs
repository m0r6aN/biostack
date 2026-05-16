namespace BioStack.KnowledgeWorker.Pipeline;

public sealed record ReviewResolutionPlan(
    string PlanVersion,
    DateTimeOffset GeneratedAtUtc,
    ReviewResolutionPlanCounts Counts,
    IReadOnlyList<ReviewResolutionPlanItem> Items);

public sealed record ReviewResolutionPlanCounts(
    int TotalItems,
    int BlockedItems,
    int ReviewRequiredItems,
    int ResearchRequestedItems,
    IReadOnlyList<ResearchSummaryBucket> ResolutionTypes);

public sealed record ReviewResolutionPlanItem(
    string ItemId,
    string CompoundName,
    string Readiness,
    string Severity,
    string ResolutionType,
    string Issue,
    string RecommendedAction,
    IReadOnlyList<string> RelatedReviewQueueItemIds,
    IReadOnlyList<string> RelatedBlockers,
    IReadOnlyList<string> RelatedQualityFlags);

public interface IReviewResolutionPlanBuilder
{
    ReviewResolutionPlan Build(PromotionManifest manifest, IReadOnlyList<ResearchReviewQueueItem> reviewQueue);
}

public sealed class ReviewResolutionPlanBuilder : IReviewResolutionPlanBuilder
{
    public ReviewResolutionPlan Build(PromotionManifest manifest, IReadOnlyList<ResearchReviewQueueItem> reviewQueue)
    {
        if (manifest is null) throw new ArgumentNullException(nameof(manifest));
        var queueByCompound = reviewQueue
            .GroupBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var items = manifest.Blocked.Concat(manifest.ReviewRequired).Concat(manifest.ResearchRequested)
            .SelectMany(candidate => ToItems(candidate, queueByCompound))
            .GroupBy(item => $"{item.CompoundName}\u001f{item.ResolutionType}\u001f{item.RecommendedAction}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ResolutionType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Issue, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReviewResolutionPlan(
            PlanVersion: "1.0.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Counts: new ReviewResolutionPlanCounts(
                TotalItems: items.Count,
                BlockedItems: items.Count(i => i.Readiness == "blocked"),
                ReviewRequiredItems: items.Count(i => i.Readiness == "review-required"),
                ResearchRequestedItems: items.Count(i => i.Readiness == "research-requested"),
                ResolutionTypes: Bucket(items)),
            Items: items);
    }

    private static IEnumerable<ReviewResolutionPlanItem> ToItems(
        PromotionManifestCandidate candidate,
        IReadOnlyDictionary<string, List<ResearchReviewQueueItem>> queueByCompound)
    {
        var queueItems = queueByCompound.TryGetValue(candidate.Name, out var q) ? q : new List<ResearchReviewQueueItem>();
        var issues = candidate.Blockers.Concat(queueItems.Select(i => i.Reason))
            .Where(issue => !string.IsNullOrWhiteSpace(issue))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (issues.Count == 0)
        {
            issues.Add("Manual review required before promotion.");
        }

        var index = 1;
        foreach (var issue in issues)
        {
            var resolutionType = ResolutionType(issue, candidate.QualityFlags);
            yield return new ReviewResolutionPlanItem(
                ItemId: $"{Slug(candidate.Name)}-resolution-{index++}",
                CompoundName: candidate.Name,
                Readiness: candidate.Readiness,
                Severity: candidate.Readiness == "blocked" ? "blocked" : candidate.Readiness == "research-requested" ? "research" : "review",
                ResolutionType: resolutionType,
                Issue: issue,
                RecommendedAction: RecommendedAction(resolutionType, issue),
                RelatedReviewQueueItemIds: queueItems
                    .Where(item => item.Reason.Equals(issue, StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.ItemId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                RelatedBlockers: candidate.Blockers,
                RelatedQualityFlags: candidate.QualityFlags);
        }
    }

    private static string ResolutionType(string issue, IReadOnlyList<string> qualityFlags)
    {
        if (issue.Contains("source-registry", StringComparison.OrdinalIgnoreCase)
            || qualityFlags.Contains("source-registry-field-mismatch", StringComparer.OrdinalIgnoreCase))
        {
            return "fix-source-authorization";
        }
        if (issue.Contains("authoritative", StringComparison.OrdinalIgnoreCase)
            || qualityFlags.Contains("missing-authoritative-support", StringComparer.OrdinalIgnoreCase))
        {
            return "add-authoritative-source";
        }
        if (issue.Contains("evidence tier", StringComparison.OrdinalIgnoreCase)) return "mark-insufficient-evidence";
        if (issue.Contains("completeness", StringComparison.OrdinalIgnoreCase)) return "expand-evidence-packet";
        if (issue.Contains("research-requested", StringComparison.OrdinalIgnoreCase)) return "perform-initial-research";
        if (issue.Contains("requested changes", StringComparison.OrdinalIgnoreCase)) return "targeted-research-rereview";
        if (issue.Contains("review queue", StringComparison.OrdinalIgnoreCase)
            || issue.Contains("needsReview", StringComparison.OrdinalIgnoreCase)
            || issue.Contains("human review", StringComparison.OrdinalIgnoreCase)) return "human-review";
        if (qualityFlags.Contains("route-specific-boundary", StringComparer.OrdinalIgnoreCase)
            || qualityFlags.Contains("alias-ambiguity", StringComparer.OrdinalIgnoreCase)) return "split-route-or-identity";
        if (qualityFlags.Contains("misinformation-heavy", StringComparer.OrdinalIgnoreCase)
            || qualityFlags.Contains("hype-boundary", StringComparer.OrdinalIgnoreCase)) return "remove-or-reword-claim";
        return "human-review";
    }

    private static string RecommendedAction(string resolutionType, string issue) => resolutionType switch
    {
        "fix-source-authorization" => "Add a source authorized for the required field use from an independent source family, map the source family, or change the claim type to match the evidence.",
        "add-authoritative-source" => "Attach an independent A1/A2 source for the safety, regulatory, monitoring, contraindication, or approved-use claim before promotion.",
        "mark-insufficient-evidence" => "Keep the claim review-gated and use evidence-gap or insufficient-evidence language until stronger human evidence is added.",
        "expand-evidence-packet" => "Add missing claims, independent sources, safety context, provenance, or review notes until completeness is substantial or complete.",
        "perform-initial-research" => "Perform initial evidence research across multiple source families, generate a compound evidence packet, and rerun the research worker.",
        "targeted-research-rereview" => "Use the requested-change notes to perform targeted follow-up research against independent source families not already used by the original claim, regenerate artifacts, and send the draft back for human re-review.",
        "split-route-or-identity" => "Separate route, formulation, ester/salt, product, alias, or parent-compound claims into distinct evidence records.",
        "remove-or-reword-claim" => "Remove benefit language or reword as a popularity, misinformation, controversy, or evidence-gap claim.",
        _ => $"Human reviewer must assess and resolve using cross-source verification: {issue}"
    };

    private static IReadOnlyList<ResearchSummaryBucket> Bucket(IReadOnlyList<ReviewResolutionPlanItem> items)
        => items.GroupBy(item => item.ResolutionType, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ResearchSummaryBucket(
                Name: group.Key,
                Count: group.Select(item => item.CompoundName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Compounds: group.Select(item => item.CompoundName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderByDescending(bucket => bucket.Count)
            .ThenBy(bucket => bucket.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string Slug(string value)
        => new string(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
}