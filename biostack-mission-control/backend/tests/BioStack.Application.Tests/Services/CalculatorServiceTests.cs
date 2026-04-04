namespace BioStack.Application.Tests.Services;

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
        Assert.Equal(20m, result.Output);
        Assert.Equal("mcg/unit (0.01mL)", result.Unit);
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
        var request = new ConversionRequest(1000m, "mg", "mcg", 1000m);

        var result = _service.CalculateConversion(request);

        Assert.Equal(1000m, result.Input);
        Assert.Equal(1000000m, result.Output);
        Assert.Equal("mcg", result.Unit);
        Assert.Contains("This is a mathematical calculation only", result.Disclaimer);
    }

    [Fact]
    public void CalculateConversion_WithZeroAmount_ThrowsArgumentException()
    {
        var request = new ConversionRequest(0m, "mg", "mcg", 1000m);

        var ex = Assert.Throws<ArgumentException>(() => _service.CalculateConversion(request));
        Assert.Contains("greater than 0", ex.Message);
    }

    [Fact]
    public void CalculateConversion_WithZeroConversionFactor_ThrowsArgumentException()
    {
        var request = new ConversionRequest(1000m, "mg", "mcg", 0m);

        var ex = Assert.Throws<ArgumentException>(() => _service.CalculateConversion(request));
        Assert.Contains("greater than 0", ex.Message);
    }
}
