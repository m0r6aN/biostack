namespace BioStack.Api.Endpoints;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BioStack.Api.Security;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

public sealed record ClaimCheckLogRequest(string Query, string? MatchedClaimId, bool DoseIntent);
public sealed record ClaimSubmissionRequest(string CompoundSlug, string Claim, string SeenOn, string? Experience, string? Theme, string? Website);

/// <summary>
/// Growth Phase 7 (03 §7): the claim checker matches only the curated library (client side,
/// from published records); this API only logs hashed queries and takes "Submit a claim you
/// saw" submissions. Nothing here produces a verdict.
/// </summary>
public static partial class ClaimCheckEndpoints
{
    public const int MaxClaimLength = 280;
    public const int MaxThemeLength = 140;
    public static readonly HashSet<string> SeenOnCategories = ["instagram", "tiktok", "youtube", "x", "reddit", "forum", "friend", "other"];
    public static readonly HashSet<string> Experiences = ["side-effect", "benefit", "nothing"];

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,98}$")]
    private static partial Regex Slug();
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,199}$")]
    private static partial Regex ClaimId();

    public static void MapClaimCheckEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/check").WithTags("ClaimCheck");
        group.MapPost("/log", Log).RequireRateLimiting("lead-capture").WithName("LogClaimCheck");
        group.MapPost("/submissions", Submit).RequireRateLimiting("lead-capture").WithName("SubmitClaim");
    }

    public static string HashQuery(string query)
    {
        var normalized = Regex.Replace(query.Trim().ToLowerInvariant(), @"\s+", " ");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }

    private static async Task<IResult> Log(ClaimCheckLogRequest request, BioStackDbContext db, CancellationToken ct)
    {
        var q = request.Query?.Trim() ?? string.Empty;
        if (q.Length is < 2 or > 500) return Results.BadRequest(new { error = "query must be 2-500 characters." });
        var matched = string.IsNullOrWhiteSpace(request.MatchedClaimId) ? null : request.MatchedClaimId.Trim();
        if (matched is not null && !ClaimId().IsMatch(matched)) return Results.BadRequest(new { error = "invalid claim id." });
        db.ClaimCheckQueries.Add(new ClaimCheckQuery { Id = Guid.NewGuid(), QueryHash = HashQuery(q), MatchedClaimId = matched, DoseIntent = request.DoseIntent, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Submit(
        ClaimSubmissionRequest request, HttpContext http, [FromServices] ICurrentUserAccessor user,
        [FromServices] IConfiguration config, BioStackDbContext db, CancellationToken ct)
    {
        // Honeypot: bots fill every field; people never see this one.
        if (!string.IsNullOrWhiteSpace(request.Website)) return Results.Accepted(value: new { status = "queued" });

        var slug = request.CompoundSlug?.Trim().ToLowerInvariant() ?? string.Empty;
        var seenOn = request.SeenOn?.Trim().ToLowerInvariant() ?? string.Empty;
        var experience = string.IsNullOrWhiteSpace(request.Experience) ? null : request.Experience.Trim().ToLowerInvariant();
        if (!Slug().IsMatch(slug) || !SeenOnCategories.Contains(seenOn) || (experience is not null && !Experiences.Contains(experience)))
            return Results.BadRequest(new { error = "Pick a compound, where you saw it, and (optionally) what you experienced." });

        var (claim, claimRedacted) = ClaimSubmissionSanitizer.Clean(request.Claim, MaxClaimLength);
        if (claim.Length < 3) return Results.BadRequest(new { error = "Tell us the claim in your own words." });
        var (theme, themeRedacted) = experience is null ? (null, false) : ClaimSubmissionSanitizer.Clean(request.Theme, MaxThemeLength);
        var emergency = ClaimSubmissionSanitizer.IsEmergency(request.Claim, request.Theme);

        db.ClaimSubmissions.Add(new ClaimSubmission
        {
            Id = Guid.NewGuid(), CompoundSlug = slug, ClaimText = claim, SeenOn = seenOn, Experience = experience,
            ThemeText = string.IsNullOrWhiteSpace(theme) ? null : theme, Redacted = claimRedacted || themeRedacted,
            Emergency = emergency, SubmitterKey = SubmitterKey(http, user, config), CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return Results.Accepted(value: new
        {
            status = "queued",
            redacted = claimRedacted || themeRedacted,
            notice = claimRedacted || themeRedacted ? ClaimSubmissionSanitizer.RedactedNotice : null,
            safetyMessage = emergency ? ClaimSubmissionSanitizer.SafetyMessage : null,
        });
    }

    /// <summary>Salted hash: signed-in user id, else client address + UTC day. Never stored raw.</summary>
    private static string SubmitterKey(HttpContext http, ICurrentUserAccessor user, IConfiguration config)
    {
        string basis;
        try { basis = $"u:{user.GetCurrentUserId()}"; }
        catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException)
        {
            // Same proxy-aware client address the rate limiter uses; behind the Container Apps
            // ingress the raw socket address is the proxy, which would merge every anonymous submitter.
            basis = $"a:{ClientIpResolver.PartitionKey(http, config)}:{DateTime.UtcNow:yyyy-MM-dd}";
        }
        var salt = ClaimSubmissionSaltConfiguration.Resolve(config);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}|{basis}"))).ToLowerInvariant()[..32];
    }
}
