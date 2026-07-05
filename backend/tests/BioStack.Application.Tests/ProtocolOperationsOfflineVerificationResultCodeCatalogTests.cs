namespace BioStack.Application.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BioStack.Application.Services;
using Xunit;

/// <summary>
/// Guards the stable result/error code catalog for the Protocol Operations offline verification
/// chain (bundle verifier, receipt verifier, and CLI). This catalog exists so auditors and
/// automation can key off stable UPPER_SNAKE codes instead of scraping human-readable text or the
/// underlying dash-cased tokens, which may be reworded over time. This is test-owned only: it does
/// not add codes to any runtime/API/result-JSON contract, and it does not change CLI output.
/// </summary>
public sealed class ProtocolOperationsOfflineVerificationResultCodeCatalogTests
{
    private static readonly string[] ExpectedKeyOrder =
    [
        "code",
        "severity",
        "artifactSurface",
        "meaning",
        "pathExpectation",
        "boundaryPostureCategory",
        "mappedTokens",
    ];

    private static readonly string[] AllowedSeverities = ["error", "info"];
    private static readonly string[] AllowedArtifactSurfaces = ["bundle", "receipt", "cli", "docs-manifest"];
    private static readonly string[] AllowedPathExpectations = ["valid-path", "failure-path"];
    private static readonly string[] AllowedBoundaryPostureCategories =
    [
        "medical-language",
        "pdf-claim",
        "persistence-claim",
        "runtime-claim",
    ];

    private static readonly Regex ForbiddenMedicalAuthorityLanguage = new(
        @"\b(recommend(?:ation|ations|ed|ing|s)?|diagnos(?:is|es|e|ed|ing|tic)|dos(?:e|es|ed|ing|age)|treat(?:ment|ments|ed|ing|s)?|prescrib(?:e|ed|ing|es)|prescription(?:s)?|medical advice)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ForbiddenPdfAuthorityLanguage = new(
        @"\b(authentic(?:ate|ated|ity)?|notariz(?:e|ed|ation))\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ForbiddenPersistenceAuthorityLanguage = new(
        @"\b(persist(?:ed|ence)?|saved?|stored?|written)\b.{0,32}\b(?:file|path|output|disk|database|db|table)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ForbiddenRuntimeAuthorityLanguage = new(
        @"\bprotocol intelligence runtime\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public void Catalog_SnapshotExists()
    {
        Assert.True(File.Exists(CatalogSnapshotPath()), $"Expected catalog snapshot at '{CatalogSnapshotPath()}'.");
    }

    [Fact]
    public void Catalog_ReparsesToStableCanonicalJson()
    {
        var raw = File.ReadAllText(CatalogSnapshotPath());
        var first = CanonicalizeJson(raw);
        var second = CanonicalizeJson(first);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Catalog_HasFrozenTopLevelShape()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(CatalogSnapshotPath()));
        var root = doc.RootElement;

        Assert.Equal(
            "biostack.protocol-operations-offline-verification-result-code-catalog",
            root.GetProperty("catalogSchemaId").GetString());
        Assert.Equal("1.0.0", root.GetProperty("catalogSchemaVersion").GetString());
        Assert.True(root.TryGetProperty("entries", out var entries));
        Assert.True(entries.GetArrayLength() > 0);
    }

    [Fact]
    public void Catalog_EntriesHaveExactlyTheFrozenKeySetInOrder()
    {
        foreach (var entry in Entries())
        {
            var propertyNames = entry.EnumerateObject().Select(p => p.Name).ToArray();
            Assert.Equal(ExpectedKeyOrder, propertyNames);
        }
    }

    [Fact]
    public void Catalog_EntriesAreInAscendingOrdinalOrderByCode_AndDuplicateFree()
    {
        var codes = Codes();

        var sorted = codes.OrderBy(code => code, StringComparer.Ordinal).ToArray();
        Assert.Equal(sorted, codes);
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Catalog_EveryEntryUsesAllowedEnumValues()
    {
        foreach (var entry in Entries())
        {
            var severity = entry.GetProperty("severity").GetString();
            Assert.Contains(severity, AllowedSeverities);

            var artifactSurface = entry.GetProperty("artifactSurface").GetString();
            Assert.Contains(artifactSurface, AllowedArtifactSurfaces);

            var pathExpectation = entry.GetProperty("pathExpectation").GetString();
            Assert.Contains(pathExpectation, AllowedPathExpectations);

            var boundaryPostureCategory = entry.GetProperty("boundaryPostureCategory");
            if (boundaryPostureCategory.ValueKind != JsonValueKind.Null)
            {
                Assert.Contains(boundaryPostureCategory.GetString(), AllowedBoundaryPostureCategories);
            }
        }
    }

    [Fact]
    public void Catalog_BoundaryPostureCategoryIsNonNullExactlyForBoundaryCodes()
    {
        foreach (var entry in Entries())
        {
            var code = entry.GetProperty("code").GetString()!;
            var boundaryPostureCategory = entry.GetProperty("boundaryPostureCategory");
            var isBoundaryCode = code.StartsWith("POBOUNDARY_", StringComparison.Ordinal);

            if (isBoundaryCode)
            {
                Assert.NotEqual(JsonValueKind.Null, boundaryPostureCategory.ValueKind);
            }
            else
            {
                Assert.Equal(JsonValueKind.Null, boundaryPostureCategory.ValueKind);
            }
        }
    }

    [Fact]
    public void Catalog_MeaningFieldsContainNoForbiddenAuthorityLanguage()
    {
        foreach (var entry in Entries())
        {
            var code = entry.GetProperty("code").GetString()!;
            var meaning = entry.GetProperty("meaning").GetString() ?? string.Empty;

            Assert.False(
                ForbiddenMedicalAuthorityLanguage.IsMatch(meaning),
                $"Entry '{code}' meaning contains forbidden medical-authority language: '{meaning}'.");
            Assert.False(
                ForbiddenPdfAuthorityLanguage.IsMatch(meaning),
                $"Entry '{code}' meaning contains forbidden PDF-authenticity-authority language: '{meaning}'.");
            Assert.False(
                ForbiddenPersistenceAuthorityLanguage.IsMatch(meaning),
                $"Entry '{code}' meaning contains forbidden persistence-authority language: '{meaning}'.");
            Assert.False(
                ForbiddenRuntimeAuthorityLanguage.IsMatch(meaning),
                $"Entry '{code}' meaning contains forbidden runtime-execution-authority language: '{meaning}'.");
        }
    }

    [Fact]
    public void Catalog_NoStringFieldIntroducesForbiddenAuthorityLanguage()
    {
        var raw = File.ReadAllText(CatalogSnapshotPath());
        using var doc = JsonDocument.Parse(raw);

        // "mappedTokens" holds pre-existing dash-cased source tokens emitted by the verifier/CLI
        // (e.g. "persisted-output-claim-not-allowed") which necessarily reference the forbidden
        // concepts by name; they are not new prose authority claims and are intentionally excluded
        // from this scan. All other string fields (code, severity, artifactSurface, meaning,
        // pathExpectation, boundaryPostureCategory, and the top-level schema fields) are checked.
        void Walk(JsonElement element, string propertyName)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        Walk(property.Value, property.Name);
                    }

                    break;
                case JsonValueKind.Array:
                    if (string.Equals(propertyName, "mappedTokens", StringComparison.Ordinal))
                    {
                        break;
                    }

                    foreach (var item in element.EnumerateArray())
                    {
                        Walk(item, propertyName);
                    }

                    break;
                case JsonValueKind.String:
                    var value = element.GetString() ?? string.Empty;
                    Assert.False(ForbiddenMedicalAuthorityLanguage.IsMatch(value), $"Forbidden medical-authority language: '{value}'.");
                    Assert.False(ForbiddenPdfAuthorityLanguage.IsMatch(value), $"Forbidden PDF-authenticity-authority language: '{value}'.");
                    Assert.False(ForbiddenPersistenceAuthorityLanguage.IsMatch(value), $"Forbidden persistence-authority language: '{value}'.");
                    Assert.False(ForbiddenRuntimeAuthorityLanguage.IsMatch(value), $"Forbidden runtime-execution-authority language: '{value}'.");
                    break;
            }
        }

        Walk(doc.RootElement, string.Empty);
    }

    [Fact]
    public void Catalog_MappedTokensAreDashCasedAndNonEmptyWhereDeclared()
    {
        var dashCased = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        foreach (var entry in Entries())
        {
            var code = entry.GetProperty("code").GetString()!;
            var mappedTokens = entry.GetProperty("mappedTokens").EnumerateArray().Select(t => t.GetString()!).ToArray();

            foreach (var token in mappedTokens)
            {
                Assert.True(dashCased.IsMatch(token), $"Entry '{code}' has non-dash-cased mapped token '{token}'.");
            }
        }
    }

    [Fact]
    public void Catalog_CoversEveryDashCasedErrorTokenEmittedByTheVerifierNegativeMatrix()
    {
        var expectedTokens = ExpectedVerifierEmittedTokens();
        var mappedTokens = AllMappedTokens();

        var uncovered = expectedTokens.Where(token => !mappedTokens.Contains(token, StringComparer.Ordinal)).ToArray();

        Assert.True(
            uncovered.Length == 0,
            $"The following verifier-emitted tokens are not mapped by any catalog entry: {string.Join(", ", uncovered)}");
    }

    [Fact]
    public void Catalog_CoversTheCliAndReceiptVerifierTokenVocabulary()
    {
        var expectedTokens = ExpectedCliAndReceiptTokens();
        var mappedTokens = AllMappedTokens();

        var uncovered = expectedTokens.Where(token => !mappedTokens.Contains(token, StringComparer.Ordinal)).ToArray();

        Assert.True(
            uncovered.Length == 0,
            $"The following CLI/receipt-verifier tokens are not mapped by any catalog entry: {string.Join(", ", uncovered)}");
    }

    /// <summary>
    /// Derives the dash-cased error tokens the bundle verifier can emit by driving it against the
    /// existing golden bundle fixture with each known negative-matrix mutation, mirroring
    /// <see cref="ProtocolOperationsExportBundleNegativeMatrixTests"/> and
    /// <see cref="ProtocolOperationsExportBundleVerifierTests"/>.
    /// </summary>
    private static string[] ExpectedVerifierEmittedTokens()
    {
        var bundle = ReadGoldenBundle();
        var verifier = new ProtocolOperationsExportBundleVerifier();
        var tokens = new SortedSet<string>(StringComparer.Ordinal);

        void Collect(BioStack.Contracts.Responses.ProtocolOperationsExportBundle? mutated)
        {
            foreach (var error in verifier.Verify(mutated).Errors)
            {
                tokens.Add(error);
            }
        }

        // Null bundle.
        Collect(null);

        // Missing metadata / integrity / report export / artifacts.
        Collect(bundle with { Metadata = null! });
        Collect(bundle with { Integrity = null! });
        Collect(bundle with { ReportExport = null! });
        Collect(bundle with { Artifacts = Array.Empty<BioStack.Contracts.Responses.ProtocolOperationsExportBundleArtifact>() });
        Collect(bundle with { Disclaimer = string.Empty });

        // Schema-version drift.
        Collect(bundle with { Metadata = bundle.Metadata with { SchemaVersion = string.Empty } });
        Collect(bundle with { Metadata = bundle.Metadata with { SchemaVersion = "9.9.9" } });
        Collect(bundle with { Metadata = bundle.Metadata with { GeneratedAtUtc = default } });
        Collect(bundle with { Metadata = bundle.Metadata with { ProfileId = Guid.Empty } });

        // Bundle integrity drift.
        Collect(bundle with { Integrity = bundle.Integrity with { HashAlgorithm = string.Empty } });
        Collect(bundle with { Integrity = bundle.Integrity with { HashAlgorithm = "md5" } });
        Collect(bundle with { Integrity = bundle.Integrity with { BundleContentHash = string.Empty } });
        Collect(bundle with { Integrity = bundle.Integrity with { BundleContentHash = new string('b', 64) } });
        Collect(bundle with { Integrity = bundle.Integrity with { ReportExportContentHash = string.Empty } });
        Collect(bundle with { Integrity = bundle.Integrity with { ReportExportContentHash = "bad-preserved-report-hash" } });

        // Embedded report export drift.
        Collect(bundle with { ReportExport = bundle.ReportExport with { Metadata = null! } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Metadata = bundle.ReportExport.Metadata with { SchemaVersion = string.Empty } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Metadata = bundle.ReportExport.Metadata with { SchemaVersion = "9.9.9" } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Metadata = bundle.ReportExport.Metadata with { GeneratedAtUtc = default } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Metadata = bundle.ReportExport.Metadata with { ProfileId = Guid.Empty } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Report = null! } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Integrity = bundle.ReportExport.Integrity with { HashAlgorithm = string.Empty } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Integrity = bundle.ReportExport.Integrity with { HashAlgorithm = "md5" } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Integrity = bundle.ReportExport.Integrity with { ContentHash = string.Empty } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Integrity = bundle.ReportExport.Integrity with { ContentHash = new string('a', 64) } } });

        // Artifact descriptor drift.
        var artifact = bundle.Artifacts[0];
        Collect(bundle with { Artifacts = [artifact with { ArtifactId = "wrong-id" }] });
        Collect(bundle with { Artifacts = [artifact, artifact with { ArtifactId = "protocol-operations-report-export-pdf", MediaType = "application/pdf" }] });
        Collect(bundle with { Artifacts = [artifact with { SchemaVersion = "9.9.9" } ] });
        Collect(bundle with { Artifacts = [artifact with { ContentHash = new string('c', 64) }] });

        // Disclaimer / boundary language drift.
        Collect(bundle with { Disclaimer = "This bundle is available for clinical care planning." });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Disclaimer = "This report provides dosing recommendation." } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Report = bundle.ReportExport.Report with { Warnings = ["Bundle persisted to C:\\exports\\protocol-operations-report.json."] } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Report = bundle.ReportExport.Report with { Warnings = ["Protocol Intelligence runtime generated this export."] } } });
        Collect(bundle with { ReportExport = bundle.ReportExport with { Report = bundle.ReportExport.Report with { Warnings = ["Take 25 mg nightly as treatment."] } } });

        return tokens.ToArray();
    }

    /// <summary>
    /// The CLI and receipt-verifier token vocabulary is enumerated directly from source constants
    /// and literal error tokens (see
    /// backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/ProtocolOperationsExportBundleVerifierCli.cs
    /// and ProtocolOperationsExportBundleVerificationReceiptJsonVerifier.cs), because those tools
    /// live in a separate test-project surface (BioStack.ProtocolOperationsExportBundleVerifierCli.Tests)
    /// and are not referenced from BioStack.Application.Tests.
    /// </summary>
    private static string[] ExpectedCliAndReceiptTokens() =>
    [
        // CLI argument parsing / input handling.
        "input-path-required",
        "unknown-argument",
        "mode-conflict",
        "input-path-duplicated",
        "input-path-is-directory",
        "input-file-missing",
        "input-read-failed",
        "input-json-invalid",
        "input-deserialization-failed",
        "input-bundle-missing",
        "receipt-generation-failed",

        // Receipt verifier structural checks.
        "receipt-schema-id-mismatch",
        "receipt-schema-version-mismatch",
        "verifier-schema-id-mismatch",
        "verifier-schema-version-mismatch",
        "receipt-status-invalid",
        "receipt-fields-missing",
        "receipt-boundaries-missing",
        "success-receipt-errors-present",
        "success-receipt-bundle-hash-missing",
        "success-receipt-report-export-hash-missing",
        "success-receipt-captured-result-not-successful",
        "failure-receipt-bundle-hash-missing",
        "failure-receipt-report-export-hash-missing",
        "receipt-check-order-mismatch",
        "receipt-bundle-schema-id-missing",
        "receipt-bundle-schema-id-mismatch",
        "receipt-bundle-schema-version-missing",
        "receipt-bundle-schema-version-mismatch",
        "verification-result-content-hash-mismatch",
        "receipt-content-hash-mismatch",

        // Receipt forbidden-content guard (superset of bundle-surface boundary tokens).
        "medical-advice-language-not-allowed",
        "persisted-output-claim-not-allowed",
        "protocol-intelligence-runtime-language-not-allowed",
        "pdf-claim-not-allowed",
        "timestamp-not-allowed",
        "host-data-not-allowed",
    ];

    private static string[] AllMappedTokens()
    {
        return Entries()
            .SelectMany(entry => entry.GetProperty("mappedTokens").EnumerateArray().Select(t => t.GetString()!))
            .ToArray();
    }

    private static BioStack.Contracts.Responses.ProtocolOperationsExportBundle ReadGoldenBundle()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "ProtocolOperationsExportBundle",
            "ProtocolOperationsExportBundle.golden.json");

        return JsonSerializer.Deserialize<BioStack.Contracts.Responses.ProtocolOperationsExportBundle>(File.ReadAllText(fixturePath))
            ?? throw new InvalidOperationException("Failed to deserialize golden bundle fixture.");
    }

    private static string[] Codes() => Entries().Select(entry => entry.GetProperty("code").GetString()!).ToArray();

    private static JsonElement[] Entries()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(CatalogSnapshotPath()));
        return doc.RootElement.GetProperty("entries").EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    private static string CanonicalizeJson(string json)
    {
        return JsonNode.Parse(json)!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static string CatalogSnapshotPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "backend",
            "tests",
            "Fixtures",
            "ProtocolOperationsExportBundle",
            "ProtocolOperationsOfflineVerificationResultCodeCatalog.golden.json");
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "backend", "BioStack.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BioStack repository root.");
    }
}
