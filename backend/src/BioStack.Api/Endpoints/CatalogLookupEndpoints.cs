namespace BioStack.Api.Endpoints;

using BioStack.Api.Auth;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;

/// <summary>
/// "Is this already in BioStack?" for a name the person typed. Owner-only. Creating, linking and declining are writes
/// (behind the consent gate); reading a lookup is not, so a person can always see what was found.
/// </summary>
public static class CatalogLookupEndpoints
{
    public static void MapCatalogLookupEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/profiles/{profileId:guid}/catalog-lookups", Create)
            .WithTags("CatalogLookups")
            .WithName("CreateCatalogLookup")
            .RequireAuthorization()
            .RequireConsent()
            .RequireRateLimiting("catalog-lookup-create");

        app.MapGet("/api/v1/profiles/{profileId:guid}/catalog-lookups", ListOpen)
            .WithTags("CatalogLookups")
            .WithName("ListOpenCatalogLookups")
            .RequireAuthorization();

        var group = app.MapGroup("/api/v1/catalog-lookups")
            .WithTags("CatalogLookups")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", Get).WithName("GetCatalogLookup");
        group.MapPost("/{id:guid}/link", Link).WithName("LinkCatalogLookup").RequireConsent();
        group.MapPost("/{id:guid}/decline", Decline).WithName("DeclineCatalogLookup").RequireConsent();
    }

    private static async Task<IResult> Create(Guid profileId, CreateCatalogLookupRequest request, ICatalogLookupService service, CancellationToken ct)
    {
        if (!CatalogLookupName.TryClean(request.Name, out var name, out var error))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = [error] });

        try
        {
            var lookup = await service.CreateAsync(profileId, name, request.CompoundId, ct);
            return Results.Accepted($"/api/v1/catalog-lookups/{lookup.Id}", lookup);
        }
        catch (CatalogLookupValidationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = [ex.Message] });
        }
        catch (CatalogLookupLimitException ex)
        {
            return Results.Json(new { code = "catalog_lookup_limit", message = ex.Message }, statusCode: StatusCodes.Status429TooManyRequests);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ListOpen(Guid profileId, ICatalogLookupService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.ListOpenAsync(profileId, ct));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> Get(Guid id, ICatalogLookupService service, CancellationToken ct)
    {
        var lookup = await service.GetAsync(id, ct);
        return lookup is null ? Results.NotFound() : Results.Ok(lookup);
    }

    private static async Task<IResult> Link(Guid id, LinkCatalogLookupRequest request, ICatalogLookupService service, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["slug"] = ["Choose one of the offered matches."] });

        try
        {
            var lookup = await service.LinkAsync(id, request.Slug, ct);
            return lookup is null ? Results.NotFound() : Results.Ok(lookup);
        }
        catch (CatalogLookupConflictException ex)
        {
            return Results.Json(new { code = ex.Code, message = ex.Message }, statusCode: StatusCodes.Status409Conflict);
        }
        catch (DoseValidationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { [ex.Field] = [ex.Message] });
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> Decline(Guid id, ICatalogLookupService service, CancellationToken ct)
    {
        try
        {
            var lookup = await service.DeclineAsync(id, ct);
            return lookup is null ? Results.NotFound() : Results.Ok(lookup);
        }
        catch (CatalogLookupConflictException ex)
        {
            return Results.Json(new { code = ex.Code, message = ex.Message }, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
