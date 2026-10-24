namespace BioStack.Domain.Enums;

/// <summary>
/// The 7-tier methodology scale (docs/biostack/evidence-grading-methodology-v1.md).
/// Values start at 10 so a stored legacy value (1-4: Limited/Moderate/Strong/Mechanistic)
/// can never be read as a new tier; migration 20261024010000 maps old rows.
/// </summary>
public enum EvidenceTier
{
    /// <summary>Not graded yet.</summary>
    Unknown = 0,
    Established = 10,
    PlausibleTheory = 11,
    EarlyEvidence = 12,
    AnecdotalSignal = 13,
    Contradicted = 14,
    Unsupported = 15,
    HypeExceedsEvidence = 16,
}

public static class EvidenceTiers
{
    /// <summary>Display label used in records, docs and the UI.</summary>
    public static string Label(EvidenceTier tier) => tier switch
    {
        EvidenceTier.Established => "Established",
        EvidenceTier.PlausibleTheory => "Plausible Theory",
        EvidenceTier.EarlyEvidence => "Early Evidence",
        EvidenceTier.AnecdotalSignal => "Anecdotal Signal",
        EvidenceTier.Contradicted => "Contradicted",
        EvidenceTier.Unsupported => "Unsupported",
        EvidenceTier.HypeExceedsEvidence => "Hype Exceeds Evidence",
        _ => "Not graded",
    };

    /// <summary>Parses a record label ("Early Evidence") or enum name ("EarlyEvidence").</summary>
    public static EvidenceTier Parse(string? value)
    {
        var key = (value ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty).Trim();
        return key.ToLowerInvariant() switch
        {
            "established" => EvidenceTier.Established,
            "plausibletheory" => EvidenceTier.PlausibleTheory,
            "earlyevidence" => EvidenceTier.EarlyEvidence,
            "anecdotalsignal" => EvidenceTier.AnecdotalSignal,
            "contradicted" => EvidenceTier.Contradicted,
            "unsupported" => EvidenceTier.Unsupported,
            "hypeexceedsevidence" => EvidenceTier.HypeExceedsEvidence,
            _ => EvidenceTier.Unknown,
        };
    }

    /// <summary>
    /// Supportive weight in [0,1] for scoring code: how much the tier supports a benefit.
    /// Contradicted, Unsupported and Hype Exceeds Evidence carry none.
    /// </summary>
    public static double SupportWeight(EvidenceTier tier) => tier switch
    {
        EvidenceTier.Established => 1d,
        EvidenceTier.EarlyEvidence => 0.5d,
        EvidenceTier.PlausibleTheory => 0.2d,
        EvidenceTier.AnecdotalSignal => 0.1d,
        _ => 0d,
    };

    /// <summary>True for tiers that say the claim is not supported (Contradicted, Unsupported, Hype Exceeds Evidence).</summary>
    public static bool IsNegative(EvidenceTier tier)
        => tier is EvidenceTier.Contradicted or EvidenceTier.Unsupported or EvidenceTier.HypeExceedsEvidence;
}
