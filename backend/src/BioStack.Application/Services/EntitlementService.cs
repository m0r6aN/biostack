namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public interface IEntitlementService
{
    Task<EntitlementResponse> GetUserEntitlementsAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<UserSubscription> GetOrCreateSubscriptionRecordAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSubscription?> GetSubscriptionByStripeCustomerAsync(string stripeCustomerId, CancellationToken cancellationToken = default);
    Task<UserSubscription?> GetSubscriptionByStripeSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class EntitlementService : IEntitlementService
{
    private static readonly string[] FutureRoles =
    [
        "retail_admin",
        "client_seat",
        "white_label",
        "affiliate_enabled"
    ];

    private readonly BioStackDbContext _db;

    public EntitlementService(BioStackDbContext db)
    {
        _db = db;
    }

    public async Task<EntitlementResponse> GetUserEntitlementsAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (userId is null)
        {
            return BuildFree();
        }

        var subscription = await _db.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId.Value, cancellationToken);

        if (subscription is null)
        {
            return BuildFree();
        }

        var isActivePro = subscription.IsPro &&
            subscription.Plan == "pro" &&
            IsStripeStatusEntitled(subscription.SubscriptionStatus, subscription.CurrentPeriodEndUtc);

        return isActivePro ? BuildPro() : BuildFree();
    }

    public async Task<UserSubscription> GetOrCreateSubscriptionRecordAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (subscription is not null)
        {
            return subscription;
        }

        subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Plan = "free",
            SubscriptionStatus = "free",
            IsPro = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.UserSubscriptions.Add(subscription);
        return subscription;
    }

    public Task<UserSubscription?> GetSubscriptionByStripeCustomerAsync(string stripeCustomerId, CancellationToken cancellationToken = default)
    {
        return _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, cancellationToken);
    }

    public Task<UserSubscription?> GetSubscriptionByStripeSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        return _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    public static bool IsStripeStatusEntitled(string status, DateTime? currentPeriodEndUtc)
    {
        var normalized = status.Trim().ToLowerInvariant();
        if (normalized is "active" or "trialing")
        {
            return true;
        }

        return normalized is "past_due" && currentPeriodEndUtc is not null && currentPeriodEndUtc > DateTime.UtcNow;
    }

    private static EntitlementResponse BuildFree()
    {
        return new EntitlementResponse(
            IsPro: false,
            Plan: "free",
            Limits: new EntitlementLimitsResponse(MaxCompounds: 2),
            Features: new EntitlementFeaturesResponse(
                StackIntelligence: false,
                FullOverlapAnalysis: false,
                ProtocolBuilder: false,
                ObservabilityCorrelations: false,
                SavedAdvancedProtocolViews: false,
                AffiliateSurfaces: true),
            FutureRoles: FutureRoles);
    }

    private static EntitlementResponse BuildPro()
    {
        return new EntitlementResponse(
            IsPro: true,
            Plan: "pro",
            Limits: new EntitlementLimitsResponse(MaxCompounds: int.MaxValue),
            Features: new EntitlementFeaturesResponse(
                StackIntelligence: true,
                FullOverlapAnalysis: true,
                ProtocolBuilder: true,
                ObservabilityCorrelations: true,
                SavedAdvancedProtocolViews: true,
                AffiliateSurfaces: true),
            FutureRoles: FutureRoles);
    }
}
