namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class AdminStagedTranscriptCandidateReviewIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-admin-staged-review-{Guid.NewGuid():N}.db");
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
    public async Task AdminCanListByReviewState_AndResponsePreservesStagedBoundary()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-list@example.com");
        await SeedReviewAsync(
            artifactId: "transcript-candidate:sig-list-1",
            reviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["kind"] = "transcript",
                ["source"] = "fixture",
            });
        await SeedReviewAsync(
            artifactId: "transcript-candidate:sig-list-2",
            reviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion);
        await SeedReviewAsync(
            artifactId: "transcript-candidate:sig-list-other",
            reviewState: TranscriptCandidateReviewState.ReviewDeferred);

        var response = await _client.GetAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews?reviewState={TranscriptCandidateReviewState.ReviewApprovedForPromotion}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);

        Assert.All(payload, item =>
        {
            Assert.Equal("non_canonical", item.Canonicality);
            Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, item.ReviewState);
            Assert.DoesNotContain("knowledgeEntryId", item.ExtraKeys, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("knowledgeEntryFk", item.ExtraKeys, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("knowledgeEntryIdFk", item.ExtraKeys, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("canonicalKnowledgeEntryId", item.ExtraKeys, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("promotionStatus", item.ExtraKeys, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("promotionExecutedAtUtc", item.ExtraKeys, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("promotionExecutionId", item.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task AdminCanGetByArtifactId()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-get@example.com");
        const string artifactId = "transcript-candidate:sig-get-1";
        await SeedReviewAsync(
            artifactId: artifactId,
            reviewState: TranscriptCandidateReviewState.PendingReview);

        var response = await _client.GetAsync($"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload!.ArtifactId);
        Assert.Equal("non_canonical", payload.Canonicality);
        Assert.Equal(TranscriptCandidateReviewState.PendingReview, payload.ReviewState);
        Assert.DoesNotContain("promotionStatus", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotionExecutedAtUtc", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotionExecutionId", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("knowledgeEntryId", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("canonicalKnowledgeEntryId", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownArtifact_ReturnsNotFound()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-missing@example.com");

        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MissingReviewStateQuery_ReturnsBadRequest()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-badrequest@example.com");

        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnauthorizedRequest_ReturnsUnauthorizedOrForbidden()
    {
        var response = await _client.GetAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews?reviewState={TranscriptCandidateReviewState.PendingReview}");

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    [Fact]
    public async Task NonAdminRequest_ReturnsUnauthorizedOrForbidden()
    {
        await SignInNonAdminAsync("non-admin-review@example.com");

        var response = await _client.GetAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews?reviewState={TranscriptCandidateReviewState.PendingReview}");

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    // ── POST /review-state — 8 tests (PR13A) ────────────────────────────────

    [Fact]
    public async Task UpdateReviewState_ApproveForPromotion_OnPendingReview_Returns200AndUpdatedState()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-update-approve@example.com");
        const string artifactId = "transcript-candidate:sig-update-approve-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.PendingReview);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/review-state",
            new { action = TranscriptCandidateReviewAction.ApproveForPromotion });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload!.ArtifactId);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, payload.ReviewState);
        Assert.Equal("non_canonical", payload.Canonicality);
        // Staged boundary: response must not expose KnowledgeEntry linkage or promotion execution fields.
        Assert.DoesNotContain("knowledgeEntryId", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("canonicalKnowledgeEntryId", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotionStatus", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotionExecutedAtUtc", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotionExecutionId", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateReviewState_RejectReview_OnPendingReview_Returns200AndUpdatedState()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-update-reject@example.com");
        const string artifactId = "transcript-candidate:sig-update-reject-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.PendingReview);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/review-state",
            new { action = TranscriptCandidateReviewAction.RejectReview });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload!.ArtifactId);
        Assert.Equal(TranscriptCandidateReviewState.ReviewRejected, payload.ReviewState);
        Assert.Equal("non_canonical", payload.Canonicality);
    }

    [Fact]
    public async Task UpdateReviewState_DeferReview_OnPendingReview_Returns200AndUpdatedState()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-update-defer@example.com");
        const string artifactId = "transcript-candidate:sig-update-defer-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.PendingReview);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/review-state",
            new { action = TranscriptCandidateReviewAction.DeferReview });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload!.ArtifactId);
        Assert.Equal(TranscriptCandidateReviewState.ReviewDeferred, payload.ReviewState);
        Assert.Equal("non_canonical", payload.Canonicality);
    }

    [Fact]
    public async Task UpdateReviewState_UnknownArtifact_Returns404()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-update-404@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:does-not-exist/review-state",
            new { action = TranscriptCandidateReviewAction.ApproveForPromotion });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateReviewState_MissingAction_Returns400()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-update-400@example.com");
        const string artifactId = "transcript-candidate:sig-update-noaction-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.PendingReview);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/review-state",
            new { action = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateReviewState_MissingBody_Returns400()
    {
        // Distinct from MissingAction: this covers the `request is null` path
        // when no body is sent at all (empty JSON content), not `{ "action": null }`.
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-update-400-nobody@example.com");
        const string artifactId = "transcript-candidate:sig-update-nobody-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.PendingReview);

        var emptyJson = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/review-state",
            emptyJson);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateReviewState_UnsupportedAction_Returns422()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-update-422-unsupported@example.com");
        const string artifactId = "transcript-candidate:sig-update-badaction-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.PendingReview);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/review-state",
            new { action = "promote_immediately" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UpdateReviewState_TransitionFromTerminalState_Returns422()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-update-422-terminal@example.com");
        const string artifactId = "transcript-candidate:sig-update-terminal-1";
        // Seed already in a terminal state (approved).
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.ReviewApprovedForPromotion);

        // Attempting any action from a terminal state must be rejected by the lifecycle.
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/review-state",
            new { action = TranscriptCandidateReviewAction.ApproveForPromotion });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UpdateReviewState_Unauthenticated_ReturnsUnauthorizedOrForbidden()
    {
        // No sign-in — raw client, no session cookie.
        var response = await _client.PostAsJsonAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:any/review-state",
            new { action = TranscriptCandidateReviewAction.ApproveForPromotion });

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    private async Task SeedReviewAsync(
        string artifactId,
        string reviewState,
        IReadOnlyDictionary<string, string>? metadata = null)
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
            isDeterministicFixture: true,
            segmentCount: 4,
            segmentSnapshotSignature: artifactId.Replace("transcript-candidate:", "", StringComparison.Ordinal),
            sourceMetadata: metadata ?? new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["source"] = "fixture",
                ["kind"] = "transcript",
            },
            createdAtUtc: "2026-05-30T00:00:00Z",
            updatedAtUtc: "2026-05-30T00:00:00Z");

        await store.UpsertAsync(record);
    }

    private async Task SignInNonAdminAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/admin"));
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

    private sealed class AdminStagedReviewDto
    {
        public string ArtifactId { get; set; } = string.Empty;
        public string Canonicality { get; set; } = string.Empty;
        public string ReviewState { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public bool IsDeterministicFixture { get; set; }
        public int SegmentCount { get; set; }
        public string SegmentSnapshotSignature { get; set; } = string.Empty;
        public Dictionary<string, string> SourceMetadata { get; set; } = new(StringComparer.Ordinal);
        public string CreatedAtUtc { get; set; } = string.Empty;
        public string UpdatedAtUtc { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, JsonElement> Extra { get; set; } = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> ExtraKeys => Extra.Keys;
    }
}
