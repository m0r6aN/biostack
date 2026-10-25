namespace BioStack.Domain.Entities;

/// <summary>
/// Growth Phase 6: a signed-in user watching a compound, claim or /vs pair for evidence
/// changes. Target ids are public slugs / claim ids, so watches survive record re-imports.
/// </summary>
public sealed class EvidenceWatch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>compound · claim · vs</summary>
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Per-user alert settings, with the one-click unsubscribe token.</summary>
public sealed class EvidenceAlertPreference
{
    public Guid UserId { get; set; }
    /// <summary>instant · weekly · off (verdict changes always honour "off").</summary>
    public string Frequency { get; set; } = "instant";
    public bool EmailEnabled { get; set; } = true;
    public string UnsubscribeToken { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One alert raised for one user (also the in-app notice list). Unique per user, claim,
/// version and type, so a verdict version alerts at most once; also enforces the
/// 1-instant-alert-per-compound-per-24h cap.
/// </summary>
public sealed class EvidenceAlertLogEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>verdict-change · study-added</summary>
    public string AlertType { get; set; } = string.Empty;
    public string CompoundSlug { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string OldVerdict { get; set; } = string.Empty;
    public string NewVerdict { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    /// <summary>pending · emailed · in-app-only · capped</summary>
    public string Delivery { get; set; } = "pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EmailedAtUtc { get; set; }
}
