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
public sealed class AuthorizationEnforcementMatrixIntegrationTests : IAsyncLifetime
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
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-authz-matrix-{Guid.NewGuid():N}.db");
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
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }

    [Theory]
    [InlineData("GET", "/api/v1/profiles")]
    [InlineData("GET", "/api/v1/profiles/11111111-1111-1111-1111-111111111111/compounds")]
    [InlineData("GET", "/api/v1/intelligence/compounds/creatine/relationships")]
    [InlineData("GET", "/api/v1/intelligence/compatibility?compounds=creatine&compounds=glycine")]
    [InlineData("POST", "/api/v1/protocols/11111111-1111-1111-1111-111111111111/review/complete")]
    [InlineData("GET", "/api/v1/admin/provider-access/requests/")]
    [InlineData("PATCH", "/api/v1/admin/provider-access/requests/11111111-1111-1111-1111-111111111111")]
    public async Task AnonymousProtectedRouteMatrix_Returns401(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PATCH")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReviewCompletion_RequiresCommander_AndFailsClosedAfterDowngrade()
    {
        var userId = await SignInAsync("review-tier-matrix@example.com");
        var profile = await CreateProfileAsync();
        await CreateActiveCompoundAsync(profile.Id);
        var protocol = await SaveProtocolAsync(profile.Id);
        var path = $"/api/v1/protocols/{protocol.Id}/review/complete";
        var request = new CompleteProtocolReviewRequest(null, "Authorization matrix review.");

        var observer = await _client.PostAsJsonAsync(path, request, JsonOptions);
        Assert.Equal(HttpStatusCode.PaymentRequired, observer.StatusCode);

        await UpsertSubscriptionAsync(userId, ProductTier.Commander, DateTime.UtcNow.AddDays(30));
        var commander = await _client.PostAsJsonAsync(path, request, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, commander.StatusCode);

        await UpsertSubscriptionAsync(userId, ProductTier.Commander, DateTime.UtcNow.AddMinutes(-1));
        var downgraded = await _client.PostAsJsonAsync(path, request, JsonOptions);
        Assert.Equal(HttpStatusCode.PaymentRequired, downgraded.StatusCode);
    }

    private async Task<Guid> SignInAsync(string email)
    {
        await _client.PostAsJsonAsync(
            "/api/v1/auth/start",
            new StartAuthRequest(email, "email", "/protocols"),
            JsonOptions);
        using var inbox = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = inbox.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions)).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private async Task<ProfileResponse> CreateProfileAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/profiles",
            new CreateProfileRequest("Review Matrix", Sex.Unspecified, 80m, 35, "Observe", "notes"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private async Task CreateActiveCompoundAsync(Guid profileId)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            new CreateCompoundRequest(
                "Creatine",
                CompoundCategory.Supplement,
                DateTime.UtcNow.Date,
                null,
                CompoundStatus.Active,
                "notes",
                SourceType.Manual),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<ProtocolResponse> SaveProtocolAsync(Guid profileId)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/protocols",
            new SaveProtocolRequest("Authorization Matrix"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProtocolResponse>(JsonOptions))!;
    }

    private async Task UpsertSubscriptionAsync(Guid userId, ProductTier tier, DateTime periodEndUtc)
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
                StripeCustomerId = "cus_authz_matrix",
                StripeSubscriptionId = "sub_authz_matrix",
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Subscriptions.Add(subscription);
        }

        subscription.Tier = tier;
        subscription.ProductCode = tier.ToString().ToLowerInvariant();
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-1);
        subscription.CurrentPeriodEndUtc = periodEndUtc;
        subscription.CancelAtPeriodEnd = false;
        subscription.StripePriceId = tier == ProductTier.Commander ? "price_commander" : "price_operator";
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
