namespace BioStack.Contracts.Requests;

public sealed record CreateProtocolComputationRequest(
    Guid? RunId,
    string Type,
    string InputSnapshot,
    string OutputResult
);
