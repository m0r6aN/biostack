namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Api.Auth;
using BioStack.Application.Services;
using BioStack.Cognition.CollectiveApi;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Persistence;
using Keon.Collective;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class CollectiveOutboundAuthorizationIntegrationTests : IAsyncLifetime
{
    private const string CollectiveClientName = "keon-collective";

    private readonly RecordingHandler _collectiveHandler = new();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-collective-auth-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("KeonCollective:LiveMode", "true");
                builder.UseSetting("KeonCollective:ControlBaseUrl", "https://p08-collective.invalid");
                builder.UseSetting("KeonCollective:PollDelayMs", "0");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                        ["Database:Provider"] = "sqlite",
                        ["FrontendUrl"] = "http://localhost:3043",
                        ["PublicApiUrl"] = "http://localhost:5000",
                        ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.UseTestKeonRuntimeClient();
                    services.RemoveBioStackDbContext();
                    services.AddDbContext<BioStackDbContext>(options =>
                        options.UseSqlite($"Data Source={_dbPath}"));
                });
                builder.ConfigureTestServices(services =>
                {
                    services.AddHttpClient(CollectiveClientName)
                        .ConfigurePrimaryHttpMessageHandler(() => _collectiveHandler);
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
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
    public async Task Stack_review_uses_current_user_consent_before_collective_submit()
    {
        await SignInAsync("p08-collective@example.com");

        using (var scope = _factory.Services.CreateScope())
        {
            Assert.IsType<CollectiveOutboundAuthorizationGate>(
                scope.ServiceProvider.GetRequiredService<ICollectiveOutboundAuthorizationGate>());
            Assert.IsType<ConsentGate>(scope.ServiceProvider.GetRequiredService<IConsentGate>());
            Assert.IsType<HttpContextCurrentUserAccessor>(
                scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>());
            Assert.Equal(
                "CollectiveLiveOrchestrator",
                scope.ServiceProvider.GetRequiredService<ICognitiveDensityOrchestrator>()
                    .GetType()
                    .Name);
        }

        var beforeConsent = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope",
            BuildSyntheticRequest());

        Assert.Equal(HttpStatusCode.OK, beforeConsent.StatusCode);
        Assert.Empty(_collectiveHandler.Requests);

        var consent = await _client.PostAsJsonAsync("/api/v1/consent", new { });
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
        var consentStatus = await consent.Content.ReadFromJsonAsync<ConsentStatusResponse>();
        Assert.NotNull(consentStatus);
        Assert.True(consentStatus.Accepted);

        var afterConsent = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope",
            BuildSyntheticRequest());

        Assert.Equal(HttpStatusCode.OK, afterConsent.StatusCode);
        var request = Assert.Single(_collectiveHandler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/collective/live-runs", request.Path);
    }

    private async Task SignInAsync(string email)
    {
        await _client.PostAsJsonAsync(
            "/api/v1/auth/start",
            new StartAuthRequest(email, "email", "/profiles"));
        using var document = await JsonDocument.ParseAsync(
            await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = document.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        var response = await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static StackReviewRequest BuildSyntheticRequest() =>
        new(
            new StackReviewEnvelopePayload(
                Goal: "P08 synthetic local-only review",
                Compounds:
                [
                    new("p08-synthetic", "P08 Synthetic", "test", "Synthetic", "None"),
                ],
                Pathways: ["synthetic-pathway"],
                DeterministicFindings:
                [
                    new(
                        "P08-SYNTHETIC",
                        "Synthetic",
                        "Synthetic local-only finding",
                        ["p08-synthetic"],
                        0m),
                ],
                KnownPatternNames: [],
                ProviderReviewPressure: 0m));

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri?.AbsolutePath ?? string.Empty));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    dataMode = "SYNTHETIC",
                    retrievalMode = "LOCAL_FAKE",
                    run = new
                    {
                        intentId = "p08-synthetic-intent",
                        correlationId = "p08-synthetic-correlation",
                        cognitionSurfaces = new
                        {
                            perspectiveReviews = new Dictionary<string, object>(),
                            contradictionReview = (object?)null,
                            confidenceProfile = new
                            {
                                model = "P08_SYNTHETIC_MODEL",
                                epistemic = "synthetic",
                                evidenceSupport = "synthetic",
                                contradictionDensity = "synthetic",
                                calibrationVersion = "synthetic",
                            },
                            reasoningGraphRef = new
                            {
                                graphId = "p08-synthetic-graph",
                                nodeCount = 0,
                                edgeCount = 0,
                            },
                            witnessSignature = "p08-synthetic-signature",
                        },
                    },
                    operatorMessages = Array.Empty<object>(),
                }),
            });
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, string Path);
}
