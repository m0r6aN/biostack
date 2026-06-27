namespace BioStack.Application.Governance;

/// <summary>
/// Stable high-risk category identifiers (Lane H). A compound landing in any of these categories
/// forces warning-first framing, evidence-limit disclosure, and removal of any safety certainty or
/// imperative usage language before its intelligence reaches a user.
/// </summary>
public static class HighRiskCategory
{
    public const string Sarm = "sarm";
    public const string Serm = "serm";
    public const string InvestigationalPeptide = "investigational-peptide";
    public const string GrayMarketPeptide = "gray-market-peptide";
    public const string CompoundedGlp1 = "compounded-glp1";
    public const string PrescriptionOnly = "prescription-only";
    public const string BannedInSport = "banned-in-sport";
    public const string UnknownIdentity = "unknown-identity";
    public const string UnclearRegulatoryStatus = "unclear-regulatory-status";
}

/// <summary>
/// Result of classifying a set of substances/category hints against the high-risk taxonomy.
/// </summary>
/// <param name="IsHighRisk">True when at least one high-risk category matched.</param>
/// <param name="Categories">The distinct matched categories (see <see cref="HighRiskCategory"/>).</param>
/// <param name="RequiredFramings">Warning-first, observational framing lines that MUST accompany any output.</param>
public sealed record HighRiskAssessment(
    bool IsHighRisk,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> RequiredFramings);

/// <summary>
/// Lane H high-risk category gate. Deterministic, dependency-free classifier that maps substance
/// names (and optional explicit category hints supplied by a caller) to high-risk categories and the
/// warning-first framing required for each.
///
/// The keyword map is intentionally a pragmatic, extensible heuristic — not a regulatory database.
/// It exists to ensure the obviously-risky categories the canon calls out (SARMs, SERMs,
/// investigational/gray-market peptides, compounded GLP-1s, prescription-only and banned-in-sport
/// substances, and unknown identity/regulatory status) cannot reach a user surface without
/// warning-first, non-prescriptive framing. "Unknown beats inference": when in doubt, warn.
/// </summary>
public sealed class HighRiskCategoryGate
{
    // Substance-name signals. Matched as case-insensitive substrings so brand/alias/code variants
    // ("RAD-140", "rad140", "testolone") all resolve. Order is irrelevant; all matches accumulate.
    private static readonly (string Keyword, string Category)[] SubstanceSignals =
    [
        // ── SARMs ──
        ("ostarine", HighRiskCategory.Sarm), ("mk-2866", HighRiskCategory.Sarm), ("mk2866", HighRiskCategory.Sarm),
        ("ligandrol", HighRiskCategory.Sarm), ("lgd-4033", HighRiskCategory.Sarm), ("lgd4033", HighRiskCategory.Sarm),
        ("testolone", HighRiskCategory.Sarm), ("rad-140", HighRiskCategory.Sarm), ("rad140", HighRiskCategory.Sarm),
        ("andarine", HighRiskCategory.Sarm), ("yk-11", HighRiskCategory.Sarm), ("yk11", HighRiskCategory.Sarm),
        ("s-23", HighRiskCategory.Sarm), ("s23", HighRiskCategory.Sarm), ("sarm", HighRiskCategory.Sarm),
        // GH secretagogues / metabolic agents commonly sold alongside SARMs as research chemicals.
        ("cardarine", HighRiskCategory.InvestigationalPeptide), ("gw-501516", HighRiskCategory.InvestigationalPeptide),
        ("gw501516", HighRiskCategory.InvestigationalPeptide), ("stenabolic", HighRiskCategory.InvestigationalPeptide),
        ("sr9009", HighRiskCategory.InvestigationalPeptide), ("mk-677", HighRiskCategory.InvestigationalPeptide),
        ("mk677", HighRiskCategory.InvestigationalPeptide), ("ibutamoren", HighRiskCategory.InvestigationalPeptide),
        // ── SERMs ──
        ("tamoxifen", HighRiskCategory.Serm), ("nolvadex", HighRiskCategory.Serm),
        ("raloxifene", HighRiskCategory.Serm), ("clomiphene", HighRiskCategory.Serm),
        ("clomid", HighRiskCategory.Serm), ("enclomiphene", HighRiskCategory.Serm),
        ("toremifene", HighRiskCategory.Serm),
        // ── Investigational peptides ──
        ("bpc-157", HighRiskCategory.InvestigationalPeptide), ("bpc157", HighRiskCategory.InvestigationalPeptide),
        ("tb-500", HighRiskCategory.InvestigationalPeptide), ("tb500", HighRiskCategory.InvestigationalPeptide),
        ("ipamorelin", HighRiskCategory.InvestigationalPeptide), ("cjc-1295", HighRiskCategory.InvestigationalPeptide),
        ("cjc1295", HighRiskCategory.InvestigationalPeptide), ("ghrp", HighRiskCategory.InvestigationalPeptide),
        ("tesamorelin", HighRiskCategory.InvestigationalPeptide), ("hexarelin", HighRiskCategory.InvestigationalPeptide),
        ("melanotan", HighRiskCategory.InvestigationalPeptide), ("pt-141", HighRiskCategory.InvestigationalPeptide),
        ("aod-9604", HighRiskCategory.InvestigationalPeptide), ("epitalon", HighRiskCategory.InvestigationalPeptide),
        ("mots-c", HighRiskCategory.InvestigationalPeptide), ("follistatin", HighRiskCategory.InvestigationalPeptide),
        ("igf-1", HighRiskCategory.InvestigationalPeptide), ("peptide", HighRiskCategory.InvestigationalPeptide),
        // ── Compounded GLP-1 / incretin agonists ──
        ("semaglutide", HighRiskCategory.CompoundedGlp1), ("tirzepatide", HighRiskCategory.CompoundedGlp1),
        ("retatrutide", HighRiskCategory.CompoundedGlp1), ("liraglutide", HighRiskCategory.CompoundedGlp1),
        ("ozempic", HighRiskCategory.CompoundedGlp1), ("wegovy", HighRiskCategory.CompoundedGlp1),
        ("mounjaro", HighRiskCategory.CompoundedGlp1), ("zepbound", HighRiskCategory.CompoundedGlp1),
        ("glp-1", HighRiskCategory.CompoundedGlp1), ("glp1", HighRiskCategory.CompoundedGlp1),
        // ── Prescription-only ──
        ("testosterone", HighRiskCategory.PrescriptionOnly), ("anastrozole", HighRiskCategory.PrescriptionOnly),
        ("arimidex", HighRiskCategory.PrescriptionOnly), ("finasteride", HighRiskCategory.PrescriptionOnly),
        ("hcg", HighRiskCategory.PrescriptionOnly), ("levothyroxine", HighRiskCategory.PrescriptionOnly),
        // ── Banned-in-sport / anabolic-androgenic ──
        ("clenbuterol", HighRiskCategory.BannedInSport), ("stanozolol", HighRiskCategory.BannedInSport),
        ("winstrol", HighRiskCategory.BannedInSport), ("trenbolone", HighRiskCategory.BannedInSport),
        ("anavar", HighRiskCategory.BannedInSport), ("oxandrolone", HighRiskCategory.BannedInSport),
        ("dianabol", HighRiskCategory.BannedInSport), ("higenamine", HighRiskCategory.BannedInSport),
        ("dnp", HighRiskCategory.BannedInSport),
    ];

    // Explicit category-hint signals (e.g. a stack-review payload's compound Category field, or a
    // source-quality label). Matched as case-insensitive substrings.
    private static readonly (string Hint, string Category)[] CategorySignals =
    [
        ("sarm", HighRiskCategory.Sarm),
        ("serm", HighRiskCategory.Serm),
        ("peptide", HighRiskCategory.InvestigationalPeptide),
        ("glp", HighRiskCategory.CompoundedGlp1),
        ("incretin", HighRiskCategory.CompoundedGlp1),
        ("prescription", HighRiskCategory.PrescriptionOnly),
        ("rx-only", HighRiskCategory.PrescriptionOnly),
        ("banned", HighRiskCategory.BannedInSport),
        ("wada", HighRiskCategory.BannedInSport),
        ("anabolic", HighRiskCategory.BannedInSport),
        ("gray-market", HighRiskCategory.GrayMarketPeptide),
        ("grey-market", HighRiskCategory.GrayMarketPeptide),
        ("unverified", HighRiskCategory.UnknownIdentity),
        ("unknown-identity", HighRiskCategory.UnknownIdentity),
        ("unknown source", HighRiskCategory.UnknownIdentity),
        ("research chemical", HighRiskCategory.UnclearRegulatoryStatus),
        ("not for human consumption", HighRiskCategory.UnclearRegulatoryStatus),
        ("unclear regulatory", HighRiskCategory.UnclearRegulatoryStatus),
        ("investigational", HighRiskCategory.InvestigationalPeptide),
    ];

    private static readonly IReadOnlyDictionary<string, string> CategoryFramings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [HighRiskCategory.Sarm] =
                "SARMs are investigational and not approved for human use; evidence on safety is limited and identity/source quality should be treated as a risk.",
            [HighRiskCategory.Serm] =
                "SERMs are prescription medicines; this is educational context only, not a recommendation to use them.",
            [HighRiskCategory.InvestigationalPeptide] =
                "This is an investigational/research peptide; safety in humans is not established and source quality should be treated as a risk.",
            [HighRiskCategory.GrayMarketPeptide] =
                "Gray-market peptides carry significant identity and purity risk; BioStack cannot verify what a given product actually contains.",
            [HighRiskCategory.CompoundedGlp1] =
                "Compounded GLP-1 products vary in identity and quality and are prescription medicines; BioStack cannot determine safety from available evidence.",
            [HighRiskCategory.PrescriptionOnly] =
                "This is a prescription-only substance; BioStack provides educational context only and cannot advise on use.",
            [HighRiskCategory.BannedInSport] =
                "This substance may be banned in tested sport and carries notable risk; treat this as educational context only.",
            [HighRiskCategory.UnknownIdentity] =
                "Identity and source quality are unknown; this should be treated as an identity/source-quality risk.",
            [HighRiskCategory.UnclearRegulatoryStatus] =
                "Regulatory status is unclear; BioStack cannot determine whether this is appropriate or lawful for a given use.",
        };

    private const string BaselineFraming =
        "This is educational context, not medical advice. BioStack cannot determine safety from available evidence — consider discussing with a qualified professional.";

    /// <summary>
    /// Classify <paramref name="substances"/> and optional <paramref name="categoryHints"/> against the
    /// high-risk taxonomy. Returns a non-high-risk assessment with no framings when nothing matches.
    /// </summary>
    public HighRiskAssessment Assess(
        IEnumerable<string>? substances,
        IEnumerable<string>? categoryHints = null)
    {
        var categories = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var name in substances ?? [])
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var hay = name.ToLowerInvariant();
            foreach (var (keyword, category) in SubstanceSignals)
            {
                if (hay.Contains(keyword, StringComparison.Ordinal))
                    categories.Add(category);
            }
        }

        foreach (var hint in categoryHints ?? [])
        {
            if (string.IsNullOrWhiteSpace(hint)) continue;
            var hay = hint.ToLowerInvariant();
            foreach (var (keyword, category) in CategorySignals)
            {
                if (hay.Contains(keyword, StringComparison.Ordinal))
                    categories.Add(category);
            }
        }

        if (categories.Count == 0)
            return new HighRiskAssessment(false, [], []);

        var framings = new List<string> { BaselineFraming };
        framings.AddRange(categories.Select(c => CategoryFramings[c]));

        return new HighRiskAssessment(true, categories.ToList(), framings);
    }
}
