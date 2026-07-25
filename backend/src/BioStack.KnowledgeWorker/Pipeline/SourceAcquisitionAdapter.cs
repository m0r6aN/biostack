namespace BioStack.KnowledgeWorker.Pipeline;

public enum SourceAcquisitionBatchStatus
{
    Completed = 0,
    NoMatch = 1,
    RateLimited = 2,
}

public sealed record SourceAcquisitionCandidate(
    string RequestId,
    string CompoundName,
    string SourceRegistryId,
    string SourceItemId,
    string SourceUrl,
    string QueryUrl,
    string SourcePublicationOrUpdateDate,
    DateTimeOffset RetrievedAtUtc,
    string RightsReviewStatusAtRetrieval,
    string RegistryBindingSha256,
    string TransformationPipelineVersion,
    string HumanReviewStatus,
    IReadOnlyList<string> EvidenceLimitations,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Fields);

public sealed record SourceAcquisitionBatch(
    SourceAcquisitionBatchStatus Status,
    IReadOnlyList<SourceAcquisitionCandidate> Candidates,
    bool Truncated,
    string? RetryAfter);

public interface ISourceAcquisitionAdapter
{
    string SourceId { get; }
    string AdapterId { get; }

    Task<SourceAcquisitionBatch> AcquireAsync(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc,
        CancellationToken cancellationToken = default);
}

public sealed class SourceAcquisitionException(string code, string message)
    : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
