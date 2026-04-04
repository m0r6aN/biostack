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
}
