namespace BioStack.Application.Services;

public sealed class NullYouTubeTranscriptMcpClient : IYouTubeTranscriptMcpClient
{
    public Task<YouTubeTranscriptMcpResponse> GetTranscriptAsync(
        TranscriptSourceReference sourceReference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        throw new TranscriptSourceMaterialProviderException(
            new TranscriptSourceMaterialResolutionFailure(
                Code: "transcript_provider_disabled",
                Message: "YouTube transcript MCP client is unavailable."));
    }
}
