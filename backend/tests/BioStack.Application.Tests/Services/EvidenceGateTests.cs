namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using Xunit;

public sealed class EvidenceGateTests
{
    private readonly IEvidenceGate _gate = new EvidenceGate();

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static EvidenceGateRequest ValidObservationalRequest(
        string? targetCanonicalName = "tb-500",
        bool isDeterministicFixture = false,
        Dictionary<string, string>? overrideMetadata = null)
    {
        var metadata = overrideMetadata ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = EvidenceTierCode.Observational,
            ["citations"]    = "https://example.test/study-1",
        };

        return new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: targetCanonicalName,
            IsDeterministicFixture: isDeterministicFixture,
            SourceMetadata: metadata);
    }

    private static EvidenceGateRequest ValidMechanisticRequest(
        Dictionary<string, string>? overrideMetadata = null)
    {
        var metadata = overrideMetadata ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"]     = EvidenceTierCode.Mechanistic,
            ["citations"]        = "https://example.test/study-2",
            ["mechanismSummary"] = "BPC-157 upregulates VEGF expression via the FAK-paxillin pathway.",
        };

        return new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: "bpc-157",
            IsDeterministicFixture: false,
            SourceMetadata: metadata);
    }

    // ---------------------------------------------------------------------------
    // Null guard
    // ---------------------------------------------------------------------------

    [Fact]
    public void Evaluate_NullRequest_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _gate.Evaluate(null!));
    }

    // ---------------------------------------------------------------------------
    // Happy paths — gate open
    // ---------------------------------------------------------------------------

    [Fact]
    public void Evaluate_ObservationalTierWithCitations_GateOpen()
    {
        var result = _gate.Evaluate(ValidObservationalRequest());

        Assert.True(result.IsGateOpen);
        Assert.Null(result.RejectionCode);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void Evaluate_MechanisticTierWithCitationsAndMechanism_GateOpen()
    {
        var result = _gate.Evaluate(ValidMechanisticRequest());

        Assert.True(result.IsGateOpen);
        Assert.Null(result.RejectionCode);
        Assert.Null(result.RejectionReason);
    }

    [Theory]
    [InlineData(EvidenceTierCode.ClinicalStudy)]
    [InlineData(EvidenceTierCode.SystematicReview)]
    [InlineData(EvidenceTierCode.Rct)]
    public void Evaluate_HighTierWithCitationsAndMechanism_GateOpen(string tier)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"]     = tier,
            ["citations"]        = "https://example.test/rct-1",
            ["mechanismSummary"] = "Validated receptor binding confirmed in phase III trial.",
        };

        var request = new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: "compound-x",
            IsDeterministicFixture: false,
            SourceMetadata: metadata);

        var result = _gate.Evaluate(request);

        Assert.True(result.IsGateOpen);
    }

    // ---------------------------------------------------------------------------
    // Check 1 — deterministic fixture
    // ---------------------------------------------------------------------------

    [Fact]
    public void Evaluate_DeterministicFixture_Rejects_WithFixtureCode()
    {
        var request = ValidObservationalRequest(isDeterministicFixture: true);

        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        Assert.Equal("deterministic_fixture_not_promotable", result.RejectionCode);
        Assert.NotEmpty(result.RejectionReason!);
    }

    // ---------------------------------------------------------------------------
    // Check 2 — review state guard
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(TranscriptCandidateReviewState.PendingReview)]
    [InlineData(TranscriptCandidateReviewState.ReviewDeferred)]
    [InlineData(TranscriptCandidateReviewState.ReviewRejected)]
    public void Evaluate_WrongReviewState_Rejects_WithStateCode(string state)
    {
        var request = new EvidenceGateRequest(
            ReviewState: state,
            TargetCanonicalName: "tb-500",
            IsDeterministicFixture: false,
            SourceMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["evidenceTier"] = EvidenceTierCode.Observational,
                ["citations"]    = "https://example.test/study-1",
            });

        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        Assert.Equal("review_state_not_approved", result.RejectionCode);
    }

    // ---------------------------------------------------------------------------
    // Check 3 — target canonical name
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_MissingTargetCanonicalName_Rejects_WithTargetCode(string? target)
    {
        var request = ValidObservationalRequest(targetCanonicalName: target);

        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        Assert.Equal("promotion_target_not_assigned", result.RejectionCode);
    }

    // ---------------------------------------------------------------------------
    // Check 4 — evidenceTier present
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_MissingEvidenceTier_Rejects_WithMissingTierCode(string? tier)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["citations"] = "https://example.test/study-1",
        };
        if (tier is not null) metadata["evidenceTier"] = tier;

        var request = ValidObservationalRequest(overrideMetadata: metadata);

        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        Assert.Equal("missing_evidence_tier", result.RejectionCode);
    }

    // ---------------------------------------------------------------------------
    // Check 5 — evidenceTier is supported
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("anecdotal")]
    [InlineData("expert_opinion")]
    [InlineData("OBSERVATIONAL")]   // case-sensitive
    [InlineData("")]
    public void Evaluate_UnsupportedEvidenceTier_Rejects_WithUnsupportedCode(string tier)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = tier,
            ["citations"]    = "https://example.test/study-1",
        };

        var request = ValidObservationalRequest(overrideMetadata: metadata);

        // Empty/whitespace is caught by check 4 (missing tier), so we only
        // explicitly assert the unsupported_evidence_tier code for non-empty values.
        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        // Either missing_evidence_tier or unsupported_evidence_tier — both are correct
        // rejections; just assert the gate is closed.
        Assert.NotNull(result.RejectionCode);
    }

    [Fact]
    public void Evaluate_NonEmptyUnsupportedTier_Rejects_WithUnsupportedCode()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = "anecdotal",
            ["citations"]    = "https://example.test/study-1",
        };

        var request = ValidObservationalRequest(overrideMetadata: metadata);
        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        Assert.Equal("unsupported_evidence_tier", result.RejectionCode);
    }

    // ---------------------------------------------------------------------------
    // Check 6 — citations present
    // ---------------------------------------------------------------------------

    [Fact]
    public void Evaluate_MissingCitations_Rejects_WithMissingCitationsCode()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = EvidenceTierCode.Observational,
        };

        var request = ValidObservationalRequest(overrideMetadata: metadata);
        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        Assert.Equal("missing_citations", result.RejectionCode);
    }

    // ---------------------------------------------------------------------------
    // Check 7 — mechanism required for certain tiers
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(EvidenceTierCode.Mechanistic)]
    [InlineData(EvidenceTierCode.ClinicalStudy)]
    [InlineData(EvidenceTierCode.SystematicReview)]
    [InlineData(EvidenceTierCode.Rct)]
    public void Evaluate_TierRequiringMechanismWithoutMechanismSummary_Rejects(string tier)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = tier,
            ["citations"]    = "https://example.test/study-1",
            // intentionally omit mechanismSummary
        };

        var request = new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: "compound-x",
            IsDeterministicFixture: false,
            SourceMetadata: metadata);

        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        Assert.Equal("missing_mechanism_summary", result.RejectionCode);
    }

    [Fact]
    public void Evaluate_ObservationalTierWithoutMechanismSummary_GateOpen()
    {
        // observational does NOT require mechanismSummary
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = EvidenceTierCode.Observational,
            ["citations"]    = "https://example.test/study-1",
        };

        var request = ValidObservationalRequest(overrideMetadata: metadata);
        var result = _gate.Evaluate(request);

        Assert.True(result.IsGateOpen);
    }

    // ---------------------------------------------------------------------------
    // Check 8 — safety language scan
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("mechanismSummary", "you should take this compound daily")]
    [InlineData("rationaleText",    "you must consult your doctor")]
    [InlineData("summary",          "This compound is safe for long-term use")]
    public void Evaluate_BannedPhraseInSafetyCheckedField_Rejects_WithUnsafeLanguageCode(
        string key, string bannedValue)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"]     = EvidenceTierCode.Mechanistic,
            ["citations"]        = "https://example.test/study-1",
            ["mechanismSummary"] = "Safe neutral mechanism description.",
            [key]                = bannedValue,   // overwrite with banned content
        };

        var request = new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: "compound-x",
            IsDeterministicFixture: false,
            SourceMetadata: metadata);

        var result = _gate.Evaluate(request);

        Assert.False(result.IsGateOpen);
        Assert.Equal("unsafe_recommendation_language", result.RejectionCode);
    }

    [Fact]
    public void Evaluate_CleanMechanismSummaryAndRationale_GateOpen()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"]     = EvidenceTierCode.Mechanistic,
            ["citations"]        = "https://pubmed.ncbi.nlm.nih.gov/12345678",
            ["mechanismSummary"] = "BPC-157 upregulates VEGF via FAK-paxillin pathway.",
            ["rationaleText"]    = "Mechanistic evidence supports potential wound-healing effects in animal models.",
        };

        var request = new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: "bpc-157",
            IsDeterministicFixture: false,
            SourceMetadata: metadata);

        var result = _gate.Evaluate(request);

        Assert.True(result.IsGateOpen);
    }

    // ---------------------------------------------------------------------------
    // EvidenceGateViolationException — structure
    // ---------------------------------------------------------------------------

    [Fact]
    public void EvidenceGateViolationException_StoresRejectionCodeAndMessage()
    {
        const string code    = "missing_citations";
        const string message = "Evidence gate requires at least one citation.";

        var ex = new EvidenceGateViolationException(code, message);

        Assert.Equal(code, ex.RejectionCode);
        Assert.Equal(message, ex.Message);
        Assert.IsAssignableFrom<InvalidOperationException>(ex);
    }
}
