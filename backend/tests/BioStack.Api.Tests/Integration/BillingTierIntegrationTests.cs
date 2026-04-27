namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
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
                    services.RemoveAll<DbContextOptions<BioStackDbContext>>();
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
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task Observer_IsBlockedOnSixthActiveCompound_AndPaidStatesCanExceedUntilExpired()
    {
        var userId = await SignInAsync("tier-user@example.com");
        var profile = await CreateProfileAsync("Tier User");

        for (var index = 1; index <= 5; index++)
        {
            Assert.Equal(HttpStatusCode.Created, (await CreateCompoundAsync(profile.Id, $"compound-{index}")).StatusCode);
        }

        var blocked = await CreateCompoundAsync(profile.Id, "compound-6");
        Assert.Equal(HttpStatusCode.PaymentRequired, blocked.StatusCode);
        var error = await blocked.Content.ReadFromJsonAsync<ProductErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal("observer_active_compound_limit", error.Code);
        Assert.Equal(5, error.Limit);

        var current = await _client.GetFromJsonAsync<CurrentSubscriptionResponse>("/api/v1/billing/subscription", JsonOptions);
        Assert.NotNull(current);
        Assert.Equal("Observer", current.Tier);
        Assert.Equal(5, current.Limits["active_compounds"]);

        await UpsertSubscriptionAsync(userId, ProductTier.Operator, "operator", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(20), cancelAtPeriodEnd: false);
        Assert.Equal(HttpStatusCode.Created, (await CreateCompoundAsync(profile.Id, "compound-paid")).StatusCode);

        await UpsertSubscriptionAsync(userId, ProductTier.Operator, "operator", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(20), cancelAtPeriodEnd: true);
        current = await _client.GetFromJsonAsync<CurrentSubscriptionResponse>("/api/v1/billing/subscription", JsonOptions);
        Assert.NotNull(current);
        Assert.Equal("Operator", current.Tier);
        Assert.True(current.CancelAtPeriodEnd);
        Assert.Equal(HttpStatusCode.Created, (await CreateCompoundAsync(profile.Id, "compound-canceling")).StatusCode);

        await UpsertSubscriptionAsync(userId, ProductTier.Operator, "operator", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(-1), cancelAtPeriodEnd: true);
        current = await _client.GetFromJsonAsync<CurrentSubscriptionResponse>("/api/v1/billing/subscription", JsonOptions);
        Assert.NotNull(current);
        Assert.Equal("Observer", current.Tier);

        var overLimitAfterDowngrade = await CreateCompoundAsync(profile.Id, "compound-expired");
        Assert.Equal(HttpStatusCode.PaymentRequired, overLimitAfterDowngrade.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Equal(7, await db.CompoundRecords.CountAsync(compound => compound.PersonId == profile.Id));
    }

    private async Task<Guid> SignInAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");

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
            SourceType.Manual), JsonOptions);

    private async Task UpsertSubscriptionAsync(Guid userId, ProductTier tier, string productCode, SubscriptionStatus status, DateTime periodEndUtc, bool cancelAtPeriodEnd)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var subscription = await db.Subscriptions.FirstOrDefaultAsync(item => item.AppUserId == userId);
        if (subscription is null)
        {
            subscription = new Subscription
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
}
