namespace BioStack.KnowledgeWorker.Pipeline;

public sealed record ResearchTaskQueue(
    string QueueVersion,
    DateTimeOffset GeneratedAtUtc,
    ResearchTaskQueueCounts Counts,
    IReadOnlyList<ResearchTaskQueueItem> Items,
    IReadOnlyList<ResearchTaskQueueResolvedItem> ResolvedItems);

public sealed record ResearchTaskQueueCounts(
    int TotalItems,
    int Urgent,
    int High,
    int Normal,
    int Low,
    int ResolvedItems);

public sealed record ResearchTaskQueueItem(
    string TaskId,
    string TaskType,
    string CompoundName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Categories,
    string Classification,
    string Priority,
    IReadOnlyList<string> RequestIds,
    IReadOnlyList<string> RequesterIds,
    DateTimeOffset FirstRequestedAtUtc,
    DateTimeOffset LatestRequestedAtUtc,
    IReadOnlyList<string> Rationales,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> SuggestedResearchDirectives,
    string TargetEvidencePath,
    string RequiredSchema,
    IReadOnlyList<string> RemediationPlanItemIds,
    IReadOnlyList<string> RemediationResolutionTypes,
    IReadOnlyList<string> RemediationRecommendedActions,
    IReadOnlyList<string> RelatedReviewQueueItemIds);

public sealed record ResearchTaskQueueResolvedItem(
    string TaskId,
    string CompoundName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Categories,
    string Classification,
    string Priority,
    IReadOnlyList<string> RequestIds,
    IReadOnlyList<string> RequesterIds,
    DateTimeOffset FirstRequestedAtUtc,
    DateTimeOffset LatestRequestedAtUtc,
    DateTimeOffset ResolvedAtUtc,
    string CurrentReadiness,
    string Resolution,
    string ResolutionReason,
    string TargetEvidencePath);

public interface IResearchTaskQueueBuilder
{
    ResearchTaskQueue Build(ResearchSummary summary, ResearchRequestIndex researchRequests, string targetEvidenceDirectory, int reviewSourceExpansionLimit = 0, ReviewResolutionPlan? reviewResolutionPlan = null);
}

public sealed class ResearchTaskQueueBuilder : IResearchTaskQueueBuilder
{
    private const string DefaultTargetEvidenceDirectory = "research/input/evidence";
    private const string EvidencePacketSchema = "evidence-packet.schema.json";

    public ResearchTaskQueue Build(ResearchSummary summary, ResearchRequestIndex researchRequests, string targetEvidenceDirectory, int reviewSourceExpansionLimit = 0, ReviewResolutionPlan? reviewResolutionPlan = null)
    {
        if (summary is null) throw new ArgumentNullException(nameof(summary));
        if (researchRequests is null) throw new ArgumentNullException(nameof(researchRequests));

        var generatedAtUtc = DateTimeOffset.UtcNow;
        var resolvedTargetDirectory = NormalizePath(string.IsNullOrWhiteSpace(targetEvidenceDirectory)
            ? DefaultTargetEvidenceDirectory
            : targetEvidenceDirectory);
        var compoundsByName = summary.Compounds.ToDictionary(compound => compound.Name, StringComparer.OrdinalIgnoreCase);
        var remediationByCompound = (reviewResolutionPlan?.Items ?? Array.Empty<ReviewResolutionPlanItem>())
            .GroupBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var items = summary.Compounds
            .Where(compound => compound.PromotionReadiness.Equals("research-requested", StringComparison.OrdinalIgnoreCase))
            .Where(compound => compound.HasResearchRequest)
            .Select(compound => ToItem(
                compound,
                researchRequests.ForCompound(compound.Name),
                resolvedTargetDirectory,
                remediationByCompound.TryGetValue(compound.Name, out var remediationItems) ? remediationItems : Array.Empty<ReviewResolutionPlanItem>()))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => PriorityRank(item.Priority))
            .ThenByDescending(item => item.LatestRequestedAtUtc)
            .ThenBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        items.AddRange(summary.Compounds
            .Where(compound => ShouldEmitSourceExpansionTask(compound, reviewSourceExpansionLimit))
            .Select(compound => ToSourceExpansionItem(
                compound,
                generatedAtUtc,
                resolvedTargetDirectory,
                reviewSourceExpansionLimit,
                remediationByCompound.TryGetValue(compound.Name, out var remediationItems) ? remediationItems : Array.Empty<ReviewResolutionPlanItem>())));

        items = items
            .OrderBy(item => PriorityRank(item.Priority))
            .ThenByDescending(item => item.LatestRequestedAtUtc)
            .ThenBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.TaskType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resolvedItems = researchRequests.All()
            .GroupBy(request => request.CompoundName, StringComparer.OrdinalIgnoreCase)
            .Select(group => ToResolvedItem(group.Key, group.ToList(), compoundsByName, generatedAtUtc, resolvedTargetDirectory))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.ResolvedAtUtc)
            .ThenBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResearchTaskQueue(
            QueueVersion: "1.0.0",
            GeneratedAtUtc: generatedAtUtc,
            Counts: new ResearchTaskQueueCounts(
                TotalItems: items.Count,
                Urgent: items.Count(item => item.Priority.Equals("urgent", StringComparison.OrdinalIgnoreCase)),
                High: items.Count(item => item.Priority.Equals("high", StringComparison.OrdinalIgnoreCase)),
                Normal: items.Count(item => item.Priority.Equals("normal", StringComparison.OrdinalIgnoreCase)),
                Low: items.Count(item => item.Priority.Equals("low", StringComparison.OrdinalIgnoreCase)),
                ResolvedItems: resolvedItems.Count),
            Items: items,
            ResolvedItems: resolvedItems);
    }

    private static ResearchTaskQueueItem ToSourceExpansionItem(
        ResearchSummaryCompound compound,
        DateTimeOffset generatedAtUtc,
        string targetEvidenceDirectory,
        int reviewSourceExpansionLimit,
        IReadOnlyList<ReviewResolutionPlanItem> remediationItems)
    {
        var slug = SubstanceRecordNormalizer.Slugify(compound.Name);
        var remaining = Math.Max(0, reviewSourceExpansionLimit - compound.SourceFamilies.Count);
        remediationItems = ScopedRemediationItems(compound, remediationItems);
        var remediationPlanItemIds = remediationItems.Select(item => item.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        var remediationResolutionTypes = remediationItems.Select(item => item.ResolutionType).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
        var remediationRecommendedActions = remediationItems.Select(item => item.RecommendedAction).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
        var relatedReviewQueueItemIds = remediationItems.SelectMany(item => item.RelatedReviewQueueItemIds).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        return new ResearchTaskQueueItem(
            TaskId: $"{slug}-review-source-expansion",
            TaskType: "expand-review-sources",
            CompoundName: compound.Name,
            Aliases: Array.Empty<string>(),
            Categories: Array.Empty<string>(),
            Classification: compound.Classification,
            Priority: compound.PromotionReadiness.Equals("blocked", StringComparison.OrdinalIgnoreCase) ? "high" : "normal",
            RequestIds: Array.Empty<string>(),
            RequesterIds: Array.Empty<string>(),
            FirstRequestedAtUtc: generatedAtUtc,
            LatestRequestedAtUtc: generatedAtUtc,
            Rationales: compound.PromotionBlockers.Concat(compound.ReviewReasons).Concat(remediationItems.Select(item => item.Issue)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Notes: SourceExpansionNotes(compound, remediationItems, reviewSourceExpansionLimit, remaining),
            SuggestedResearchDirectives: SuggestedSourceExpansionDirectives(compound, remediationItems, reviewSourceExpansionLimit, remaining),
            TargetEvidencePath: NormalizePath(Path.Combine(targetEvidenceDirectory, $"{slug}.evidence.json")),
            RequiredSchema: EvidencePacketSchema,
            RemediationPlanItemIds: remediationPlanItemIds,
            RemediationResolutionTypes: remediationResolutionTypes,
            RemediationRecommendedActions: remediationRecommendedActions,
            RelatedReviewQueueItemIds: relatedReviewQueueItemIds);
    }

    private static IReadOnlyList<ReviewResolutionPlanItem> ScopedRemediationItems(
        ResearchSummaryCompound compound,
        IReadOnlyList<ReviewResolutionPlanItem> remediationItems)
    {
        if (compound.RequestedRemediationPlanItemIds.Count == 0) return remediationItems;
        var requested = new HashSet<string>(compound.RequestedRemediationPlanItemIds, StringComparer.OrdinalIgnoreCase);
        var scoped = remediationItems.Where(item => requested.Contains(item.ItemId)).ToList();
        return scoped.Count > 0 ? scoped : remediationItems;
    }

    private static bool ShouldEmitSourceExpansionTask(ResearchSummaryCompound compound, int reviewSourceExpansionLimit)
    {
        if (reviewSourceExpansionLimit <= 0) return false;
        if (compound.SourceFamilies.Count >= reviewSourceExpansionLimit) return false;
        if (!compound.PromotionReadiness.Equals("review-required", StringComparison.OrdinalIgnoreCase)
            && !compound.PromotionReadiness.Equals("blocked", StringComparison.OrdinalIgnoreCase)) return false;

        return IsPartialReviewState(compound) || HasSourceExpansionSignal(compound);
    }

    private static bool IsPartialReviewState(ResearchSummaryCompound compound)
        => compound.Completeness.Equals("minimal", StringComparison.OrdinalIgnoreCase)
           || compound.Completeness.Equals("partial", StringComparison.OrdinalIgnoreCase)
           || compound.OverallEvidenceTier.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
           || compound.OverallEvidenceTier.Equals("Insufficient", StringComparison.OrdinalIgnoreCase)
           || compound.OverallEvidenceTier.Equals("Limited", StringComparison.OrdinalIgnoreCase)
           || compound.HasRequestedChanges;

    private static bool HasSourceExpansionSignal(ResearchSummaryCompound compound)
        => compound.QualityFlags.Any(flag => flag.Equals("missing-authoritative-support", StringComparison.OrdinalIgnoreCase)
                                             || flag.Equals("source-registry-field-mismatch", StringComparison.OrdinalIgnoreCase)
                                             || flag.Equals("source-registry-unmapped-source", StringComparison.OrdinalIgnoreCase))
           || compound.ReviewReasons.Any(reason => reason.Contains("requires authoritative", StringComparison.OrdinalIgnoreCase)
                                                   || reason.Contains("source-registry", StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> SuggestedSourceExpansionDirectives(
        ResearchSummaryCompound compound,
        IReadOnlyList<ReviewResolutionPlanItem> remediationItems,
        int reviewSourceExpansionLimit,
        int remaining)
    {
        var currentFamilies = compound.SourceFamilies.Count == 0
            ? "none recorded"
            : string.Join(", ", compound.SourceFamilies);
        var remediationIds = remediationItems.Count == 0
            ? "none recorded"
            : string.Join(", ", remediationItems.Select(item => item.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
        var directives = new List<string>
        {
            $"Perform automatic review-source expansion for {compound.Name} as a continuation of remediation plan item(s): {remediationIds}.",
            $"Update the compound evidence packet matching {EvidencePacketSchema} while preserving unresolved remediation context.",
            $"Current independent source families: {currentFamilies}. Configured review source expansion limit: {reviewSourceExpansionLimit}; seek up to {remaining} additional materially different source family/families before human-only review.",
            "Do not clear review by rereading source families already present in the packet; add independent corroboration or preserve the review gate.",
            "Prefer regulator/label, clinical guideline/professional society, systematic review/meta-analysis, controlled human study, paper-level primary literature, or structured authority database sources as appropriate to the claim.",
            "If the expansion limit is reached or no independent corroboration is found, keep ops.needsReview = true and record the remaining gap as partially complete and needing human review.",
            "Do not fabricate evidence; leave unsupported claims out of the packet until a source-backed record exists."
        };

        directives.AddRange(remediationItems.Select(item => $"Original remediation action ({item.ItemId}, {item.ResolutionType}): {item.RecommendedAction}"));
        directives.AddRange(compound.PromotionBlockers.Select(blocker => $"Promotion blocker to resolve if independently source-backed: {blocker}"));
        return directives
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> SourceExpansionNotes(
        ResearchSummaryCompound compound,
        IReadOnlyList<ReviewResolutionPlanItem> remediationItems,
        int reviewSourceExpansionLimit,
        int remaining)
    {
        var notes = new List<string>
        {
            $"Automatic source-expansion fallback: {compound.SourceFamilies.Count}/{reviewSourceExpansionLimit} independent source families observed; seek up to {remaining} additional family/families before human-only review."
        };
        notes.AddRange(remediationItems.Select(item => $"Continue remediation item {item.ItemId}: {item.RecommendedAction}"));
        return notes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static ResearchTaskQueueItem? ToItem(
        ResearchSummaryCompound compound,
        IReadOnlyList<ResearchRequestInfo> requests,
        string targetEvidenceDirectory,
        IReadOnlyList<ReviewResolutionPlanItem> remediationItems)
    {
        if (requests.Count == 0) return null;

        var slug = SubstanceRecordNormalizer.Slugify(compound.Name);
        if (slug.Length == 0) return null;

        var ordered = requests
            .OrderBy(request => request.RequestedAt)
            .ThenBy(request => request.RequestId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var latest = ordered[^1];
        var priority = ordered
            .Select(request => NormalizePriority(request.Priority))
            .OrderBy(PriorityRank)
            .First();
        var aliases = ordered
            .SelectMany(request => request.Aliases)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categories = AggregateCategories(ordered);
        var requestIds = ordered
            .Select(request => request.RequestId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var requesterIds = ordered
            .Select(request => request.RequesterId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var rationales = ordered
            .Select(request => request.Rationale)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var notes = ordered
            .SelectMany(request => request.Notes)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        remediationItems = ScopedRemediationItems(compound, remediationItems);
        notes.AddRange(remediationItems.Select(item => $"Continue remediation item {item.ItemId}: {item.RecommendedAction}"));
        notes = notes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var remediationPlanItemIds = remediationItems.Select(item => item.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        var remediationResolutionTypes = remediationItems.Select(item => item.ResolutionType).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
        var remediationRecommendedActions = remediationItems.Select(item => item.RecommendedAction).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
        var relatedReviewQueueItemIds = remediationItems.SelectMany(item => item.RelatedReviewQueueItemIds).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();

        return new ResearchTaskQueueItem(
            TaskId: $"{slug}-initial-research",
            TaskType: "generate-evidence-packet",
            CompoundName: compound.Name,
            Aliases: aliases,
            Categories: categories,
            Classification: compound.Classification,
            Priority: priority,
            RequestIds: requestIds,
            RequesterIds: requesterIds,
            FirstRequestedAtUtc: ordered[0].RequestedAt,
            LatestRequestedAtUtc: latest.RequestedAt,
            Rationales: rationales.Concat(remediationItems.Select(item => item.Issue)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Notes: notes,
            SuggestedResearchDirectives: SuggestedResearchDirectives(compound.Name, categories, latest.Rationale, notes)
                .Concat(remediationItems.Select(item => $"Original remediation action ({item.ItemId}, {item.ResolutionType}): {item.RecommendedAction}"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            TargetEvidencePath: NormalizePath(Path.Combine(targetEvidenceDirectory, $"{slug}.evidence.json")),
            RequiredSchema: EvidencePacketSchema,
            RemediationPlanItemIds: remediationPlanItemIds,
            RemediationResolutionTypes: remediationResolutionTypes,
            RemediationRecommendedActions: remediationRecommendedActions,
            RelatedReviewQueueItemIds: relatedReviewQueueItemIds);
    }

    private static ResearchTaskQueueResolvedItem? ToResolvedItem(
        string compoundName,
        IReadOnlyList<ResearchRequestInfo> requests,
        IReadOnlyDictionary<string, ResearchSummaryCompound> compoundsByName,
        DateTimeOffset generatedAtUtc,
        string targetEvidenceDirectory)
    {
        if (!compoundsByName.TryGetValue(compoundName, out var compound)) return null;
        if (!compound.HasResearchRequest) return null;
        if (compound.PromotionReadiness.Equals("research-requested", StringComparison.OrdinalIgnoreCase)) return null;

        var ordered = requests
            .OrderBy(request => request.RequestedAt)
            .ThenBy(request => request.RequestId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ordered.Count == 0) return null;

        var slug = SubstanceRecordNormalizer.Slugify(compoundName);
        if (slug.Length == 0) return null;

        var aliases = ordered
            .SelectMany(request => request.Aliases)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categories = AggregateCategories(ordered);
        var requestIds = ordered
            .Select(request => request.RequestId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var priority = ordered
            .Select(request => NormalizePriority(request.Priority))
            .OrderBy(PriorityRank)
            .First();
        var requesterIds = ordered
            .Select(request => request.RequesterId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResearchTaskQueueResolvedItem(
            TaskId: $"{slug}-initial-research",
            CompoundName: compoundName,
            Aliases: aliases,
            Categories: categories,
            Classification: compound.Classification,
            Priority: priority,
            RequestIds: requestIds,
            RequesterIds: requesterIds,
            FirstRequestedAtUtc: ordered[0].RequestedAt,
            LatestRequestedAtUtc: ordered[^1].RequestedAt,
            ResolvedAtUtc: generatedAtUtc,
            CurrentReadiness: compound.PromotionReadiness,
            Resolution: "evidence-detected",
            ResolutionReason: $"Initial research request consumed on this run because pipeline evidence now exists and the compound moved to '{compound.PromotionReadiness}'.",
            TargetEvidencePath: NormalizePath(Path.Combine(targetEvidenceDirectory, $"{slug}.evidence.json")));
    }

    private static IReadOnlyList<string> SuggestedResearchDirectives(
        string compoundName,
        IReadOnlyList<string> categories,
        string latestRationale,
        IReadOnlyList<string> notes)
    {
        var directives = new List<string>
        {
            $"Generate a compound evidence packet for {compoundName} that matches {EvidencePacketSchema}.",
            "Use research/directives/02-source-registry-agent.md to validate or extend source coverage before claim extraction.",
            "Use research/directives/03-category-evidence-agent.md to extract claim-level evidence while preserving route, formulation, alias, and regulatory distinctions.",
            "Use research/directives/04-preprocess-compile-review.md after packet generation so the next worker pass can preprocess, compile, and review the result.",
            "For any review or requested-change work, verify claims against materially different source families than the original evidence sources; do not clear review by rereading the same source.",
            "Do not fabricate evidence; leave unsupported claims out of the packet until a source-backed record exists."
        };

        if (categories.Count > 0)
        {
            directives.Add($"Treat the operator categories as routing hints: {string.Join(", ", categories)}.");
        }

        if (!string.IsNullOrWhiteSpace(latestRationale))
        {
            directives.Add($"Prioritize the operator rationale: {latestRationale.Trim()}");
        }

        directives.AddRange(notes.Select(note => $"Operator note: {note.Trim()}"));

        return directives
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> AggregateCategories(IReadOnlyList<ResearchRequestInfo> requests)
        => requests
            .SelectMany(request => request.Categories)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string NormalizePriority(string priority)
        => priority.Trim().ToLowerInvariant() switch
        {
            "urgent" => "urgent",
            "high" => "high",
            "low" => "low",
            _ => "normal"
        };

    private static int PriorityRank(string priority)
        => NormalizePriority(priority) switch
        {
            "urgent" => 0,
            "high" => 1,
            "normal" => 2,
            "low" => 3,
            _ => 2
        };

    private static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimEnd('/');
}