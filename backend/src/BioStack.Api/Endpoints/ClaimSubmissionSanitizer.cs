namespace BioStack.Api.Endpoints;

using System.Text.RegularExpressions;

/// <summary>
/// 03 §9 processing step 1: strip numbers+units, currency, URLs, @handles and seller names,
/// replacing each with "[removed]". Also detects emergency-sounding text (06 §C).
/// </summary>
public static partial class ClaimSubmissionSanitizer
{
    public const string Removed = "[removed]";
    public const string RedactedNotice =
        "We stripped the dose numbers and seller details. That's policy, not personal. The claim itself is in the queue.";
    public const string SafetyMessage =
        "If this is happening now, contact emergency services or poison control.";

    // Maintained list of seller / storefront words; extend as moderators find more.
    private static readonly string[] SellerTerms =
    [
        "amazon", "iherb", "vitacost", "ebay", "etsy", "aliexpress", "alibaba", "shopify", "walmart",
        "peptide sciences", "amino asylum", "swiss chems", "limitless life", "core peptides", "biotech peptides",
        "discount code", "coupon", "promo code", "use code", "link in bio",
    ];

    [GeneratedRegex(@"https?://\S+|www\.\S+|\b[a-z0-9-]+\.(?:com|net|org|io|co|shop|store|us|uk|ca|xyz)\b\S*", RegexOptions.IgnoreCase)]
    private static partial Regex Url();
    [GeneratedRegex(@"(?<![\w@])@[A-Za-z0-9_.]{2,}")]
    private static partial Regex Handle();
    [GeneratedRegex(@"[$€£¥]\s?\d[\d,.]*|\b\d[\d,.]*\s?(?:usd|dollars?|bucks|eur|gbp)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Currency();
    [GeneratedRegex(@"\b\d+(?:[.,]\d+)?(?:\s?(?:[-–/]|to)\s?\d+(?:[.,]\d+)?)*\s?(?:mg|mcg|µg|ug|iu|units?|ml|cc|g|grams?|tabs?|caps?|pills?|clicks?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Dose();
    [GeneratedRegex(@"\b(?:chest pain|can'?t breathe|cannot breathe|trouble breathing|hospitali[sz]ed|in the er\b|emergency room|seizure|fainted|passed out|overdose|suicid\w*|anaphyla\w*|heart attack|stroke)\b", RegexOptions.IgnoreCase)]
    private static partial Regex EmergencyPattern();

    public static (string Text, bool Redacted) Clean(string? input, int maxLength)
    {
        var original = Regex.Replace((input ?? string.Empty).Trim(), @"\s+", " ");
        var text = Url().Replace(original, Removed);
        text = Handle().Replace(text, Removed);
        text = Currency().Replace(text, Removed);
        text = Dose().Replace(text, Removed);
        foreach (var term in SellerTerms)
            text = Regex.Replace(text, $@"\b{Regex.Escape(term)}\b", Removed, RegexOptions.IgnoreCase);
        var redacted = !string.Equals(text, original, StringComparison.Ordinal);
        if (text.Length > maxLength) text = text[..maxLength].TrimEnd();
        return (text, redacted);
    }

    public static bool IsEmergency(params string?[] texts) => texts.Any(t => !string.IsNullOrEmpty(t) && EmergencyPattern().IsMatch(t));
}
