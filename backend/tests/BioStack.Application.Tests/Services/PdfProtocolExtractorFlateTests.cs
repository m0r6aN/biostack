namespace BioStack.Application.Tests.Services;

using System.IO.Compression;
using System.Text;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using Xunit;

// Regression tests for the protocol-upload remediation: ordinary PDFs from
// Word, Google Docs, and scanners store their page content in /FlateDecode
// (zlib) streams. Before this change the extractor only saw the raw bytes and
// such uploads collapsed into "This PDF did not expose readable text" or OCR
// fallback. These tests pin the FlateDecode extraction behavior directly on
// PdfProtocolExtractor plus one ingestion-service round trip.
public sealed class PdfProtocolExtractorFlateTests
{
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    [Fact]
    public async Task ExtractAsync_FlateDecodeContentStream_ExtractsTextOperators()
    {
        var pdf = BuildPdf(
            ("/Type /Page", null),
            ("<< /Length {0} /Filter /FlateDecode >>", Compress(
                "BT /F1 12 Tf 72 720 Td (BPC-157 500mcg daily) Tj ET")));

        var result = await CreateExtractor().ExtractAsync(Request(pdf));

        Assert.Contains("BPC-157 500mcg daily", result.ExtractedText);
    }

    [Fact]
    public async Task ExtractAsync_FlateDecodeTjAndTjArrays_PreservesStreamOrder()
    {
        var pdf = BuildPdf(
            ("/Type /Page", null),
            ("<< /Length {0} /Filter /FlateDecode >>", Compress(
                "BT (Semaglutide 0.25mg weekly) Tj ET")),
            ("<< /Length {0} /Filter /FlateDecode >>", Compress(
                "BT [(TB-500) -20 (2mg twice weekly)] TJ ET")));

        var result = await CreateExtractor().ExtractAsync(Request(pdf));

        var lines = result.ExtractedText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .ToList();
        Assert.Equal(2, lines.Count);
        Assert.Contains("Semaglutide 0.25mg weekly", lines[0]);
        Assert.Contains("TB-500 2mg twice weekly", lines[1]);
    }

    [Fact]
    public async Task ExtractAsync_FlateDecodeImageStream_IsSkippedAndTextStreamStillExtracted()
    {
        // Binary pixel data that must never be mistaken for protocol text.
        var imageBytes = new byte[] { 0x00, 0xFF, 0x01, 0xFE, 0x80, 0x7F, 0x42, 0x99 };
        var pdf = BuildPdf(
            ("/Type /Page", null),
            ("<< /Length {0} /Filter /FlateDecode /Subtype /Image /Width 8 /Height 8 >>", Zip(imageBytes)),
            ("<< /Length {0} /Filter /FlateDecode >>", Compress(
                "BT (NAD+ 100mg 2x weekly) Tj ET")));

        var result = await CreateExtractor().ExtractAsync(Request(pdf));

        Assert.Equal("NAD+ 100mg 2x weekly", result.ExtractedText.Trim());
    }

    [Fact]
    public async Task ExtractAsync_UncompressedStreams_StillExtracted()
    {
        // The historic behavior must not regress for uncompressed content.
        var pdf = BuildPdf(
            ("/Type /Page", null),
            ("<< /Length {0} >>", "BT (GHK-Cu 2mg daily) Tj ET"));

        var result = await CreateExtractor().ExtractAsync(Request(pdf));

        Assert.Contains("GHK-Cu 2mg daily", result.ExtractedText);
    }

    [Fact]
    public async Task ExtractAsync_RawDeflateStreamWithoutZlibHeader_FallsBackAndExtracts()
    {
        var text = "BT (MOTS-C 5mg 3x weekly) Tj ET";
        using var raw = new MemoryStream();
        using (var deflate = new DeflateStream(raw, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            deflate.Write(Latin1.GetBytes(text));
        }

        var stream = Latin1.GetString(raw.ToArray());
        var pdf = $"%PDF-1.4\n1 0 obj << /Type /Page >>\n2 0 obj << /Length {stream.Length} /Filter /FlateDecode >>\nstream\n{stream}\nendstream\nendobj\n%%EOF";

        var result = await CreateExtractor().ExtractAsync(Request(Latin1.GetBytes(pdf)));

        Assert.Contains("MOTS-C 5mg 3x weekly", result.ExtractedText);
    }

    [Fact]
    public async Task ExtractAsync_CorruptFlateDecodeStream_DoesNotThrowAndSurfacesReadableTextError()
    {
        var garbage = Latin1.GetString(Enumerable.Range(0, 64).Select(i => (byte)((i * 37 + 11) % 251)).ToArray());
        var pdf = $"%PDF-1.4\n1 0 obj << /Type /Page >>\n2 0 obj << /Length {garbage.Length} /Filter /FlateDecode >>\nstream\n{garbage}\nendstream\nendobj\n%%EOF";

        // No text operators were recovered: the extraction path must fail with the
        // safe "no readable text" guidance instead of crashing or emitting garbage.
        var error = await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            CreateExtractor().ExtractAsync(Request(Latin1.GetBytes(pdf))));
        Assert.Contains("did not expose readable text", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IngestAsync_FlateDecodePdf_ExtractsNormalizedProtocolText()
    {
        var pdf = BuildPdf(
            ("/Type /Page", null),
            ("<< /Length {0} /Filter /FlateDecode >>", Compress(
                "BT (BPC-157 500mcg daily) Tj ET\nBT (TB-500 2mg twice weekly) Tj ET")));

        var ingestion = new ProtocolIngestionService(
            new IProtocolTextExtractor[] { new PdfProtocolExtractor() },
            new ProtocolNormalizationService(),
            new ProtocolFingerprintService(),
            new ProtocolAnalysisCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
                    new Microsoft.Extensions.Options.OptionsWrapper<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions>(
                        new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions())),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtocolAnalysisCache>.Instance),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtocolIngestionService>.Instance);

        var result = await ingestion.IngestAsync(new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload,
            null,
            null,
            "protocol.pdf",
            "application/pdf",
            pdf));

        Assert.Contains("BPC-157 500mcg daily", result.NormalizedText);
        Assert.Contains("TB-500 2mg twice weekly", result.NormalizedText);
    }

    private static PdfProtocolExtractor CreateExtractor() => new();

    private static ProtocolIngestionRequest Request(byte[] bytes) => new(
        ProtocolInputType.FileUpload,
        null,
        null,
        "synthetic-protocol.pdf",
        "application/pdf",
        bytes);

    private static string Compress(string content)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(Latin1.GetBytes(content));
        }

        return Latin1.GetString(output.ToArray());
    }

    private static string Zip(byte[] content)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(content);
        }

        return Latin1.GetString(output.ToArray());
    }

    // Builds a minimal PDF-like byte layout: an optional page object plus any
    // number of stream objects. Entries are (dictionary, streamData); a null
    // data value renders a plain (stream-less) object such as /Type /Page.
    private static byte[] BuildPdf(params (string Dictionary, string? Data)[] objects)
    {
        var builder = new StringBuilder("%PDF-1.4\n");
        var number = 1;
        foreach (var (dictionary, data) in objects)
        {
            if (data is null)
            {
                builder.Append($"{number} 0 obj {dictionary} endobj\n");
            }
            else
            {
                builder.Append($"{number} 0 obj {string.Format(dictionary, data.Length)}\nstream\n{data}\nendstream\nendobj\n");
            }

            number++;
        }

        builder.Append("%%EOF");
        return Latin1.GetBytes(builder.ToString());
    }
}
