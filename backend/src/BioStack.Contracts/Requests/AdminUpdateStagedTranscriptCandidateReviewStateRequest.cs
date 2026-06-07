namespace BioStack.Contracts.Requests;

/// <summary>
/// Body for POST .../staged-transcript-candidate-reviews/{artifactId}/review-state.
/// <c>Action</c> is a review-lifecycle verb: <c>approve_for_promotion</c>, <c>reject_review</c>,
/// or <c>defer_review</c>.  Approval is a state label only — it does not execute promotion.
/// </summary>
public sealed record AdminUpdateStagedTranscriptCandidateReviewStateRequest(
    string? Action);
