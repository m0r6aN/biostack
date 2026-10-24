namespace BioStack.Domain.Tests.Evidence;

using BioStack.Domain.Enums;
using BioStack.Domain.Evidence;
using Xunit;

/// <summary>Growth spec 02: five verdicts plus NOT YET GRADED, mapped onto the canonical 7-tier scale.</summary>
public sealed class ClaimVerdictsTests
{
    [Theory]
    [InlineData(EvidenceTier.Established, ClaimVerdicts.ProvenInHumans)]
    [InlineData(EvidenceTier.EarlyEvidence, ClaimVerdicts.Promising)]
    [InlineData(EvidenceTier.PlausibleTheory, ClaimVerdicts.AnimalOnly)]
    [InlineData(EvidenceTier.AnecdotalSignal, ClaimVerdicts.Thin)]
    [InlineData(EvidenceTier.Unsupported, ClaimVerdicts.Thin)]
    [InlineData(EvidenceTier.Contradicted, ClaimVerdicts.Myth)]
    [InlineData(EvidenceTier.Unknown, ClaimVerdicts.NotYetGraded)]
    public void Every_tier_maps_to_one_verdict(EvidenceTier tier, string verdict)
        => Assert.Equal(verdict, ClaimVerdicts.FromTier(tier));

    [Fact]
    public void Hype_exceeds_evidence_takes_its_verdict_from_the_evidence_basis()
    {
        Assert.Equal(ClaimVerdicts.AnimalOnly, ClaimVerdicts.FromTier(EvidenceTier.HypeExceedsEvidence, EvidenceTier.PlausibleTheory));
        Assert.Equal(ClaimVerdicts.Thin, ClaimVerdicts.FromTier(EvidenceTier.HypeExceedsEvidence, EvidenceTier.Unsupported));
        Assert.Equal(ClaimVerdicts.Thin, ClaimVerdicts.FromTier(EvidenceTier.HypeExceedsEvidence));
    }

    [Fact]
    public void Proven_and_myth_need_two_reviewers_and_proven_needs_a_qualifier()
    {
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        Assert.Contains(ClaimVerdicts.PublishBlockers(ClaimVerdicts.Myth, 1, 1, now, now, ""), b => b.Contains("2 reviewer(s)"));
        Assert.Empty(ClaimVerdicts.PublishBlockers(ClaimVerdicts.Myth, 1, 2, now, now, ""));
        Assert.Contains(ClaimVerdicts.PublishBlockers(ClaimVerdicts.ProvenInHumans, 1, 2, now, now, ""), b => b.Contains("qualifier"));
        Assert.Empty(ClaimVerdicts.PublishBlockers(ClaimVerdicts.Thin, 1, 1, now, now, ""));
        Assert.Contains(ClaimVerdicts.PublishBlockers(ClaimVerdicts.Thin, 0, 1, now, now, ""), b => b.Contains("source"));
        Assert.Contains(ClaimVerdicts.PublishBlockers(ClaimVerdicts.Thin, 1, 1, null, now, ""), b => b.Contains("verified_at"));
        Assert.Contains(ClaimVerdicts.PublishBlockers(ClaimVerdicts.Thin, 1, 1, now, null, ""), b => b.Contains("verdict date"));
    }

    [Fact]
    public void Qualifier_reads_in_population_for_outcome_under_trial_conditions()
        => Assert.Equal("In adults with obesity, for weight loss, under trial conditions.",
            ClaimVerdicts.Qualifier("adults with obesity", "weight loss"));

    [Theory]
    [InlineData(0, HypeGap.Low)]
    [InlineData(14, HypeGap.Low)]
    [InlineData(15, HypeGap.Moderate)]
    [InlineData(39, HypeGap.Moderate)]
    [InlineData(40, HypeGap.High)]
    [InlineData(64, HypeGap.High)]
    [InlineData(65, HypeGap.Extreme)]
    public void Band_thresholds_match_spec(int gap, string band) => Assert.Equal(band, HypeGap.BandFor(gap));

    [Fact]
    public void Hype_score_uses_rubric_and_clamped_intensity()
    {
        Assert.Equal(55, HypeGap.HypeScore("Common", 1.0));
        Assert.Equal(100, HypeGap.HypeScore("Viral", 1.3));
        Assert.Equal(8, HypeGap.HypeScore("Rare", 0.1)); // clamped to 0.8
        Assert.Null(HypeGap.HypeScore(null, 1.0));
    }

    [Fact]
    public void Band_subtracts_evidence_score_and_applies_myth_and_hype_tie_in_rules()
    {
        // Common (55) on an ANIMAL ONLY claim (35) → gap 20 → Moderate.
        Assert.Equal(HypeGap.Moderate, HypeGap.Band(ClaimVerdicts.AnimalOnly, EvidenceTier.PlausibleTheory, 55));
        // Same hype on a PROVEN claim → no gap → Low.
        Assert.Equal(HypeGap.Low, HypeGap.Band(ClaimVerdicts.ProvenInHumans, EvidenceTier.Established, 55));
        // MYTH with HypeScore ≥ 40 always shows Extreme.
        Assert.Equal(HypeGap.Extreme, HypeGap.Band(ClaimVerdicts.Myth, EvidenceTier.Contradicted, 40));
        // Hype Exceeds Evidence floors the band at High, scored or not.
        Assert.Equal(HypeGap.High, HypeGap.Band(ClaimVerdicts.Thin, EvidenceTier.HypeExceedsEvidence, null));
        Assert.Equal(HypeGap.High, HypeGap.Band(ClaimVerdicts.Thin, EvidenceTier.HypeExceedsEvidence, 10));
        Assert.Equal(HypeGap.Extreme, HypeGap.Band(ClaimVerdicts.Thin, EvidenceTier.HypeExceedsEvidence, 95));
        // Nothing scored, no Hype Exceeds Evidence grade → nothing to show.
        Assert.Equal(string.Empty, HypeGap.Band(ClaimVerdicts.AnimalOnly, EvidenceTier.PlausibleTheory, null));
    }
}
