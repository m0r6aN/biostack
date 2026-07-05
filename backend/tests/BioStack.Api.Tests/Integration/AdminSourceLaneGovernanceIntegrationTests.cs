namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
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
public sealed class AdminSourceLaneGovernanceIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-admin-source-governance-{Guid.NewGuid():N}.db");
        _factory = BuildFactory(enableProvider: true);
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
    public async Task IntakeResolution_EmitsReceipts_AdvancesLifecycle_AndLinksStagedCandidate()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-source-lane-receipts@example.com");

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=source-lane-governed",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata },
            ChannelOptions: null);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/admin/knowledge-source-intake", request, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var intake = await createResponse.Content.ReadFromJsonAsync<IntakeResponseDto>(JsonOptions);
        Assert.NotNull(intake);
        Assert.Contains("source.intake.received", await ReceiptClassesAsync());

        var resolveResponse = await _client.PostAsync(
            $"/api/v1/admin/knowledge-source-intake/{intake!.IntakeRequestId}/resolve-transcript",
            content: null);
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var intakeEntity = await db.KnowledgeSourceIntakeRequests.SingleAsync(x => x.Id == intake.IntakeRequestId);
        Assert.Equal("resolved", intakeEntity.Status);
        Assert.Null(intakeEntity.FailureReason);
        Assert.NotNull(intakeEntity.UpdatedAtUtc);

        var staged = await db.Set<StagedTranscriptCandidateReviewEntity>().SingleAsync();
        Assert.Equal(intake.IntakeRequestId, staged.IntakeRequestId);

        var classes = await ReceiptClassesAsync();
        Assert.Contains("source.transcript.resolved", classes);
        Assert.Contains("source.candidate.staged", classes);
    }

    [Fact]
    public async Task ReviewStateAndPromotion_EmitSourceLaneReceipts()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-source-lane-promotion@example.com");
        const string artifactId = "transcript-candidate:source-lane-promo-1";
        const string canonicalName = "source-lane-promo-caffeine";
        var knowledgeEntryId = await SeedKnowledgeEntryAsync(canonicalName);
        await SeedReviewAsync(artifactId);

        var reviewResponse = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/review-state",
            new { action = "approve_for_promotion" });
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        var targetResponse = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-target",
            new { targetCanonicalName = canonicalName });
        Assert.Equal(HttpStatusCode.OK, targetResponse.StatusCode);

        var promotionResponse = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/execute-promotion",
            content: null);
        Assert.Equal(HttpStatusCode.OK, promotionResponse.StatusCode);
        var payload = await promotionResponse.Content.ReadFromJsonAsync<AdminStagedReviewDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(knowledgeEntryId, payload!.PromotedKnowledgeEntryId);

        var classes = await ReceiptClassesAsync();
        Assert.Contains("source.review-state.changed", classes);
        Assert.Contains("source.artifact.promoted", classes);
    }

    [Fact]
    public async Task AdminKnowledgeIngest_IsFencedOffByDefault()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-source-lane-ingest-fence@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/admin/knowledge/ingest",
            new[]
            {
                new KnowledgeEntry
                {
                    Id = Guid.NewGuid(),
                    CanonicalName = "bypass-fence-test",
                    SourceReferences = new List<string> { "https://example.test/source" },
                },
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.False(await db.KnowledgeEntries.AnyAsync(x => x.CanonicalName == "bypass-fence-test"));
    }

    private WebApplicationFactory<Program> BuildFactory(bool enableProvider)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                        ["FrontendUrl"] = "http://localhost:3043",
                        ["PublicApiUrl"] = "http://localhost:5000",
                        ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
                        ["Stripe:OperatorPriceId"] = "price_operator",
                        ["Stripe:CommanderPriceId"] = "price_commander",
                        ["Stripe:WebhookSecret"] = "whsec_test",
                        ["TranscriptProviders:YouTube:Enabled"] = enableProvider ? "true" : "false",
                    }));
                builder.ConfigureServices(services =>
                {
                    services.RemoveBioStackDbContext();
                    services.AddDbContext<BioStackDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
                    services.RemoveAll<ITranscriptSourceMaterialProvider>();
                    services.AddScoped<ITranscriptSourceMaterialProvider, FakeEnabledTranscriptSourceMaterialProvider>();
                });
            });

    private async Task<List<string>> ReceiptClassesAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.SpineEntries
            .OrderBy(e => e.CreatedAt)
            .Select(e => e.ReceiptClass)
            .ToListAsync();
    }

    private async Task SeedReviewAsync(string artifactId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<ITranscriptCandidateReviewStore>();
        var record = TranscriptCandidateReviewRecord.Create(
            artifactId: artifactId,
            canonicality: TranscriptCandidateReviewRecord.NonCanonical,
            reviewState: TranscriptCandidateReviewState.PendingReview,
            sourceType: "video_url",
            sourceUrl: $"https://example.test/{artifactId}",
            provider: "fixture",
            isDeterministicFixture: false,
            segmentCount: 2,
            segmentSnapshotSignature: artifactId.Replace("transcript-candidate:", "", StringComparison.Ordinal),
            sourceMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["evidenceTier"] = "observational",
                ["citations"] = "https://example.test/study-1",
            },
            createdAtUtc: "2026-07-05T00:00:00Z",
            updatedAtUtc: "2026-07-05T00:00:00Z");
        await store.UpsertAsync(record);
    }

    private async Task<Guid> SeedKnowledgeEntryAsync(string canonicalName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var knowledgeSource = scope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
        var entry = new KnowledgeEntry
        {
            Id = Guid.NewGuid(),
            CanonicalName = canonicalName,
            SourceReferences = new List<string>(),
        };
        await knowledgeSource.UpsertCompoundAsync(entry);
        return entry.Id;
    }

    private sealed class FakeEnabledTranscriptSourceMaterialProvider : ITranscriptSourceMaterialProvider
    {
        public Task<TranscriptSourceMaterialResult> ResolveAsync(
            TranscriptSourceReference sourceReference,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new TranscriptSourceMaterialResult(
                SourceReference: sourceReference,
                Provider: "fake_enabled_provider",
                Segments: new[]
                {
                    new TranscriptSegment(1, "alpha", 0.0, 1.0),
                    new TranscriptSegment(2, "beta", 1.0, 1.0),
                },
                RetrievedAtIsoUtc: "2026-07-05T00:00:00Z",
                Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["videoId"] = "source-lane-governed",
                    ["provider"] = "fake",
                },
                IsDeterministicFixture: true));
        }
    }

    private sealed class IntakeResponseDto
    {
        public Guid IntakeRequestId { get; set; }
    }

    private sealed class AdminStagedReviewDto
    {
        public Guid? PromotedKnowledgeEntryId { get; set; }
    }
}
