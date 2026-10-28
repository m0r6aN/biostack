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

        decimal concentrationMcgPerMl = Checked(() => (request.PeptideAmountMg * 1000m) / request.DiluentVolumeMl);

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

        decimal volumeMl = Checked(() => request.DesiredDoseMcg / request.ConcentrationMcgPerMl);

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

        decimal factor = ResolveConversionFactor(request.FromUnit, request.ToUnit);

        decimal convertedAmount = Checked(() => request.Amount * factor);

        var result = CalculatorResult.Create(
            request.Amount,
            convertedAmount,
            request.ToUnit,
            $"{request.FromUnit} to {request.ToUnit} = {request.FromUnit} * {factor}",
            Disclaimer
        );

        return MapToResponse(result);
    }

    /// <summary>Turns decimal overflow into a validation error (HTTP 400) instead of an unhandled 500.</summary>
    private static decimal Checked(Func<decimal> compute)
    {
        try
        {
            return compute();
        }
        catch (OverflowException)
        {
            throw new ArgumentException("Value is outside the range this calculator can compute reliably. Recheck the entered numbers and units.");
        }
    }

    private static decimal ResolveConversionFactor(string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Units are required. Supported units: mcg, mg, g.");

        var key = $"{from.Trim().ToLowerInvariant()}->{to.Trim().ToLowerInvariant()}";
        return key switch
        {
            "mg->mcg" => 1000m,
            "mcg->mg" => 0.001m,
            "g->mg" => 1000m,
            "mg->g" => 0.001m,
            "g->mcg" => 1_000_000m,
            "mcg->g" => 0.000001m,
            "mg->mg" => 1m,
            "mcg->mcg" => 1m,
            "g->g" => 1m,
            _ => throw new ArgumentException($"Unsupported conversion from {from} to {to}. Supported units: mcg, mg, g.")
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
