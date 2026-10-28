namespace BioStack.Contracts.Responses;

public sealed record DoseLogEntryResponse(
    Guid Id,
    Guid CompoundId,
    string CompoundName,
    string LogDate,
    DateTime TakenAtUtc,
    decimal Amount,
    string Unit,
    string? Note);
