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

    private static async Task<IResult> CheckOverlap(OverlapCheckRequest request, IOverlapService overlapService, CancellationToken ct)
    {
        var flags = await overlapService.CheckOverlapAsync(request, ct);
        return Results.Ok(new { overlaps = flags });
    }

    private static async Task<IResult> CheckInteractions(
        OverlapCheckRequest request,
        IInteractionIntelligenceService interactionIntelligenceService,
        IFeatureGate featureGate,
        CancellationToken ct)
    {
        var result = await interactionIntelligenceService.EvaluatePublicByNamesAsync(request.CompoundNames, ct);

        // Owner ruling 2026-09-16, extended 2026-09-16: "The public view should definitely be the
        // same as observed [Observer]." Routed through the single InteractionIntelligenceProjection
        // point (same one #369 introduced for the protocol/current-stack-intelligence surfaces) —
        // no new gate mechanism. An anonymous caller has no current-user context, so
        // HasReasoningAccessAsync fails closed to the reduced (pair + severity only) shape. An
        // authenticated caller holding reviewed_relationship_graph gets the full shape from this
        // same endpoint.
        var hasReasoningAccess = await InteractionIntelligenceProjection.HasReasoningAccessAsync(featureGate, ct);
        return Results.Ok(InteractionIntelligenceProjection.Project(result, hasReasoningAccess));
    }
}
