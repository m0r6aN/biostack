namespace BioStack.ProtocolOperationsExportBundleVerifierCli;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioStack.Contracts.Responses;

internal static class ProtocolOperationsExportBundleVerificationReceiptJson
{
    private const string ReceiptSchemaId = "biostack.protocol-operations-export-bundle.verification-receipt";
    private const string ReceiptSchemaVersion = "1.0.0";
    private const string VerifierSchemaId = "biostack.protocol-operations-export-bundle.verifier";
    private const string VerifierSchemaVersion = "1.0.0";
    private const string BundleSchemaId = "biostack.protocol-operations-export-bundle";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static void Write(
        TextWriter stdout,
        string status,
        ProtocolOperationsExportBundle? bundle,
        ProtocolOperationsExportBundleVerificationResult? result,
        IReadOnlyList<string>? errorsOverride)
    {
        var receiptWithoutHash = CreateReceiptWithoutHash(status, bundle, result, errorsOverride);
        var receiptContentHash = ComputeSha256(JsonSerializer.Serialize(receiptWithoutHash, SerializerOptions));
        var receipt = new Receipt(
            receiptWithoutHash.ReceiptSchemaId,
            receiptWithoutHash.ReceiptSchemaVersion,
            receiptWithoutHash.VerifierSchemaId,
            receiptWithoutHash.VerifierSchemaVersion,
            receiptWithoutHash.Status,
            receiptWithoutHash.BundleSchemaId,
            receiptWithoutHash.BundleSchemaVersion,
            receiptWithoutHash.ComputedBundleContentHash,
            receiptWithoutHash.SuppliedBundleContentHash,
            receiptWithoutHash.ComputedReportExportContentHash,
            receiptWithoutHash.SuppliedReportExportContentHash,
            receiptWithoutHash.Checks,
            receiptWithoutHash.Errors,
            receiptWithoutHash.Boundaries,
            receiptWithoutHash.VerificationResultContentHash,
            receiptContentHash);

        stdout.Write(JsonSerializer.Serialize(receipt, SerializerOptions));
    }

    private static ReceiptWithoutHash CreateReceiptWithoutHash(
        string status,
        ProtocolOperationsExportBundle? bundle,
        ProtocolOperationsExportBundleVerificationResult? result,
        IReadOnlyList<string>? errorsOverride)
    {
        var checks = result?.Checks?.ToArray() ?? [];
        var errors = errorsOverride?.ToArray() ?? result?.Errors?.ToArray() ?? [];
        var verificationResultContentHash = ComputeVerificationResultContentHash(status, result, errors);

        return new ReceiptWithoutHash(
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
            new ReceiptBoundaries(
                ObservationalOnly: true,
                NonMedical: true,
                NoPersistence: true,
                NoPdf: true,
                NoRuntimeExpansion: true),
            verificationResultContentHash);
    }

    private static string ComputeVerificationResultContentHash(
        string status,
        ProtocolOperationsExportBundleVerificationResult? result,
        IReadOnlyList<string> errors)
    {
        var material = new VerificationResultHashMaterial(
            Status: status,
            VerifierSchemaId: VerifierSchemaId,
            VerifierSchemaVersion: VerifierSchemaVersion,
            ComputedBundleContentHash: NormalizeHash(result?.ActualBundleSha256),
            SuppliedBundleContentHash: NormalizeHash(result?.ExpectedBundleSha256),
            ComputedReportExportContentHash: NormalizeHash(result?.ActualReportExportSha256),
            SuppliedReportExportContentHash: NormalizeHash(result?.ExpectedReportExportSha256),
            Checks: result?.Checks?.ToArray() ?? [],
            Errors: errors.ToArray());

        return ComputeSha256(JsonSerializer.Serialize(material, SerializerOptions));
    }

    private static string? NormalizeHash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string ComputeSha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash);
    }

    private sealed record ReceiptWithoutHash(
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
        ReceiptBoundaries Boundaries,
        string VerificationResultContentHash);

    private sealed record Receipt(
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
        ReceiptBoundaries Boundaries,
        string VerificationResultContentHash,
        string ReceiptContentHash);

    private sealed record VerificationResultHashMaterial(
        string Status,
        string VerifierSchemaId,
        string VerifierSchemaVersion,
        string? ComputedBundleContentHash,
        string? SuppliedBundleContentHash,
        string? ComputedReportExportContentHash,
        string? SuppliedReportExportContentHash,
        IReadOnlyList<string> Checks,
        IReadOnlyList<string> Errors);

    private sealed record ReceiptBoundaries(
        bool ObservationalOnly,
        bool NonMedical,
        bool NoPersistence,
        bool NoPdf,
        bool NoRuntimeExpansion);
}
