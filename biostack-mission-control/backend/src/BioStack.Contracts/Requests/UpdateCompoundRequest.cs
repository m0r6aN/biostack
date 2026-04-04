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
    decimal? PricePaid = null
);
