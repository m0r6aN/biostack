namespace BioStack.Api.Endpoints;

using System.Text.RegularExpressions;
using BioStack.Api.Alerts;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public sealed record WatchRequest(string TargetType, string TargetId);
public sealed record AlertPreferenceRequest(string Frequency, bool EmailEnabled);

/// <summary>
/// Growth Phase 6 (03 §6): one-tap watch on a compound, claim or /vs pair (free account),
/// alert preferences with a global frequency, in-app notices, and a public one-click
/// unsubscribe by token (no sign-in, works from the email's List-Unsubscribe-Post too).
/// </summary>
public static partial class WatchlistEndpoints
{
    public const int MaxWatchesPerUser = 200;
    private static readonly HashSet<string> TargetTypes = ["compound", "claim", "vs"];
    private static readonly HashSet<string> Frequencies = ["instant", "weekly", "off"];

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,98}(/[a-z0-9][a-z0-9-]{0,98})?$")]
    private static partial Regex TargetIdPattern();

    public static void MapWatchlistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/watchlist").WithTags("Watchlist").RequireAuthorization();
        group.MapGet("/", List).WithName("ListWatches");
        group.MapPost("/", Add).WithName("AddWatch");
        group.MapDelete("/{id:guid}", Remove).WithName("RemoveWatch");

        var alerts = app.MapGroup("/api/v1/alerts").WithTags("Alerts");
        alerts.MapGet("/preferences", GetPreferences).RequireAuthorization().WithName("GetAlertPreferences");
        alerts.MapPut("/preferences", PutPreferences).RequireAuthorization().WithName("PutAlertPreferences");
        alerts.MapGet("/notices", Notices).RequireAuthorization().WithName("GetAlertNotices");
        alerts.MapPost("/unsubscribe/{token}", Unsubscribe).RequireRateLimiting("lead-capture").WithName("UnsubscribeAlerts");
    }

    public static bool IsValidTarget(string? type, string? id) =>
        type is not null && TargetTypes.Contains(type) && id is not null && TargetIdPattern().IsMatch(id)
        && (type == "vs") == id.Contains('/');

    private static async Task<IResult> List([FromServices] ICurrentUserAccessor user, BioStackDbContext db, CancellationToken ct)
    {
        var userId = user.GetCurrentUserId();
        var watches = await db.EvidenceWatches.AsNoTracking().Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .Select(w => new { w.Id, w.TargetType, w.TargetId, w.CreatedAtUtc }).ToListAsync(ct);
        return Results.Ok(watches);
    }

    private static async Task<IResult> Add(WatchRequest request, [FromServices] ICurrentUserAccessor user, BioStackDbContext db, CancellationToken ct)
    {
        var type = request.TargetType?.Trim().ToLowerInvariant();
        var id = request.TargetId?.Trim().ToLowerInvariant();
        if (!IsValidTarget(type, id)) return Results.BadRequest(new { error = "A compound slug, claim id or a/b pair is required." });
        var userId = user.GetCurrentUserId();
        var existing = await db.EvidenceWatches.FirstOrDefaultAsync(w => w.UserId == userId && w.TargetType == type && w.TargetId == id, ct);
        if (existing is not null) return Results.Ok(new { existing.Id, existing.TargetType, existing.TargetId, existing.CreatedAtUtc });
        if (await db.EvidenceWatches.CountAsync(w => w.UserId == userId, ct) >= MaxWatchesPerUser)
            return Results.BadRequest(new { error = "Watch limit reached." });
        var watch = new EvidenceWatch { Id = Guid.NewGuid(), UserId = userId, TargetType = type!, TargetId = id!, CreatedAtUtc = DateTime.UtcNow };
        db.EvidenceWatches.Add(watch);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/watchlist/{watch.Id}", new { watch.Id, watch.TargetType, watch.TargetId, watch.CreatedAtUtc });
    }

    private static async Task<IResult> Remove(Guid id, [FromServices] ICurrentUserAccessor user, BioStackDbContext db, CancellationToken ct)
    {
        var userId = user.GetCurrentUserId();
        var watch = await db.EvidenceWatches.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);
        if (watch is null) return Results.NotFound();
        db.EvidenceWatches.Remove(watch);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetPreferences([FromServices] ICurrentUserAccessor user, BioStackDbContext db, CancellationToken ct)
    {
        var userId = user.GetCurrentUserId();
        var pref = await db.EvidenceAlertPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return Results.Ok(new { frequency = pref?.Frequency ?? "instant", emailEnabled = pref?.EmailEnabled ?? true });
    }

    private static async Task<IResult> PutPreferences(AlertPreferenceRequest request, [FromServices] ICurrentUserAccessor user, BioStackDbContext db, CancellationToken ct)
    {
        var frequency = request.Frequency?.Trim().ToLowerInvariant();
        if (frequency is null || !Frequencies.Contains(frequency)) return Results.BadRequest(new { error = "frequency must be instant, weekly or off." });
        var userId = user.GetCurrentUserId();
        var pref = await db.EvidenceAlertPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (pref is null)
        {
            pref = new EvidenceAlertPreference { UserId = userId, UnsubscribeToken = EvidenceAlertService.NewToken() };
            db.EvidenceAlertPreferences.Add(pref);
        }
        pref.Frequency = frequency;
        pref.EmailEnabled = request.EmailEnabled;
        pref.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { frequency = pref.Frequency, emailEnabled = pref.EmailEnabled });
    }

    private static async Task<IResult> Notices([FromServices] ICurrentUserAccessor user, BioStackDbContext db, CancellationToken ct)
    {
        var userId = user.GetCurrentUserId();
        var notices = await db.EvidenceAlertLog.AsNoTracking().Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAtUtc).Take(50)
            .Select(a => new { a.Id, a.AlertType, a.CompoundSlug, a.ClaimId, a.Version, a.OldVerdict, a.NewVerdict, a.Reason, a.CreatedAtUtc })
            .ToListAsync(ct);
        return Results.Ok(notices);
    }

    /// <summary>One click: turns email alerts off for the token's owner. Idempotent; unknown tokens still 204.</summary>
    private static async Task<IResult> Unsubscribe(string token, BioStackDbContext db, CancellationToken ct)
    {
        if (token.Length is < 16 or > 64) return Results.NoContent();
        var pref = await db.EvidenceAlertPreferences.FirstOrDefaultAsync(p => p.UnsubscribeToken == token, ct);
        if (pref is not null)
        {
            pref.EmailEnabled = false;
            pref.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return Results.NoContent();
    }
}
