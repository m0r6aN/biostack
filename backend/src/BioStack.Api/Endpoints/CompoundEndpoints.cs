namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public static class CompoundEndpoints
{
    private static IResult ProductGate(FeatureLimitExceededException ex) =>
        Results.Json(
            new ProductErrorResponse(ex.Code, ex.Message, ex.Tier.ToString(), ex.Limit, true),
            statusCode: StatusCodes.Status402PaymentRequired);

    public static void MapCompoundEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/compounds")
            .WithTags("Compounds")
            .RequireAuthorization();

        group.MapGet("/", GetCompounds)
            .WithName("GetCompounds");

        group.MapPost("/", CreateCompound)
            .WithName("CreateCompound");

        group.MapPut("/{id}", UpdateCompound)
            .WithName("UpdateCompound");

        group.MapDelete("/{id}", DeleteCompound)
            .WithName("DeleteCompound");
    }

    private static async Task<IResult> GetCompounds(Guid profileId, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            var compounds = await compoundService.GetCompoundsByProfileAsync(profileId, ct);
            return Results.Ok(compounds);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> CreateCompound(Guid profileId, CreateCompoundRequest request, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            var compound = await compoundService.CreateCompoundAsync(profileId, request, ct);
            return Results.Created($"/api/v1/compounds/{compound.Id}", compound);
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> UpdateCompound(Guid profileId, Guid id, UpdateCompoundRequest request, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            var compound = await compoundService.UpdateCompoundAsync(profileId, id, request, ct);
            return Results.Ok(compound);
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> DeleteCompound(Guid profileId, Guid id, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            await compoundService.DeleteCompoundAsync(profileId, id, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
