namespace BioStack.Application.Tests;

using System.Text.Json;
using BioStack.Application.Services;
using Xunit;

public sealed class ProtocolOperationsOfflineVerificationCapstoneGuardTests
{
    [Fact]
    public void ContractManifestSnapshot_FreezesOfflineVerificationSchemaIdentifiers()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));
        var bundle = manifest.RootElement.GetProperty("bundle");
        var receipt = manifest.RootElement.GetProperty("receipt");

        Assert.Equal("biostack.protocol-operations-export-bundle", bundle.GetProperty("schemaId").GetString());
        Assert.Equal(ProtocolOperationsExportBundleService.SchemaVersion, bundle.GetProperty("schemaVersion").GetString());
        Assert.Equal("biostack.protocol-operations-export-bundle.verification-receipt", receipt.GetProperty("receiptSchemaId").GetString());
        Assert.Equal("1.0.0", receipt.GetProperty("receiptSchemaVersion").GetString());
        Assert.Equal("biostack.protocol-operations-export-bundle.verifier", receipt.GetProperty("verifierSchemaId").GetString());
        Assert.Equal("1.0.0", receipt.GetProperty("verifierSchemaVersion").GetString());
    }

    [Fact]
    public void RepoReadme_PreservesEducationalObservationalSafetyPosture()
    {
        var readme = File.ReadAllText(Path.Combine(RepositoryRoot(), "README.md"));

        Assert.Contains("BioStack is for educational and observational use only.", readme, StringComparison.Ordinal);
        Assert.Contains("Not Medical Advice", readme, StringComparison.Ordinal);
        Assert.Contains("medical dosing recommendations", readme, StringComparison.Ordinal);
        Assert.Contains("clinical diagnosis", readme, StringComparison.Ordinal);
        Assert.Contains("Mathematical Logic Only", readme, StringComparison.Ordinal);
        Assert.Contains("pure mathematical formulas", readme, StringComparison.Ordinal);
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
