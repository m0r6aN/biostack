namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe;
using ApplicationBillingService = BioStack.Application.Services.BillingService;
using Xunit;

public sealed class BillingWebhookReceiptTests
{
    [Fact]
    public async Task UnexpectedHandlerException_KeepsFailedReceiptAndSkipsReplay()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, operatorPriceId: "price_operator");
        var stripeEvent = new Event
        {
            Id = "evt_unexpected",
            Type = "customer.subscription.updated",
            Data = null!,
        };

        await Assert.ThrowsAsync<NullReferenceException>(() => service.ProcessStripeEventAsync(stripeEvent));

        db.ChangeTracker.Clear();
        var receipt = await db.StripeWebhookEvents.SingleAsync();
        Assert.Equal(StripeWebhookProcessingStatuses.Failed, receipt.ProcessingStatus);
        Assert.Equal("unexpected_processing_error", receipt.FailureCode);
        Assert.Equal(1, receipt.AttemptCount);
        Assert.DoesNotContain("Exception", receipt.FailureCode, StringComparison.Ordinal);

        Assert.Equal(
            StripeWebhookProcessingResult.AlreadyProcessed,
            await service.ProcessStripeEventAsync(stripeEvent));

        db.ChangeTracker.Clear();
        receipt = await db.StripeWebhookEvents.SingleAsync();
        Assert.Equal(StripeWebhookProcessingStatuses.Failed, receipt.ProcessingStatus);
        Assert.Equal("unexpected_processing_error", receipt.FailureCode);
        Assert.Equal(1, receipt.AttemptCount);
    }

    [Fact]
    public async Task QuarantinedReceipt_CanStillBeReclaimed()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.AppUsers.Add(new AppUser
        {
            Id = userId,
            Provider = "email",
            ProviderKey = "receipt-reclaim@example.com",
            Email = "receipt-reclaim@example.com",
            DisplayName = "Receipt Reclaim",
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        string? approvedOperatorPrice = null;
        var service = CreateService(db, () => approvedOperatorPrice);
        var stripeEvent = new Event
        {
            Id = "evt_reclaim",
            Type = "customer.subscription.updated",
            Data = new EventData
            {
                Object = new Stripe.Subscription
                {
                    Id = "sub_evt_reclaim",
                    CustomerId = "cus_evt_reclaim",
                    Status = "active",
                    CancelAtPeriodEnd = false,
                    Metadata = new Dictionary<string, string> { ["appUserId"] = userId.ToString() },
                    Items = new StripeList<SubscriptionItem>
                    {
                        Data =
                        [
                            new SubscriptionItem
                            {
                                Price = new Price { Id = "price_operator_pending" },
                                CurrentPeriodStart = DateTime.UtcNow.AddDays(-1),
                                CurrentPeriodEnd = DateTime.UtcNow.AddDays(30),
                            },
                        ],
                    },
                },
            },
        };

        Assert.Equal(StripeWebhookProcessingResult.Quarantined, await service.ProcessStripeEventAsync(stripeEvent));
        db.ChangeTracker.Clear();
        var receipt = await db.StripeWebhookEvents.SingleAsync();
        Assert.Equal(StripeWebhookProcessingStatuses.Quarantined, receipt.ProcessingStatus);
        Assert.Equal("unknown_stripe_price", receipt.FailureCode);
        Assert.Equal(1, receipt.AttemptCount);

        approvedOperatorPrice = "price_operator_pending";
        Assert.Equal(StripeWebhookProcessingResult.Processed, await service.ProcessStripeEventAsync(stripeEvent));

        db.ChangeTracker.Clear();
        receipt = await db.StripeWebhookEvents.SingleAsync();
        Assert.Equal(StripeWebhookProcessingStatuses.Processed, receipt.ProcessingStatus);
        Assert.Null(receipt.FailureCode);
        Assert.Equal(2, receipt.AttemptCount);
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Empty(await service.GetQuarantinedStripeEventsAsync());
    }

    private static ApplicationBillingService CreateService(BioStackDbContext db, string? operatorPriceId)
        => CreateService(db, () => operatorPriceId);

    private static ApplicationBillingService CreateService(BioStackDbContext db, Func<string?> operatorPriceId)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(item => item["Stripe:OperatorPriceId"]).Returns(() => operatorPriceId());
        config.Setup(item => item["Stripe:CommanderPriceId"]).Returns("price_commander");
        var accessor = new Mock<ICurrentUserAccessor>();
        accessor.Setup(item => item.GetCurrentUserId()).Returns(Guid.NewGuid());
        return new ApplicationBillingService(
            db,
            new AppUserRepository(db),
            accessor.Object,
            new FeatureGate(db, accessor.Object),
            config.Object);
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
}
