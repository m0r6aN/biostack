namespace BioStack.Application.Services;

using BioStack.Infrastructure.Knowledge;

public interface ITranscriptCandidatePromotionService
{
    /// <summary>
    /// Executes promotion for the staged review identified by <paramref name="artifactId"/>.
    ///
    /// Pre-conditions (all enforced before any write):
    ///   - Record exists (404 if not)
    ///   - ReviewState == review_approved_for_promotion (409 if not)
    ///   - TargetCanonicalName is set (409 if not)
    ///   - IsDeterministicFixture == false (409 if true)
    ///   - Target KnowledgeEntry exists in knowledge base (404 if not)
    ///
    /// Idempotency: if PromotedKnowledgeEntryId and PromotedAtUtc are already stamped,
    /// returns the existing record without writing again (200 replay).
    /// </summary>
    Task<TranscriptCandidateReviewRecord> ExecutePromotionAsync(
        string artifactId,
        CancellationToken cancellationToken = default);
}

public sealed class TranscriptCandidatePromotionService : ITranscriptCandidatePromotionService
{
    private readonly ITranscriptCandidateReviewStore _reviewStore;
    private readonly IKnowledgeSource _knowledgeSource;
    private readonly IEvidenceGate _evidenceGate;

    public TranscriptCandidatePromotionService(
        ITranscriptCandidateReviewStore reviewStore,
        IKnowledgeSource knowledgeSource,
        IEvidenceGate evidenceGate)
    {
        _reviewStore = reviewStore;
        _knowledgeSource = knowledgeSource;
        _evidenceGate = evidenceGate;
    }

    public async Task<TranscriptCandidateReviewRecord> ExecutePromotionAsync(
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

        // State guard: must be approved for promotion.
        if (!string.Equals(record.ReviewState, TranscriptCandidateReviewState.ReviewApprovedForPromotion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Promotion can only be executed on records in state '{TranscriptCandidateReviewState.ReviewApprovedForPromotion}'. " +
                $"Current state: '{record.ReviewState}'.");
        }

        // Target guard: promotion target must have been assigned.
        if (string.IsNullOrWhiteSpace(record.TargetCanonicalName))
        {
            throw new InvalidOperationException(
                "Promotion cannot be executed: no promotion target has been assigned to this record.");
        }

        // Fixture guard (defense in depth — also enforced at PR14A assignment time).
        if (record.IsDeterministicFixture)
        {
            throw new InvalidOperationException(
                "Promotion cannot be executed on a deterministic fixture record.");
        }

        // Idempotency: if already promoted, return the existing result without writing again.
        if (record.PromotedKnowledgeEntryId is not null && record.PromotedAtUtc is not null)
        {
            return record;
        }

        // KE-4 Evidence gate — fail-closed; evaluated after idempotency so replays bypass the check.
        var gateRequest = new EvidenceGateRequest(
            ReviewState: record.ReviewState,
            TargetCanonicalName: record.TargetCanonicalName,
            IsDeterministicFixture: record.IsDeterministicFixture,
            SourceMetadata: record.SourceMetadata);

        var gateResult = _evidenceGate.Evaluate(gateRequest);
        if (!gateResult.IsGateOpen)
        {
            throw new EvidenceGateViolationException(
                rejectionCode: gateResult.RejectionCode!,
                message: gateResult.RejectionReason!);
        }

        // Look up the target knowledge entry.
        var ke = await _knowledgeSource.GetCompoundAsync(record.TargetCanonicalName, cancellationToken);
        if (ke is null)
        {
            throw new KeyNotFoundException(
                $"Knowledge entry '{record.TargetCanonicalName}' not found in knowledge base.");
        }

        // Append the transcript source URL to the KE's source references (de-duplicate).
        if (!ke.SourceReferences.Contains(record.SourceUrl, StringComparer.Ordinal))
        {
            ke.SourceReferences.Add(record.SourceUrl);
            await _knowledgeSource.UpsertCompoundAsync(ke, cancellationToken);
        }

        var promotedAtUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        return await _reviewStore.RecordPromotionCompletionAsync(
            artifactId: artifactId,
            promotedKnowledgeEntryId: ke.Id,
            promotedAtUtc: promotedAtUtc,
            cancellationToken: cancellationToken);
    }
}
