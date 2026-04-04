namespace BioStack.Contracts.Requests;

public sealed record ConversionRequest(
    decimal Amount,
    string FromUnit,
    string ToUnit,
    decimal? ConversionFactor = null
);
