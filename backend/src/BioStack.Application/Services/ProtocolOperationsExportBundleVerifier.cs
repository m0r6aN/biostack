namespace BioStack.Application.Services;

using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.Application.Abstractions;
using BioStack.Contracts.Responses;

public sealed class ProtocolOperationsExportBundleVerifier : IProtocolOperationsExportBundleVerifier
{
    private const string JsonArtifactId = "protocol-operations-report-export-json";
    private const string JsonArtifactMediaType = "application/json";
    private const string JsonArtifactRole = "report-export";

    private static readonly string[] OrderedChecks =
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
    ];

    private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
    {
        WriteIndented = false
    };

    private static readonly Regex ForbiddenMedicalLanguage = new(
        @"\b(recommend(?:ation|ations|ed|ing|s)?|diagnos(?:is|es|e|ed|ing|tic)|dos(?:e|es|ed|ing|age)|treat(?:ment|ments|ed|ing|s)?|prescrib(?:e|ed|ing|es)|prescription(?:s)?|medical advice)\b|\btake\s+\d+(?:\.\d+)?\s*(?:mg|mcg|g|ml)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ForbiddenPersistenceLanguage = new(
        @"(?:\b(?:persist(?:ed|ence)?|saved?|stored?|written)\b.{0,32}\b(?:file|path|output|disk|pdf|json|bundle)\b)|(?:\bfile://)|(?:[A-Za-z]:\\)|(?:^|[\s""'])(?:\.{1,2}/|/)[^\s]*\.(?:json|pdf)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public ProtocolOperationsExportBundleVerificationResult Verify(ProtocolOperationsExportBundle? bundle)
    {
        var errors = new List<string>();

        if (bundle is null)
        {
            errors.Add("bundle-missing");
            return BuildResult(
                schemaVersion: string.Empty,
                expectedBundleSha256: string.Empty,
                actualBundleSha256: string.Empty,
                expectedReportExportSha256: string.Empty,
                actualReportExportSha256: string.Empty,
                errors);
        }

        var metadata = bundle.Metadata;
        var integrity = bundle.Integrity;
        var reportExport = bundle.ReportExport;
        var reportIntegrity = reportExport?.Integrity;
        var reportMetadata = reportExport?.Metadata;
        var report = reportExport?.Report;

        var actualReportExportSha256 = report is null
            ? string.Empty
            : ProtocolOperationsReportExportService.ComputeContentHash(report);
        var expectedReportExportSha256 = reportIntegrity?.ContentHash ?? string.Empty;
        var actualBundleSha256 = CanComputeBundleHash(bundle)
            ? ProtocolOperationsExportBundleService.ComputeBundleContentHash(
                metadata!,
                reportExport!,
                bundle.Artifacts,
                bundle.Disclaimer)
            : string.Empty;
        var expectedBundleSha256 = integrity?.BundleContentHash ?? string.Empty;

        if (metadata is null)
        {
            errors.Add("metadata-missing");
            errors.Add("schema-version-missing");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(metadata.SchemaVersion))
            {
                errors.Add("schema-version-missing");
            }
            else if (!string.Equals(
                         metadata.SchemaVersion,
                         ProtocolOperationsExportBundleService.SchemaVersion,
                         StringComparison.Ordinal))
            {
                errors.Add("schema-version-mismatch");
            }

            if (metadata.GeneratedAtUtc == default)
            {
                errors.Add("bundle-generated-at-missing");
            }

            if (metadata.ProfileId == Guid.Empty)
            {
                errors.Add("bundle-profile-id-missing");
            }
        }

        if (integrity is null)
        {
            errors.Add("bundle-integrity-missing");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(integrity.HashAlgorithm))
            {
                errors.Add("bundle-hash-algorithm-missing");
            }
            else if (!string.Equals(
                         integrity.HashAlgorithm,
                         ProtocolOperationsExportBundleService.HashAlgorithmName,
                         StringComparison.Ordinal))
            {
                errors.Add("bundle-hash-algorithm-mismatch");
            }

            if (string.IsNullOrWhiteSpace(integrity.BundleContentHash))
            {
                errors.Add("bundle-sha256-missing");
            }
            else if (!string.Equals(integrity.BundleContentHash, actualBundleSha256, StringComparison.Ordinal))
            {
                errors.Add("bundle-sha256-mismatch");
            }
        }

        if (reportExport is null)
        {
            errors.Add("embedded-report-export-missing");
        }
        else
        {
            if (reportMetadata is null)
            {
                errors.Add("embedded-report-export-metadata-missing");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(reportMetadata.SchemaVersion))
                {
                    errors.Add("embedded-report-export-schema-version-missing");
                }
                else if (!string.Equals(
                             reportMetadata.SchemaVersion,
                             ProtocolOperationsReportExportService.SchemaVersion,
                             StringComparison.Ordinal))
                {
                    errors.Add("embedded-report-export-schema-version-mismatch");
                }

                if (reportMetadata.GeneratedAtUtc == default)
                {
                    errors.Add("embedded-report-export-generated-at-missing");
                }

                if (reportMetadata.ProfileId == Guid.Empty)
                {
                    errors.Add("embedded-report-export-profile-id-missing");
                }
            }

            if (report is null)
            {
                errors.Add("embedded-report-content-missing");
            }

            if (reportIntegrity is null)
            {
                errors.Add("embedded-report-export-integrity-missing");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(reportIntegrity.HashAlgorithm))
                {
                    errors.Add("embedded-report-export-hash-algorithm-missing");
                }
                else if (!string.Equals(
                             reportIntegrity.HashAlgorithm,
                             ProtocolOperationsReportExportService.HashAlgorithmName,
                             StringComparison.Ordinal))
                {
                    errors.Add("embedded-report-export-hash-algorithm-mismatch");
                }

                if (string.IsNullOrWhiteSpace(reportIntegrity.ContentHash))
                {
                    errors.Add("embedded-report-export-sha256-missing");
                }
                else if (!string.Equals(reportIntegrity.ContentHash, actualReportExportSha256, StringComparison.Ordinal))
                {
                    errors.Add("embedded-report-export-sha256-mismatch");
                }
            }

            if (integrity is null || string.IsNullOrWhiteSpace(integrity.ReportExportContentHash))
            {
                errors.Add("preserved-report-export-sha256-missing");
            }
            else if (!string.Equals(integrity.ReportExportContentHash, expectedReportExportSha256, StringComparison.Ordinal))
            {
                errors.Add("preserved-report-export-sha256-mismatch");
            }
        }

        ValidateJsonArtifactDescriptor(bundle, actualReportExportSha256, errors);
        ValidateBoundaryClaims(bundle, errors);

        return BuildResult(
            schemaVersion: metadata?.SchemaVersion ?? string.Empty,
            expectedBundleSha256: expectedBundleSha256,
            actualBundleSha256: actualBundleSha256,
            expectedReportExportSha256: expectedReportExportSha256,
            actualReportExportSha256: actualReportExportSha256,
            errors);
    }

    private static void ValidateJsonArtifactDescriptor(
        ProtocolOperationsExportBundle bundle,
        string actualReportExportSha256,
        ICollection<string> errors)
    {
        if (bundle.Artifacts is null || bundle.Artifacts.Count == 0)
        {
            errors.Add("json-artifact-descriptor-missing");
            return;
        }

        ProtocolOperationsExportBundleArtifact? jsonArtifact = null;

        foreach (var artifact in bundle.Artifacts)
        {
            if (artifact is null)
            {
                errors.Add("artifact-descriptor-missing");
                continue;
            }

            if (ContainsPdfMarker(artifact))
            {
                errors.Add("pdf-artifact-not-allowed");
            }

            if (IsJsonArtifact(artifact))
            {
                jsonArtifact ??= artifact;
                continue;
            }

            if (string.Equals(artifact.MediaType, JsonArtifactMediaType, StringComparison.Ordinal) ||
                string.Equals(artifact.Role, JsonArtifactRole, StringComparison.Ordinal))
            {
                errors.Add("json-artifact-descriptor-incorrect");
            }
        }

        if (jsonArtifact is null)
        {
            errors.Add("json-artifact-descriptor-missing");
            return;
        }

        if (!string.Equals(jsonArtifact.ArtifactId, JsonArtifactId, StringComparison.Ordinal) ||
            !string.Equals(jsonArtifact.MediaType, JsonArtifactMediaType, StringComparison.Ordinal) ||
            !string.Equals(jsonArtifact.Role, JsonArtifactRole, StringComparison.Ordinal))
        {
            errors.Add("json-artifact-descriptor-incorrect");
        }

        if (!string.Equals(
                jsonArtifact.SchemaVersion,
                ProtocolOperationsReportExportService.SchemaVersion,
                StringComparison.Ordinal))
        {
            errors.Add("json-artifact-schema-version-mismatch");
        }

        if (!string.Equals(jsonArtifact.ContentHash, actualReportExportSha256, StringComparison.Ordinal))
        {
            errors.Add("json-artifact-content-hash-mismatch");
        }
    }

    private static void ValidateBoundaryClaims(
        ProtocolOperationsExportBundle bundle,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(bundle.Disclaimer))
        {
            errors.Add("bundle-disclaimer-missing");
        }
        else if (!string.Equals(
                     bundle.Disclaimer,
                     ProtocolOperationsExportBundleService.Disclaimer,
                     StringComparison.Ordinal))
        {
            errors.Add("bundle-disclaimer-mismatch");
        }

        if (bundle.ReportExport is null || string.IsNullOrWhiteSpace(bundle.ReportExport.Disclaimer))
        {
            errors.Add("embedded-report-export-disclaimer-missing");
        }
        else if (!string.Equals(
                     bundle.ReportExport.Disclaimer,
                     ProtocolOperationsReportExportService.Disclaimer,
                     StringComparison.Ordinal))
        {
            errors.Add("embedded-report-export-disclaimer-mismatch");
        }

        var payload = JsonSerializer.Serialize(bundle, PayloadSerializerOptions);

        if (payload.Contains("protocol intelligence", StringComparison.OrdinalIgnoreCase))
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
    }

    private static bool CanComputeBundleHash(ProtocolOperationsExportBundle bundle)
    {
        return bundle.Metadata is not null &&
               bundle.ReportExport is not null &&
               bundle.Artifacts is not null &&
               bundle.Integrity is not null;
    }

    private static bool ContainsPdfMarker(ProtocolOperationsExportBundleArtifact artifact)
    {
        return ContainsPdfMarker(artifact.ArtifactId) ||
               ContainsPdfMarker(artifact.MediaType) ||
               ContainsPdfMarker(artifact.Role);
    }

    private static bool ContainsPdfMarker(string? value)
    {
        return value?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsJsonArtifact(ProtocolOperationsExportBundleArtifact artifact)
    {
        return string.Equals(artifact.ArtifactId, JsonArtifactId, StringComparison.Ordinal) ||
               (string.Equals(artifact.MediaType, JsonArtifactMediaType, StringComparison.Ordinal) &&
                string.Equals(artifact.Role, JsonArtifactRole, StringComparison.Ordinal));
    }

    private static ProtocolOperationsExportBundleVerificationResult BuildResult(
        string schemaVersion,
        string expectedBundleSha256,
        string actualBundleSha256,
        string expectedReportExportSha256,
        string actualReportExportSha256,
        IReadOnlyList<string> errors)
    {
        return new ProtocolOperationsExportBundleVerificationResult(
            IsValid: errors.Count == 0,
            SchemaVersion: schemaVersion,
            ExpectedBundleSha256: expectedBundleSha256,
            ActualBundleSha256: actualBundleSha256,
            ExpectedReportExportSha256: expectedReportExportSha256,
            ActualReportExportSha256: actualReportExportSha256,
            Checks: OrderedChecks,
            Errors: errors.ToArray());
    }
}
