namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using Xunit;

public sealed class AnalyzerSecondaryGoalsTests
{
    private static ProtocolNormalizationService CreateNormalizationService()
    {
        return new ProtocolNormalizationService();
    }

    [Fact]
    public void BuildAnalysisContext_NormalizesSecondaryGoals()
    {
        var service = CreateNormalizationService();
        var context = service.BuildAnalysisContext(
            "healing", new[] { " Fat Loss ", "fat loss", "", "energy" }, null, null, null, null);
        Assert.Equal(new List<string> { "energy", "fat loss" }, context.SecondaryGoals);
    }

    [Fact]
    public void BuildAnalysisContext_NullSecondaryGoals_YieldsEmptyList()
    {
        var service = CreateNormalizationService();
        var context = service.BuildAnalysisContext("healing", null, null, null, null, null);
        Assert.Empty(context.SecondaryGoals);
    }

    [Fact]
    public void BuildAnalysisContext_AllBlankSecondaryGoals_YieldsEmptyList()
    {
        var service = CreateNormalizationService();
        var context = service.BuildAnalysisContext(
            "healing", new[] { "", "  " }, null, null, null, null);
        Assert.Empty(context.SecondaryGoals);
    }

    [Fact]
    public void GetAnalysisKey_DiffersWhenSecondaryGoalsDiffer()
    {
        var fingerprint = new ProtocolFingerprintService();
        var service = CreateNormalizationService();
        var protocol = new NormalizedProtocol(new List<NormalizedProtocolCompound>(), new List<BioStack.Contracts.Responses.ProtocolBlendExpansionResponse>());

        var without = service.BuildAnalysisContext("healing", null, null, null, null, null);
        var with = service.BuildAnalysisContext("healing", new[] { "energy" }, null, null, null, null);

        Assert.NotEqual(
            fingerprint.GetAnalysisKey(protocol, without),
            fingerprint.GetAnalysisKey(protocol, with));
    }

    [Fact]
    public void CombinedGoalAlignment_WeightsSecondaryAtHalf()
    {
        // KnowledgeEntry whose benefits match ONLY the secondary goal term.
        var entry = new KnowledgeEntry
        {
            CanonicalName = "Retatrutide",
            Benefits = new List<string> { "weight loss" },
            Pathways = new List<string>(),
            MechanismSummary = string.Empty
        };

        var primaryOnly = CounterfactualCandidateService.CombinedGoalAlignment(entry, "healing", new List<string>());
        var withSecondary = CounterfactualCandidateService.CombinedGoalAlignment(entry, "healing", new List<string> { "weight loss" });

        Assert.Equal(0d, primaryOnly);
        Assert.True(withSecondary > 0d);

        var directPrimary = CounterfactualCandidateService.CombinedGoalAlignment(entry, "weight loss", new List<string>());
        Assert.Equal(directPrimary * 0.5d, withSecondary, precision: 10);
    }
}
