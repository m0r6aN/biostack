namespace BioStack.Api.Alerts;

using System.Security.Cryptography;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class EvidenceAlertOptions
{
    /// <summary>Off by default: set EvidenceAlerts__Enabled=true once an email transport is configured.</summary>
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 60;
    /// <summary>How far back to look for newly published versions on each run.</summary>
    public int LookbackHours { get; set; } = 48;
    public string SiteBaseUrl { get; set; } = "https://biostack.cc";
}

public sealed record AlertRunSummary(int Events, int NewAlerts, int Emailed, int InAppOnly, int Capped);

/// <summary>
/// Growth Phase 6: raises alerts for watchers when the council (or Clint) publishes a claim
/// version, then delivers them. Verdict changes go out instantly, capped at one instant email
/// per compound per 24h; "study added" goes out in a weekly bundle. Every alert is also an
/// in-app notice. Frequency "off" or no email transport → in-app only.
/// </summary>
public sealed class EvidenceAlertService(
    BioStackDbContext db, IKnowledgeService knowledge, IEvidenceAlertEmailSender email, EvidenceAlertOptions options, ILogger<EvidenceAlertService> logger)
{
    public async Task<AlertRunSummary> RunOnceAsync(DateTime nowUtc, CancellationToken ct)
    {
        var since = nowUtc.AddHours(-options.LookbackHours);
        var entries = await knowledge.GetAllCompoundsAsync(ct);
        var events = entries
            .Where(e => e.Details is not null)
            .SelectMany(e => EvidenceAlertPlanner.Events(e.Slug, e.CanonicalName, e.Details!.PublicClaims, since))
            .ToList();

        var created = 0;
        if (events.Count > 0)
        {
            var watches = await db.EvidenceWatches.AsNoTracking().ToListAsync(ct);
            var existing = (await db.EvidenceAlertLog.AsNoTracking()
                    .Where(a => a.CreatedAtUtc >= since.AddDays(-7))
                    .Select(a => new { a.UserId, a.ClaimId, a.Version, a.AlertType })
                    .ToListAsync(ct))
                .Select(a => (a.UserId, a.ClaimId, a.Version, a.AlertType)).ToHashSet();
            foreach (var e in events)
            {
                foreach (var userId in watches.Where(w => EvidenceAlertPlanner.Matches(w, e)).Select(w => w.UserId).Distinct())
                {
                    if (!existing.Add((userId, e.ClaimId, e.Version, e.AlertType))) continue;
                    db.EvidenceAlertLog.Add(new EvidenceAlertLogEntry
                    {
                        Id = Guid.NewGuid(), UserId = userId, AlertType = e.AlertType, CompoundSlug = e.CompoundSlug,
                        ClaimId = e.ClaimId, Version = e.Version, OldVerdict = e.OldVerdict, NewVerdict = e.NewVerdict,
                        Reason = Truncate(e.Reason, 500), CreatedAtUtc = nowUtc,
                    });
                    created++;
                }
            }
            await db.SaveChangesAsync(ct);
        }

        // Rebuild each alert's event from the log row + current records (a weekly bundle can be
        // older than the lookback window).
        var claimInfo = entries.Where(e => e.Details is not null)
            .SelectMany(e => e.Details!.PublicClaims.Select(c => (c.Id, Name: e.CanonicalName, c.Statement)))
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());
        ClaimChangeEvent? EventFor(EvidenceAlertLogEntry a) => claimInfo.TryGetValue(a.ClaimId, out var info)
            ? new ClaimChangeEvent(a.CompoundSlug, info.Name, a.ClaimId, EvidenceAlertPlanner.RedactDoses(info.Statement), a.Version, a.OldVerdict, a.NewVerdict, a.Reason, a.CreatedAtUtc, a.AlertType)
            : null;
        var pending = await db.EvidenceAlertLog.Where(a => a.Delivery == "pending").ToListAsync(ct);
        int emailed = 0, inApp = 0, capped = 0;
        foreach (var group in pending.GroupBy(a => a.UserId))
        {
            var user = await db.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == group.Key, ct);
            var pref = await EnsurePreferenceAsync(group.Key, nowUtc, ct);
            var site = options.SiteBaseUrl.TrimEnd('/');
            var token = pref.UnsubscribeToken;
            var canEmail = email.IsConfigured && pref.EmailEnabled && pref.Frequency != "off" && !string.IsNullOrWhiteSpace(user?.Email);

            foreach (var alert in group.Where(a => a.AlertType == EvidenceAlertPlanner.VerdictChange))
            {
                var e = EventFor(alert);
                if (!canEmail || e is null || pref.Frequency == "weekly")
                {
                    if (pref.Frequency == "weekly" && canEmail && e is not null) continue; // goes in the weekly bundle
                    alert.Delivery = "in-app-only"; inApp++; continue;
                }
                var recent = await db.EvidenceAlertLog.AnyAsync(a => a.UserId == alert.UserId && a.CompoundSlug == alert.CompoundSlug
                    && a.AlertType == EvidenceAlertPlanner.VerdictChange && a.Delivery == "emailed" && a.EmailedAtUtc >= nowUtc.AddHours(-24), ct);
                if (recent) { alert.Delivery = "capped"; capped++; continue; }
                await email.SendAsync(EvidenceAlertCopy.VerdictChanged(user!.Email, e, site, token), ct);
                alert.Delivery = "emailed"; alert.EmailedAtUtc = nowUtc; emailed++;
            }

            var bundle = group.Where(a => a.Delivery == "pending" && (a.AlertType == EvidenceAlertPlanner.StudyAdded || pref.Frequency == "weekly")).ToList();
            if (bundle.Count == 0) continue;
            if (!canEmail) { bundle.ForEach(a => a.Delivery = "in-app-only"); inApp += bundle.Count; continue; }
            var lastBundle = await db.EvidenceAlertLog.Where(a => a.UserId == group.Key && a.Delivery == "emailed"
                && (a.AlertType == EvidenceAlertPlanner.StudyAdded || pref.Frequency == "weekly")).MaxAsync(a => (DateTime?)a.EmailedAtUtc, ct);
            if (lastBundle is { } last && last > nowUtc.AddDays(-7)) continue;
            var bundleEvents = bundle.Select(EventFor).OfType<ClaimChangeEvent>().ToList();
            if (bundleEvents.Count > 0)
                await email.SendAsync(EvidenceAlertCopy.StudyBundle(user!.Email, bundleEvents, site, token), ct);
            bundle.ForEach(a => { a.Delivery = "emailed"; a.EmailedAtUtc = nowUtc; });
            emailed += bundle.Count;
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Evidence alerts: {Events} events, {New} new, {Emailed} emailed, {InApp} in-app, {Capped} capped", events.Count, created, emailed, inApp, capped);
        return new AlertRunSummary(events.Count, created, emailed, inApp, capped);
    }

    public async Task<EvidenceAlertPreference> EnsurePreferenceAsync(Guid userId, DateTime nowUtc, CancellationToken ct)
    {
        var pref = await db.EvidenceAlertPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (pref is not null) return pref;
        pref = new EvidenceAlertPreference { UserId = userId, UnsubscribeToken = NewToken(), UpdatedAtUtc = nowUtc };
        db.EvidenceAlertPreferences.Add(pref);
        await db.SaveChangesAsync(ct);
        return pref;
    }

    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}

/// <summary>Runs <see cref="EvidenceAlertService"/> on an interval when EvidenceAlerts:Enabled is true.</summary>
public sealed class EvidenceAlertHostedService(IServiceScopeFactory scopes, EvidenceAlertOptions options, ILogger<EvidenceAlertHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Evidence alerts disabled (EvidenceAlerts:Enabled=false).");
            return;
        }
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(5, options.IntervalMinutes)));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<EvidenceAlertService>().RunOnceAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Evidence alert run failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
