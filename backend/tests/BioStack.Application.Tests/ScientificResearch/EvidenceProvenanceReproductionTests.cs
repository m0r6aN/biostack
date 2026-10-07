namespace BioStack.Application.Tests.ScientificResearch;

using BioStack.Application.Abstractions.ScientificResearch;
using BioStack.Application.ScientificResearch;
using BioStack.Application.Services;
using Xunit;

/// <summary>
/// Regression coverage for evidence-staging eligibility and external source attribution.
/// All artifacts, identifiers, and locators are deterministic synthetic values.
/// </summary>
public sealed class EvidenceProvenanceReproductionTests
{
    [Fact]
    public async Task Failed_research_artifact_must_not_reach_an_open_evidence_gate()
    {
        var store = new InMemoryReviewStore();
        var staging = CreateStaging(ResearchJobStatusCode.Failed, store);

        var exception = await Assert.ThrowsAsync<ScientificResearchProviderException>(
            () => staging.StageFromJobAsync("synthetic-failed-job"));

        Assert.Equal("artifact_not_stageable", exception.ErrorCode);
        Assert.Equal(0, store.LookupCount);
        Assert.Equal(0, store.UpsertCount);
    }

    [Theory]
    [InlineData(ResearchJobStatusCode.Cancelled)]
    [InlineData(ResearchJobStatusCode.RejectedByPolicy)]
    [InlineData(ResearchJobStatusCode.Queued)]
    [InlineData(ResearchJobStatusCode.ResolvingIdentity)]
    [InlineData(ResearchJobStatusCode.GatheringEvidence)]
    [InlineData(ResearchJobStatusCode.Normalizing)]
    [InlineData(ResearchJobStatusCode.Completed)]
    public async Task Ineligible_research_artifact_must_not_enter_review_staging(
        ResearchJobStatusCode status)
    {
        var store = new InMemoryReviewStore();
        var staging = CreateStaging(status, store);

        var exception = await Assert.ThrowsAsync<ScientificResearchProviderException>(
            () => staging.StageFromJobAsync("synthetic-ineligible-job"));

        Assert.Equal("artifact_not_stageable", exception.ErrorCode);
        Assert.Equal(0, store.LookupCount);
        Assert.Equal(0, store.UpsertCount);
    }

    [Fact]
    public void Synthetic_job_identifier_must_not_satisfy_source_attribution()
    {
        var result = EvaluateCitations(
            "research_job:synthetic-failed-job | workflow:synthetic_workflow | " +
            "tooluniverse:0.0-test | tool:synthetic_tool");

        Assert.False(result.IsGateOpen);
        Assert.Equal("missing_external_source_locator", result.RejectionCode);
    }

    [Theory]
    [InlineData(ResearchJobStatusCode.PendingReview, true)]
    [InlineData(ResearchJobStatusCode.Partial, false)]
    public async Task Candidate_research_artifact_stages_once_and_remains_non_canonical(
        ResearchJobStatusCode status,
        bool repeatStaging)
    {
        var store = new InMemoryReviewStore();
        var staging = CreateStaging(status, store);

        var record = await staging.StageFromJobAsync("synthetic-candidate-job");

        Assert.Equal(TranscriptCandidateReviewRecord.NonCanonical, record.Canonicality);
        Assert.Equal(TranscriptCandidateReviewState.PendingReview, record.ReviewState);
        Assert.Equal(1, store.UpsertCount);

        if (repeatStaging)
        {
            var repeated = await staging.StageFromJobAsync("synthetic-candidate-job");
            Assert.Equal(record, repeated);
            Assert.Equal(1, store.UpsertCount);
        }
    }

    [Theory]
    [InlineData("https://example.test/study-1", true)]
    [InlineData("http://example.test/study-1", false)]
    [InlineData("DOI:10.1000/182", false)]
    [InlineData("  PMID:12345678  ", false)]
    public void Stable_external_source_locator_opens_gate(
        string locator,
        bool includeInternalProvenance)
    {
        var citations = includeInternalProvenance
            ? $"research_job:synthetic-job | tool:synthetic-tool | {locator}"
            : locator;

        var result = EvaluateCitations(citations);

        Assert.True(result.IsGateOpen);
        Assert.Null(result.RejectionCode);
    }

    [Theory]
    [InlineData("ftp://example.test/study-1")]
    [InlineData("tool:https://example.test/study-1")]
    [InlineData("https:///study-1")]
    [InlineData("doi:")]
    [InlineData("doi:10.1000")]
    [InlineData("doi:10.1000/has space")]
    [InlineData("pmid:")]
    [InlineData("pmid:12x")]
    public void Invalid_or_internal_locator_keeps_gate_closed(string citations)
    {
        var result = EvaluateCitations(citations);

        Assert.False(result.IsGateOpen);
        Assert.Equal("missing_external_source_locator", result.RejectionCode);
    }

    private static ScientificResearchCandidateStagingService CreateStaging(
        ResearchJobStatusCode status,
        InMemoryReviewStore store)
        => new(new SyntheticResearchProvider(status), store);

    private static EvidenceGateResult EvaluateCitations(string citations)
    {
        var gate = new EvidenceGate();
        return gate.Evaluate(new EvidenceGateRequest(
            ReviewState: TranscriptCandidateReviewState.ReviewApprovedForPromotion,
            TargetCanonicalName: "Synthetic Compound",
            IsDeterministicFixture: false,
            SourceMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["evidenceTier"] = EvidenceTierCode.Observational,
                ["citations"] = citations,
                ["summary"] = "Synthetic evidence metadata for deterministic testing.",
            }));
    }

    private sealed class SyntheticResearchProvider(ResearchJobStatusCode status)
        : IScientificResearchProvider
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
                Status: status,
                Partial: status == ResearchJobStatusCode.Partial,
                StartedAtUtc: DateTimeOffset.UnixEpoch,
                FinishedAtUtc: DateTimeOffset.UnixEpoch.AddSeconds(1),
                ToolsInvoked: ["synthetic_tool"],
                Warnings: ["synthetic candidate warning"],
                FailureDetails: null,
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

        public int LookupCount { get; private set; }

        public int UpsertCount { get; private set; }

        public Task<TranscriptCandidateReviewRecord?> GetByArtifactIdAsync(
            string artifactId,
            CancellationToken cancellationToken = default)
        {
            LookupCount++;
            return Task.FromResult(_records.TryGetValue(artifactId, out var record) ? record : null);
        }

        public Task UpsertAsync(
            TranscriptCandidateReviewRecord record,
            CancellationToken cancellationToken = default)
        {
            UpsertCount++;
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
