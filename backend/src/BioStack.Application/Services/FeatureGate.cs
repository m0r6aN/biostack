namespace BioStack.Application.Services;

using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class FeatureCodes
{
    public const string ActiveCompounds = "active_compounds";
    public const string PaidIntelligence = "paid_intelligence";
    public const string CommanderIntelligence = "commander_intelligence";
    public const string ReviewedRelationshipGraph = "reviewed_relationship_graph";
    public const string SourceQualityTracker = "source_quality_tracker";
    public const string Glp1ObservabilityPack = "glp1_observability_pack";
    public const string SideEffectAmbiguityDetector = "side_effect_ambiguity_detector";
    public const string HighRiskWarningFirstGuardrails = "high_risk_warning_first_guardrails";
}

public sealed class FeatureGate : IFeatureGate
{
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

        return ProductContract.Current.HasPaidAccess(subscription.Status, subscription.CurrentPeriodEndUtc, DateTime.UtcNow)
            ? subscription.Tier
            : ProductTier.Observer;
    }

    public async Task<bool> IsEnabledAsync(string featureCode, CancellationToken cancellationToken = default)
    {
        var tier = await GetCurrentTierAsync(cancellationToken);
        return ProductContract.Current.IsFeatureEnabled(featureCode, tier);
    }

    public async Task<int?> GetLimitAsync(string featureCode, CancellationToken cancellationToken = default)
    {
        var tier = await GetCurrentTierAsync(cancellationToken);
        return ProductContract.Current.GetLimit(featureCode, tier);
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
