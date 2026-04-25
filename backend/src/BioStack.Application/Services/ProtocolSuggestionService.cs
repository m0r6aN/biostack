namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;

public sealed class ProtocolSuggestionService : IProtocolSuggestionService
{
    public List<ProtocolSuggestionResponse> Suggest(
        ProtocolParseResult parseResult,
        IReadOnlyList<ProtocolIssueResponse> issues,
        CounterfactualResultDto counterfactuals)
    {
        var suggestions = new List<ProtocolSuggestionResponse>();

        foreach (var issue in issues)
        {
            if (issue.Type == "redundancy" && issue.Compounds.Count > 2)
            {
                var weakest = issue.Compounds.Last();
                suggestions.Add(new ProtocolSuggestionResponse(
                    "remove",
                    $"Consider removing {weakest} first and reassessing the protocol score.",
                    new List<string> { weakest }));
            }

            if (issue.Type == "overlap" && issue.Compounds.Count > 1)
            {
                suggestions.Add(new ProtocolSuggestionResponse(
                    "swap",
                    $"Review whether {issue.Compounds[1]} is adding a distinct mechanism or should be swapped for a different category.",
                    issue.Compounds.Take(2).ToList()));
            }

            if (issue.Type == "excessive_compounds")
            {
                suggestions.Add(new ProtocolSuggestionResponse(
                    "simplify",
                    "Reduce the stack to the highest-confidence compounds before adding secondary mechanisms.",
                    issue.Compounds));
            }

            if (issue.Type == "inefficiency")
            {
                suggestions.Add(new ProtocolSuggestionResponse(
                    "clarify",
                    "Add explicit dose, unit, and frequency for each compound to improve BioStack scoring confidence.",
                    issue.Compounds));
            }
        }

        suggestions.AddRange(counterfactuals.BestRemoveOne
            .Where(counterfactual => counterfactual.DeltaScore > 0)
            .Take(2)
            .Select(counterfactual => new ProtocolSuggestionResponse(
                "remove",
                counterfactual.Recommendation,
                new List<string> { counterfactual.RemovedCompound })));

        suggestions.AddRange(counterfactuals.BestSwapOne
            .Where(swap => swap.DeltaScore > 0)
            .Take(2)
            .Select(swap => new ProtocolSuggestionResponse(
                "swap",
                swap.Recommendation,
                new List<string> { swap.OriginalCompound, swap.CandidateCompound })));

        if (suggestions.Count == 0 && parseResult.Entries.Count > 0)
        {
            suggestions.Add(new ProtocolSuggestionResponse(
                "maintain",
                "No major redundancy was detected in the MVP rules; keep tracking response data before escalating complexity.",
                parseResult.Entries.Select(entry => entry.CompoundName).ToList()));
        }

        return suggestions
            .GroupBy(suggestion => $"{suggestion.Type}:{suggestion.Message}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(5)
            .ToList();
    }
}

public interface IProtocolSuggestionService
{
    List<ProtocolSuggestionResponse> Suggest(
        ProtocolParseResult parseResult,
        IReadOnlyList<ProtocolIssueResponse> issues,
        CounterfactualResultDto counterfactuals);
}
