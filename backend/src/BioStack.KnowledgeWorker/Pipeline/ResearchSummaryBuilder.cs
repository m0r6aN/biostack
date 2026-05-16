namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public sealed record ResearchSummary(
    int DraftSubstanceCount,
    int ReviewQueueItemCount,
    int ResearchRequestCount,
    IReadOnlyList<ResearchSummaryCompound> Compounds,
    IReadOnlyList<ResearchReviewCategory> ReviewCategories,
    IReadOnlyList<ResearchSummaryBucket> PromotionReadiness,
    IReadOnlyList<ResearchSummaryBucket> QualityFlags,
    IReadOnlyList<ResearchSummaryBucket> ReviewReasons,
    IReadOnlyList<ResearchSummaryBucket> Classifications,
    IReadOnlyList<ResearchSummaryBucket> EvidenceTiers);

public sealed record ResearchSummaryCompound(
    string Name,
    string Classification,
    string OverallEvidenceTier,
    string Completeness,
    bool NeedsReview,
    int ReviewQueueItemCount,
    string PromotionReadiness,
    IReadOnlyList<string> PromotionBlockers,
    IReadOnlyList<string> ReviewDecisionIds,
    bool HasRequestedChanges,
    bool HasResearchRequest,
    IReadOnlyList<string> ResearchRequestIds,
    IReadOnlyList<string> RequestedRemediationPlanItemIds,
    IReadOnlyList<string> SourceFamilies,
    IReadOnlyList<string> QualityFlags,
    IReadOnlyList<string> ReviewReasons);

public sealed record ResearchSummaryBucket(string Name, int Count, IReadOnlyList<string> Compounds);

public sealed record ResearchReviewCategory(
    string Name,
    int Count,
    IReadOnlyList<string> Compounds,
    IReadOnlyList<string> Signals,
    IReadOnlyList<string> RecommendedActions);

public interface IResearchSummaryBuilder
{
    ResearchSummary Build(JsonArray draftSubstances, IReadOnlyList<ResearchReviewQueueItem> reviewQueue);
    ResearchSummary Build(JsonArray draftSubstances, IReadOnlyList<ResearchReviewQueueItem> reviewQueue, ReviewDecisionIndex reviewDecisions);
    ResearchSummary Build(JsonArray draftSubstances, IReadOnlyList<ResearchReviewQueueItem> reviewQueue, ReviewDecisionIndex reviewDecisions, ResearchRequestIndex researchRequests);
}

public sealed class ResearchSummaryBuilder : IResearchSummaryBuilder
{
    public ResearchSummary Build(JsonArray draftSubstances, IReadOnlyList<ResearchReviewQueueItem> reviewQueue)
        => Build(draftSubstances, reviewQueue, ReviewDecisionIndex.Empty);

    public ResearchSummary Build(JsonArray draftSubstances, IReadOnlyList<ResearchReviewQueueItem> reviewQueue, ReviewDecisionIndex reviewDecisions)
        => Build(draftSubstances, reviewQueue, reviewDecisions, ResearchRequestIndex.Empty);

    public ResearchSummary Build(JsonArray draftSubstances, IReadOnlyList<ResearchReviewQueueItem> reviewQueue, ReviewDecisionIndex reviewDecisions, ResearchRequestIndex researchRequests)
    {
        var activeReviewQueue = reviewQueue
            .Where(item => !reviewDecisions.IsCompoundArchived(item.CompoundName))
            .Where(item => !reviewDecisions.IsReviewQueueItemResolved(item.CompoundName, item.ItemId))
            .ToList();

        var reviewCounts = activeReviewQueue
            .GroupBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var compounds = draftSubstances
            .Where(node => node is not null)
            .Where(node => !reviewDecisions.IsCompoundArchived(ReadString(node?["identity"]?["canonicalName"])))
            .Select(node => ToCompound(node, reviewCounts, reviewDecisions, researchRequests))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var draftSubstanceCount = compounds.Count;

        AddPendingResearchRequests(compounds, researchRequests, reviewDecisions);
        compounds = compounds.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

        return new ResearchSummary(
            DraftSubstanceCount: draftSubstanceCount,
            ReviewQueueItemCount: activeReviewQueue.Count,
            ResearchRequestCount: compounds.Count(c => c.PromotionReadiness == "research-requested"),
            Compounds: compounds,
            ReviewCategories: Classify(compounds),
            PromotionReadiness: Bucket(compounds, c => new[] { c.PromotionReadiness }),
            QualityFlags: Bucket(compounds, c => c.QualityFlags),
            ReviewReasons: Bucket(compounds, c => c.ReviewReasons),
            Classifications: Bucket(compounds, c => new[] { c.Classification }),
            EvidenceTiers: Bucket(compounds, c => new[] { c.OverallEvidenceTier }));
    }

    private static List<ResearchReviewCategory> Classify(IReadOnlyList<ResearchSummaryCompound> compounds)
    {
        var categories = new Dictionary<string, List<(string Compound, string Signal)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var compound in compounds)
        {
            AddWhen(categories, "Regulatory / Approval", compound, HasAny(compound,
                "regulatory-boundary", "regulatory-ambiguity", "investigational-compound",
                "no-approved-label-source", "a1-label-backed", "fda-warning"));
            AddWhen(categories, "Safety Critical", compound, HasAny(compound,
                "no-safety-canonicalization", "fda-warning", "hormone-axis-boundary",
                "prescription-hormone-context", "missing-authoritative-support"));
            AddWhen(categories, "Misinformation / Vendor Claims", compound, HasAny(compound,
                "misinformation-heavy", "hype-boundary", "performance-use-boundary"));
            AddWhen(categories, "Weak or Emerging Human Evidence", compound,
                HasAny(compound, "limited-human-evidence", "emerging-longevity-compound", "investigational-compound")
                || compound.OverallEvidenceTier.Equals("Insufficient", StringComparison.OrdinalIgnoreCase)
                || compound.OverallEvidenceTier.Equals("Limited", StringComparison.OrdinalIgnoreCase));
            AddWhen(categories, "Route / Formulation Ambiguity", compound, HasAny(compound,
                "route-specific-boundary", "alias-ambiguity", "product-specific-labels"));
            AddWhen(categories, "Product-Specific Label", compound, HasAny(compound,
                "product-specific-labels", "a1-label-backed"));
            AddWhen(categories, "Sports / Doping", compound, HasAny(compound,
                "sports-prohibited-boundary", "performance-use-boundary"));
            AddWhen(categories, "Source Registry Authorization", compound, HasAny(compound,
                "source-registry-field-mismatch", "source-registry-unmapped-source"));
            AddWhen(categories, "Requested Changes", compound, compound.HasRequestedChanges);
            AddWhen(categories, "Research Requested", compound, compound.HasResearchRequest && compound.PromotionReadiness == "research-requested");
        }

        return categories
            .Select(pair => new ResearchReviewCategory(
                Name: pair.Key,
                Count: pair.Value.Select(x => x.Compound).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Compounds: pair.Value.Select(x => x.Compound)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Signals: pair.Value.Select(x => x.Signal)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(signal => signal, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                RecommendedActions: RecommendedActionsFor(pair.Key)))
            .OrderByDescending(category => category.Count)
            .ThenBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> RecommendedActionsFor(string category) => category switch
    {
        "Regulatory / Approval" => new[]
        {
            "Verify jurisdiction-specific status against authoritative regulator or product-label sources.",
            "Keep investigational, compounded, off-label, and approved-product contexts separate.",
            "Do not publish approved-use language unless the exact product/use case is source-backed."
        },
        "Safety Critical" => new[]
        {
            "Require A1/A2 support for contraindications, warnings, adverse effects, monitoring, and safety-critical fields.",
            "Keep safety fields empty, unknown, or review-required until authoritative support is attached.",
            "Escalate prescription, hormone-axis, GLP-1, SARM, peptide, and severe-risk claims for human review."
        },
        "Misinformation / Vendor Claims" => new[]
        {
            "Treat social, vendor, and community claims as popularity or misinformation signals only.",
            "Attach a misinformation-monitoring source or reclassify unsupported claims as evidence gaps.",
            "Use cautious customer-facing language that separates claimed uses from supported evidence."
        },
        "Weak or Emerging Human Evidence" => new[]
        {
            "Prevent benefit overstatement; summarize the studied population, endpoint, and evidence limitations.",
            "Separate preclinical, biomarker, phase 2, and observational evidence from proven human outcomes.",
            "Keep emerging compounds review-gated until higher-quality human evidence or regulator sources are added."
        },
        "Route / Formulation Ambiguity" => new[]
        {
            "Split route-specific, formulation-specific, ester/salt, product, and alias-specific claims before publication.",
            "Do not generalize topical, oral, injectable, compounded, or vendor-product evidence across routes.",
            "Confirm canonical identity and aliases before merging records or stack rules."
        },
        "Product-Specific Label" => new[]
        {
            "Verify the latest product label before publication.",
            "Attach product-specific provenance to approved indications, product dosing, storage, and contraindication fields.",
            "Avoid applying one brand or formulation label to other brands, compounded products, or research products."
        },
        "Sports / Doping" => new[]
        {
            "Check the current WADA list and any sport, league, employer, or jurisdiction-specific policy.",
            "Separate anti-doping status from medical safety or clinical efficacy claims.",
            "Flag performance-enhancement contexts for legal, safety, and policy review."
        },
        "Source Registry Authorization" => new[]
        {
            "Add a source authorized for the claim's required field use or change the claim type.",
            "If the source is valid but unmapped, add a source-family mapping or source registry entry.",
            "Do not compile unsupported claims into canonical fields until the authorization mismatch is resolved."
        },
        "Requested Changes" => new[]
        {
            "Re-run targeted research against the reviewer notes and changed evidence packet.",
            "Confirm the requested changes are addressed before approving promotion.",
            "Keep the draft in re-review until a later approval, archive, or reject decision closes the request."
        },
        "Research Requested" => new[]
        {
            "Create an evidence packet for the requested compound and save it under research/input/evidence.",
            "Run the evidence agent/directive pass before attempting promotion review.",
            "Once evidence exists, the compound enters the normal review and promotion readiness flow."
        },
        _ => new[] { "Review source provenance, claim type, and customer-facing language before publication." }
    };

    private static bool HasAny(ResearchSummaryCompound compound, params string[] signals)
    {
        var set = new HashSet<string>(signals, StringComparer.OrdinalIgnoreCase);
        return compound.QualityFlags.Any(set.Contains)
               || compound.ReviewReasons.Any(reason => set.Any(signal => reason.Contains(signal, StringComparison.OrdinalIgnoreCase)));
    }

    private static void AddWhen(
        Dictionary<string, List<(string Compound, string Signal)>> categories,
        string category,
        ResearchSummaryCompound compound,
        bool condition)
    {
        if (!condition) return;
        if (!categories.TryGetValue(category, out var list))
        {
            list = new List<(string Compound, string Signal)>();
            categories[category] = list;
        }

        var signals = compound.QualityFlags.Concat(compound.ReviewReasons)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .DefaultIfEmpty(category);
        foreach (var signal in signals)
        {
            list.Add((compound.Name, signal));
        }
    }

    private static ResearchSummaryCompound ToCompound(
        JsonNode? draftNode,
        IReadOnlyDictionary<string, int> reviewQueueCounts,
        ReviewDecisionIndex reviewDecisions,
        ResearchRequestIndex researchRequests)
    {
        var identity = draftNode?["identity"];
        var ops = draftNode?["ops"];
        var evidence = draftNode?["evidence"];
        var provenance = draftNode?["provenance"];
        var name = ReadString(identity?["canonicalName"]);
        var qualityFlags = ReadStringArray(ops?["qualityFlags"]);
        var reviewReasons = ReadStringArray(ops?["reviewReasons"]);
        var sourceFamilies = ReadSourceFamilies(provenance?["sourceRecords"]);
        var reviewQueueItemCount = reviewQueueCounts.TryGetValue(name, out var count) ? count : 0;
        var decisions = reviewDecisions.ForCompound(name);
        var hasPromotionApproval = reviewDecisions.HasPromotionApproval(name);
        var hasRequestedChanges = reviewDecisions.HasPendingRequestedChanges(name);
        var requests = researchRequests.ForCompound(name);
        var blockers = PromotionBlockers(
            qualityFlags,
            reviewReasons,
            ReadString(evidence?["overallTier"]),
            ReadString(ops?["completeness"]),
            ReadBool(ops?["needsReview"]),
            reviewQueueItemCount,
            hasPromotionApproval,
            hasRequestedChanges);

        return new ResearchSummaryCompound(
            Name: name,
            Classification: ReadString(identity?["classification"]),
            OverallEvidenceTier: ReadString(evidence?["overallTier"]),
            Completeness: ReadString(ops?["completeness"]),
            NeedsReview: ReadBool(ops?["needsReview"]),
            ReviewQueueItemCount: reviewQueueItemCount,
            PromotionReadiness: DeterminePromotionReadiness(blockers),
            PromotionBlockers: blockers,
            ReviewDecisionIds: decisions.Select(d => d.DecisionId).Where(id => id.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            HasRequestedChanges: hasRequestedChanges,
            HasResearchRequest: requests.Count > 0,
            ResearchRequestIds: requests.Select(r => r.RequestId).Where(id => id.Length > 0).ToList(),
            RequestedRemediationPlanItemIds: reviewDecisions.PendingRequestedRemediationPlanItemIds(name),
            SourceFamilies: sourceFamilies,
            QualityFlags: qualityFlags,
            ReviewReasons: reviewReasons);
    }

    private static void AddPendingResearchRequests(
        List<ResearchSummaryCompound> compounds,
        ResearchRequestIndex researchRequests,
        ReviewDecisionIndex reviewDecisions)
    {
        var existing = new HashSet<string>(compounds.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var group in researchRequests.All().GroupBy(r => r.CompoundName, StringComparer.OrdinalIgnoreCase))
        {
            var name = group.Key;
            if (existing.Contains(name) || reviewDecisions.IsCompoundArchived(name)) continue;
            var requests = group.OrderByDescending(r => r.RequestedAt).ToList();
            var latest = requests[0];
            var decisions = reviewDecisions.ForCompound(name);
            var requestedRemediationPlanItemIds = reviewDecisions.PendingRequestedRemediationPlanItemIds(name);
            compounds.Add(new ResearchSummaryCompound(
                Name: name,
                Classification: latest.Classification.Length > 0 ? latest.Classification : "Unknown",
                OverallEvidenceTier: "Unknown",
                Completeness: "requested",
                NeedsReview: true,
                ReviewQueueItemCount: 0,
                PromotionReadiness: "research-requested",
                PromotionBlockers: new[] { "research-requested: evidence packet has not been generated" },
                ReviewDecisionIds: decisions.Select(d => d.DecisionId).Where(id => id.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                HasRequestedChanges: requestedRemediationPlanItemIds.Count > 0 || reviewDecisions.HasPendingRequestedChanges(name),
                HasResearchRequest: true,
                ResearchRequestIds: requests.Select(r => r.RequestId).Where(id => id.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                RequestedRemediationPlanItemIds: requestedRemediationPlanItemIds,
                SourceFamilies: Array.Empty<string>(),
                QualityFlags: new[] { "research-requested" },
                ReviewReasons: requests.Select(r => r.Rationale.Length > 0 ? $"Research requested: {r.Rationale}" : "Research requested for new compound.")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()));
            existing.Add(name);
        }
    }

    private static string DeterminePromotionReadiness(IReadOnlyList<string> blockers)
    {
        if (blockers.Any(b => b.StartsWith("blocked:", StringComparison.OrdinalIgnoreCase))) return "blocked";
        if (blockers.Count > 0) return "review-required";
        return "candidate-for-promotion";
    }

    private static IReadOnlyList<string> PromotionBlockers(
        IReadOnlyList<string> qualityFlags,
        IReadOnlyList<string> reviewReasons,
        string evidenceTier,
        string completeness,
        bool needsReview,
        int reviewQueueItemCount,
        bool hasPromotionApproval,
        bool hasRequestedChanges)
    {
        var blockers = new List<string>();
        if (qualityFlags.Any(flag => flag.Equals("source-registry-field-mismatch", StringComparison.OrdinalIgnoreCase)
                                    || flag.Equals("source-registry-unmapped-source", StringComparison.OrdinalIgnoreCase)))
        {
            blockers.Add("blocked: unresolved source-registry authorization issue");
        }
        if (qualityFlags.Any(flag => flag.Equals("missing-authoritative-support", StringComparison.OrdinalIgnoreCase))
            || reviewReasons.Any(reason => reason.Contains("requires authoritative", StringComparison.OrdinalIgnoreCase)))
        {
            blockers.Add("blocked: missing required authoritative support");
        }
        if (hasRequestedChanges)
        {
            blockers.Add("review-required: requested changes pending re-review");
        }
        if (!hasPromotionApproval)
        {
            if (needsReview) blockers.Add("review-required: draft is marked needsReview");
            if (reviewQueueItemCount > 0) blockers.Add($"review-required: {reviewQueueItemCount} review queue item(s)");
            if (completeness.Equals("minimal", StringComparison.OrdinalIgnoreCase)
                || completeness.Equals("partial", StringComparison.OrdinalIgnoreCase))
            {
                blockers.Add($"review-required: completeness is {completeness}");
            }
            if (evidenceTier.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                || evidenceTier.Equals("Insufficient", StringComparison.OrdinalIgnoreCase)
                || evidenceTier.Equals("Limited", StringComparison.OrdinalIgnoreCase))
            {
                blockers.Add($"review-required: evidence tier is {evidenceTier}");
            }
        }

        return blockers.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<ResearchSummaryBucket> Bucket(
        IReadOnlyList<ResearchSummaryCompound> compounds,
        Func<ResearchSummaryCompound, IEnumerable<string>> selector)
        => compounds
            .SelectMany(compound => selector(compound)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new { Value = value.Trim(), compound.Name }))
            .GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ResearchSummaryBucket(
                Name: group.Key,
                Count: group.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Compounds: group.Select(x => x.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderByDescending(bucket => bucket.Count)
            .ThenBy(bucket => bucket.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string ReadString(JsonNode? node)
        => node?.GetValue<string>()?.Trim() ?? string.Empty;

    private static bool ReadBool(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<bool>(out var result) && result;

    private static List<string> ReadStringArray(JsonNode? node)
    {
        if (node is not JsonArray arr) return new List<string>();
        return arr.Select(item => item?.GetValue<string>()?.Trim() ?? string.Empty)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ReadSourceFamilies(JsonNode? node)
    {
        if (node is not JsonArray arr) return new List<string>();
        return arr.Select(item => ReadString(item?["sourceType"]))
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}