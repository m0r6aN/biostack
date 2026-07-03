namespace BioStack.ProtocolOperationsExportBundleVerifierCli;

using System.Text.Json;

internal static class ProtocolOperationsExportBundleVerificationReceiptJsonVerifier
{
    public static ReceiptVerificationResult Verify(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ReceiptVerificationResult(false, ProtocolOperationsExportBundleVerificationReceiptJson.InvalidInputStatus, string.Empty, ["input-json-invalid"]);
        }

        ProtocolOperationsExportBundleVerificationReceipt receipt;
        try
        {
            receipt = ProtocolOperationsExportBundleVerificationReceiptJson.Parse(json);
        }
        catch (JsonException)
        {
            return new ReceiptVerificationResult(false, ProtocolOperationsExportBundleVerificationReceiptJson.InvalidJsonStatus, string.Empty, ["input-json-invalid"]);
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
            errors.Count == 0,
            string.IsNullOrWhiteSpace(receipt.Status) ? ProtocolOperationsExportBundleVerificationReceiptJson.InvalidInputStatus : receipt.Status,
            receipt.ReceiptContentHash ?? string.Empty,
            errors);
    }

    private static void ValidateStructure(ProtocolOperationsExportBundleVerificationReceipt receipt, ICollection<string> errors)
    {
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

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.IsSuccessStatus(receipt.Status)
            && !ProtocolOperationsExportBundleVerificationReceiptJson.IsFailureStatus(receipt.Status))
        {
            errors.Add("receipt-status-invalid");
        }

        if (receipt.Checks is null || receipt.Errors is null)
        {
            errors.Add("receipt-fields-missing");
        }

        if (receipt.Boundaries is null
            || !receipt.Boundaries.ObservationalOnly
            || !receipt.Boundaries.NonMedical
            || !receipt.Boundaries.NoPersistence
            || !receipt.Boundaries.NoPdf
            || !receipt.Boundaries.NoRuntimeExpansion)
        {
            errors.Add("receipt-boundaries-missing");
        }

        if (ProtocolOperationsExportBundleVerificationReceiptJson.IsSuccessStatus(receipt.Status))
        {
            if (string.IsNullOrWhiteSpace(receipt.ComputedBundleContentHash)
                || string.IsNullOrWhiteSpace(receipt.SuppliedBundleContentHash))
            {
                errors.Add("success-receipt-bundle-hash-missing");
            }

            if (string.IsNullOrWhiteSpace(receipt.ComputedReportExportContentHash)
                || string.IsNullOrWhiteSpace(receipt.SuppliedReportExportContentHash))
            {
                errors.Add("success-receipt-report-export-hash-missing");
            }
        }

        if (receipt.Status == ProtocolOperationsExportBundleVerificationReceiptJson.VerificationFailedStatus)
        {
            if (string.IsNullOrWhiteSpace(receipt.ComputedBundleContentHash)
                || string.IsNullOrWhiteSpace(receipt.SuppliedBundleContentHash))
            {
                errors.Add("failure-receipt-bundle-hash-missing");
            }

            if (string.IsNullOrWhiteSpace(receipt.ComputedReportExportContentHash)
                || string.IsNullOrWhiteSpace(receipt.SuppliedReportExportContentHash))
            {
                errors.Add("failure-receipt-report-export-hash-missing");
            }
        }

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.AllowsMissingBindings(receipt.Status)
            && string.IsNullOrWhiteSpace(receipt.BundleSchemaId))
        {
            errors.Add("receipt-bundle-schema-id-missing");
        }

        if (!ProtocolOperationsExportBundleVerificationReceiptJson.AllowsMissingBindings(receipt.Status)
            && string.IsNullOrWhiteSpace(receipt.BundleSchemaVersion))
        {
            errors.Add("receipt-bundle-schema-version-missing");
        }
    }
}

internal sealed record ReceiptVerificationResult(
    bool IsValid,
    string Status,
    string ReceiptContentHash,
    IReadOnlyList<string> Errors);
