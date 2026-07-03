namespace BioStack.Application.Tests;

using System.Text.Json;
using BioStack.Application.Abstractions;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using Xunit;

public sealed class ProtocolOperationsExportBundleVerifierTests
{
    private static readonly Guid ProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProtocolId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTime ReportGeneratedAtUtc = new(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BundleGeneratedAtUtc = new(2026, 1, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Verify_ReturnsValidResult_ForValidBundle()
    {
        var result = BuildVerifier().Verify(ValidBundle());

        Assert.True(result.IsValid);
        Assert.Equal(ProtocolOperationsExportBundleService.SchemaVersion, result.SchemaVersion);
        Assert.Empty(result.Errors);
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
            result.Checks);
    }

    [Fact]
    public void Verify_FailsClosed_ForNullBundle()
    {
        var result = BuildVerifier().Verify(null);

        Assert.False(result.IsValid);
        Assert.Equal(["bundle-missing"], result.Errors);
    }

    [Theory]
    [InlineData("", "schema-version-missing")]
    [InlineData("2.0.0", "schema-version-mismatch")]
    public void Verify_Fails_WhenBundleSchemaVersionIsMissingOrUnexpected(string schemaVersion, string expectedError)
    {
        var bundle = ValidBundle() with
        {
            Metadata = ValidBundle().Metadata with { SchemaVersion = schemaVersion }
        };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors);
    }

    [Fact]
    public void Verify_Fails_WhenRequiredMetadataIsMissing()
    {
        var bundle = ValidBundle() with { Metadata = null! };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("metadata-missing", result.Errors);
    }

    [Fact]
    public void Verify_Fails_WhenJsonArtifactDescriptorIsMissing()
    {
        var bundle = ValidBundle() with { Artifacts = Array.Empty<ProtocolOperationsExportBundleArtifact>() };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("json-artifact-descriptor-missing", result.Errors);
    }

    [Fact]
    public void Verify_Fails_WhenJsonArtifactDescriptorIsIncorrect()
    {
        var bundle = ValidBundle() with
        {
            Artifacts =
            [
                new ProtocolOperationsExportBundleArtifact(
                    "wrong-artifact-id",
                    "application/json",
                    "report-export",
                    ProtocolOperationsReportExportService.SchemaVersion,
                    ValidBundle().ReportExport.Integrity.ContentHash)
            ]
        };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("json-artifact-descriptor-incorrect", result.Errors);
    }

    [Fact]
    public void Verify_Fails_WhenEmbeddedReportExportIsMissing()
    {
        var bundle = ValidBundle() with { ReportExport = null! };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("embedded-report-export-missing", result.Errors);
    }

    [Fact]
    public void Verify_Fails_WhenEmbeddedReportExportHashIsTampered()
    {
        var bundle = ValidBundle() with
        {
            ReportExport = ValidBundle().ReportExport with
            {
                Integrity = ValidBundle().ReportExport.Integrity with
                {
                    ContentHash = "bad-embedded-report-hash"
                }
            }
        };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("embedded-report-export-sha256-mismatch", result.Errors);
    }

    [Fact]
    public void Verify_Fails_WhenPreservedReportExportHashDoesNotMatchEmbeddedIntegrityHash()
    {
        var bundle = ValidBundle() with
        {
            Integrity = ValidBundle().Integrity with
            {
                ReportExportContentHash = "bad-preserved-report-hash"
            }
        };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("preserved-report-export-sha256-mismatch", result.Errors);
    }

    [Fact]
    public void Verify_Fails_WhenBundleSha256IsTampered()
    {
        var bundle = ValidBundle() with
        {
            Integrity = ValidBundle().Integrity with
            {
                BundleContentHash = "bad-bundle-hash"
            }
        };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("bundle-sha256-mismatch", result.Errors);
    }

    [Fact]
    public void Verify_RejectsPdfArtifactDescriptor()
    {
        var bundle = ValidBundle() with
        {
            Artifacts =
            [
                new ProtocolOperationsExportBundleArtifact(
                    "protocol-operations-report-export-pdf",
                    "application/pdf",
                    "report-export",
                    ProtocolOperationsReportExportService.SchemaVersion,
                    ValidBundle().ReportExport.Integrity.ContentHash)
            ]
        };

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("pdf-artifact-not-allowed", result.Errors);
    }

    [Fact]
    public void Verify_RejectsFilePathOrPersistedOutputClaims()
    {
        var bundle = WithWarnings(ValidBundle(), ["Bundle persisted to C:\\exports\\protocol-operations-report.json"]);

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("persisted-output-claim-not-allowed", result.Errors);
    }

    [Fact]
    public void Verify_RejectsProtocolIntelligenceRuntimeWording()
    {
        var bundle = WithWarnings(ValidBundle(), ["Protocol Intelligence runtime generated this export."]);

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("protocol-intelligence-runtime-language-not-allowed", result.Errors);
    }

    [Theory]
    [InlineData("This export includes a recommendation.")]
    [InlineData("This is a diagnosis summary.")]
    [InlineData("Take 25 mg nightly.")]
    [InlineData("This treatment plan should be followed.")]
    [InlineData("This prescription should be refilled.")]
    [InlineData("This is medical advice.")]
    public void Verify_RejectsMedicalAdviceLanguage(string warning)
    {
        var bundle = WithWarnings(ValidBundle(), [warning]);

        var result = BuildVerifier().Verify(bundle);

        Assert.False(result.IsValid);
        Assert.Contains("medical-advice-language-not-allowed", result.Errors);
    }

    [Fact]
    public void Verify_PreservesDeterministicCheckOrdering()
    {
        var verifier = BuildVerifier();
        var checks1 = verifier.Verify(ValidBundle()).Checks;
        var checks2 = verifier.Verify(ValidBundle()).Checks;

        Assert.Equal(checks1, checks2);
    }

    [Fact]
    public void Verify_PreservesDeterministicErrorOrdering()
    {
        var bundle = ValidBundle() with
        {
            Metadata = null!,
            ReportExport = null!,
            Artifacts = Array.Empty<ProtocolOperationsExportBundleArtifact>(),
            Integrity = ValidBundle().Integrity with
            {
                BundleContentHash = "tampered-bundle-hash"
            },
            Disclaimer = string.Empty
        };

        var result = BuildVerifier().Verify(bundle);

        Assert.Equal(
            [
                "metadata-missing",
                "schema-version-missing",
                "bundle-sha256-mismatch",
                "embedded-report-export-missing",
                "json-artifact-descriptor-missing",
                "bundle-disclaimer-missing",
                "embedded-report-export-disclaimer-missing"
            ],
            result.Errors);
    }

    [Fact]
    public void Verify_DoesNotMutateInput()
    {
        var bundle = ValidBundle();
        var before = JsonSerializer.Serialize(bundle);

        _ = BuildVerifier().Verify(bundle);

        Assert.Equal(before, JsonSerializer.Serialize(bundle));
    }

    [Fact]
    public void Verify_IsPureAndDoesNotRequireExternalDependencies()
    {
        IProtocolOperationsExportBundleVerifier verifier = new ProtocolOperationsExportBundleVerifier();

        var result = verifier.Verify(ValidBundle());

        Assert.True(result.IsValid);
    }

    private static IProtocolOperationsExportBundleVerifier BuildVerifier()
    {
        return new ProtocolOperationsExportBundleVerifier();
    }

    private static ProtocolOperationsExportBundle ValidBundle()
    {
        var reportExport = ValidReportExport();
        var metadata = new ProtocolOperationsExportBundleMetadata(
            ProtocolOperationsExportBundleService.SchemaVersion,
            BundleGeneratedAtUtc,
            ProfileId,
            ProtocolId);

        var artifacts = new List<ProtocolOperationsExportBundleArtifact>
        {
            new(
                "protocol-operations-report-export-json",
                "application/json",
                "report-export",
                reportExport.Metadata.SchemaVersion,
                reportExport.Integrity.ContentHash)
        };

        var integrity = new ProtocolOperationsExportBundleIntegrity(
            ProtocolOperationsExportBundleService.HashAlgorithmName,
            ProtocolOperationsExportBundleService.ComputeBundleContentHash(
                metadata,
                reportExport,
                artifacts,
                ProtocolOperationsExportBundleService.Disclaimer),
            reportExport.Integrity.ContentHash);

        return new ProtocolOperationsExportBundle(
            metadata,
            reportExport,
            artifacts,
            integrity,
            ProtocolOperationsExportBundleService.Disclaimer);
    }

    private static ProtocolOperationsReportExport ValidReportExport()
    {
        var report = ValidReport();
        return new ProtocolOperationsReportExport(
            new ProtocolOperationsReportExportMetadata(
                ProtocolOperationsReportExportService.SchemaVersion,
                ReportGeneratedAtUtc,
                ProfileId,
                ProtocolId),
            report,
            new ProtocolOperationsReportExportIntegrity(
                ProtocolOperationsReportExportService.HashAlgorithmName,
                ProtocolOperationsReportExportService.ComputeContentHash(report)),
            ProtocolOperationsReportExportService.Disclaimer);
    }

    private static ProtocolOperationsReport ValidReport()
    {
        return new ProtocolOperationsReport(
            ProfileId,
            ProtocolId,
            ReportGeneratedAtUtc,
            new ProtocolOperationsSummary(
                ActiveCompoundsCount: 1,
                LoggedDosesCount: 1,
                CheckInCount: 1,
                MonitoringEntryCount: 3,
                MilestoneCount: 1,
                EvidenceReferenceCount: 0,
                LatestActivityUtc: new DateTime(2026, 1, 7, 8, 0, 0, DateTimeKind.Utc)),
            new List<ProtocolOperationsEvent>
            {
                new("CompoundStarted", new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc), "Compound activity logged."),
                new("CheckInCreated", new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc), "Check-in logged.")
            },
            Array.Empty<ProtocolOperationsEvidenceReference>(),
            ["No evidence references recorded."]);
    }

    private static ProtocolOperationsExportBundle WithWarnings(
        ProtocolOperationsExportBundle bundle,
        IReadOnlyList<string> warnings)
    {
        return bundle with
        {
            ReportExport = bundle.ReportExport with
            {
                Report = bundle.ReportExport.Report with
                {
                    Warnings = warnings
                }
            }
        };
    }
}
