namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
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
using Newtonsoft.Json;
using Stripe;
using Xunit;

[Trait("Category", "Integration")]
public sealed class BillingTierIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-billing-{Guid.NewGuid():N}.db");
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

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
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

    [Fact]
    public async Task Observer_IsBlockedBeyondActiveCompoundLimit_AndPaidStatesCanExceedUntilExpired()
    {
        var limit = ProductContract.Current.GetLimit(FeatureCodes.ActiveCompounds, ProductTier.Observer)!.Value;
        var userId = await SignInAsync("tier-user@example.com");
        var profile = await CreateProfileAsync("Tier User");

        for (var index = 1; index <= limit; index++)
        {
            Assert.Equal(HttpStatusCode.Created, (await CreateCompoundAsync(profile.Id, $"compound-{index}")).StatusCode);
        }

        var blocked = await CreateCompoundAsync(profile.Id, $"compound-{limit + 1}");
        Assert.Equal(HttpStatusCode.PaymentRequired, blocked.StatusCode);
        var error = await blocked.Content.ReadFromJsonAsync<ProductErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal("observer_active_compound_limit", error.Code);
        Assert.Equal(limit, error.Limit);

        var current = await _client.GetFromJsonAsync<CurrentSubscriptionResponse>("/api/v1/billing/subscription", JsonOptions);
        Assert.NotNull(current);
        Assert.Equal(ProductContract.Current.Version, current.ContractVersion);
        Assert.Equal("Observer", current.Tier);
        Assert.Equal(limit, current.Limits["active_compounds"]);
        Assert.False(current.Features.ContainsKey("protocol_intelligence_contracts"));
        Assert.False(current.Features.ContainsKey("protocol_phase_map"));
        Assert.False(current.Features[FeatureCodes.ReviewedRelationshipGraph]);
        Assert.False(current.Features[FeatureCodes.SourceQualityTracker]);
        Assert.False(current.Features[FeatureCodes.Glp1ObservabilityPack]);
        Assert.False(current.Features[FeatureCodes.SideEffectAmbiguityDetector]);
        Assert.False(current.Features.ContainsKey("longitudinal_protocol_intelligence_report"));
        Assert.True(current.Features[FeatureCodes.HighRiskWarningFirstGuardrails]);

        await UpsertSubscriptionAsync(userId, ProductTier.Operator, "operator", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(20), cancelAtPeriodEnd: false);
        Assert.Equal(HttpStatusCode.Created, (await CreateCompoundAsync(profile.Id, "compound-paid")).StatusCode);

        await UpsertSubscriptionAsync(userId, ProductTier.Operator, "operator", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(20), cancelAtPeriodEnd: true);
        current = await _client.GetFromJsonAsync<CurrentSubscriptionResponse>("/api/v1/billing/subscription", JsonOptions);
        Assert.NotNull(current);
        Assert.Equal("Operator", current.Tier);
        Assert.True(current.CancelAtPeriodEnd);
        Assert.False(current.Features.ContainsKey("protocol_intelligence_contracts"));
        Assert.False(current.Features.ContainsKey("protocol_phase_map"));
        Assert.True(current.Features[FeatureCodes.ReviewedRelationshipGraph]);
        Assert.True(current.Features[FeatureCodes.SourceQualityTracker]);
        Assert.True(current.Features[FeatureCodes.Glp1ObservabilityPack]);
        Assert.False(current.Features[FeatureCodes.SideEffectAmbiguityDetector]);
        Assert.False(current.Features.ContainsKey("longitudinal_protocol_intelligence_report"));
        Assert.True(current.Features[FeatureCodes.HighRiskWarningFirstGuardrails]);
        Assert.Equal(HttpStatusCode.Created, (await CreateCompoundAsync(profile.Id, "compound-canceling")).StatusCode);

        await UpsertSubscriptionAsync(userId, ProductTier.Commander, "commander", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(20), cancelAtPeriodEnd: false);
        current = await _client.GetFromJsonAsync<CurrentSubscriptionResponse>("/api/v1/billing/subscription", JsonOptions);
        Assert.NotNull(current);
        Assert.Equal("Commander", current.Tier);
        Assert.False(current.Features.ContainsKey("protocol_intelligence_contracts"));
        Assert.False(current.Features.ContainsKey("protocol_phase_map"));
        Assert.True(current.Features[FeatureCodes.ReviewedRelationshipGraph]);
        Assert.True(current.Features[FeatureCodes.SourceQualityTracker]);
        Assert.True(current.Features[FeatureCodes.Glp1ObservabilityPack]);
        Assert.True(current.Features[FeatureCodes.SideEffectAmbiguityDetector]);
        Assert.False(current.Features.ContainsKey("longitudinal_protocol_intelligence_report"));
        Assert.True(current.Features[FeatureCodes.HighRiskWarningFirstGuardrails]);

        await UpsertSubscriptionAsync(userId, ProductTier.Operator, "operator", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(-1), cancelAtPeriodEnd: true);
        current = await _client.GetFromJsonAsync<CurrentSubscriptionResponse>("/api/v1/billing/subscription", JsonOptions);
        Assert.NotNull(current);
        Assert.Equal("Observer", current.Tier);

        var overLimitAfterDowngrade = await CreateCompoundAsync(profile.Id, "compound-expired");
        Assert.Equal(HttpStatusCode.PaymentRequired, overLimitAfterDowngrade.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Equal(limit + 2, await db.CompoundRecords.CountAsync(compound => compound.PersonId == profile.Id));
    }

    [Fact]
    public async Task SignedWebhook_IsIdempotentAndPersistsOneSubscription()
    {
        var userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            db.AppUsers.Add(new AppUser
            {
                Id = userId,
                Provider = "email",
                ProviderKey = "signed-webhook@example.com",
                Email = "signed-webhook@example.com",
                DisplayName = "Signed Webhook User",
                CreatedAtUtc = DateTime.UtcNow,
                LastSeenAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var stripeEvent = SubscriptionEvent("evt_signed_idempotent", userId, "price_operator");
        var first = await PostSignedStripeEventAsync(stripeEvent);
        var replay = await PostSignedStripeEventAsync(stripeEvent);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Equal(1, await verificationDb.Subscriptions.CountAsync());
        var receipt = await verificationDb.StripeWebhookEvents.SingleAsync();
        Assert.Equal(StripeWebhookProcessingStatuses.Processed, receipt.ProcessingStatus);
        Assert.Equal(1, receipt.AttemptCount);
    }

    [Fact]
    public async Task SignedWebhook_QuarantinesUnknownPrice_AndInvalidSignatureWritesNothing()
    {
        var stripeEvent = SubscriptionEvent("evt_unknown_signed", Guid.NewGuid(), "price_not_approved");

        var invalid = await PostStripeEventAsync(stripeEvent, "t=1,v1=invalid");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var quarantined = await PostSignedStripeEventAsync(stripeEvent);
        Assert.Equal(HttpStatusCode.Conflict, quarantined.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var receipt = await db.StripeWebhookEvents.SingleAsync();
        Assert.Equal(StripeWebhookProcessingStatuses.Quarantined, receipt.ProcessingStatus);
        Assert.Equal("unknown_stripe_price", receipt.FailureCode);
        Assert.Equal(1, receipt.AttemptCount);
    }

    private async Task<Guid> SignInAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        var consent = await _client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
    }

    private async Task<ProfileResponse> CreateProfileAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/profiles", new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> CreateCompoundAsync(Guid profileId, string name)
        => _client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/compounds", new CreateCompoundRequest(
            name,
            CompoundCategory.Peptide,
            DateTime.UtcNow.Date,
            null,
            CompoundStatus.Active,
            "notes",
            BioStack.Domain.Enums.SourceType.Manual), JsonOptions);

    private async Task UpsertSubscriptionAsync(Guid userId, ProductTier tier, string productCode, SubscriptionStatus status, DateTime periodEndUtc, bool cancelAtPeriodEnd)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var subscription = await db.Subscriptions.FirstOrDefaultAsync(item => item.AppUserId == userId);
        if (subscription is null)
        {
            subscription = new BioStack.Domain.Entities.Subscription
            {
                Id = Guid.NewGuid(),
                AppUserId = userId,
                StripeCustomerId = "cus_test",
                StripeSubscriptionId = "sub_test",
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Subscriptions.Add(subscription);
        }

        subscription.Tier = tier;
        subscription.ProductCode = productCode;
        subscription.Status = status;
        subscription.CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-10);
        subscription.CurrentPeriodEndUtc = periodEndUtc;
        subscription.CancelAtPeriodEnd = cancelAtPeriodEnd;
        subscription.StripePriceId = tier == ProductTier.Commander ? "price_commander" : "price_operator";
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private Task<HttpResponseMessage> PostSignedStripeEventAsync(Event stripeEvent)
    {
        var json = JsonConvert.SerializeObject(stripeEvent);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = Encoding.UTF8.GetBytes($"{timestamp}.{json}");
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("whsec_test"), signedPayload)).ToLowerInvariant();
        return PostStripeEventJsonAsync(json, $"t={timestamp},v1={signature}");
    }

    private Task<HttpResponseMessage> PostStripeEventAsync(Event stripeEvent, string signature)
        => PostStripeEventJsonAsync(JsonConvert.SerializeObject(stripeEvent), signature);

    private async Task<HttpResponseMessage> PostStripeEventJsonAsync(string json, string signature)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billing/stripe/webhook")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature", signature);
        return await _client.SendAsync(request);
    }

    private static Event SubscriptionEvent(string eventId, Guid userId, string priceId)
        => new()
        {
            Id = eventId,
            Object = "event",
            ApiVersion = StripeConfiguration.ApiVersion,
            Created = DateTime.UtcNow,
            Livemode = false,
            PendingWebhooks = 1,
            Type = "customer.subscription.updated",
            Data = new EventData
            {
                Object = new Stripe.Subscription
                {
                    Id = $"sub_{eventId}",
                    Object = "subscription",
                    CustomerId = $"cus_{eventId}",
                    Status = "active",
                    CancelAtPeriodEnd = false,
                    Metadata = new Dictionary<string, string> { ["appUserId"] = userId.ToString() },
                    Items = new StripeList<SubscriptionItem>
                    {
                        Data =
                        [
                            new SubscriptionItem
                            {
                                Price = new Price { Id = priceId, Object = "price" },
                                CurrentPeriodStart = DateTime.UtcNow.AddDays(-1),
                                CurrentPeriodEnd = DateTime.UtcNow.AddDays(30)
                            }
                        ]
                    }
                }
            }
        };
}
