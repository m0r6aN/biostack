namespace BioStack.Application.Services;

public interface ITranscriptCandidateReviewStore
{
    Task UpsertAsync(TranscriptCandidateReviewRecord record, CancellationToken cancellationToken = default);

    Task<TranscriptCandidateReviewRecord?> GetByArtifactIdAsync(
        string artifactId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TranscriptCandidateReviewRecord>> ListByReviewStateAsync(
        string reviewState,
        CancellationToken cancellationToken = default);

    Task<TranscriptCandidateReviewRecord> UpdateReviewStateAsync(
        string artifactId,
        string expectedCurrentReviewState,
        string nextReviewState,
        string updatedAtUtc,
        string? expectedRowVersion = null,
        CancellationToken cancellationToken = default);

    Task<TranscriptCandidateReviewRecord> AssignPromotionTargetAsync(
        string artifactId,
        string targetCanonicalName,
        CancellationToken cancellationToken = default);
}
