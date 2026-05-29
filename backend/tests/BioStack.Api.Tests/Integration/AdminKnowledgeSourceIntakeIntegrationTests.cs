namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Contracts.Requests;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class AdminKnowledgeSourceIntakeIntegrationTests : IAsyncLifetime
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
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-admin-intake-{Guid.NewGuid():N}.db");
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
    public async Task VideoUrl_HappyPath_PersistsQueuedIntake()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-video@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=SpzHHYvCNGU",
            OptionalInstructions: "Focus on compounds and claims",
            RequestedOutputs: new[]
            {
                RequestedOutputArea.Claims,
                RequestedOutputArea.CompoundsMentioned,
                RequestedOutputArea.SafetyFlags
            },
            ChannelOptions: null);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IntakeResponseDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("queued", payload!.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var entity = await db.KnowledgeSourceIntakeRequests.SingleAsync(x => x.Id == payload.IntakeRequestId);
        Assert.Equal("video_url", entity.SourceType);
        Assert.Equal("https://www.youtube.com/watch?v=SpzHHYvCNGU", entity.SourceUrl);
        Assert.Equal("Focus on compounds and claims", entity.OptionalInstructions);
        Assert.Contains("claims", entity.RequestedOutputs);
        Assert.Contains("compounds_mentioned", entity.RequestedOutputs);
        Assert.Contains("safety_flags", entity.RequestedOutputs);
        Assert.Equal("queued", entity.Status);
    }

    [Fact]
    public async Task ChannelUrl_HappyPath_PersistsChannelFilters()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-channel@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata, RequestedOutputArea.Claims },
            ChannelOptions: new ChannelIngestionOptions(
                MaxVideos: 25,
                PublishedAfterUtc: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PublishedBeforeUtc: new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero)));

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IntakeResponseDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("queued", payload!.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var entity = await db.KnowledgeSourceIntakeRequests.SingleAsync(x => x.Id == payload.IntakeRequestId);
        Assert.Equal("channel_url", entity.SourceType);
        Assert.Equal(25, entity.MaxVideos);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), entity.PublishedAfterUtc);
        Assert.Equal(new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero), entity.PublishedBeforeUtc);
        Assert.Equal("queued", entity.Status);
    }

    [Fact]
    public async Task UnauthorizedRequest_ReturnsUnauthorizedOrForbidden()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=SpzHHYvCNGU",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    [Fact]
    public async Task NonAdminRequest_ReturnsUnauthorizedOrForbidden()
    {
        await SignInNonAdminAsync("non-admin-intake@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=SpzHHYvCNGU",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    [Fact]
    public async Task InvalidVideoUrl_ReturnsBadRequest()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-invalid-video@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidChannelUrl_ReturnsBadRequest()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-invalid-channel@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/watch?v=SpzHHYvCNGU",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MismatchedSourceTypeAndUrl_ReturnsBadRequest()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-mismatch@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/channel/UC2D2CMWXMOVWx7giW1n3LIg",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MaxVideosOutOfBounds_ReturnsBadRequest()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-maxvideos@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: new ChannelIngestionOptions(
                MaxVideos: 999,
                PublishedAfterUtc: null,
                PublishedBeforeUtc: null));

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidDateRange_ReturnsBadRequest()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-daterange@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: new ChannelIngestionOptions(
                MaxVideos: 5,
                PublishedAfterUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PublishedBeforeUtc: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    private async Task SignInNonAdminAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/admin"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement
            .EnumerateArray()
            .First(message =>
                string.Equals(
                    message.GetProperty("contact").GetString(),
                    email,
                    StringComparison.OrdinalIgnoreCase))
            .GetProperty("link")
            .GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
    }

    private sealed class IntakeResponseDto
    {
        public Guid IntakeRequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
