namespace BioStack.Application.Tests.Services;

// This file mirrors frontend/src/lib/analyzerGoals.ts — keep both lists byte-identical for token strings;
// these tokens are the authority. When updating tokens here, update analyzerGoals.ts to match.

using BioStack.Infrastructure.Knowledge;
using Xunit;

/// <summary>
/// Guarantees every goal token string the analyzer can send matches at least one knowledge-base
/// entry's Benefits, Pathways, or MechanismSummary. If a token matches nothing, that goal would
/// silently score zero alignment — so this test fails loudly.
/// </summary>
public sealed class AnalyzerGoalVocabularyTests
{
    public static TheoryData<string, string> CategoryTokens => new()
    {
        // key                  tokens
        { "recovery",           "healing injury recovery tissue repair" },
        { "energy",             "energy metabolic health" },
        { "cognitive",          "cognitive enhancement" },
        { "longevity",          "anti-aging cellular repair longevity" },
        { "performance",        "performance muscle endurance recovery" },
        { "skin",               "skin collagen anti-aging" },
        { "organ",              "gut health cardiovascular organ health" },
    };

    public static TheoryData<string, string> GoalTokens => new()
    {
        // key                          tokens
        { "recovery-muscles",           "tissue repair muscle joint tendon" },
        { "recovery-inflammation",      "reduced inflammation" },
        { "recovery-injury",            "injury recovery healing" },
        { "recovery-post-workout",      "recovery healing" },
        { "energy-levels",              "energy" },
        { "energy-mitochondrial",       "cellular energy mitochondrial" },
        { "energy-metabolic",           "metabolic health insulin sensitivity" },
        { "energy-fat-loss",            "fat loss weight loss" },
        { "cognitive-focus",            "cognitive enhancement focus" },
        { "cognitive-memory",           "cognitive enhancement memory" },
        { "cognitive-performance",      "cognitive enhancement" },
        { "cognitive-neuro-health",     "cognitive neurological health" },
        { "longevity-aging",            "anti-aging" },
        { "longevity-cellular",         "DNA repair cellular repair" },
        { "longevity-pathways",         "longevity anti-aging" },
        { "performance-endurance",      "endurance energy" },
        { "performance-strength",       "muscle strength tissue repair" },
        { "performance-training",       "recovery energy" },
        { "skin-elasticity",            "skin elasticity anti-aging" },
        { "skin-appearance",            "skin anti-aging" },
        { "skin-collagen",              "collagen skin anti-aging" },
        { "organ-health",               "organ health liver kidney" },
        { "organ-gut",                  "gut health" },
        { "organ-cardiovascular",       "cardiovascular heart metabolic" },
    };

    [Theory]
    [MemberData(nameof(CategoryTokens))]
    public void CategoryToken_MatchesAtLeastOneKnowledgeEntry(string key, string tokens)
    {
        AssertTokensMatchKnowledge(key, tokens);
    }

    [Theory]
    [MemberData(nameof(GoalTokens))]
    public void GoalToken_MatchesAtLeastOneKnowledgeEntry(string key, string tokens)
    {
        AssertTokensMatchKnowledge(key, tokens);
    }

    private static void AssertTokensMatchKnowledge(string key, string tokens)
    {
        var source = new LocalKnowledgeSource();
        var entries = source.GetAllCompoundsAsync().GetAwaiter().GetResult();

        var splitTokens = tokens.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var matched = entries.Any(entry =>
            entry.Benefits.Any(benefit =>
                splitTokens.Any(token => benefit.Contains(token, StringComparison.OrdinalIgnoreCase)))
            || entry.Pathways.Any(pathway =>
                splitTokens.Any(token => pathway.Contains(token, StringComparison.OrdinalIgnoreCase)))
            || splitTokens.Any(token =>
                entry.MechanismSummary.Contains(token, StringComparison.OrdinalIgnoreCase)));

        Assert.True(
            matched,
            $"Goal key '{key}' with tokens [{tokens}] matched zero knowledge-base entries. " +
            $"Adjust BOTH this file and frontend/src/lib/analyzerGoals.ts so at least one token " +
            $"aligns with a real Benefits, Pathways, or MechanismSummary term in LocalKnowledgeSource.");
    }
}
