namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class YouTubeTranscriptSourceMaterialProviderTests
{
    [Fact]
    public async Task DisabledByDefault_FailsDeterministically_AndDoesNotCallMcp()
    {
        var mcp = new SpyYouTubeTranscriptMcpClient();
        var options = Options.Create(new YouTubeTranscriptProviderOptions());
        var provider = new YouTubeTranscriptSourceMaterialProvider(mcp, options);

        var ex = await Assert.ThrowsAsync<TranscriptSourceMaterialProviderException>(() =>
            provider.ResolveAsync(new TranscriptSourceReference(
                SourceType: "video_url",
                SourceUrl: "https://www.youtube.com/watch?v=test123")));

        Assert.Equal("transcript_provider_disabled", ex.Failure.Code);
        Assert.False(mcp.WasCalled);
    }

    [Fact]
    public async Task Enabled_MapsMcpResponse_ToTranscriptSourceMaterialResult()
    {
        var mcpResponse = new YouTubeTranscriptMcpResponse(
            Provider: "youtube_mcp_test",
            RetrievedAtIsoUtc: "2026-01-01T00:00:00Z",
            Segments:
            [
                new TranscriptSegment(
                    Sequence: 1,
                    Text: "segment one",
                    StartSeconds: 0.0,
                    DurationSeconds: 2.5),
                new TranscriptSegment(
                    Sequence: 2,
                    Text: "segment two",
                    StartSeconds: 2.5,
                    DurationSeconds: 2.0)
            ],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["videoId"] = "test123",
                ["source"] = "mcp"
            });

        var mcp = new SpyYouTubeTranscriptMcpClient(mcpResponse);
        var options = Options.Create(new YouTubeTranscriptProviderOptions { Enabled = true });
        var provider = new YouTubeTranscriptSourceMaterialProvider(mcp, options);

        var sourceReference = new TranscriptSourceReference(
            SourceType: "video_url",
            SourceUrl: "https://www.youtube.com/watch?v=test123");

        var resolved = await provider.ResolveAsync(sourceReference);

        Assert.True(mcp.WasCalled);
        Assert.Equal(sourceReference, resolved.SourceReference);
        Assert.Equal("youtube_mcp_test", resolved.Provider);
        Assert.Equal("2026-01-01T00:00:00Z", resolved.RetrievedAtIsoUtc);
        Assert.Equal(2, resolved.Segments.Count);
        Assert.Equal("segment one", resolved.Segments[0].Text);
        Assert.Equal("test123", resolved.Metadata["videoId"]);
        Assert.False(resolved.IsDeterministicFixture);
    }

    [Fact]
    public async Task Enabled_Path_RemainsProviderOnly_NoCanonicalPromotionOrSafetyMetadata()
    {
        var mcpResponse = new YouTubeTranscriptMcpResponse(
            Provider: "youtube_mcp_test",
            RetrievedAtIsoUtc: "2026-01-01T00:00:00Z",
            Segments:
            [
                new TranscriptSegment(
                    Sequence: 1,
                    Text: "provider only transcript",
                    StartSeconds: 0.0,
                    DurationSeconds: 1.0)
            ],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["videoId"] = "provider-only"
            });

        var mcp = new SpyYouTubeTranscriptMcpClient(mcpResponse);
        var options = Options.Create(new YouTubeTranscriptProviderOptions { Enabled = true });
        var provider = new YouTubeTranscriptSourceMaterialProvider(mcp, options);

        var resolved = await provider.ResolveAsync(new TranscriptSourceReference(
            SourceType: "video_url",
            SourceUrl: "https://www.youtube.com/watch?v=provider-only"));

        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("knowledgeentryid", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("canonical", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("promotion", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("candidate", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("safety", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("classification", StringComparison.OrdinalIgnoreCase));
        Assert.True(mcp.WasCalled);
    }

    private sealed class SpyYouTubeTranscriptMcpClient : IYouTubeTranscriptMcpClient
    {
        private readonly YouTubeTranscriptMcpResponse? _response;

        public SpyYouTubeTranscriptMcpClient(YouTubeTranscriptMcpResponse? response = null)
        {
            _response = response;
        }

        public bool WasCalled { get; private set; }

        public Task<YouTubeTranscriptMcpResponse> GetTranscriptAsync(
            TranscriptSourceReference sourceReference,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            cancellationToken.ThrowIfCancellationRequested();

            if (_response is null)
            {
                throw new InvalidOperationException("No response configured for MCP spy.");
            }

            return Task.FromResult(_response);
        }
    }
}
