namespace BioStack.ProtocolOperationsExportBundleVerifierCli;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.Contracts.Responses;

internal static class ProtocolOperationsExportBundleVerificationReceiptJson
{
    internal const string ReceiptSchemaId = "biostack.protocol-operations-export-bundle.verification-receipt";
    internal const string ReceiptSchemaVersion = "1.0.0";
    internal const string VerifierSchemaId = "biostack.protocol-operations-export-bundle.verifier";
    internal const string VerifierSchemaVersion = "1.0.0";
    internal const string BundleSchemaId = "biostack.protocol-operations-export-bundle";
    internal const string VerifiedStatus = "verified";
    internal const string VerificationFailedStatus = "verification-failed";
    internal const string MissingFileStatus = "missing-file";
    internal const string InvalidJsonStatus = "invalid-json";
    internal const string InvalidInputStatus = "invalid-input";

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    internal static readonly Regex ForbiddenMedicalLanguage = new(
        @"\b(recommend(?:ation|ations|ed|ing|s)?|diagnos(?:is|es|e|ed|ing|tic)|dos(?:e|es|ed|ing|age)|treat(?:ment|ments|ed|ing|s)?|prescrib(?:e|ed|ing|es)|prescription(?:s)?|medical advice)\b|\btake\s+\d+(?:\.\d+)?\s*(?:mg|mcg|g|ml)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static readonly Regex ForbiddenPersistenceLanguage = new(
        @"(?:\b(?:persist(?:ed|ence)?|saved?|stored?|written)\b.{0,32}\b(?:file|path|output|disk|pdf|json|bundle)\b)|(?:\bfile://)|(?:[A-Za-z]:\\)|(?:^|[\s""'])(?:\.{1,2}/|/)[^\s]*\.(?:json|pdf)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static readonly Regex ForbiddenTimestampLanguage = new(
        @"\b\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static readonly Regex ForbiddenPdfLanguage = new(
        @"\bpdf\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static readonly string[] SuccessStatuses = [VerifiedStatus];
    internal static readonly string[] FailureStatuses = [VerificationFailedStatus, MissingFileStatus, InvalidJsonStatus, InvalidInputStatus];

    public static void Write(
        TextWriter stdout,
        string status,
        ProtocolOperationsExportBundle? bundle,
        ProtocolOperationsExportBundleVerificationResult? result,
        IReadOnlyList<string>? errorsOverride)
    {
        var receipt = Create(status, bundle, result, errorsOverride);
        stdout.Write(Serialize(receipt));
    }

    internal static ProtocolOperationsExportBundleVerificationReceipt Create(
        string status,
        ProtocolOperationsExportBundle? bundle,
        ProtocolOperationsExportBundleVerificationResult? result,
        IReadOnlyList<string>? errorsOverride)
    {
        var checks = result?.Checks?.ToArray() ?? [];
        var errors = errorsOverride?.ToArray() ?? result?.Errors?.ToArray() ?? [];
        var receipt = new ProtocolOperationsExportBundleVerificationReceipt(
            ReceiptSchemaId,
            ReceiptSchemaVersion,
            VerifierSchemaId,
            VerifierSchemaVersion,
            status,
            bundle is null ? null : BundleSchemaId,
            bundle?.Metadata?.SchemaVersion,
            NormalizeHash(result?.ActualBundleSha256),
            NormalizeHash(result?.ExpectedBundleSha256),
            NormalizeHash(result?.ActualReportExportSha256),
            NormalizeHash(result?.ExpectedReportExportSha256),
            checks,
            errors,
            new ProtocolOperationsExportBundleVerificationReceiptBoundaries(
                ObservationalOnly: true,
                NonMedical: true,
                NoPersistence: true,
                NoPdf: true,
                NoRuntimeExpansion: true),
            VerificationResultContentHash: string.Empty,
            ReceiptContentHash: string.Empty);

        var verificationHash = ComputeVerificationResultContentHash(receipt);
        var receiptWithVerificationHash = receipt with { VerificationResultContentHash = verificationHash };
        var receiptHash = ComputeReceiptContentHash(receiptWithVerificationHash);
        return receiptWithVerificationHash with { ReceiptContentHash = receiptHash };
    }

    internal static ProtocolOperationsExportBundleVerificationReceipt Parse(string json)
        => JsonSerializer.Deserialize<ProtocolOperationsExportBundleVerificationReceipt>(json, SerializerOptions)
            ?? throw new JsonException("Receipt payload deserialized to null.");

    internal static string Serialize(ProtocolOperationsExportBundleVerificationReceipt receipt)
        => JsonSerializer.Serialize(receipt, SerializerOptions);

    internal static string ComputeVerificationResultContentHash(ProtocolOperationsExportBundleVerificationReceipt receipt)
    {
        var material = new ProtocolOperationsExportBundleVerificationReceiptHashMaterial(
            receipt.Status,
            receipt.VerifierSchemaId,
            receipt.VerifierSchemaVersion,
            receipt.ComputedBundleContentHash,
            receipt.SuppliedBundleContentHash,
            receipt.ComputedReportExportContentHash,
            receipt.SuppliedReportExportContentHash,
            receipt.Checks,
            receipt.Errors);

        return ComputeSha256(JsonSerializer.Serialize(material, SerializerOptions));
    }

    internal static string ComputeReceiptContentHash(ProtocolOperationsExportBundleVerificationReceipt receipt)
    {
        var withoutHash = new ProtocolOperationsExportBundleVerificationReceiptWithoutHash(
            receipt.ReceiptSchemaId,
            receipt.ReceiptSchemaVersion,
            receipt.VerifierSchemaId,
            receipt.VerifierSchemaVersion,
            receipt.Status,
            receipt.BundleSchemaId,
            receipt.BundleSchemaVersion,
            receipt.ComputedBundleContentHash,
            receipt.SuppliedBundleContentHash,
            receipt.ComputedReportExportContentHash,
            receipt.SuppliedReportExportContentHash,
            receipt.Checks,
            receipt.Errors,
            receipt.Boundaries,
            receipt.VerificationResultContentHash);

        return ComputeSha256(JsonSerializer.Serialize(withoutHash, SerializerOptions));
    }

    internal static bool IsSuccessStatus(string status)
        => SuccessStatuses.Contains(status, StringComparer.Ordinal);

    internal static bool IsFailureStatus(string status)
        => FailureStatuses.Contains(status, StringComparer.Ordinal);

    internal static bool AllowsMissingBindings(string status)
        => status is MissingFileStatus or InvalidJsonStatus or InvalidInputStatus;

    internal static void AppendForbiddenContentErrors(string payload, ICollection<string> errors)
    {
        if (payload.Contains("Protocol Intelligence runtime", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("protocol-intelligence-runtime-language-not-allowed");
        }

        if (ForbiddenMedicalLanguage.IsMatch(payload))
        {
            errors.Add("medical-advice-language-not-allowed");
        }

        if (ForbiddenPersistenceLanguage.IsMatch(payload))
        {
            errors.Add("persisted-output-claim-not-allowed");
        }

        if (ForbiddenPdfLanguage.IsMatch(payload))
        {
            errors.Add("pdf-claim-not-allowed");
        }

        if (ForbiddenTimestampLanguage.IsMatch(payload))
        {
            errors.Add("timestamp-not-allowed");
        }

        if (payload.Contains("machineName", StringComparison.Ordinal)
            || payload.Contains("userName", StringComparison.Ordinal)
            || payload.Contains("currentDirectory", StringComparison.Ordinal)
            || payload.Contains("environment", StringComparison.Ordinal)
            || payload.Contains("processId", StringComparison.Ordinal)
            || payload.Contains("stackTrace", StringComparison.Ordinal)
            || payload.Contains("inputPath", StringComparison.Ordinal)
            || payload.Contains("GeneratedAtUtc", StringComparison.Ordinal))
        {
            errors.Add("host-data-not-allowed");
        }
    }

    private static string? NormalizeHash(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string ComputeSha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash);
    }
}

internal sealed record ProtocolOperationsExportBundleVerificationReceipt(
    string ReceiptSchemaId,
    string ReceiptSchemaVersion,
    string VerifierSchemaId,
    string VerifierSchemaVersion,
    string Status,
    string? BundleSchemaId,
    string? BundleSchemaVersion,
    string? ComputedBundleContentHash,
    string? SuppliedBundleContentHash,
    string? ComputedReportExportContentHash,
    string? SuppliedReportExportContentHash,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Errors,
    ProtocolOperationsExportBundleVerificationReceiptBoundaries Boundaries,
    string VerificationResultContentHash,
    string ReceiptContentHash);

internal sealed record ProtocolOperationsExportBundleVerificationReceiptBoundaries(
    bool ObservationalOnly,
    bool NonMedical,
    bool NoPersistence,
    bool NoPdf,
    bool NoRuntimeExpansion);

internal sealed record ProtocolOperationsExportBundleVerificationReceiptHashMaterial(
    string Status,
    string VerifierSchemaId,
    string VerifierSchemaVersion,
    string? ComputedBundleContentHash,
    string? SuppliedBundleContentHash,
    string? ComputedReportExportContentHash,
    string? SuppliedReportExportContentHash,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Errors);

internal sealed record ProtocolOperationsExportBundleVerificationReceiptWithoutHash(
    string ReceiptSchemaId,
    string ReceiptSchemaVersion,
    string VerifierSchemaId,
    string VerifierSchemaVersion,
    string Status,
    string? BundleSchemaId,
    string? BundleSchemaVersion,
    string? ComputedBundleContentHash,
    string? SuppliedBundleContentHash,
    string? ComputedReportExportContentHash,
    string? SuppliedReportExportContentHash,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Errors,
    ProtocolOperationsExportBundleVerificationReceiptBoundaries Boundaries,
    string VerificationResultContentHash);
