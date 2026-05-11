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

        group.MapGet("", GetReceiptsBySubject)
            .WithName("GetReceiptsBySubject");
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

    private static async Task<IResult> GetReceiptsBySubject(
        string subject,
        ISpineRepository spine,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Results.BadRequest("subject query parameter is required");

        var entries = await spine.GetBySubjectAsync(subject, ct);
        return Results.Ok(entries.Select(MapToResponse));
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
