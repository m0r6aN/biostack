namespace BioStack.Application.Tests.Services;

using System.Reflection;
using BioStack.Application.Services;
using BioStack.Application.Tests.Fixtures;
using Xunit;

public sealed class TranscriptCandidateReviewStoreContractTests
{
    [Fact]
    public void RecordCreate_NonCanonicalOnly_AllowsNonCanonical()
    {
        var model = CreateReviewModel();
        var now = "2025-01-01T00:00:00Z";

        var record = TranscriptCandidateReviewRecord.Create(
            artifactId: model.ArtifactId,
            canonicality: "non_canonical",
            reviewState: model.ReviewState,
            sourceType: model.SourceType,
            sourceUrl: model.SourceUrl,
            provider: model.Provider,
            isDeterministicFixture: model.IsDeterministicFixture,
            segmentCount: model.SegmentCount,
            segmentSnapshotSignature: model.SegmentSnapshotSignature,
            sourceMetadata: model.SourceMetadata,
            createdAtUtc: now,
            updatedAtUtc: now);

        Assert.Equal("non_canonical", record.Canonicality);
    }

    [Fact]
    public void RecordCreate_NonCanonicalOnly_RejectsCanonical()
    {
        var model = CreateReviewModel();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TranscriptCandidateReviewRecord.Create(
                artifactId: model.ArtifactId,
                canonicality: "canonical",
                reviewState: model.ReviewState,
                sourceType: model.SourceType,
                sourceUrl: model.SourceUrl,
                provider: model.Provider,
                isDeterministicFixture: model.IsDeterministicFixture,
                segmentCount: model.SegmentCount,
                segmentSnapshotSignature: model.SegmentSnapshotSignature,
                sourceMetadata: model.SourceMetadata,
                createdAtUtc: "2025-01-01T00:00:00Z",
                updatedAtUtc: "2025-01-01T00:00:00Z"));

        Assert.Contains("Only non-canonical staged transcript candidate review records are supported", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TranscriptCandidateReviewState.PendingReview)]
    [InlineData(TranscriptCandidateReviewState.ReviewDeferred)]
    [InlineData(TranscriptCandidateReviewState.ReviewRejected)]
    [InlineData(TranscriptCandidateReviewState.ReviewApprovedForPromotion)]
    public void RecordCreate_ReviewStateConstrainedToLifecycleConstants_AllowsSupportedStates(string reviewState)
    {
        var model = CreateReviewModel();

        var record = TranscriptCandidateReviewRecord.Create(
            artifactId: model.ArtifactId,
            canonicality: "non_canonical",
            reviewState: reviewState,
            sourceType: model.SourceType,
            sourceUrl: model.SourceUrl,
            provider: model.Provider,
            isDeterministicFixture: model.IsDeterministicFixture,
            segmentCount: model.SegmentCount,
            segmentSnapshotSignature: model.SegmentSnapshotSignature,
            sourceMetadata: model.SourceMetadata,
            createdAtUtc: "2025-01-01T00:00:00Z",
            updatedAtUtc: "2025-01-01T00:00:00Z");

        Assert.Equal(reviewState, record.ReviewState);
    }

    [Fact]
    public void RecordCreate_ReviewStateConstrainedToLifecycleConstants_RejectsUnsupportedState()
    {
        var model = CreateReviewModel();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TranscriptCandidateReviewRecord.Create(
                artifactId: model.ArtifactId,
                canonicality: "non_canonical",
                reviewState: "review_in_escalation",
                sourceType: model.SourceType,
                sourceUrl: model.SourceUrl,
                provider: model.Provider,
                isDeterministicFixture: model.IsDeterministicFixture,
                segmentCount: model.SegmentCount,
                segmentSnapshotSignature: model.SegmentSnapshotSignature,
                sourceMetadata: model.SourceMetadata,
                createdAtUtc: "2025-01-01T00:00:00Z",
                updatedAtUtc: "2025-01-01T00:00:00Z"));

        Assert.Contains("Unsupported review state", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StoreContract_ExposesNoMethodForCanonicalKnowledgeEntryWrites()
    {
        var names = typeof(ITranscriptCandidateReviewStore)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToArray();

        Assert.DoesNotContain(names, name => name.Contains("KnowledgeEntry", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Canonical", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StoreContract_ExposesNoMethodForPromotionExecution()
    {
        // This guard ensures the store interface never gains execution methods.
        // "Promote" (verb) and "Execute" are the blocked tokens.
        // "Promotion" (noun) is intentionally NOT blocked: AssignPromotionTargetAsync
        // is a label-assignment method added in PR14A and is not promotion execution.
        // Actual execution is PR14B scope and must NOT appear here.
        var names = typeof(ITranscriptCandidateReviewStore)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToArray();

        Assert.DoesNotContain(names, name => name.Contains("Promote", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Execute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ArtifactIdentity_IsDeterministicAndStableAcrossRecordCreation()
    {
        var model = CreateReviewModel();

        var recordA = TranscriptCandidateReviewRecord.Create(
            artifactId: model.ArtifactId,
            canonicality: "non_canonical",
            reviewState: model.ReviewState,
            sourceType: model.SourceType,
            sourceUrl: model.SourceUrl,
            provider: model.Provider,
            isDeterministicFixture: model.IsDeterministicFixture,
            segmentCount: model.SegmentCount,
            segmentSnapshotSignature: model.SegmentSnapshotSignature,
            sourceMetadata: model.SourceMetadata,
            createdAtUtc: "2025-01-01T00:00:00Z",
            updatedAtUtc: "2025-01-01T00:00:00Z");

        var recordB = TranscriptCandidateReviewRecord.Create(
            artifactId: model.ArtifactId,
            canonicality: "non_canonical",
            reviewState: model.ReviewState,
            sourceType: model.SourceType,
            sourceUrl: model.SourceUrl,
            provider: model.Provider,
            isDeterministicFixture: model.IsDeterministicFixture,
            segmentCount: model.SegmentCount,
            segmentSnapshotSignature: model.SegmentSnapshotSignature,
            sourceMetadata: model.SourceMetadata,
            createdAtUtc: "2025-01-01T00:00:00Z",
            updatedAtUtc: "2025-01-01T00:00:00Z");

        Assert.Equal(recordA.ArtifactId, recordB.ArtifactId);
        Assert.Equal(model.ArtifactId, recordA.ArtifactId);
    }

    [Fact]
    public void SourceMetadata_IsDeterministicallyOrdered_ForFutureRoundTrip()
    {
        var model = CreateReviewModel();
        var unsorted = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["zeta"] = "z",
            ["alpha"] = "a",
            ["middle"] = "m",
        };

        var record = TranscriptCandidateReviewRecord.Create(
            artifactId: model.ArtifactId,
            canonicality: "non_canonical",
            reviewState: model.ReviewState,
            sourceType: model.SourceType,
            sourceUrl: model.SourceUrl,
            provider: model.Provider,
            isDeterministicFixture: model.IsDeterministicFixture,
            segmentCount: model.SegmentCount,
            segmentSnapshotSignature: model.SegmentSnapshotSignature,
            sourceMetadata: unsorted,
            createdAtUtc: "2025-01-01T00:00:00Z",
            updatedAtUtc: "2025-01-01T00:00:00Z");

        var keys = record.SourceMetadata.Keys.ToArray();
        Assert.Equal(new[] { "alpha", "middle", "zeta" }, keys);
    }

    [Fact]
    public void UpdateReviewState_ApproveForPromotion_RemainsStateAndEligibilitySignalOnly()
    {
        var model = CreateReviewModel();

        var record = TranscriptCandidateReviewRecord.Create(
            artifactId: model.ArtifactId,
            canonicality: "non_canonical",
            reviewState: TranscriptCandidateReviewState.PendingReview,
            sourceType: model.SourceType,
            sourceUrl: model.SourceUrl,
            provider: model.Provider,
            isDeterministicFixture: model.IsDeterministicFixture,
            segmentCount: model.SegmentCount,
            segmentSnapshotSignature: model.SegmentSnapshotSignature,
            sourceMetadata: model.SourceMetadata,
            createdAtUtc: "2025-01-01T00:00:00Z",
            updatedAtUtc: "2025-01-01T00:00:00Z");

        var updated = record.WithReviewState(
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            updatedAtUtc: "2025-01-01T00:01:00Z");

        Assert.Equal("non_canonical", updated.Canonicality);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, updated.ReviewState);
        Assert.DoesNotContain("summary", string.Join("|", updated.SourceMetadata.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("claim", string.Join("|", updated.SourceMetadata.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fact", string.Join("|", updated.SourceMetadata.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("safety", string.Join("|", updated.SourceMetadata.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("medical", string.Join("|", updated.SourceMetadata.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("network", string.Join("|", updated.SourceMetadata.Keys), StringComparison.OrdinalIgnoreCase);
    }

    private static TranscriptCandidateArtifactReviewModel CreateReviewModel()
    {
        var sourceMaterial = Tb500TranscriptFixture.CreateResult();
        var staged = new TranscriptCandidateArtifactStagingService().Stage(sourceMaterial);
        return new TranscriptCandidateArtifactReviewService().BuildReviewModel(staged);
    }
}
