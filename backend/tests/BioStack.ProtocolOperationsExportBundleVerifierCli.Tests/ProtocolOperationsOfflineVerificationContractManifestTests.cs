namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

public sealed class ProtocolOperationsOfflineVerificationContractManifestTests
{
    [Fact]
    public void ProtocolOperationsOfflineVerificationContractManifest_DerivedCliSurfaceMatchesSnapshot()
    {
        var expected = CanonicalizeJson(File.ReadAllText(ManifestSnapshotPath()));
        var actual = CanonicalizeJson(BuildManifestFromCliSurface());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProtocolOperationsOfflineVerificationContractManifest_CliSurfaceFreezesExactFlagsAndOrdering()
    {
        using var manifest = JsonDocument.Parse(BuildManifestFromCliSurface());
        var cli = manifest.RootElement.GetProperty("cli");
        var hashSurfaces = manifest.RootElement.GetProperty("hashSurfaces");

        Assert.Equal("positional-bundle-json-input", cli.GetProperty("verifyBundleJsonMode").GetString());
        Assert.Equal("--receipt-json", cli.GetProperty("emitReceiptJsonFlag").GetString());
        Assert.Equal("--verify-receipt-json", cli.GetProperty("verifyReceiptJsonFlag").GetString());
        Assert.Equal(
            new[]
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
            ReadStringArray(hashSurfaces.GetProperty("verificationChecksOrder")));
        Assert.Empty(ReadStringArray(hashSurfaces.GetProperty("verificationErrorsOrder")));
    }

    private static string BuildManifestFromCliSurface()
    {
        using var receipt = JsonDocument.Parse(EmitReceiptJson());
        var root = receipt.RootElement;
        var boundaries = root.GetProperty("boundaries");
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
                schemaId = root.GetProperty("bundleSchemaId").GetString(),
                schemaVersion = root.GetProperty("bundleSchemaVersion").GetString(),
                hashAlgorithm = "SHA-256",
                topLevelFields = new[] { "Metadata", "ReportExport", "Artifacts", "Integrity", "Disclaimer" },
                metadataFields = new[] { "SchemaVersion", "GeneratedAtUtc", "ProfileId", "ProtocolId" },
                artifactFields = new[] { "ArtifactId", "MediaType", "Role", "SchemaVersion", "ContentHash" },
                integrityFields = new[] { "HashAlgorithm", "BundleContentHash", "ReportExportContentHash" }
            },
            receipt = new
            {
                receiptSchemaId = root.GetProperty("receiptSchemaId").GetString(),
                receiptSchemaVersion = root.GetProperty("receiptSchemaVersion").GetString(),
                verifierSchemaId = root.GetProperty("verifierSchemaId").GetString(),
                verifierSchemaVersion = root.GetProperty("verifierSchemaVersion").GetString(),
                boundaryFields = ReadPropertyNames(boundaries),
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
                verificationChecksOrder = ReadStringArray(root.GetProperty("checks")),
                verificationErrorsOrder = ReadStringArray(root.GetProperty("errors"))
            },
            boundaries = new
            {
                noMedicalAdvice = boundaries.GetProperty("nonMedical").GetBoolean(),
                noPdfGeneration = boundaries.GetProperty("noPdf").GetBoolean(),
                noPersistenceDatabaseOrFileWriteClaimsExceptReceiptEmission = boundaries.GetProperty("noPersistence").GetBoolean(),
                noProtocolIntelligenceRuntimeBehavior = boundaries.GetProperty("noRuntimeExpansion").GetBoolean()
            }
        };

        return JsonSerializer.Serialize(manifest);
    }

    private static string EmitReceiptJson()
    {
        var bundlePath = CreateTempJsonFile(ReadBundleFixtureJson());
        var result = InvokeCli("--receipt-json", bundlePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);

        return result.StandardOutput;
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string[] ReadPropertyNames(JsonElement element)
    {
        return element.EnumerateObject().Select(property => property.Name).ToArray();
    }

    private static string[] ReadStringArray(JsonElement element)
    {
        return element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
    }

    private static string ReadBundleFixtureJson()
    {
        return File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "backend",
            "tests",
            "BioStack.Application.Tests",
            "Fixtures",
            "ProtocolOperationsExportBundle",
            "ProtocolOperationsExportBundle.golden.json"));
    }

    private static string CreateTempJsonFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
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
