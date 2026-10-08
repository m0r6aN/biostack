namespace BioStack.Contracts.Responses;

public sealed record ProviderAccessConfirmationResponse(
    Guid RequestId,
    string Status,
    DateTime SubmittedAtUtc);

public sealed record ProviderAccessReviewResponse(
    Guid RequestId,
    string Email,
    string Name,
    string Organization,
    string Role,
    string Status,
    string? Owner,
    string ConsentVersion,
    DateTime ConsentRecordedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    // PR-PROV-001 SG4/R6: read-only, deterministically computed SLA staleness. Additive —
    // neither field is persisted; both are derived at read time from CreatedAtUtc/Status plus
    // the configured ProviderAccess:SlaDays threshold.
    int DaysOpen,
    bool IsOverdue);

// PR-PROV-001: manually-invoked admin retention sweep result. Reports counts only — no PII.
public sealed record ProviderAccessRetentionSweepResponse(
    int EligibleCount,
    int AnonymizedCount,
    int RetentionDays,
    DateTime SweepPerformedAtUtc);
