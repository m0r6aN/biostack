namespace BioStack.Application.Tests.Services;

using System.Text;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using Xunit;

public sealed class ProtocolIngestionPdfReproductionTests
{
    [Fact]
    public async Task ExtractAsync_AlreadyCancelledRequest_DoesNotEnterRegexExtraction()
    {
        var extractor = new PdfProtocolExtractor();
        var request = new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload,
            null,
            null,
            "synthetic-protocol.pdf",
            "application/pdf",
            Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << /Type /Page >> stream\n(BPC-157 500mcg daily) Tj\nendstream\nendobj"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            extractor.ExtractAsync(request, cancellation.Token));
    }
}
