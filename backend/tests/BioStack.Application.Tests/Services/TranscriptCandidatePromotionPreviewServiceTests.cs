namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using Xunit;

// ---------------------------------------------------------------------------
// Fakes
// ---------------------------------------------------------------------------

sealed class PreviewFakeReviewStore : ITranscriptCandidateReviewStore
{
    public TranscriptCandidateReviewRecord? Record { get; set; }
    public bool RecordPromotionCompletionCalled { get; private set; }

    public Task<TranscriptCandidateReviewRecord?> GetByArtifactIdAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Record);

    public Task<TranscriptCandidateReviewRecord> RecordPromotionCompletionAsync(
        string artifactId,
        Guid promotedKnowledgeEntryId,
        string promotedAtUtc,
        CancellationToken cancellationToken = default)
    {
        RecordPromotionCompletionCalled = true;
        throw new NotSupportedException(
            "Preview service must never call RecordPromotionCompletionAsync.");
    }

    public Task UpsertAsync(
        TranscriptCandidateReviewRecord record,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TranscriptCandidateReviewRecord>> ListAsync(
        TranscriptCandidateReviewFilter filter,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<TranscriptCandidateReviewRecord> UpdateReviewStateAsync(
        string artifactId,
        string expectedCurrentReviewState,
        string nextReviewState,
        string updatedAtUtc,
        string? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<TranscriptCandidateReviewRecord> AssignPromotionTargetAsync(
        string artifactId,
        string targetCanonicalName,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

sealed class PreviewFakeKnowledgeSource : IKnowledgeSource
{
    public KnowledgeEntry? KeToReturn { get; set; }

    public Task<KnowledgeEntry?> GetCompoundAsync(
        string name,
        CancellationToken cancellationToken = default)
        => Task.FromResult(KeToReturn);

    public Task<List<KnowledgeEntry>> GetAllCompoundsAsync(
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<List<KnowledgeEntry>> SearchCompoundsByPathwayAsync(
        string pathway,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<KnowledgeUpsertDisposition> UpsertCompoundAsync(
        KnowledgeEntry entry,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Preview service must never call UpsertCompoundAsync.");

    public Task<int> IngestBulkAsync(
        List<KnowledgeEntry> entries,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public sealed class TranscriptCandidatePromotionPreviewServiceTests
{
    // ---------------------------------------------------------------------------
    // Shared helpers
    // ---------------------------------------------------------------------------

    private static readonly string T = "2025-01-01T00:00:00.0000000Z";

    private static TranscriptCandidateReviewRecord ApprovedRecord(
        string artifactId = "artifact-preview-001",
        string? targetCanonicalName = "tb-500",
        bool isDeterministicFixture = false,
        Dictionary<string, string>? metadata = null,
        Guid? promotedKnowledgeEntryId = null,
        string? promotedAtUtc = null)
    {
        var meta = metadata ?? ObservationalMetadata();
        return new TranscriptCandidateReviewRecord(
            ArtifactId: artifactId,
            Canonicality: TranscriptCandidateReviewRecord.NonCanonical,
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            SourceType: "youtube",
            SourceUrl: "https://youtube.com/watch?v=preview-001",
            Provider: "test-provider",
            IsDeterministicFixture: isDeterministicFixture,
            SegmentCount: 3,
            SegmentSnapshotSignature: "sig-preview-001",
            SourceMetadata: meta,
            CreatedAtUtc: T,
            UpdatedAtUtc: T,
            TargetCanonicalName: targetCanonicalName,
            PromotedKnowledgeEntryId: promotedKnowledgeEntryId,
            PromotedAtUtc: promotedAtUtc);
    }

    private static Dictionary<string, string> ObservationalMetadata() =>
        new(StringComparer.Ordinal)
        {
            ["evidenceTier"] = EvidenceTierCode.Observational,
            ["citations"]    = "https://example.test/study-1",
        };

    private static Dictionary<string, string> MechanisticMetadata() =>
        new(StringComparer.Ordinal)
        {
            ["evidenceTier"]     = EvidenceTierCode.Mechanistic,
            ["citations"]        = "https://example.test/study-1",
            ["mechanismSummary"] = "TB-500 upregulates actin polymerisation via thymosin-beta-4.",
        };

    private static KnowledgeEntry MakeKe(string canonicalName = "tb-500") =>
        new() { Id = Guid.NewGuid(), CanonicalName = canonicalName };

    private static TranscriptCandidatePromotionPreviewService BuildSut(
        PreviewFakeReviewStore store,
        PreviewFakeKnowledgeSource knowledgeSource)
        => new(store, knowledgeSource, new EvidenceGate());

    // ---------------------------------------------------------------------------
    // Test 1 — Happy path: CanPromote=true
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_HappyPath_ReturnsCanPromoteTrue_WithResolvedTargetAndEvidenceGatePass()
    {
        var keId = Guid.NewGuid();
        var store = new PreviewFakeReviewStore { Record = ApprovedRecord() };
        var ks    = new PreviewFakeKnowledgeSource { KeToReturn = new KnowledgeEntry { Id = keId, CanonicalName = "tb-500" } };
        var sut   = BuildSut(store, ks);

        var result = await sut.PreviewAsync("artifact-preview-001");

        Assert.True(result.CanPromote);
        Assert.Equal("artifact-preview-001", result.ArtifactId);
        Assert.Equal(TranscriptCandidateReviewState.ReviewApprovedForPromotion, result.ReviewState);
        Assert.True(result.TargetAssigned);
        Assert.Equal("tb-500", result.TargetCanonicalName);
        Assert.Equal(keId, result.ResolvedTargetKnowledgeEntryId);
        Assert.False(result.AlreadyPromoted);
        Assert.Null(result.PromotedKnowledgeEntryId);
        Assert.True(result.EvidenceGate.Passed);
        Assert.Equal(EvidenceTierCode.Observational, result.EvidenceGate.Tier);
        Assert.Equal(1, result.EvidenceGate.CitationCount);
        Assert.Empty(result.EvidenceGate.FailureReasons);
        Assert.Empty(result.BlockingReasons);
        Assert.False(result.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 2 — Unknown artifactId → KeyNotFoundException
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_UnknownArtifactId_ThrowsKeyNotFoundException()
    {
        var store = new PreviewFakeReviewStore { Record = null };
        var ks    = new PreviewFakeKnowledgeSource();
        var sut   = BuildSut(store, ks);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.PreviewAsync("does-not-exist"));
    }

    // ---------------------------------------------------------------------------
    // Test 3 — Wrong review state → CanPromote=false, state blocking reason
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_PendingReview_ReturnsCanPromoteFalse_WithStateReason()
    {
        var record = new TranscriptCandidateReviewRecord(
            ArtifactId: "artifact-pending-001",
            Canonicality: TranscriptCandidateReviewRecord.NonCanonical,
            ReviewState: TranscriptCandidateReviewState.PendingReview,
            SourceType: "youtube",
            SourceUrl: "https://youtube.com/watch?v=pending-001",
            Provider: "test-provider",
            IsDeterministicFixture: false,
            SegmentCount: 2,
            SegmentSnapshotSignature: "sig-pending-001",
            SourceMetadata: ObservationalMetadata(),
            CreatedAtUtc: T,
            UpdatedAtUtc: T,
            TargetCanonicalName: "tb-500");

        var store = new PreviewFakeReviewStore { Record = record };
        var ks    = new PreviewFakeKnowledgeSource { KeToReturn = MakeKe() };
        var sut   = BuildSut(store, ks);

        var result = await sut.PreviewAsync("artifact-pending-001");

        Assert.False(result.CanPromote);
        Assert.Contains("review_state_not_approved", result.BlockingReasons);
        Assert.DoesNotContain("promotion_target_not_assigned", result.BlockingReasons);
        Assert.False(result.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 4 — Approved but no target → CanPromote=false, target blocking reason
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_ApprovedWithoutTarget_ReturnsCanPromoteFalse_WithTargetReason()
    {
        var store = new PreviewFakeReviewStore { Record = ApprovedRecord(targetCanonicalName: null) };
        var ks    = new PreviewFakeKnowledgeSource { KeToReturn = MakeKe() };
        var sut   = BuildSut(store, ks);

        var result = await sut.PreviewAsync("artifact-preview-001");

        Assert.False(result.CanPromote);
        Assert.False(result.TargetAssigned);
        Assert.Null(result.TargetCanonicalName);
        Assert.Contains("promotion_target_not_assigned", result.BlockingReasons);
        Assert.False(result.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 5 — Target KE not found → CanPromote=false, KE resolution reason
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_TargetKnowledgeEntryNotFound_ReturnsCanPromoteFalse_WithTargetResolutionReason()
    {
        var store = new PreviewFakeReviewStore { Record = ApprovedRecord() };
        var ks    = new PreviewFakeKnowledgeSource { KeToReturn = null };   // KE does not exist
        var sut   = BuildSut(store, ks);

        var result = await sut.PreviewAsync("artifact-preview-001");

        Assert.False(result.CanPromote);
        Assert.Null(result.ResolvedTargetKnowledgeEntryId);
        Assert.Contains("target_knowledge_entry_not_found", result.BlockingReasons);
        // Gate itself passed (structural + evidence ok)
        Assert.True(result.EvidenceGate.Passed);
        Assert.False(result.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 6 — Evidence gate failure → CanPromote=false, gate failure reason
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_EvidenceGateFailure_ReturnsCanPromoteFalse_WithEvidenceReason()
    {
        // No citations → gate rejects with missing_citations
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = EvidenceTierCode.Observational,
            // citations intentionally omitted
        };

        var store = new PreviewFakeReviewStore { Record = ApprovedRecord(metadata: metadata) };
        var ks    = new PreviewFakeKnowledgeSource { KeToReturn = MakeKe() };
        var sut   = BuildSut(store, ks);

        var result = await sut.PreviewAsync("artifact-preview-001");

        Assert.False(result.CanPromote);
        Assert.False(result.EvidenceGate.Passed);
        Assert.Contains("missing_citations", result.EvidenceGate.FailureReasons);
        Assert.Contains("missing_citations", result.BlockingReasons);
        Assert.False(result.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 7 — Deterministic fixture → CanPromote=false
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_DeterministicFixtureRecord_ReturnsCanPromoteFalse()
    {
        var store = new PreviewFakeReviewStore
        {
            Record = ApprovedRecord(isDeterministicFixture: true)
        };
        var ks  = new PreviewFakeKnowledgeSource { KeToReturn = MakeKe() };
        var sut = BuildSut(store, ks);

        var result = await sut.PreviewAsync("artifact-preview-001");

        Assert.False(result.CanPromote);
        Assert.Contains("deterministic_fixture_not_promotable", result.BlockingReasons);
        Assert.False(result.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 8 — Already promoted → stable prior info, AlreadyPromoted=true
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_AlreadyPromoted_ReturnsStablePriorPromotionInfo()
    {
        var existingKeId      = Guid.NewGuid();
        var existingPromotedAt = "2025-06-01T10:00:00.0000000Z";

        var store = new PreviewFakeReviewStore
        {
            Record = ApprovedRecord(
                promotedKnowledgeEntryId: existingKeId,
                promotedAtUtc: existingPromotedAt)
        };
        var ks  = new PreviewFakeKnowledgeSource();   // KE lookup must not be called
        var sut = BuildSut(store, ks);

        var result = await sut.PreviewAsync("artifact-preview-001");

        Assert.False(result.CanPromote);
        Assert.True(result.AlreadyPromoted);
        Assert.Equal(existingKeId, result.PromotedKnowledgeEntryId);
        Assert.Contains("already_promoted", result.BlockingReasons);
        Assert.False(result.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 9 — Side-effect freedom: RecordPromotionCompletionAsync never called
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Preview_IsSideEffectFree_DoesNotStampOrMutateRecord()
    {
        var store = new PreviewFakeReviewStore { Record = ApprovedRecord() };
        var ks    = new PreviewFakeKnowledgeSource { KeToReturn = MakeKe() };
        var sut   = BuildSut(store, ks);

        // Must not throw; FakeReviewStore.RecordPromotionCompletionAsync throws if called.
        var result = await sut.PreviewAsync("artifact-preview-001");

        Assert.False(store.RecordPromotionCompletionCalled);
        Assert.True(result.CanPromote);
        Assert.False(result.WouldWrite);
    }

    // ---------------------------------------------------------------------------
    // Test 10 — Empty artifactId → ArgumentException
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Preview_EmptyArtifactId_ThrowsArgumentException(string? artifactId)
    {
        var store = new PreviewFakeReviewStore();
        var ks    = new PreviewFakeKnowledgeSource();
        var sut   = BuildSut(store, ks);

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.PreviewAsync(artifactId!));
    }
}
