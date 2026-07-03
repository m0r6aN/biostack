namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

public sealed class ProtocolOperationsExportBundleVerifierCliRoundTripTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";
    private const string ReadmeRelativePath = @"backend\tools\BioStack.ProtocolOperationsExportBundleVerifierCli\README.md";

    [Fact]
    public void Readme_DocumentsSupportedModesAndGuarantees()
    {
        var readmePath = GetRepoFilePath(ReadmeRelativePath);

        Assert.True(File.Exists(readmePath), $"Expected README at '{readmePath}'.");

        var readme = File.ReadAllText(readmePath);

        Assert.Contains("BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json>", readme, StringComparison.Ordinal);
        Assert.Contains("BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json> --receipt-json", readme, StringComparison.Ordinal);
        Assert.Contains("BioStack.ProtocolOperationsExportBundleVerifierCli --verify-receipt-json <receipt.json>", readme, StringComparison.Ordinal);
        Assert.Contains("does not generate PDFs", readme, StringComparison.Ordinal);
        Assert.Contains("does not write files unless stdout is redirected by caller", readme, StringComparison.Ordinal);
        Assert.Contains("does not access persistence/database", readme, StringComparison.Ordinal);
        Assert.Contains("does not call export-generation services", readme, StringComparison.Ordinal);
        Assert.Contains("does not replay original bundle verification during receipt verification", readme, StringComparison.Ordinal);
        Assert.Contains("does not expand Protocol Intelligence runtime behavior", readme, StringComparison.Ordinal);
        Assert.Contains("does not provide medical advice, dosing, diagnosis, treatment, prescription, or recommendations", readme, StringComparison.Ordinal);
        Assert.Contains("emits deterministic receipt JSON with no timestamps, hostnames, local paths, or environment-specific values", readme, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./bundle.json", readme, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./bundle.json --receipt-json > receipt.json", readme, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- --verify-receipt-json ./receipt.json", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_RoundTrip_PrintsHumanSummary_ForGoldenBundle()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());

        var result = InvokeCli(bundlePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Contains("status: verified", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("checks:", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- observational-boundary", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("errors:", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- none", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_RoundTrip_EmitsDeterministicReceiptJson_ForGoldenBundle()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());

        var firstResult = InvokeCli("--receipt-json", bundlePath);
        var secondResult = InvokeCli("--receipt-json", bundlePath);

        using var receipt = ParseJson(firstResult.StandardOutput);

        Assert.Equal(0, firstResult.ExitCode);
        Assert.Equal(string.Empty, firstResult.StandardError);
        Assert.Equal(firstResult.StandardOutput, secondResult.StandardOutput);
        Assert.Equal("verified", ReadString(receipt.RootElement, "status"));
        Assert.Empty(ReadStringArray(receipt.RootElement, "errors"));
        Assert.True(receipt.RootElement.GetProperty("boundaries").GetProperty("observationalOnly").GetBoolean());
        Assert.True(receipt.RootElement.GetProperty("boundaries").GetProperty("nonMedical").GetBoolean());
        Assert.True(receipt.RootElement.GetProperty("boundaries").GetProperty("noPersistence").GetBoolean());
        Assert.True(receipt.RootElement.GetProperty("boundaries").GetProperty("noPdf").GetBoolean());
        Assert.True(receipt.RootElement.GetProperty("boundaries").GetProperty("noRuntimeExpansion").GetBoolean());
    }

    [Fact]
    public void Run_RoundTrip_VerifiesEmittedReceiptJson()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());
        var receiptPath = CreateTempFile(InvokeCli("--receipt-json", bundlePath).StandardOutput);

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            JoinLines(
                "Protocol Operations Export Bundle Verification Receipt: VALID",
                "Status: verified",
                $"ReceiptContentHash: {ReadReceiptContentHash(receiptPath)}"),
            result.StandardOutput);
    }

    [Fact]
    public void Run_RoundTrip_VerifiesReceiptWithoutOriginalBundlePresent()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());
        var receiptPath = CreateTempFile(InvokeCli("--receipt-json", bundlePath).StandardOutput);

        File.Delete(bundlePath);

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.DoesNotContain(bundlePath, Directory.EnumerateFiles(Path.GetDirectoryName(bundlePath)!));
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Protocol Operations Export Bundle Verification Receipt: VALID", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_RoundTrip_ReceiptVerificationStdoutIsDeterministic()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());
        var receiptPath = CreateTempFile(InvokeCli("--receipt-json", bundlePath).StandardOutput);

        var firstResult = InvokeCli("--verify-receipt-json", receiptPath);
        var secondResult = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(0, firstResult.ExitCode);
        Assert.Equal(firstResult.StandardOutput, secondResult.StandardOutput);
    }

    [Fact]
    public void Run_RoundTrip_TamperedReceiptFailsVerification()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());
        var receiptJson = InvokeCli("--receipt-json", bundlePath).StandardOutput;
        var receiptPath = CreateTempFile(TamperReceipt(receiptJson, "receiptContentHash", new string('a', 64)));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Protocol Operations Export Bundle Verification Receipt: INVALID", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("receipt-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_RoundTrip_ReceiptJsonOmitsTimestampHostPathAndEnvironmentData()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());

        var result = InvokeCli("--receipt-json", bundlePath);

        Assert.DoesNotContain(bundlePath, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.GetDirectoryName(bundlePath)!, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.CurrentDirectory, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.MachineName, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.UserName, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("GeneratedAtUtc", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("T12:00:00Z", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("PATH", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("\"stackTrace\"", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_RoundTrip_ReceiptJsonOmitsPdfPersistenceRuntimeAndMedicalAdviceWording()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());

        var result = InvokeCli("--receipt-json", bundlePath);

        Assert.DoesNotContain("pdf-generated", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("persisted-output", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Protocol Intelligence runtime output", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("medical advice", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dosing", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("diagnosis", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("treatment", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prescription", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recommendation", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static JsonDocument ParseJson(string json) => JsonDocument.Parse(json);

    private static string ReadString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString() ?? string.Empty;

    private static string[] ReadStringArray(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    private static string ReadReceiptContentHash(string receiptPath)
    {
        using var receipt = ParseJson(File.ReadAllText(receiptPath));
        return ReadString(receipt.RootElement, "receiptContentHash");
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private static string TamperReceipt(string receiptJson, string propertyName, string propertyValue)
    {
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(receiptJson)!;
        payload[propertyName] = JsonSerializer.SerializeToElement(propertyValue);
        return JsonSerializer.Serialize(payload);
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

    private static string GetRepoFilePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate) || Directory.Exists(Path.GetDirectoryName(candidate)!))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(relativePath, AppContext.BaseDirectory);
    }

    private static string JoinLines(params string[] lines) =>
        string.Join(Environment.NewLine, lines) + Environment.NewLine;
}
