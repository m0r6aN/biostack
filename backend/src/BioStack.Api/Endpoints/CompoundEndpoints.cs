namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class CompoundEndpoints
{
    public static void MapCompoundEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/compounds")
            .WithTags("Compounds");

        group.MapGet("/", GetCompounds)
            .WithName("GetCompounds");

        group.MapPost("/", CreateCompound)
            .WithName("CreateCompound");

        group.MapPut("/{id}", UpdateCompound)
            .WithName("UpdateCompound");

        group.MapDelete("/{id}", DeleteCompound)
            .WithName("DeleteCompound");

        group.MapGet("/current-stack-intelligence", GetCurrentStackIntelligence)
            .WithName("GetCurrentStackIntelligence");

        app.MapPut("/api/v1/compounds/{id}", UpdateCompound)
            .WithTags("Compounds")
            .WithName("UpdateCompoundLegacy");

        app.MapDelete("/api/v1/compounds/{id}", DeleteCompound)
            .WithTags("Compounds")
            .WithName("DeleteCompoundLegacy");
    }

    private static async Task<IResult> GetCompounds(Guid profileId, ICompoundService compoundService, CancellationToken ct)
    {
        var compounds = await compoundService.GetCompoundsByProfileAsync(profileId, ct);
        return Results.Ok(compounds);
    }

    private static async Task<IResult> CreateCompound(Guid profileId, CreateCompoundRequest request, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            var compound = await compoundService.CreateCompoundAsync(profileId, request, ct);
            return Results.Created($"/api/v1/compounds/{compound.Id}", compound);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> UpdateCompound(Guid id, UpdateCompoundRequest request, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            var compound = await compoundService.UpdateCompoundAsync(id, request, ct);
            return Results.Ok(compound);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> DeleteCompound(Guid id, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            await compoundService.DeleteCompoundAsync(id, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetCurrentStackIntelligence(Guid profileId, ICurrentStackIntelligenceService stackIntelligenceService, CancellationToken ct)
    {
        try
        {
            var intelligence = await stackIntelligenceService.GetCurrentStackAsync(profileId, ct);
            return Results.Ok(intelligence);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
