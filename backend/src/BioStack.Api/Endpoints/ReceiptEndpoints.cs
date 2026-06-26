namespace BioStack.Api.Endpoints;

using System.Text.Json;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;

public static class ReceiptEndpoints
{
    public static void MapReceiptEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/receipts")
            .WithTags("Receipts");

        group.MapGet("/{uri}", GetReceiptByUri)
            .WithName("GetReceiptByUri");

        group.MapGet("", GetReceipts)
            .WithName("GetReceipts");
    }

    private static async Task<IResult> GetReceiptByUri(
        string uri,
        ISpineRepository spine,
        CancellationToken ct)
    {
        var decoded = Uri.UnescapeDataString(uri);
        var entry = await spine.GetByReceiptUriAsync(decoded, ct);

        if (entry is null)
            return Results.NotFound();

        return Results.Ok(MapToResponse(entry));
    }

    private static async Task<IResult> GetReceipts(
        string? subject,
        string? actor,
        ISpineRepository spine,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(subject))
        {
            var entries = await spine.GetBySubjectAsync(subject, ct);
            return Results.Ok(entries.Select(MapToResponse));
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            var entries = await spine.GetByActorAsync(actor, ct);
            return Results.Ok(entries.Select(MapToResponse));
        }

        return Results.BadRequest("Either 'subject' or 'actor' query parameter is required");
    }

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
