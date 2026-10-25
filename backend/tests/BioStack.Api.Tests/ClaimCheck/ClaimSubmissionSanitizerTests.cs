namespace BioStack.Api.Tests.ClaimCheck;

using BioStack.Api.Endpoints;
using Xunit;

public sealed class ClaimSubmissionSanitizerTests
{
    [Theory]
    [InlineData("Ran 5mg a day and healed my tendon", "5mg")]
    [InlineData("250-500 mcg twice daily", "250-500 mcg")]
    [InlineData("got it from https://shady.example/shop for cheap", "https://")]
    [InlineData("check peptidevendor.com they ship fast", "peptidevendor.com")]
    [InlineData("@peptideguy says it works", "@peptideguy")]
    [InlineData("only $49.99 a vial", "$49.99")]
    [InlineData("bought on Amazon, use code SAVE10", "Amazon")]
    [InlineData("Swiss Chems sells it", "Swiss Chems")]
    public void StripsDosesCurrencyUrlsHandlesAndSellers(string input, string mustBeGone)
    {
        var (text, redacted) = ClaimSubmissionSanitizer.Clean(input, 280);
        Assert.True(redacted);
        Assert.DoesNotContain(mustBeGone, text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ClaimSubmissionSanitizer.Removed, text);
    }

    [Fact]
    public void LeavesPlainClaimsAlone_AndTruncates()
    {
        var (text, redacted) = ClaimSubmissionSanitizer.Clean("  BPC-157   heals   tendons  ", 280);
        Assert.Equal("BPC-157 heals tendons", text);
        Assert.False(redacted);
        Assert.Equal(140, ClaimSubmissionSanitizer.Clean(new string('a', 400), 140).Text.Length);
    }

    [Theory]
    [InlineData("I had chest pain after the second week", true)]
    [InlineData("ended up hospitalized", true)]
    [InlineData("mild nausea in the morning", false)]
    public void DetectsEmergencyLanguage(string text, bool expected)
        => Assert.Equal(expected, ClaimSubmissionSanitizer.IsEmergency(text));

    [Fact]
    public void QueryHash_IsNormalizedAndNotReversible()
    {
        Assert.Equal(ClaimCheckEndpoints.HashQuery("MK-677  has no side effects"), ClaimCheckEndpoints.HashQuery(" mk-677 has no side effects "));
        Assert.Equal(64, ClaimCheckEndpoints.HashQuery("x y").Length);
    }
}
