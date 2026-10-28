namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;

/// <summary>
/// Which active items count toward the Observer cap (spec v5, D4 narrowed by D6).
/// An item is outside the cap only when it is confirmed to be an everyday item: linked to a catalog
/// record whose catalog category is Supplement, Vitamin, Mineral, AminoAcid or Nutraceutical. Everything
/// else counts: research or other catalog categories, Coenzyme, and every unverified (custom) item
/// whatever category it carries. A category the user typed is never trusted; only a server-set
/// CategorySource of "catalog" is.
/// </summary>
public static class ObserverCapRules
{
    public const string StateCatalog = "catalog";
    public const string StateUnverified = "unverified";
    public const string SourceCatalog = "catalog";
    public const string SourceUser = "user";

    public static readonly IReadOnlySet<CompoundCategory> ExemptCategories = new HashSet<CompoundCategory>
    {
        CompoundCategory.Supplement,
        CompoundCategory.Vitamin,
        CompoundCategory.Mineral,
        CompoundCategory.AminoAcid,
        CompoundCategory.Nutraceutical,
    };

    /// <summary>Legacy rows (NULL state) read as catalog when linked, otherwise unverified.</summary>
    public static string EffectiveState(string? catalogSlug, string? verificationState) =>
        !string.IsNullOrWhiteSpace(verificationState)
            ? verificationState
            : string.IsNullOrWhiteSpace(catalogSlug) ? StateUnverified : StateCatalog;

    /// <summary>NULL reads as "user".</summary>
    public static string EffectiveCategorySource(string? categorySource) =>
        string.IsNullOrWhiteSpace(categorySource) ? SourceUser : categorySource;

    /// <param name="catalogCategory">The linked catalog record's current category when it can be resolved; otherwise the category stored at link time.</param>
    public static bool IsExempt(CompoundRecord record, CompoundCategory catalogCategory) =>
        !string.IsNullOrWhiteSpace(record.CatalogSlug)
        && EffectiveState(record.CatalogSlug, record.VerificationState) == StateCatalog
        && EffectiveCategorySource(record.CategorySource) == SourceCatalog
        && ExemptCategories.Contains(catalogCategory);
}
