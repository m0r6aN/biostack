namespace BioStack.Contracts.Responses;

/// <summary>
/// Public-safe trust state for a compound.
/// Internal-only fields (analyst notes, raw scrape, internal IDs) are excluded.
/// </summary>
public sealed record TrustLedgerResponse(
    string Slug,
    string CanonicalName,
    string EvidenceTier,        // "strong" | "moderate" | "limited" | "mechanistic" | "unknown"
    string Completeness,        // "complete" | "partial" | "minimal"
    bool NeedsReview,
    IReadOnlyList<string> QualityFlags,
    string RegulatoryBoundary,
    IReadOnlyList<TrustLedgerClaim> Claims,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<string> PromotionBlockers,
    IReadOnlyList<string> RequiredNextActions,
    string Status);             // "promoted" | "review-gated" | "not-found"

public sealed record TrustLedgerClaim(
    string ClaimText,
    string Confidence,          // "high" | "moderate" | "low" | "insufficient"
    IReadOnlyList<string> SourceRefs,
    string? ExtractedQuote,
    IReadOnlyList<string> ReviewFlags);
