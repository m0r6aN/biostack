namespace BioStack.Api.Endpoints;

using System.Text.RegularExpressions;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public sealed record CommunityTheme(string Id, string Label, string Kind);
public sealed record CommunityThemeView(string Id, string Label);
public sealed record CommunityThemesResponse(
    string CompoundSlug, int WindowDays, int MinCount, int? Submissions,
    IReadOnlyList<CommunityThemeView> SideEffects, IReadOnlyList<CommunityThemeView> Benefits);
public sealed record ModerateApproveRequest(string Theme);
public sealed record ModerationItem(
    Guid Id, string CompoundSlug, string ClaimText, string SeenOn, string? Experience, string? ThemeText,
    bool Redacted, bool Emergency, DateTime CreatedAtUtc);

/// <summary>Controlled theme vocabulary for "What people report" (03 §9 step 2). Moderators can only approve into these.</summary>
public static class CommunityThemeVocabulary
{
    public const string SideEffect = "side-effect";
    public const string Benefit = "benefit";

    public static readonly IReadOnlyList<CommunityTheme> All =
    [
        new("nausea", "Nausea", SideEffect),
        new("gi-upset", "Stomach or bowel upset", SideEffect),
        new("injection-site-reaction", "Injection-site reaction", SideEffect),
        new("headache", "Headache", SideEffect),
        new("fatigue", "Tiredness", SideEffect),
        new("dizziness", "Dizziness", SideEffect),
        new("sleep-changes", "Sleep changes", SideEffect),
        new("appetite-changes", "Appetite changes", SideEffect),
        new("water-retention", "Water retention", SideEffect),
        new("heart-rate-changes", "Heart-rate changes", SideEffect),
        new("mood-changes", "Mood changes", SideEffect),
        new("numbness-tingling", "Numbness or tingling", SideEffect),
        new("less-pain", "Less pain", Benefit),
        new("faster-recovery", "Faster recovery", Benefit),
        new("better-sleep", "Better sleep", Benefit),
        new("reduced-appetite", "Reduced appetite", Benefit),
        new("weight-change", "Weight change", Benefit),
        new("more-energy", "More energy", Benefit),
        new("better-mood", "Better mood", Benefit),
        new("better-focus", "Better focus", Benefit),
    ];

    private static readonly Dictionary<string, CommunityTheme> ById = All.ToDictionary(t => t.Id, StringComparer.Ordinal);

    public static CommunityTheme? Find(string? id) => id is not null && ById.TryGetValue(id, out var t) ? t : null;
}

/// <summary>A minimal row for aggregation (keeps the k-threshold logic pure and testable).</summary>
public sealed record ApprovedThemeRow(string Theme, string SubmitterKey, DateTime CreatedAtUtc);

public static class CommunityThemeAggregator
{
    /// <summary>06 §C BLOCKER: never show a theme below k = 10 independent people. Config can raise it, never lower it.</summary>
    public const int MinimumK = 10;
    public const int DefaultWindowDays = 180;

    public static int ResolveK(IConfiguration config) => Math.Max(MinimumK, config.GetValue("CommunityColumns:MinCount", MinimumK));

    public static int ResolveWindowDays(IConfiguration config) =>
        Math.Clamp(config.GetValue("CommunityColumns:WindowDays", DefaultWindowDays), 30, 730);

    /// <summary>
    /// Themes are shown only when at least <paramref name="k"/> distinct submitters approved into that theme
    /// inside the window. Returns the themes (ordered by independent count, counts not exposed) and N, the number
    /// of independent submissions behind the shown themes (one per submitter per theme).
    /// </summary>
    public static (IReadOnlyList<CommunityTheme> Shown, int Submissions) Aggregate(
        IEnumerable<ApprovedThemeRow> rows, DateTime nowUtc, int k, int windowDays)
    {
        var since = nowUtc.AddDays(-windowDays);
        var counted = rows
            .Where(r => r.CreatedAtUtc >= since && r.CreatedAtUtc <= nowUtc)
            .Select(r => (Theme: CommunityThemeVocabulary.Find(r.Theme), r.SubmitterKey))
            .Where(r => r.Theme is not null && !string.IsNullOrEmpty(r.SubmitterKey))
            .GroupBy(r => r.Theme!.Id)
            .Select(g => (Theme: g.First().Theme!, People: g.Select(x => x.SubmitterKey).Distinct(StringComparer.Ordinal).Count()))
            .Where(x => x.People >= k)
            .OrderByDescending(x => x.People).ThenBy(x => x.Theme.Id, StringComparer.Ordinal)
            .ToList();
        return (counted.Select(x => x.Theme).ToList(), counted.Sum(x => x.People));
    }
}

/// <summary>
/// Growth Phase 9 (03 §9): community columns. Public reads expose aggregated themes only (no text,
/// no quotes, no per-theme counts, no identities). Moderation is admin-only: approve a sanitized
/// submission into the controlled theme vocabulary, or reject it.
/// </summary>
public static partial class CommunityEndpoints
{
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,98}$")]
    private static partial Regex Slug();

    public static void MapCommunityEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/community/{slug}/themes", Themes).WithTags("Community").WithName("GetCommunityThemes");

        var admin = app.MapGroup("/api/v1/admin/community").WithTags("Community").RequireAuthorization("AdminOnly");
        admin.MapGet("/themes", () => Results.Ok(CommunityThemeVocabulary.All)).WithName("ListCommunityThemeVocabulary");
        admin.MapGet("/submissions", Queue).WithName("ListClaimSubmissionsForModeration");
        admin.MapPost("/submissions/{id:guid}/approve", Approve).WithName("ApproveClaimSubmission");
        admin.MapPost("/submissions/{id:guid}/reject", Reject).WithName("RejectClaimSubmission");
    }

    private static async Task<IResult> Themes(string slug, HttpContext http, [FromServices] IConfiguration config, BioStackDbContext db, CancellationToken ct)
    {
        slug = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (!Slug().IsMatch(slug)) return Results.NotFound();
        var k = CommunityThemeAggregator.ResolveK(config);
        var windowDays = CommunityThemeAggregator.ResolveWindowDays(config);
        var now = DateTime.UtcNow;
        var since = now.AddDays(-windowDays);
        var rows = await db.ClaimSubmissions.AsNoTracking()
            .Where(s => s.CompoundSlug == slug && s.Status == "approved" && s.Theme != null && s.CreatedAtUtc >= since)
            .Select(s => new ApprovedThemeRow(s.Theme!, s.SubmitterKey, s.CreatedAtUtc))
            .ToListAsync(ct);
        var (shown, n) = CommunityThemeAggregator.Aggregate(rows, now, k, windowDays);
        http.Response.Headers.CacheControl = "public, max-age=600";
        return Results.Ok(new CommunityThemesResponse(
            slug, windowDays, k, shown.Count == 0 ? null : n,
            shown.Where(t => t.Kind == CommunityThemeVocabulary.SideEffect).Select(t => new CommunityThemeView(t.Id, t.Label)).ToList(),
            shown.Where(t => t.Kind == CommunityThemeVocabulary.Benefit).Select(t => new CommunityThemeView(t.Id, t.Label)).ToList()));
    }

    private static async Task<IResult> Queue(BioStackDbContext db, CancellationToken ct, string? status = "pending", int take = 50)
    {
        status = status is "approved" or "rejected" ? status : "pending";
        var items = await db.ClaimSubmissions.AsNoTracking()
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.Emergency).ThenBy(s => s.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 200))
            .Select(s => new ModerationItem(s.Id, s.CompoundSlug, s.ClaimText, s.SeenOn, s.Experience, s.ThemeText, s.Redacted, s.Emergency, s.CreatedAtUtc))
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> Approve(Guid id, ModerateApproveRequest request, BioStackDbContext db, CancellationToken ct)
    {
        var row = await db.ClaimSubmissions.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (row is null) return Results.NotFound();
        var theme = CommunityThemeVocabulary.Find(request.Theme?.Trim());
        if (theme is null) return Results.BadRequest(new { error = "theme must come from the controlled vocabulary." });
        if (row.Experience is not (CommunityThemeVocabulary.SideEffect or CommunityThemeVocabulary.Benefit))
            return Results.BadRequest(new { error = "only side-effect or benefit experiences can become a theme." });
        if (row.Experience != theme.Kind)
            return Results.BadRequest(new { error = $"theme '{theme.Id}' is a {theme.Kind} theme; the submission reported a {row.Experience}." });

        // Moderator recheck (06 §C): run the stripper again before anything is used.
        var (claim, claimRedacted) = ClaimSubmissionSanitizer.Clean(row.ClaimText, ClaimCheckEndpoints.MaxClaimLength);
        var (themeText, themeRedacted) = ClaimSubmissionSanitizer.Clean(row.ThemeText, ClaimCheckEndpoints.MaxThemeLength);
        row.ClaimText = claim;
        row.ThemeText = string.IsNullOrEmpty(themeText) ? null : themeText;
        row.Redacted |= claimRedacted || themeRedacted;
        row.Status = "approved";
        row.Theme = theme.Id;
        row.ModeratedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = row.Id, status = row.Status, theme = row.Theme });
    }

    private static async Task<IResult> Reject(Guid id, BioStackDbContext db, CancellationToken ct)
    {
        var row = await db.ClaimSubmissions.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (row is null) return Results.NotFound();
        row.Status = "rejected";
        row.Theme = null;
        row.ModeratedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = row.Id, status = row.Status });
    }
}
