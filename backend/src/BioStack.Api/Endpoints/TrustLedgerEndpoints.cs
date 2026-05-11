namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;

public static class TrustLedgerEndpoints
{
    public static void MapTrustLedgerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/knowledge")
            .WithTags("TrustLedger");

        group.MapGet("/compounds/{slug}/trust-ledger", GetTrustLedger)
            .WithName("GetCompoundTrustLedger");
    }

    private static async Task<IResult> GetTrustLedger(
        string slug,
        ITrustLedgerService trustLedgerService,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Results.BadRequest("slug is required");

        var result = await trustLedgerService.GetTrustLedgerAsync(slug, ct);

        if (result is null)
            return Results.NotFound();

        // Review-gated: return reduced payload without claims
        if (result.Status == "review-gated" && result.PromotionBlockers.Count > 0)
        {
            return Results.Json(
                new
                {
                    slug = result.Slug,
                    regulatoryBoundary = result.RegulatoryBoundary,
                    qualityFlags = result.QualityFlags,
                    status = "review-gated",
                    reason = "This compound is under review and claims are not yet public."
                },
                statusCode: 200);
        }

        return Results.Ok(result);
    }
}
