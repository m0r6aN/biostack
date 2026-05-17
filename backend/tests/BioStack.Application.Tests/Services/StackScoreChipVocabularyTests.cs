namespace BioStack.Application.Tests.Services;

using Xunit;

/// <summary>
/// Guards the complete vocabulary of chip labels emitted by ProtocolService.CalculateStackScore.
/// None of the approved labels should contain prescriptive, optimization, or clinical verdict language.
/// Update AllPossibleChips whenever the chip generation logic changes.
/// </summary>
public sealed class StackScoreChipVocabularyTests
{
    // Every distinct string that CalculateStackScore can emit, derived from reading
    // ProtocolService.cs CalculateStackScore (lines ~840-868).
    // Parametric labels use representative counts so the template strings are covered.
    public static TheoryData<string> AllPossibleChips => new()
    {
        // Interaction flag chip (first slot)
        "No interaction flags",
        "1 review-first interaction signal",
        "2 review-first interaction signals",
        "5 review-first interaction signals",

        // Redundancy chip (second slot)
        "Low redundancy",
        "Moderate redundancy",
        "High redundancy",

        // Evidence chip (third slot)
        "Strong evidence base",
        "Moderate evidence base",
        "Limited evidence base",

        // Optional synergy chip (fourth slot)
        "1 synergy signal",
        "2 synergy signals",
        "4 synergy signals",

        // Empty-stack sentinel chips
        "No active compounds",
        "No simulation inputs",
    };

    // Prescriptive, optimization, or clinical verdict language that must never appear.
    private static readonly string[] BannedPhrases =
    [
        "optimized",
        "optimize",
        "best",
        "recovery optimized",
        "healing focus",
        "recovery-focused",    // audited replacement label — also must not be added to backend
        "recovery goal",
        "repair goal",
        "recommend",
        "should",
        "safe to combine",
        "clinical",
        "ideal",
        "superior",
        "perfect",
        "maximum",
    ];

    [Theory]
    [MemberData(nameof(AllPossibleChips))]
    public void ChipLabel_DoesNotContainPrescriptiveOrVerdictLanguage(string chip)
    {
        foreach (var banned in BannedPhrases)
        {
            Assert.DoesNotContain(banned, chip, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [MemberData(nameof(AllPossibleChips))]
    public void ChipLabel_IsNonEmpty(string chip)
    {
        Assert.False(string.IsNullOrWhiteSpace(chip));
    }
}
