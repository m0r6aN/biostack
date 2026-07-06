namespace BioStack.Application.Services;

public sealed record TranscriptCandidateReviewRecord(
    string ArtifactId,
    string Canonicality,
    string ReviewState,
    string SourceType,
    string SourceUrl,
    string Provider,
    bool IsDeterministicFixture,
    int SegmentCount,
    string SegmentSnapshotSignature,
    IReadOnlyDictionary<string, string> SourceMetadata,
    string CreatedAtUtc,
    string UpdatedAtUtc,
    string? RowVersion = null,
    string? TargetCanonicalName = null,
    Guid? PromotedKnowledgeEntryId = null,
    string? PromotedAtUtc = null,
    Guid? IntakeRequestId = null)
{
    public const string NonCanonical = "non_canonical";

    private static readonly HashSet<string> AllowedReviewStates = new(StringComparer.Ordinal)
    {
        TranscriptCandidateReviewState.PendingReview,
        TranscriptCandidateReviewState.ReviewDeferred,
        TranscriptCandidateReviewState.ReviewRejected,
        TranscriptCandidateReviewState.ReviewApprovedForPromotion,
    };

    public static TranscriptCandidateReviewRecord Create(
        string artifactId,
        string canonicality,
        string reviewState,
        string sourceType,
        string sourceUrl,
        string provider,
        bool isDeterministicFixture,
        int segmentCount,
        string segmentSnapshotSignature,
        IReadOnlyDictionary<string, string> sourceMetadata,
        string createdAtUtc,
        string updatedAtUtc,
        string? rowVersion = null,
        string? targetCanonicalName = null,
        Guid? promotedKnowledgeEntryId = null,
        string? promotedAtUtc = null,
        Guid? intakeRequestId = null)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            throw new ArgumentException("ArtifactId is required.", nameof(artifactId));
        }

        if (!string.Equals(canonicality, NonCanonical, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Only non-canonical staged transcript candidate review records are supported. Actual canonicality: '{canonicality}'.");
        }

        if (string.IsNullOrWhiteSpace(reviewState))
        {
            throw new ArgumentException("ReviewState is required.", nameof(reviewState));
        }

        if (!AllowedReviewStates.Contains(reviewState))
        {
            throw new InvalidOperationException($"Unsupported review state '{reviewState}'.");
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException("SourceType is required.", nameof(sourceType));
        }

        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            throw new ArgumentException("SourceUrl is required.", nameof(sourceUrl));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider is required.", nameof(provider));
        }

        if (segmentCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentCount), "SegmentCount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(segmentSnapshotSignature))
        {
            throw new ArgumentException("SegmentSnapshotSignature is required.", nameof(segmentSnapshotSignature));
        }

        ArgumentNullException.ThrowIfNull(sourceMetadata);

        if (string.IsNullOrWhiteSpace(createdAtUtc))
        {
            throw new ArgumentException("CreatedAtUtc is required.", nameof(createdAtUtc));
        }

        if (string.IsNullOrWhiteSpace(updatedAtUtc))
        {
            throw new ArgumentException("UpdatedAtUtc is required.", nameof(updatedAtUtc));
        }

        var sortedMetadata = sourceMetadata
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

        return new TranscriptCandidateReviewRecord(
            ArtifactId: artifactId,
            Canonicality: canonicality,
            ReviewState: reviewState,
            SourceType: sourceType,
            SourceUrl: sourceUrl,
            Provider: provider,
            IsDeterministicFixture: isDeterministicFixture,
            SegmentCount: segmentCount,
            SegmentSnapshotSignature: segmentSnapshotSignature,
            SourceMetadata: sortedMetadata,
            CreatedAtUtc: createdAtUtc,
            UpdatedAtUtc: updatedAtUtc,
            RowVersion: rowVersion,
            TargetCanonicalName: targetCanonicalName,
            PromotedKnowledgeEntryId: promotedKnowledgeEntryId,
            PromotedAtUtc: promotedAtUtc,
            IntakeRequestId: intakeRequestId);
    }

    public TranscriptCandidateReviewRecord WithReviewState(
        string reviewState,
        string updatedAtUtc,
        string? rowVersion = null)
    {
        if (string.IsNullOrWhiteSpace(reviewState))
        {
            throw new ArgumentException("ReviewState is required.", nameof(reviewState));
        }

        if (!AllowedReviewStates.Contains(reviewState))
        {
            throw new InvalidOperationException($"Unsupported review state '{reviewState}'.");
        }

        if (string.IsNullOrWhiteSpace(updatedAtUtc))
        {
            throw new ArgumentException("UpdatedAtUtc is required.", nameof(updatedAtUtc));
        }

        return this with
        {
            ReviewState = reviewState,
            UpdatedAtUtc = updatedAtUtc,
            RowVersion = rowVersion,
        };
    }

    public TranscriptCandidateReviewRecord WithPromotionTarget(
        string targetCanonicalName,
        string updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(targetCanonicalName))
        {
            throw new ArgumentException("TargetCanonicalName is required.", nameof(targetCanonicalName));
        }

        if (string.IsNullOrWhiteSpace(updatedAtUtc))
        {
            throw new ArgumentException("UpdatedAtUtc is required.", nameof(updatedAtUtc));
        }

        return this with
        {
            TargetCanonicalName = targetCanonicalName,
            UpdatedAtUtc = updatedAtUtc,
        };
    }
}
