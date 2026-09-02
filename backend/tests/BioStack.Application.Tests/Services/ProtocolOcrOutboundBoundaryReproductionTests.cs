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
        var factory = new SingleClientFactory(new HttpClient(handler));
        var service = CreateService(factory, new StubConsentGate(_ => Task.FromResult(false)));

        await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            service.ExtractAsync(syntheticDocumentBytes, "synthetic-scan.png"));

        var rawBytesCrossedBoundary = handler.RequestCount > 0 &&
            syntheticDocumentBytes.SequenceEqual(handler.CapturedBody ?? []);
        Assert.False(
            rawBytesCrossedBoundary,
            $"Raw document bytes reached the configured OCR endpoint without an explicit outbound authorization boundary. " +
            $"interceptedRequests={handler.RequestCount}; destination={handler.CapturedUri}");
        Assert.Equal(0, factory.CreateClientCount);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task ConfiguredOcr_ConsentGateFailure_DoesNotSendProviderRequest()
    {
        var handler = new InterceptingHandler();
        var factory = new SingleClientFactory(new HttpClient(handler));
        var service = CreateService(
            factory,
            new StubConsentGate(_ => Task.FromException<bool>(new InvalidOperationException("Synthetic consent gate failure."))));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExtractAsync("synthetic private document payload"u8.ToArray(), "synthetic-scan.png"));

        Assert.Equal(0, factory.CreateClientCount);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task ConfiguredOcr_CurrentConsentGranted_SendsExactlyOneSyntheticRequest()
    {
        var syntheticDocumentBytes = "synthetic private document payload"u8.ToArray();
        var handler = new InterceptingHandler();
        var factory = new SingleClientFactory(new HttpClient(handler));
        var service = CreateService(factory, new StubConsentGate(_ => Task.FromResult(true)));

        var result = await service.ExtractAsync(syntheticDocumentBytes, "synthetic-scan.png");

        Assert.Equal(1, factory.CreateClientCount);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(new Uri("https://synthetic-ocr.invalid/computervision/imageanalysis:analyze?api-version=2024-02-01&features=read"), handler.CapturedUri);
        Assert.True(syntheticDocumentBytes.SequenceEqual(handler.CapturedBody ?? []));
        Assert.Equal("BPC-157 500mcg daily", result.ExtractedText);
    }

    private static AzureVisionProtocolOcrService CreateService(
        SingleClientFactory factory,
        IConsentGate consentGate) =>
        new(
            factory,
            Options.Create(new ProtocolOcrOptions
            {
                Endpoint = "https://synthetic-ocr.invalid",
                ApiKey = "synthetic-test-key",
            }),
            consentGate);

    private sealed class StubConsentGate(Func<CancellationToken, Task<bool>> isGranted) : IConsentGate
    {
        public Task<bool> IsConsentGrantedAsync(CancellationToken cancellationToken = default) =>
            isGranted(cancellationToken);

        public Task<ConsentStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ConsentStatus> RecordAsync(string? requestedVersion, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ConsentStatus> DeclineAsync(string? requestedVersion, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public int CreateClientCount { get; private set; }

        public HttpClient CreateClient(string name)
        {
            CreateClientCount++;
            return client;
        }
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
                    "{\"readResult\":{\"blocks\":[{\"lines\":[{\"text\":\"BPC-157 500mcg daily\"}]}]}}",
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }
}
