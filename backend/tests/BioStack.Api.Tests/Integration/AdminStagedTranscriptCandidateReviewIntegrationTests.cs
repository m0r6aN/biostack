namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
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
    public async Task List_NoQueryParams_Returns200WithAllRecords()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-allrecords@example.com");
        await SeedReviewAsync("transcript-candidate:all-1", TranscriptCandidateReviewState.PendingReview);
        await SeedReviewAsync("transcript-candidate:all-2", TranscriptCandidateReviewState.ReviewDeferred);

        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
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

    // ── GET /staged-transcript-candidate-reviews — PR13B additions ──────────

    [Fact]
    public async Task ListStagedTranscriptCandidateReviews_WhitespaceReviewState_Returns400()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-ws@example.com");

        // URL-encode a whitespace-only value; the handler's IsNullOrWhiteSpace guard returns 400.
        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews?reviewState=%20%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListStagedTranscriptCandidateReviews_ByReviewRejected_ReturnsOnlyRejectedRecords()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-rejected@example.com");
        await SeedReviewAsync("transcript-candidate:sig-rejected-1", TranscriptCandidateReviewState.ReviewRejected);
        await SeedReviewAsync("transcript-candidate:sig-rejected-2", TranscriptCandidateReviewState.ReviewRejected);
        await SeedReviewAsync("transcript-candidate:sig-rejected-pending", TranscriptCandidateReviewState.PendingReview);

        var response = await _client.GetAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews?reviewState={TranscriptCandidateReviewState.ReviewRejected}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.All(payload, item =>
            Assert.Equal(TranscriptCandidateReviewState.ReviewRejected, item.ReviewState));
    }

    [Fact]
    public async Task ListStagedTranscriptCandidateReviews_NoMatches_Returns200WithEmptyCollection()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-empty@example.com");

        // Use a valid but seeded-empty state value; the store returns an empty list → 200 with [].
        var response = await _client.GetAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews?reviewState={TranscriptCandidateReviewState.ReviewDeferred}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        Assert.Empty(payload!);
    }

    // ── GET /staged-transcript-candidate-reviews — PR15 additions (read-side observability) ──

    [Fact]
    public async Task List_PromotedTrue_ReturnsOnlyPromotedRecords()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-pr15-promoted-true@example.com");
        var promotedId = Guid.NewGuid();
        await SeedReviewAsync(
            "transcript-candidate:pr15-prom-yes-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: "caffeine",
            promotedKnowledgeEntryId: promotedId,
            promotedAtUtc: "2026-06-07T10:00:00Z");
        await SeedReviewAsync(
            "transcript-candidate:pr15-prom-no-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false);
        await SeedReviewAsync(
            "transcript-candidate:pr15-prom-no-2",
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false);

        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews?promoted=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        var item = Assert.Single(payload!);
        Assert.Equal(promotedId, item.PromotedKnowledgeEntryId);
    }

    [Fact]
    public async Task List_PromotedFalse_ReturnsOnlyUnpromotedRecords()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-pr15-promoted-false@example.com");
        await SeedReviewAsync(
            "transcript-candidate:pr15-unprom-promoted-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: "caffeine",
            promotedKnowledgeEntryId: Guid.NewGuid(),
            promotedAtUtc: "2026-06-07T10:00:00Z");
        await SeedReviewAsync(
            "transcript-candidate:pr15-unprom-pending-1",
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false);
        await SeedReviewAsync(
            "transcript-candidate:pr15-unprom-approved-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false);

        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews?promoted=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.All(payload, item => Assert.Null(item.PromotedKnowledgeEntryId));
    }

    [Fact]
    public async Task List_TargetAssignedTrue_ReturnsOnlyTargetAssignedRecords()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-pr15-target-true@example.com");
        await SeedReviewAsync(
            "transcript-candidate:pr15-target-yes-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: "bpc-157");
        await SeedReviewAsync(
            "transcript-candidate:pr15-target-no-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false);
        await SeedReviewAsync(
            "transcript-candidate:pr15-target-no-2",
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false);

        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews?targetAssigned=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        var item = Assert.Single(payload!);
        Assert.Equal("bpc-157", item.TargetCanonicalName);
    }

    [Fact]
    public async Task List_TargetAssignedFalse_ReturnsOnlyRecordsWithoutTarget()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-pr15-target-false@example.com");
        await SeedReviewAsync(
            "transcript-candidate:pr15-notarget-with-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: "tb-500");
        await SeedReviewAsync(
            "transcript-candidate:pr15-notarget-without-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false);
        await SeedReviewAsync(
            "transcript-candidate:pr15-notarget-without-2",
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false);

        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews?targetAssigned=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.All(payload, item => Assert.Null(item.TargetCanonicalName));
    }

    [Fact]
    public async Task List_CompoundFilter_ApprovedAndNotPromoted_ReturnsIntersection()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-pr15-compound@example.com");
        // Approved + promoted (should NOT appear)
        await SeedReviewAsync(
            "transcript-candidate:pr15-cmp-approved-promoted-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: "caffeine",
            promotedKnowledgeEntryId: Guid.NewGuid(),
            promotedAtUtc: "2026-06-07T10:00:00Z");
        // Approved + unpromoted (should appear)
        await SeedReviewAsync(
            "transcript-candidate:pr15-cmp-approved-unpromoted-1",
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false);
        // Pending + unpromoted (should NOT appear — state filter excludes it)
        await SeedReviewAsync(
            "transcript-candidate:pr15-cmp-pending-unpromoted-1",
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false);

        var response = await _client.GetAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews?reviewState={TranscriptCandidateReviewState.ReviewApprovedForPromotion}&promoted=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        var item = Assert.Single(payload!);
        Assert.Equal("transcript-candidate:pr15-cmp-approved-unpromoted-1", item.ArtifactId);
        Assert.Null(item.PromotedKnowledgeEntryId);
    }

    [Fact]
    public async Task List_ResultsOrderedByUpdatedAtDescending()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-pr15-ordering@example.com");
        await SeedReviewAsync(
            "transcript-candidate:pr15-ord-middle-1",
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false,
            updatedAtUtc: "2026-06-05T00:00:00Z");
        await SeedReviewAsync(
            "transcript-candidate:pr15-ord-oldest-1",
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false,
            updatedAtUtc: "2026-06-01T00:00:00Z");
        await SeedReviewAsync(
            "transcript-candidate:pr15-ord-newest-1",
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false,
            updatedAtUtc: "2026-06-07T00:00:00Z");

        var response = await _client.GetAsync("/api/v1/admin/staged-transcript-candidate-reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminStagedReviewDto>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload!.Count);
        // Results must arrive newest-first (UpdatedAtUtc descending).
        Assert.Equal("transcript-candidate:pr15-ord-newest-1",  payload[0].ArtifactId);
        Assert.Equal("transcript-candidate:pr15-ord-middle-1",  payload[1].ArtifactId);
        Assert.Equal("transcript-candidate:pr15-ord-oldest-1",  payload[2].ArtifactId);
    }

    [Fact]
    public async Task GetStagedTranscriptCandidateReview_ResponseDoesNotExposeCanonicalOrPromotionFields()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-review-boundary@example.com");
        const string artifactId = "transcript-candidate:sig-boundary-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.PendingReview);

        var response = await _client.GetAsync($"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(payload);
        // Review-safe boundary: none of the following canonical or promotion fields may appear.
        Assert.DoesNotContain("knowledgeEntryId",          payload!.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("knowledgeEntryFk",          payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("canonicalKnowledgeEntryId", payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotionStatus",           payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotionExecutedAtUtc",    payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotionExecutionId",      payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("summary",                   payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("safetyClassification",      payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("medicalInterpretation",     payload.ExtraKeys, StringComparer.OrdinalIgnoreCase);
        // Canonicality field must always read "non_canonical".
        Assert.Equal("non_canonical", payload.Canonicality);
    }

    [Fact]
    public async Task GetStagedTranscriptCandidateReview_Unauthenticated_ReturnsUnauthorizedOrForbidden()
    {
        // No sign-in — raw client, no session cookie.
        var response = await _client.GetAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:any-artifact");

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    // ── POST /review-state — 9 tests (PR13A) ────────────────────────────────

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

    // ── POST /promotion-target — 7 tests (PR14A) ────────────────────────────

    [Fact]
    public async Task AssignPromotionTarget_ApprovedNonFixtureRecord_Returns200AndSetsTargetCanonicalName()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-promo-target-happy@example.com");
        const string artifactId = "transcript-candidate:sig-promo-target-happy-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.ReviewApprovedForPromotion, isDeterministicFixture: false);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-target",
            new { targetCanonicalName = "caffeine" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload!.ArtifactId);
        Assert.Equal("caffeine", payload.TargetCanonicalName);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, payload.ReviewState);
        Assert.Equal("non_canonical", payload.Canonicality);
        Assert.Null(payload.PromotedKnowledgeEntryId);
        Assert.Null(payload.PromotedAtUtc);
    }

    [Fact]
    public async Task AssignPromotionTarget_WhitespaceTargetCanonicalName_Returns400()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-promo-target-ws@example.com");
        const string artifactId = "transcript-candidate:sig-promo-target-ws-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.ReviewApprovedForPromotion, isDeterministicFixture: false);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-target",
            new { targetCanonicalName = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignPromotionTarget_MissingBody_Returns400()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-promo-target-nobody@example.com");
        const string artifactId = "transcript-candidate:sig-promo-target-nobody-1";
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.ReviewApprovedForPromotion, isDeterministicFixture: false);

        var emptyJson = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-target",
            emptyJson);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignPromotionTarget_UnknownArtifactId_Returns404()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-promo-target-404@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:does-not-exist/promotion-target",
            new { targetCanonicalName = "caffeine" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignPromotionTarget_RecordNotInApprovedState_Returns409()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-promo-target-409-state@example.com");
        const string artifactId = "transcript-candidate:sig-promo-target-409-state-1";
        // Seed in pending_review — not eligible for promotion target assignment.
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.PendingReview, isDeterministicFixture: false);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-target",
            new { targetCanonicalName = "caffeine" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AssignPromotionTarget_DeterministicFixtureRecord_Returns409()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-promo-target-409-fixture@example.com");
        const string artifactId = "transcript-candidate:sig-promo-target-409-fixture-1";
        // Seed approved but deterministic fixture — fixtures cannot receive a promotion target.
        await SeedReviewAsync(artifactId, TranscriptCandidateReviewState.ReviewApprovedForPromotion, isDeterministicFixture: true);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/promotion-target",
            new { targetCanonicalName = "caffeine" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AssignPromotionTarget_Unauthenticated_ReturnsUnauthorizedOrForbidden()
    {
        // No sign-in — raw client, no session cookie.
        var response = await _client.PostAsJsonAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:any/promotion-target",
            new { targetCanonicalName = "caffeine" });

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    // ── POST /execute-promotion — 8 tests (PR14B) ───────────────────────────

    [Fact]
    public async Task ExecutePromotion_HappyPath_Returns200_AndStampsPromotionFields()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-exec-promo-happy@example.com");
        const string artifactId = "transcript-candidate:sig-exec-promo-happy-1";
        const string canonicalName = "caffeine-exec-happy-1";
        var keId = await SeedKnowledgeEntryAsync(canonicalName);
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: canonicalName);

        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/execute-promotion",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload!.ArtifactId);
        Assert.Equal(keId, payload.PromotedKnowledgeEntryId);
        Assert.NotNull(payload.PromotedAtUtc);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, payload.ReviewState);
    }

    [Fact]
    public async Task ExecutePromotion_IdempotentReplay_Returns200_WithSamePromotionResult()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-exec-promo-idempotent@example.com");
        const string artifactId = "transcript-candidate:sig-exec-promo-idempotent-1";
        const string canonicalName = "caffeine-exec-idempotent-1";
        var keId = await SeedKnowledgeEntryAsync(canonicalName);
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: canonicalName);

        var first = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/execute-promotion",
            null);
        var second = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/execute-promotion",
            null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var firstPayload = await first.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        var secondPayload = await second.Content.ReadFromJsonAsync<AdminStagedReviewDto>();
        Assert.NotNull(firstPayload);
        Assert.NotNull(secondPayload);
        Assert.Equal(keId, firstPayload!.PromotedKnowledgeEntryId);
        Assert.Equal(firstPayload.PromotedKnowledgeEntryId, secondPayload!.PromotedKnowledgeEntryId);
        Assert.Equal(firstPayload.PromotedAtUtc, secondPayload.PromotedAtUtc);
    }

    [Fact]
    public async Task ExecutePromotion_UnknownArtifactId_Returns404()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-exec-promo-404@example.com");

        var response = await _client.PostAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:does-not-exist/execute-promotion",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExecutePromotion_KnowledgeEntryNotFound_Returns404()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-exec-promo-ke-404@example.com");
        const string artifactId = "transcript-candidate:sig-exec-promo-ke-404-1";
        // Seed a review pointing to a KE that does not exist in the knowledge base.
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: "no-such-compound");

        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/execute-promotion",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExecutePromotion_RecordNotInApprovedState_Returns409()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-exec-promo-409-state@example.com");
        const string artifactId = "transcript-candidate:sig-exec-promo-409-state-1";
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.PendingReview,
            isDeterministicFixture: false);

        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/execute-promotion",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ExecutePromotion_NoPromotionTargetAssigned_Returns409()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-exec-promo-409-notarget@example.com");
        const string artifactId = "transcript-candidate:sig-exec-promo-409-notarget-1";
        // Approved, but no targetCanonicalName assigned — target guard fires.
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: false,
            targetCanonicalName: null);

        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/execute-promotion",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ExecutePromotion_DeterministicFixtureRecord_Returns409()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-exec-promo-409-fixture@example.com");
        const string artifactId = "transcript-candidate:sig-exec-promo-409-fixture-1";
        // Fixture guard fires before KE lookup; no KE needs to exist.
        // UpsertAsync bypasses the PR14A guard, creating an artificial state that exercises
        // the defense-in-depth fixture check in TranscriptCandidatePromotionService.
        await SeedReviewAsync(
            artifactId,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            isDeterministicFixture: true,
            targetCanonicalName: "caffeine-fixture-guard");

        var response = await _client.PostAsync(
            $"/api/v1/admin/staged-transcript-candidate-reviews/{artifactId}/execute-promotion",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ExecutePromotion_Unauthenticated_ReturnsUnauthorizedOrForbidden()
    {
        // No sign-in — raw client, no session cookie.
        var response = await _client.PostAsync(
            "/api/v1/admin/staged-transcript-candidate-reviews/transcript-candidate:any/execute-promotion",
            null);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected unauthorized or forbidden, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    private Task SeedReviewAsync(
        string artifactId,
        string reviewState,
        IReadOnlyDictionary<string, string>? metadata = null)
        => SeedReviewAsync(artifactId, reviewState, isDeterministicFixture: true, metadata: metadata);

    private async Task SeedReviewAsync(
        string artifactId,
        string reviewState,
        bool isDeterministicFixture,
        string? targetCanonicalName = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        Guid? promotedKnowledgeEntryId = null,
        string? promotedAtUtc = null,
        string? updatedAtUtc = null)
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
                ["kind"] = "transcript",
            },
            createdAtUtc: "2026-05-30T00:00:00Z",
            updatedAtUtc: updatedAtUtc ?? "2026-05-30T00:00:00Z",
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
        public string? TargetCanonicalName { get; set; }
        public Guid? PromotedKnowledgeEntryId { get; set; }
        public string? PromotedAtUtc { get; set; }

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, JsonElement> Extra { get; set; } = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> ExtraKeys => Extra.Keys;
    }
}
