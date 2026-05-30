namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Application.Tests.Fixtures;
using Xunit;

public sealed class TranscriptCandidateReviewLifecycleTests
{
    private readonly ITranscriptCandidateArtifactReviewService _reviewService = new TranscriptCandidateArtifactReviewService();
    private readonly ITranscriptCandidateReviewLifecycle _lifecycle = new TranscriptCandidateReviewLifecycle();

    [Fact]
    public void Baseline_DefaultReviewModelState_IsPendingReview()
    {
        var reviewModel = CreateReviewModel();

        Assert.Equal(TranscriptCandidateReviewState.PendingReview, reviewModel.ReviewState);
    }

    [Fact]
    public void ApplyAction_PendingToReviewApprovedForPromotion_IsAllowed()
    {
        var reviewModel = CreateReviewModel();

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.ApproveForPromotion);

        Assert.True(decision.IsTransitionAllowed);
        Assert.Equal(TranscriptCandidateReviewState.PendingReview, decision.FromReviewState);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, decision.ToReviewState);
        Assert.True(decision.IsPromotionEligible);
        Assert.Equal("non_canonical", decision.Canonicality);
        Assert.Null(decision.RejectionReason);
    }

    [Fact]
    public void ApplyAction_PendingToReviewRejected_IsAllowed()
    {
        var reviewModel = CreateReviewModel();

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.RejectReview);

        Assert.True(decision.IsTransitionAllowed);
        Assert.Equal(TranscriptCandidateReviewState.ReviewRejected, decision.ToReviewState);
        Assert.False(decision.IsPromotionEligible);
        Assert.Equal("non_canonical", decision.Canonicality);
        Assert.Null(decision.RejectionReason);
    }

    [Fact]
    public void ApplyAction_PendingToReviewDeferred_IsAllowed()
    {
        var reviewModel = CreateReviewModel();

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.DeferReview);

        Assert.True(decision.IsTransitionAllowed);
        Assert.Equal(TranscriptCandidateReviewState.ReviewDeferred, decision.ToReviewState);
        Assert.False(decision.IsPromotionEligible);
        Assert.Equal("non_canonical", decision.Canonicality);
        Assert.Null(decision.RejectionReason);
    }

    [Fact]
    public void ApplyAction_ReviewRejected_IsNonPromotingAndTerminalForPr6()
    {
        var reviewModel = CreateReviewModel() with
        {
            ReviewState = TranscriptCandidateReviewState.ReviewRejected,
        };

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.ApproveForPromotion);

        Assert.False(decision.IsTransitionAllowed);
        Assert.Equal(TranscriptCandidateReviewState.ReviewRejected, decision.FromReviewState);
        Assert.Equal(TranscriptCandidateReviewState.ReviewRejected, decision.ToReviewState);
        Assert.False(decision.IsPromotionEligible);
        Assert.Equal("invalid_transition_from_terminal_state", decision.RejectionReason);
    }

    [Fact]
    public void ApplyAction_ReviewDeferred_IsNonPromotingAndTerminalForPr6()
    {
        var reviewModel = CreateReviewModel() with
        {
            ReviewState = TranscriptCandidateReviewState.ReviewDeferred,
        };

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.RejectReview);

        Assert.False(decision.IsTransitionAllowed);
        Assert.Equal(TranscriptCandidateReviewState.ReviewDeferred, decision.FromReviewState);
        Assert.Equal(TranscriptCandidateReviewState.ReviewDeferred, decision.ToReviewState);
        Assert.False(decision.IsPromotionEligible);
        Assert.Equal("invalid_transition_from_terminal_state", decision.RejectionReason);
    }

    [Fact]
    public void ApplyAction_ReviewApprovedForPromotion_DoesNotCanonicalize()
    {
        var reviewModel = CreateReviewModel();

        var approveDecision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.ApproveForPromotion);

        Assert.True(approveDecision.IsTransitionAllowed);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, approveDecision.ToReviewState);
        Assert.Equal("non_canonical", approveDecision.Canonicality);
        Assert.True(approveDecision.IsPromotionEligible);
    }

    [Fact]
    public void ApplyAction_UnsupportedAction_FailsDeterministically()
    {
        var reviewModel = CreateReviewModel();

        var decision = _lifecycle.ApplyAction(reviewModel, "archive_review");

        Assert.False(decision.IsTransitionAllowed);
        Assert.Equal(TranscriptCandidateReviewState.PendingReview, decision.FromReviewState);
        Assert.Equal(TranscriptCandidateReviewState.PendingReview, decision.ToReviewState);
        Assert.Equal("unsupported_action", decision.RejectionReason);
        Assert.False(decision.IsPromotionEligible);
    }

    [Fact]
    public void ApplyAction_UnsupportedState_FailsDeterministically()
    {
        var reviewModel = CreateReviewModel() with
        {
            ReviewState = "review_in_escalation",
        };

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.DeferReview);

        Assert.False(decision.IsTransitionAllowed);
        Assert.Equal("review_in_escalation", decision.FromReviewState);
        Assert.Equal("review_in_escalation", decision.ToReviewState);
        Assert.Equal("unsupported_review_state", decision.RejectionReason);
    }

    [Fact]
    public void ApplyAction_NonNonCanonicalInput_IsRejected()
    {
        var reviewModel = CreateReviewModel() with
        {
            Canonicality = "canonical",
        };

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.ApproveForPromotion);

        Assert.False(decision.IsTransitionAllowed);
        Assert.Equal("only_non_canonical_candidates_supported", decision.RejectionReason);
        Assert.Equal("canonical", decision.Canonicality);
    }

    [Fact]
    public void ApplyAction_SameInputAndAction_ReturnsIdenticalDecision()
    {
        var reviewModel = CreateReviewModel();

        var decisionA = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.DeferReview);
        var decisionB = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.DeferReview);

        Assert.Equal(decisionA, decisionB);
    }

    [Fact]
    public void ApplyAction_DoesNotTouchPersistenceOrKnowledgeEntriesSurface()
    {
        var reviewModel = CreateReviewModel();

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.DeferReview);

        Assert.NotNull(decision);
        Assert.NotNull(decision.ArtifactId);
    }

    [Fact]
    public void ApplyAction_SurfaceContainsNoExtractionSummarySafetyMedicalOrNetworkBehavior()
    {
        var reviewModel = CreateReviewModel();

        var decision = _lifecycle.ApplyAction(reviewModel, TranscriptCandidateReviewAction.DeferReview);

        Assert.Equal(TranscriptCandidateReviewState.ReviewDeferred, decision.ToReviewState);
        Assert.DoesNotContain("summary", decision.ToReviewState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("claim", decision.ToReviewState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fact", decision.ToReviewState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("safety", decision.ToReviewState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("medical", decision.ToReviewState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("network", decision.ToReviewState, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyAction_NullReviewModel_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _lifecycle.ApplyAction(null!, TranscriptCandidateReviewAction.DeferReview));
    }

    [Fact]
    public void ApplyAction_BlankAction_ThrowsArgumentException()
    {
        var reviewModel = CreateReviewModel();

        var ex = Assert.Throws<ArgumentException>(() => _lifecycle.ApplyAction(reviewModel, ""));

        Assert.Contains("Action is required.", ex.Message, StringComparison.Ordinal);
    }

    private TranscriptCandidateArtifactReviewModel CreateReviewModel()
    {
        var sourceMaterial = Tb500TranscriptFixture.CreateResult();
        var staged = new TranscriptCandidateArtifactStagingService().Stage(sourceMaterial);
        return _reviewService.BuildReviewModel(staged);
    }
}
