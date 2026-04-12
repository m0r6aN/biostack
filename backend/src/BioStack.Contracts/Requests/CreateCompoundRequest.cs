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
    Guid? KnowledgeEntryId = null,
    string Goal = "",
    string Source = "",
    decimal? PricePaid = null,
    Guid? CalculatorResultId = null
);
