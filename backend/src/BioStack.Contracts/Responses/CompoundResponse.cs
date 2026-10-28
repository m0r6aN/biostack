namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record CompoundResponse(
    Guid Id,
    Guid PersonId,
    string Name,
    CompoundCategory Category,
    DateTime? StartDate,
    DateTime? EndDate,
    CompoundStatus Status,
    string Notes,
    SourceType SourceType,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string Goal = "",
    string Source = "",
    decimal? PricePaid = null,
    decimal? DoseAmount = null,
    string? DoseUnit = null,
    string? DoseFrequency = null,
    string? DoseTime = null,
    string? FrequencyKind = null,
    int? FrequencyIntervalDays = null,
    IReadOnlyList<int>? FrequencyWeekdays = null,
    int? FrequencyTimesPerWeek = null,
    DateTime? FrequencyStartDate = null,
    IReadOnlyList<string>? FrequencyTimesOfDay = null,
    // Server-derived, response-only: "catalog" (linked) or "unverified"; "catalog" or "user".
    string? CatalogSlug = null,
    string? VerificationState = null,
    string? CategorySource = null,
    bool IncludeInProviderSummary = true
);
