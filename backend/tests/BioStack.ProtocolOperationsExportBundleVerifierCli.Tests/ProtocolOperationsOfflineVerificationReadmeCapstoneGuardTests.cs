namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text.Json;
using Xunit;

public sealed class ProtocolOperationsOfflineVerificationReadmeCapstoneGuardTests
{
    [Fact]
    public void CliReadme_DocumentsTheSameThreeSupportedOperationsAsTheManifestSnapshot()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));
        var cli = manifest.RootElement.GetProperty("cli");
        var readme = File.ReadAllText(CliReadmePath());

        Assert.Equal("positional-bundle-json-input", cli.GetProperty("verifyBundleJsonMode").GetString());
        Assert.Equal("--receipt-json", cli.GetProperty("emitReceiptJsonFlag").GetString());
        Assert.Equal("--verify-receipt-json", cli.GetProperty("verifyReceiptJsonFlag").GetString());

        Assert.Contains("BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json>", readme, StringComparison.Ordinal);
        Assert.Contains("BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json> --receipt-json", readme, StringComparison.Ordinal);
        Assert.Contains("BioStack.ProtocolOperationsExportBundleVerifierCli --verify-receipt-json <receipt.json>", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void CliReadme_PreservesLockedOfflineBoundaryClaims()
    {
        var readme = File.ReadAllText(CliReadmePath());

        Assert.Contains("does not provide medical advice, dosing, diagnosis, treatment, prescription, or recommendations", readme, StringComparison.Ordinal);
        Assert.Contains("does not generate PDFs", readme, StringComparison.Ordinal);
        Assert.Contains("does not make PDF authenticity claims", readme, StringComparison.Ordinal);
        Assert.Contains("does not access persistence/database", readme, StringComparison.Ordinal);
        Assert.Contains("does not verify persistence or database state", readme, StringComparison.Ordinal);
        Assert.Contains("does not call export-generation services", readme, StringComparison.Ordinal);
        Assert.Contains("does not replay original bundle verification during receipt verification", readme, StringComparison.Ordinal);
        Assert.Contains("does not expand Protocol Intelligence runtime behavior", readme, StringComparison.Ordinal);
        Assert.Contains("Receipt verification is receipt-only validation. It does not require the original bundle file to remain present.", readme, StringComparison.Ordinal);
    }

    private static string ManifestSnapshotPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "backend",
            "tests",
            "Fixtures",
            "ProtocolOperationsExportBundle",
            "ProtocolOperationsOfflineVerificationContractManifest.golden.json");
    }

    private static string CliReadmePath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "backend",
            "tools",
            "BioStack.ProtocolOperationsExportBundleVerifierCli",
            "README.md");
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
