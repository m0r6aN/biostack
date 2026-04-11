namespace BioStack.Contracts.Responses;

public sealed record ProtocolPhaseResponse(
    Guid Id,
    Guid PersonId,
    string Name,
    DateTime? StartDate,
    DateTime? EndDate,
    string Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
