namespace BioStack.Api.Endpoints;

using BioStack.Api.Auth;
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

        // One call for the Everything I'm Taking tab: the user's items with catalog link status, read-time
        // "now in BioStack" offers and the server-derived cap count.
        app.MapGet("/api/v1/profiles/{profileId}/taking", GetTaking)
            .WithTags("Compounds")
            .RequireAuthorization()
            .WithName("GetTaking");

        group.MapPost("/", CreateCompound)
            .WithName("CreateCompound")
            .RequireConsent();

        group.MapPut("/{id}", UpdateCompound)
            .WithName("UpdateCompound")
            .RequireConsent();

        group.MapDelete("/{id}", DeleteCompound)
            .WithName("DeleteCompound")
            .RequireConsent();
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

    private static async Task<IResult> GetTaking(Guid profileId, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await compoundService.GetTakingAsync(profileId, ct));
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
        catch (DoseValidationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [ex.Field] = [ex.Message],
            });
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = [ex.Message],
            });
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
        catch (DoseValidationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [ex.Field] = [ex.Message],
            });
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = [ex.Message],
            });
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
