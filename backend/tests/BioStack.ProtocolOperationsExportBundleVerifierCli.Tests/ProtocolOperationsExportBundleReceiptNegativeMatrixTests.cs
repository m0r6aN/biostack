namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

public sealed class ProtocolOperationsExportBundleReceiptNegativeMatrixTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    [Fact]
    public void VerifyReceipt_RejectsBundleSchemaIdDrift_WhenReceiptHashIsRebound()
    {
        var receiptJson = MutateSuccessReceipt(root => root["bundleSchemaId"] = "drifted.bundle.schema");

        var result = VerifyReceipt(receiptJson);

        AssertRejected(result, "receipt-bundle-schema-id-mismatch");
    }

    [Fact]
    public void VerifyReceipt_RejectsBundleSchemaVersionDrift_WhenReceiptHashIsRebound()
    {
        var receiptJson = MutateSuccessReceipt(root => root["bundleSchemaVersion"] = "9.9.9");

        var result = VerifyReceipt(receiptJson);

        AssertRejected(result, "receipt-bundle-schema-version-mismatch");
    }

    [Fact]
    public void VerifyReceipt_RejectsVerifierIdDrift_WhenReceiptHashIsRebound()
    {
        var receiptJson = MutateSuccessReceipt(root => root["verifierSchemaId"] = "drifted.verifier");

        var result = VerifyReceipt(receiptJson);

        AssertRejected(result, "verifier-schema-id-mismatch");
    }

    [Fact]
    public void VerifyReceipt_RejectsVerifierVersionDrift_WhenReceiptHashIsRebound()
    {
        var receiptJson = MutateSuccessReceipt(root => root["verifierSchemaVersion"] = "9.9.9");

        var result = VerifyReceipt(receiptJson);

        AssertRejected(result, "verifier-schema-version-mismatch");
    }

    [Fact]
    public void VerifyReceipt_RejectsReceiptContentHashDrift()
    {
        var receipt = SuccessReceipt();
        receipt["receiptContentHash"] = new string('f', 64);

        var result = VerifyReceipt(receipt.ToJsonString(SerializerOptions));

        AssertRejected(result, "receipt-content-hash-mismatch");
    }

    [Fact]
    public void VerifyReceipt_RejectsVerificationMaterialHashDrift()
    {
        var receipt = SuccessReceipt();
        receipt["verificationResultContentHash"] = new string('e', 64);
        receipt["receiptContentHash"] = ComputeReceiptContentHash(receipt);

        var result = VerifyReceipt(receipt.ToJsonString(SerializerOptions));

        AssertRejected(result, "verification-result-content-hash-mismatch");
    }

    [Fact]
    public void VerifyReceipt_RejectsCapturedBundleHashDrift_WhenReceiptHashIsRebound()
    {
        var receiptJson = MutateSuccessReceipt(root => root["suppliedBundleContentHash"] = new string('a', 64));

        var result = VerifyReceipt(receiptJson);

        AssertRejected(result, "success-receipt-captured-result-not-successful");
    }

    [Fact]
    public void VerifyReceipt_RejectsCapturedReportHashDrift_WhenReceiptHashIsRebound()
    {
        var receiptJson = MutateSuccessReceipt(root => root["suppliedReportExportContentHash"] = new string('b', 64));

        var result = VerifyReceipt(receiptJson);

        AssertRejected(result, "success-receipt-captured-result-not-successful");
    }

    [Fact]
    public void VerifyReceipt_RejectsReorderedChecks_WhenReceiptHashIsRebound()
    {
        var receiptJson = MutateSuccessReceipt(root =>
        {
            var checks = root["checks"]!.AsArray();
            var reversed = new JsonArray(checks.Reverse().Select(item => JsonValue.Create(item!.GetValue<string>())).ToArray());
            root["checks"] = reversed;
        });

        var result = VerifyReceipt(receiptJson);

        AssertRejected(result, "receipt-check-order-mismatch");
    }

    [Fact]
    public void VerifyReceipt_RejectsSuccessClaimWithCapturedFailureErrors_WhenReceiptHashIsRebound()
    {
        var receiptJson = MutateSuccessReceipt(root =>
        {
            root["errors"] = new JsonArray("bundle-sha256-mismatch");
        });

        var result = VerifyReceipt(receiptJson);

        AssertRejected(result, "success-receipt-errors-present");
        AssertRejected(result, "success-receipt-captured-result-not-successful");
    }

    [Theory]
    [InlineData("status", "Protocol Intelligence runtime generated receipt.", "protocol-intelligence-runtime-language-not-allowed")]
    [InlineData("status", "Receipt persisted to C:\\exports\\receipt.json.", "persisted-output-claim-not-allowed")]
    [InlineData("status", "PDF receipt generated.", "pdf-claim-not-allowed")]
    [InlineData("status", "Take 25 mg nightly.", "medical-advice-language-not-allowed")]
    public void VerifyReceipt_RejectsForbiddenReceiptSurfaceLanguage(string propertyName, string value, string expectedError)
    {
        var receipt = SuccessReceipt();
        receipt[propertyName] = value;
        RebindReceiptHashes(receipt);

        var result = VerifyReceipt(receipt.ToJsonString(SerializerOptions));

        AssertRejected(result, expectedError);
    }

    private static string MutateSuccessReceipt(Action<JsonObject> mutate)
    {
        var receipt = SuccessReceipt();
        mutate(receipt);
        RebindReceiptHashes(receipt);
        return receipt.ToJsonString(SerializerOptions);
    }

    private static JsonObject SuccessReceipt()
    {
        var bundlePath = CreateTempFile(ReadFixtureJson());
        var result = InvokeCli("--receipt-json", bundlePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);

        return JsonNode.Parse(result.StandardOutput)?.AsObject()
            ?? throw new InvalidOperationException("Failed to parse receipt JSON.");
    }

    private static CliResult VerifyReceipt(string receiptJson)
        => InvokeCli("--verify-receipt-json", CreateTempFile(receiptJson));

    private static void AssertRejected(CliResult result, string expectedError)
    {
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Protocol Operations Export Bundle Verification Receipt: INVALID", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains(expectedError, result.StandardOutput, StringComparison.Ordinal);
    }

    private static void RebindReceiptHashes(JsonObject receipt)
    {
        receipt["verificationResultContentHash"] = ComputeVerificationResultContentHash(receipt);
        receipt["receiptContentHash"] = ComputeReceiptContentHash(receipt);
    }

    private static string ComputeVerificationResultContentHash(JsonObject receipt)
    {
        var material = new
        {
            Status = ReadString(receipt, "status"),
            VerifierSchemaId = ReadString(receipt, "verifierSchemaId"),
            VerifierSchemaVersion = ReadString(receipt, "verifierSchemaVersion"),
            ComputedBundleContentHash = ReadNullableString(receipt, "computedBundleContentHash"),
            SuppliedBundleContentHash = ReadNullableString(receipt, "suppliedBundleContentHash"),
            ComputedReportExportContentHash = ReadNullableString(receipt, "computedReportExportContentHash"),
            SuppliedReportExportContentHash = ReadNullableString(receipt, "suppliedReportExportContentHash"),
            Checks = ReadStringArray(receipt, "checks"),
            Errors = ReadStringArray(receipt, "errors"),
        };

        return ComputeSha256(JsonSerializer.Serialize(material, SerializerOptions));
    }

    private static string ComputeReceiptContentHash(JsonObject receipt)
    {
        var boundaries = receipt["boundaries"]!.AsObject();
        var withoutHash = new
        {
            ReceiptSchemaId = ReadString(receipt, "receiptSchemaId"),
            ReceiptSchemaVersion = ReadString(receipt, "receiptSchemaVersion"),
            VerifierSchemaId = ReadString(receipt, "verifierSchemaId"),
            VerifierSchemaVersion = ReadString(receipt, "verifierSchemaVersion"),
            Status = ReadString(receipt, "status"),
            BundleSchemaId = ReadNullableString(receipt, "bundleSchemaId"),
            BundleSchemaVersion = ReadNullableString(receipt, "bundleSchemaVersion"),
            ComputedBundleContentHash = ReadNullableString(receipt, "computedBundleContentHash"),
            SuppliedBundleContentHash = ReadNullableString(receipt, "suppliedBundleContentHash"),
            ComputedReportExportContentHash = ReadNullableString(receipt, "computedReportExportContentHash"),
            SuppliedReportExportContentHash = ReadNullableString(receipt, "suppliedReportExportContentHash"),
            Checks = ReadStringArray(receipt, "checks"),
            Errors = ReadStringArray(receipt, "errors"),
            Boundaries = new
            {
                ObservationalOnly = boundaries["observationalOnly"]!.GetValue<bool>(),
                NonMedical = boundaries["nonMedical"]!.GetValue<bool>(),
                NoPersistence = boundaries["noPersistence"]!.GetValue<bool>(),
                NoPdf = boundaries["noPdf"]!.GetValue<bool>(),
                NoRuntimeExpansion = boundaries["noRuntimeExpansion"]!.GetValue<bool>(),
            },
            VerificationResultContentHash = ReadString(receipt, "verificationResultContentHash"),
        };

        return ComputeSha256(JsonSerializer.Serialize(withoutHash, SerializerOptions));
    }

    private static string ReadString(JsonObject receipt, string propertyName)
        => receipt[propertyName]?.GetValue<string>() ?? string.Empty;

    private static string? ReadNullableString(JsonObject receipt, string propertyName)
        => receipt[propertyName] is null ? null : receipt[propertyName]!.GetValue<string>();

    private static string[] ReadStringArray(JsonObject receipt, string propertyName)
        => receipt[propertyName]!.AsArray().Select(item => item?.GetValue<string>() ?? string.Empty).ToArray();

    private static string ComputeSha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash);
    }

    private static CliResult InvokeCli(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);

        return new CliResult(exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string CreateTempFile(string content)
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

    private sealed record CliResult(int ExitCode, string StandardOutput, string StandardError);
}
