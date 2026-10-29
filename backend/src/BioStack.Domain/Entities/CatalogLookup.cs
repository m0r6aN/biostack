namespace BioStack.Domain.Entities;

/// <summary>
/// One person's question "is this item already in BioStack?", for a name they typed. Owned by the user and deleted
/// with their account. Unique per (user, normalized name): asking again returns the stored row. Phase 3 runs the internal
/// catalog match only: nothing is sent outside BioStack and nothing is submitted anywhere.
/// </summary>
public sealed class CatalogLookup
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PersonId { get; set; }

    /// <summary>The user's own item this lookup is about. Cleared (not deleted) if the item is deleted.</summary>
    public Guid? CompoundRecordId { get; set; }

    /// <summary>The name, normalized for matching (lowercase letters and digits).</summary>
    public string NameKey { get; set; } = string.Empty;

    /// <summary>The name as typed, cleaned (max 60).</summary>
    public string QueryName { get; set; } = string.Empty;

    /// <summary>See <see cref="CatalogLookupStatus"/>.</summary>
    public string Status { get; set; } = CatalogLookupStatus.Pending;

    /// <summary>The best match, mirrored from the first entry of <see cref="CandidatesJson"/>.</summary>
    public string? MatchSlug { get; set; }
    public string? MatchKind { get; set; }
    public string? MatchLabel { get; set; }
    public float? MatchScore { get; set; }

    /// <summary>Top candidates as JSON (slug, name, kind, label, reason). Catalog facts only, never user data.</summary>
    public string? CandidatesJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? LastCheckedAtUtc { get; set; }
}

public static class CatalogLookupStatus
{
    /// <summary>Created, waiting for the matcher.</summary>
    public const string Pending = "pending";
    /// <summary>The matcher found at least one "Same as" or "Similar" candidate; waiting for the person.</summary>
    public const string Matched = "matched";
    /// <summary>The matcher found nothing in BioStack's library. The item stays Unverified.</summary>
    public const string NoMatch = "no_match";
    /// <summary>The person confirmed a candidate and the item is now linked.</summary>
    public const string Linked = "linked";
    /// <summary>The person said "Not the same".</summary>
    public const string Declined = "declined";
}

public static class CatalogMatchKind
{
    public const string Exact = "exact";
    public const string Alias = "alias";
    public const string BrandOrSynonym = "brand_or_synonym";
    public const string Slug = "slug";
    public const string Partial = "partial";
    public const string Fuzzy = "fuzzy";
}

public static class CatalogMatchLabel
{
    public const string SameAs = "same_as";
    public const string Similar = "similar";
}
