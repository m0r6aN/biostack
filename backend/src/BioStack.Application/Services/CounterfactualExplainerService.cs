namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;

public sealed class CounterfactualExplainerService : ICounterfactualExplainerService
{
    public List<string> ExplainRemoval(InteractionCounterfactualResponse counterfactual)
    {
        var reasons = new List<string>();

        if (counterfactual.Summary.Redundancies == 0)
        {
            reasons.Add("Overlapping pathways reduced.");
        }

        if (counterfactual.DeltaScore >= 3)
        {
            reasons.Add("Predicted stack quality improved meaningfully.");
        }

        if (counterfactual.TopFindings.Count < 2)
        {
            reasons.Add("Protocol becomes easier to interpret cleanly.");
        }

        return reasons.Count > 0 ? reasons : new List<string> { "This removal simplifies the protocol without improving the score much." };
    }

    public List<string> ExplainSwap(InteractionSwapRecommendationResponse swap)
    {
        var reasons = new List<string>();

        if (swap.Reasons.Any(reason => reason.Contains("redundancy", StringComparison.OrdinalIgnoreCase)))
        {
            reasons.Add("Overlapping pathway load reduced.");
        }

        if (swap.Reasons.Any(reason => reason.Contains("evidence", StringComparison.OrdinalIgnoreCase)))
        {
            reasons.Add("Higher-support compound candidate.");
        }

        if (swap.DeltaScore > 0)
        {
            reasons.Add("Predicted score improves while keeping protocol breadth.");
        }

        return reasons.Count > 0 ? reasons : new List<string> { swap.Recommendation };
    }

    public List<string> ExplainGoalAware(string goal, int baselineScore, int newScore)
    {
        var reasons = new List<string>
        {
            $"Stronger fit for {goal}.",
        };

        if (newScore >= baselineScore)
        {
            reasons.Add("Improves or preserves the baseline BioStack score.");
        }

        return reasons;
    }
}

public interface ICounterfactualExplainerService
{
    List<string> ExplainRemoval(InteractionCounterfactualResponse counterfactual);
    List<string> ExplainSwap(InteractionSwapRecommendationResponse swap);
    List<string> ExplainGoalAware(string goal, int baselineScore, int newScore);
}
