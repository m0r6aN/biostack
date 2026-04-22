namespace BioStack.Contracts.Responses;

public sealed record CurrentSubscriptionResponse(
    string Tier,
    string Status,
    string ProductCode,
    bool IsPaid,
    bool CancelAtPeriodEnd,
    DateTime? CurrentPeriodEndUtc,
    Dictionary<string, bool> Features,
    Dictionary<string, int?> Limits
);
