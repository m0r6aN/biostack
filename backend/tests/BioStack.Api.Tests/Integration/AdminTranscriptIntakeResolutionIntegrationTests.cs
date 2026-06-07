namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class AdminTranscriptIntakeResolutionIntegrationTests : IAsyncLifetime
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
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-admin-transcript-resolution-{Guid.NewGuid():N}.db");
        _factory = BuildFactory(configureServices: null, enableProvider: false);

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
    public async Task DisabledByDefault_QueuedVideoUrl_ReturnsDeterministicDisabledResult()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-resolve-disabled@example.com");

        var intakeId = await CreateIntakeAsync(KnowledgeSourceType.VideoUrl, "https://www.youtube.com/watch?v=SpzHHYvCNGU");

        var response = await _client.PostAsync($"/api/v1/admin/knowledge-source-intake/{intakeId}/resolve-transcript", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ResolutionResponseDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(intakeId, payload!.IntakeRequestId);
        Assert.Equal("failed", payload.Status);
        Assert.Equal("transcript_provider_disabled", payload.ResultCode);
        Assert.Null(payload.SegmentCount);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Equal(0, await db.KnowledgeEntries.CountAsync());
        Assert.Equal(0, await db.Set<StagedTranscriptCandidateReviewEntity>().CountAsync());
    }

    [Fact]
    public async Task NonQueuedIntake_RejectedBeforeProviderCall()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-resolve-nonqueued@example.com");

        var intakeId = await CreateIntakeAsync(KnowledgeSourceType.VideoUrl, "https://www.youtube.com/watch?v=SpzHHYvCNGU");
        await UpdateIntakeStatusAsync(intakeId, "processing");

        var response = await _client.PostAsync($"/api/v1/admin/knowledge-source-intake/{intakeId}/resolve-transcript", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MessageDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Contains("Only queued intake requests are supported", payload!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnsupportedSourceType_RejectedBeforeProviderCall()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-resolve-unsupported@example.com");

        var intakeId = await CreateIntakeAsync(
            KnowledgeSourceType.ChannelUrl,
            "https://www.youtube.com/@HubermanLab",
            new ChannelIngestionOptions(5, null, null));

        var response = await _client.PostAsync($"/api/v1/admin/knowledge-source-intake/{intakeId}/resolve-transcript", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MessageDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Contains("Unsupported transcript source type", payload!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingIntakeId_ReturnsBadRequest_WithSafeMessage()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-resolve-missing@example.com");

        var missingId = Guid.NewGuid();
        var response = await _client.PostAsync(
            $"/api/v1/admin/knowledge-source-intake/{missingId}/resolve-transcript",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MessageDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Contains("was not found", payload!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnabledProviderPath_UsesMockedProviderOnly_AndReturnsReviewSafeResult()
    {
        var enabledDbPath = Path.Combine(Path.GetTempPath(), $"biostack-admin-transcript-resolution-enabled-{Guid.NewGuid():N}.db");
        await using var enabledFactory = BuildFactory(services =>
        {
            services.RemoveAll<ITranscriptSourceMaterialProvider>();
            services.AddScoped<ITranscriptSourceMaterialProvider, FakeEnabledTranscriptSourceMaterialProvider>();
        }, enableProvider: true, dbPathOverride: enabledDbPath);

        using var enabledClient = enabledFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await AdminAuthTestHelper.SignInAsAdminAsync(enabledClient, enabledFactory, "admin-resolve-enabled@example.com");

        var intakeRequest = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=fake-enabled",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata },
            ChannelOptions: null);

        var createResponse = await enabledClient.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", intakeRequest, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var intakePayload = await createResponse.Content.ReadFromJsonAsync<IntakeResponseDto>(JsonOptions);
        Assert.NotNull(intakePayload);

        var response = await enabledClient.PostAsync(
            $"/api/v1/admin/knowledge-source-intake/{intakePayload!.IntakeRequestId}/resolve-transcript",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ResolutionResponseDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("resolved", payload!.Status);
        Assert.Equal("ok", payload.ResultCode);
        Assert.Equal("fake_enabled_provider", payload.Provider);
        Assert.Equal(2, payload.SegmentCount);
        Assert.True(payload.IsDeterministicFixture);
        Assert.NotNull(payload.ProviderMetadata);
        Assert.True(payload.ProviderMetadata!.ContainsKey("videoId"));
        Assert.False(payload.ProviderMetadata.Keys.Any(k => k.Contains("canonical", StringComparison.OrdinalIgnoreCase)));
        Assert.False(payload.ProviderMetadata.Keys.Any(k => k.Contains("promotion", StringComparison.OrdinalIgnoreCase)));
        Assert.False(payload.ProviderMetadata.Keys.Any(k => k.Contains("safety", StringComparison.OrdinalIgnoreCase)));

        using var scope = enabledFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Equal(0, await db.KnowledgeEntries.CountAsync());
        Assert.Equal(1, await db.Set<StagedTranscriptCandidateReviewEntity>().CountAsync());

        var reviewStore = scope.ServiceProvider.GetRequiredService<ITranscriptCandidateReviewStore>();
        var pendingRecords = await reviewStore.ListByReviewStateAsync(
            TranscriptCandidateReviewState.PendingReview, CancellationToken.None);
        Assert.Single(pendingRecords);

        var staged = pendingRecords[0];
        Assert.StartsWith("transcript-candidate:", staged.ArtifactId, StringComparison.Ordinal);
        Assert.Equal(TranscriptCandidateReviewRecord.NonCanonical, staged.Canonicality);
        Assert.Equal(TranscriptCandidateReviewState.PendingReview, staged.ReviewState);
        Assert.Equal("video_url", staged.SourceType);
        Assert.Equal("https://www.youtube.com/watch?v=fake-enabled", staged.SourceUrl);
        Assert.Equal("fake_enabled_provider", staged.Provider);
        Assert.True(staged.IsDeterministicFixture);
        Assert.Equal(2, staged.SegmentCount);
        Assert.True(staged.SourceMetadata.ContainsKey("videoId"));
        Assert.False(staged.SourceMetadata.Keys.Any(k =>
            k.Contains("canonical", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("promotion", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("summary", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("safety", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("medical", StringComparison.OrdinalIgnoreCase)));

        try
        {
            if (File.Exists(enabledDbPath))
            {
                File.Delete(enabledDbPath);
            }
        }
        catch (IOException)
        {
        }
    }

    private WebApplicationFactory<Program> BuildFactory(
        Action<IServiceCollection>? configureServices,
        bool enableProvider,
        string? dbPathOverride = null)
    {
        var dbPath = dbPathOverride ?? _dbPath;
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}",
                        ["FrontendUrl"] = "http://localhost:3043",
                        ["PublicApiUrl"] = "http://localhost:5000",
                        ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
                        ["Stripe:OperatorPriceId"] = "price_operator",
                        ["Stripe:CommanderPriceId"] = "price_commander",
                        ["Stripe:WebhookSecret"] = "whsec_test",
                        ["TranscriptProviders:YouTube:Enabled"] = enableProvider ? "true" : "false"
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveBioStackDbContext();
                    services.AddDbContext<BioStackDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
                    configureServices?.Invoke(services);
                });
            });
    }

    private async Task<Guid> CreateIntakeAsync(
        KnowledgeSourceType sourceType,
        string sourceUrl,
        ChannelIngestionOptions? channelOptions = null)
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: sourceType,
            SourceUrl: sourceUrl,
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata },
            ChannelOptions: channelOptions);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IntakeResponseDto>(JsonOptions);
        Assert.NotNull(payload);
        return payload!.IntakeRequestId;
    }

    private async Task UpdateIntakeStatusAsync(Guid intakeId, string status)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var entity = await db.KnowledgeSourceIntakeRequests.SingleAsync(x => x.Id == intakeId);
        entity.Status = status;
        await db.SaveChangesAsync();
    }

    private sealed class FakeEnabledTranscriptSourceMaterialProvider : ITranscriptSourceMaterialProvider
    {
        public Task<TranscriptSourceMaterialResult> ResolveAsync(
            TranscriptSourceReference sourceReference,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["videoId"] = "fake-enabled",
                ["source"] = "fake-provider"
            };

            var result = new TranscriptSourceMaterialResult(
                SourceReference: sourceReference,
                Provider: "fake_enabled_provider",
                Segments:
                [
                    new TranscriptSegment(1, "alpha", 0.0, 1.0),
                    new TranscriptSegment(2, "beta", 1.0, 1.1)
                ],
                RetrievedAtIsoUtc: "2026-01-01T00:00:00Z",
                Metadata: metadata,
                IsDeterministicFixture: true);

            return Task.FromResult(result);
        }
    }

    private sealed class IntakeResponseDto
    {
        public Guid IntakeRequestId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class ResolutionResponseDto
    {
        public Guid IntakeRequestId { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ResultCode { get; set; } = string.Empty;
        public int? SegmentCount { get; set; }
        public bool? IsDeterministicFixture { get; set; }
        public Dictionary<string, string>? ProviderMetadata { get; set; }
    }

    private sealed class MessageDto
    {
        public string Message { get; set; } = string.Empty;
    }
}
