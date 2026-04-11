namespace BioStack.Contracts.Requests;

public sealed record CreateProtocolPhaseRequest(
    string Name,
    DateTime? StartDate,
    DateTime? EndDate,
    string Notes
);
