namespace BioStack.Application.Tests.Services;

using System.Net;
using System.Text;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using Xunit;

public sealed class LinkProtocolExtractorSecurityTests
{
    [Theory]
    [InlineData("https://127.0.0.1/protocol.txt")]
    [InlineData("https://10.0.0.1/protocol.txt")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://192.168.1.10/protocol.txt")]
    [InlineData("https://[::1]/protocol.txt")]
    [InlineData("https://[fd00::1]/protocol.txt")]
    public async Task ExtractAsync_PrivateOrLocalDestination_IsRejectedBeforeSend(string url)
    {
        var handler = new SequenceHandler(_ => PlainTextResponse("should not be fetched"));
        var extractor = CreateExtractor(handler);

        var exception = await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            extractor.ExtractAsync(LinkRequest(url)));

        Assert.Contains("public internet", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.SendCount);
    }

    [Theory]
    [InlineData("http://1.1.1.1/protocol.txt")]
    [InlineData("https://user:password@1.1.1.1/protocol.txt")]
    [InlineData("https://1.1.1.1:8443/protocol.txt")]
    public async Task ExtractAsync_NonStandardAuthority_IsRejectedBeforeSend(string url)
    {
        var handler = new SequenceHandler(_ => PlainTextResponse("should not be fetched"));
        var extractor = CreateExtractor(handler);

        await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            extractor.ExtractAsync(LinkRequest(url)));

        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task ExtractAsync_PublicRedirectToPrivateAddress_IsRejected()
    {
        var handler = new SequenceHandler(sendCount => sendCount == 1
            ? RedirectResponse("https://127.0.0.1/private.txt")
            : PlainTextResponse("should not be fetched"));
        var extractor = CreateExtractor(handler);

        await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            extractor.ExtractAsync(LinkRequest("https://1.1.1.1/protocol.txt")));

        Assert.Equal(1, handler.SendCount);
    }

    [Fact]
    public async Task ExtractAsync_DeclaredOversizedResponse_IsRejected()
    {
        var response = PlainTextResponse("small body");
        response.Content.Headers.ContentLength = LinkProtocolExtractor.MaxLinkResponseBytes + 1;
        var extractor = CreateExtractor(new SequenceHandler(_ => response));

        var exception = await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            extractor.ExtractAsync(LinkRequest("https://1.1.1.1/protocol.txt")));

        Assert.Contains("too large", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_StreamedOversizedResponse_IsRejected()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(new byte[LinkProtocolExtractor.MaxLinkResponseBytes + 1])),
        };
        response.Content.Headers.ContentType = new("text/plain");
        var extractor = CreateExtractor(new SequenceHandler(_ => response));

        var exception = await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            extractor.ExtractAsync(LinkRequest("https://1.1.1.1/protocol.txt")));

        Assert.Contains("too large", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_TooManyRedirects_IsRejected()
    {
        var handler = new SequenceHandler(_ => RedirectResponse("/next.txt"));
        var extractor = CreateExtractor(handler);

        var exception = await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            extractor.ExtractAsync(LinkRequest("https://1.1.1.1/protocol.txt")));

        Assert.Contains("redirected too many times", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, handler.SendCount);
    }

    [Fact]
    public async Task ExtractAsync_PublicPlainText_ReturnsExtractedText()
    {
        var extractor = CreateExtractor(new SequenceHandler(_ => PlainTextResponse("BPC-157 500mcg daily")));

        var result = await extractor.ExtractAsync(LinkRequest("https://1.1.1.1/protocol.txt"));

        Assert.Equal("BPC-157 500mcg daily", result.ExtractedText);
    }

    private static LinkProtocolExtractor CreateExtractor(HttpMessageHandler handler) =>
        new(new StubHttpClientFactory(handler));

    private static ProtocolIngestionRequest LinkRequest(string url) =>
        new(ProtocolInputType.Link, null, url, "protocol.txt", null, null);

    private static HttpResponseMessage PlainTextResponse(string body)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        return response;
    }

    private static HttpResponseMessage RedirectResponse(string location)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class SequenceHandler(Func<int, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(responseFactory(SendCount));
        }
    }
}
