namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

public sealed class ProtocolOperationsExportBundleVerifierCliTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";
    private const string ExpectedReportExportSha256 = "61cf6bec473668ef2562dfaba7e579e269e499801ae0ab33010bb62868a0b779";
    private const string ExpectedBundleSha256 = "744054e7ead52b5473aec8e88e22ff5ffc37658a30061fbbd800c3b339c14c19";

    [Fact]
    public void Run_ReturnsZero_ForValidGoldenFixture()
    {
        var fixturePath = CreateTempBundleFile(ReadFixtureJson());

        var result = InvokeCli(fixturePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
    }

    [Fact]
    public void Run_PrintsStableSuccessSummary_ForValidGoldenFixture()
    {
        var fixturePath = CreateTempBundleFile(ReadFixtureJson());

        var result = InvokeCli(fixturePath);

        Assert.Equal(
            JoinLines(
                "status: verified",
                "schema-version: 1.0.0",
                $"expected-bundle-sha256: {ExpectedBundleSha256}",
                $"actual-bundle-sha256: {ExpectedBundleSha256}",
                $"expected-report-export-sha256: {ExpectedReportExportSha256}",
                $"actual-report-export-sha256: {ExpectedReportExportSha256}",
                "checks:",
                "- bundle-non-null",
                "- schema-version",
                "- required-metadata",
                "- json-artifact-descriptor",
                "- embedded-report-export",
                "- embedded-report-export-hash",
                "- preserved-report-export-hash",
                "- bundle-sha256",
                "- observational-boundary",
                "errors:",
                "- none"),
            result.StandardOutput);
    }

    [Fact]
    public void Run_ReturnsReceiptJson_ForValidGoldenFixture()
    {
        var fixturePath = CreateTempBundleFile(ReadFixtureJson());

        var result = InvokeCli("--receipt-json", fixturePath);
        using var receipt = ParseReceipt(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Equal("biostack.protocol-operations-export-bundle.verification-receipt", ReadString(receipt, "receiptSchemaId"));
        Assert.Equal("1.0.0", ReadString(receipt, "receiptSchemaVersion"));
        Assert.Equal("biostack.protocol-operations-export-bundle.verifier", ReadString(receipt, "verifierSchemaId"));
        Assert.Equal("1.0.0", ReadString(receipt, "verifierSchemaVersion"));
        Assert.Equal("biostack.protocol-operations-export-bundle", ReadString(receipt, "bundleSchemaId"));
        Assert.Equal("1.0.0", ReadString(receipt, "bundleSchemaVersion"));
        Assert.Equal("verified", ReadString(receipt, "status"));
        Assert.Equal(ExpectedBundleSha256, ReadString(receipt, "computedBundleContentHash"));
        Assert.Equal(ExpectedBundleSha256, ReadString(receipt, "suppliedBundleContentHash"));
        Assert.Equal(ExpectedReportExportSha256, ReadString(receipt, "computedReportExportContentHash"));
        Assert.Equal(ExpectedReportExportSha256, ReadString(receipt, "suppliedReportExportContentHash"));
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
                "observational-boundary",
            ],
            ReadStringArray(receipt, "checks"));
        Assert.Empty(ReadStringArray(receipt, "errors"));
        Assert.Equal("true", receipt.RootElement.GetProperty("boundaries").GetProperty("observationalOnly").GetBoolean().ToString().ToLowerInvariant());
        Assert.Equal("true", receipt.RootElement.GetProperty("boundaries").GetProperty("nonMedical").GetBoolean().ToString().ToLowerInvariant());
        Assert.Equal("true", receipt.RootElement.GetProperty("boundaries").GetProperty("noPersistence").GetBoolean().ToString().ToLowerInvariant());
        Assert.Equal("true", receipt.RootElement.GetProperty("boundaries").GetProperty("noPdf").GetBoolean().ToString().ToLowerInvariant());
        Assert.Equal("true", receipt.RootElement.GetProperty("boundaries").GetProperty("noRuntimeExpansion").GetBoolean().ToString().ToLowerInvariant());
        Assert.Equal(
            ComputeExpectedVerificationResultContentHash(receipt.RootElement),
            ReadString(receipt, "verificationResultContentHash"));
        Assert.Equal(
            ComputeExpectedReceiptContentHash(receipt.RootElement),
            ReadString(receipt, "receiptContentHash"));
    }

    [Fact]
    public void Run_VerifiesValidSuccessReceiptJson()
    {
        var fixturePath = CreateTempBundleFile(ReadFixtureJson());
        var receiptPath = CreateTempBundleFile(InvokeCli("--receipt-json", fixturePath).StandardOutput);

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            JoinLines(
                "Protocol Operations Export Bundle Verification Receipt: VALID",
                "Status: verified",
                $"ReceiptContentHash: {ReadString(ParseReceipt(File.ReadAllText(receiptPath)), "receiptContentHash")}"),
            result.StandardOutput);
    }

    [Fact]
    public void Run_VerifiesValidSupportedFailureReceiptJson()
    {
        var receiptJson = InvokeCli("--receipt-json", Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json")).StandardOutput;
        var receiptPath = CreateTempBundleFile(receiptJson);

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Protocol Operations Export Bundle Verification Receipt: VALID", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("Status: missing-file", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReceiptJson_IsByteForByteDeterministic_ForRepeatedValidGoldenFixtureRuns()
    {
        var fixturePath = CreateTempBundleFile(ReadFixtureJson());

        var result1 = InvokeCli("--receipt-json", fixturePath);
        var result2 = InvokeCli("--receipt-json", fixturePath);

        Assert.Equal(0, result1.ExitCode);
        Assert.Equal(0, result2.ExitCode);
        Assert.Equal(result1.StandardOutput, result2.StandardOutput);
    }

    [Fact]
    public void Run_VerifyReceiptJson_FailsClosed_WhenReceiptFileIsMissing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        var result = InvokeCli("--verify-receipt-json", missingPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            JoinLines(
                "Protocol Operations Export Bundle Verification Receipt: INVALID",
                "Status: missing-file",
                "Errors: input-file-missing"),
            result.StandardOutput);
    }

    [Fact]
    public void Run_VerifyReceiptJson_FailsClosed_WhenReceiptJsonIsInvalid()
    {
        var receiptPath = CreateTempBundleFile("{ not-json");

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            JoinLines(
                "Protocol Operations Export Bundle Verification Receipt: INVALID",
                "Status: invalid-json",
                "Errors: input-json-invalid"),
            result.StandardOutput);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenReceiptSchemaIdDrifts()
    {
        var receiptPath = CreateTempBundleFile(TamperReceipt("receiptSchemaId", "drifted-receipt-schema-id"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("receipt-schema-id-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenReceiptSchemaVersionDrifts()
    {
        var receiptPath = CreateTempBundleFile(TamperReceipt("receiptSchemaVersion", "9.9.9"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("receipt-schema-version-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenReceiptContentHashIsTampered()
    {
        var receiptPath = CreateTempBundleFile(TamperReceipt("receiptContentHash", new string('a', 64)));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("receipt-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenStatusIsTampered()
    {
        var receiptPath = CreateTempBundleFile(TamperReceipt("status", "persist this runtime output"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("receipt-status-invalid", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("persisted-output-claim-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenChecksAreReordered()
    {
        var receiptPath = CreateTempBundleFile(TamperReceiptArrayOrder("checks"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("verification-result-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("receipt-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenErrorsAreReordered()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Bundle persisted to C:\\\\exports\\\\protocol-operations-report.json.",
                StringComparison.Ordinal));
        var failureReceipt = InvokeCli("--receipt-json", fixturePath).StandardOutput;
        var receiptPath = CreateTempBundleFile(TamperReceiptArrayOrder("errors", failureReceipt));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("verification-result-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("receipt-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenBoundaryFieldIsRemoved()
    {
        var receiptPath = CreateTempBundleFile(RemoveReceiptProperty("boundaries", "noPdf"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("receipt-boundaries-missing", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenPdfWordingIsIntroduced()
    {
        var receiptPath = CreateTempBundleFile(TamperReceipt("status", "pdf-generated"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("pdf-claim-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenRuntimeWordingIsIntroduced()
    {
        var receiptPath = CreateTempBundleFile(TamperReceipt("status", "Protocol Intelligence runtime output"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("protocol-intelligence-runtime-language-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("recommendation")]
    [InlineData("diagnosis")]
    [InlineData("Take 25 mg nightly.")]
    [InlineData("treatment plan")]
    [InlineData("prescription")]
    public void Run_VerifyReceiptJson_Fails_WhenMedicalAdviceLanguageIsIntroduced(string forbiddenValue)
    {
        var receiptPath = CreateTempBundleFile(TamperReceipt("status", forbiddenValue));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("medical-advice-language-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenSuccessReceiptIsMissingBundleHashBinding()
    {
        var receiptPath = CreateTempBundleFile(TamperReceiptToNull("computedBundleContentHash"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("success-receipt-bundle-hash-missing", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_Fails_WhenSuccessReceiptIsMissingReportHashBinding()
    {
        var receiptPath = CreateTempBundleFile(TamperReceiptToNull("computedReportExportContentHash"));

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("success-receipt-report-export-hash-missing", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_AllowsFailureReceiptWithoutUnavailableBindings()
    {
        var receiptJson = InvokeCli("--receipt-json", Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json")).StandardOutput;
        var receiptPath = CreateTempBundleFile(receiptJson);

        var result = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Status: missing-file", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_VerifyReceiptJson_IsDeterministicAcrossRepeatedRuns()
    {
        var receiptPath = CreateTempBundleFile(InvokeCli("--receipt-json", CreateTempBundleFile(ReadFixtureJson())).StandardOutput);

        var result1 = InvokeCli("--verify-receipt-json", receiptPath);
        var result2 = InvokeCli("--verify-receipt-json", receiptPath);

        Assert.Equal(0, result1.ExitCode);
        Assert.Equal(result1.StandardOutput, result2.StandardOutput);
    }

    [Fact]
    public void Run_ReturnsDeterministicFailureReceipt_ForTamperedBundle()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(ExpectedBundleSha256, new string('b', 64), StringComparison.Ordinal));

        var result = InvokeCli("--receipt-json", fixturePath);
        using var receipt = ParseReceipt(result.StandardOutput);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("verification-failed", ReadString(receipt, "status"));
        Assert.Equal(ExpectedBundleSha256, ReadString(receipt, "computedBundleContentHash"));
        Assert.Equal(new string('b', 64), ReadString(receipt, "suppliedBundleContentHash"));
        Assert.Equal(
            ["bundle-sha256-mismatch"],
            ReadStringArray(receipt, "errors"));
        Assert.Equal(
            ComputeExpectedVerificationResultContentHash(receipt.RootElement),
            ReadString(receipt, "verificationResultContentHash"));
        Assert.Equal(
            ComputeExpectedReceiptContentHash(receipt.RootElement),
            ReadString(receipt, "receiptContentHash"));
    }

    [Fact]
    public void Run_ReturnsDeterministicFailureReceipt_ForPersistenceBoundaryViolation()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Bundle persisted to C:\\\\exports\\\\protocol-operations-report.json.",
                StringComparison.Ordinal));

        var result = InvokeCli("--receipt-json", fixturePath);
        using var receipt = ParseReceipt(result.StandardOutput);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("verification-failed", ReadString(receipt, "status"));
        Assert.Contains("persisted-output-claim-not-allowed", ReadStringArray(receipt, "errors"));
        Assert.Equal(
            ComputeExpectedVerificationResultContentHash(receipt.RootElement),
            ReadString(receipt, "verificationResultContentHash"));
        Assert.Equal(
            ComputeExpectedReceiptContentHash(receipt.RootElement),
            ReadString(receipt, "receiptContentHash"));
    }

    [Fact]
    public void Run_ReturnsDeterministicFailureReceipt_ForProtocolIntelligenceRuntimeBoundaryViolation()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Protocol Intelligence runtime generated export.",
                StringComparison.Ordinal));

        var result = InvokeCli("--receipt-json", fixturePath);
        using var receipt = ParseReceipt(result.StandardOutput);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("verification-failed", ReadString(receipt, "status"));
        Assert.Contains("protocol-intelligence-runtime-language-not-allowed", ReadStringArray(receipt, "errors"));
    }

    [Fact]
    public void Run_ReturnsDeterministicFailureReceipt_ForMedicalAdviceBoundaryViolation()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Take 25 mg nightly.",
                StringComparison.Ordinal));

        var result = InvokeCli("--receipt-json", fixturePath);
        using var receipt = ParseReceipt(result.StandardOutput);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("verification-failed", ReadString(receipt, "status"));
        Assert.Contains("medical-advice-language-not-allowed", ReadStringArray(receipt, "errors"));
    }

    [Fact]
    public void Run_ReturnsMinimalFailureReceiptWithoutBundleClaims_WhenFileIsMissing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        var result = InvokeCli("--receipt-json", missingPath);
        using var receipt = ParseReceipt(result.StandardOutput);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("missing-file", ReadString(receipt, "status"));
        Assert.Equal("biostack.protocol-operations-export-bundle.verification-receipt", ReadString(receipt, "receiptSchemaId"));
        Assert.Equal("1.0.0", ReadString(receipt, "receiptSchemaVersion"));
        Assert.Equal(["input-file-missing"], ReadStringArray(receipt, "errors"));
        Assert.True(receipt.RootElement.GetProperty("bundleSchemaId").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("bundleSchemaVersion").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("computedBundleContentHash").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("suppliedBundleContentHash").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("computedReportExportContentHash").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("suppliedReportExportContentHash").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public void Run_ReturnsMinimalFailureReceiptWithoutBundleClaims_WhenJsonIsInvalid()
    {
        var fixturePath = CreateTempBundleFile("{ not-json");

        var result = InvokeCli("--receipt-json", fixturePath);
        using var receipt = ParseReceipt(result.StandardOutput);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("invalid-json", ReadString(receipt, "status"));
        Assert.Equal(["input-json-invalid"], ReadStringArray(receipt, "errors"));
        Assert.True(receipt.RootElement.GetProperty("bundleSchemaId").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("bundleSchemaVersion").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("computedBundleContentHash").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("suppliedBundleContentHash").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("computedReportExportContentHash").ValueKind is JsonValueKind.Null);
        Assert.True(receipt.RootElement.GetProperty("suppliedReportExportContentHash").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public void Run_ReceiptJson_DoesNotIncludeHostOrPathSpecificData()
    {
        var fixturePath = CreateTempBundleFile(ReadFixtureJson());

        var result = InvokeCli("--receipt-json", fixturePath);

        Assert.DoesNotContain(fixturePath, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.GetDirectoryName(fixturePath)!, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.CurrentDirectory, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.MachineName, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.UserName, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("GeneratedAtUtc", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("T12:00:00Z", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("PATH", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("\"stackTrace\"", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenBundleHashIsTampered()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(ExpectedBundleSha256, new string('b', 64), StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("status: verification-failed", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- bundle-sha256-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenReportHashIsTampered()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(ExpectedReportExportSha256, new string('a', 64), StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- embedded-report-export-sha256-mismatch", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- json-artifact-content-hash-mismatch", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenPdfArtifactClaimIsPresent()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace("application/json", "application/pdf", StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- pdf-artifact-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenPersistenceClaimIsPresent()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Bundle persisted to C:\\\\exports\\\\protocol-operations-report.json.",
                StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- persisted-output-claim-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenProtocolIntelligenceRuntimeLanguageIsPresent()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Protocol Intelligence runtime generated export.",
                StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- protocol-intelligence-runtime-language-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenMedicalAdviceLanguageIsPresent()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Take 25 mg nightly.",
                StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("- medical-advice-language-not-allowed", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenFileIsMissing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        var result = InvokeCli(missingPath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(
            JoinLines(
                "status: missing-file",
                "schema-version: (unavailable)",
                "expected-bundle-sha256: (unavailable)",
                "actual-bundle-sha256: (unavailable)",
                "expected-report-export-sha256: (unavailable)",
                "actual-report-export-sha256: (unavailable)",
                "checks:",
                "- none",
                "errors:",
                "- input-file-missing"),
            result.StandardOutput);
    }

    [Fact]
    public void Run_ReturnsNonZero_WhenJsonIsInvalid()
    {
        var fixturePath = CreateTempBundleFile("{ not-json");

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("status: invalid-json", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- input-json-invalid", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_PreservesStableOrderedErrors_InFailureOutput()
    {
        var fixturePath = CreateTempBundleFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Bundle persisted to C:\\\\exports\\\\protocol-operations-report.json.",
                StringComparison.Ordinal));

        var result = InvokeCli(fixturePath);

        Assert.Equal(
            [
                "- bundle-sha256-mismatch",
                "- embedded-report-export-sha256-mismatch",
                "- json-artifact-content-hash-mismatch",
                "- persisted-output-claim-not-allowed",
            ],
            ReadErrorLines(result.StandardOutput));
    }

    [Fact]
    public void Run_DoesNotMutateInputMaterial()
    {
        var originalJson = ReadFixtureJson();
        var fixturePath = CreateTempBundleFile(originalJson);

        var result = InvokeCli(fixturePath);
        var fileJson = File.ReadAllText(fixturePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(originalJson, fileJson);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static JsonDocument ParseReceipt(string output) => JsonDocument.Parse(output);

    private static string ReadString(JsonElement element, string propertyName)
        => element.GetProperty(propertyName).GetString() ?? string.Empty;

    private static string ReadString(JsonDocument document, string propertyName)
        => document.RootElement.GetProperty(propertyName).GetString() ?? string.Empty;

    private static string[] ReadStringArray(JsonDocument document, string propertyName)
        => document.RootElement.GetProperty(propertyName).EnumerateArray().Select(element => element.GetString() ?? string.Empty).ToArray();

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
                checks = root.GetProperty("checks").EnumerateArray().Select(element => element.GetString()).ToArray(),
                errors = root.GetProperty("errors").EnumerateArray().Select(element => element.GetString()).ToArray(),
            });

        return ComputeSha256(json);
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
                checks = root.GetProperty("checks").EnumerateArray().Select(element => element.GetString()).ToArray(),
                errors = root.GetProperty("errors").EnumerateArray().Select(element => element.GetString()).ToArray(),
                boundaries = new
                {
                    observationalOnly = boundaries.GetProperty("observationalOnly").GetBoolean(),
                    nonMedical = boundaries.GetProperty("nonMedical").GetBoolean(),
                    noPersistence = boundaries.GetProperty("noPersistence").GetBoolean(),
                    noPdf = boundaries.GetProperty("noPdf").GetBoolean(),
                    noRuntimeExpansion = boundaries.GetProperty("noRuntimeExpansion").GetBoolean(),
                },
                verificationResultContentHash = root.GetProperty("verificationResultContentHash").GetString(),
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
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash);
    }

    private static string CreateTempBundleFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private static string TamperReceipt(string propertyName, string replacementValue)
        => TamperReceipt(propertyName, replacementValue, InvokeCli("--receipt-json", CreateTempBundleFile(ReadFixtureJson())).StandardOutput);

    private static string TamperReceipt(string propertyName, string replacementValue, string receiptJson)
    {
        using var document = ParseReceipt(receiptJson);
        var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(receiptJson)!;
        payload[propertyName] = replacementValue;
        return JsonSerializer.Serialize(payload);
    }

    private static string TamperReceiptToNull(string propertyName)
    {
        var receiptJson = InvokeCli("--receipt-json", CreateTempBundleFile(ReadFixtureJson())).StandardOutput;
        var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(receiptJson)!;
        payload[propertyName] = null;
        return JsonSerializer.Serialize(payload);
    }

    private static string TamperReceiptArrayOrder(string propertyName)
        => TamperReceiptArrayOrder(propertyName, InvokeCli("--receipt-json", CreateTempBundleFile(ReadFixtureJson())).StandardOutput);

    private static string TamperReceiptArrayOrder(string propertyName, string receiptJson)
    {
        using var document = ParseReceipt(receiptJson);
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(receiptJson)!;
        var array = payload[propertyName].EnumerateArray().Select(element => element.GetString()).Reverse().ToArray();
        var serializedPayload = JsonSerializer.Deserialize<Dictionary<string, object?>>(receiptJson)!;
        serializedPayload[propertyName] = array;
        return JsonSerializer.Serialize(serializedPayload);
    }

    private static string RemoveReceiptProperty(string objectPropertyName, string nestedPropertyName)
    {
        var receiptJson = InvokeCli("--receipt-json", CreateTempBundleFile(ReadFixtureJson())).StandardOutput;
        var payload = JsonNode.Parse(receiptJson)!.AsObject();
        payload[objectPropertyName]!.AsObject().Remove(nestedPropertyName);
        return payload.ToJsonString();
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

    private static string JoinLines(params string[] lines) =>
        string.Join(Environment.NewLine, lines) + Environment.NewLine;

    private static IReadOnlyList<string> ReadErrorLines(string output)
    {
        var lines = output
            .Split(Environment.NewLine, StringSplitOptions.None)
            .SkipWhile(line => !string.Equals(line, "errors:", StringComparison.Ordinal))
            .Skip(1)
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .ToArray();

        return lines;
    }
}
