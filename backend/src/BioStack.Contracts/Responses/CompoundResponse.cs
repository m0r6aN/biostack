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
    bool IsCanonical,
    Guid? KnowledgeEntryId,
    string CanonicalName,
    string Goal = "",
    string Source = "",
    decimal? PricePaid = null
);
