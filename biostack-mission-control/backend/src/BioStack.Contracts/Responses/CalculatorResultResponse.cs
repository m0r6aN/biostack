namespace BioStack.Contracts.Responses;

public sealed record CalculatorResultResponse(
    decimal Input,
    decimal Output,
    string Unit,
    string Formula,
    string Disclaimer
);
