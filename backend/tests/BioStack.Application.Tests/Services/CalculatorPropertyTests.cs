namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using Xunit;

/// <summary>Seeded, repeatable property and boundary tests. Oracles are the unit definitions, written independently of the service.</summary>
public class CalculatorPropertyTests
{
    private readonly CalculatorService _service = new();
    private const int Iterations = 500;
    private static readonly string[] Units = { "mcg", "mg", "g" };
    private static readonly decimal[] PerGram = { 1_000_000m, 1_000m, 1m };

    /// <summary>Roughly log-uniform in [1e-4, 1e6], using decimals (so rounding stays well below the tolerance).</summary>
    private static decimal Sample(Random r) => (decimal)Math.Pow(10, r.NextDouble() * 10 - 4);

    private static void Close(decimal expected, decimal actual, decimal rel = 1e-12m) =>
        Assert.True(Math.Abs(actual - expected) <= Math.Abs(expected) * rel, $"expected {expected}, got {actual}");

    [Fact]
    public void Conversion_MatchesDefinition_ForEveryUnitPair()
    {
        var r = new Random(1);
        for (var i = 0; i < Iterations; i++)
        {
            var x = Sample(r);
            var a = r.Next(3);
            var b = r.Next(3);
            var expected = x / PerGram[a] * PerGram[b];
            Close(expected, _service.CalculateConversion(new ConversionRequest(x, Units[a], Units[b])).Output);
        }
    }

    [Fact]
    public void Conversion_RoundTrips()
    {
        var r = new Random(2);
        for (var i = 0; i < Iterations; i++)
        {
            var x = Sample(r);
            var a = r.Next(3);
            var b = r.Next(3);
            var there = _service.CalculateConversion(new ConversionRequest(x, Units[a], Units[b])).Output;
            var back = _service.CalculateConversion(new ConversionRequest(there, Units[b], Units[a])).Output;
            Close(x, back);
        }
    }

    [Fact]
    public void ConcentrationTimesVolume_EqualsDose()
    {
        var r = new Random(3);
        for (var i = 0; i < Iterations; i++)
        {
            var mg = Sample(r);
            var ml = Sample(r);
            var dose = Sample(r);
            var conc = _service.CalculateReconstitution(new ReconstitutionRequest(mg, ml)).Output;
            var vol = _service.CalculateVolume(new VolumeRequest(dose, conc)).Output;
            Close(dose, conc * vol);
        }
    }

    [Fact]
    public void Volume_ScalesLinearlyWithDose()
    {
        var r = new Random(4);
        for (var i = 0; i < Iterations; i++)
        {
            var dose = Sample(r);
            var conc = Sample(r);
            var v1 = _service.CalculateVolume(new VolumeRequest(dose, conc)).Output;
            var v2 = _service.CalculateVolume(new VolumeRequest(dose * 2m, conc)).Output;
            Close(v1 * 2m, v2);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.0000000001)]
    public void NonPositiveInputs_AreRejectedEverywhere(double bad)
    {
        var x = (decimal)bad;
        Assert.Throws<ArgumentException>(() => _service.CalculateReconstitution(new ReconstitutionRequest(x, 1m)));
        Assert.Throws<ArgumentException>(() => _service.CalculateReconstitution(new ReconstitutionRequest(1m, x)));
        Assert.Throws<ArgumentException>(() => _service.CalculateVolume(new VolumeRequest(x, 1m)));
        Assert.Throws<ArgumentException>(() => _service.CalculateVolume(new VolumeRequest(1m, x)));
        Assert.Throws<ArgumentException>(() => _service.CalculateConversion(new ConversionRequest(x, "mg", "mcg")));
    }

    [Fact]
    public void TinyAndHugeValidValues_ComputeExactly()
    {
        Assert.Equal(0.001m, _service.CalculateConversion(new ConversionRequest(0.000001m, "mg", "mcg")).Output);
        Assert.Equal(1_000_000_000_000m, _service.CalculateConversion(new ConversionRequest(1_000_000_000m, "mg", "mcg")).Output);
        Assert.Equal(1m, _service.CalculateReconstitution(new ReconstitutionRequest(0.001m, 1m)).Output);
        Assert.Equal(2_000_000m, _service.CalculateReconstitution(new ReconstitutionRequest(1000m, 0.5m)).Output);
    }

    [Fact]
    public void Worked_Example_250mcg_From_5mg_In_2ml_Is_Ten_U100_Units()
    {
        // 250 mcg from 5 mg in 2 mL: 2500 mcg/mL, 0.1 mL, 10 U-100 units.
        var conc = _service.CalculateReconstitution(new ReconstitutionRequest(5m, 2m)).Output;
        var ml = _service.CalculateVolume(new VolumeRequest(250m, conc)).Output;
        Assert.Equal(2500m, conc);
        Assert.Equal(0.1m, ml);
        Assert.Equal(10m, ml * 100m);
    }
}
