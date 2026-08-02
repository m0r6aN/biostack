namespace BioStack.Application.ScientificResearch;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioStack.Application.Abstractions.ScientificResearch;
using BioStack.Application.Services;

/// <summary>
/// Stages sidecar research artifacts into the existing non-canonical review lifecycle.
/// Reuses staged candidate review store; never writes canonical knowledge.
/// </summary>
public interface IScientificResearchCandidateStagingService
{
    Task<TranscriptCandidateReviewRecord> StageFromJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}

public sealed class ScientificResearchCandidateStagingService(
    IScientificResearchProvider researchProvider,
    ITranscriptCandidateReviewStore reviewStore) : IScientificResearchCandidateStagingService
{
    public const string SourceType = "scientific_research";
    public const string ProviderName = "biostack-research-sidecar";
    public const string ArtifactIdPrefix = "research:";

    public async Task<TranscriptCandidateReviewRecord> StageFromJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        var artifact = await researchProvider.GetResultAsync(jobId, cancellationToken);
        var artifactId = ArtifactIdPrefix + artifact.JobId;
        var now = DateTimeOffset.UtcNow.ToString("O");

        var existing = await reviewStore.GetByArtifactIdAsync(artifactId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var signature = ComputeSignature(artifact);
        var metadata = BuildMetadata(artifact);
        var sourceUrl = $"biostack://research/jobs/{artifact.JobId}";

        var record = TranscriptCandidateReviewRecord.Create(
            artifactId: artifactId,
            canonicality: TranscriptCandidateReviewRecord.NonCanonical,
            reviewState: TranscriptCandidateReviewState.PendingReview,
            sourceType: SourceType,
            sourceUrl: sourceUrl,
            provider: ProviderName,
            isDeterministicFixture: false,
            segmentCount: Math.Max(1, artifact.ToolsInvoked.Count),
            segmentSnapshotSignature: signature,
            sourceMetadata: metadata,
            createdAtUtc: now,
            updatedAtUtc: now,
            targetCanonicalName: null,
            intakeRequestId: null);

        await reviewStore.UpsertAsync(record, cancellationToken);
        return record;
    }

    private static string ComputeSignature(ScientificResearchArtifact artifact)
    {
        var payload = string.Join(
            "|",
            artifact.ResearchArtifactId,
            artifact.JobId,
            artifact.Workflow,
            artifact.Status.ToString(),
            artifact.Partial,
            string.Join(",", artifact.ToolsInvoked.OrderBy(x => x, StringComparer.Ordinal)),
            string.Join(";", artifact.Warnings.Take(20)));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(ScientificResearchArtifact artifact)
    {
        var citations = new List<string>
        {
            $"research_job:{artifact.JobId}",
            $"workflow:{artifact.Workflow}",
        };
        if (!string.IsNullOrWhiteSpace(artifact.ToolUniverseVersion))
        {
            citations.Add($"tooluniverse:{artifact.ToolUniverseVersion}");
        }

        citations.AddRange(artifact.ToolsInvoked.Select(t => $"tool:{t}"));

        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["evidenceTier"] = "observational",
            ["citations"] = string.Join(" | ", citations.Distinct(StringComparer.Ordinal)),
            ["mechanismSummary"] = "Candidate scientific research packet staged for human review; not canonical knowledge.",
            ["summary"] = string.Join(" ", artifact.Warnings.Take(5)),
            ["researchArtifactId"] = artifact.ResearchArtifactId,
            ["provider"] = artifact.Provider,
            ["providerVersion"] = artifact.ProviderVersion,
            ["workflow"] = artifact.Workflow,
            ["workflowVersion"] = artifact.WorkflowVersion,
            ["partial"] = artifact.Partial ? "true" : "false",
            ["status"] = artifact.Status.ToString(),
            ["executionDevice"] = artifact.ExecutionDevice,
            ["failureDetails"] = artifact.FailureDetails ?? string.Empty,
            ["toolsInvoked"] = string.Join(",", artifact.ToolsInvoked),
            ["provenanceJson"] = JsonSerializer.Serialize(artifact.Provenance),
            ["subjectHint"] = artifact.Provenance.TryGetValue("correlation_id", out var corr) ? corr : string.Empty,
        };

        return map;
    }
}
