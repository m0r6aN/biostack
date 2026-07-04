namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using System.Text.Json;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

/// <summary>
/// One narrow end-to-end golden walkthrough that follows the documented offline kit path exactly as
/// an operator would run it: load the golden bundle fixture, verify the bundle, emit a verification
/// receipt, verify the emitted receipt, then emit --result-json for both bundle and receipt
/// verification. It proves the happy path still holds together and stays deterministic, that the
/// input bundle is never mutated, that the receipt binds to the supplied bundle material, and that
/// receipt verification is receipt-only (it does not require the original bundle or invoke
/// export-generation).
/// </summary>
public sealed class ProtocolOperationsExportBundleVerifierCliOfflineKitWalkthroughTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";

    [Fact]
    public void OfflineKitWalkthrough_FollowsDocumentedHappyPath_Deterministically()
    {
        var bundleJson = ReadFixtureJson();

        // Step 1: operator has a known golden export bundle on disk.
        var bundlePath = CreateTempJsonFile(bundleJson);
        var bundleBytesBefore = File.ReadAllBytes(bundlePath);

        // Step 2: verify the bundle (human summary).
        var bundleSummary = InvokeCli(bundlePath);
        Assert.Equal(0, bundleSummary.ExitCode);
        Assert.Equal(string.Empty, bundleSummary.StandardError);
        Assert.Contains("status: verified", bundleSummary.StandardOutput, StringComparison.Ordinal);

        // Step 3: emit a verification receipt.
        var receiptEmission = InvokeCli("--receipt-json", bundlePath);
        Assert.Equal(0, receiptEmission.ExitCode);
        var receiptJson = receiptEmission.StandardOutput;
        var receiptPath = CreateTempJsonFile(receiptJson);

        using (var receipt = JsonDocument.Parse(receiptJson))
        {
            // Step 3a: the receipt binds to the supplied bundle verification material.
            using var bundle = JsonDocument.Parse(bundleJson);
            var suppliedBundleHash = bundle.RootElement.GetProperty("Integrity").GetProperty("BundleContentHash").GetString();
            Assert.Equal("verified", receipt.RootElement.GetProperty("status").GetString());
            Assert.Equal(suppliedBundleHash, receipt.RootElement.GetProperty("suppliedBundleContentHash").GetString());
            Assert.Equal(suppliedBundleHash, receipt.RootElement.GetProperty("computedBundleContentHash").GetString());
            Assert.False(string.IsNullOrWhiteSpace(receipt.RootElement.GetProperty("receiptContentHash").GetString()));
        }

        // Step 4: verify the emitted receipt.
        var receiptVerification = InvokeCli("--verify-receipt-json", receiptPath);
        Assert.Equal(0, receiptVerification.ExitCode);

        // Step 4a: receipt verification is receipt-only — it does not require the original bundle file
        // to remain present and therefore cannot invoke export-generation services.
        File.Delete(bundlePath);
        var receiptVerificationWithoutBundle = InvokeCli("--verify-receipt-json", receiptPath);
        Assert.Equal(0, receiptVerificationWithoutBundle.ExitCode);
        Assert.Equal(receiptVerification.StandardOutput, receiptVerificationWithoutBundle.StandardOutput);

        // Step 5 + 6: emit --result-json for bundle and receipt verification.
        var bundleResultPath = CreateTempJsonFile(bundleJson);
        var bundleResult = InvokeCli("--result-json", bundleResultPath);
        Assert.Equal(0, bundleResult.ExitCode);
        AssertParsesToObject(bundleResult.StandardOutput, expectedStatus: "verified");

        var receiptResult = InvokeCli("--result-json", "--verify-receipt-json", receiptPath);
        Assert.Equal(0, receiptResult.ExitCode);
        AssertParsesToObject(receiptResult.StandardOutput, expectedStatus: "verified");

        // Step 7: the whole sequence is deterministic — a second independent run of the receipt and
        // result surfaces from the same supplied bundle is byte-identical.
        var bundlePathSecond = CreateTempJsonFile(bundleJson);
        Assert.Equal(receiptJson, InvokeCli("--receipt-json", bundlePathSecond).StandardOutput);
        Assert.Equal(bundleResult.StandardOutput, InvokeCli("--result-json", bundlePathSecond).StandardOutput);
        Assert.Equal(receiptResult.StandardOutput, InvokeCli("--result-json", "--verify-receipt-json", receiptPath).StandardOutput);

        // Input bundle content is never mutated by verification or receipt emission.
        Assert.Equal(bundleBytesBefore, File.ReadAllBytes(bundlePathSecond));
    }

    [Fact]
    public void OfflineKitWalkthrough_DocsDocumentTheCommandsTheOperatorRuns()
    {
        var readme = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "backend", "tools", "BioStack.ProtocolOperationsExportBundleVerifierCli", "README.md"));

        foreach (var command in new[] { "--result-json", "--receipt-json", "--verify-receipt-json" })
        {
            Assert.Contains(command, readme, StringComparison.Ordinal);
        }
    }

    private static void AssertParsesToObject(string json, string expectedStatus)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.Equal(expectedStatus, document.RootElement.GetProperty("status").GetString());
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
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ProtocolOperationsExportBundle", FixtureFileName);
        return File.ReadAllText(fixturePath);
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "backend", "BioStack.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BioStack repository root.");
    }
}
