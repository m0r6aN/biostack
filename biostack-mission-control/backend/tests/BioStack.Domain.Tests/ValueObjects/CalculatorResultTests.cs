namespace BioStack.Domain.Tests.ValueObjects;

using Xunit;
using BioStack.Domain.ValueObjects;

public class CalculatorResultTests
{
    [Fact]
    public void Create_WithValidInputs_SetsPropertiesCorrectly()
    {
        var input = 100m;
        var output = 50m;
        var unit = "mg";
        var formula = "Output = Input / 2";

        var result = CalculatorResult.Create(input, output, unit, formula);

        Assert.Equal(input, result.Input);
        Assert.Equal(output, result.Output);
        Assert.Equal(unit, result.Unit);
        Assert.Equal(formula, result.Formula);
        Assert.Equal("This is a mathematical calculation only. Not medical advice.", result.Disclaimer);
    }

    [Fact]
    public void Create_WithCustomDisclaimer_UsesCustomDisclaimer()
    {
        var customDisclaimer = "Custom medical disclaimer text";

        var result = CalculatorResult.Create(10m, 20m, "mg", "formula", customDisclaimer);

        Assert.Equal(customDisclaimer, result.Disclaimer);
    }

    [Fact]
    public void Create_WithNullCustomDisclaimer_UsesDefaultDisclaimer()
    {
        var result = CalculatorResult.Create(10m, 20m, "mg", "formula", null);

        Assert.Equal("This is a mathematical calculation only. Not medical advice.", result.Disclaimer);
    }
}
