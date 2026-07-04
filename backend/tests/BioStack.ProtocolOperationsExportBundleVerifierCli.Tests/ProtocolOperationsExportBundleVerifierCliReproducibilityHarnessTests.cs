namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using System.Text.Json;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

/// <summary>
/// Reproducibility harness: proves the offline verifier emits byte-identical output across repeated
/// runs for every supported mode. A verifier that produces different output twice cannot be trusted,
/// so each scenario is executed several times and every run must match the first exactly (exit code,
/// stdout, stderr). Byte-identical stdout also freezes recomputed hashes, ordered checks/errors,
/// receipt id/hash, and JSON property ordering.
/// </summary>
public sealed class ProtocolOperationsExportBundleVerifierCliReproducibilityHarnessTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";
    private const string ExpectedBundleSha256 = "744054e7ead52b5473aec8e88e22ff5ffc37658a30061fbbd800c3b339c14c19";
    private const int RepeatCount = 5;

    [Fact]
    public void ValidBundleVerification_IsReproducibleAcrossRuns()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        AssertReproducible(() => InvokeCli(bundlePath), expectedExitCode: 0);
    }

    [Fact]
    public void InvalidBundleVerification_IsReproducibleAcrossRuns()
    {
        var bundlePath = CreateTempJsonFile(InvalidBundleJson());

        AssertReproducible(() => InvokeCli(bundlePath), expectedExitCode: 1);
    }

    [Fact]
    public void ReceiptEmission_FromSameSuppliedBundle_IsReproducibleAcrossRuns()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var runs = AssertReproducible(() => InvokeCli("--receipt-json", bundlePath), expectedExitCode: 0);

        // Both receipt-level hashes (the receipt's stable identity) are present and byte-identical
        // across independent emissions; equality of full stdout above already froze their values.
        using var receipt = JsonDocument.Parse(runs[0].StandardOutput);
        Assert.False(string.IsNullOrWhiteSpace(ReadString(receipt.RootElement, "receiptContentHash")));
        Assert.False(string.IsNullOrWhiteSpace(ReadString(receipt.RootElement, "verificationResultContentHash")));
    }

    [Fact]
    public void ValidReceiptVerification_IsReproducibleAcrossRuns()
    {
        var receiptPath = CreateTempJsonFile(EmitReceiptJson());

        AssertReproducible(() => InvokeCli("--verify-receipt-json", receiptPath), expectedExitCode: 0);
    }

    [Fact]
    public void InvalidReceiptVerification_IsReproducibleAcrossRuns()
    {
        var receiptPath = CreateTempJsonFile("{ not-json");

        AssertReproducible(() => InvokeCli("--verify-receipt-json", receiptPath), expectedExitCode: 1);
    }

    [Fact]
    public void ResultJson_ForBundleVerification_IsReproducibleAcrossRuns()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        AssertReproducible(() => InvokeCli("--result-json", bundlePath), expectedExitCode: 0);
    }

    [Fact]
    public void ResultJson_ForReceiptVerification_IsReproducibleAcrossRuns()
    {
        var receiptPath = CreateTempJsonFile(EmitReceiptJson());

        AssertReproducible(
            () => InvokeCli("--result-json", "--verify-receipt-json", receiptPath),
            expectedExitCode: 0);
    }

    private static IReadOnlyList<(int ExitCode, string StandardOutput, string StandardError)> AssertReproducible(
        Func<(int ExitCode, string StandardOutput, string StandardError)> invoke,
        int expectedExitCode)
    {
        var runs = new List<(int ExitCode, string StandardOutput, string StandardError)>(RepeatCount);
        for (var i = 0; i < RepeatCount; i++)
        {
            runs.Add(invoke());
        }

        var first = runs[0];
        Assert.Equal(expectedExitCode, first.ExitCode);

        for (var i = 1; i < runs.Count; i++)
        {
            Assert.Equal(first.ExitCode, runs[i].ExitCode);
            Assert.Equal(first.StandardOutput, runs[i].StandardOutput);
            Assert.Equal(first.StandardError, runs[i].StandardError);
        }

        return runs;
    }

    private static string EmitReceiptJson()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());
        var result = InvokeCli("--receipt-json", bundlePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);

        return result.StandardOutput;
    }

    private static string InvalidBundleJson()
    {
        return ReadFixtureJson().Replace(ExpectedBundleSha256, new string('b', 64), StringComparison.Ordinal);
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
        return element.TryGetProperty(propertyName, out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }
}
