namespace BioStack.Api.Tests.Alerts;

using BioStack.Api.Alerts;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

/// <summary>Growth Phase 6: alert planning, delivery rules, caps and copy (03 §6, 01 §4).</summary>
public sealed class EvidenceAlertServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc);
    private readonly SqliteConnection _connection;
    private readonly CapturingSender _sender = new();
    private List<KnowledgePublicClaim> _claims = new();

    public EvidenceAlertServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = Db();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private BioStackDbContext Db() => new(new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite(_connection).Options);

    private EvidenceAlertService Service(BioStackDbContext db, IEvidenceAlertEmailSender? sender = null)
    {
        var knowledge = new Mock<IKnowledgeService>();
        knowledge.Setup(k => k.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new[] { Entry("Retatrutide", "retatrutide", _claims) });
        return new EvidenceAlertService(db, knowledge.Object, sender ?? _sender,
            new EvidenceAlertOptions { Enabled = true, SiteBaseUrl = "https://biostack.cc" }, NullLogger<EvidenceAlertService>.Instance);
    }

    private static KnowledgeEntryResponse Entry(string name, string slug, List<KnowledgePublicClaim> claims) => new(
        name, new(), CompoundCategory.Peptide, "", "", EvidenceTier.Unknown, new(), "", new(), new(), new(), new(), new(),
        "", "", "", "", "", "", new(), new(), null, new(), "", "", new(), "", "",
        new KnowledgeEntryDetails { PublicClaims = claims }, slug);

    private static KnowledgePublicClaim Claim(string id, params (int v, string verdict, DateTime? at, string reason)[] versions) => new()
    {
        Id = id, Statement = "Retatrutide causes weight loss at 12 mg", Verdict = versions[^1].verdict,
        Changelog = versions.Select(x => new KnowledgeClaimChange { Version = x.v, Verdict = x.verdict, PublishedAt = x.at, Reason = x.reason }).ToList(),
    };

    private async Task<Guid> UserWatching(string type, string id, string? frequency = null)
    {
        await using var db = Db();
        var user = new AppUser { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.com", Provider = "email", ProviderKey = Guid.NewGuid().ToString() };
        db.AppUsers.Add(user);
        db.EvidenceWatches.Add(new EvidenceWatch { Id = Guid.NewGuid(), UserId = user.Id, TargetType = type, TargetId = id });
        if (frequency is not null)
            db.EvidenceAlertPreferences.Add(new EvidenceAlertPreference { UserId = user.Id, Frequency = frequency, UnsubscribeToken = EvidenceAlertService.NewToken() });
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public void Planner_OnlyPublishedVersionsAlert_AndClassifiesVerdictChangeVsStudyAdded()
    {
        var claims = new[]
        {
            Claim("c1", (1, "THIN", Now.AddDays(-30), "first"), (2, "PROMISING", Now.AddHours(-2), "A Phase 3 trial reported."), (3, "PROMISING", null, "draft, not published")),
            Claim("c2", (1, "THIN", Now.AddDays(-30), "first"), (2, "THIN", Now.AddHours(-1), "Added a 40 mg/week cohort.")),
        };
        var events = EvidenceAlertPlanner.Events("retatrutide", "Retatrutide", claims, Now.AddHours(-48));
        Assert.Equal(2, events.Count);
        var change = Assert.Single(events, e => e.ClaimId == "c1");
        Assert.Equal((EvidenceAlertPlanner.VerdictChange, "THIN", "PROMISING", 2), (change.AlertType, change.OldVerdict, change.NewVerdict, change.Version));
        var study = Assert.Single(events, e => e.ClaimId == "c2");
        Assert.Equal(EvidenceAlertPlanner.StudyAdded, study.AlertType);
        Assert.DoesNotContain("40 mg", study.Reason);
        Assert.DoesNotContain("12 mg", study.Statement);
    }

    [Theory]
    [InlineData("compound", "retatrutide", true)]
    [InlineData("claim", "c1", true)]
    [InlineData("vs", "retatrutide/semaglutide", true)]
    [InlineData("vs", "bpc-157/tb-500", false)]
    [InlineData("compound", "semaglutide", false)]
    public void Planner_MatchesCompoundClaimAndVsWatches(string type, string id, bool expected)
    {
        var e = new ClaimChangeEvent("retatrutide", "Retatrutide", "c1", "s", 2, "THIN", "PROMISING", "r", Now, EvidenceAlertPlanner.VerdictChange);
        Assert.Equal(expected, EvidenceAlertPlanner.Matches(new EvidenceWatch { TargetType = type, TargetId = id }, e));
    }

    [Fact]
    public async Task VerdictChange_EmailsOnce_WithOldNewReasonChangelogAndUnsubscribe_NoDose()
    {
        var userId = await UserWatching("compound", "retatrutide");
        _claims = new() { Claim("c1", (1, "THIN", Now.AddDays(-30), "first"), (2, "PROMISING", Now.AddHours(-2), "A Phase 3 trial at 8 mg reported.")) };

        await using (var db = Db())
        {
            var summary = await Service(db).RunOnceAsync(Now, default);
            Assert.Equal((1, 1), (summary.NewAlerts, summary.Emailed));
        }
        var mail = Assert.Single(_sender.Sent);
        Assert.Contains("THIN → PROMISING", mail.Subject);
        Assert.Contains("https://biostack.cc/knowledge/retatrutide/changelog", mail.PlainText);
        Assert.Contains("https://biostack.cc/u/", mail.UnsubscribeUrl);
        Assert.StartsWith("https://biostack.cc/api/v1/alerts/unsubscribe/", mail.OneClickUrl);
        Assert.Contains("Stop these emails in one click", mail.PlainText);
        Assert.DoesNotMatch(@"\d\s?mg", mail.PlainText + mail.Html + mail.Subject);

        await using (var db = Db())
        {
            var again = await Service(db).RunOnceAsync(Now.AddHours(1), default);
            Assert.Equal(0, again.NewAlerts);
            Assert.Single(_sender.Sent);
            var notice = Assert.Single(db.EvidenceAlertLog.Where(a => a.UserId == userId));
            Assert.Equal("emailed", notice.Delivery);
        }
    }

    [Fact]
    public async Task SecondVerdictChangeForSameCompoundWithin24h_IsCapped_ButStillAnInAppNotice()
    {
        await UserWatching("compound", "retatrutide");
        _claims = new()
        {
            Claim("c1", (1, "THIN", Now.AddDays(-30), "first"), (2, "PROMISING", Now.AddHours(-3), "trial")),
        };
        await using (var db = Db()) await Service(db).RunOnceAsync(Now, default);
        _claims.Add(Claim("c2", (1, "THIN", Now.AddDays(-30), "first"), (2, "MYTH", Now.AddHours(1), "review")));
        await using (var db = Db())
        {
            var s = await Service(db).RunOnceAsync(Now.AddHours(2), default);
            Assert.Equal(1, s.Capped);
            Assert.Equal(2, db.EvidenceAlertLog.Count());
        }
        Assert.Single(_sender.Sent);
    }

    [Fact]
    public async Task StudyAdded_GoesOutAsOneWeeklyBundle()
    {
        await UserWatching("claim", "c1");
        _claims = new() { Claim("c1", (1, "THIN", Now.AddDays(-30), "first"), (2, "THIN", Now.AddHours(-2), "Added a cohort study.")) };
        await using (var db = Db()) await Service(db).RunOnceAsync(Now, default);
        var mail = Assert.Single(_sender.Sent);
        Assert.Contains("new study added", mail.Subject);
        Assert.Contains("Verdict: unchanged (THIN)", mail.PlainText);
    }

    [Fact]
    public async Task FrequencyOff_OrNoTransport_KeepsAlertsInAppOnly()
    {
        var off = await UserWatching("compound", "retatrutide", frequency: "off");
        _claims = new() { Claim("c1", (1, "THIN", Now.AddDays(-30), "first"), (2, "PROMISING", Now.AddHours(-2), "trial")) };
        await using (var db = Db())
        {
            var s = await Service(db, new NoAlertEmailSender()).RunOnceAsync(Now, default);
            Assert.Equal(0, s.Emailed);
            Assert.Equal("in-app-only", db.EvidenceAlertLog.Single(a => a.UserId == off).Delivery);
        }
        Assert.Empty(_sender.Sent);
    }

    private sealed class CapturingSender : IEvidenceAlertEmailSender
    {
        public List<AlertEmail> Sent { get; } = new();
        public bool IsConfigured => true;
        public Task SendAsync(AlertEmail email, CancellationToken ct) { Sent.Add(email); return Task.CompletedTask; }
    }
}
