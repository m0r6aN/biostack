namespace BioStack.Application.Tests.ScientificResearch;

using BioStack.Application.Abstractions.ScientificResearch;
using BioStack.Application.ScientificResearch;
using BioStack.Application.Services;
using Xunit;

/// <summary>
/// Test-only reproductions for audit findings R1 and E1.
///
/// Each assertion states the intended provenance invariant and is expected to
/// fail against the pinned implementation until the production boundary is
/// hardened. All artifacts and identifiers are synthetic.
/// </summary>
public sealed class EvidenceProvenanceReproductionTests
{
    [Fact]
    public async Task Failed_research_artifact_must_not_reach_an_open_evidence_gate()
    {
        var store = new InMemoryReviewStore();
        var staging = new ScientificResearchCandidateStagingService(
            new FailedResearchProvider(),
            store);

        var staged = await staging.StageFromJobAsync("synthetic-failed-job");
        var gate = new EvidenceGate();
        var result = gate.Evaluate(new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: "Synthetic Compound",
            IsDeterministicFixture: staged.IsDeterministicFixture,
            SourceMetadata: staged.SourceMetadata));

        Assert.False(
            result.IsGateOpen,
            "A failed research artifact was staged as observational evidence, and its generated job/workflow/tool labels opened the evidence gate.");
    }

    [Fact]
    public void Synthetic_job_identifier_must_not_satisfy_source_attribution()
    {
        var gate = new EvidenceGate();
        var request = new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: "Synthetic Compound",
            IsDeterministicFixture: false,
            SourceMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["evidenceTier"] = EvidenceTierCode.Observational,
                ["citations"] = "research_job:synthetic-failed-job | workflow:synthetic_workflow | tool:synthetic_tool",
                ["summary"] = "Synthetic test metadata without a source locator.",
            });

        var result = gate.Evaluate(request);

        Assert.False(
            result.IsGateOpen,
            "Synthetic job, workflow, and tool labels were accepted as source-attributing citations.");
    }

    private sealed class FailedResearchProvider : IScientificResearchProvider
    {
        public Task<ScientificResearchArtifact> GetResultAsync(
            string jobId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ScientificResearchArtifact(
                ResearchArtifactId: "synthetic-artifact",
                JobId: jobId,
                ResearchRequestId: "synthetic-request",
                Provider: "synthetic-provider",
                ProviderVersion: "0.0-test",
                Workflow: "synthetic_workflow",
                WorkflowVersion: "0.0-test",
                ToolUniverseVersion: "0.0-test",
                Status: ResearchJobStatusCode.Failed,
                Partial: false,
                StartedAtUtc: DateTimeOffset.UnixEpoch,
                FinishedAtUtc: DateTimeOffset.UnixEpoch.AddSeconds(1),
                ToolsInvoked: ["synthetic_tool"],
                Warnings: ["synthetic failure"],
                FailureDetails: "synthetic failure details",
                ExecutionDevice: "test",
                Provenance: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["correlation_id"] = "synthetic-correlation",
                }));

        public Task<ResearchJobHandle> SubmitAsync(
            ScientificResearchRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ResearchJobStatus> GetStatusAsync(
            string jobId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task CancelAsync(
            string jobId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class InMemoryReviewStore : ITranscriptCandidateReviewStore
    {
        private readonly Dictionary<string, TranscriptCandidateReviewRecord> _records = new(StringComparer.Ordinal);

        public Task<TranscriptCandidateReviewRecord?> GetByArtifactIdAsync(
            string artifactId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_records.TryGetValue(artifactId, out var record) ? record : null);

        public Task UpsertAsync(
            TranscriptCandidateReviewRecord record,
            CancellationToken cancellationToken = default)
        {
            _records[record.ArtifactId] = record;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TranscriptCandidateReviewRecord>> ListAsync(
            TranscriptCandidateReviewFilter filter,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TranscriptCandidateReviewRecord>>(_records.Values.ToList());

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

        public Task<TranscriptCandidateReviewRecord> RecordPromotionCompletionAsync(
            string artifactId,
            Guid promotedKnowledgeEntryId,
            string promotedAtUtc,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
