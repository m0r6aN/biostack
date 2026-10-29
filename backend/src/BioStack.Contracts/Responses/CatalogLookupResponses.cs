namespace BioStack.Contracts.Responses;

/// <summary>A catalog record the name may refer to. <c>Label</c> is "same_as" or "similar". Catalog facts only.</summary>
public sealed record CatalogCandidateResponse(string Slug, string Name, string Kind, string Label, string Reason);

/// <summary>
/// One catalog lookup. <c>Status</c>: pending, matched, no_match, linked, declined. A "matched" lookup is waiting for the
/// person: nothing is ever linked automatically.
/// </summary>
public sealed record CatalogLookupResponse(
    Guid Id,
    Guid ProfileId,
    Guid? CompoundId,
    string QueryName,
    string Status,
    IReadOnlyList<CatalogCandidateResponse> Candidates,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record NoticeResponse(
    Guid Id,
    string Kind,
    string TitleKey,
    /// <summary>The note's values as a JSON object (for example the item name and the catalog name).</summary>
    System.Text.Json.JsonElement Params,
    string? ActionPath,
    Guid? RelatedId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool Read);

public sealed record UnreadCountResponse(int Count);
