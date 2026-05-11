namespace BioStack.Application.Tests.Governance;

using BioStack.Application.Governance;
using Xunit;

[Trait("Category", "Unit")]
public class DoctrineSanitizerTests
{
    private static readonly DoctrineSanitizer Sut = new();

    [Theory]
    [InlineData("You should take 500mg daily", true)]
    [InlineData("you must dose at noon", true)]
    [InlineData("This is safe for long-term use", true)]
    [InlineData("This cures inflammation", true)]
    [InlineData("Will treat the condition", true)]
    [InlineData("Stop taking immediately", true)]
    [InlineData("This observational data is consistent with published literature.", false)]
    [InlineData("Evidence-limited commentary only.", false)]
    [InlineData("The stack shows elevated redundancy across mTOR pathway.", false)]
    public void ContainsBannedPhrase_DetectsCorrectly(string text, bool expected)
    {
        Assert.Equal(expected, Sut.ContainsBannedPhrase(text));
    }

    [Fact]
    public void SanitizeFinding_WhenBanned_ReplacesWithStandardFallback()
    {
        const string banned = "You should take 500mg daily for best results.";
        var result = Sut.SanitizeFinding(banned);
        Assert.DoesNotContain("You should", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[review-required]", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizeFinding_WhenClean_ReturnsOriginal()
    {
        const string clean = "Observed correlation between compound use and improved recovery markers.";
        var result = Sut.SanitizeFinding(clean);
        Assert.Equal(clean, result);
    }

    [Fact]
    public void SanitizeAll_ProcessesEveryEntry()
    {
        var inputs = new[]
        {
            "You should take this daily.",
            "Observed markers within normal range.",
            "This cures the problem."
        };
        var results = Sut.SanitizeAll(inputs).ToList();
        Assert.Equal(3, results.Count);
        Assert.Contains("[review-required]", results[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal(inputs[1], results[1]);
        Assert.Contains("[review-required]", results[2], StringComparison.OrdinalIgnoreCase);
    }
}
