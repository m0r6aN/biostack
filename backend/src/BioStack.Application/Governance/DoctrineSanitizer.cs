namespace BioStack.Application.Governance;

using System.Text.RegularExpressions;

/// <summary>
/// CC-1 Doctrine Guard: strips imperative medical phrases from AI-generated
/// narrative before it leaves the SRB endpoint. Non-executable invariant —
/// never claims to diagnose, prescribe, or guarantee outcomes.
/// </summary>
public sealed class DoctrineSanitizer
{
    // Class D personalized medical direction patterns (Guidance Content Contract v1).
    private static readonly Regex[] BannedPatterns =
    [
        new(@"\byou\s+should\b",             RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\byou\s+must\b",               RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\btake\s+\d+\s*(mg|mcg|g)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bdose\s+at\b",                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bis\s+safe\b",                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bsafe\s+for\s+you\b",         RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bwill\s+treat\b",             RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bcures?\b",                   RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bproven\s+to\b",              RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bstop\s+taking\b",            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bstart\s+at\b",               RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bincrease\s+to\b",            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bthe\s+best\s+dose\s+for\s+you\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\brecommended\s+dose\s+for\s+your\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bai\s+recommends\b",          RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\byou\s+should\s+take\b",      RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    private const string Fallback = "[review-required] output contained non-executable doctrine violation";

    public bool ContainsBannedPhrase(string text)
        => BannedPatterns.Any(p => p.IsMatch(text));

    public string SanitizeFinding(string text)
        => ContainsBannedPhrase(text) ? Fallback : text;

    public IEnumerable<string> SanitizeAll(IEnumerable<string> texts)
        => texts.Select(SanitizeFinding);
}
