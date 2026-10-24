namespace BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// Substance-record evidence tier vocabulary: the 7-tier methodology scale
/// (docs/biostack/evidence-grading-methodology-v1.md) plus "Not graded".
/// Relationship/graph packets keep their own vocabulary (see CompoundGraphBuilder).
/// </summary>
public static class RecordEvidenceTiers
{
    public const string Established = "Established";
    public const string PlausibleTheory = "Plausible Theory";
    public const string EarlyEvidence = "Early Evidence";
    public const string AnecdotalSignal = "Anecdotal Signal";
    public const string Contradicted = "Contradicted";
    public const string Unsupported = "Unsupported";
    public const string HypeExceedsEvidence = "Hype Exceeds Evidence";
    public const string NotGraded = "Not graded";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Established, PlausibleTheory, EarlyEvidence, AnecdotalSignal, Contradicted, Unsupported, HypeExceedsEvidence, NotGraded,
    };

    /// <summary>
    /// Maps an evidence-packet tier to a record tier. Packets may still carry the legacy
    /// scale; those map by evidence strength only (no claim text is available here):
    /// Strong→Established, Moderate→Early Evidence, Limited→Plausible Theory,
    /// Anecdotal→Anecdotal Signal, Insufficient→Unsupported, Unknown→Not graded.
    /// </summary>
    public static string Normalize(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        foreach (var t in All)
            if (t.Equals(v, StringComparison.OrdinalIgnoreCase)) return t;
        return v.ToLowerInvariant() switch
        {
            "strong" => Established,
            "moderate" => EarlyEvidence,
            "limited" => PlausibleTheory,
            "anecdotal" => AnecdotalSignal,
            "insufficient" => Unsupported,
            _ => NotGraded,
        };
    }

    /// <summary>Supportive rank: higher means stronger support for the claim. Negative tiers rank lowest.</summary>
    public static int Rank(string? value) => Normalize(value) switch
    {
        Established => 7,
        EarlyEvidence => 6,
        PlausibleTheory => 5,
        AnecdotalSignal => 4,
        Unsupported => 3,
        Contradicted => 2,
        HypeExceedsEvidence => 2,
        _ => 1,
    };

    /// <summary>True when the tier does not establish the claim (everything except Established and Early Evidence).</summary>
    public static bool IsWeak(string? value)
    {
        var t = Normalize(value);
        return t != Established && t != EarlyEvidence;
    }
}
