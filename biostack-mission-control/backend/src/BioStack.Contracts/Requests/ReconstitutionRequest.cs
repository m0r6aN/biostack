namespace BioStack.Contracts.Requests;

public sealed record ReconstitutionRequest(
    decimal PeptideAmountMg,
    decimal DiluentVolumeMl
);
