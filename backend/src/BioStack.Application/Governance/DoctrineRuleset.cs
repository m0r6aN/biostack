namespace BioStack.Application.Governance;

using System.Text.RegularExpressions;

/// <summary>
/// How much provenance a piece of output carries. Determines which doctrine tier applies.
/// Defaults to <see cref="Unattributed"/> so callers must PROVE attribution — fail-closed.
/// </summary>
public enum OutputAttribution
{
    /// <summary>
    /// BioStack-authored narrative with no source carried alongside it. Strictest tier:
    /// both personalized direction and unattributed subject claims are prohibited.
    /// </summary>
    Unattributed = 0,

    /// <summary>
    /// The statement carries source citations and was produced from reviewed structure
    /// (a template or a source-backed field), not free-form model prose. Personalized
    /// direction is STILL prohibited; reporting what a cited source found is permitted.
    /// </summary>
    SourceAttributed = 1,
}

/// <summary>
/// Single source of truth for Guidance Content Contract v1 doctrine patterns.
///
/// Two tiers, because the contract distinguishes SPEAKER from SUBJECT:
///
///   • <see cref="PersonalizedDirection"/> — prohibited unconditionally. These address the user
///     in the second person or issue a recommendation ("you should", "safe for you",
///     "AI recommends", "start at", "take 5 mg"). A citation does not redeem them: the contract's
///     own Class A examples deliberately avoid imperatives even when reporting a source
///     ("Reviewed trials initiated participants between 0.5 and 1.0 mg weekly").
///
///   • <see cref="AttributionSensitiveClaim"/> — prohibited only when UNATTRIBUTED. These are
///     claims about the subject ("is safe", "cures", "proven to", "will treat"). As BioStack's
///     own assertion they are Class D; as a report of what a cited trial found they are Class A
///     published evidence context, which the contract explicitly permits.
///
/// This closes F2. The blocklist previously could not tell "BioStack says it is safe"
/// (prohibited) from "the cited trial reported it is safe" (permitted), so it suppressed the
/// evidence language the product exists to surface. Note this makes the CODE match the
/// CONTRACT — Class A always permitted source-backed evidence context — so no contract version
/// bump is required.
///
/// Both <see cref="DoctrineSanitizer"/> and <see cref="PolicyGate"/> consume this type;
/// <c>DoctrineRulesetParityTests</c> fails CI if they ever diverge again (F4).
/// </summary>
public static class DoctrineRuleset
{
    private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.Compiled;

    /// <summary>
    /// Class D personalized medical direction. Prohibited regardless of attribution.
    /// </summary>
    public static readonly IReadOnlyList<Regex> PersonalizedDirection =
    [
        new(@"\byou\s+should\b",                    Opts),
        new(@"\byou\s+must\b",                      Opts),
        new(@"\byou\s+should\s+take\b",             Opts),
        new(@"\btake\s+\d+\s*(mg|mcg|g)\b",         Opts),
        new(@"\bdose\s+at\b",                       Opts),
        new(@"\bsafe\s+for\s+you\b",                Opts),
        new(@"\bthe\s+best\s+dose\s+for\s+you\b",   Opts),
        new(@"\brecommended\s+dose\s+for\s+your\b", Opts),
        new(@"\bai\s+recommends\b",                 Opts),
        new(@"\bstop\s+taking\b",                   Opts),
        new(@"\bstart\s+at\b",                      Opts),
        new(@"\bincrease\s+to\b",                   Opts),
    ];

    /// <summary>
    /// Subject claims that are Class D as BioStack's own assertion but Class A when reporting
    /// a cited source. Prohibited only for <see cref="OutputAttribution.Unattributed"/> output.
    /// </summary>
    public static readonly IReadOnlyList<Regex> AttributionSensitiveClaim =
    [
        new(@"\bis\s+safe\b",    Opts),
        new(@"\bcures?\b",       Opts),
        new(@"\bproven\s+to\b",  Opts),
        new(@"\bwill\s+treat\b", Opts),
    ];

    /// <summary>
    /// Requests BioStack must refuse to act on: sourcing/procurement, administration how-to,
    /// and explicit dosing-instruction seeking.
    ///
    /// Scope is deliberately narrow. "Should I stop taking this before surgery?" is a legitimate
    /// safety question and must reach evidence context plus clinician framing, not a refusal.
    /// "How much should I take?" seeks an instruction BioStack may not give, and is refused.
    /// </summary>
    public static readonly IReadOnlyList<Regex> UnsafeRequestIntent =
    [
        new(@"\bwhere\s+(can|do|to)\b.*\b(buy|get|order|source|purchase)\b",     Opts),
        new(@"\bhow\s+(do|to|can)\b.*\b(inject|administer|reconstitute|dose)\b", Opts),
        new(@"\b(buy|order|source)\b.*\b(online|vendor|supplier|gray\s*market|grey\s*market)\b", Opts),
        new(@"\binject(ion|ing)?\b.*\b(protocol|schedule|site|how)\b",           Opts),
        // Dosing-instruction seeking — compensates for no longer screening input with output doctrine.
        new(@"\b(how\s+much|what\s+dose|what\s+dosage)\b.{0,60}\b(should|do|can)\s+i\b", Opts),
        new(@"\b(should|do)\s+i\s+(take|use|inject)\b.{0,40}\b\d+\s*(mg|mcg|g|iu|units)\b", Opts),
    ];

    /// <summary>True when the text issues Class D personalized medical direction.</summary>
    public static bool MatchesPersonalizedDirection(string text)
        => !string.IsNullOrWhiteSpace(text)
           && PersonalizedDirection.Any(p => p.IsMatch(text));

    /// <summary>True when the text asserts a subject claim that requires attribution.</summary>
    public static bool MatchesAttributionSensitiveClaim(string text)
        => !string.IsNullOrWhiteSpace(text)
           && AttributionSensitiveClaim.Any(p => p.IsMatch(text));

    /// <summary>
    /// The doctrine decision for one piece of output. Personalized direction is always
    /// prohibited; subject claims are prohibited only when the output carries no attribution.
    /// </summary>
    public static bool IsProhibited(string text, OutputAttribution attribution = OutputAttribution.Unattributed)
    {
        if (MatchesPersonalizedDirection(text))
            return true;

        return attribution != OutputAttribution.SourceAttributed
               && MatchesAttributionSensitiveClaim(text);
    }

    /// <summary>
    /// True when a user request seeks sourcing, administration, or dosing instructions.
    /// Output doctrine is intentionally NOT consulted here (F6).
    /// </summary>
    public static bool MatchesUnsafeRequestIntent(string text)
        => !string.IsNullOrWhiteSpace(text)
           && UnsafeRequestIntent.Any(p => p.IsMatch(text));
}
