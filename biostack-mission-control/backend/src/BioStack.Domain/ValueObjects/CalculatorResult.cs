namespace BioStack.Domain.ValueObjects;

public sealed record CalculatorResult(
    decimal Input,
    decimal Output,
    string Unit,
    string Formula,
    string Disclaimer
)
{
    public static CalculatorResult Create(
        decimal input,
        decimal output,
        string unit,
        string formula,
        string? customDisclaimer = null)
    {
        const string DefaultDisclaimer = "This is a mathematical calculation only. Not medical advice.";
        var disclaimer = customDisclaimer ?? DefaultDisclaimer;

        return new CalculatorResult(input, output, unit, formula, disclaimer);
    }
}
