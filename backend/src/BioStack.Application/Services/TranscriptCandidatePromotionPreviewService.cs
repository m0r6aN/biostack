namespace BioStack.Application.Services;

using BioStack.Infrastructure.Knowledge;

/// <summary>
/// Evidence gate summary included in every preview result.
/// Tier, CitationCount, and MechanismSummaryPresent are derived directly from
/// SourceMetadata regardless of whether gate evaluation was reached.
/// Passed is true only when the gate was evaluated and all checks passed.
/// FailureReasons contains the single gate rejection reason when gate fails
/// (evidence gate is fail-fast: one reason per evaluation).
/// </summary>
public sealed record PromotionEvidenceGateSummary(
    bool Passed,
    string? Tier,
    int CitationCount,
    bool MechanismSummaryPresent,
    IReadOnlyList<string> FailureReasons);

/// <summary>
/// Full read-only preview of what an ExecutePromotionAsync call would do
/// for the given artifactId, without performing any writes.
/// WouldWrite is always false.
/// </summary>
public sealed record PromotionPreviewResult(
    string ArtifactId,
    bool CanPromote,
    string ReviewState,
    bool TargetAssigned,
    string? TargetCanonicalName,
    Guid? ResolvedTargetKnowledgeEntryId,
    bool AlreadyPromoted,
    Guid? PromotedKnowledgeEntryId,
    PromotionEvidenceGateSummary EvidenceGate,
    IReadOnlyList<string> BlockingReasons,
    bool WouldWrite);

/// <summary>
/// RR-2: Non-mutating promotion preview contract.
/// Evaluates promotion readiness with the same semantics as
/// ITranscriptCandidatePromotionService without writing anything.
/// </summary>
public interface ITranscriptCandidatePromotionPreviewService
{
    /// <summary>
    /// Returns a full promotion readiness preview for the staged review
    /// identified by <paramref name="artifactId"/>.
    ///
    /// Never writes, stamps, or mutates any record or knowledge entry.
    /// WouldWrite is always false.
    ///
    /// Throws <see cref="KeyNotFoundException"/> if no record exists for
    /// <paramref name="artifactId"/>.
    /// </summary>
    Task<PromotionPreviewResult> PreviewAsync(
        string artifactId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// RR-2 implementation: stateless read-path evaluation of promotion readiness.
/// Mirrors the guard order of TranscriptCandidatePromotionService.ExecutePromotionAsync
/// without writing anything.
/// </summary>
public sealed class TranscriptCandidatePromotionPreviewService : ITranscriptCandidatePromotionPreviewService
{
    private readonly ITranscriptCandidateReviewStore _reviewStore;
    private readonly IKnowledgeSource _knowledgeSource;
    private readonly IEvidenceGate _evidenceGate;

    public TranscriptCandidatePromotionPreviewService(
        ITranscriptCandidateReviewStore reviewStore,
        IKnowledgeSource knowledgeSource,
        IEvidenceGate evidenceGate)
    {
        _reviewStore = reviewStore;
        _knowledgeSource = knowledgeSource;
        _evidenceGate = evidenceGate;
    }

    public async Task<PromotionPreviewResult> PreviewAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            throw new ArgumentException("ArtifactId is required.", nameof(artifactId));
        }

        var record = await _reviewStore.GetByArtifactIdAsync(artifactId, cancellationToken);
        if (record is null)
        {
            throw new KeyNotFoundException(
                $"No staged transcript candidate review record found for artifactId '{artifactId}'.");
        }

        // Already-promoted idempotency path: return stable prior promotion info.
        if (record.PromotedKnowledgeEntryId is not null && record.PromotedAtUtc is not null)
        {
            return new PromotionPreviewResult(
                ArtifactId: record.ArtifactId,
                CanPromote: false,
                ReviewState: record.ReviewState,
                TargetAssigned: !string.IsNullOrWhiteSpace(record.TargetCanonicalName),
                TargetCanonicalName: record.TargetCanonicalName,
                ResolvedTargetKnowledgeEntryId: null,
                AlreadyPromoted: true,
                PromotedKnowledgeEntryId: record.PromotedKnowledgeEntryId,
                EvidenceGate: BuildEvidenceSummary(record.SourceMetadata, passed: false, failureReasons: []),
                BlockingReasons: ["already_promoted"],
                WouldWrite: false);
        }

        var blockingReasons = new List<string>();

        // Structural check 1: review state.
        var stateOk = string.Equals(
            record.ReviewState,
            TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            StringComparison.Ordinal);

        if (!stateOk)
        {
            blockingReasons.Add("review_state_not_approved");
        }

        // Structural check 2: promotion target assigned.
        var targetAssigned = !string.IsNullOrWhiteSpace(record.TargetCanonicalName);
        if (!targetAssigned)
        {
            blockingReasons.Add("promotion_target_not_assigned");
        }

        // Structural check 3: fixture guard.
        if (record.IsDeterministicFixture)
        {
            blockingReasons.Add("deterministic_fixture_not_promotable");
        }

        // Evidence gate evaluation — only when structural checks all pass.
        PromotionEvidenceGateSummary evidenceSummary;
        Guid? resolvedTargetKnowledgeEntryId = null;

        if (blockingReasons.Count == 0)
        {
            var gateRequest = new EvidenceGateRequest(
                ReviewState: record.ReviewState,
                TargetCanonicalName: record.TargetCanonicalName,
                IsDeterministicFixture: record.IsDeterministicFixture,
                SourceMetadata: record.SourceMetadata);

            var gateResult = _evidenceGate.Evaluate(gateRequest);

            if (!gateResult.IsGateOpen)
            {
                blockingReasons.Add(gateResult.RejectionCode!);
                evidenceSummary = BuildEvidenceSummary(
                    record.SourceMetadata,
                    passed: false,
                    failureReasons: [gateResult.RejectionCode!]);
            }
            else
            {
                // KE resolution — only when evidence gate passes.
                var ke = await _knowledgeSource.GetCompoundAsync(record.TargetCanonicalName!, cancellationToken);
                if (ke is null)
                {
                    blockingReasons.Add("target_knowledge_entry_not_found");
                    evidenceSummary = BuildEvidenceSummary(
                        record.SourceMetadata,
                        passed: true,
                        failureReasons: []);
                }
                else
                {
                    resolvedTargetKnowledgeEntryId = ke.Id;
                    evidenceSummary = BuildEvidenceSummary(
                        record.SourceMetadata,
                        passed: true,
                        failureReasons: []);
                }
            }
        }
        else
        {
            // Structural failure: compute metadata-derived summary without gate evaluation.
            evidenceSummary = BuildEvidenceSummary(
                record.SourceMetadata,
                passed: false,
                failureReasons: []);
        }

        var canPromote = blockingReasons.Count == 0;

        return new PromotionPreviewResult(
            ArtifactId: record.ArtifactId,
            CanPromote: canPromote,
            ReviewState: record.ReviewState,
            TargetAssigned: targetAssigned,
            TargetCanonicalName: record.TargetCanonicalName,
            ResolvedTargetKnowledgeEntryId: resolvedTargetKnowledgeEntryId,
            AlreadyPromoted: false,
            PromotedKnowledgeEntryId: null,
            EvidenceGate: evidenceSummary,
            BlockingReasons: blockingReasons,
            WouldWrite: false);
    }

    /// <summary>
    /// Derives an evidence summary from raw SourceMetadata.
    /// Always safe to call regardless of gate evaluation path.
    /// </summary>
    private static PromotionEvidenceGateSummary BuildEvidenceSummary(
        IReadOnlyDictionary<string, string> metadata,
        bool passed,
        IReadOnlyList<string> failureReasons)
    {
        metadata.TryGetValue("evidenceTier", out var tier);
        var tierValue = string.IsNullOrWhiteSpace(tier) ? null : tier;

        metadata.TryGetValue("citations", out var citations);
        var citationCount = CountCitations(citations);

        var mechanismSummaryPresent =
            metadata.TryGetValue("mechanismSummary", out var mechanism) &&
            !string.IsNullOrWhiteSpace(mechanism);

        return new PromotionEvidenceGateSummary(
            Passed: passed,
            Tier: tierValue,
            CitationCount: citationCount,
            MechanismSummaryPresent: mechanismSummaryPresent,
            FailureReasons: failureReasons);
    }

    private static int CountCitations(string? citations)
    {
        if (string.IsNullOrWhiteSpace(citations))
            return 0;

        return citations
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Count(s => !string.IsNullOrWhiteSpace(s));
    }
}
