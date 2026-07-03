namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

public sealed class ProtocolOperationsExportBundleVerifierCliTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";
    private const string ExpectedReportExportSha256 = "61cf6bec473668ef2562dfaba7e579e269e499801ae0ab33010bb62868a0b779";
    private const string ExpectedBundleSha256 = "744054e7ead52b5473aec8e88e22ff5ffc37658a30061fbbd800c3b339c14c19";

    [Fact]
    public void Run_ReturnsZero_ForValidGoldenFixture()
    {
        var fixturePath = CreateTempBundleFile(ReadFixtureJson());

        var result = InvokeCli(fixturePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
    }

    [Fact]
    public void Run_PrintsStableSuccessSummary_ForValidGoldenFixture()
    {
        var fixturePath = CreateTempBundleFile(ReadFixtureJson());

        var result = InvokeCli(fixturePath);

        Assert.Equal(
            JoinLines(
                "status: verified",
                "schema-version: 1.0.0",
                $"expected-bundle-sha256: {ExpectedBundleSha256}",
                $"actual-bundle-sha256: {ExpectedBundleSha256}",
                $"expected-report-export-sha256: {ExpectedReportExportSha256}",
                $"actual-report-export-sha256: {ExpectedReportExportSha256}",
                "checks:",
                "- bundle-non-null",
                "- schema-version",
                "- required-metadata",
                "- json-artifact-descriptor",
                "- embedded-report-export",
                "- embedded-report-export-hash",
                "- preserved-report-export-hash",
                "- bundle-sha256",
                "- observational-boundary",
                "errors:",
                "- none"),
            result.StandardOutput);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenBundleHashIsTampered()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(ExpectedBundleSha256, new string('b', 64), StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("status: verification-failed", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- bundle-sha256-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenReportHashIsTampered()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(ExpectedReportExportSha256, new string('c', 64), StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- embedded-report-export-sha256-mismatch", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- json-artifact-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenPdfArtifactClaimIsPresent()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace("application/json", "application/pdf", StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- pdf-artifact-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenPersistenceClaimIsPresent()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Bundle persisted to C:\\\\exports\\\\protocol-operations-report.json.",
                StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- persisted-output-claim-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenProtocolIntelligenceRuntimeLanguageIsPresent()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Protocol Intelligence runtime generated export.",
                StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- protocol-intelligence-runtime-language-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenMedicalAdviceLanguageIsPresent()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Take 25 mg nightly.",
                StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- medical-advice-language-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenFileIsMissing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        var result = InvokeCli(missingPath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(
            JoinLines(
                "status: missing-file",
                "schema-version: (unavailable)",
                "expected-bundle-sha256: (unavailable)",
                "actual-bundle-sha256: (unavailable)",
                "expected-report-export-sha256: (unavailable)",
                "actual-report-export-sha256: (unavailable)",
                "checks:",
                "- none",
                "errors:",
                "- input-file-missing"),
            result.StandardOutput);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenJsonIsInvalid()
    {
        var fixturePath = CreateTempBundleFile("{ not-json }");

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("status: invalid-json", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- input-json-invalid", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_PreservesStableOrderedErrors_InFailureOutput()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Bundle persisted to C:\\\\exports\\\\protocol-operations-report.json.",
                StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.Equal(
            [
                "- bundle-sha256-mismatch",
                "- embedded-report-export-sha256-mismatch",
                "- json-artifact-content-hash-mismatch",
                "- persisted-output-claim-not-allowed"
            ],
            ReadErrorLines(result.StandardOutput));
    }

    [Fact]
    public void Run_DoesNotMutateInputMaterial()
    {
        var originalJson = ReadFixtureJson();
        var fixturePath = CreateTempBundleFile(originalJson);

        var result = InvokeCli(fixturePath);
        var fileJson = File.ReadAllText(fixturePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(originalJson, fileJson);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(string path)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run([path], stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string CreateTempBundleFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
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

    private static string JoinLines(params string[] lines) =>
        string.Join(Environment.NewLine, lines) + Environment.NewLine;

    private static IReadOnlyList<string> ReadErrorLines(string output)
    {
        var lines = output
            .Split(Environment.NewLine, StringSplitOptions.None)
            .SkipWhile(line => !string.Equals(line, "errors:", StringComparison.Ordinal))
            .Skip(1)
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .ToArray();

        return lines;
    }
}
