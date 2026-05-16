namespace BioStack.KnowledgeWorker.Pipeline;

public sealed record PromotionManifest(
    string ManifestVersion,
    DateTimeOffset GeneratedAtUtc,
    PromotionManifestCounts Counts,
    PromotionManifestOutputs Outputs,
    IReadOnlyList<PromotionManifestCandidate> Blocked,
    IReadOnlyList<PromotionManifestCandidate> ReviewRequired,
    IReadOnlyList<PromotionManifestCandidate> ResearchRequested,
    IReadOnlyList<PromotionManifestCandidate> CandidatesForPromotion);

public sealed record PromotionManifestCounts(
    int TotalDrafts,
    int Blocked,
    int ReviewRequired,
    int ResearchRequested,
    int CandidatesForPromotion);

public sealed record PromotionManifestOutputs(
    string DraftSubstances,
    string ReviewQueue,
    string ResearchSummary,
    string RunReport,
    string? ResearchTaskQueue = null,
    string? CompoundGraph = null);

public sealed record PromotionManifestCandidate(
    string Name,
    string Classification,
    string Readiness,
    string OverallEvidenceTier,
    string Completeness,
    int ReviewQueueItemCount,
    IReadOnlyList<string> ReviewDecisionIds,
    bool HasRequestedChanges,
    bool HasResearchRequest,
    IReadOnlyList<string> ResearchRequestIds,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> QualityFlags,
    IReadOnlyList<string> RequiredNextActions);

public interface IPromotionManifestBuilder
{
    PromotionManifest Build(ResearchSummary summary, PromotionManifestOutputs outputs);
}

public sealed class PromotionManifestBuilder : IPromotionManifestBuilder
{
    public PromotionManifest Build(ResearchSummary summary, PromotionManifestOutputs outputs)
    {
        if (summary is null) throw new ArgumentNullException(nameof(summary));
        if (outputs is null) throw new ArgumentNullException(nameof(outputs));

        var all = summary.Compounds.Select(c => ToCandidate(c, summary.ReviewCategories)).ToList();
        var blocked = all.Where(c => c.Readiness == "blocked").OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var reviewRequired = all.Where(c => c.Readiness == "review-required").OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var researchRequested = all.Where(c => c.Readiness == "research-requested").OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var candidates = all.Where(c => c.Readiness == "candidate-for-promotion").OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

        return new PromotionManifest(
            ManifestVersion: "1.0.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Counts: new PromotionManifestCounts(
                TotalDrafts: summary.DraftSubstanceCount,
                Blocked: blocked.Count,
                ReviewRequired: reviewRequired.Count,
                ResearchRequested: researchRequested.Count,
                CandidatesForPromotion: candidates.Count),
            Outputs: outputs,
            Blocked: blocked,
            ReviewRequired: reviewRequired,
            ResearchRequested: researchRequested,
            CandidatesForPromotion: candidates);
    }

    private static PromotionManifestCandidate ToCandidate(
        ResearchSummaryCompound compound,
        IReadOnlyList<ResearchReviewCategory> categories)
    {
        var actions = categories
            .Where(category => category.Compounds.Contains(compound.Name, StringComparer.OrdinalIgnoreCase))
            .SelectMany(category => category.RecommendedActions)
            .Concat(DefaultActions(compound))
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(action => action, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PromotionManifestCandidate(
            Name: compound.Name,
            Classification: compound.Classification,
            Readiness: compound.PromotionReadiness,
            OverallEvidenceTier: compound.OverallEvidenceTier,
            Completeness: compound.Completeness,
            ReviewQueueItemCount: compound.ReviewQueueItemCount,
            ReviewDecisionIds: compound.ReviewDecisionIds,
            HasRequestedChanges: compound.HasRequestedChanges,
            HasResearchRequest: compound.HasResearchRequest,
            ResearchRequestIds: compound.ResearchRequestIds,
            Blockers: compound.PromotionBlockers,
            QualityFlags: compound.QualityFlags,
            RequiredNextActions: actions);
    }

    private static IEnumerable<string> DefaultActions(ResearchSummaryCompound compound)
    {
        if (compound.PromotionReadiness == "blocked")
        {
            yield return "Resolve all blocked promotion blockers before review approval or import promotion.";
        }
        else if (compound.PromotionReadiness == "research-requested")
        {
            yield return "Create a compound evidence packet from the research request, then rerun the research worker so the item enters normal review.";
        }
        else if (compound.PromotionReadiness == "review-required")
        {
            if (compound.HasRequestedChanges)
            {
                yield return "Run targeted follow-up research for requested changes using independent source families not already used by the original claim, then send the updated draft back through human re-review.";
            }
            yield return "Complete human review with cross-source verification and clear promotion blockers before marking candidate-for-promotion.";
        }
        else
        {
            yield return "Eligible for promotion review; verify final provenance and schema output before import.";
        }
    }
}