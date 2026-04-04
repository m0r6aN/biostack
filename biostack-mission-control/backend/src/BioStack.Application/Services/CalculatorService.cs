namespace BioStack.Application.Services;

using BioStack.Domain.ValueObjects;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class CalculatorService : ICalculatorService
{
    private const string Disclaimer = "This is a mathematical calculation only. Not medical advice.";

    public CalculatorResultResponse CalculateReconstitution(ReconstitutionRequest request)
    {
        if (request.PeptideAmountMg <= 0)
            throw new ArgumentException("Peptide amount must be greater than 0");
        if (request.DiluentVolumeMl <= 0)
            throw new ArgumentException("Diluent volume must be greater than 0");

        decimal concentrationMcgPerMl = (request.PeptideAmountMg * 1000m) / request.DiluentVolumeMl;

        var result = CalculatorResult.Create(
            request.PeptideAmountMg,
            concentrationMcgPerMl,
            "mcg/mL",
            "Concentration = (Peptide mg * 1000) / Diluent mL",
            Disclaimer
        );

        return MapToResponse(result);
    }

    public CalculatorResultResponse CalculateVolume(VolumeRequest request)
    {
        if (request.DesiredDoseMcg <= 0)
            throw new ArgumentException("Desired dose must be greater than 0");
        if (request.ConcentrationMcgPerMl <= 0)
            throw new ArgumentException("Concentration must be greater than 0");

        decimal volumeMl = request.DesiredDoseMcg / request.ConcentrationMcgPerMl;

        var result = CalculatorResult.Create(
            request.DesiredDoseMcg,
            volumeMl,
            "mL",
            "Volume = Desired Dose / Concentration",
            Disclaimer
        );

        return MapToResponse(result);
    }

    public CalculatorResultResponse CalculateConversion(ConversionRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        decimal factor = request.ConversionFactor ?? ResolveConversionFactor(request.FromUnit, request.ToUnit);

        if (factor <= 0)
            throw new ArgumentException("Conversion factor must be greater than 0");

        decimal convertedAmount = request.Amount * factor;

        var result = CalculatorResult.Create(
            request.Amount,
            convertedAmount,
            request.ToUnit,
            $"{request.FromUnit} to {request.ToUnit} = {request.FromUnit} * {factor}",
            Disclaimer
        );

        return MapToResponse(result);
    }

    private static decimal ResolveConversionFactor(string from, string to)
    {
        var key = $"{from.ToLowerInvariant()}->{to.ToLowerInvariant()}";
        return key switch
        {
            "mg->mcg" => 1000m,
            "mcg->mg" => 0.001m,
            "g->mg" => 1000m,
            "mg->g" => 0.001m,
            "g->mcg" => 1_000_000m,
            "mcg->g" => 0.000001m,
            _ => throw new ArgumentException($"No known conversion factor for {from} to {to}. Provide a conversionFactor.")
        };
    }

    private static CalculatorResultResponse MapToResponse(CalculatorResult result)
    {
        return new CalculatorResultResponse(
            result.Input,
            result.Output,
            result.Unit,
            result.Formula,
            result.Disclaimer
        );
    }
}

public interface ICalculatorService
{
    CalculatorResultResponse CalculateReconstitution(ReconstitutionRequest request);
    CalculatorResultResponse CalculateVolume(VolumeRequest request);
    CalculatorResultResponse CalculateConversion(ConversionRequest request);
}
