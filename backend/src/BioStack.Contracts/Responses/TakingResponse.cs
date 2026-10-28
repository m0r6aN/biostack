namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

/// <summary>Everything I'm Taking: the user's own items with their catalog link status.</summary>
public sealed record TakingResponse(
    IReadOnlyList<TakingItemResponse> Items,
    TakingCapResponse Cap
);

public sealed record TakingItemResponse(
    CompoundResponse Compound,
    // True only for an active item that occupies one of the Observer slots (server-derived).
    bool CountsTowardCap,
    // Read-time "this may be in BioStack" offers for an unlinked item. Offers only: linking is always the
    // user's own confirmed action (an update with catalogSlug), never automatic.
    IReadOnlyList<CatalogSuggestionResponse> Suggestions
);

public sealed record CatalogSuggestionResponse(
    string Slug,
    string Name,
    CompoundCategory Category,
    // "name" or "alias": an exact (normalized) match, never fuzzy.
    string MatchedOn
);

public sealed record TakingCapResponse(
    // Observer limit for active research or unconfirmed items; null when the plan has no limit.
    int? Limit,
    int Counted
);
