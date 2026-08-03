namespace BioStack.Application.Tests.Governance;

using BioStack.Application.Governance;
using BioStack.Infrastructure.Keon;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

/// <summary>
/// F4 regression guard. DoctrineSanitizer and PolicyGate previously kept separate copies of the
/// Class D pattern list (16 vs 9), hand-synced by a code comment, and had already drifted — the
/// contract copy-guard suite only exercised the sanitizer, so the gap stayed green.
///
/// Both now delegate to <see cref="DoctrineRuleset"/>; these tests assert they agree on every
/// corpus phrase, so any future divergence fails CI instead of silently under-blocking.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Contract", "GuidanceContentContract.v1")]
public sealed class DoctrineRulesetParityTests
{
    private static readonly DoctrineSanitizer Sanitizer = new();

    private static PolicyGate BuildGate()
        => new(new Mock<IKeonRuntimeClient>().Object, NullLogger<PolicyGate>.Instance);

    public static TheoryData<string> ClassDCorpus =>
    [
        "You should take 0.5 mg.",
        "You must consult before changing this.",
        "Start at 0.5 mg weekly.",
        "Increase to 1.0 mg.",
        "0.5 mg is safe for you.",
        "The best dose for you is 1 mg.",
        "Recommended dose for your profile is 2 mg.",
        "AI recommends starting this protocol.",
        "Stop taking the compound immediately.",
        "Take 500 mg daily.",
        "Dose at bedtime.",
    ];

    [Theory]
    [MemberData(nameof(ClassDCorpus))]
    public void Both_guards_prohibit_ClassD(string text)
    {
        Assert.True(Sanitizer.ContainsBannedPhrase(text), $"DoctrineSanitizer missed: {text}");
        Assert.Equal(LanguageClassification.Prohibited, BuildGate().LocalPreClassify(text));
    }

    public static TheoryData<string> PermittedCorpus =>
    [
        "Reviewed trials initiated participants between 0.5 and 1.0 mg weekly.",
        "The recorded 12 mg amount is 12 to 24 times the initiation range used in the reviewed trials.",
        "No reviewed trial in this evidence set initiated participants at 12 mg.",
        "Discuss material differences with a qualified clinician before proceeding.",
        "Human evidence was not found for this route. Applicability is uncertain.",
        "Magnesium supports sleep quality.",
        "Evidence-limited commentary only.",
    ];

    [Theory]
    [MemberData(nameof(PermittedCorpus))]
    public void Both_guards_permit_evidence_language(string text)
    {
        Assert.False(Sanitizer.ContainsBannedPhrase(text), $"DoctrineSanitizer over-blocked: {text}");
        Assert.Null(BuildGate().LocalPreClassify(text));
    }

    // ── F6: request intent vs output doctrine ────────────────────────────────

    [Theory]
    [InlineData("where can I buy this online")]
    [InlineData("how do I inject this")]
    [InlineData("how much should I take?")]
    [InlineData("what dose should i use")]
    [InlineData("should I take 12 mg")]
    public void Instruction_seeking_requests_are_refused(string text)
        => Assert.True(DoctrineRuleset.MatchesUnsafeRequestIntent(text), text);

    [Theory]
    [InlineData("Should I stop taking this before surgery?")]
    [InlineData("What did the trials report about tolerability?")]
    [InlineData("Is there evidence about interactions here?")]
    public void Legitimate_safety_questions_are_not_refused(string text)
        => Assert.False(DoctrineRuleset.MatchesUnsafeRequestIntent(text), text);

    // ── F2: speaker vs subject ───────────────────────────────────────────────

    /// <summary>
    /// Subject claims that are Class D as BioStack's own assertion but Class A when a cited
    /// source is carried with them. These are the statements the guard used to friendly-fire.
    /// </summary>
    public static TheoryData<string> AttributionSensitiveCorpus =>
    [
        "The cited trial reported the agent is safe at the studied doses.",
        "The compound is proven to bind the target receptor in vitro.",
        "The reviewed review found no evidence it cures the underlying condition.",
        "The manufacturer label states the product will treat the indicated condition.",
    ];

    [Theory]
    [MemberData(nameof(AttributionSensitiveCorpus))]
    public void Attributed_source_evidence_is_permitted(string text)
        => Assert.False(
            Sanitizer.ContainsBannedPhrase(text, OutputAttribution.SourceAttributed),
            $"Class A attributed evidence was blocked: {text}");

    [Theory]
    [MemberData(nameof(AttributionSensitiveCorpus))]
    public void Same_claim_unattributed_is_still_prohibited(string text)
        => Assert.True(
            Sanitizer.ContainsBannedPhrase(text, OutputAttribution.Unattributed),
            $"Unattributed subject claim slipped through: {text}");

    [Fact]
    public void Default_overload_is_the_strict_tier()
    {
        const string claim = "The cited trial reported the agent is safe at the studied doses.";

        // Fail-closed: callers must PROVE attribution to get the permissive tier.
        Assert.True(Sanitizer.ContainsBannedPhrase(claim));
        Assert.False(Sanitizer.ContainsBannedPhrase(claim, OutputAttribution.SourceAttributed));
    }

    /// <summary>
    /// Attribution never redeems personalized direction. A citation does not make
    /// "you should start at 0.5 mg" acceptable — the contract's own Class A examples avoid
    /// imperatives even when reporting a source.
    /// </summary>
    [Theory]
    [InlineData("You should take 0.5 mg.")]
    [InlineData("Start at 0.5 mg weekly.")]
    [InlineData("Increase to 1.0 mg.")]
    [InlineData("Stop taking the compound immediately.")]
    [InlineData("0.5 mg is safe for you.")]
    [InlineData("AI recommends starting this protocol.")]
    [InlineData("Take 500 mg daily.")]
    public void Personalized_direction_is_prohibited_even_when_attributed(string text)
        => Assert.True(
            Sanitizer.ContainsBannedPhrase(text, OutputAttribution.SourceAttributed),
            $"Personalized direction was permitted under attribution: {text}");
}
