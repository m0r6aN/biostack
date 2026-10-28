namespace BioStack.Application.Tests.Services;

using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using Xunit;

/// <summary>
/// Runs the SAME vector file the frontend TypeScript calculator is tested against
/// (shared/calculator/golden-vectors.json, copied next to the test assembly). The expected values in that file
/// were worked out by hand from the unit definitions, not generated from either implementation.
/// </summary>
public class CalculatorGoldenVectorTests
{
    private const decimal RelativeTolerance = 1e-9m;
    private static readonly string VectorPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "calculator-golden-vectors.json");
    private static readonly JsonElement Vectors = JsonDocument.Parse(File.ReadAllText(VectorPath)).RootElement;
    private readonly CalculatorService _service = new();

    public static IEnumerable<object[]> Section(string name) =>
        Vectors.GetProperty(name).EnumerateArray().Select(e => new object[] { e.GetProperty("id").GetString()! });

    public static IEnumerable<object[]> MassConversionIds() => Section("massConversion");
    public static IEnumerable<object[]> ReconstitutionIds() => Section("reconstitution");
    public static IEnumerable<object[]> VolumeIds() => Section("volume");
    public static IEnumerable<object[]> U100Ids() => Section("u100");
    public static IEnumerable<object[]> PipelineIds() => Section("pipeline");
    public static IEnumerable<object[]> RejectionIds() => Section("rejections");

    private static JsonElement Find(string section, string id) =>
        Vectors.GetProperty(section).EnumerateArray().Single(e => e.GetProperty("id").GetString() == id);

    private static decimal D(JsonElement e, string name) => decimal.Parse(e.GetProperty(name).GetRawText(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

    private static void AssertClose(decimal expected, decimal actual)
    {
        Assert.True(
            Math.Abs(actual - expected) <= Math.Abs(expected) * RelativeTolerance,
            $"expected {expected}, got {actual}");
    }

    [Fact]
    public void VectorFile_IsLoaded()
    {
        Assert.Equal(1, Vectors.GetProperty("version").GetInt32());
        Assert.NotEmpty(Vectors.GetProperty("pipeline").EnumerateArray());
    }

    [Theory]
    [MemberData(nameof(MassConversionIds))]
    public void MassConversion_MatchesGoldenVector(string id)
    {
        var v = Find("massConversion", id);
        var result = _service.CalculateConversion(new ConversionRequest(D(v, "amount"), v.GetProperty("from").GetString()!, v.GetProperty("to").GetString()!));
        AssertClose(D(v, "expected"), result.Output);
    }

    [Theory]
    [MemberData(nameof(ReconstitutionIds))]
    public void Reconstitution_MatchesGoldenVector(string id)
    {
        var v = Find("reconstitution", id);
        var result = _service.CalculateReconstitution(new ReconstitutionRequest(D(v, "powderMg"), D(v, "diluentMl")));
        AssertClose(D(v, "expectedMcgPerMl"), result.Output);
    }

    [Theory]
    [MemberData(nameof(VolumeIds))]
    public void Volume_MatchesGoldenVector(string id)
    {
        var v = Find("volume", id);
        var result = _service.CalculateVolume(new VolumeRequest(D(v, "doseMcg"), D(v, "concentrationMcgPerMl")));
        AssertClose(D(v, "expectedMl"), result.Output);
    }

    [Theory]
    [MemberData(nameof(U100Ids))]
    public void U100Units_AreHundredTimesTheVolume(string id)
    {
        // The backend returns mL; a U-100 syringe has 100 units per mL. Same vector as the frontend: a dose of
        // ml * 1000 mcg at 1000 mcg/mL draws exactly `ml` mL.
        var v = Find("u100", id);
        var ml = D(v, "ml");
        var result = _service.CalculateVolume(new VolumeRequest(ml * 1000m, 1000m));
        AssertClose(D(v, "expectedUnits"), result.Output * 100m);
    }

    [Theory]
    [MemberData(nameof(PipelineIds))]
    public void Pipeline_PowderToConcentrationToVolumeToU100_MatchesGoldenVector(string id)
    {
        var v = Find("pipeline", id);
        var conc = _service.CalculateReconstitution(new ReconstitutionRequest(D(v, "powderMg"), D(v, "diluentMl"))).Output;
        AssertClose(D(v, "expectedMcgPerMl"), conc);

        var ml = _service.CalculateVolume(new VolumeRequest(D(v, "doseMcg"), conc)).Output;
        AssertClose(D(v, "expectedMl"), ml);
        AssertClose(D(v, "expectedU100Units"), ml * 100m);
    }

    [Theory]
    [MemberData(nameof(RejectionIds))]
    public void Rejections_ThrowArgumentException(string id)
    {
        var v = Find("rejections", id);
        Action act = v.GetProperty("kind").GetString() switch
        {
            "reconstitution" => () => _service.CalculateReconstitution(new ReconstitutionRequest(D(v, "powderMg"), D(v, "diluentMl"))),
            "volume" => () => _service.CalculateVolume(new VolumeRequest(D(v, "doseMcg"), D(v, "concentrationMcgPerMl"))),
            "conversion" => () => _service.CalculateConversion(new ConversionRequest(D(v, "amount"), v.GetProperty("from").GetString()!, v.GetProperty("to").GetString()!)),
            var other => throw new InvalidOperationException($"unknown rejection kind {other}"),
        };
        Assert.Throws<ArgumentException>(act);
    }
}
