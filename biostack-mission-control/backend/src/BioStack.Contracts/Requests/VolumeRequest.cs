namespace BioStack.Contracts.Requests;

public sealed record VolumeRequest(
    decimal DesiredDoseMcg,
    decimal ConcentrationMcgPerMl
);
