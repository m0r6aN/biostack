namespace BioStack.Contracts.Responses;

public sealed record CalculatorResultRecordResponse(
    Guid Id,
    Guid PersonId,
    Guid? CompoundRecordId,
    string CalculatorKind,
    Dictionary<string, string> Inputs,
    Dictionary<string, string> Outputs,
    string Unit,
    string Formula,
    string DisplaySummary,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
