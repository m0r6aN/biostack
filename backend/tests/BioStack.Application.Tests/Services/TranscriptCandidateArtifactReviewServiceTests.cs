namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Application.Tests.Fixtures;
using Xunit;

public sealed class TranscriptCandidateArtifactReviewServiceTests
{
    private readonly ITranscriptCandidateArtifactReviewService _service = new TranscriptCandidateArtifactReviewService();

    [Fact]
    public void BuildReviewModel_FromStagedDescriptor_ReturnsDeterministicReviewModel()
    {
        var stagedDescriptor = CreateStagedDescriptor();

        var reviewA = _service.BuildReviewModel(stagedDescriptor);
        var reviewB = _service.BuildReviewModel(stagedDescriptor);

        Assert.Equal(reviewA.ArtifactId, reviewB.ArtifactId);
        Assert.Equal(reviewA.ReviewState, reviewB.ReviewState);
        Assert.Equal(reviewA.Canonicality, reviewB.Canonicality);
        Assert.Equal(reviewA.SourceType, reviewB.SourceType);
        Assert.Equal(reviewA.SourceUrl, reviewB.SourceUrl);
        Assert.Equal(reviewA.Provider, reviewB.Provider);
        Assert.Equal(reviewA.IsDeterministicFixture, reviewB.IsDeterministicFixture);
        Assert.Equal(reviewA.SegmentCount, reviewB.SegmentCount);
        Assert.Equal(reviewA.SegmentSnapshotSignature, reviewB.SegmentSnapshotSignature);
        Assert.Equal(reviewA.SourceMetadata.Count, reviewB.SourceMetadata.Count);

        foreach (var key in reviewA.SourceMetadata.Keys)
        {
            Assert.True(reviewB.SourceMetadata.ContainsKey(key));
            Assert.Equal(reviewA.SourceMetadata[key], reviewB.SourceMetadata[key]);
        }
    }

    [Fact]
    public void BuildReviewModel_IsExplicitlyNonCanonicalAndPendingReview()
    {
        var stagedDescriptor = CreateStagedDescriptor();

        var review = _service.BuildReviewModel(stagedDescriptor);

        Assert.Equal("pending_review", review.ReviewState);
        Assert.Equal("non_canonical", review.Canonicality);
    }

    [Fact]
    public void BuildReviewModel_UsesCandidateScopedDeterministicArtifactId()
    {
        var stagedDescriptor = CreateStagedDescriptor();

        var review = _service.BuildReviewModel(stagedDescriptor);

        Assert.Equal($"transcript-candidate:{stagedDescriptor.SegmentSnapshotSignature}", review.ArtifactId);
    }

    [Fact]
    public void BuildReviewModel_DoesNotPromoteSummarizeExtractSafetyClassifyOrInterpret()
    {
        var stagedDescriptor = CreateStagedDescriptor();

        var review = _service.BuildReviewModel(stagedDescriptor);

        Assert.Equal(stagedDescriptor.SegmentCount, review.SegmentCount);
        Assert.Equal(stagedDescriptor.SegmentSnapshotSignature, review.SegmentSnapshotSignature);

        Assert.DoesNotContain("summary", review.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("summarization", review.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("claim", review.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("fact", review.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("safety", review.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("medical", review.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("interpretation", review.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("promotion", review.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildReviewModel_DoesNotTouchPersistenceSurface_ApiIsPureDescriptorToModel()
    {
        var stagedDescriptor = CreateStagedDescriptor();

        var review = _service.BuildReviewModel(stagedDescriptor);

        Assert.NotNull(review);
        Assert.NotNull(review.SourceMetadata);
    }

    [Fact]
    public void BuildReviewModel_NullDescriptor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.BuildReviewModel(null!));
    }

    [Fact]
    public void BuildReviewModel_UnsupportedCanonicalDescriptor_ThrowsInvalidOperationException()
    {
        var stagedDescriptor = CreateStagedDescriptor() with { Canonicality = "canonical" };

        var ex = Assert.Throws<InvalidOperationException>(() => _service.BuildReviewModel(stagedDescriptor));

        Assert.Contains("Only non-canonical transcript candidate descriptors are supported", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildReviewModel_InvalidDescriptor_ThrowsCleanArgumentException()
    {
        var stagedDescriptor = CreateStagedDescriptor() with { SourceUrl = "" };

        var ex = Assert.Throws<ArgumentException>(() => _service.BuildReviewModel(stagedDescriptor));

        Assert.Contains("SourceUrl is required.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildReviewModel_NegativeSegmentCount_ThrowsArgumentOutOfRangeException()
    {
        var stagedDescriptor = CreateStagedDescriptor() with { SegmentCount = -1 };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _service.BuildReviewModel(stagedDescriptor));

        Assert.Contains("SegmentCount cannot be negative.", ex.Message, StringComparison.Ordinal);
    }

    private static TranscriptCandidateArtifactDescriptor CreateStagedDescriptor()
    {
        var sourceMaterial = Tb500TranscriptFixture.CreateResult();
        var stagingService = new TranscriptCandidateArtifactStagingService();
        return stagingService.Stage(sourceMaterial);
    }
}
