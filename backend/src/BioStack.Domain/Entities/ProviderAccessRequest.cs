namespace BioStack.Domain.Entities;

public sealed class ProviderAccessRequest
{
    /// <summary>Redaction marker written into name/organization/role on anonymization.</summary>
    public const string RedactedMarker = "[redacted]";

    /// <summary>
    /// Domain suffix for the per-row redacted email. A fixed literal marker cannot be reused for
    /// <see cref="Email"/> because the table enforces a unique index on it; this keeps the value
    /// obviously non-PII and deterministic while remaining unique per row.
    /// </summary>
    private const string RedactedEmailDomain = "@redacted.invalid";

    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? Owner { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public DateTime ConsentRecordedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// PR-PROV-001 SG4/R6: pure, deterministic eligibility check for the admin-invoked retention
    /// sweep. True only for <c>closed</c> requests whose <see cref="UpdatedAtUtc"/> is older than
    /// <paramref name="retentionDays"/> relative to <paramref name="nowUtc"/>. Already-anonymized
    /// rows (email already the redacted marker) are not re-eligible.
    /// </summary>
    public bool IsEligibleForRetentionAnonymization(DateTime nowUtc, int retentionDays)
    {
        if (!string.Equals(Status, "closed", StringComparison.Ordinal))
            return false;

        if (Email.EndsWith(RedactedEmailDomain, StringComparison.Ordinal))
            return false;

        var ageDays = (nowUtc - UpdatedAtUtc).TotalDays;
        return ageDays > retentionDays;
    }

    /// <summary>
    /// PR-PROV-001 SG4/R6: pure anonymization — replaces email/name/organization/role with the
    /// fixed redacted marker. <see cref="Status"/>, <see cref="Owner"/>, every timestamp, and
    /// <see cref="ConsentVersion"/>/<see cref="ConsentRecordedAtUtc"/> are retained for audit.
    /// Does not touch <see cref="UpdatedAtUtc"/> itself — the caller decides whether anonymization
    /// counts as an update for its own bookkeeping.
    /// </summary>
    public void AnonymizeForRetention()
    {
        Email = $"redacted-{Id:N}{RedactedEmailDomain}";
        Name = RedactedMarker;
        Organization = RedactedMarker;
        Role = RedactedMarker;
    }
}
