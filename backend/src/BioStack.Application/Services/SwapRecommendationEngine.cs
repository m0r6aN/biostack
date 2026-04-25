namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;

internal static class SwapRecommendationEngine
{
    internal const double MinPathwayJaccard = 0.2d;
    internal const double MinSimilarityScore = 0.25d;
    internal const double BorderlineSimilarityFloor = 0.35d;
    internal const int MaxCandidatesPerOriginal = 5;
    internal const int MaxSwapsReturned = 3;
    internal const double MinDeltaForRecommendation = 2d;
    internal const double MinDeltaForWeakExplanation = 4d;
    internal const double ImprovesThreshold = 3d;
    internal const double WorsensThreshold = -3d;
    internal const double MinPathwayPreservationRatio = 0.34d;
    internal const double MinGoalAlignmentPreservation = 0.5d;

    internal const string ExclusionSameCompound = "same_compound";
    internal const string ExclusionInStack = "in_stack";
    internal const string ExclusionClassificationMismatch = "classification_mismatch";
    internal const string ExclusionPathwayNotPreserved = "pathway_not_preserved";
    internal const string ExclusionIntroducesInterference = "introduces_interference";
    internal const string ExclusionLowSimilarity = "low_similarity_score";

    internal const string SuppressionTrivialDelta = "trivial_delta";
    internal const string SuppressionBorderlineSimilarity = "borderline_similarity_weak_delta";
    internal const string SuppressionWeakExplanation = "weak_explanation_only";
    internal const string SuppressionNoReasons = "no_reasons";
    internal const string SuppressionRankCutoff = "rank_cutoff";

    private static readonly HashSet<string> StrongReasonAtoms = new(StringComparer.Ordinal)
    {
        SwapReasonAtoms.ReducesRedundancy,
        SwapReasonAtoms.LowersInterference,
        SwapReasonAtoms.StrongerEvidence,
        SwapReasonAtoms.PreservesSynergy
    };

    internal delegate Task<InteractionIntelligenceResponse> VariantEvaluator(
        IReadOnlyList<KnowledgeEntry> entries,
        CancellationToken cancellationToken);

    internal sealed record SwapCandidateTrace(
        string OriginalCompound,
        string CandidateCompound,
        double SimilarityScore,
        bool PassedEligibility,
        string? ExclusionReason,
        double? DeltaScore,
        IReadOnlyList<string> Reasons,
        bool Surfaced,
        string? SuppressionReason);

    internal sealed record SwapEvaluationResult(
        List<InteractionSwapRecommendationResponse> Recommendations,
        List<SwapCandidateTrace> Traces);

    public static async Task<List<InteractionSwapRecommendationResponse>> BuildSwapsAsync(
        IReadOnlyList<KnowledgeEntry> currentEntries,
        IReadOnlyList<KnowledgeEntry> candidatePool,
        double baselineCompositeScore,
        InteractionSummaryResponse baselineSummary,
        IReadOnlyList<InteractionResultResponse> baselineInteractions,
        VariantEvaluator evaluateVariant,
        CancellationToken cancellationToken)
    {
        var result = await EvaluateAsync(
            currentEntries,
            candidatePool,
            baselineCompositeScore,
            baselineSummary,
            baselineInteractions,
            evaluateVariant,
            cancellationToken);
        return result.Recommendations;
    }

    internal static async Task<SwapEvaluationResult> EvaluateAsync(
        IReadOnlyList<KnowledgeEntry> currentEntries,
        IReadOnlyList<KnowledgeEntry> candidatePool,
        double baselineCompositeScore,
        InteractionSummaryResponse baselineSummary,
        IReadOnlyList<InteractionResultResponse> baselineInteractions,
        VariantEvaluator evaluateVariant,
        CancellationToken cancellationToken)
    {
        var traces = new List<SwapCandidateTrace>();
        var recommendations = new List<InteractionSwapRecommendationResponse>();

        if (currentEntries.Count == 0 || candidatePool.Count == 0)
        {
            return new SwapEvaluationResult(recommendations, traces);
        }

        var stackNames = BuildNameSet(currentEntries);

        foreach (var original in currentEntries)
        {
            var remainingStack = currentEntries
                .Where(entry => !ReferenceEquals(entry, original))
                .ToList();
            var preservableSynergyPairs = CollectPreservableSynergyPairs(baselineInteractions, original);

            var eligible = new List<(KnowledgeEntry Entry, double Similarity)>();

            foreach (var candidate in candidatePool)
            {
                var exclusion = CheckEligibility(candidate, original, stackNames, remainingStack);
                if (exclusion is not null)
                {
                    traces.Add(new SwapCandidateTrace(
                        original.CanonicalName, candidate.CanonicalName,
                        0d, false, exclusion, null, Array.Empty<string>(), false, null));
                    continue;
                }

                var similarity = CalculateSimilarity(original, candidate);
                if (similarity < MinSimilarityScore)
                {
                    traces.Add(new SwapCandidateTrace(
                        original.CanonicalName, candidate.CanonicalName,
                        Math.Round(similarity, 2), false, ExclusionLowSimilarity, null, Array.Empty<string>(), false, null));
                    continue;
                }

                eligible.Add((candidate, similarity));
            }

            var topEligible = eligible
                .OrderByDescending(x => x.Similarity)
                .ThenBy(x => x.Entry.CanonicalName, StringComparer.OrdinalIgnoreCase)
                .Take(MaxCandidatesPerOriginal)
                .ToList();

            foreach (var (entry, similarity) in topEligible)
            {
                var variantEntries = remainingStack.Append(entry).ToList();
                var variant = await evaluateVariant(variantEntries, cancellationToken);

                var deltaScore = Math.Round(variant.CompositeScore - baselineCompositeScore, 2);
                var reasons = DeriveReasons(
                    original, entry,
                    baselineSummary, variant.Summary,
                    preservableSynergyPairs, variant.Interactions,
                    deltaScore);

                var suppression = EvaluateSuppression(deltaScore, similarity, reasons);
                if (suppression is not null)
                {
                    traces.Add(new SwapCandidateTrace(
                        original.CanonicalName, entry.CanonicalName,
                        Math.Round(similarity, 2), true, null, deltaScore, reasons, false, suppression));
                    continue;
                }

                var deltaPercent = Math.Round(CalculateDeltaPercent(deltaScore, baselineCompositeScore), 2);
                var verdict = ResolveVerdict(deltaScore);

                recommendations.Add(new InteractionSwapRecommendationResponse(
                    original.CanonicalName,
                    entry.CanonicalName,
                    baselineCompositeScore,
                    variant.CompositeScore,
                    deltaScore,
                    deltaPercent,
                    verdict,
                    reasons,
                    BuildRecommendation(original.CanonicalName, entry.CanonicalName, verdict, deltaPercent),
                    Math.Round(similarity, 2),
                    variant.Summary,
                    variant.TopFindings));

                traces.Add(new SwapCandidateTrace(
                    original.CanonicalName, entry.CanonicalName,
                    Math.Round(similarity, 2), true, null, deltaScore, reasons, true, null));
            }
        }

        var ranked = recommendations
            .Select(r =>
            {
                var original = currentEntries.First(e =>
                    string.Equals(e.CanonicalName, r.OriginalCompound, StringComparison.OrdinalIgnoreCase));
                var candidate = candidatePool.First(c =>
                    string.Equals(c.CanonicalName, r.CandidateCompound, StringComparison.OrdinalIgnoreCase));
                return new
                {
                    R = r,
                    Strong = r.Reasons.Count(a => StrongReasonAtoms.Contains(a)),
                    IntendedUse = IntendedUseScore(original, candidate),
                    EvidenceDelta = EvidenceWeight(candidate.EvidenceTier) - EvidenceWeight(original.EvidenceTier)
                };
            })
            .OrderByDescending(x => x.R.DeltaScore)
            .ThenByDescending(x => x.Strong)
            .ThenByDescending(x => x.IntendedUse)
            .ThenByDescending(x => x.EvidenceDelta)
            .ThenByDescending(x => x.R.SimilarityScore)
            .ThenBy(x => x.R.OriginalCompound, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.R.CandidateCompound, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var finalList = ranked.Take(MaxSwapsReturned).Select(x => x.R).ToList();

        var finalKeys = new HashSet<(string O, string C)>(
            finalList.Select(r => (r.OriginalCompound, r.CandidateCompound)));
        for (var i = 0; i < traces.Count; i++)
        {
            var t = traces[i];
            if (t.Surfaced && !finalKeys.Contains((t.OriginalCompound, t.CandidateCompound)))
            {
                traces[i] = t with { Surfaced = false, SuppressionReason = SuppressionRankCutoff };
            }
        }

        return new SwapEvaluationResult(finalList, traces);
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

    private static string? CheckEligibility(
        KnowledgeEntry candidate,
        KnowledgeEntry original,
        HashSet<string> stackNames,
        IReadOnlyList<KnowledgeEntry> remainingStack)
    {
        if (string.Equals(candidate.CanonicalName, original.CanonicalName, StringComparison.OrdinalIgnoreCase))
        {
            return ExclusionSameCompound;
        }

        if (stackNames.Contains(candidate.CanonicalName)
            || candidate.Aliases.Any(alias => !string.IsNullOrWhiteSpace(alias) && stackNames.Contains(alias.Trim())))
        {
            return ExclusionInStack;
        }

        if (candidate.Classification != original.Classification || candidate.Classification == CompoundCategory.Unknown)
        {
            return ExclusionClassificationMismatch;
        }

        if (!PreservesKeyPathways(original, candidate) && !HasExplicitPeerSignal(candidate, original))
        {
            return ExclusionPathwayNotPreserved;
        }

        foreach (var remaining in remainingStack)
        {
            if (IntroducesInterference(candidate, remaining))
            {
                return ExclusionIntroducesInterference;
            }
        }

        return null;
    }

    private static bool PreservesKeyPathways(KnowledgeEntry original, KnowledgeEntry candidate)
    {
        if (original.Pathways.Count == 0)
        {
            return true;
        }

        var shared = original.Pathways
            .Intersect(candidate.Pathways, StringComparer.OrdinalIgnoreCase)
            .Count();
        var required = Math.Max(1, (int)Math.Ceiling(original.Pathways.Count * MinPathwayPreservationRatio));
        return shared >= required;
    }

    private static double IntendedUseScore(KnowledgeEntry original, KnowledgeEntry candidate)
    {
        if (original.Pathways.Count == 0)
        {
            return 0d;
        }
        var shared = original.Pathways
            .Intersect(candidate.Pathways, StringComparer.OrdinalIgnoreCase)
            .Count();
        return (double)shared / original.Pathways.Count;
    }

    private static HashSet<(string, string)> CollectPreservableSynergyPairs(
        IReadOnlyList<InteractionResultResponse> baselineInteractions,
        KnowledgeEntry original)
    {
        var pairs = new HashSet<(string, string)>();
        if (baselineInteractions.Count == 0)
        {
            return pairs;
        }

        foreach (var result in baselineInteractions)
        {
            if (!IsPositivePairSignal(result.Type))
            {
                continue;
            }
            if (IsOriginal(result.CompoundA, original) || IsOriginal(result.CompoundB, original))
            {
                continue;
            }
            pairs.Add(NormalizePairKey(result.CompoundA, result.CompoundB));
        }
        return pairs;
    }

    private static bool IsPositivePairSignal(InteractionType type) =>
        type == InteractionType.Synergistic || type == InteractionType.Complementary;

    private static (string, string) NormalizePairKey(string a, string b)
    {
        var left = (a ?? string.Empty).Trim().ToLowerInvariant();
        var right = (b ?? string.Empty).Trim().ToLowerInvariant();
        return string.CompareOrdinal(left, right) <= 0 ? (left, right) : (right, left);
    }

    private static int CountPreservedSynergies(
        IReadOnlySet<(string, string)> preservableSynergyPairs,
        IReadOnlyList<InteractionResultResponse> variantInteractions)
    {
        if (preservableSynergyPairs.Count == 0 || variantInteractions.Count == 0)
        {
            return 0;
        }

        // Use a set of matched keys so that duplicate Synergistic rows for the same
        // pair do not inflate the count past preservableSynergyPairs.Count and
        // incorrectly block the preserves_synergy gate (which checks ==).
        var preservedKeys = new HashSet<(string, string)>();
        foreach (var result in variantInteractions)
        {
            if (!IsPositivePairSignal(result.Type))
            {
                continue;
            }
            var key = NormalizePairKey(result.CompoundA, result.CompoundB);
            if (preservableSynergyPairs.Contains(key))
            {
                preservedKeys.Add(key);
            }
        }
        return preservedKeys.Count;
    }

    private static bool IsOriginal(string name, KnowledgeEntry original)
    {
        if (string.Equals(name, original.CanonicalName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return original.Aliases.Any(alias =>
            !string.IsNullOrWhiteSpace(alias)
            && string.Equals(name, alias.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? EvaluateSuppression(double deltaScore, double similarity, IReadOnlyList<string> reasons)
    {
        if (Math.Abs(deltaScore) < MinDeltaForRecommendation)
        {
            return SuppressionTrivialDelta;
        }

        if (reasons.Count == 0)
        {
            return SuppressionNoReasons;
        }

        var hasStrong = reasons.Any(r => StrongReasonAtoms.Contains(r));
        if (!hasStrong && Math.Abs(deltaScore) < MinDeltaForWeakExplanation)
        {
            return SuppressionWeakExplanation;
        }

        if (similarity < BorderlineSimilarityFloor && Math.Abs(deltaScore) < MinDeltaForWeakExplanation)
        {
            return SuppressionBorderlineSimilarity;
        }

        return null;
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
        IReadOnlySet<(string, string)> preservableSynergyPairs,
        IReadOnlyList<InteractionResultResponse> variantInteractions,
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

        if (preservableSynergyPairs.Count > 0
            && CountPreservedSynergies(preservableSynergyPairs, variantInteractions) == preservableSynergyPairs.Count)
        {
            reasons.Add(SwapReasonAtoms.PreservesSynergy);
        }

        if (EvidenceWeight(candidate.EvidenceTier) > EvidenceWeight(original.EvidenceTier)
            && candidate.EvidenceTier != EvidenceTier.Unknown)
        {
            reasons.Add(SwapReasonAtoms.StrongerEvidence);
        }

        var redundancyDrop = baselineSummary.Redundancies - variantSummary.Redundancies;
        var interferenceDrop = baselineSummary.Interferences - variantSummary.Interferences;
        if (deltaScore > 0
            && (redundancyDrop + interferenceDrop) >= 2
            && !reasons.Contains(SwapReasonAtoms.ImprovesSignalClarity))
        {
            reasons.Add(SwapReasonAtoms.ImprovesSignalClarity);
        }

        if (reasons.Count == 0
            && deltaScore > 0
            && IntendedUseScore(original, candidate) >= MinGoalAlignmentPreservation)
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
