namespace BioStack.Domain.Entities;

/// <summary>
/// Growth Phase 7 (03 §7): one claim-checker query. Stored hashed (no raw text, no user
/// identity) with the claim it matched, so unmatched demand can feed the editorial queue.
/// </summary>
public sealed class ClaimCheckQuery
{
    public Guid Id { get; set; }
    /// <summary>SHA-256 hex of the normalized query.</summary>
    public string QueryHash { get; set; } = string.Empty;
    public string? MatchedClaimId { get; set; }
    public bool DoseIntent { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// "Submit a claim you saw" (03 §7 no-match flow, 03 §9 input). Free text is sanitized on the
/// way in (no doses, currency, URLs, handles or seller names). No URL, creator, photo or dose
/// fields exist. Moderation and theme fields are used by the community columns (Phase 9).
/// </summary>
public sealed class ClaimSubmission
{
    public Guid Id { get; set; }
    public string CompoundSlug { get; set; } = string.Empty;
    /// <summary>The claim in the submitter's words, sanitized, ≤ 280 chars.</summary>
    public string ClaimText { get; set; } = string.Empty;
    /// <summary>instagram · tiktok · youtube · x · reddit · forum · friend · other</summary>
    public string SeenOn { get; set; } = "other";
    /// <summary>side-effect · benefit · nothing · null</summary>
    public string? Experience { get; set; }
    /// <summary>Sanitized, ≤ 140 chars.</summary>
    public string? ThemeText { get; set; }
    public bool Redacted { get; set; }
    /// <summary>Emergency-sounding text: the submitter saw the safety message; flagged for editorial review.</summary>
    public bool Emergency { get; set; }
    /// <summary>Salted hash of the submitter (user id, or client address + day) so k-thresholds count independent people.</summary>
    public string SubmitterKey { get; set; } = string.Empty;
    /// <summary>pending · approved · rejected</summary>
    public string Status { get; set; } = "pending";
    public string? Theme { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModeratedAtUtc { get; set; }
}
