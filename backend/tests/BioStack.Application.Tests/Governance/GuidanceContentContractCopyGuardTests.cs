namespace BioStack.Application.Tests.Governance;

using BioStack.Application.Governance;
using Xunit;

/// <summary>
/// Copy-guard tests for BioStack Guidance Content Contract v1 Class D prohibitions.
/// Class A/B/C evidence language must remain permitted.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Contract", "GuidanceContentContract.v1")]
public sealed class GuidanceContentContractCopyGuardTests
{
    private static readonly DoctrineSanitizer Sut = new();

    [Theory]
    [InlineData("You should take 0.5 mg.")]
    [InlineData("Start at 0.5 mg weekly.")]
    [InlineData("Increase to 1.0 mg.")]
    [InlineData("0.5 mg is safe for you.")]
    [InlineData("The best dose for you is 1 mg.")]
    [InlineData("Recommended dose for your profile is 2 mg.")]
    [InlineData("AI recommends starting this protocol.")]
    [InlineData("Stop taking the compound immediately.")]
    public void ClassD_personalized_direction_is_banned(string text)
    {
        Assert.True(Sut.ContainsBannedPhrase(text), text);
    }

    [Theory]
    [InlineData(
        "Reviewed trials initiated participants between 0.5 and 1.0 mg weekly and used the following escalation schedules.")]
    [InlineData(
        "The recorded 12 mg amount is 12 to 24 times the initiation range used in the reviewed trials.")]
    [InlineData(
        "No reviewed trial in this evidence set initiated participants at 12 mg.")]
    [InlineData(
        "The reviewed evidence supports a lower-exposure initiation context than the amount entered.")]
    [InlineData(
        "Discuss material differences with a qualified clinician before proceeding.")]
    [InlineData(
        "Human evidence was not found for this route. Applicability is uncertain.")]
    [InlineData(
        "FAERS spontaneous reports are not incidence estimates or proven causation.")]
    public void ClassABC_evidence_language_is_permitted(string text)
    {
        Assert.False(Sut.ContainsBannedPhrase(text), text);
    }
}
