namespace BioStack.Application.Governance;

/// <summary>
/// CC-1 Doctrine Guard: strips imperative medical phrases from AI-generated
/// narrative before it leaves the SRB endpoint. Non-executable invariant —
/// never claims to diagnose, prescribe, or guarantee outcomes.
///
/// Patterns live in <see cref="DoctrineRuleset"/> so this type and <see cref="PolicyGate"/>
/// cannot drift apart (they previously did: 16 patterns vs 9).
///
/// The parameterless overloads treat text as <see cref="OutputAttribution.Unattributed"/> —
/// the strictest tier — so existing callers keep their current behaviour. Callers that can
/// PROVE the text carries source citations may opt into
/// <see cref="OutputAttribution.SourceAttributed"/>, which permits reporting what a cited
/// source found while still prohibiting personalized direction (F2).
/// </summary>
public sealed class DoctrineSanitizer
{
    private const string Fallback = "[review-required] output contained non-executable doctrine violation";

    public bool ContainsBannedPhrase(string text)
        => ContainsBannedPhrase(text, OutputAttribution.Unattributed);

    public bool ContainsBannedPhrase(string text, OutputAttribution attribution)
        => DoctrineRuleset.IsProhibited(text, attribution);

    public string SanitizeFinding(string text)
        => SanitizeFinding(text, OutputAttribution.Unattributed);

    public string SanitizeFinding(string text, OutputAttribution attribution)
        => ContainsBannedPhrase(text, attribution) ? Fallback : text;

    public IEnumerable<string> SanitizeAll(IEnumerable<string> texts)
        => texts.Select(SanitizeFinding);

    public IEnumerable<string> SanitizeAll(IEnumerable<string> texts, OutputAttribution attribution)
        => texts.Select(text => SanitizeFinding(text, attribution));
}
