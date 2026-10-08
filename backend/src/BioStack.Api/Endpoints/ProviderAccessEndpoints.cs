namespace BioStack.Api.Endpoints;

using System.Net.Mail;
using System.Security.Claims;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProviderAccessRequestContract = BioStack.Contracts.Requests.ProviderAccessRequest;
using UpdateProviderAccessRequestContract = BioStack.Contracts.Requests.UpdateProviderAccessRequest;

public static class ProviderAccessEndpoints
{
    private const string ConsentVersion = "provider-access-v1";
    private static readonly HashSet<string> AllowedStatuses =
        ["pending", "contacted", "qualified", "pilot", "closed"];

    // PR-PROV-001 SG4/R6: configuration keys for admin-operations hardening. Both have safe,
    // documented defaults and are overridable via ProviderAccess__SlaDays / ProviderAccess__RetentionDays
    // (no appsettings.json entry is required — reading via IConfiguration directly keeps this
    // parcel inside its Allowed Files, matching the existing Stripe/Redis/Kompress inline-read
    // pattern already used in Program.cs).
    private const int DefaultSlaDays = 5;
    private const int DefaultRetentionDays = 365;

    public static void MapProviderAccessEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/provider-access/requests", CreateRequest)
            .WithTags("Provider Access")
            .AllowAnonymous()
            .RequireRateLimiting("provider-access")
            .WithName("CreateProviderAccessRequest");

        var admin = app.MapGroup("/api/v1/admin/provider-access/requests")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        admin.MapGet("/", ListRequests)
            .WithName("ListProviderAccessRequests");

        admin.MapPatch("/{requestId:guid}", UpdateRequest)
            .WithName("UpdateProviderAccessRequest");

        // PR-PROV-001 SG4/R6: manually-invoked only. Never scheduled/triggered automatically by
        // this parcel — recurring execution is GATED-2, named to the release owner.
        admin.MapPost("/retention-sweep", RunRetentionSweep)
            .WithName("RunProviderAccessRetentionSweep");
    }

    private static async Task<IResult> CreateRequest(
        [FromBody] ProviderAccessRequestContract request,
        BioStackDbContext db,
        CancellationToken ct)
    {
        // Honeypot submissions receive the same response shape but are not persisted.
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return Results.Accepted(value: CreateAcknowledgement());
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var name = request.Name.Trim();
        var organization = request.Organization.Trim();
        var role = request.Role.Trim();

        if (!request.Consent)
        {
            return Results.BadRequest(new { error = "Consent is required to submit a provider access request." });
        }

        if (!MailAddress.TryCreate(email, out _) || email.Length > 255)
        {
            return Results.BadRequest(new { error = "Enter a valid email address." });
        }

        if (name.Length is < 2 or > 160 || organization.Length is < 2 or > 200 || role.Length is < 2 or > 120)
        {
            return Results.BadRequest(new { error = "Name, organization, and role are required and must fit the indicated fields." });
        }

        var existing = await db.ProviderAccessRequests
            .FirstOrDefaultAsync(item => item.Email == email, ct);

        if (existing is not null)
        {
            return Results.Accepted(value: CreateAcknowledgement());
        }

        var now = DateTime.UtcNow;
        var entity = new ProviderAccessRequest
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            Organization = organization,
            Role = role,
            Status = "pending",
            ConsentVersion = ConsentVersion,
            ConsentRecordedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.ProviderAccessRequests.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A concurrent request for the same normalized email won the unique-index race.
            db.Entry(entity).State = EntityState.Detached;
            if (!await db.ProviderAccessRequests.AsNoTracking().AnyAsync(item => item.Email == email, ct))
                throw;
        }

        return Results.Accepted(value: CreateAcknowledgement());
    }

    private static async Task<IResult> ListRequests(
        [FromQuery] string? status,
        [FromQuery] string? owner,
        BioStackDbContext db,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var query = db.ProviderAccessRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            if (!AllowedStatuses.Contains(normalizedStatus))
            {
                return Results.BadRequest(new { error = "Unknown provider request status." });
            }

            query = query.Where(item => item.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(owner))
        {
            var normalizedOwner = owner.Trim();
            query = query.Where(item => item.Owner == normalizedOwner);
        }

        var entities = await query
            .OrderBy(item => item.Status == "pending" ? 0 : 1)
            .ThenByDescending(item => item.CreatedAtUtc)
            .ToListAsync(ct);

        var slaDays = ReadSlaDays(configuration);
        var now = DateTime.UtcNow;
        return Results.Ok(entities.Select(item => ToReview(item, now, slaDays)).ToArray());
    }

    private static async Task<IResult> UpdateRequest(
        Guid requestId,
        [FromBody] UpdateProviderAccessRequestContract request,
        BioStackDbContext db,
        IConfiguration configuration,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var status = request.Status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(status))
        {
            return Results.BadRequest(new { error = "Unknown provider request status." });
        }

        var owner = string.IsNullOrWhiteSpace(request.Owner) ? null : request.Owner.Trim();
        if (owner is { Length: > 160 })
        {
            return Results.BadRequest(new { error = "Owner must be 160 characters or fewer." });
        }

        var entity = await db.ProviderAccessRequests.FirstOrDefaultAsync(item => item.Id == requestId, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        var fromStatus = entity.Status;
        var fromOwner = entity.Owner;
        var statusChanged = !string.Equals(fromStatus, status, StringComparison.Ordinal);
        var ownerChanged = !string.Equals(fromOwner, owner, StringComparison.Ordinal);

        entity.Status = status;
        entity.Owner = owner;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        // PR-PROV-001 SG4/R6: append-only audit row on every actual status/owner change. A
        // genuine no-op PATCH (same status, same owner) writes no audit row.
        if (statusChanged || ownerChanged)
        {
            var actorId = CurrentUserId(principal) ?? Guid.Empty;
            db.ProviderAccessAuditEntries.Add(new ProviderAccessAuditEntry
            {
                Id = Guid.NewGuid(),
                RequestId = entity.Id,
                ActorId = actorId,
                FromStatus = fromStatus,
                ToStatus = status,
                FromOwner = fromOwner,
                ToOwner = owner,
                OccurredAtUtc = entity.UpdatedAtUtc,
            });
        }

        await db.SaveChangesAsync(ct);

        var slaDays = ReadSlaDays(configuration);
        return Results.Ok(ToReview(entity, DateTime.UtcNow, slaDays));
    }

    /// <summary>
    /// PR-PROV-001 SG4/R6: manually-invoked admin retention sweep. Anonymizes <c>closed</c>
    /// requests older than the configured retention window. Never scheduled automatically by
    /// this parcel (GATED-2).
    /// </summary>
    private static async Task<IResult> RunRetentionSweep(
        BioStackDbContext db,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var retentionDays = ReadRetentionDays(configuration);
        var now = DateTime.UtcNow;

        var closedRequests = await db.ProviderAccessRequests
            .Where(item => item.Status == "closed")
            .ToListAsync(ct);

        var eligible = closedRequests
            .Where(item => item.IsEligibleForRetentionAnonymization(now, retentionDays))
            .ToList();

        foreach (var item in eligible)
        {
            item.AnonymizeForRetention();
        }

        if (eligible.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new ProviderAccessRetentionSweepResponse(
            EligibleCount: eligible.Count,
            AnonymizedCount: eligible.Count,
            RetentionDays: retentionDays,
            SweepPerformedAtUtc: now));
    }

    private static int ReadSlaDays(IConfiguration configuration)
        => configuration.GetValue<int?>("ProviderAccess:SlaDays") ?? DefaultSlaDays;

    private static int ReadRetentionDays(IConfiguration configuration)
        => configuration.GetValue<int?>("ProviderAccess:RetentionDays") ?? DefaultRetentionDays;

    private static Guid? CurrentUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static ProviderAccessConfirmationResponse CreateAcknowledgement()
        => new(Guid.NewGuid(), "pending", DateTime.UtcNow);

    private static ProviderAccessReviewResponse ToReview(ProviderAccessRequest entity, DateTime nowUtc, int slaDays)
    {
        var daysOpen = Math.Max(0, (int)(nowUtc - entity.CreatedAtUtc).TotalDays);
        var isOverdue = string.Equals(entity.Status, "pending", StringComparison.Ordinal) && daysOpen > slaDays;

        return new ProviderAccessReviewResponse(
            entity.Id,
            entity.Email,
            entity.Name,
            entity.Organization,
            entity.Role,
            entity.Status,
            entity.Owner,
            entity.ConsentVersion,
            entity.ConsentRecordedAtUtc,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            daysOpen,
            isOverdue);
    }
}
