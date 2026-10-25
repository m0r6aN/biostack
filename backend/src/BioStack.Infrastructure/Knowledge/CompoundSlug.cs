namespace BioStack.Infrastructure.Knowledge;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Deterministic lower-kebab-case slug for compound lookups (Lane C runtime side).
///
/// Mirrors the offline worker's <c>SubstanceRecordNormalizer.Slugify</c> so a compound name
/// resolved at request time maps to the same slug the worker persisted on
/// <c>CompoundGraphRelationship.SubjectSlug</c>/<c>ObjectSlug</c>. Keep these in sync.
/// </summary>
public static class CompoundSlug
{
    private static readonly Regex KebabSanitize = new("[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex KebabTrim = new("^-+|-+$", RegexOptions.Compiled);

    public static string From(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var lower = input.Trim().ToLower(CultureInfo.InvariantCulture);
        var replaced = KebabSanitize.Replace(lower, "-");
        return KebabTrim.Replace(replaced, string.Empty);
    }

    /// <summary>
    /// Public dossier slug (<c>/knowledge/{slug}</c>), matching the curated records'
    /// <c>identity.slug</c>: apostrophes are dropped before kebab-casing ("Lion's mane" →
    /// "lions-mane", "NAD+" → "nad"). Must stay identical to <c>compoundSlug()</c> in
    /// <c>frontend/src/lib/compoundSlug.ts</c>. <see cref="From"/> is unchanged so graph slugs
    /// persisted by the worker keep resolving.
    /// </summary>
    public static string ForDossier(string? input) =>
        From(input?.Replace("'", string.Empty).Replace("\u2019", string.Empty));
}
