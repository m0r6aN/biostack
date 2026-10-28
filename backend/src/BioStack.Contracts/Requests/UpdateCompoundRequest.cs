namespace BioStack.Contracts.Requests;

using BioStack.Domain.Enums;

public sealed record UpdateCompoundRequest(
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
    IReadOnlyList<string>? FrequencyTimesOfDay = null
);
