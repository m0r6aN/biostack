namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;

internal static class SwapRecommendationEngine
{
    internal const double MinPathwayJaccard = 0.2d;
    internal const double MinSimilarityScore = 0.25d;
    internal const int MaxCandidatesPerOriginal = 5;
    internal const int MaxSwapsReturned = 3;
    internal const double MinDeltaForRecommendation = 2d;
    internal const double ImprovesThreshold = 3d;
    internal const double WorsensThreshold = -3d;

    internal delegate Task<InteractionIntelligenceResponse> VariantEvaluator(
        IReadOnlyList<KnowledgeEntry> entries,
        CancellationToken cancellationToken);

    public static async Task<List<InteractionSwapRecommendationResponse>> BuildSwapsAsync(
        IReadOnlyList<KnowledgeEntry> currentEntries,
        IReadOnlyList<KnowledgeEntry> candidatePool,
        double baselineCompositeScore,
        InteractionSummaryResponse baselineSummary,
        VariantEvaluator evaluateVariant,
        CancellationToken cancellationToken)
    {
        if (currentEntries.Count == 0 || candidatePool.Count == 0)
        {
            return new List<InteractionSwapRecommendationResponse>();
        }

        var stackNames = BuildNameSet(currentEntries);
        var recommendations = new List<InteractionSwapRecommendationResponse>();

        foreach (var original in currentEntries)
        {
            var remainingStack = currentEntries
                .Where(entry => !ReferenceEquals(entry, original))
                .ToList();

            var candidates = candidatePool
                .Where(candidate => IsEligibleCandidate(candidate, original, stackNames, remainingStack))
                .Select(candidate => new
                {
                    Entry = candidate,
                    Similarity = CalculateSimilarity(original, candidate)
                })
                .Where(scored => scored.Similarity >= MinSimilarityScore)
                .OrderByDescending(scored => scored.Similarity)
                .ThenBy(scored => scored.Entry.CanonicalName, StringComparer.OrdinalIgnoreCase)
                .Take(MaxCandidatesPerOriginal)
                .ToList();

            foreach (var scored in candidates)
            {
                var variantEntries = remainingStack.Append(scored.Entry).ToList();
                var variant = await evaluateVariant(variantEntries, cancellationToken);

                var deltaScore = Math.Round(variant.CompositeScore - baselineCompositeScore, 2);
                if (Math.Abs(deltaScore) < MinDeltaForRecommendation)
                {
                    continue;
                }

                var deltaPercent = Math.Round(CalculateDeltaPercent(deltaScore, baselineCompositeScore), 2);
                var verdict = ResolveVerdict(deltaScore);
                var reasons = DeriveReasons(original, scored.Entry, baselineSummary, variant.Summary, deltaScore);

                recommendations.Add(new InteractionSwapRecommendationResponse(
                    original.CanonicalName,
                    scored.Entry.CanonicalName,
                    baselineCompositeScore,
                    variant.CompositeScore,
                    deltaScore,
                    deltaPercent,
                    verdict,
                    reasons,
                    BuildRecommendation(original.CanonicalName, scored.Entry.CanonicalName, verdict, deltaPercent),
                    Math.Round(scored.Similarity, 2),
                    variant.Summary,
                    variant.TopFindings));
            }
        }

        return recommendations
            .OrderByDescending(swap => swap.DeltaScore)
            .ThenByDescending(swap => swap.SimilarityScore)
            .ThenBy(swap => swap.OriginalCompound, StringComparer.OrdinalIgnoreCase)
            .ThenBy(swap => swap.CandidateCompound, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSwapsReturned)
            .ToList();
    }

    private static HashSet<string> BuildNameSet(IReadOnlyList<KnowledgeEntry> entries)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            names.Add(entry.CanonicalName);
            foreach (var alias in entry.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    names.Add(alias.Trim());
                }
            }
        }
        return names;
    }

    private static bool IsEligibleCandidate(
        KnowledgeEntry candidate,
        KnowledgeEntry original,
        HashSet<string> stackNames,
        IReadOnlyList<KnowledgeEntry> remainingStack)
    {
        if (string.Equals(candidate.CanonicalName, original.CanonicalName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (stackNames.Contains(candidate.CanonicalName)
            || candidate.Aliases.Any(alias => !string.IsNullOrWhiteSpace(alias) && stackNames.Contains(alias.Trim())))
        {
            return false;
        }

        if (candidate.Classification != original.Classification || candidate.Classification == CompoundCategory.Unknown)
        {
            return false;
        }

        if (PathwayJaccard(candidate.Pathways, original.Pathways) < MinPathwayJaccard
            && !HasExplicitPeerSignal(candidate, original))
        {
            return false;
        }

        foreach (var remaining in remainingStack)
        {
            if (IntroducesInterference(candidate, remaining))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IntroducesInterference(KnowledgeEntry candidate, KnowledgeEntry peer)
    {
        if (NameMatches(candidate.AvoidWith, peer) || NameMatches(peer.AvoidWith, candidate))
        {
            return true;
        }

        if (MentionsByName(candidate.DrugInteractions, peer) || MentionsByName(peer.DrugInteractions, candidate))
        {
            return true;
        }

        return false;
    }

    private static bool HasExplicitPeerSignal(KnowledgeEntry candidate, KnowledgeEntry original)
    {
        return NameMatches(candidate.PairsWellWith, original)
            || NameMatches(original.PairsWellWith, candidate)
            || NameMatches(candidate.CompatibleBlends, original)
            || NameMatches(original.CompatibleBlends, candidate);
    }

    private static bool NameMatches(IEnumerable<string> candidates, KnowledgeEntry target)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var trimmed = candidate.Trim();
            if (string.Equals(trimmed, target.CanonicalName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (target.Aliases.Any(alias => string.Equals(trimmed, alias.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        return false;
    }

    private static bool MentionsByName(IEnumerable<string> candidates, KnowledgeEntry target)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (candidate.Contains(target.CanonicalName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (target.Aliases.Any(alias => !string.IsNullOrWhiteSpace(alias)
                && candidate.Contains(alias, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        return false;
    }

    private static double PathwayJaccard(List<string> left, List<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return 0d;
        }

        var leftSet = new HashSet<string>(left, StringComparer.OrdinalIgnoreCase);
        var rightSet = new HashSet<string>(right, StringComparer.OrdinalIgnoreCase);
        var intersection = leftSet.Intersect(rightSet, StringComparer.OrdinalIgnoreCase).Count();
        var union = leftSet.Union(rightSet, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 0d : (double)intersection / union;
    }

    private static double CalculateSimilarity(KnowledgeEntry original, KnowledgeEntry candidate)
    {
        var pathwayScore = PathwayJaccard(original.Pathways, candidate.Pathways);
        var classificationScore = original.Classification == candidate.Classification ? 1d : 0d;
        var evidenceScore = candidate.EvidenceTier == EvidenceTier.Unknown ? 0d : 0.5d
            + (EvidenceWeight(candidate.EvidenceTier) * 0.5d);
        var pairingScore = HasExplicitPeerSignal(candidate, original) ? 1d : 0d;

        return (0.5d * pathwayScore) + (0.2d * classificationScore) + (0.2d * evidenceScore) + (0.1d * pairingScore);
    }

    private static double EvidenceWeight(EvidenceTier tier) => tier switch
    {
        EvidenceTier.Strong => 1d,
        EvidenceTier.Mechanistic => 0.8d,
        EvidenceTier.Moderate => 0.5d,
        EvidenceTier.Limited => 0.2d,
        _ => 0d
    };

    private static List<string> DeriveReasons(
        KnowledgeEntry original,
        KnowledgeEntry candidate,
        InteractionSummaryResponse baselineSummary,
        InteractionSummaryResponse variantSummary,
        double deltaScore)
    {
        var reasons = new List<string>();

        if (variantSummary.Redundancies < baselineSummary.Redundancies)
        {
            reasons.Add(SwapReasonAtoms.ReducesRedundancy);
        }

        if (variantSummary.Interferences < baselineSummary.Interferences)
        {
            reasons.Add(SwapReasonAtoms.LowersInterference);
        }

        if (variantSummary.Synergies >= baselineSummary.Synergies && variantSummary.Synergies > 0)
        {
            reasons.Add(SwapReasonAtoms.PreservesSynergy);
        }

        if (EvidenceWeight(candidate.EvidenceTier) > EvidenceWeight(original.EvidenceTier))
        {
            reasons.Add(SwapReasonAtoms.StrongerEvidence);
        }

        if (deltaScore > 0
            && variantSummary.Redundancies < baselineSummary.Redundancies
            && !reasons.Contains(SwapReasonAtoms.ImprovesSignalClarity))
        {
            reasons.Add(SwapReasonAtoms.ImprovesSignalClarity);
        }

        if (reasons.Count == 0 && deltaScore > 0)
        {
            reasons.Add(SwapReasonAtoms.ImprovesGoalAlignment);
        }

        return reasons.Take(3).ToList();
    }

    private static double CalculateDeltaPercent(double deltaScore, double baselineCompositeScore)
    {
        var denominator = Math.Max(1d, baselineCompositeScore);
        return (deltaScore / denominator) * 100d;
    }

    private static string ResolveVerdict(double deltaScore)
    {
        if (deltaScore >= ImprovesThreshold)
        {
            return SwapVerdicts.LikelyImproves;
        }

        if (deltaScore <= WorsensThreshold)
        {
            return SwapVerdicts.LikelyWorsens;
        }

        return SwapVerdicts.LittleExpectedChange;
    }

    private static string BuildRecommendation(string original, string candidate, string verdict, double deltaPercent)
    {
        var magnitude = Math.Abs(deltaPercent);
        return verdict switch
        {
            SwapVerdicts.LikelyImproves =>
                $"Replacing {original} with {candidate} likely improves predicted stack efficiency by about {magnitude:0.#}%.",
            SwapVerdicts.LikelyWorsens =>
                $"Replacing {original} with {candidate} likely reduces predicted stack efficiency by about {magnitude:0.#}%.",
            _ =>
                $"Replacing {original} with {candidate} is not predicted to materially change stack efficiency."
        };
    }
}
