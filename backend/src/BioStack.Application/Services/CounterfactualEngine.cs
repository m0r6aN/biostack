namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;

public sealed class CounterfactualEngine : ICounterfactualEngine
{
    // Aligns with InteractionIntelligenceService.ResolveVerdict ("improves" >= 3).
    // We do not surface a removal/swap recommendation unless the predicted
    // gain crosses this bar — protocols that score the same with or without
    // a compound must not produce a "remove this" message.
    private const double MeaningfulImprovementDelta = 3d;

    private readonly IInteractionIntelligenceService _interactionIntelligenceService;
    private readonly ICounterfactualCandidateService _candidateService;
    private readonly ICounterfactualExplainerService _explainerService;

    public CounterfactualEngine(
        IInteractionIntelligenceService interactionIntelligenceService,
        ICounterfactualCandidateService candidateService,
        ICounterfactualExplainerService explainerService)
    {
        _interactionIntelligenceService = interactionIntelligenceService;
        _candidateService = candidateService;
        _explainerService = explainerService;
    }

    public async Task<CounterfactualResultDto> OptimizeAsync(
        List<ProtocolEntryResponse> baselineProtocol,
        IReadOnlyList<KnowledgeEntry> knownEntries,
        OptimizationContext context,
        CancellationToken cancellationToken = default)
    {
        var baseline = await _interactionIntelligenceService.EvaluateAsync(knownEntries, cancellationToken);
        var baselineScore = (int)Math.Round(baseline.CompositeScore);
        var bestRemove = baseline.Counterfactuals
            .Where(item => item.DeltaScore >= MeaningfulImprovementDelta)
            .OrderByDescending(item => item.DeltaScore)
            .Take(5)
            .Select(item => item with { Recommendation = string.Join(" ", _explainerService.ExplainRemoval(item)) })
            .ToList();
        var bestSwap = baseline.Swaps
            .Where(swap => !context.ExcludedCompoundIds.Any(item => string.Equals(item, swap.CandidateCompound, StringComparison.OrdinalIgnoreCase)))
            .Where(swap => swap.DeltaScore >= MeaningfulImprovementDelta)
            .OrderByDescending(item => item.DeltaScore)
            .Take(5)
            .Select(item => item with { Recommendation = string.Join(" ", _explainerService.ExplainSwap(item)) })
            .ToList();

        var simplified = await BuildSimplifiedProtocolAsync(baselineProtocol, knownEntries.ToList(), context, baselineScore, cancellationToken);
        var goalAwareOptions = await BuildGoalAwareOptionsAsync(baselineProtocol, knownEntries.ToList(), context, baselineScore, cancellationToken);

        return new CounterfactualResultDto(
            baselineScore,
            bestRemove,
            bestSwap,
            simplified,
            goalAwareOptions);
    }

    private async Task<SimplifiedProtocolResponse?> BuildSimplifiedProtocolAsync(
        List<ProtocolEntryResponse> baselineProtocol,
        List<KnowledgeEntry> knownEntries,
        OptimizationContext context,
        int baselineScore,
        CancellationToken cancellationToken)
    {
        var remainingProtocol = baselineProtocol.ToList();
        var remainingEntries = knownEntries.ToList();
        var removed = new List<string>();
        var reasons = new List<string>();
        var currentScore = baselineScore;

        while (remainingProtocol.Count > context.MaxCompounds)
        {
            var intelligence = await _interactionIntelligenceService.EvaluateAsync(remainingEntries, cancellationToken);
            var candidate = intelligence.Counterfactuals
                .Where(item => item.DeltaScore >= 0)
                .Where(item => !context.RequiredCompoundIds.Any(required => string.Equals(required, item.RemovedCompound, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(item => item.DeltaScore)
                .FirstOrDefault();

            if (candidate is null)
            {
                break;
            }

            var nextProtocol = remainingProtocol
                .Where(entry => !string.Equals(entry.CompoundName, candidate.RemovedCompound, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var nextEntries = remainingEntries
                .Where(entry => !string.Equals(entry.CanonicalName, candidate.RemovedCompound, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var nextScore = (int)Math.Round(candidate.VariantScore);

            if (nextScore < context.ScoreFloor)
            {
                break;
            }

            remainingProtocol = nextProtocol;
            remainingEntries = nextEntries;
            currentScore = nextScore;
            removed.Add(candidate.RemovedCompound);
            reasons.AddRange(_explainerService.ExplainRemoval(candidate));
        }

        return removed.Count == 0
            ? null
            : new SimplifiedProtocolResponse(remainingProtocol, currentScore, removed, reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private async Task<List<GoalAwareOptimizationResponse>> BuildGoalAwareOptionsAsync(
        List<ProtocolEntryResponse> baselineProtocol,
        List<KnowledgeEntry> knownEntries,
        OptimizationContext context,
        int baselineScore,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.Goal))
        {
            return new List<GoalAwareOptimizationResponse>();
        }

        var beam = new List<(List<KnowledgeEntry> Entries, List<ProtocolEntryResponse> Protocol)>
        {
            (knownEntries.ToList(), baselineProtocol.ToList())
        };

        for (var depth = 0; depth < 2; depth++)
        {
            var nextBeam = new List<(List<KnowledgeEntry> Entries, List<ProtocolEntryResponse> Protocol, double Score)>();

            foreach (var node in beam)
            {
                var candidates = await _candidateService.GetGoalCandidatesAsync(context.Goal, node.Entries, context, cancellationToken);
                foreach (var candidate in candidates.Take(context.BeamWidth))
                {
                    if (node.Protocol.Count >= context.MaxCompounds)
                    {
                        continue;
                    }

                    var expandedEntries = node.Entries.Append(candidate.KnowledgeEntry).ToList();
                    var expandedProtocol = node.Protocol.Append(new ProtocolEntryResponse(candidate.CanonicalName, 0, string.Empty, string.Empty, string.Empty)).ToList();
                    var score = (await _interactionIntelligenceService.EvaluateAsync(expandedEntries, cancellationToken)).CompositeScore;
                    nextBeam.Add((expandedEntries, expandedProtocol, score));
                }
            }

            beam = nextBeam
                .OrderByDescending(node => node.Score)
                .Take(context.BeamWidth)
                .Select(node => (node.Entries, node.Protocol))
                .ToList();
        }

        var results = new List<GoalAwareOptimizationResponse>();
        foreach (var node in beam)
        {
            var intelligence = await _interactionIntelligenceService.EvaluateAsync(node.Entries, cancellationToken);
            var score = (int)Math.Round(intelligence.CompositeScore);
            results.Add(new GoalAwareOptimizationResponse(
                context.Goal,
                node.Protocol,
                score,
                _explainerService.ExplainGoalAware(context.Goal, baselineScore, score)));
        }

        return results
            .OrderByDescending(option => option.Score)
            .Take(3)
            .ToList();
    }
}

public interface ICounterfactualEngine
{
    Task<CounterfactualResultDto> OptimizeAsync(
        List<ProtocolEntryResponse> baselineProtocol,
        IReadOnlyList<KnowledgeEntry> knownEntries,
        OptimizationContext context,
        CancellationToken cancellationToken = default);
}
