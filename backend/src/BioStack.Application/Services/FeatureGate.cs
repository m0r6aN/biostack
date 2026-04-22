namespace BioStack.Application.Services;

using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class FeatureCodes
{
    public const string ActiveCompounds = "active_compounds";
    public const string PaidIntelligence = "paid_intelligence";
    public const string CommanderIntelligence = "commander_intelligence";
}

public sealed class FeatureGate : IFeatureGate
{
    public const int ObserverActiveCompoundLimit = 5;
    private readonly BioStackDbContext _db;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public FeatureGate(BioStackDbContext db, ICurrentUserAccessor currentUserAccessor)
    {
        _db = db;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<ProductTier> GetCurrentTierAsync(CancellationToken cancellationToken = default)
        => await GetEffectiveTierAsync(_currentUserAccessor.GetCurrentUserId(), cancellationToken);

    public async Task<ProductTier> GetEffectiveTierAsync(Guid appUserId, CancellationToken cancellationToken = default)
    {
        var subscription = await _db.Subscriptions
            .Where(s => s.AppUserId == appUserId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
            return ProductTier.Observer;

        var now = DateTime.UtcNow;
        var paidThrough = subscription.CurrentPeriodEndUtc is not null && subscription.CurrentPeriodEndUtc > now;
        var hasPaidAccess = subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing
            && (subscription.CurrentPeriodEndUtc is null || paidThrough);

        return hasPaidAccess ? subscription.Tier : ProductTier.Observer;
    }

    public async Task<bool> IsEnabledAsync(string featureCode, CancellationToken cancellationToken = default)
    {
        var tier = await GetCurrentTierAsync(cancellationToken);
        return featureCode switch
        {
            FeatureCodes.PaidIntelligence => tier >= ProductTier.Operator,
            FeatureCodes.CommanderIntelligence => tier >= ProductTier.Commander,
            _ => true
        };
    }

    public async Task<int?> GetLimitAsync(string featureCode, CancellationToken cancellationToken = default)
    {
        var tier = await GetCurrentTierAsync(cancellationToken);
        return featureCode == FeatureCodes.ActiveCompounds && tier == ProductTier.Observer
            ? ObserverActiveCompoundLimit
            : null;
    }

    public async Task EnsureEnabledAsync(string featureCode, CancellationToken cancellationToken = default)
    {
        if (await IsEnabledAsync(featureCode, cancellationToken))
            return;

        var tier = await GetCurrentTierAsync(cancellationToken);
        throw new FeatureLimitExceededException(
            featureCode,
            featureCode == FeatureCodes.CommanderIntelligence
                ? "Commander is required for this intelligence surface."
                : "Operator is required for this intelligence surface.",
            tier,
            null);
    }
}

public interface IFeatureGate
{
    Task<ProductTier> GetCurrentTierAsync(CancellationToken cancellationToken = default);
    Task<ProductTier> GetEffectiveTierAsync(Guid appUserId, CancellationToken cancellationToken = default);
    Task<bool> IsEnabledAsync(string featureCode, CancellationToken cancellationToken = default);
    Task<int?> GetLimitAsync(string featureCode, CancellationToken cancellationToken = default);
    Task EnsureEnabledAsync(string featureCode, CancellationToken cancellationToken = default);
}
