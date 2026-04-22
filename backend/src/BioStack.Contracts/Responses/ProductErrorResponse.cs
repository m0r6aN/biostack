namespace BioStack.Contracts.Responses;

public sealed record ProductErrorResponse(
    string Code,
    string Message,
    string Tier,
    int? Limit,
    bool UpgradeRequired
);
