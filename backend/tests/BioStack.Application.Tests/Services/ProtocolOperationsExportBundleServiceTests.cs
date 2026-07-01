namespace BioStack.Application.Tests.Services;

using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using Moq;
using Xunit;

public sealed class ProtocolOperationsExportBundleServiceTests
{
    private static readonly Guid ProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProtocolId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTime FixedGeneratedAtUtc = new(2026, 1, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Regex ForbiddenLanguage = new(
        @"\b(recommend\w*|dose|dosing|diagnos(is|e|ed|tic)?|prescri(be|ption)|treatment|protocol\s+intelligence)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public async Task GetBundleAsync_IsDeterministic_ForSeededExportAndFixedClock()
    {
        var export = SeededExport();
        var service = BuildService(export);

        var bundle1 = await service.GetBundleAsync(ProfileId);
        var bundle2 = await service.GetBundleAsync(ProfileId);

        Assert.Equal(ProtocolOperationsExportBundleService.SchemaVersion, bundle1.Metadata.SchemaVersion);
        Assert.Equal(FixedGeneratedAtUtc, bundle1.Metadata.GeneratedAtUtc);
        Assert.Equal(ProfileId, bundle1.Metadata.ProfileId);
        Assert.Equal(ProtocolId, bundle1.Metadata.ProtocolId);
        Assert.Equal(bundle1.Integrity.BundleContentHash, bundle2.Integrity.BundleContentHash);
        Assert.Equal(JsonSerializer.Serialize(bundle1), JsonSerializer.Serialize(bundle2));
        Assert.Equal(64, bundle1.Integrity.BundleContentHash.Length);
    }

    [Fact]
    public async Task GetBundleAsync_BundleHashChanges_WhenEmbeddedExportChanges()
    {
        var baseBundle = await BuildService(SeededExport()).GetBundleAsync(ProfileId);
        var changedExport = SeededExport() with
        {
            Report = SeededReport() with
            {
                Warnings = new List<string> { "Different observation state." },
            },
            Integrity = new ProtocolOperationsReportExportIntegrity(
                ProtocolOperationsReportExportService.HashAlgorithmName,
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
        };

        var changedBundle = await BuildService(changedExport).GetBundleAsync(ProfileId);

        Assert.NotEqual(baseBundle.Integrity.BundleContentHash, changedBundle.Integrity.BundleContentHash);
    }

    [Fact]
    public async Task GetBundleAsync_PreservesEmbeddedReportExportHash()
    {
        var export = SeededExport();

        var bundle = await BuildService(export).GetBundleAsync(ProfileId);

        Assert.Equal(export.Integrity.ContentHash, bundle.Integrity.ReportExportContentHash);
        Assert.Equal(export.Integrity.ContentHash, bundle.ReportExport.Integrity.ContentHash);
    }

    [Fact]
    public async Task GetBundleAsync_IncludesJsonReportExportArtifactDescriptor()
    {
        var export = SeededExport();

        var bundle = await BuildService(export).GetBundleAsync(ProfileId);

        var artifact = Assert.Single(bundle.Artifacts);
        Assert.Equal("protocol-operations-report-export-json", artifact.ArtifactId);
        Assert.Equal("application/json", artifact.MediaType);
        Assert.Equal("report-export", artifact.Role);
        Assert.Equal(export.Metadata.SchemaVersion, artifact.SchemaVersion);
        Assert.Equal(export.Integrity.ContentHash, artifact.ContentHash);
    }

    [Fact]
    public async Task GetBundleAsync_IncludesObservationalDisclaimer()
    {
        var bundle = await BuildService(SeededExport()).GetBundleAsync(ProfileId);

        Assert.False(string.IsNullOrWhiteSpace(bundle.Disclaimer));
        Assert.Contains("observational", bundle.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("non-medical", bundle.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBundleAsync_SerializedBundleDoesNotContainForbiddenLanguage()
    {
        var bundle = await BuildService(SeededExport()).GetBundleAsync(ProfileId);

        var serialized = JsonSerializer.Serialize(bundle);

        Assert.False(
            ForbiddenLanguage.IsMatch(serialized),
            "Export bundle must remain observational and avoid restricted medical or legacy runtime language.");
    }

    private static ProtocolOperationsExportBundleService BuildService(ProtocolOperationsReportExport export)
    {
        var exportService = new Mock<IProtocolOperationsReportExportService>();
        exportService
            .Setup(s => s.GetExportAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        return new ProtocolOperationsExportBundleService(exportService.Object, () => FixedGeneratedAtUtc);
    }

    private static ProtocolOperationsReportExport SeededExport()
    {
        var report = SeededReport();
        var contentHash = ProtocolOperationsReportExportService.ComputeContentHash(report);

        return new ProtocolOperationsReportExport(
            new ProtocolOperationsReportExportMetadata(
                ProtocolOperationsReportExportService.SchemaVersion,
                new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc),
                ProfileId,
                ProtocolId),
            report,
            new ProtocolOperationsReportExportIntegrity(
                ProtocolOperationsReportExportService.HashAlgorithmName,
                contentHash),
            ProtocolOperationsReportExportService.Disclaimer);
    }

    private static ProtocolOperationsReport SeededReport() =>
        new(
            ProfileId,
            ProtocolId,
            new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc),
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
                new(
                    "CompoundStarted",
                    new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                    "Compound activity logged."),
                new(
                    "CheckInCreated",
                    new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc),
                    "Check-in logged."),
            },
            new List<ProtocolOperationsEvidenceReference>(),
            new List<string> { "No evidence references recorded." });
}
