namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class AdminPromotionPreviewIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-promo-preview-{Guid.NewGuid():N}.db");
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
                        ["FrontendUrl"]   = "http://localhost:3043",
                        ["PublicApiUrl"]  = "http://localhost:5000",
                        ["Jwt:Secret"]    = "test-secret-value-that-is-long-enough-for-hmac",
                        ["Stripe:OperatorPriceId"]  = "price_operator",
                        ["Stripe:CommanderPriceId"] = "price_commander",
                        ["Stripe:WebhookSecret"]    = "whsec_test",
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
                File.Delete(_dbPath);
        }
        catch (IOException) { }
    }

    // ---------------------------------------------------------------------------
    // Test 1 — Happy path: 200, canPromote=true, wouldWrite=false
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PreviewPromotion_HappyPath_Returns200_CanPromoteTrue_WouldWriteFalse()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-preview-happy@example.com");
        const string artifactId = "transcript-candidate:preview-happy-1";
        const string canonicalName = "tb-500-preview-happy-1";
        var keId = await SeedKnowledgeEntryAsync(canonicalName);
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: canonicalName,
            metadata: ValidEvidenceMetadata());

        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-preview",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PromotionPreviewDto>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload!.ArtifactId);
        Assert.True(payload.CanPromote);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, payload.ReviewState);
        Assert.True(payload.TargetAssigned);
        Assert.Equal(canonicalName, payload.TargetCanonicalName);
        Assert.Equal(keId, payload.ResolvedTargetKnowledgeEntryId);
        Assert.False(payload.AlreadyPromoted);
        Assert.Null(payload.PromotedKnowledgeEntryId);
        Assert.True(payload.EvidenceGate.Passed);
        Assert.Empty(payload.BlockingReasons);
        Assert.False(payload.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 2 — Unknown artifactId: 404
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PreviewPromotion_UnknownArtifactId_Returns404()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-preview-404@example.com");

        var response = await _client.PostAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:preview-does-not-exist/promotion-preview",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------------------------------------------------------------------
    // Test 3 — Wrong state: 200 with canPromote=false and state blocking reason
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PreviewPromotion_PendingReview_Returns200_CanPromoteFalse_WithStateReason()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-preview-pending@example.com");
        const string artifactId = "transcript-candidate:preview-pending-1";
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false,
            targetCanonicalName: "tb-500-preview-pending",
            metadata: ValidEvidenceMetadata());

        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-preview",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PromotionPreviewDto>();
        Assert.NotNull(payload);
        Assert.False(payload!.CanPromote);
        Assert.Contains("review_state_not_approved", payload.BlockingReasons);
        Assert.False(payload.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 4 — Evidence gate failure: 200 with canPromote=false, gate.passed=false
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PreviewPromotion_EvidenceGateFailure_Returns200_CanPromoteFalse_GateFailed()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-preview-gate@example.com");
        const string artifactId = "transcript-candidate:preview-gate-1";
        // No citations → gate rejects with missing_citations
        var badMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = "observational",
            // citations intentionally omitted
        };
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: "tb-500-preview-gate",
            metadata: badMetadata);

        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-preview",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PromotionPreviewDto>();
        Assert.NotNull(payload);
        Assert.False(payload!.CanPromote);
        Assert.False(payload.EvidenceGate.Passed);
        Assert.Contains("missing_citations", payload.EvidenceGate.FailureReasons);
        Assert.Contains("missing_citations", payload.BlockingReasons);
        Assert.False(payload.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 5 — Unauthenticated: 401 or 403
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PreviewPromotion_Unauthenticated_ReturnsUnauthorizedOrForbidden()
    {
        // No sign-in — raw client, no session cookie.
        var response = await _client.PostAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:any/promotion-preview",
            null);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    // ---------------------------------------------------------------------------
    // Test 6 — Side-effect freedom: preview does not stamp promotedKnowledgeEntryId
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PreviewPromotion_DoesNotMutateRecord_VerifiedByFollowUpGet()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-preview-no-mutate@example.com");
        const string artifactId = "transcript-candidate:preview-no-mutate-1";
        const string canonicalName = "tb-500-preview-no-mutate-1";
        await SeedKnowledgeEntryAsync(canonicalName);
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: canonicalName,
            metadata: ValidEvidenceMetadata());

        // Call the preview endpoint.
        var previewResponse = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-preview",
            null);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var previewPayload = await previewResponse.Content.ReadFromJsonAsync<PromotionPreviewDto>();
        Assert.NotNull(previewPayload);
        Assert.True(previewPayload!.CanPromote);
        Assert.False(previewPayload.WouldWrite);

        // Verify the record was NOT mutated — no promotion stamps should exist.
        var getResponse = await _client.GetAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var record = await getResponse.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(record);
        Assert.Null(record!.PromotedKnowledgeEntryId);
        Assert.Null(record.PromotedAtUtc);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, record.ReviewState);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private async Task SeedReviewAsync(
        string artifactId,
        string reviewState,
        bool isDeterministicFixture,
        string? targetCanonicalName = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        Guid? promotedKnowledgeEntryId = null,
        string? promotedAtUtc = null)
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ITranscriptCandidateReviewStore>();

        var record = TranscriptCandidateReviewRecord.Create(
            artifactId: artifactId,
            canonicality: TranscriptCandidateReviewRecord.NonCanonical,
            reviewState: reviewState,
            sourceType: "video",
            sourceUrl: $"https://example.test/{artifactId}",
            provider: "fixture",
            isDeterministicFixture: isDeterministicFixture,
            segmentCount: 4,
            segmentSnapshotSignature: artifactId.Replace("transcript-candidate:", "", StringComparison.Ordinal),
            sourceMetadata: metadata ?? new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["source"] = "fixture",
                ["kind"]   = "transcript",
            },
            createdAtUtc: "2026-05-30T00:00:00Z",
            updatedAtUtc: "2026-05-30T00:00:00Z",
            targetCanonicalName: targetCanonicalName,
            promotedKnowledgeEntryId: promotedKnowledgeEntryId,
            promotedAtUtc: promotedAtUtc);

        await store.UpsertAsync(record);
    }

    private async Task<Guid> SeedKnowledgeEntryAsync(string canonicalName)
    {
        using var scope = _factory.Services.CreateScope();
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

    private static IReadOnlyDictionary<string, string> ValidEvidenceMetadata()
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = "observational",
            ["citations"]    = "https://example.test/study-1",
        };

    // ---------------------------------------------------------------------------
    // Local DTOs for deserializing the preview response
    // ---------------------------------------------------------------------------

    private sealed class PromotionPreviewDto
    {
        public string ArtifactId { get; set; } = string.Empty;
        public bool CanPromote { get; set; }
        public string ReviewState { get; set; } = string.Empty;
        public bool TargetAssigned { get; set; }
        public string? TargetCanonicalName { get; set; }
        public Guid? ResolvedTargetKnowledgeEntryId { get; set; }
        public bool AlreadyPromoted { get; set; }
        public Guid? PromotedKnowledgeEntryId { get; set; }
        public EvidenceGateDto EvidenceGate { get; set; } = new();
        public List<string> BlockingReasons { get; set; } = new();
        public bool WouldWrite { get; set; }
    }

    private sealed class EvidenceGateDto
    {
        public bool Passed { get; set; }
        public string? Tier { get; set; }
        public int CitationCount { get; set; }
        public bool MechanismSummaryPresent { get; set; }
        public List<string> FailureReasons { get; set; } = new();
    }

    private sealed class AdminStagedReviewDto
    {
        public string ArtifactId { get; set; } = string.Empty;
        public string Canonicality { get; set; } = string.Empty;
        public string ReviewState { get; set; } = string.Empty;
        public Guid? PromotedKnowledgeEntryId { get; set; }
        public string? PromotedAtUtc { get; set; }
        public string? TargetCanonicalName { get; set; }

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, JsonElement> Extra { get; set; } = new(StringComparer.Ordinal);
    }
}
