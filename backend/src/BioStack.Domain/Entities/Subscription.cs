namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class Subscription
{
    public Guid Id { get; set; }
    public Guid AppUserId { get; set; }
    public string ProductCode { get; set; } = "observer";
    public ProductTier Tier { get; set; } = ProductTier.Observer;
    public BillingProvider Provider { get; set; } = BillingProvider.Stripe;
    public string StripeCustomerId { get; set; } = string.Empty;
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string? StripePriceId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.None;
    public DateTime? CurrentPeriodStartUtc { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public AppUser? AppUser { get; set; }
}
