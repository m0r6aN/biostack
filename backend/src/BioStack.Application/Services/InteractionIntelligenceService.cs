namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;

public sealed class InteractionIntelligenceService : IInteractionIntelligenceService
{
    private readonly IKnowledgeSource _knowledgeSource;
    private readonly ICompoundInteractionHintRepository _hintRepository;

    public InteractionIntelligenceService(
        IKnowledgeSource knowledgeSource,
        ICompoundInteractionHintRepository hintRepository)
    {
        _knowledgeSource = knowledgeSource;
        _hintRepository = hintRepository;
    }

    public async Task<InteractionIntelligenceResponse> EvaluateByNamesAsync(
        IEnumerable<string> compoundNames,
        CancellationToken cancellationToken = default)
    {
        var names = compoundNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var entries = new List<KnowledgeEntry>();
        foreach (var name in names)
        {
            var entry = await _knowledgeSource.GetCompoundAsync(name, cancellationToken);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return await EvaluateAsync(entries, cancellationToken);
    }

    public async Task<InteractionIntelligenceResponse> EvaluateAsync(
        IReadOnlyList<KnowledgeEntry> entries,
        CancellationToken cancellationToken = default)
    {
        return await EvaluateAsync(entries, includeCounterfactuals: true, cancellationToken);
    }

    private async Task<InteractionIntelligenceResponse> EvaluateAsync(
        IReadOnlyList<KnowledgeEntry> entries,
        bool includeCounterfactuals,
        CancellationToken cancellationToken)
    {
        var interactions = new List<InteractionResultResponse>();

        for (var i = 0; i < entries.Count; i++)
        {
            for (var j = i + 1; j < entries.Count; j++)
            {
                interactions.Add(await EvaluatePairAsync(entries[i], entries[j], cancellationToken));
            }
        }

        var summary = new InteractionSummaryResponse(
            interactions.Count(result => result.Type == InteractionType.Synergistic),
            interactions.Count(result => result.Type == InteractionType.Redundant),
            interactions.Count(result => result.Type == InteractionType.Interfering));

        var score = new ProtocolInteractionScoreResponse(
            Math.Round(interactions.Where(result => result.Type == InteractionType.Synergistic).Sum(result => result.Confidence), 2),
            Math.Round(interactions.Where(result => result.Type == InteractionType.Redundant).Sum(result => result.Confidence), 2),
            Math.Round(interactions.Where(result => result.Type == InteractionType.Interfering).Sum(result => result.Confidence * 1.5d), 2));
        var compositeScore = CalculateCompositeScore(score);

        var topFindings = interactions
            .Where(result => result.Type != InteractionType.Neutral)
            .OrderByDescending(result => result.Confidence)
            .ThenBy(result => result.CompoundA, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.CompoundB, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(result => new InteractionFindingResponse(
                result.Type,
                new List<string> { result.CompoundA, result.CompoundB },
                BuildFindingMessage(result),
                result.Confidence))
            .ToList();

        var counterfactuals = includeCounterfactuals
            ? await BuildCounterfactualsAsync(entries, compositeScore, cancellationToken)
            : new List<InteractionCounterfactualResponse>();

        return new InteractionIntelligenceResponse(summary, score, compositeScore, topFindings, interactions, counterfactuals);
    }

    private async Task<InteractionResultResponse> EvaluatePairAsync(
        KnowledgeEntry compoundA,
        KnowledgeEntry compoundB,
        CancellationToken cancellationToken)
    {
        var sharedPathways = compoundA.Pathways
            .Intersect(compoundB.Pathways, StringComparer.OrdinalIgnoreCase)
            .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hint = await _hintRepository.FindPairAsync(compoundA.CanonicalName, compoundB.CanonicalName, cancellationToken);
        if (hint is not null)
        {
            return new InteractionResultResponse(
                compoundA.CanonicalName,
                compoundB.CanonicalName,
                hint.InteractionType,
                Math.Round((double)hint.Strength, 2),
                sharedPathways,
                string.IsNullOrWhiteSpace(hint.Notes) ? "Known interaction pattern" : hint.Notes,
                HintBacked: true);
        }

        if (HasNamedMatch(compoundA.AvoidWith, compoundB) || HasNamedMatch(compoundB.AvoidWith, compoundA))
        {
            return new InteractionResultResponse(
                compoundA.CanonicalName,
                compoundB.CanonicalName,
                InteractionType.Interfering,
                0.72d,
                sharedPathways,
                "Avoid-with guidance directly links these compounds.",
                HintBacked: false);
        }

        if (HasNamedInteraction(compoundA.DrugInteractions, compoundB) || HasNamedInteraction(compoundB.DrugInteractions, compoundA))
        {
            return new InteractionResultResponse(
                compoundA.CanonicalName,
                compoundB.CanonicalName,
                InteractionType.Interfering,
                0.64d,
                sharedPathways,
                "Interaction notes suggest a review-worthy pairing.",
                HintBacked: false);
        }

        if (HasNamedMatch(compoundA.PairsWellWith, compoundB) || HasNamedMatch(compoundB.PairsWellWith, compoundA) ||
            HasNamedMatch(compoundA.CompatibleBlends, compoundB) || HasNamedMatch(compoundB.CompatibleBlends, compoundA))
        {
            return new InteractionResultResponse(
                compoundA.CanonicalName,
                compoundB.CanonicalName,
                InteractionType.Synergistic,
                0.59d,
                sharedPathways,
                "Compound metadata suggests a complementary pairing.",
                HintBacked: false);
        }

        if (sharedPathways.Count > 0)
        {
            return new InteractionResultResponse(
                compoundA.CanonicalName,
                compoundB.CanonicalName,
                InteractionType.Redundant,
                CalculatePathwayOverlapConfidence(compoundA, compoundB, sharedPathways.Count),
                sharedPathways,
                sharedPathways.Count == 1
                    ? $"Shared pathway overlap detected: {sharedPathways[0]}."
                    : $"Shared pathway overlap detected across {sharedPathways.Count} pathways.",
                HintBacked: false);
        }

        return new InteractionResultResponse(
            compoundA.CanonicalName,
            compoundB.CanonicalName,
            InteractionType.Neutral,
            0.30d,
            sharedPathways,
            "No significant overlap detected from the current rule set.",
            HintBacked: false);
    }

    private static bool HasNamedMatch(IEnumerable<string> candidates, KnowledgeEntry target)
    {
        return candidates.Any(candidate =>
            string.Equals(candidate.Trim(), target.CanonicalName, StringComparison.OrdinalIgnoreCase)
            || target.Aliases.Any(alias => string.Equals(candidate.Trim(), alias.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasNamedInteraction(IEnumerable<string> candidates, KnowledgeEntry target)
    {
        return candidates.Any(candidate =>
            candidate.Contains(target.CanonicalName, StringComparison.OrdinalIgnoreCase)
            || target.Aliases.Any(alias => candidate.Contains(alias, StringComparison.OrdinalIgnoreCase)));
    }

    private static double CalculatePathwayOverlapConfidence(KnowledgeEntry compoundA, KnowledgeEntry compoundB, int sharedPathwayCount)
    {
        var baseConfidence = 0.45d + Math.Min(0.20d, sharedPathwayCount * 0.08d);
        var evidenceLift = (GetEvidenceWeight(compoundA.EvidenceTier) + GetEvidenceWeight(compoundB.EvidenceTier)) / 2d;
        return Math.Round(Math.Min(0.82d, baseConfidence + evidenceLift), 2);
    }

    private static double GetEvidenceWeight(EvidenceTier tier)
    {
        return tier switch
        {
            EvidenceTier.Strong => 0.10d,
            EvidenceTier.Mechanistic => 0.08d,
            EvidenceTier.Moderate => 0.05d,
            EvidenceTier.Limited => 0.02d,
            _ => 0d
        };
    }

    private static string BuildFindingMessage(InteractionResultResponse result)
    {
        return result.Type switch
        {
            InteractionType.Synergistic => $"{result.CompoundA} and {result.CompoundB} show a complementary signal worth tracking together.",
            InteractionType.Redundant => $"{result.CompoundA} and {result.CompoundB} appear to overlap enough that attribution may get muddy.",
            InteractionType.Interfering => $"{result.CompoundA} and {result.CompoundB} raise a review-first interaction signal.",
            _ => $"{result.CompoundA} and {result.CompoundB} do not currently trigger a strong interaction signal."
        };
    }

    private async Task<List<InteractionCounterfactualResponse>> BuildCounterfactualsAsync(
        IReadOnlyList<KnowledgeEntry> entries,
        double baselineCompositeScore,
        CancellationToken cancellationToken)
    {
        if (entries.Count < 2)
        {
            return new List<InteractionCounterfactualResponse>();
        }

        var counterfactuals = new List<InteractionCounterfactualResponse>();

        for (var i = 0; i < entries.Count; i++)
        {
            var removed = entries[i];
            var variantEntries = entries
                .Where((_, index) => index != i)
                .ToList();

            var variant = await EvaluateAsync(variantEntries, includeCounterfactuals: false, cancellationToken);
            var deltaScore = Math.Round(variant.CompositeScore - baselineCompositeScore, 2);
            var deltaPercent = Math.Round(CalculateDeltaPercent(deltaScore, baselineCompositeScore), 2);
            var verdict = ResolveVerdict(deltaScore);

            counterfactuals.Add(new InteractionCounterfactualResponse(
                removed.CanonicalName,
                variant.CompositeScore,
                deltaScore,
                deltaPercent,
                verdict,
                BuildRecommendation(removed.CanonicalName, verdict, deltaPercent),
                variant.Summary,
                variant.TopFindings));
        }

        return counterfactuals
            .OrderByDescending(counterfactual => counterfactual.DeltaScore)
            .ThenBy(counterfactual => counterfactual.RemovedCompound, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static double CalculateCompositeScore(ProtocolInteractionScoreResponse score)
    {
        var composite = 50d
            + (score.SynergyScore * 18d)
            - (score.RedundancyPenalty * 14d)
            - (score.InterferencePenalty * 12d);

        return Math.Round(Math.Clamp(composite, 0d, 100d), 2);
    }

    private static double CalculateDeltaPercent(double deltaScore, double baselineCompositeScore)
    {
        var denominator = Math.Max(1d, baselineCompositeScore);
        return (deltaScore / denominator) * 100d;
    }

    private static string ResolveVerdict(double deltaScore)
    {
        if (deltaScore >= 3d)
        {
            return "improves";
        }

        if (deltaScore <= -3d)
        {
            return "worsens";
        }

        return "no_meaningful_change";
    }

    private static string BuildRecommendation(string removedCompound, string verdict, double deltaPercent)
    {
        var roundedPercent = Math.Abs(deltaPercent);

        return verdict switch
        {
            "improves" => $"Removing {removedCompound} likely improves predicted stack efficiency by about {roundedPercent:0.#}%.",
            "worsens" => $"Removing {removedCompound} likely reduces predicted stack efficiency by about {roundedPercent:0.#}%.",
            _ => $"Removing {removedCompound} is not predicted to materially change stack efficiency."
        };
    }
}

public interface IInteractionIntelligenceService
{
    Task<InteractionIntelligenceResponse> EvaluateByNamesAsync(IEnumerable<string> compoundNames, CancellationToken cancellationToken = default);
    Task<InteractionIntelligenceResponse> EvaluateAsync(IReadOnlyList<KnowledgeEntry> entries, CancellationToken cancellationToken = default);
}
