namespace BioStack.ProtocolOperationsExportBundleVerifierCli;

using System.Text.Json;
using BioStack.Contracts.Responses;

internal static class ProtocolOperationsExportBundleVerificationResultJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    internal static void WriteBundleVerificationResult(
        TextWriter stdout,
        string status,
        ProtocolOperationsExportBundleVerificationResult? result,
        IReadOnlyList<string>? errorsOverride)
    {
        var payload = new BundleVerificationResultPayload(
            status,
            ArtifactTypeChecked: "protocol-operations-export-bundle",
            SchemaIdChecked: "biostack.protocol-operations-export-bundle",
            SchemaVersionChecked: Normalize(result?.SchemaVersion),
            ExpectedBundleContentHash: Normalize(result?.ExpectedBundleSha256),
            ActualBundleContentHash: Normalize(result?.ActualBundleSha256),
            ExpectedReportExportContentHash: Normalize(result?.ExpectedReportExportSha256),
            ActualReportExportContentHash: Normalize(result?.ActualReportExportSha256),
            Checks: result?.Checks?.ToArray() ?? [],
            Errors: errorsOverride?.ToArray() ?? result?.Errors?.ToArray() ?? []);

        stdout.Write(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    internal static void WriteReceiptVerificationResult(TextWriter stdout, ReceiptVerificationResult verification)
    {
        var payload = new ReceiptVerificationResultPayload(
            verification.Status,
            ArtifactTypeChecked: "protocol-operations-export-bundle-verification-receipt",
            ReceiptSchemaIdChecked: Normalize(verification.ReceiptSchemaId),
            ReceiptSchemaVersionChecked: Normalize(verification.ReceiptSchemaVersion),
            VerifierSchemaIdChecked: Normalize(verification.VerifierSchemaId),
            VerifierSchemaVersionChecked: Normalize(verification.VerifierSchemaVersion),
            BundleSchemaIdChecked: Normalize(verification.BundleSchemaId),
            BundleSchemaVersionChecked: Normalize(verification.BundleSchemaVersion),
            ComputedBundleContentHash: Normalize(verification.ComputedBundleContentHash),
            SuppliedBundleContentHash: Normalize(verification.SuppliedBundleContentHash),
            ComputedReportExportContentHash: Normalize(verification.ComputedReportExportContentHash),
            SuppliedReportExportContentHash: Normalize(verification.SuppliedReportExportContentHash),
            VerificationResultContentHash: Normalize(verification.VerificationResultContentHash),
            ReceiptContentHash: Normalize(verification.ReceiptContentHash),
            Checks: verification.Checks.ToArray(),
            Errors: verification.Errors.ToArray(),
            Boundaries: verification.Boundaries);

        stdout.Write(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed record BundleVerificationResultPayload(
        string Status,
        string ArtifactTypeChecked,
        string SchemaIdChecked,
        string? SchemaVersionChecked,
        string? ExpectedBundleContentHash,
        string? ActualBundleContentHash,
        string? ExpectedReportExportContentHash,
        string? ActualReportExportContentHash,
        IReadOnlyList<string> Checks,
        IReadOnlyList<string> Errors);

    private sealed record ReceiptVerificationResultPayload(
        string Status,
        string ArtifactTypeChecked,
        string? ReceiptSchemaIdChecked,
        string? ReceiptSchemaVersionChecked,
        string? VerifierSchemaIdChecked,
        string? VerifierSchemaVersionChecked,
        string? BundleSchemaIdChecked,
        string? BundleSchemaVersionChecked,
        string? ComputedBundleContentHash,
        string? SuppliedBundleContentHash,
        string? ComputedReportExportContentHash,
        string? SuppliedReportExportContentHash,
        string? VerificationResultContentHash,
        string? ReceiptContentHash,
        IReadOnlyList<string> Checks,
        IReadOnlyList<string> Errors,
        ReceiptVerificationBoundaries? Boundaries);
}
