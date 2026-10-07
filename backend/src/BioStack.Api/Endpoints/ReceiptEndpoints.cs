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

        group.MapPost("/chain/checkpoints", CreateCheckpoint)
            .WithName("CreateSpineChainCheckpoint");

        group.MapGet("/chain/checkpoints", ListCheckpoints)
            .WithName("ListSpineChainCheckpoints");

        group.MapGet("/chain/checkpoints/latest/export", ExportLatestCheckpoint)
            .WithName("ExportLatestSpineChainCheckpoint");

        group.MapGet("/chain/checkpoints/verify", VerifyCheckpoint)
            .WithName("VerifySpineChainCheckpoint");
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

    /// <summary>F3+: snapshot and optionally HMAC-sign the current chain head.</summary>
    private static async Task<IResult> CreateCheckpoint(
        ISpineCheckpointService checkpoints,
        ClaimsPrincipal principal,
        CancellationToken ct,
        string? note = null)
    {
        if (!IsAdmin(principal))
            return Results.NotFound();

        try
        {
            var checkpoint = await checkpoints.CreateCheckpointAsync(note, ct);
            return Results.Ok(MapCheckpoint(checkpoint));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { code = "empty_spine", message = ex.Message });
        }
    }

    private static async Task<IResult> ListCheckpoints(
        ISpineCheckpointService checkpoints,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        if (!IsAdmin(principal))
            return Results.NotFound();

        var items = await checkpoints.ListAsync(ct: ct);
        return Results.Ok(items.Select(MapCheckpoint));
    }

    private static async Task<IResult> ExportLatestCheckpoint(
        ISpineCheckpointService checkpoints,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        if (!IsAdmin(principal))
            return Results.NotFound();

        var json = await checkpoints.ExportLatestManifestJsonAsync(ct);
        if (json is null)
            return Results.NotFound(new { code = "no_checkpoint", message = "No checkpoint recorded." });

        return Results.Content(json, "application/json");
    }

    private static async Task<IResult> VerifyCheckpoint(
        ISpineCheckpointService checkpoints,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        if (!IsAdmin(principal))
            return Results.NotFound();

        var result = await checkpoints.VerifyLatestAsync(ct);
        return Results.Ok(new
        {
            isFullyValid = result.IsFullyValid,
            chainIntact = result.ChainIntact,
            checkpointPresent = result.CheckpointPresent,
            headMatchesCheckpoint = result.HeadMatchesCheckpoint,
            signatureValid = result.SignatureValid,
            externallyAnchored = result.ExternallyAnchored,
            chainEntriesVerified = result.ChainEntriesVerified,
            checkpointSequenceNumber = result.CheckpointSequenceNumber,
            checkpointHeadEntryHash = result.CheckpointHeadEntryHash,
            reason = result.Reason,
        });
    }

    private static object MapCheckpoint(SpineChainCheckpoint c) => new
    {
        id = c.Id,
        sequenceNumber = c.SequenceNumber,
        headEntryHash = c.HeadEntryHash,
        checkpointedAtUtc = c.CheckpointedAtUtc,
        source = c.Source,
        signatureAlgorithm = c.SignatureAlgorithm,
        signature = c.Signature,
        note = c.Note,
    };

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
