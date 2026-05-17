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
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

// PR 1A (B1): server-side PaidIntelligence enforcement on POST /api/analyze/protocol.
[Trait("Category", "Integration")]
public sealed class AnalyzerGateIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-analyzer-gate-{Guid.NewGuid():N}.db");
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

        await using var scope = _factory.Services.CreateAsyncScope();
        var knowledgeSource = scope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
        var seedSource = new LocalKnowledgeSource();
        var seedEntries = await seedSource.GetAllCompoundsAsync();
        foreach (var entry in seedEntries)
        {
            await knowledgeSource.UpsertCompoundAsync(entry);
        }
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
    public async Task AnalyzeProtocol_AnonymousUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/analyze/protocol", new
        {
            inputText = "BPC-157 500mcg daily"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeProtocol_ObserverTier_ReturnsUpgradeRequired()
    {
        await SignInAsync("observer@example.com");

        var response = await _client.PostAsJsonAsync("/api/analyze/protocol", new
        {
            inputText = "BPC-157 500mcg daily"
        });

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ProductErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal("paid_intelligence", error!.Code);
        Assert.Equal("Observer", error.Tier);
        Assert.True(error.UpgradeRequired);
    }

    [Fact]
    public async Task AnalyzeProtocol_OperatorTier_Returns200()
    {
        var userId = await SignInAsync("operator@example.com");
        await UpsertSubscriptionAsync(userId, ProductTier.Operator);

        var response = await _client.PostAsJsonAsync("/api/analyze/protocol", new
        {
            inputText = "BPC-157 500mcg daily"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeProtocol_CommanderTier_Returns200()
    {
        var userId = await SignInAsync("commander@example.com");
        await UpsertSubscriptionAsync(userId, ProductTier.Commander);

        var response = await _client.PostAsJsonAsync("/api/analyze/protocol", new
        {
            inputText = "BPC-157 500mcg daily"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    private async Task UpsertSubscriptionAsync(Guid userId, ProductTier tier)
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
        subscription.ProductCode = tier == ProductTier.Commander ? "commander" : "operator";
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1);
        subscription.CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(30);
        subscription.CancelAtPeriodEnd = false;
        subscription.StripePriceId = tier == ProductTier.Commander ? "price_commander" : "price_operator";
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
