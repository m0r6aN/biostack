namespace BioStack.Application.Tests.Services;

using System.IO.Compression;
using System.Text;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class ProtocolIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_Paste_NormalizesTextAndCachesResult()
    {
        var service = CreateService(new PlainTextProtocolExtractor());
        var request = new ProtocolIngestionRequest(ProtocolInputType.Paste, "BPC-157\t500μg daily", null, "paste", "text/plain", null);

        var first = await service.IngestAsync(request);
        var second = await service.IngestAsync(request);

        Assert.Equal("BPC-157 500mcg daily", first.NormalizedText);
        Assert.Equal(first.ParseFingerprint, second.ParseFingerprint);
    }

    [Fact]
    public async Task IngestAsync_CsvUpload_PreservesRowStructure()
    {
        var service = CreateService(new SpreadsheetProtocolExtractor());
        var request = new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload,
            null,
            null,
            "protocol.csv",
            "text/csv",
            Encoding.UTF8.GetBytes("Compound,Dose,Frequency\nBPC-157,500mcg,daily"));

        var result = await service.IngestAsync(request);

        Assert.Contains("Compound: BPC-157", result.NormalizedText);
        Assert.Contains("Dose: 500mcg", result.NormalizedText);
    }

    [Fact]
    public async Task IngestAsync_DocxUpload_ReadsParagraphsAndTables()
    {
        var service = CreateService(new DocxProtocolExtractor());
        var request = new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload,
            null,
            null,
            "protocol.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            CreateMinimalDocx());

        var result = await service.IngestAsync(request);

        Assert.Contains("Healing protocol", result.NormalizedText);
        Assert.Contains("BPC-157 | 500mcg | daily", result.NormalizedText);
    }

    [Fact]
    public async Task IngestAsync_XlsxUpload_ReadsWorksheetRows()
    {
        var service = CreateService(new SpreadsheetProtocolExtractor());
        var request = new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload,
            null,
            null,
            "protocol.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CreateMinimalXlsx());

        var result = await service.IngestAsync(request);

        Assert.Contains("Sheet: Stack", result.NormalizedText);
        Assert.Contains("Compound: BPC-157", result.NormalizedText);
    }

    [Fact]
    public async Task IngestAsync_ImageUsesOcrWarnings()
    {
        var service = CreateService(new ImageOcrProtocolExtractor(new FakeOcrService()));
        var request = new ProtocolIngestionRequest(
            ProtocolInputType.CameraScan,
            null,
            null,
            "scan.jpg",
            "image/jpeg",
            [1, 2, 3]);

        var result = await service.IngestAsync(request);

        Assert.True(result.LowConfidence);
        Assert.Contains("low contrast", result.Warnings[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BPC-157", result.NormalizedText);
    }

    private static ProtocolIngestionService CreateService(params IProtocolTextExtractor[] extractors)
    {
        var cache = new ProtocolAnalysisCache(
            new MemoryCache(new MemoryCacheOptions()),
            new MemoryDistributedCache(new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions())),
            NullLogger<ProtocolAnalysisCache>.Instance);

        return new ProtocolIngestionService(
            extractors,
            new ProtocolNormalizationService(),
            new ProtocolFingerprintService(),
            cache,
            NullLogger<ProtocolIngestionService>.Instance);
    }

    private static byte[] CreateMinimalDocx()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                </Types>
                """);
            AddEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
                </Relationships>
                """);
            AddEntry(archive, "word/document.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>
                    <w:p><w:r><w:t>Healing protocol</w:t></w:r></w:p>
                    <w:tbl>
                      <w:tr>
                        <w:tc><w:p><w:r><w:t>BPC-157</w:t></w:r></w:p></w:tc>
                        <w:tc><w:p><w:r><w:t>500mcg</w:t></w:r></w:p></w:tc>
                        <w:tc><w:p><w:r><w:t>daily</w:t></w:r></w:p></w:tc>
                      </w:tr>
                    </w:tbl>
                  </w:body>
                </w:document>
                """);
        }

        return memory.ToArray();
    }

    private static byte[] CreateMinimalXlsx()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/>
                </Types>
                """);
            AddEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            AddEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Stack" sheetId="1" r:id="rId1"/>
                  </sheets>
                </workbook>
                """);
            AddEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            AddEntry(archive, "xl/sharedStrings.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <si><t>Compound</t></si>
                  <si><t>Dose</t></si>
                  <si><t>Frequency</t></si>
                  <si><t>BPC-157</t></si>
                  <si><t>500mcg</t></si>
                  <si><t>daily</t></si>
                </sst>
                """);
            AddEntry(archive, "xl/worksheets/sheet1.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData>
                    <row r="1">
                      <c r="A1" t="s"><v>0</v></c>
                      <c r="B1" t="s"><v>1</v></c>
                      <c r="C1" t="s"><v>2</v></c>
                    </row>
                    <row r="2">
                      <c r="A2" t="s"><v>3</v></c>
                      <c r="B2" t="s"><v>4</v></c>
                      <c r="C2" t="s"><v>5</v></c>
                    </row>
                  </sheetData>
                </worksheet>
                """);
        }

        return memory.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private sealed class FakeOcrService : IProtocolOcrService
    {
        public Task<ProtocolOcrResult> ExtractAsync(byte[] imageBytes, string? sourceName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProtocolOcrResult(
                "BPC-157 500mcg daily",
                ["Image had low contrast."],
                true));
        }
    }
}
