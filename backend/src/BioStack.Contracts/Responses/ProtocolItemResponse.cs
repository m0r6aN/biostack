namespace BioStack.Contracts.Responses;

public sealed record ProtocolItemResponse(
    Guid Id,
    Guid ProtocolId,
    Guid CompoundRecordId,
    Guid? CalculatorResultId,
    string Notes,
    CompoundResponse? Compound
);
