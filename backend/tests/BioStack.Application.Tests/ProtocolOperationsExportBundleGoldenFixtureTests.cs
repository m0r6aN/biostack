namespace BioStack.Application.Tests;

using System.Text.Json;
using BioStack.Application.Abstractions;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using Xunit;

public sealed class ProtocolOperationsExportBundleGoldenFixtureTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";
    private const string ExpectedReportExportSha256 = "61cf6bec473668ef2562dfaba7e579e269e499801ae0ab33010bb62868a0b779";
    private const string ExpectedBundleSha256 = "744054e7ead52b5473aec8e88e22ff5ffc37658a30061fbbd800c3b339c14c19";

    private static readonly string[] ExpectedChecks =
    [
        "bundle-non-null",
        "schema-version",
        "required-metadata",
        "json-artifact-descriptor",
        "embedded-report-export",
        "embedded-report-export-hash",
        "preserved-report-export-hash",
        "bundle-sha256",
        "observational-boundary",
    ];

    [Fact]
    public void GoldenFixture_ValidFixtureRoundTripsAndVerifies()
    {
        var fixtureJson = ReadFixtureJson();
        var bundle = DeserializeBundle(fixtureJson);
        var verifier = BuildVerifier();

        var reportHash = ProtocolOperationsReportExportService.ComputeContentHash(bundle.ReportExport.Report);
        var bundleHash = ProtocolOperationsExportBundleService.ComputeBundleContentHash(
            bundle.Metadata,
            bundle.ReportExport,
            bundle.Artifacts,
            bundle.Disclaimer);

        var result = verifier.Verify(bundle);

        Assert.Equal(ExpectedReportExportSha256, reportHash);
        Assert.Equal(ExpectedBundleSha256, bundleHash);
        Assert.Equal(ExpectedReportExportSha256, bundle.ReportExport.Integrity.ContentHash);
        Assert.Equal(ExpectedReportExportSha256, bundle.Integrity.ReportExportContentHash);
        Assert.Equal(ExpectedReportExportSha256, Assert.Single(bundle.Artifacts).ContentHash);
        Assert.Equal(ExpectedBundleSha256, bundle.Integrity.BundleContentHash);
        Assert.True(result.IsValid);
        Assert.Equal(ProtocolOperationsExportBundleService.SchemaVersion, result.SchemaVersion);
        Assert.Equal(ExpectedChecks, result.Checks);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void GoldenFixture_VerificationDoesNotMutateOriginalFixtureContent()
    {
        var fixtureJson = ReadFixtureJson();
        var bundle = DeserializeBundle(fixtureJson);
        var serializedBefore = JsonSerializer.Serialize(bundle);

        _ = BuildVerifier().Verify(bundle);

        var serializedAfter = JsonSerializer.Serialize(bundle);
        Assert.Equal(fixtureJson, ReadFixtureJson());
        Assert.Equal(serializedBefore, serializedAfter);
    }

    [Fact]
    public void GoldenFixture_TamperedBundleHashFailsClosed()
    {
        var tamperedJson = ReadFixtureJson().Replace(ExpectedBundleSha256, new string('f', 64), StringComparison.Ordinal);

        var result = BuildVerifier().Verify(DeserializeBundle(tamperedJson));

        Assert.False(result.IsValid);
        Assert.Equal(new[] { "bundle-sha256-mismatch" }, result.Errors);
        Assert.Equal(ExpectedChecks, result.Checks);
    }

    [Fact]
    public void GoldenFixture_TamperedEmbeddedReportFailsClosedWithStableErrorOrdering()
    {
        const string original = "Observational history is limited to recorded events.";
        const string tampered = "Observational history is limited to reviewed events.";

        var tamperedJson = ReadFixtureJson().Replace(original, tampered, StringComparison.Ordinal);
        var result = BuildVerifier().Verify(DeserializeBundle(tamperedJson));

        Assert.False(result.IsValid);
        Assert.Equal(
            new[]
            {
                "bundle-sha256-mismatch",
                "embedded-report-export-sha256-mismatch",
                "json-artifact-content-hash-mismatch",
            },
            result.Errors);
        Assert.Equal(ExpectedChecks, result.Checks);
    }

    [Fact]
    public void GoldenFixture_ForbiddenPersistenceClaimFailsClosedWithStableErrorOrdering()
    {
        const string original = "Observational history is limited to recorded events.";
        const string tampered = "Bundle persisted to C:\\\\exports\\\\protocol-operations-report.json.";

        var tamperedJson = ReadFixtureJson().Replace(original, tampered, StringComparison.Ordinal);
        var result = BuildVerifier().Verify(DeserializeBundle(tamperedJson));

        Assert.False(result.IsValid);
        Assert.Equal(
            new[]
            {
                "bundle-sha256-mismatch",
                "embedded-report-export-sha256-mismatch",
                "json-artifact-content-hash-mismatch",
                "persisted-output-claim-not-allowed",
            },
            result.Errors);
    }

    [Fact]
    public void GoldenFixture_ForbiddenProtocolIntelligenceRuntimeLanguageFailsClosed()
    {
        const string original = "Observational history is limited to recorded events.";
        const string tampered = "Protocol Intelligence runtime generated this export.";

        var tamperedJson = ReadFixtureJson().Replace(original, tampered, StringComparison.Ordinal);
        var result = BuildVerifier().Verify(DeserializeBundle(tamperedJson));

        Assert.False(result.IsValid);
        Assert.Equal(
            new[]
            {
                "bundle-sha256-mismatch",
                "embedded-report-export-sha256-mismatch",
                "json-artifact-content-hash-mismatch",
                "protocol-intelligence-runtime-language-not-allowed",
            },
            result.Errors);
    }

    [Fact]
    public void GoldenFixture_ForbiddenMedicalAdviceLanguageFailsClosed()
    {
        const string original = "Observational history is limited to recorded events.";
        const string tampered = "Take 25 mg nightly.";

        var tamperedJson = ReadFixtureJson().Replace(original, tampered, StringComparison.Ordinal);
        var result = BuildVerifier().Verify(DeserializeBundle(tamperedJson));

        Assert.False(result.IsValid);
        Assert.Equal(
            new[]
            {
                "bundle-sha256-mismatch",
                "embedded-report-export-sha256-mismatch",
                "json-artifact-content-hash-mismatch",
                "medical-advice-language-not-allowed",
            },
            result.Errors);
    }

    private static IProtocolOperationsExportBundleVerifier BuildVerifier() => new ProtocolOperationsExportBundleVerifier();

    private static ProtocolOperationsExportBundle DeserializeBundle(string json)
    {
        return JsonSerializer.Deserialize<ProtocolOperationsExportBundle>(json)
            ?? throw new InvalidOperationException("Failed to deserialize golden fixture bundle.");
    }

    private static string ReadFixtureJson()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "ProtocolOperationsExportBundle",
            FixtureFileName);

        return File.ReadAllText(fixturePath);
    }
}
