namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public interface IEntitlementService
{
    Task<EntitlementResponse> GetUserEntitlementsAsync(Guid? userId, CancellationToken cancellationToken = default);
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

        // Single source of truth: the billing ledger the Stripe webhook maintains
        // (Subscription.ProductCode/Tier/Status). Operator and Commander are the
        // paid tiers — the monetization UI markets them together as "Pro".
        var isActivePro = await _db.Subscriptions
            .AsNoTracking()
            .AnyAsync(
                s => s.AppUserId == userId.Value
                    && (s.ProductCode == "operator" || s.ProductCode == "commander")
                    && (s.Status == SubscriptionStatus.Active
                        || s.Status == SubscriptionStatus.Trialing
                        || (s.Status == SubscriptionStatus.PastDue
                            && s.CurrentPeriodEndUtc != null
                            && s.CurrentPeriodEndUtc > DateTime.UtcNow)),
                cancellationToken);

        return isActivePro ? BuildPro() : BuildFree();
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
