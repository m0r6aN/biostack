namespace BioStack.Application.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.Application.Services;
using Xunit;

public sealed class ProtocolOperationsOfflineVerificationContractManifestTests
{
    [Fact]
    public void ProtocolOperationsOfflineVerificationContractManifest_SnapshotExists()
    {
        Assert.True(
            File.Exists(ManifestSnapshotPath()),
            $"Expected manifest snapshot at '{ManifestSnapshotPath()}'.");
    }

    [Fact]
    public void ProtocolOperationsOfflineVerificationContractManifest_DerivedApplicationSurfaceMatchesSnapshot()
    {
        var expected = CanonicalizeJson(File.ReadAllText(ManifestSnapshotPath()));
        var actual = CanonicalizeJson(BuildManifestFromApplicationSurface());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProtocolOperationsOfflineVerificationContractManifest_SnapshotContainsExpectedContractSections()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));

        Assert.True(manifest.RootElement.TryGetProperty("bundle", out _));
        Assert.True(manifest.RootElement.TryGetProperty("receipt", out _));
        Assert.True(manifest.RootElement.TryGetProperty("cli", out _));
        Assert.True(manifest.RootElement.TryGetProperty("hashSurfaces", out _));
        Assert.True(manifest.RootElement.TryGetProperty("boundaries", out _));
    }

    [Fact]
    public void ProtocolOperationsOfflineVerificationContractManifest_ApplicationSurfaceRequiresNoRuntimeServices()
    {
        var actual = CanonicalizeJson(BuildManifestFromApplicationSurface());

        Assert.DoesNotContain("Protocol Intelligence runtime", actual, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("persisted", actual, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pdf-generated", actual, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ProtocolOperationsExportBundleService.SchemaVersion, actual, StringComparison.Ordinal);
        Assert.Contains(ProtocolOperationsExportBundleService.HashAlgorithmName, actual, StringComparison.Ordinal);
    }

    private static string BuildManifestFromApplicationSurface()
    {
        using var bundle = JsonDocument.Parse(File.ReadAllText(BundleFixturePath()));
        var root = bundle.RootElement;
        var manifest = new
        {
            manifestSchemaId = "biostack.protocol-operations-offline-verification-contract-manifest",
            manifestSchemaVersion = "1.0.0",
            manifestScope = "protocol_operations_offline_verification_contract_manifest",
            manifestPosture = new
            {
                backendOnly = true,
                testOwned = true,
                driftGuard = true,
                nonProductSurface = true
            },
            bundle = new
            {
                schemaId = "biostack.protocol-operations-export-bundle",
                schemaVersion = ProtocolOperationsExportBundleService.SchemaVersion,
                hashAlgorithm = ProtocolOperationsExportBundleService.HashAlgorithmName,
                topLevelFields = ReadPropertyNames(root),
                metadataFields = ReadPropertyNames(root.GetProperty("Metadata")),
                artifactFields = ReadPropertyNames(root.GetProperty("Artifacts")[0]),
                integrityFields = ReadPropertyNames(root.GetProperty("Integrity"))
            },
            receipt = new
            {
                receiptSchemaId = "biostack.protocol-operations-export-bundle.verification-receipt",
                receiptSchemaVersion = "1.0.0",
                verifierSchemaId = "biostack.protocol-operations-export-bundle.verifier",
                verifierSchemaVersion = "1.0.0",
                boundaryFields = new[]
                {
                    "observationalOnly",
                    "nonMedical",
                    "noPersistence",
                    "noPdf",
                    "noRuntimeExpansion"
                },
                hashFields = new[]
                {
                    "computedBundleContentHash",
                    "suppliedBundleContentHash",
                    "computedReportExportContentHash",
                    "suppliedReportExportContentHash",
                    "verificationResultContentHash",
                    "receiptContentHash"
                }
            },
            cli = new
            {
                verifyBundleJsonMode = "positional-bundle-json-input",
                emitReceiptJsonFlag = "--receipt-json",
                verifyReceiptJsonFlag = "--verify-receipt-json"
            },
            hashSurfaces = new
            {
                bundleContentHashField = "BundleContentHash",
                embeddedReportExportContentHashField = "ReportExportContentHash",
                verificationResultContentHashField = "verificationResultContentHash",
                receiptContentHashField = "receiptContentHash",
                verificationChecksOrder = new[]
                {
                    "bundle-non-null",
                    "schema-version",
                    "required-metadata",
                    "json-artifact-descriptor",
                    "embedded-report-export",
                    "embedded-report-export-hash",
                    "preserved-report-export-hash",
                    "bundle-sha256",
                    "observational-boundary"
                },
                verificationErrorsOrder = Array.Empty<string>()
            },
            boundaries = new
            {
                noMedicalAdvice = true,
                noPdfGeneration = true,
                noPersistenceDatabaseOrFileWriteClaimsExceptReceiptEmission = true,
                noProtocolIntelligenceRuntimeBehavior = true
            }
        };

        return JsonSerializer.Serialize(manifest);
    }

    private static string[] ReadPropertyNames(JsonElement element)
    {
        return element.EnumerateObject().Select(property => property.Name).ToArray();
    }

    private static string CanonicalizeJson(string json)
    {
        return JsonNode.Parse(json)!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
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

    private static string BundleFixturePath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "backend",
            "tests",
            "BioStack.Application.Tests",
            "Fixtures",
            "ProtocolOperationsExportBundle",
            "ProtocolOperationsExportBundle.golden.json");
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
