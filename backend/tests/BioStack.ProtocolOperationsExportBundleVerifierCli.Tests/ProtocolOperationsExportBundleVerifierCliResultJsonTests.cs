namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using System.Text.Json;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

public sealed class ProtocolOperationsExportBundleVerifierCliResultJsonTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";
    private const string ExpectedReportExportSha256 = "61cf6bec473668ef2562dfaba7e579e269e499801ae0ab33010bb62868a0b779";
    private const string ExpectedBundleSha256 = "744054e7ead52b5473aec8e88e22ff5ffc37658a30061fbbd800c3b339c14c19";

    [Fact]
    public void Run_ReturnsResultJson_ForValidBundle_WhenResultJsonFlagSupplied()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());
        var result = InvokeCli("--result-json", bundlePath);
        using var payload = JsonDocument.Parse(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Equal("verified", ReadString(payload.RootElement, "status"));
        Assert.Equal("protocol-operations-export-bundle", ReadString(payload.RootElement, "artifactTypeChecked"));
        Assert.Equal("biostack.protocol-operations-export-bundle", ReadString(payload.RootElement, "schemaIdChecked"));
        Assert.Equal("1.0.0", ReadString(payload.RootElement, "schemaVersionChecked"));
        Assert.Equal(ExpectedBundleSha256, ReadString(payload.RootElement, "expectedBundleContentHash"));
        Assert.Equal(ExpectedBundleSha256, ReadString(payload.RootElement, "actualBundleContentHash"));
        Assert.Equal(ExpectedReportExportSha256, ReadString(payload.RootElement, "expectedReportExportContentHash"));
        Assert.Equal(ExpectedReportExportSha256, ReadString(payload.RootElement, "actualReportExportContentHash"));
        Assert.Equal(
            [
                "bundle-non-null",
                "schema-version",
                "required-metadata",
                "json-artifact-descriptor",
                "embedded-report-export",
                "embedded-report-export-hash",
                "preserved-report-export-hash",
                "bundle-sha256",
                "observational-boundary"
            ],
            ReadStringArray(payload.RootElement, "checks"));
        Assert.Empty(ReadStringArray(payload.RootElement, "errors"));
    }

    [Fact]
    public void Run_ReturnsResultJson_ForInvalidBundle_WhenResultJsonFlagSupplied()
    {
        var bundlePath = CreateTempJsonFile(
            ReadFixtureJson().Replace(ExpectedBundleSha256, new string('b', 64), StringComparison.Ordinal));

        var result = InvokeCli("--result-json", bundlePath);
        using var payload = JsonDocument.Parse(result.StandardOutput);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("verification-failed", ReadString(payload.RootElement, "status"));
        Assert.Equal("protocol-operations-export-bundle", ReadString(payload.RootElement, "artifactTypeChecked"));
        Assert.Equal("1.0.0", ReadString(payload.RootElement, "schemaVersionChecked"));
        Assert.Contains("bundle-sha256-mismatch", ReadStringArray(payload.RootElement, "errors"));
    }

    [Fact]
    public void Run_ReturnsResultJson_ForValidReceipt_WhenResultJsonFlagSupplied()
    {
        var receiptPath = CreateTempJsonFile(InvokeCli("--receipt-json", CreateTempJsonFile(ReadFixtureJson())).StandardOutput);
        var result = InvokeCli("--result-json", "--verify-receipt-json", receiptPath);
        using var payload = JsonDocument.Parse(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("verified", ReadString(payload.RootElement, "status"));
        Assert.Equal("protocol-operations-export-bundle-verification-receipt", ReadString(payload.RootElement, "artifactTypeChecked"));
        Assert.Equal("biostack.protocol-operations-export-bundle.verification-receipt", ReadString(payload.RootElement, "receiptSchemaIdChecked"));
        Assert.Equal("1.0.0", ReadString(payload.RootElement, "receiptSchemaVersionChecked"));
        Assert.Equal("biostack.protocol-operations-export-bundle.verifier", ReadString(payload.RootElement, "verifierSchemaIdChecked"));
        Assert.Equal("1.0.0", ReadString(payload.RootElement, "verifierSchemaVersionChecked"));
        Assert.Equal("biostack.protocol-operations-export-bundle", ReadString(payload.RootElement, "bundleSchemaIdChecked"));
        Assert.Equal("1.0.0", ReadString(payload.RootElement, "bundleSchemaVersionChecked"));
        Assert.Empty(ReadStringArray(payload.RootElement, "errors"));
        Assert.False(string.IsNullOrWhiteSpace(ReadString(payload.RootElement, "receiptContentHash")));
    }

    [Fact]
    public void Run_ReturnsResultJson_ForInvalidReceipt_WhenResultJsonFlagSupplied()
    {
        var receiptPath = CreateTempJsonFile("{ not-json");
        var result = InvokeCli("--result-json", "--verify-receipt-json", receiptPath);
        using var payload = JsonDocument.Parse(result.StandardOutput);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("invalid-json", ReadString(payload.RootElement, "status"));
        Assert.Equal("protocol-operations-export-bundle-verification-receipt", ReadString(payload.RootElement, "artifactTypeChecked"));
        Assert.Contains("input-json-invalid", ReadStringArray(payload.RootElement, "errors"));
    }

    [Fact]
    public void Run_ResultJson_IsDeterministic_ForRepeatedBundleVerificationRuns()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var first = InvokeCli("--result-json", bundlePath);
        var second = InvokeCli("--result-json", bundlePath);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(first.StandardOutput, second.StandardOutput);
    }

    [Fact]
    public void Run_ResultJson_DoesNotIncludeHostPathOrForbiddenAuthorityWording()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());
        var result = InvokeCli("--result-json", bundlePath);

        Assert.DoesNotContain(bundlePath, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.GetDirectoryName(bundlePath)!, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.CurrentDirectory, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.MachineName, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.UserName, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("GeneratedAtUtc", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("medical advice", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("diagnosis", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("treatment", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prescription", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pdf-generated", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("persisted-output", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Protocol Intelligence runtime output", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string CreateTempJsonFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
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

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetString() ?? string.Empty;
    }

    private static string[] ReadStringArray(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
    }
}
