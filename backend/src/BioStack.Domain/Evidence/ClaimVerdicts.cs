using BioStack.Domain.Enums;

namespace BioStack.Domain.Evidence;

/// <summary>
/// Public claim verdicts (growth spec 02-verdict-system.md) mapped onto the canonical
/// 7-tier scale. The tier stays the source of truth; the verdict is a plain-language
/// label derived from it, and only shown once a reviewer has signed off.
/// </summary>
public static class ClaimVerdicts
{
    public const string ProvenInHumans = "PROVEN IN HUMANS";
    public const string Promising = "PROMISING";
    public const string AnimalOnly = "ANIMAL ONLY";
    public const string Thin = "THIN";
    public const string Myth = "MYTH";
    public const string NotYetGraded = "NOT YET GRADED";

    public static readonly IReadOnlyList<string> All =
        new[] { ProvenInHumans, Promising, AnimalOnly, Thin, Myth, NotYetGraded };

    /// <summary>
    /// Tier → verdict. Hype Exceeds Evidence is the Hype Gap tie-in, not an evidence
    /// level, so its verdict comes from <paramref name="evidenceBasis"/> (the tier the
    /// evidence itself earns); with no basis it falls back to THIN, the most
    /// conservative non-MYTH verdict.
    /// </summary>
    public static string FromTier(EvidenceTier tier, EvidenceTier evidenceBasis = EvidenceTier.Unknown) => tier switch
    {
        EvidenceTier.Established => ProvenInHumans,
        EvidenceTier.EarlyEvidence => Promising,
        EvidenceTier.PlausibleTheory => AnimalOnly,
        EvidenceTier.AnecdotalSignal => Thin,
        EvidenceTier.Unsupported => Thin,
        EvidenceTier.Contradicted => Myth,
        EvidenceTier.HypeExceedsEvidence => evidenceBasis is EvidenceTier.HypeExceedsEvidence or EvidenceTier.Unknown
            ? Thin
            : FromTier(evidenceBasis),
        _ => NotYetGraded,
    };

    /// <summary>EvidenceScore from the spec's Hype Gap formula.</summary>
    public static int EvidenceScore(string verdict) => verdict switch
    {
        ProvenInHumans => 100,
        Promising => 70,
        AnimalOnly => 35,
        Thin => 15,
        _ => 0,
    };

    /// <summary>PROVEN IN HUMANS and MYTH need two reviewers; everything else one.</summary>
    public static int RequiredReviewers(string verdict) => verdict is ProvenInHumans or Myth ? 2 : 1;

    /// <summary>Re-check cadence: PROMISING every 3 months, everything else every 6.</summary>
    public static int RecheckMonths(string verdict) => verdict == Promising ? 3 : 6;

    /// <summary>"In [population], for [outcome], under trial conditions." Required on every PROVEN IN HUMANS.</summary>
    public static string Qualifier(string? population, string? outcome)
        => string.IsNullOrWhiteSpace(population) || string.IsNullOrWhiteSpace(outcome)
            ? string.Empty
            : $"In {population.Trim()}, for {outcome.Trim()}, under trial conditions.";

    /// <summary>
    /// Why a candidate verdict cannot be published yet. Empty means publishable.
    /// </summary>
    public static IReadOnlyList<string> PublishBlockers(
        string candidateVerdict,
        int resolvedSourceCount,
        int reviewerCount,
        DateTime? verifiedAt,
        DateTime? publishedAt,
        string qualifier)
    {
        var blockers = new List<string>();
        if (candidateVerdict == NotYetGraded) blockers.Add("not graded");
        if (resolvedSourceCount < 1) blockers.Add("needs at least one resolvable source");
        if (verifiedAt is null) blockers.Add("needs verified_at");
        if (reviewerCount < RequiredReviewers(candidateVerdict))
            blockers.Add($"needs {RequiredReviewers(candidateVerdict)} reviewer sign-off(s)");
        if (candidateVerdict == ProvenInHumans && string.IsNullOrWhiteSpace(qualifier))
            blockers.Add("PROVEN IN HUMANS needs a population and outcome qualifier");
        if (publishedAt is null) blockers.Add("not published");
        return blockers;
    }
}

/// <summary>
/// Hype Gap v0: editorial scoring (spec section 4). The reviewer picks a volume level
/// from the 5-point rubric and an intensity multiplier; the public sees the band only.
/// </summary>
public static class HypeGap
{
    public const string Low = "Low";
    public const string Moderate = "Moderate";
    public const string High = "High";
    public const string Extreme = "Extreme";

    public const string MethodNote = "Hype is scored by our editors from public search and social signals.";
    public const string HypeExceedsEvidenceNote =
        "Graded Hype Exceeds Evidence: online certainty outruns the cited evidence.";

    /// <summary>Rubric level → base HypeScore (0–100). Rare/Niche/Common/Everywhere/Viral.</summary>
    public static int? RubricScore(string? level) => (level ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "rare" => 10,
        "niche" => 30,
        "common" => 55,
        "everywhere" => 75,
        "viral" => 95,
        _ => null,
    };

    /// <summary>Intensity multiplier is clamped to the spec's 0.8–1.3 range.</summary>
    public static int? HypeScore(string? rubricLevel, double? intensity)
    {
        var baseScore = RubricScore(rubricLevel);
        if (baseScore is null) return null;
        var multiplier = Math.Clamp(intensity ?? 1.0, 0.8, 1.3);
        return (int)Math.Min(100, Math.Round(baseScore.Value * multiplier, MidpointRounding.AwayFromZero));
    }

    public static string BandFor(int gap) => gap switch
    {
        < 15 => Low,
        < 40 => Moderate,
        < 65 => High,
        _ => Extreme,
    };

    /// <summary>
    /// Band for a claim. Returns empty when there is nothing to show (no editorial score
    /// and no Hype Exceeds Evidence grade). Hype Exceeds Evidence floors the band at High;
    /// MYTH with HypeScore ≥ 40 always shows Extreme.
    /// </summary>
    public static string Band(string verdict, EvidenceTier tier, int? hypeScore)
    {
        string band = string.Empty;
        if (hypeScore is int score)
        {
            if (verdict == ClaimVerdicts.Myth && score >= 40) return Extreme;
            band = BandFor(Math.Max(0, score - ClaimVerdicts.EvidenceScore(verdict)));
        }

        if (tier == EvidenceTier.HypeExceedsEvidence && Rank(band) < Rank(High)) band = High;
        return band;
    }

    private static int Rank(string band) => band switch
    {
        Low => 1,
        Moderate => 2,
        High => 3,
        Extreme => 4,
        _ => 0,
    };
}
