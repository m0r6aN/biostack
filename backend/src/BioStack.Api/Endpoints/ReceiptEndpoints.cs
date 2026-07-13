namespace BioStack.Api.Endpoints;

using System.Security.Claims;
using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Keon;

public static class ReceiptEndpoints
{
    public static void MapReceiptEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/receipts")
            .WithTags("Receipts")
            .RequireAuthorization();

        group.MapGet("/{uri}", GetReceiptByUri)
            .WithName("GetReceiptByUri");

        group.MapGet("", GetReceipts)
            .WithName("GetReceipts");
    }

    private static async Task<IResult> GetReceiptByUri(
        string uri,
        ISpineRepository spine,
        ICurrentUserAccessor currentUser,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var decoded = Uri.UnescapeDataString(uri);
        var entry = await spine.GetByReceiptUriAsync(decoded, ct);

        if (entry is null)
            return Results.NotFound();

        if (!IsAdmin(principal) && !IsCurrentUserReceipt(entry, currentUser))
            return Results.NotFound();

        return Results.Ok(MapToResponse(entry));
    }

    private static async Task<IResult> GetReceipts(
        string? subject,
        string? actor,
        ISpineRepository spine,
        ICurrentUserAccessor currentUser,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var isAdmin = IsAdmin(principal);
        var currentActorId = ReceiptActor.User(currentUser.GetCurrentUserId()).ActorId;

        if (!string.IsNullOrWhiteSpace(subject))
        {
            var entries = await spine.GetBySubjectAsync(subject, ct);
            if (!isAdmin)
                entries = entries.Where(entry => string.Equals(entry.ActorId, currentActorId, StringComparison.Ordinal)).ToList();

            return Results.Ok(entries.Select(MapToResponse));
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            if (!isAdmin && !string.Equals(actor, currentActorId, StringComparison.Ordinal))
                return Results.Forbid();

            var entries = await spine.GetByActorAsync(actor, ct);
            return Results.Ok(entries.Select(MapToResponse));
        }

        return Results.BadRequest("Either 'subject' or 'actor' query parameter is required");
    }

    private static bool IsAdmin(ClaimsPrincipal principal) =>
        principal.HasClaim("role", "1");

    private static bool IsCurrentUserReceipt(SpineEntry entry, ICurrentUserAccessor currentUser) =>
        string.Equals(
            entry.ActorId,
            ReceiptActor.User(currentUser.GetCurrentUserId()).ActorId,
            StringComparison.Ordinal);

    private static object MapToResponse(SpineEntry e)
    {
        IReadOnlyList<string> evidenceRefs;
        try
        {
            evidenceRefs = JsonSerializer.Deserialize<List<string>>(e.EvidenceRefsJson)
                          ?? [];
        }
        catch
        {
            evidenceRefs = [];
        }

        return new
        {
            receiptUri = e.ReceiptUri,
            subjectUri = e.SubjectUri,
            tenantId = e.TenantId,
            actorId = e.ActorId,
            timestampUtc = e.TimestampUtc,
            decision = e.Decision,
            receiptClass = e.ReceiptClass,
            policyHash = new
            {
                value = e.PolicyHashValue,
                version = e.PolicyHashVersion,
            },
            inputHash = e.InputHash,
            evidenceRefs,
            effectStatus = e.EffectStatus,
        };
    }
}
