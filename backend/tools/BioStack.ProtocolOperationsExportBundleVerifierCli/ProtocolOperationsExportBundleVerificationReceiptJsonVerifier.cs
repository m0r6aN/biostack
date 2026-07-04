namespace BioStack.ProtocolOperationsExportBundleVerifierCli;

using System.Text.Json;
using BioStack.Application.Services;

internal static class ProtocolOperationsExportBundleVerificationReceiptJsonVerifier
{
    private static readonly string[] ExpectedChecks =
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
    ];

    public static ReceiptVerificationResult Verify(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ReceiptVerificationResult.ForFailure(
                ProtocolOperationsExportBundleVerificationReceiptJson.InvalidInputStatus,
                ["input-json-invalid"]);
        }

        ProtocolOperationsExportBundleVerificationReceipt receipt;
        try
        {
            receipt = ProtocolOperationsExportBundleVerificationReceiptJson.Parse(json);
        }
        catch (JsonException)
        {
            return ReceiptVerificationResult.ForFailure(
                ProtocolOperationsExportBundleVerificationReceiptJson.InvalidJsonStatus,
                ["input-json-invalid"]);
        }

        var errors = new List<string>();
        ValidateStructure(receipt, errors);
        ProtocolOperationsExportBundleVerificationReceiptJson.AppendForbiddenContentErrors(json, errors);

        var computedVerificationResultHash = ProtocolOperationsExportBundleVerificationReceiptJson.ComputeVerificationResultContentHash(receipt);
        if (!string.Equals(receipt.VerificationResultContentHash, computedVerificationResultHash, StringComparison.Ordinal))
        {
            errors.Add("verification-result-content-hash-mismatch");
        }

        var computedReceiptHash = ProtocolOperationsExportBundleVerificationReceiptJson.ComputeReceiptContentHash(receipt);
        if (!string.Equals(receipt.ReceiptContentHash, computedReceiptHash, StringComparison.Ordinal))
        {
            errors.Add("receipt-content-hash-mismatch");
        }

        return new ReceiptVerificationResult(
            IsValid: errors.Count == 0,
            Status: string.IsNullOrWhiteSpace(receipt.Status)
                ? ProtocolOperationsExportBundleVerificationReceiptJson.InvalidInputStatus
                : receipt.Status,
            ReceiptContentHash: receipt.ReceiptContentHash ?? string.Empty,
            ReceiptSchemaId: receipt.ReceiptSchemaId,
            ReceiptSchemaVersion: receipt.ReceiptSchemaVersion,
            VerifierSchemaId: receipt.VerifierSchemaId,
            VerifierSchemaVersion: receipt.VerifierSchemaVersion,
            BundleSchemaId: receipt.BundleSchemaId,
            BundleSchemaVersion: receipt.BundleSchemaVersion,
            ComputedBundleContentHash: receipt.ComputedBundleContentHash,
            SuppliedBundleContentHash: receipt.SuppliedBundleContentHash,
            ComputedReportExportContentHash: receipt.ComputedReportExportContentHash,
            SuppliedReportExportContentHash: receipt.SuppliedReportExportContentHash,
            VerificationResultContentHash: receipt.VerificationResultContentHash,
            Checks: receipt.Checks ?? [],
            Errors: errors,
            Boundaries: receipt.Boundaries is null
                ? null
                : new ReceiptVerificationBoundaries(
                    receipt.Boundaries.ObservationalOnly,
                    receipt.Boundaries.NonMedical,
                    receipt.Boundaries.NoPersistence,
                    receipt.Boundaries.NoPdf,
                    receipt.Boundaries.NoRuntimeExpansion));
    }

    private static void ValidateStructure(ProtocolOperationsExportBundleVerificationReceipt receipt, ICollection<string> errors)
    {
        var receiptChecks = receipt.Checks ?? [];
        var receiptErrors = receipt.Errors ?? [];

        if (!string.Equals(receipt.ReceiptSchemaId, ProtocolOperationsExportBundleVerificationReceiptJson.ReceiptSchemaId, StringComparison.Ordinal))
        {
            errors.Add("receipt-schema-id-mismatch");
        }

        if (!string.Equals(receipt.ReceiptSchemaVersion, ProtocolOperationsExportBundleVerificationReceiptJson.ReceiptSchemaVersion, StringComparison.Ordinal))
        {
            errors.Add("receipt-schema-version-mismatch");
        }

        if (!string.Equals(receipt.VerifierSchemaId, ProtocolOperationsExportBundleVerificationReceiptJson.VerifierSchemaId, StringComparison.Ordinal))
        {
            errors.Add("verifier-schema-id-mismatch");
        }

        if (!string.Equals(receipt.VerifierSchemaVersion, ProtocolOperationsExportBundleVerificationReceiptJson.VerifierSchemaVersion, StringComparison.Ordinal))
        {
            errors.Add("verifier-schema-version-mismatch");
        }

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.IsSuccessStatus(receipt.Status) &&
            !ProtocolOperationsExportBundleVerificationReceiptJson.IsFailureStatus(receipt.Status))
        {
            errors.Add("receipt-status-invalid");
        }

        if (receipt.Checks is null || receipt.Errors is null)
        {
            errors.Add("receipt-fields-missing");
        }

        if (receipt.Boundaries is null ||
            !receipt.Boundaries.ObservationalOnly ||
            !receipt.Boundaries.NonMedical ||
            !receipt.Boundaries.NoPersistence ||
            !receipt.Boundaries.NoPdf ||
            !receipt.Boundaries.NoRuntimeExpansion)
        {
            errors.Add("receipt-boundaries-missing");
        }

        if (ProtocolOperationsExportBundleVerificationReceiptJson.IsSuccessStatus(receipt.Status))
        {
            if (receiptErrors.Count != 0)
            {
                errors.Add("success-receipt-errors-present");
            }

            if (string.IsNullOrWhiteSpace(receipt.ComputedBundleContentHash) ||
                string.IsNullOrWhiteSpace(receipt.SuppliedBundleContentHash))
            {
                errors.Add("success-receipt-bundle-hash-missing");
            }

            if (string.IsNullOrWhiteSpace(receipt.ComputedReportExportContentHash) ||
                string.IsNullOrWhiteSpace(receipt.SuppliedReportExportContentHash))
            {
                errors.Add("success-receipt-report-export-hash-missing");
            }

            if ((!string.IsNullOrWhiteSpace(receipt.ComputedBundleContentHash) &&
                 !string.IsNullOrWhiteSpace(receipt.SuppliedBundleContentHash) &&
                 !string.Equals(receipt.ComputedBundleContentHash, receipt.SuppliedBundleContentHash, StringComparison.Ordinal)) ||
                (!string.IsNullOrWhiteSpace(receipt.ComputedReportExportContentHash) &&
                 !string.IsNullOrWhiteSpace(receipt.SuppliedReportExportContentHash) &&
                 !string.Equals(receipt.ComputedReportExportContentHash, receipt.SuppliedReportExportContentHash, StringComparison.Ordinal)) ||
                receiptErrors.Count != 0)
            {
                errors.Add("success-receipt-captured-result-not-successful");
            }
        }

        if (string.Equals(receipt.Status, ProtocolOperationsExportBundleVerificationReceiptJson.VerificationFailedStatus, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(receipt.ComputedBundleContentHash) ||
                string.IsNullOrWhiteSpace(receipt.SuppliedBundleContentHash))
            {
                errors.Add("failure-receipt-bundle-hash-missing");
            }

            if (string.IsNullOrWhiteSpace(receipt.ComputedReportExportContentHash) ||
                string.IsNullOrWhiteSpace(receipt.SuppliedReportExportContentHash))
            {
                errors.Add("failure-receipt-report-export-hash-missing");
            }
        }

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.AllowsMissingBindings(receipt.Status) &&
            !ExpectedChecks.SequenceEqual(receiptChecks))
        {
            errors.Add("receipt-check-order-mismatch");
        }

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.AllowsMissingBindings(receipt.Status) &&
            string.IsNullOrWhiteSpace(receipt.BundleSchemaId))
        {
            errors.Add("receipt-bundle-schema-id-missing");
        }

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.AllowsMissingBindings(receipt.Status) &&
            !string.Equals(receipt.BundleSchemaId, ProtocolOperationsExportBundleVerificationReceiptJson.BundleSchemaId, StringComparison.Ordinal))
        {
            errors.Add("receipt-bundle-schema-id-mismatch");
        }

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.AllowsMissingBindings(receipt.Status) &&
            string.IsNullOrWhiteSpace(receipt.BundleSchemaVersion))
        {
            errors.Add("receipt-bundle-schema-version-missing");
        }

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.AllowsMissingBindings(receipt.Status) &&
            !string.Equals(receipt.BundleSchemaVersion, ProtocolOperationsExportBundleService.SchemaVersion, StringComparison.Ordinal))
        {
            errors.Add("receipt-bundle-schema-version-mismatch");
        }
    }
}

internal sealed record ReceiptVerificationResult(
    bool IsValid,
    string Status,
    string ReceiptContentHash,
    string? ReceiptSchemaId,
    string? ReceiptSchemaVersion,
    string? VerifierSchemaId,
    string? VerifierSchemaVersion,
    string? BundleSchemaId,
    string? BundleSchemaVersion,
    string? ComputedBundleContentHash,
    string? SuppliedBundleContentHash,
    string? ComputedReportExportContentHash,
    string? SuppliedReportExportContentHash,
    string? VerificationResultContentHash,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Errors,
    ReceiptVerificationBoundaries? Boundaries)
{
    internal static ReceiptVerificationResult ForFailure(string status, IReadOnlyList<string> errors) =>
        new(
            IsValid: false,
            Status: status,
            ReceiptContentHash: string.Empty,
            ReceiptSchemaId: null,
            ReceiptSchemaVersion: null,
            VerifierSchemaId: null,
            VerifierSchemaVersion: null,
            BundleSchemaId: null,
            BundleSchemaVersion: null,
            ComputedBundleContentHash: null,
            SuppliedBundleContentHash: null,
            ComputedReportExportContentHash: null,
            SuppliedReportExportContentHash: null,
            VerificationResultContentHash: null,
            Checks: [],
            Errors: errors,
            Boundaries: null);
}

internal sealed record ReceiptVerificationBoundaries(
    bool ObservationalOnly,
    bool NonMedical,
    bool NoPersistence,
    bool NoPdf,
    bool NoRuntimeExpansion);
