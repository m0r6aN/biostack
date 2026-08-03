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

        group.MapGet("/chain/verify", VerifyChain)
            .WithName("VerifyReceiptChain");
    }

    /// <summary>
    /// F3: walk the Governed Spine and confirm every entry rehashes and links to its predecessor.
    /// Admin-only — it reports ledger-wide state, and the earliest break location is operational
    /// detail an ordinary caller has no need for.
    /// </summary>
    private static async Task<IResult> VerifyChain(
        ISpineRepository spine,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        if (!IsAdmin(principal))
            return Results.NotFound();

        var result = await spine.VerifyChainAsync(ct);

        return Results.Ok(new
        {
            isIntact = result.IsIntact,
            entriesVerified = result.EntriesVerified,
            firstBrokenReceiptUri = result.FirstBrokenReceiptUri,
            reason = result.Reason,
        });
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
            // F3 chain position — lets a holder verify this receipt sits where it claims to.
            sequenceNumber = e.SequenceNumber,
            previousEntryHash = e.PreviousEntryHash,
            entryHash = e.EntryHash,
        };
    }
}
