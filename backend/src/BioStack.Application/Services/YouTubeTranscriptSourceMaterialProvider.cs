namespace BioStack.Application.Services;

using Microsoft.Extensions.Options;

public sealed class YouTubeTranscriptSourceMaterialProvider : ITranscriptSourceMaterialProvider
{
    private readonly IYouTubeTranscriptMcpClient _mcpClient;
    private readonly IOptions<YouTubeTranscriptProviderOptions> _options;

    public YouTubeTranscriptSourceMaterialProvider(
        IYouTubeTranscriptMcpClient mcpClient,
        IOptions<YouTubeTranscriptProviderOptions> options)
    {
        _mcpClient = mcpClient;
        _options = options;
    }

    public async Task<TranscriptSourceMaterialResult> ResolveAsync(
        TranscriptSourceReference sourceReference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.Value ?? new YouTubeTranscriptProviderOptions();
        if (!options.Enabled)
        {
            throw new TranscriptSourceMaterialProviderException(
                new TranscriptSourceMaterialResolutionFailure(
                    Code: "transcript_provider_disabled",
                    Message: "YouTube transcript provider is disabled."));
        }

        var response = await _mcpClient.GetTranscriptAsync(sourceReference, cancellationToken);

        return new TranscriptSourceMaterialResult(
            SourceReference: sourceReference,
            Provider: string.IsNullOrWhiteSpace(response.Provider) ? "youtube_mcp" : response.Provider,
            Segments: response.Segments,
            RetrievedAtIsoUtc: response.RetrievedAtIsoUtc,
            Metadata: response.Metadata,
            IsDeterministicFixture: false);
    }
}

public sealed class TranscriptSourceMaterialProviderException : Exception, ITranscriptSourceMaterialProviderFailure
{
    public TranscriptSourceMaterialProviderException(TranscriptSourceMaterialResolutionFailure failure)
        : base(failure.Message)
    {
        Failure = failure;
    }

    public TranscriptSourceMaterialResolutionFailure Failure { get; }
}
