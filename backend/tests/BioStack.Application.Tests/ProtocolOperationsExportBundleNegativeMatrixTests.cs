namespace BioStack.Application.Tests;

using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using Xunit;

public sealed class ProtocolOperationsExportBundleNegativeMatrixTests
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

    [Fact]
    public void Verify_RejectsBundleSchemaVersionDrift_OnCurrentBundleSurface()
    {
        var bundle = ReadBundle();

        var result = Verify(bundle with
        {
            Metadata = bundle.Metadata with { SchemaVersion = "9.9.9" },
        });

        AssertRejected(result, "schema-version-mismatch");
        Assert.DoesNotContain("schemaId", JsonSerializer.Serialize(bundle), StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_RejectsEmbeddedReportHashDrift()
    {
        var bundle = ReadBundle();

        var result = Verify(bundle with
        {
            ReportExport = bundle.ReportExport with
            {
                Integrity = bundle.ReportExport.Integrity with
                {
                    ContentHash = new string('a', 64),
                },
            },
        });

        AssertRejected(result, "embedded-report-export-sha256-mismatch");
    }

    [Fact]
    public void Verify_RejectsBundleHashDrift()
    {
        var bundle = ReadBundle();

        var result = Verify(bundle with
        {
            Integrity = bundle.Integrity with
            {
                BundleContentHash = new string('b', 64),
            },
        });

        AssertRejected(result, "bundle-sha256-mismatch");
    }

    [Fact]
    public void Verify_RejectsArtifactDescriptorDrift()
    {
        var bundle = ReadBundle();
        var artifact = Assert.Single(bundle.Artifacts);

        var result = Verify(bundle with
        {
            Artifacts =
            [
                artifact with
                {
                    ArtifactId = "protocol-operations-report-export-runtime",
                    Role = "runtime-output",
                },
            ],
        });

        AssertRejected(result, "json-artifact-descriptor-missing");
        AssertRejected(result, "json-artifact-descriptor-incorrect");
    }

    [Fact]
    public void Verify_RejectsArtifactContentHashDrift()
    {
        var bundle = ReadBundle();
        var artifact = Assert.Single(bundle.Artifacts);

        var result = Verify(bundle with
        {
            Artifacts = [artifact with { ContentHash = new string('c', 64) }],
        });

        AssertRejected(result, "json-artifact-content-hash-mismatch");
    }

    [Fact]
    public void Verify_RejectsBundleDisclaimerBoundaryDrift()
    {
        var bundle = ReadBundle();

        var result = Verify(bundle with
        {
            Disclaimer = "This bundle is available for clinical care planning.",
        });

        AssertRejected(result, "bundle-disclaimer-mismatch");
    }

    [Fact]
    public void Verify_RejectsEmbeddedDisclaimerBoundaryDrift()
    {
        var bundle = ReadBundle();

        var result = Verify(bundle with
        {
            ReportExport = bundle.ReportExport with
            {
                Disclaimer = "This report provides dosing recommendation.",
            },
        });

        AssertRejected(result, "embedded-report-export-disclaimer-mismatch");
        AssertRejected(result, "medical-advice-language-not-allowed");
    }

    [Theory]
    [InlineData("Bundle persisted to C:\\exports\\protocol-operations-report.json.", "persisted-output-claim-not-allowed")]
    [InlineData("Protocol Intelligence runtime generated this export.", "protocol-intelligence-runtime-language-not-allowed")]
    [InlineData("Take 25 mg nightly as treatment.", "medical-advice-language-not-allowed")]
    public void Verify_RejectsForbiddenBundleSurfaceLanguage(string warning, string expectedError)
    {
        var bundle = ReadBundle();

        var result = Verify(bundle with
        {
            ReportExport = bundle.ReportExport with
            {
                Report = bundle.ReportExport.Report with
                {
                    Warnings = [warning],
                },
            },
        });

        AssertRejected(result, expectedError);
    }

    [Fact]
    public void Verify_RejectsPdfArtifactSurface()
    {
        var bundle = ReadBundle();
        var artifact = Assert.Single(bundle.Artifacts);

        var result = Verify(bundle with
        {
            Artifacts =
            [
                artifact,
                artifact with
                {
                    ArtifactId = "protocol-operations-report-export-pdf",
                    MediaType = "application/pdf",
                },
            ],
        });

        AssertRejected(result, "pdf-artifact-not-allowed");
    }

    [Fact]
    public void Verify_PreservesContractualCheckAndErrorOrdering_ForNegativeMatrix()
    {
        var bundle = ReadBundle();

        var result = Verify(bundle with
        {
            Metadata = bundle.Metadata with { SchemaVersion = "9.9.9" },
            Artifacts = [],
            Integrity = bundle.Integrity with { BundleContentHash = new string('d', 64) },
            Disclaimer = string.Empty,
        });

        Assert.Equal(ExpectedChecks, result.Checks);
        Assert.Equal(
            [
                "schema-version-mismatch",
                "bundle-sha256-mismatch",
                "json-artifact-descriptor-missing",
                "bundle-disclaimer-missing",
            ],
            result.Errors);
    }

    private static ProtocolOperationsExportBundleVerificationResult Verify(ProtocolOperationsExportBundle bundle)
        => new ProtocolOperationsExportBundleVerifier().Verify(bundle);

    private static void AssertRejected(ProtocolOperationsExportBundleVerificationResult result, string expectedError)
    {
        Assert.False(result.IsValid);
        Assert.Equal(ExpectedChecks, result.Checks);
        Assert.Contains(expectedError, result.Errors);
    }

    private static ProtocolOperationsExportBundle ReadBundle()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "ProtocolOperationsExportBundle",
            "ProtocolOperationsExportBundle.golden.json");

        return JsonSerializer.Deserialize<ProtocolOperationsExportBundle>(File.ReadAllText(fixturePath))
            ?? throw new InvalidOperationException("Failed to deserialize golden bundle fixture.");
    }
}
