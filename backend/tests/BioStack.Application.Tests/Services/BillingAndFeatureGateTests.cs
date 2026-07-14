namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stripe;
using AppSubscription = BioStack.Domain.Entities.Subscription;
using ApplicationBillingService = BioStack.Application.Services.BillingService;
using Xunit;

public sealed class BillingAndFeatureGateTests
{
    [Fact]
    public async Task FeatureGate_ReturnsExpectedLimitsAndPaidFeaturesByTier()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var accessor = CurrentUser(userId);
        var gate = new FeatureGate(db, accessor.Object);
        db.AppUsers.Add(new AppUser
        {
            Id = userId,
            Provider = "email",
            ProviderKey = "feature@example.com",
            Email = "feature@example.com",
            DisplayName = "Feature User",
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        Assert.Equal(ProductTier.Observer, await gate.GetCurrentTierAsync());
        Assert.Equal(ProductContract.Current.GetLimit(FeatureCodes.ActiveCompounds, ProductTier.Observer), await gate.GetLimitAsync(FeatureCodes.ActiveCompounds));
        Assert.False(await gate.IsEnabledAsync(FeatureCodes.PaidIntelligence));
        Assert.False(await gate.IsEnabledAsync("unknown_feature"));

        db.Subscriptions.Add(new AppSubscription
        {
            Id = Guid.NewGuid(),
            AppUserId = userId,
            ProductCode = "commander",
            Tier = ProductTier.Commander,
            StripeCustomerId = "cus_test",
            StripeSubscriptionId = "sub_test",
            StripePriceId = "price_commander",
            Status = SubscriptionStatus.Active,
            CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        Assert.Equal(ProductTier.Commander, await gate.GetCurrentTierAsync());
        Assert.Null(await gate.GetLimitAsync(FeatureCodes.ActiveCompounds));
        Assert.True(await gate.IsEnabledAsync(FeatureCodes.CommanderIntelligence));
    }

    [Fact]
    public async Task FeatureGate_PastDueDowngradesImmediatelyUnderContract()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var accessor = CurrentUser(userId);
        db.AppUsers.Add(new AppUser
        {
            Id = userId,
            Provider = "email",
            ProviderKey = "past-due@example.com",
            Email = "past-due@example.com",
            DisplayName = "Past Due User",
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        });
        db.Subscriptions.Add(new AppSubscription
        {
            Id = Guid.NewGuid(),
            AppUserId = userId,
            ProductCode = "operator",
            Tier = ProductTier.Operator,
            StripeCustomerId = "cus_past_due",
            StripeSubscriptionId = "sub_past_due",
            StripePriceId = "price_operator",
            Status = SubscriptionStatus.PastDue,
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var gate = new FeatureGate(db, accessor.Object);

        Assert.Equal(0, ProductContract.Current.Billing.PastDueGraceDays);
        Assert.Equal(ProductTier.Observer, await gate.GetCurrentTierAsync());
        Assert.False(await gate.IsEnabledAsync(FeatureCodes.PaidIntelligence));
    }

    [Fact]
    public void ProductContract_DefinesCanonicalPlansRoutesAndHealth()
    {
        var contract = ProductContract.Current;

        Assert.Equal("1.0.0", contract.Version);
        Assert.Equal("month", contract.Billing.Interval);
        Assert.Equal(["observer", "operator", "commander"], contract.Billing.Plans.Select(plan => plan.Code));
        Assert.Equal(1200, contract.GetPlan("operator").MonthlyPriceCents);
        Assert.Equal("/start", contract.Routes.Canonical["onboarding"]);
        Assert.Equal("/tools/analyzer", contract.Routes.Canonical["analyzer"]);
        Assert.Equal("/health", contract.Health.LivenessPath);
        Assert.Equal("/health/keon", contract.Health.KeonDependencyPath);
    }

    [Fact]
    public async Task WebhookReconciliation_IsIdempotentAndWritesSubscriptionState()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.AppUsers.Add(new AppUser
        {
            Id = userId,
            Provider = "email",
            ProviderKey = "billing@example.com",
            Email = "billing@example.com",
            DisplayName = "Billing User",
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(item => item["Stripe:OperatorPriceId"]).Returns("price_operator");
        config.Setup(item => item["Stripe:CommanderPriceId"]).Returns("price_commander");

        var accessor = CurrentUser(userId);
        var service = new ApplicationBillingService(
            db,
            new AppUserRepository(db),
            accessor.Object,
            new FeatureGate(db, accessor.Object),
            config.Object);

        var stripeEvent = new Event
        {
            Id = "evt_subscription_updated",
            Type = "customer.subscription.updated",
            Data = new EventData
            {
                Object = new Stripe.Subscription
                {
                    Id = "sub_123",
                    CustomerId = "cus_123",
                    Status = "active",
                    CancelAtPeriodEnd = false,
                    Metadata = new Dictionary<string, string> { ["appUserId"] = userId.ToString() },
                    Items = new StripeList<SubscriptionItem>
                    {
                        Data =
                        [
                            new SubscriptionItem
                            {
                                Price = new Price { Id = "price_commander" },
                                CurrentPeriodStart = DateTime.UtcNow.AddDays(-1),
                                CurrentPeriodEnd = DateTime.UtcNow.AddDays(30)
                            }
                        ]
                    }
                }
            }
        };

        await service.ProcessStripeEventAsync(stripeEvent);
        await service.ProcessStripeEventAsync(stripeEvent);

        var subscription = await db.Subscriptions.SingleAsync();
        Assert.Equal(ProductTier.Commander, subscription.Tier);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal("sub_123", subscription.StripeSubscriptionId);
        Assert.Equal(1, await db.StripeWebhookEvents.CountAsync());
    }

    private static BioStackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), $"biostack-feature-{Guid.NewGuid():N}.db")}")
            .Options;
        var db = new BioStackDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static Mock<ICurrentUserAccessor> CurrentUser(Guid userId)
    {
        var accessor = new Mock<ICurrentUserAccessor>();
        accessor.Setup(item => item.GetCurrentUserId()).Returns(userId);
        return accessor;
    }
}
