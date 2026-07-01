namespace BioStack.Application.Tests.Services;

using System.Text.RegularExpressions;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public sealed class ProtocolOperationsReportServiceTests
{
    private static readonly Guid ProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProtocolId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Regex ForbiddenLanguage = new(
        @"\b(recommend(ed|s)?|dose|dosing|diagnos(is|e|ed)|prescri(be|ption)|treatment|protocol\s+intelligence)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public async Task GetReportAsync_IsDeterministic_ForSeededInput()
    {
        var before = DateTime.UtcNow;
        var report1 = await BuildService(SeededTimeline()).GetReportAsync(ProfileId);
        var report2 = await BuildService(SeededTimeline()).GetReportAsync(ProfileId);
        var after = DateTime.UtcNow;

        Assert.Equal(report1.Summary, report2.Summary);
        Assert.Equal(report1.RecentEvents, report2.RecentEvents);
        Assert.Equal(report1.Warnings, report2.Warnings);
        Assert.Equal(report1.ProtocolId, report2.ProtocolId);
        Assert.InRange(report1.GeneratedAtUtc, before, after);
    }

    [Fact]
    public async Task GetReportAsync_ContainsObservationalCountsAndStateOnly()
    {
        var report = await BuildService(SeededTimeline()).GetReportAsync(ProfileId);

        Assert.Equal(ProfileId, report.ProfileId);
        Assert.Equal(ProtocolId, report.ProtocolId);
        Assert.Equal(1, report.Summary.ActiveCompoundsCount);
        Assert.Equal(1, report.Summary.LoggedDosesCount);
        Assert.Equal(1, report.Summary.CheckInCount);
        Assert.Equal(3, report.Summary.MonitoringEntryCount); // CompoundStarted + CheckInCreated + PhaseStarted
        Assert.True(report.Summary.MilestoneCount > 0);
        Assert.Equal(0, report.Summary.EvidenceReferenceCount);
        Assert.NotNull(report.Summary.LatestActivityUtc);
        Assert.Contains("No evidence references recorded for this protocol yet.", report.Warnings);
        Assert.DoesNotContain(
            report.Warnings, w => w.Contains("No active protocol", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetReportAsync_WithNoData_ReturnsHonestEmptyStateAndWarnings()
    {
        var protocolRepo = new Mock<IProtocolRepository>();
        protocolRepo.Setup(r => r.GetByPersonIdAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Protocol>());
        var compoundRepo = new Mock<ICompoundRecordRepository>();
        compoundRepo.Setup(r => r.GetByPersonIdAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompoundRecord>());
        var checkInRepo = new Mock<ICheckInRepository>();
        checkInRepo.Setup(r => r.GetByPersonIdAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CheckIn>());
        var timelineRepo = new Mock<ITimelineEventRepository>();
        timelineRepo.Setup(r => r.GetByPersonIdAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimelineEvent>());

        var service = new ProtocolOperationsReportService(
            protocolRepo.Object,
            compoundRepo.Object,
            checkInRepo.Object,
            timelineRepo.Object,
            new ProtocolPortalBaseline(),
            BuildOwnershipGuard());

        var report = await service.GetReportAsync(ProfileId);

        Assert.Null(report.ProtocolId);
        Assert.Equal(0, report.Summary.ActiveCompoundsCount);
        Assert.Equal(0, report.Summary.LoggedDosesCount);
        Assert.Equal(0, report.Summary.CheckInCount);
        Assert.Equal(0, report.Summary.MonitoringEntryCount);
        Assert.Null(report.Summary.LatestActivityUtc);
        Assert.Empty(report.RecentEvents);
        Assert.Contains("No active protocol found for this profile.", report.Warnings);
        Assert.Contains("No active compounds recorded.", report.Warnings);
        Assert.Contains("No check-ins recorded yet.", report.Warnings);
    }

    [Fact]
    public async Task GetReportAsync_DoesNotContainRecommendationDiagnosisDosingOrProtocolIntelligenceLanguage()
    {
        var report = await BuildService(SeededTimeline()).GetReportAsync(ProfileId);

        var serialized = System.Text.Json.JsonSerializer.Serialize(report);

        Assert.False(
            ForbiddenLanguage.IsMatch(serialized),
            $"Report contains forbidden narrative/recommendation language: {serialized}");
    }

    private static ProtocolOperationsReportService BuildService(List<TimelineEvent> timeline)
    {
        var protocol = new Protocol
        {
            Id = ProtocolId,
            PersonId = ProfileId,
            Name = "Seeded Protocol",
            IsDraft = false,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var compound = new CompoundRecord
        {
            Id = Guid.NewGuid(),
            PersonId = ProfileId,
            Name = "Seeded Compound",
            Status = CompoundStatus.Active,
        };

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            PersonId = ProfileId,
            Date = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
        };

        var protocolRepo = new Mock<IProtocolRepository>();
        protocolRepo.Setup(r => r.GetByPersonIdAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Protocol> { protocol });

        var compoundRepo = new Mock<ICompoundRecordRepository>();
        compoundRepo.Setup(r => r.GetByPersonIdAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompoundRecord> { compound });

        var checkInRepo = new Mock<ICheckInRepository>();
        checkInRepo.Setup(r => r.GetByPersonIdAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CheckIn> { checkIn });

        var timelineRepo = new Mock<ITimelineEventRepository>();
        timelineRepo.Setup(r => r.GetByPersonIdAsync(ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeline);

        return new ProtocolOperationsReportService(
            protocolRepo.Object,
            compoundRepo.Object,
            checkInRepo.Object,
            timelineRepo.Object,
            new ProtocolPortalBaseline(),
            BuildOwnershipGuard());
    }

    private static IOwnershipGuard BuildOwnershipGuard()
    {
        var guard = new Mock<IOwnershipGuard>();
        guard.Setup(g => g.EnsureProfileOwnedAsync(ProfileId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return guard.Object;
    }

    private static List<TimelineEvent> SeededTimeline() =>
    [
        new TimelineEvent
        {
            Id = Guid.NewGuid(),
            PersonId = ProfileId,
            EventType = EventType.CompoundStarted,
            Title = "Started Seeded Compound",
            OccurredAtUtc = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            RelatedEntityId = Guid.NewGuid(),
        },
        new TimelineEvent
        {
            Id = Guid.NewGuid(),
            PersonId = ProfileId,
            EventType = EventType.CheckInCreated,
            Title = "Check-in recorded",
            OccurredAtUtc = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc),
        },
        new TimelineEvent
        {
            Id = Guid.NewGuid(),
            PersonId = ProfileId,
            EventType = EventType.ProtocolPhaseStarted,
            Title = "Started protocol phase: Phase 1",
            OccurredAtUtc = new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc),
        },
        new TimelineEvent
        {
            Id = Guid.NewGuid(),
            PersonId = ProfileId,
            EventType = EventType.NoteAdded,
            Title = "Scheduled doses marked taken",
            Description = "Doses logged for 2026-01-06.",
            OccurredAtUtc = new DateTime(2026, 1, 6, 8, 0, 0, DateTimeKind.Utc),
            RelatedEntityType = ProtocolPortalService.DoseLogMarker,
        },
        new TimelineEvent
        {
            Id = Guid.NewGuid(),
            PersonId = ProfileId,
            EventType = EventType.NoteAdded,
            Title = "Care-team message",
            Description = "Just checking in, feeling good this week.",
            OccurredAtUtc = new DateTime(2026, 1, 7, 8, 0, 0, DateTimeKind.Utc),
            RelatedEntityType = ProtocolPortalService.CareTeamMessageMarker,
        },
    ];
}
