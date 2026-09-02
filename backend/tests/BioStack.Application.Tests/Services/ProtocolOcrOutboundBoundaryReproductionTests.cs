namespace BioStack.Application.Tests.Services;

using System.Net;
using System.Text;
using BioStack.Application.Services;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class ProtocolOcrOutboundBoundaryReproductionTests
{
    [Fact]
    public async Task ConfiguredOcr_DoesNotTransmitRawBytesWithoutExplicitOutboundAuthorization()
    {
        var syntheticDocumentBytes = "synthetic private document payload"u8.ToArray();
        var handler = new InterceptingHandler();
        var service = new AzureVisionProtocolOcrService(
            new SingleClientFactory(new HttpClient(handler)),
            Options.Create(new ProtocolOcrOptions
            {
                Endpoint = "https://synthetic-ocr.invalid",
                ApiKey = "synthetic-test-key",
            }));

        await service.ExtractAsync(syntheticDocumentBytes, "synthetic-scan.png");

        var rawBytesCrossedBoundary = handler.RequestCount > 0 &&
            syntheticDocumentBytes.SequenceEqual(handler.CapturedBody ?? []);
        Assert.False(
            rawBytesCrossedBoundary,
            $"Raw document bytes reached the configured OCR endpoint without an explicit outbound authorization boundary. " +
            $"interceptedRequests={handler.RequestCount}; destination={handler.CapturedUri}");
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class InterceptingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public byte[]? CapturedBody { get; private set; }
        public Uri? CapturedUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            CapturedUri = request.RequestUri;
            CapturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"readResult\":{\"blocks\":[]}}",
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }
}
