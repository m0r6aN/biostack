namespace BioStack.Domain.Entities;

/// <summary>
/// A short in-app note for one person, shown in the header bell and at their next sign-in. Owned by the user and
/// deleted with their account. Any feature creates one through <c>INoticeService.UpsertAsync</c>; a repeated
/// <see cref="DedupeKey"/> updates the same note instead of stacking another.
/// </summary>
public sealed class UserNotice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>What the note is about, e.g. <see cref="UserNoticeKind.CatalogMatch"/>. The app renders the text from this and <see cref="ParamsJson"/>.</summary>
    public string Kind { get; set; } = string.Empty;
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>Values for the text, as JSON. May hold the user's own words (an item name); shown only to them after sign-in.</summary>
    public string ParamsJson { get; set; } = "{}";

    /// <summary>A relative path inside the app, or null. Never an absolute or external URL.</summary>
    public string? ActionPath { get; set; }
    public Guid? RelatedId { get; set; }

    public string DedupeKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
    public DateTime? DismissedAtUtc { get; set; }
}

public static class UserNoticeKind
{
    /// <summary>A "Same as" or "Similar" catalog item is waiting for the person to confirm.</summary>
    public const string CatalogMatch = "catalog_match";
}
