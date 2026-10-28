namespace BioStack.Application.Tests.Services;

using System.Text.Json;
using Xunit;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public class CalculatorServiceTests
{
    private readonly CalculatorService _service = new();

    [Fact]
    public void CalculateReconstitution_WithValidInputs_ReturnsCorrectResult()
    {
        var request = new ReconstitutionRequest(5m, 2.5m);

        var result = _service.CalculateReconstitution(request);

        Assert.Equal(5m, result.Input);
        Assert.Equal(2000m, result.Output);
        Assert.Equal("mcg/mL", result.Unit);
        Assert.Equal("Concentration = (Peptide mg * 1000) / Diluent mL", result.Formula);
        Assert.Contains("This is a mathematical calculation only", result.Disclaimer);
    }

    [Fact]
    public void CalculateReconstitution_WithZeroPeptideAmount_ThrowsArgumentException()
    {
        var request = new ReconstitutionRequest(0m, 2.5m);

        var ex = Assert.Throws<ArgumentException>(() => _service.CalculateReconstitution(request));
        Assert.Contains("greater than 0", ex.Message);
    }

    [Fact]
    public void CalculateReconstitution_WithZeroDiluentVolume_ThrowsArgumentException()
    {
        var request = new ReconstitutionRequest(5m, 0m);

        var ex = Assert.Throws<ArgumentException>(() => _service.CalculateReconstitution(request));
        Assert.Contains("greater than 0", ex.Message);
    }

    [Fact]
    public void CalculateVolume_WithValidInputs_ReturnsCorrectResult()
    {
        var request = new VolumeRequest(100m, 10m);

        var result = _service.CalculateVolume(request);

        Assert.Equal(100m, result.Input);
        Assert.Equal(10m, result.Output);
        Assert.Equal("mL", result.Unit);
        Assert.Contains("This is a mathematical calculation only", result.Disclaimer);
    }

    [Fact]
    public void CalculateVolume_WithZeroDesiredDose_ThrowsArgumentException()
    {
        var request = new VolumeRequest(0m, 10m);

        var ex = Assert.Throws<ArgumentException>(() => _service.CalculateVolume(request));
        Assert.Contains("greater than 0", ex.Message);
    }

    [Fact]
    public void CalculateVolume_WithZeroConcentration_ThrowsArgumentException()
    {
        var request = new VolumeRequest(100m, 0m);

        var ex = Assert.Throws<ArgumentException>(() => _service.CalculateVolume(request));
        Assert.Contains("greater than 0", ex.Message);
    }

    [Fact]
    public void CalculateConversion_WithValidInputs_ReturnsCorrectResult()
    {
        var request = new ConversionRequest(1000m, "mg", "mcg");

        var result = _service.CalculateConversion(request);

        Assert.Equal(1000m, result.Input);
        Assert.Equal(1000000m, result.Output);
        Assert.Equal("mcg", result.Unit);
        Assert.Contains("This is a mathematical calculation only", result.Disclaimer);
    }

    [Fact]
    public void CalculateConversion_UsesFixedUnitTable()
    {
        var request = new ConversionRequest(2m, "mg", "mcg");

        var result = _service.CalculateConversion(request);

        Assert.Equal(2m, result.Input);
        Assert.Equal(2000m, result.Output);
        Assert.Equal("mcg", result.Unit);
        Assert.Contains("mg to mcg", result.Formula);
    }

    [Fact]
    public void CalculateConversion_WithUnsupportedUnit_ThrowsArgumentException()
    {
        var request = new ConversionRequest(1m, "iu", "mg");

        var ex = Assert.Throws<ArgumentException>(() => _service.CalculateConversion(request));
        Assert.Contains("Unsupported conversion", ex.Message);
    }

    [Fact]
    public void CalculateConversion_WithZeroAmount_ThrowsArgumentException()
    {
        var request = new ConversionRequest(0m, "mg", "mcg");

        var ex = Assert.Throws<ArgumentException>(() => _service.CalculateConversion(request));
        Assert.Contains("greater than 0", ex.Message);
    }

    [Fact]
    public void ConversionRequest_HasNoClientSuppliedFactor()
    {
        Assert.DoesNotContain(
            typeof(ConversionRequest).GetProperties(),
            p => p.Name.Contains("Factor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConversionRequest_WithConversionFactorInJson_IsRejectedAtDeserialization()
    {
        // Same System.Text.Json behavior ASP.NET minimal APIs use to bind the body. A request that tries to
        // override the unit table must fail to bind (HTTP 400), not be ignored and not be honoured.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        const string tampered = "{\"amount\":1,\"fromUnit\":\"mg\",\"toUnit\":\"mcg\",\"conversionFactor\":5}";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConversionRequest>(tampered, options));
    }

    [Fact]
    public void ConversionRequest_WithoutFactorInJson_Deserializes()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var request = JsonSerializer.Deserialize<ConversionRequest>("{\"amount\":1,\"fromUnit\":\"mg\",\"toUnit\":\"mcg\"}", options);

        Assert.NotNull(request);
        Assert.Equal(1000m, _service.CalculateConversion(request!).Output);
    }

    [Fact]
    public void CalculateConversion_FormulaShowsOnlyTheTableFactor()
    {
        var result = _service.CalculateConversion(new ConversionRequest(1m, "MG", "mcg"));

        Assert.Equal("MG to mcg = MG * 1000", result.Formula);
    }

    [Theory]
    [InlineData("", "mg")]
    [InlineData("mg", " ")]
    public void CalculateConversion_WithBlankUnit_ThrowsArgumentException(string from, string to)
    {
        Assert.Throws<ArgumentException>(() => _service.CalculateConversion(new ConversionRequest(1m, from, to)));
    }

    [Fact]
    public void Calculations_OnDecimalOverflow_ThrowArgumentExceptionNotOverflowException()
    {
        Assert.Throws<ArgumentException>(() => _service.CalculateConversion(new ConversionRequest(decimal.MaxValue, "g", "mcg")));
        Assert.Throws<ArgumentException>(() => _service.CalculateReconstitution(new ReconstitutionRequest(decimal.MaxValue, 1m)));
        Assert.Throws<ArgumentException>(() => _service.CalculateVolume(new VolumeRequest(decimal.MaxValue, 0.0000000000000000000000000001m)));
    }
}
