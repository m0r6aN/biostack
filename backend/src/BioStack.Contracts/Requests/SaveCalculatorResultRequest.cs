namespace BioStack.Contracts.Requests;

public sealed record SaveCalculatorResultRequest(
    string CalculatorKind,
    Dictionary<string, string> Inputs,
    Dictionary<string, string> Outputs,
    string Unit,
    string Formula,
    string DisplaySummary,
    Guid? CompoundRecordId = null
);
