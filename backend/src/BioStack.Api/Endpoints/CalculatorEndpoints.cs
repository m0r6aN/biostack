namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class CalculatorEndpoints
{
    public static void MapCalculatorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/calculators")
            .WithTags("Calculators");

        group.MapPost("/reconstitution", CalculateReconstitution)
            .WithName("CalculateReconstitution");

        group.MapPost("/volume", CalculateVolume)
            .WithName("CalculateVolume");

        group.MapPost("/conversion", CalculateConversion)
            .WithName("CalculateConversion");

        group.MapGet("/profiles/{profileId}/results", GetSavedResults)
            .WithName("GetSavedCalculatorResults");

        group.MapPost("/profiles/{profileId}/results", SaveResult)
            .WithName("SaveCalculatorResult");

        group.MapPost("/profiles/{profileId}/results/{resultId}/attach", AttachResult)
            .WithName("AttachCalculatorResult");
    }

    private static IResult CalculateReconstitution(ReconstitutionRequest request, ICalculatorService calculatorService)
    {
        try
        {
            var result = calculatorService.CalculateReconstitution(request);
            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult CalculateVolume(VolumeRequest request, ICalculatorService calculatorService)
    {
        try
        {
            var result = calculatorService.CalculateVolume(request);
            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult CalculateConversion(ConversionRequest request, ICalculatorService calculatorService)
    {
        try
        {
            var result = calculatorService.CalculateConversion(request);
            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetSavedResults(Guid profileId, ICalculatorResultRecordService resultService, CancellationToken ct)
    {
        try
        {
            var results = await resultService.GetByProfileAsync(profileId, ct);
            return Results.Ok(results);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> SaveResult(Guid profileId, SaveCalculatorResultRequest request, ICalculatorResultRecordService resultService, CancellationToken ct)
    {
        try
        {
            var result = await resultService.SaveAsync(profileId, request, ct);
            return Results.Created($"/api/v1/calculators/profiles/{profileId}/results/{result.Id}", result);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> AttachResult(Guid profileId, Guid resultId, AttachCalculatorResultRequest request, ICalculatorResultRecordService resultService, CancellationToken ct)
    {
        try
        {
            var result = await resultService.AttachAsync(profileId, resultId, request, ct);
            return Results.Ok(result);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
