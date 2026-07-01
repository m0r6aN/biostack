namespace BioStack.Application.Tests.Services;

using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using Moq;
using Xunit;

public sealed class ProtocolOperationsReportExportServiceTests
{
    private static readonly Guid ProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProtocolId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly Regex ForbiddenLanguage = new(
        @"\b(recommend\w*|dose|dosing|diagnos(is|e|ed|tic)?|prescri(be|ption)|treatment|protocol\s+intelligence)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public async Task GetExportAsync_IsDeterministic_ForSeededReport()
    {
        var before = DateTime.UtcNow;
        var export1 = await BuildService(SeededReport()).GetExportAsync(ProfileId);
        var export2 = await BuildService(SeededReport()).GetExportAsync(ProfileId);
        var after = DateTime.UtcNow;

        Assert.Equal(ProtocolOperationsReportExportService.SchemaVersion, export1.Metadata.SchemaVersion);
        Assert.Equal(export1.Metadata.ProfileId, export2.Metadata.ProfileId);
        Assert.Equal(export1.Metadata.ProtocolId, export2.Metadata.ProtocolId);
        Assert.InRange(export1.Metadata.GeneratedAtUtc, before, after);
        Assert.InRange(export2.Metadata.GeneratedAtUtc, before, after);
        Assert.Equal(export1.Integrity.HashAlgorithm, export2.Integrity.HashAlgorithm);
        Assert.Equal(export1.Integrity.ContentHash, export2.Integrity.ContentHash);
        Assert.Equal(64, export1.Integrity.ContentHash.Length);
    }

    [Fact]
    public async Task GetExportAsync_ContentHashChangesWhenReportContentChanges()
    {
        var baseReport = SeededReport();
        var changedReport = baseReport with
        {
            Warnings = new List<string> { "Different warning state." },
        };

        var export1 = await BuildService(baseReport).GetExportAsync(ProfileId);
        var export2 = await BuildService(changedReport).GetExportAsync(ProfileId);

        Assert.NotEqual(export1.Integrity.ContentHash, export2.Integrity.ContentHash);
    }

    [Fact]
    public async Task GetExportAsync_IncludesNonMedicalDisclaimer()
    {
        var export = await BuildService(SeededReport()).GetExportAsync(ProfileId);

        Assert.False(string.IsNullOrWhiteSpace(export.Disclaimer));
        Assert.Equal(ProtocolOperationsReportExportService.Disclaimer, export.Disclaimer);
        Assert.Contains("observational", export.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetExportAsync_DoesNotContainForbiddenLanguage()
    {
        var export = await BuildService(SeededReport()).GetExportAsync(ProfileId);

        var serialized = JsonSerializer.Serialize(export);

        Assert.False(
            ForbiddenLanguage.IsMatch(serialized),
            $"Export contains forbidden narrative/recommendation language: {serialized}");
    }

    private static ProtocolOperationsReportExportService BuildService(ProtocolOperationsReport report)
    {
        var reportService = new Mock<IProtocolOperationsReportService>();
        reportService
            .Setup(s => s.GetReportAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        return new ProtocolOperationsReportExportService(reportService.Object);
    }

    private static ProtocolOperationsReport SeededReport()
    {
        return new ProtocolOperationsReport(
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
                new("CompoundStarted", new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc), "Compound started"),
                new("CheckInCreated", new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc), "Check-in logged"),
            },
            new List<ProtocolOperationsEvidenceReference>(),
            new List<string> { "No evidence references recorded for protocol yet." });
    }
}
