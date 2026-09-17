namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class KnowledgeEndpoints
{
    public static void MapKnowledgeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/knowledge")
            .WithTags("Knowledge");

        group.MapGet("/compounds", GetAllCompounds)
            .WithName("GetAllCompounds");

        group.MapGet("/compounds/{name}", GetCompound)
            .WithName("GetCompound");

        group.MapPost("/overlap-check", CheckOverlap)
            .WithName("CheckOverlap");

        group.MapPost("/interaction-check", CheckInteractions)
            .WithName("CheckInteractions");
    }

    private static async Task<IResult> GetAllCompounds(IKnowledgeService knowledgeService, CancellationToken ct)
    {
        var compounds = await knowledgeService.GetAllCompoundsAsync(ct);
        return Results.Ok(compounds);
    }

    private static async Task<IResult> GetCompound(string name, IKnowledgeService knowledgeService, CancellationToken ct)
    {
        var compound = await knowledgeService.GetCompoundAsync(name, ct);
        return compound is null ? Results.NotFound() : Results.Ok(compound);
    }

    private static async Task<IResult> CheckOverlap(
        OverlapCheckRequest request,
        IOverlapService overlapService,
        IFeatureGate featureGate,
        CancellationToken ct)
    {
        var flags = await overlapService.CheckOverlapAsync(request, ct);

        // Owner ruling 2026-09-16, extended 2026-09-17: the public overlap-check tool is the same
        // Observer boundary as every other per-pair interaction surface — flagged pairs and
        // severity are public; the unsourced per-pair reasoning (Description/EvidenceConfidence)
        // requires reviewed_relationship_graph (Operator). This endpoint carries no
        // .RequireAuthorization(), so an anonymous caller must fail closed to the reduced shape,
        // exactly like an authenticated Observer.
        var hasReasoningAccess = await InteractionIntelligenceProjection.HasReasoningAccessAsync(featureGate, ct);
        return Results.Ok(new { overlaps = InteractionIntelligenceProjection.ProjectFlags(flags, hasReasoningAccess) });
    }

    private static async Task<IResult> CheckInteractions(
        OverlapCheckRequest request,
        IInteractionIntelligenceService interactionIntelligenceService,
        CancellationToken ct)
    {
        var result = await interactionIntelligenceService.EvaluatePublicByNamesAsync(request.CompoundNames, ct);
        return Results.Ok(result);
    }
}
