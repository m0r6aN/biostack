namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;

public sealed class CounterfactualCandidateService : ICounterfactualCandidateService
{
    private readonly IKnowledgeSource _knowledgeSource;

    public CounterfactualCandidateService(IKnowledgeSource knowledgeSource)
    {
        _knowledgeSource = knowledgeSource;
    }

    public async Task<List<ProtocolCandidate>> GetSwapCandidatesAsync(
        KnowledgeEntry target,
        IReadOnlyList<KnowledgeEntry> currentProtocol,
        OptimizationContext context,
        CancellationToken cancellationToken = default)
    {
        var pool = await _knowledgeSource.GetAllCompoundsAsync(cancellationToken);
        var currentNames = currentProtocol.Select(entry => entry.CanonicalName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentPathways = currentProtocol.SelectMany(entry => entry.Pathways).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return pool
            .Where(candidate => !currentNames.Contains(candidate.CanonicalName))
            .Where(candidate => !context.ExcludedCompoundIds.Any(item => string.Equals(item, candidate.CanonicalName, StringComparison.OrdinalIgnoreCase)))
            .Where(candidate => candidate.Classification == target.Classification || SharedPathwayCount(candidate, target) > 0)
            .Where(candidate => !HasExplicitConflict(candidate, currentProtocol))
            .Select(candidate => new ProtocolCandidate(
                candidate.CanonicalName,
                candidate,
                GoalAlignment(candidate, context.Goal),
                PathwaySimilarity(candidate, target, currentPathways),
                BuildReason(candidate, target, context.Goal)))
            .OrderByDescending(candidate => candidate.GoalAlignmentScore + candidate.PathwaySimilarityScore)
            .ThenByDescending(candidate => candidate.KnowledgeEntry.EvidenceTier)
            .Take(8)
            .ToList();
    }

    public async Task<List<ProtocolCandidate>> GetGoalCandidatesAsync(
        string goal,
        IReadOnlyList<KnowledgeEntry> currentProtocol,
        OptimizationContext context,
        CancellationToken cancellationToken = default)
    {
        var pool = await _knowledgeSource.GetAllCompoundsAsync(cancellationToken);
        var currentNames = currentProtocol.Select(entry => entry.CanonicalName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return pool
            .Where(candidate => !currentNames.Contains(candidate.CanonicalName))
            .Where(candidate => !context.ExcludedCompoundIds.Any(item => string.Equals(item, candidate.CanonicalName, StringComparison.OrdinalIgnoreCase)))
            .Where(candidate => GoalAlignment(candidate, goal) > 0.2d)
            .Select(candidate => new ProtocolCandidate(
                candidate.CanonicalName,
                candidate,
                GoalAlignment(candidate, goal),
                0d,
                $"Stronger fit for {goal}."))
            .OrderByDescending(candidate => candidate.GoalAlignmentScore)
            .ThenByDescending(candidate => candidate.KnowledgeEntry.EvidenceTier)
            .Take(10)
            .ToList();
    }

    private static int SharedPathwayCount(KnowledgeEntry left, KnowledgeEntry right)
    {
        return left.Pathways.Intersect(right.Pathways, StringComparer.OrdinalIgnoreCase).Count();
    }

    private static double PathwaySimilarity(KnowledgeEntry candidate, KnowledgeEntry target, HashSet<string> currentPathways)
    {
        var sharedWithTarget = SharedPathwayCount(candidate, target);
        var sharedWithProtocol = candidate.Pathways.Count(pathway => currentPathways.Contains(pathway));
        return Math.Round((sharedWithTarget * 0.6d) + (sharedWithProtocol * 0.2d), 2);
    }

    private static bool HasExplicitConflict(KnowledgeEntry candidate, IReadOnlyList<KnowledgeEntry> currentProtocol)
    {
        return currentProtocol.Any(existing =>
            candidate.AvoidWith.Any(item => item.Contains(existing.CanonicalName, StringComparison.OrdinalIgnoreCase))
            || existing.AvoidWith.Any(item => item.Contains(candidate.CanonicalName, StringComparison.OrdinalIgnoreCase)));
    }

    private static double GoalAlignment(KnowledgeEntry candidate, string goal)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            return 0d;
        }

        var normalizedGoal = goal.Trim().ToLowerInvariant();
        var goalTokens = normalizedGoal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var benefitHits = candidate.Benefits.Count(benefit => goalTokens.Any(token => benefit.Contains(token, StringComparison.OrdinalIgnoreCase)));
        var pathwayHits = candidate.Pathways.Count(pathway => goalTokens.Any(token => pathway.Contains(token, StringComparison.OrdinalIgnoreCase)));
        var mechanismHits = goalTokens.Count(token => candidate.MechanismSummary.Contains(token, StringComparison.OrdinalIgnoreCase));
        return Math.Round((benefitHits * 0.5d) + (pathwayHits * 0.35d) + (mechanismHits * 0.15d), 2);
    }

    private static string BuildReason(KnowledgeEntry candidate, KnowledgeEntry target, string goal)
    {
        if (!string.IsNullOrWhiteSpace(goal) && GoalAlignment(candidate, goal) > 0.5d)
        {
            return $"Stronger goal alignment for {goal}.";
        }

        if (candidate.Classification == target.Classification)
        {
            return $"Same {target.Classification} category with a cleaner fit.";
        }

        return "Related pathway coverage without duplicating the same slot.";
    }
}

public interface ICounterfactualCandidateService
{
    Task<List<ProtocolCandidate>> GetSwapCandidatesAsync(
        KnowledgeEntry target,
        IReadOnlyList<KnowledgeEntry> currentProtocol,
        OptimizationContext context,
        CancellationToken cancellationToken = default);

    Task<List<ProtocolCandidate>> GetGoalCandidatesAsync(
        string goal,
        IReadOnlyList<KnowledgeEntry> currentProtocol,
        OptimizationContext context,
        CancellationToken cancellationToken = default);
}
