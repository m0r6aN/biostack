namespace BioStack.Application.Services;

public static class TranscriptCandidateReviewState
{
    public const string PendingReview = "pending_review";
    public const string ReviewDeferred = "review_deferred";
    public const string ReviewRejected = "review_rejected";
    public const string ReviewApprovedForPromotion = "review_approved_for_promotion";
}

public static class TranscriptCandidateReviewAction
{
    public const string DeferReview = "defer_review";
    public const string RejectReview = "reject_review";
    public const string ApproveForPromotion = "approve_for_promotion";
}

public sealed record TranscriptCandidateReviewLifecycleDecision(
    string ArtifactId,
    string FromReviewState,
    string ToReviewState,
    string Canonicality,
    bool IsPromotionEligible,
    bool IsTransitionAllowed,
    string? RejectionReason);

public interface ITranscriptCandidateReviewLifecycle
{
    TranscriptCandidateReviewLifecycleDecision ApplyAction(
        TranscriptCandidateArtifactReviewModel reviewModel,
        string action);
}

public sealed class TranscriptCandidateReviewLifecycle : ITranscriptCandidateReviewLifecycle
{
    private const string NonCanonical = "non_canonical";

    private static readonly IReadOnlyDictionary<(string State, string Action), string> AllowedTransitions =
        new Dictionary<(string State, string Action), string>(new OrdinalTupleComparer())
        {
            {
                (TranscriptCandidateReviewState.PendingReview, TranscriptCandidateReviewAction.ApproveForPromotion),
                TranscriptCandidateReviewState.ReviewApprovedForPromotion
            },
            {
                (TranscriptCandidateReviewState.PendingReview, TranscriptCandidateReviewAction.RejectReview),
                TranscriptCandidateReviewState.ReviewRejected
            },
            {
                (TranscriptCandidateReviewState.PendingReview, TranscriptCandidateReviewAction.DeferReview),
                TranscriptCandidateReviewState.ReviewDeferred
            },
        };

    private static readonly HashSet<string> SupportedStates = new(StringComparer.Ordinal)
    {
        TranscriptCandidateReviewState.PendingReview,
        TranscriptCandidateReviewState.ReviewDeferred,
        TranscriptCandidateReviewState.ReviewRejected,
        TranscriptCandidateReviewState.ReviewApprovedForPromotion,
    };

    private static readonly HashSet<string> SupportedActions = new(StringComparer.Ordinal)
    {
        TranscriptCandidateReviewAction.DeferReview,
        TranscriptCandidateReviewAction.RejectReview,
        TranscriptCandidateReviewAction.ApproveForPromotion,
    };

    public TranscriptCandidateReviewLifecycleDecision ApplyAction(
        TranscriptCandidateArtifactReviewModel reviewModel,
        string action)
    {
        ArgumentNullException.ThrowIfNull(reviewModel);

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        if (!string.Equals(reviewModel.Canonicality, NonCanonical, StringComparison.Ordinal))
        {
            return Rejected(
                reviewModel,
                action,
                reason: "only_non_canonical_candidates_supported");
        }

        if (!SupportedStates.Contains(reviewModel.ReviewState))
        {
            return Rejected(
                reviewModel,
                action,
                reason: "unsupported_review_state");
        }

        if (!SupportedActions.Contains(action))
        {
            return Rejected(
                reviewModel,
                action,
                reason: "unsupported_action");
        }

        if (!AllowedTransitions.TryGetValue((reviewModel.ReviewState, action), out var toState))
        {
            return Rejected(
                reviewModel,
                action,
                reason: "invalid_transition_from_terminal_state");
        }

        return new TranscriptCandidateReviewLifecycleDecision(
            ArtifactId: reviewModel.ArtifactId,
            FromReviewState: reviewModel.ReviewState,
            ToReviewState: toState,
            Canonicality: NonCanonical,
            IsPromotionEligible: string.Equals(
                toState,
                TranscriptCandidateReviewState.ReviewApprovedForPromotion,
                StringComparison.Ordinal),
            IsTransitionAllowed: true,
            RejectionReason: null);
    }

    private static TranscriptCandidateReviewLifecycleDecision Rejected(
        TranscriptCandidateArtifactReviewModel reviewModel,
        string action,
        string reason)
        => new(
            ArtifactId: reviewModel.ArtifactId,
            FromReviewState: reviewModel.ReviewState,
            ToReviewState: reviewModel.ReviewState,
            Canonicality: reviewModel.Canonicality,
            IsPromotionEligible: false,
            IsTransitionAllowed: false,
            RejectionReason: reason);

    private sealed class OrdinalTupleComparer : IEqualityComparer<(string State, string Action)>
    {
        public bool Equals((string State, string Action) x, (string State, string Action) y)
            => string.Equals(x.State, y.State, StringComparison.Ordinal)
               && string.Equals(x.Action, y.Action, StringComparison.Ordinal);

        public int GetHashCode((string State, string Action) obj)
            => HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.State),
                StringComparer.Ordinal.GetHashCode(obj.Action));
    }
}
