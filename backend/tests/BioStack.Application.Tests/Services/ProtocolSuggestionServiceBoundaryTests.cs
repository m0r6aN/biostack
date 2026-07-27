namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using Xunit;

public sealed class ProtocolSuggestionServiceBoundaryTests
{
    [Fact]
    public void Suggest_UsesObservationalLanguageForRuleSignals()
    {
        var issues = new List<ProtocolIssueResponse>
        {
            new("redundancy", "Redundancy detected.", new List<string> { "A", "B", "C" }),
            new("overlap", "Overlap detected.", new List<string> { "A", "B" }),
            new("excessive_compounds", "Many compounds.", new List<string> { "A", "B", "C" }),
            new("inefficiency", "Incomplete details.", new List<string> { "A" })
        };

        var suggestions = new ProtocolSuggestionService().Suggest(CreateParseResult(), issues, EmptyCounterfactuals());
        var messages = string.Join(" ", suggestions.Select(suggestion => suggestion.Message));

        Assert.Contains("observational scenario for review, not a recommended action", messages);
        Assert.Contains("without recommending a swap", messages);
        Assert.Contains("reduces interpretability", messages);
        Assert.Contains("limits BioStack scoring confidence", messages);
        Assert.DoesNotContain("Consider removing", messages, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Reduce the stack", messages, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("should be swapped", messages, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Add explicit", messages, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Suggest_DoesNotTreatNoDetectedRedundancyAsSafetyOrOptimizationProof()
    {
        var suggestion = Assert.Single(new ProtocolSuggestionService().Suggest(
            CreateParseResult(),
            Array.Empty<ProtocolIssueResponse>(),
            EmptyCounterfactuals()));

        Assert.Contains("does not establish that the protocol is safe or optimal", suggestion.Message);
        Assert.Contains("response data remains necessary", suggestion.Message);
    }

    [Fact]
    public void Suggest_ReframesNonEmptyCounterfactualsWithoutForwardingActionCopy()
    {
        var counterfactuals = new CounterfactualResultDto(
            60,
            new List<InteractionCounterfactualResponse>
            {
                new(
                    "A",
                    68,
                    8,
                    13.3,
                    "improves",
                    "REMOVE A NOW",
                    new InteractionSummaryResponse(0, 0, 0),
                    new List<InteractionFindingResponse>())
            },
            new List<InteractionSwapRecommendationResponse>
            {
                new(
                    "A",
                    "B",
                    60,
                    70,
                    10,
                    16.7,
                    "likely_improves",
                    new List<string> { "reduces_redundancy" },
                    "SWAP A FOR B NOW",
                    0.8,
                    new InteractionSummaryResponse(0, 0, 0),
                    new List<InteractionFindingResponse>())
            },
            null,
            new List<GoalAwareOptimizationResponse>());

        var suggestions = new ProtocolSuggestionService().Suggest(
            CreateParseResult(),
            Array.Empty<ProtocolIssueResponse>(),
            counterfactuals);

        Assert.Equal(2, suggestions.Count);
        Assert.All(suggestions, suggestion => Assert.Equal("scenario", suggestion.Type));
        Assert.All(suggestions, suggestion => Assert.Contains("not a recommendation", suggestion.Message));
        Assert.DoesNotContain(suggestions, suggestion => suggestion.Message.Contains("REMOVE A NOW", StringComparison.Ordinal));
        Assert.DoesNotContain(suggestions, suggestion => suggestion.Message.Contains("SWAP A FOR B NOW", StringComparison.Ordinal));
    }

    private static ProtocolParseResult CreateParseResult()
    {
        return new ProtocolParseResult(
            new List<ProtocolEntryResponse> { new("A", 0, string.Empty, string.Empty, string.Empty) },
            new Dictionary<string, KnowledgeEntry>(StringComparer.OrdinalIgnoreCase),
            new List<ProtocolBlendExpansionResponse>());
    }

    private static CounterfactualResultDto EmptyCounterfactuals()
    {
        return new CounterfactualResultDto(
            60,
            new List<InteractionCounterfactualResponse>(),
            new List<InteractionSwapRecommendationResponse>(),
            null,
            new List<GoalAwareOptimizationResponse>());
    }
}
