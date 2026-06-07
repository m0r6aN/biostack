namespace BioStack.Application.Services;

public interface ITranscriptSourceMaterialProvider
{
    Task<TranscriptSourceMaterialResult> ResolveAsync(
        TranscriptSourceReference sourceReference,
        CancellationToken cancellationToken = default);
}

public interface ITranscriptSourceMaterialProviderFailure
{
    TranscriptSourceMaterialResolutionFailure Failure { get; }
}

public sealed record TranscriptSourceReference(
    string SourceType,
    string SourceUrl);

public sealed record TranscriptSourceMaterialResult(
    TranscriptSourceReference SourceReference,
    string Provider,
    IReadOnlyList<TranscriptSegment> Segments,
    string RetrievedAtIsoUtc,
    IReadOnlyDictionary<string, string> Metadata,
    bool IsDeterministicFixture);

public sealed record TranscriptSegment(
    int Sequence,
    string Text,
    double? StartSeconds,
    double? DurationSeconds);

public sealed record TranscriptSourceMaterialResolutionFailure(
    string Code,
    string Message);
