namespace BioStack.Application.Services;

public interface IYouTubeTranscriptMcpClient
{
    Task<YouTubeTranscriptMcpResponse> GetTranscriptAsync(
        TranscriptSourceReference sourceReference,
        CancellationToken cancellationToken = default);
}

public sealed record YouTubeTranscriptMcpResponse(
    string Provider,
    string RetrievedAtIsoUtc,
    IReadOnlyList<TranscriptSegment> Segments,
    IReadOnlyDictionary<string, string> Metadata);
