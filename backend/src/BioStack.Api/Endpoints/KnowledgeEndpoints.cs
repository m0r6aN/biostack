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
        CancellationToken ct)
    {
        var result = await interactionIntelligenceService.EvaluateByNamesAsync(request.CompoundNames, ct);
        return Results.Ok(result);
    }
}
