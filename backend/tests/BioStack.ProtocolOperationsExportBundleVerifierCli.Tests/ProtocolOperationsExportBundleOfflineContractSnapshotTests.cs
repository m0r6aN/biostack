namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

public sealed class ProtocolOperationsExportBundleOfflineContractSnapshotTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";

    [Fact]
    public void BundleJson_ContractShapeAndBoundaryLanguage_MatchSnapshot()
    {
        using var bundle = ParseJson(ReadFixtureJson());

        Assert.Equal(
            ["Metadata", "ReportExport", "Artifacts", "Integrity", "Disclaimer"],
            ReadPropertyNames(bundle.RootElement));
        Assert.Equal(
            ["SchemaVersion", "GeneratedAtUtc", "ProfileId", "ProtocolId"],
            ReadPropertyNames(bundle.RootElement.GetProperty("Metadata")));
        Assert.Equal(
            ["Metadata", "Report", "Integrity", "Disclaimer"],
            ReadPropertyNames(bundle.RootElement.GetProperty("ReportExport")));
        Assert.Equal(
            ["HashAlgorithm", "BundleContentHash", "ReportExportContentHash"],
            ReadPropertyNames(bundle.RootElement.GetProperty("Integrity")));

        var artifact = bundle.RootElement.GetProperty("Artifacts")[0];
        Assert.Equal(
            ["ArtifactId", "MediaType", "Role", "SchemaVersion", "ContentHash"],
            ReadPropertyNames(artifact));

        var report = bundle.RootElement.GetProperty("ReportExport").GetProperty("Report");
        Assert.Equal(
            ["ProfileId", "ProtocolId", "GeneratedAtUtc", "Summary", "RecentEvents", "EvidenceReferences", "Warnings"],
            ReadPropertyNames(report));
        Assert.Equal(
            ["ActiveCompoundsCount", "LoggedDosesCount", "CheckInCount", "MonitoringEntryCount", "MilestoneCount", "EvidenceReferenceCount", "LatestActivityUtc"],
            ReadPropertyNames(report.GetProperty("Summary")));

        var payload = ReadFixtureJson();
        Assert.DoesNotContain("pdf-generated", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("persisted-output", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Protocol Intelligence runtime", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user-facing", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("diagnosis", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("treatment", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dosing", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prescription", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recommendation", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("medical advice", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReceiptJson_ContractShapeAndBoundaryLanguage_MatchSnapshot()
    {
        using var receipt = ParseJson(EmitReceiptJson());

        Assert.Equal(
            [
                "receiptSchemaId",
                "receiptSchemaVersion",
                "verifierSchemaId",
                "verifierSchemaVersion",
                "status",
                "bundleSchemaId",
                "bundleSchemaVersion",
                "computedBundleContentHash",
                "suppliedBundleContentHash",
                "computedReportExportContentHash",
                "suppliedReportExportContentHash",
                "checks",
                "errors",
                "boundaries",
                "verificationResultContentHash",
                "receiptContentHash"
            ],
            ReadPropertyNames(receipt.RootElement));
        Assert.Equal(
            ["observationalOnly", "nonMedical", "noPersistence", "noPdf", "noRuntimeExpansion"],
            ReadPropertyNames(receipt.RootElement.GetProperty("boundaries")));
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
            ReadStringArray(receipt.RootElement.GetProperty("checks")));
        Assert.Empty(ReadStringArray(receipt.RootElement.GetProperty("errors")));

        var payload = receipt.RootElement.GetRawText();
        Assert.DoesNotContain("pdf-generated", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("persisted-output", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Protocol Intelligence runtime", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user-facing", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("diagnosis", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("treatment", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dosing", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prescription", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recommendation", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("medical advice", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReceiptContentHash_ExcludesOnlyReceiptContentHashField()
    {
        var receiptJson = EmitReceiptJson();
        var tamperedReceipt = UpdateReceipt(receiptJson, root =>
        {
            root["receiptContentHash"] = new string('a', 64);
        });

        var result = VerifyReceiptJson(tamperedReceipt);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("receipt-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("verification-result-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiptHashContracts_FailWhenVerificationResultFieldOrderOrValuesDrift()
    {
        var receiptJson = EmitReceiptJson();
        var driftedStatusReceipt = UpdateReceipt(receiptJson, root =>
        {
            root["status"] = "verification-failed";
            root["receiptContentHash"] = ComputeExpectedReceiptContentHash(root);
        });

        var result = VerifyReceiptJson(driftedStatusReceipt);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("verification-result-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("receipt-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiptHashContracts_FailWhenReceiptShapeFieldsDrift()
    {
        var receiptJson = EmitReceiptJson();
        var driftedReceipt = UpdateReceipt(receiptJson, root =>
        {
            var boundaries = root["boundaries"]!.AsObject();
            boundaries.Remove("noPdf");
            root["receiptContentHash"] = ComputeExpectedReceiptContentHash(root);
        });

        var result = VerifyReceiptJson(driftedReceipt);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("receipt-boundaries-missing", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("receipt-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiptJson_SerializationIsByteStableAndHashStable()
    {
        var first = EmitReceiptJson();
        var second = EmitReceiptJson();

        Assert.Equal(first, second);

        using var firstReceipt = ParseJson(first);
        using var secondReceipt = ParseJson(second);
        Assert.Equal(
            ComputeExpectedVerificationResultContentHash(firstReceipt.RootElement),
            ComputeExpectedVerificationResultContentHash(secondReceipt.RootElement));
        Assert.Equal(
            ComputeExpectedReceiptContentHash(firstReceipt.RootElement),
            ComputeExpectedReceiptContentHash(secondReceipt.RootElement));
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string EmitReceiptJson()
    {
        var bundlePath = CreateTempBundleFile(ReadFixtureJson());
        var result = InvokeCli("--receipt-json", bundlePath);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
        return result.StandardOutput;
    }

    private static (int ExitCode, string StandardOutput, string StandardError) VerifyReceiptJson(string receiptJson)
    {
        var receiptPath = CreateTempBundleFile(receiptJson);
        return InvokeCli("--verify-receipt-json", receiptPath);
    }

    private static JsonDocument ParseJson(string json) => JsonDocument.Parse(json);

    private static string[] ReadPropertyNames(JsonElement element) =>
        element.EnumerateObject().Select(property => property.Name).ToArray();

    private static string[] ReadStringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    private static string UpdateReceipt(string receiptJson, Action<JsonObject> mutate)
    {
        var root = JsonNode.Parse(receiptJson)!.AsObject();
        mutate(root);
        return root.ToJsonString();
    }

    private static string ComputeExpectedVerificationResultContentHash(JsonElement root)
    {
        var json = JsonSerializer.Serialize(
            new
            {
                status = root.GetProperty("status").GetString(),
                verifierSchemaId = root.GetProperty("verifierSchemaId").GetString(),
                verifierSchemaVersion = root.GetProperty("verifierSchemaVersion").GetString(),
                computedBundleContentHash = ReadOptionalString(root, "computedBundleContentHash"),
                suppliedBundleContentHash = ReadOptionalString(root, "suppliedBundleContentHash"),
                computedReportExportContentHash = ReadOptionalString(root, "computedReportExportContentHash"),
                suppliedReportExportContentHash = ReadOptionalString(root, "suppliedReportExportContentHash"),
                checks = ReadStringArray(root.GetProperty("checks")),
                errors = ReadStringArray(root.GetProperty("errors"))
            });

        return ComputeSha256(json);
    }

    private static string ComputeExpectedReceiptContentHash(JsonNode rootNode)
    {
        using var root = ParseJson(rootNode.ToJsonString());
        return ComputeExpectedReceiptContentHash(root.RootElement);
    }

    private static string ComputeExpectedReceiptContentHash(JsonElement root)
    {
        var boundaries = root.GetProperty("boundaries");
        var json = JsonSerializer.Serialize(
            new
            {
                receiptSchemaId = root.GetProperty("receiptSchemaId").GetString(),
                receiptSchemaVersion = root.GetProperty("receiptSchemaVersion").GetString(),
                verifierSchemaId = root.GetProperty("verifierSchemaId").GetString(),
                verifierSchemaVersion = root.GetProperty("verifierSchemaVersion").GetString(),
                status = root.GetProperty("status").GetString(),
                bundleSchemaId = ReadOptionalString(root, "bundleSchemaId"),
                bundleSchemaVersion = ReadOptionalString(root, "bundleSchemaVersion"),
                computedBundleContentHash = ReadOptionalString(root, "computedBundleContentHash"),
                suppliedBundleContentHash = ReadOptionalString(root, "suppliedBundleContentHash"),
                computedReportExportContentHash = ReadOptionalString(root, "computedReportExportContentHash"),
                suppliedReportExportContentHash = ReadOptionalString(root, "suppliedReportExportContentHash"),
                checks = ReadStringArray(root.GetProperty("checks")),
                errors = ReadStringArray(root.GetProperty("errors")),
                boundaries = new
                {
                    observationalOnly = boundaries.GetProperty("observationalOnly").GetBoolean(),
                    nonMedical = boundaries.GetProperty("nonMedical").GetBoolean(),
                    noPersistence = boundaries.GetProperty("noPersistence").GetBoolean(),
                    noPdf = boundaries.TryGetProperty("noPdf", out var noPdf) && noPdf.GetBoolean(),
                    noRuntimeExpansion = boundaries.GetProperty("noRuntimeExpansion").GetBoolean()
                },
                verificationResultContentHash = root.GetProperty("verificationResultContentHash").GetString()
            });

        return ComputeSha256(json);
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        var property = root.GetProperty(propertyName);
        return property.ValueKind is JsonValueKind.Null ? null : property.GetString();
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CreateTempBundleFile(string json)
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
}
