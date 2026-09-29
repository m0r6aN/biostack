namespace BioStack.Api.Endpoints;

using BioStack.Api.Auth;
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
        var compounds = await compoundService.GetCompoundsByProfileAsync(profileId, ct);
        return Results.Ok(compounds);
    }

    private static async Task<IResult> CreateCompound(
        Guid profileId,
        CreateCompoundRequest request,
        ICompoundService compoundService,
        IEntitlementService entitlementService,
        HttpContext http,
        CancellationToken ct)
    {
        try
        {
            var entitlements = await entitlementService.GetUserEntitlementsAsync(HttpUser.GetUserId(http.User), ct);
            var existingCompounds = await compoundService.GetCompoundsByProfileAsync(profileId, ct);
            if (!entitlements.IsPro && existingCompounds.Count() >= entitlements.Limits.MaxCompounds)
            {
                return Results.Problem(
                    title: "Compound limit reached",
                    detail: "Free plans can track 2 compounds. Upgrade to Pro for unlimited stack intelligence.",
                    statusCode: StatusCodes.Status402PaymentRequired);
            }

            var compound = await compoundService.CreateCompoundAsync(profileId, request, ct);
            return Results.Created($"/api/v1/compounds/{compound.Id}", compound);
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
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

    private static async Task<IResult> UpdateCompound(Guid id, UpdateCompoundRequest request, ICompoundService compoundService, CancellationToken ct)
    {
        try
        {
            var compound = await compoundService.UpdateCompoundAsync(id, request, ct);
            return Results.Ok(compound);
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
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
}
