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
                    $"A redundancy signal includes {weakest}; excluding one compound is an observational scenario for review, not a recommended action.",
                    new List<string> { weakest }));
            }

            if (issue.Type == "overlap" && issue.Compounds.Count > 1)
            {
                suggestions.Add(new ProtocolSuggestionResponse(
                    "swap",
                    $"{issue.Compounds[1]} may not add a distinct mechanism; the available signal identifies category overlap without recommending a swap.",
                    issue.Compounds.Take(2).ToList()));
            }

            if (issue.Type == "excessive_compounds")
            {
                suggestions.Add(new ProtocolSuggestionResponse(
                    "simplify",
                    "The number of compounds reduces interpretability; higher-confidence and secondary mechanisms are not clearly distinguishable from the available data.",
                    issue.Compounds));
            }

            if (issue.Type == "inefficiency")
            {
                suggestions.Add(new ProtocolSuggestionResponse(
                    "clarify",
                    "Dose, unit, or frequency data is incomplete, which limits BioStack scoring confidence.",
                    issue.Compounds));
            }
        }

        suggestions.AddRange(counterfactuals.BestRemoveOne
            .Where(counterfactual => counterfactual.DeltaScore > 0)
            .Take(2)
            .Select(counterfactual => new ProtocolSuggestionResponse(
                "scenario",
                $"A modeled scenario excluding {counterfactual.RemovedCompound} changed the predicted score by {counterfactual.DeltaScore:+0.##;-0.##;0}; this is observational and is not a recommendation to remove or change the protocol.",
                new List<string> { counterfactual.RemovedCompound })));

        suggestions.AddRange(counterfactuals.BestSwapOne
            .Where(swap => swap.DeltaScore > 0)
            .Take(2)
            .Select(swap => new ProtocolSuggestionResponse(
                "scenario",
                $"A modeled substitution scenario involving {swap.OriginalCompound} and {swap.CandidateCompound} changed the predicted score by {swap.DeltaScore:+0.##;-0.##;0}; this is observational and is not a recommendation to substitute or change the protocol.",
                new List<string> { swap.OriginalCompound, swap.CandidateCompound })));

        if (suggestions.Count == 0 && parseResult.Entries.Count > 0)
        {
            suggestions.Add(new ProtocolSuggestionResponse(
                "maintain",
                "The MVP rules detected no major redundancy. This observational result does not establish that the protocol is safe or optimal; response data remains necessary.",
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
