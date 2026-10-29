namespace BioStack.Api.Endpoints;

using System.Text.RegularExpressions;
using BioStack.Application.Services;

/// <summary>
/// Cleans a typed name before it becomes a catalog lookup. One item name only: dose numbers and seller words are removed,
/// and anything that looks like a link, handle, email or sentence is refused. Same rules the claim checker uses.
/// </summary>
public static partial class CatalogLookupName
{
    // Letters (any language), digits, space and - + . ' ( )
    [GeneratedRegex(@"^[\p{L}\p{M}\p{N} \-+.'()]+$")]
    private static partial Regex AllowedCharacters();

    public static bool TryClean(string? raw, out string cleaned, out string error)
    {
        cleaned = string.Empty;
        error = "Enter the name of one item, between 2 and 60 characters.";
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 200) return false;
        if (raw.Contains('@') || raw.Contains("://", StringComparison.Ordinal)
            || raw.Contains("www.", StringComparison.OrdinalIgnoreCase)) return false;

        var (text, _) = ClaimSubmissionSanitizer.Clean(raw, 200);
        text = Regex.Replace(text.Replace(ClaimSubmissionSanitizer.Removed, " "), @"\s+", " ").Trim();
        if (text.Length < CatalogLookupService.MinNameLength || text.Length > CatalogLookupService.MaxNameLength) return false;
        if (!text.Any(char.IsLetter) || !AllowedCharacters().IsMatch(text)) return false;

        cleaned = text;
        return true;
    }
}
