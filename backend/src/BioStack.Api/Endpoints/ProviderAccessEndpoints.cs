namespace BioStack.Api.Endpoints;

using System.Net.Mail;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderAccessRequestContract = BioStack.Contracts.Requests.ProviderAccessRequest;
using UpdateProviderAccessRequestContract = BioStack.Contracts.Requests.UpdateProviderAccessRequest;

public static class ProviderAccessEndpoints
{
    private const string ConsentVersion = "provider-access-v1";
    private static readonly HashSet<string> AllowedStatuses =
        ["pending", "contacted", "qualified", "pilot", "closed"];

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

        return Results.Ok(entities.Select(ToReview).ToArray());
    }

    private static async Task<IResult> UpdateRequest(
        Guid requestId,
        [FromBody] UpdateProviderAccessRequestContract request,
        BioStackDbContext db,
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

        entity.Status = status;
        entity.Owner = owner;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToReview(entity));
    }

    private static ProviderAccessConfirmationResponse CreateAcknowledgement()
        => new(Guid.NewGuid(), "pending", DateTime.UtcNow);

    private static ProviderAccessReviewResponse ToReview(ProviderAccessRequest entity)
        => new(
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
            entity.UpdatedAtUtc);
}
