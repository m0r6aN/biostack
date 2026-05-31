namespace BioStack.Api.Tests;

using BioStack.Application.Services;
using BioStack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class StagedTranscriptCandidateReviewStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public StagedTranscriptCandidateReviewStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task UpsertAndGetByArtifactId_RoundTripsDeterministically()
    {
        var artifactId = "transcript-candidate:sig-a";
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        var record = CreateRecord(
            artifactId: artifactId,
            reviewState: TranscriptCandidateReviewState.PendingReview,
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["zeta"] = "z",
                ["alpha"] = "a",
                ["middle"] = "m",
            });

        await store.UpsertAsync(record);

        var loaded = await store.GetByArtifactIdAsync(artifactId);

        Assert.NotNull(loaded);
        Assert.Equal("non_canonical", loaded!.Canonicality);
        Assert.Equal(TranscriptCandidateReviewState.PendingReview, loaded.ReviewState);
        Assert.Equal(new[] { "alpha", "middle", "zeta" }, loaded.SourceMetadata.Keys.ToArray());
    }

    [Fact]
    public async Task ListByReviewState_FiltersOnlyRequestedState()
    {
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        await store.UpsertAsync(CreateRecord("transcript-candidate:sig-1", TranscriptCandidateReviewState.PendingReview));
        await store.UpsertAsync(CreateRecord("transcript-candidate:sig-2", TranscriptCandidateReviewState.ReviewDeferred));
        await store.UpsertAsync(CreateRecord("transcript-candidate:sig-3", TranscriptCandidateReviewState.ReviewDeferred));

        var deferred = await store.ListByReviewStateAsync(TranscriptCandidateReviewState.ReviewDeferred);

        Assert.Equal(2, deferred.Count);
        Assert.All(deferred, x => Assert.Equal(TranscriptCandidateReviewState.ReviewDeferred, x.ReviewState));
    }

    [Fact]
    public async Task UpdateReviewState_EnforcesExpectedCurrentState()
    {
        var artifactId = "transcript-candidate:sig-enforce";
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        await store.UpsertAsync(CreateRecord(artifactId, TranscriptCandidateReviewState.PendingReview));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.UpdateReviewStateAsync(
                artifactId: artifactId,
                expectedCurrentReviewState: TranscriptCandidateReviewState.ReviewRejected,
                nextReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
                updatedAtUtc: "2026-05-30T00:00:01Z"));

        Assert.Contains("ReviewState mismatch", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateReviewState_UpdatesWhenExpectedMatches()
    {
        var artifactId = "transcript-candidate:sig-update";
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        await store.UpsertAsync(CreateRecord(artifactId, TranscriptCandidateReviewState.PendingReview));

        var updated = await store.UpdateReviewStateAsync(
            artifactId: artifactId,
            expectedCurrentReviewState: TranscriptCandidateReviewState.PendingReview,
            nextReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            updatedAtUtc: "2026-05-30T00:00:01Z");

        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, updated.ReviewState);
        Assert.Equal("2026-05-30T00:00:01Z", updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task MissingArtifactId_Behavior_IsExplicit()
    {
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        await Assert.ThrowsAsync<ArgumentException>(() => store.GetByArtifactIdAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => store.UpdateReviewStateAsync(
            artifactId: "",
            expectedCurrentReviewState: TranscriptCandidateReviewState.PendingReview,
            nextReviewState: TranscriptCandidateReviewState.ReviewDeferred,
            updatedAtUtc: "2026-05-30T00:00:01Z"));
    }

    [Fact]
    public async Task Upsert_DuplicateArtifactId_UpdatesExistingRecord()
    {
        var artifactId = "transcript-candidate:sig-dup";
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        await store.UpsertAsync(CreateRecord(artifactId, TranscriptCandidateReviewState.PendingReview));
        await store.UpsertAsync(CreateRecord(artifactId, TranscriptCandidateReviewState.ReviewDeferred));

        var loaded = await store.GetByArtifactIdAsync(artifactId);

        Assert.NotNull(loaded);
        Assert.Equal(TranscriptCandidateReviewState.ReviewDeferred, loaded!.ReviewState);
    }

    [Fact]
    public async Task CanonicalityConstraint_NonCanonicalOnly_EnforcedByStoreRecordValidation()
    {
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        var invalid = TranscriptCandidateReviewRecord.Create(
            artifactId: "transcript-candidate:sig-invalid",
            canonicality: "non_canonical",
            reviewState: TranscriptCandidateReviewState.PendingReview,
            sourceType: "video",
            sourceUrl: "https://example.test/video",
            provider: "fixture",
            isDeterministicFixture: true,
            segmentCount: 2,
            segmentSnapshotSignature: "sig-invalid",
            sourceMetadata: new Dictionary<string, string>(StringComparer.Ordinal),
            createdAtUtc: "2026-05-30T00:00:00Z",
            updatedAtUtc: "2026-05-30T00:00:00Z") with { Canonicality = "canonical" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.UpsertAsync(invalid));
    }

    [Fact]
    public async Task ReviewStates_AreLimitedToLifecycleConstants()
    {
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        var invalid = TranscriptCandidateReviewRecord.Create(
            artifactId: "transcript-candidate:sig-state",
            canonicality: "non_canonical",
            reviewState: TranscriptCandidateReviewState.PendingReview,
            sourceType: "video",
            sourceUrl: "https://example.test/video",
            provider: "fixture",
            isDeterministicFixture: true,
            segmentCount: 2,
            segmentSnapshotSignature: "sig-state",
            sourceMetadata: new Dictionary<string, string>(StringComparer.Ordinal),
            createdAtUtc: "2026-05-30T00:00:00Z",
            updatedAtUtc: "2026-05-30T00:00:00Z") with { ReviewState = "unknown_state" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.UpsertAsync(invalid));
    }

    [Fact]
    public async Task RowVersionConcurrency_Parameter_DoesNotLeakIntoContractShape()
    {
        var artifactId = "transcript-candidate:sig-rowversion";
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        await store.UpsertAsync(CreateRecord(artifactId, TranscriptCandidateReviewState.PendingReview));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.UpdateReviewStateAsync(
                artifactId: artifactId,
                expectedCurrentReviewState: TranscriptCandidateReviewState.PendingReview,
                nextReviewState: TranscriptCandidateReviewState.ReviewDeferred,
                updatedAtUtc: "2026-05-30T00:00:01Z",
                expectedRowVersion: "abc"));

        Assert.Contains("RowVersion-based concurrency is not enabled", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StoreOperations_DoNotWriteKnowledgeEntries()
    {
        var artifactId = "transcript-candidate:sig-no-ke";
        using var db = CreateDbContext();
        var store = new StagedTranscriptCandidateReviewStore(db);

        var before = await db.KnowledgeEntries.CountAsync();
        await store.UpsertAsync(CreateRecord(artifactId, TranscriptCandidateReviewState.PendingReview));
        await store.UpdateReviewStateAsync(
            artifactId,
            TranscriptCandidateReviewState.PendingReview,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            "2026-05-30T00:00:01Z");
        var after = await db.KnowledgeEntries.CountAsync();

        Assert.Equal(before, after);
    }

    [Fact]
    public async Task ApprovedForPromotion_RemainsReviewStateOnly_NoPromotionExecutionColumns()
    {
        var artifactId = "transcript-candidate:sig-approved";
        var store = new StagedTranscriptCandidateReviewStore(CreateDbContext());

        await store.UpsertAsync(CreateRecord(artifactId, TranscriptCandidateReviewState.PendingReview));
        var updated = await store.UpdateReviewStateAsync(
            artifactId,
            TranscriptCandidateReviewState.PendingReview,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            "2026-05-30T00:00:01Z");

        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, updated.ReviewState);
        Assert.DoesNotContain("promotion", string.Join("|", updated.SourceMetadata.Keys), StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private BioStackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new BioStackDbContext(options);
    }

    private static TranscriptCandidateReviewRecord CreateRecord(
        string artifactId,
        string reviewState,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return TranscriptCandidateReviewRecord.Create(
            artifactId: artifactId,
            canonicality: "non_canonical",
            reviewState: reviewState,
            sourceType: "video",
            sourceUrl: $"https://example.test/{artifactId}",
            provider: "fixture",
            isDeterministicFixture: true,
            segmentCount: 3,
            segmentSnapshotSignature: artifactId.Replace("transcript-candidate:", "", StringComparison.Ordinal),
            sourceMetadata: metadata ?? new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["source"] = "fixture",
                ["kind"] = "transcript",
            },
            createdAtUtc: "2026-05-30T00:00:00Z",
            updatedAtUtc: "2026-05-30T00:00:00Z");
    }
}
