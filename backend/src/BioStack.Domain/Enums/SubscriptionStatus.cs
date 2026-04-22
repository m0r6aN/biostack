namespace BioStack.Domain.Enums;

public enum SubscriptionStatus
{
    None = 0,
    Incomplete = 1,
    Trialing = 2,
    Active = 3,
    PastDue = 4,
    Canceled = 5,
    Unpaid = 6,
    IncompleteExpired = 7,
    Paused = 8
}
