namespace BioStack.Application.Tests.ScientificResearch;

using BioStack.Application.Abstractions.ScientificResearch;
using BioStack.Application.ScientificResearch;
using BioStack.Application.Services;
using Xunit;

public sealed class ScientificResearchCandidateStagingServiceTests
{
    [Fact]
    public async Task StageFromJobAsync_creates_pending_non_canonical_review_record()
    {
        var provider = new FakeResearchProvider();
        var store = new InMemoryReviewStore();
        var sut = new ScientificResearchCandidateStagingService(provider, store);

        var record = await sut.StageFromJobAsync("job-1");

        Assert.Equal("research:job-1", record.ArtifactId);
        Assert.Equal(TranscriptCandidateReviewRecord.NonCanonical, record.Canonicality);
        Assert.Equal(TranscriptCandidateReviewState.PendingReview, record.ReviewState);
        Assert.Equal(ScientificResearchCandidateStagingService.SourceType, record.SourceType);
        Assert.Equal(ScientificResearchCandidateStagingService.ProviderName, record.Provider);
        Assert.True(record.SourceMetadata.ContainsKey("citations"));
        Assert.True(record.SourceMetadata.ContainsKey("evidenceTier"));
        Assert.Equal("observational", record.SourceMetadata["evidenceTier"]);

        // Idempotent staging
        var again = await sut.StageFromJobAsync("job-1");
        Assert.Equal(record.ArtifactId, again.ArtifactId);
        Assert.Equal(1, store.UpsertCount);
    }

    private sealed class FakeResearchProvider : IScientificResearchProvider
    {
        public Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ScientificResearchArtifact> GetResultAsync(
            string jobId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ScientificResearchArtifact(
                ResearchArtifactId: "artifact-1",
                JobId: jobId,
                ResearchRequestId: "req-1",
                Provider: "biostack-research-sidecar",
                ProviderVersion: "0.1.0",
                Workflow: "research_adverse_events",
                WorkflowVersion: "0.1.0",
                ToolUniverseVersion: "1.4.0",
                Status: ResearchJobStatusCode.Partial,
                Partial: true,
                StartedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
                FinishedAtUtc: DateTimeOffset.UtcNow,
                ToolsInvoked: ["FAERS_count_reactions_by_drug_event"],
                Warnings: ["candidate only"],
                FailureDetails: null,
                ExecutionDevice: "cpu",
                Provenance: new Dictionary<string, string>
                {
                    ["tooluniverse_pin"] = "1.4.0",
                    ["scaffold"] = "false",
                }));

        public Task<ResearchJobStatus> GetStatusAsync(
            string jobId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ResearchJobHandle> SubmitAsync(
            ScientificResearchRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class InMemoryReviewStore : ITranscriptCandidateReviewStore
    {
        private readonly Dictionary<string, TranscriptCandidateReviewRecord> _records = new(StringComparer.Ordinal);
        public int UpsertCount { get; private set; }

        public Task UpsertAsync(TranscriptCandidateReviewRecord record, CancellationToken cancellationToken = default)
        {
            UpsertCount++;
            _records[record.ArtifactId] = record;
            return Task.CompletedTask;
        }

        public Task<TranscriptCandidateReviewRecord?> GetByArtifactIdAsync(
            string artifactId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_records.TryGetValue(artifactId, out var record) ? record : null);

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
            => throw new NotImplementedException();

        public Task<TranscriptCandidateReviewRecord> AssignPromotionTargetAsync(
            string artifactId,
            string targetCanonicalName,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<TranscriptCandidateReviewRecord> RecordPromotionCompletionAsync(
            string artifactId,
            Guid promotedKnowledgeEntryId,
            string promotedAtUtc,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
