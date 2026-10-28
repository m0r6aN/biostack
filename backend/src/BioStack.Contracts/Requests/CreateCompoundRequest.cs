namespace BioStack.Contracts.Requests;

using BioStack.Domain.Enums;

public sealed record CreateCompoundRequest(
    string Name,
    CompoundCategory Category,
    DateTime? StartDate,
    DateTime? EndDate,
    CompoundStatus Status,
    string Notes,
    SourceType SourceType,
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
    // Catalog link the user picked (a catalog slug). The server validates it against the live catalog and
    // derives the category and verification state itself; a client-sent category is ignored when linked.
    // On update: null keeps the existing link, an empty string removes it.
    string? CatalogSlug = null,
    // Per-item provider-summary toggle. null = default on at create, unchanged on update.
    bool? IncludeInProviderSummary = null
);
