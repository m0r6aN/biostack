namespace BioStack.Api.Tests.Integration;

using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Stripe;
using StripeSubscription = Stripe.Subscription;
using Xunit;

[Trait("Category", "Integration")]
public sealed class StripeWebhookQuarantineIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-stripe-quarantine-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                        ["FrontendUrl"] = "http://localhost:3043",
                        ["PublicApiUrl"] = "http://localhost:5000",
                        ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
                        ["Stripe:OperatorPriceId"] = "price_operator",
                        ["Stripe:CommanderPriceId"] = "price_commander",
                        ["Stripe:WebhookSecret"] = "whsec_test",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveBioStackDbContext();
                    services.AddDbContext<BioStackDbContext>(options =>
                        options.UseSqlite($"Data Source={_dbPath}"));
                });
            });

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        try
        {
            if (System.IO.File.Exists(_dbPath))
            {
                System.IO.File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
        }
    }

    private static Event SubscriptionEvent(string eventId, string customerId, string priceId, string? metadataUserId)
    {
        var subscription = new StripeSubscription
        {
            Id = $"sub_{eventId}",
            CustomerId = customerId,
            Status = "active",
            Metadata = metadataUserId is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["appUserId"] = metadataUserId },
            Items = new StripeList<SubscriptionItem>
            {
                Data =
                [
                    new SubscriptionItem
                    {
                        Id = $"si_{eventId}",
                        Price = new Price { Id = priceId },
                        CurrentPeriodStart = DateTime.UtcNow.AddDays(-1),
                        CurrentPeriodEnd = DateTime.UtcNow.AddDays(30),
                    },
                ],
            },
        };

        return new Event
        {
            Id = eventId,
            Type = "customer.subscription.updated",
            Data = new EventData { Object = subscription },
        };
    }

    private async Task<AppUser> SeedUserAsync(IServiceScope scope, string email)
    {
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Provider = "email",
            ProviderKey = email,
            Email = email,
            DisplayName = "Stripe Quarantine Test User",
        };
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task SubscriptionEvent_ForUnmappedCustomer_IsQuarantinedAsUnmappedAppUser()
    {
        using var scope = _factory.Services.CreateScope();
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();

        var result = await billing.ProcessStripeEventAsync(
            SubscriptionEvent($"evt_unmapped_{Guid.NewGuid():N}", "cus_unknown", "price_operator", metadataUserId: null));

        Assert.Equal(StripeWebhookProcessingResult.Quarantined, result);

        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var receipt = await db.StripeWebhookEvents.SingleAsync();
        Assert.Equal(StripeWebhookProcessingStatuses.Quarantined, receipt.ProcessingStatus);
        Assert.Equal("unmapped_app_user", receipt.FailureCode);
        Assert.Equal(1, receipt.AttemptCount);
    }

    [Fact]
    public async Task SubscriptionEvent_ForUnknownPrice_IsQuarantinedAsUnknownStripePrice()
    {
        using var scope = _factory.Services.CreateScope();
        var user = await SeedUserAsync(scope, "stripe-price-test@example.com");
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();

        var result = await billing.ProcessStripeEventAsync(
            SubscriptionEvent($"evt_price_{Guid.NewGuid():N}", "cus_price_test", "price_bogus", user.Id.ToString()));

        Assert.Equal(StripeWebhookProcessingResult.Quarantined, result);

        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var receipt = await db.StripeWebhookEvents.SingleAsync();
        Assert.Equal(StripeWebhookProcessingStatuses.Quarantined, receipt.ProcessingStatus);
        Assert.Equal("unknown_stripe_price", receipt.FailureCode);
    }

    [Fact]
    public async Task SubscriptionEvent_ForMappedCustomer_ReconcilesSubscriptionRowWithPaidPeriod()
    {
        using var scope = _factory.Services.CreateScope();
        var user = await SeedUserAsync(scope, "stripe-reconcile-test@example.com");
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();

        var result = await billing.ProcessStripeEventAsync(
            SubscriptionEvent($"evt_reconcile_{Guid.NewGuid():N}", "cus_reconcile_test", "price_operator", user.Id.ToString()));

        Assert.Equal(StripeWebhookProcessingResult.Processed, result);

        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var subscription = await db.Subscriptions.SingleAsync();
        Assert.Equal(user.Id, subscription.AppUserId);
        Assert.Equal(ProductTier.Operator, subscription.Tier);
        Assert.Equal("price_operator", subscription.StripePriceId);
        Assert.True(subscription.CurrentPeriodEndUtc > DateTime.UtcNow);
    }
}
