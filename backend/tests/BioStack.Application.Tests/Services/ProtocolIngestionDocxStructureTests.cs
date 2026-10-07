namespace BioStack.Application.Tests.Services;

using System.IO.Compression;
using System.Text;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

// Preservation guard for the DOCX structure filtering behavior.
//
// A protocol DOCX mixes real dosing lines with document structure: heading
// paragraphs, a title, table header rows ("Week | Compound | Dose"), and
// reference lists. The ingestion contract has two halves that must hold
// together:
//   1. DocxProtocolExtractor keeps paragraphs intact and joins table cells of
//      a row with " | " so per-row structure survives extraction.
//   2. ProtocolParser's recognition gate then drops structural segments
//      (headers, prose, label cells) instead of emitting them as fake
//      "Unknown" compounds, while real compound lines survive.
//
// These tests pin both halves so the FlateDecode PDF remediation and any
// future extractor work cannot silently regress DOCX uploads.
public sealed class ProtocolIngestionDocxStructureTests
{
    private static readonly string[] StructuralMarkers =
    [
        "Protocol Overview",
        "Week",
        "Compound",
        "Dose",
        "References",
        "Provider notes",
    ];

    [Fact]
    public async Task ExtractAsync_DocxWithHeadingsAndTables_PreservesParagraphAndRowStructure()
    {
        var extractor = new DocxProtocolExtractor();

        var result = await extractor.ExtractAsync(Request(BuildDocx("""
            <w:p><w:r><w:t>Protocol Overview</w:t></w:r></w:p>
            <w:p><w:r><w:t>Take GHK-Cu 2mg daily for 4 weeks</w:t></w:r></w:p>
            <w:tbl>
              <w:tr>
                <w:tc><w:p><w:r><w:t>Week</w:t></w:r></w:p></w:tc>
                <w:tc><w:p><w:r><w:t>Compound</w:t></w:r></w:p></w:tc>
                <w:tc><w:p><w:r><w:t>Dose</w:t></w:r></w:p></w:tc>
              </w:tr>
              <w:tr>
                <w:tc><w:p><w:r><w:t>1-8</w:t></w:r></w:p></w:tc>
                <w:tc><w:p><w:r><w:t>BPC-157</w:t></w:r></w:p></w:tc>
                <w:tc><w:p><w:r><w:t>500mcg</w:t></w:r></w:p></w:tc>
              </w:tr>
            </w:tbl>
            <w:p><w:r><w:t>References: journal.example.org/1234</w:t></w:r></w:p>
            """)));

        var lines = result.ExtractedText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .ToList();

        // Paragraphs are preserved verbatim as their own blocks...
        Assert.Contains("Protocol Overview", lines);
        Assert.Contains("Take GHK-Cu 2mg daily for 4 weeks", lines);
        // ...and each table row is joined cell-by-cell with " | " in document order.
        Assert.Contains("Week | Compound | Dose", lines);
        Assert.Contains("1-8 | BPC-157 | 500mcg", lines);
        Assert.Contains("References: journal.example.org/1234", lines);
        Assert.Equal(5, lines.Count);
    }

    [Fact]
    public async Task ParseAsync_DocxExtractedText_KeepsCompoundLinesAndDropsStructure()
    {
        var parser = CreateParser();
        var extractor = new DocxProtocolExtractor();
        var extraction = await extractor.ExtractAsync(Request(BuildDocx("""
            <w:p><w:r><w:t>Protocol Overview</w:t></w:r></w:p>
            <w:p><w:r><w:t>Provider notes: titrate slowly.</w:t></w:r></w:p>
            <w:tbl>
              <w:tr>
                <w:tc><w:p><w:r><w:t>Week</w:t></w:r></w:p></w:tc>
                <w:tc><w:p><w:r><w:t>Compound</w:t></w:r></w:p></w:tc>
                <w:tc><w:p><w:r><w:t>Dose</w:t></w:r></w:p></w:tc>
              </w:tr>
              <w:tr>
                <w:tc><w:p><w:r><w:t>1-8</w:t></w:r></w:p></w:tc>
                <w:tc><w:p><w:r><w:t>BPC-157</w:t></w:r></w:p></w:tc>
                <w:tc><w:p><w:r><w:t>500mcg</w:t></w:r></w:p></w:tc>
              </w:tr>
            </w:tbl>
            <w:p><w:r><w:t>Take GHK-Cu 2mg daily for 4 weeks</w:t></w:r></w:p>
            <w:p><w:r><w:t>References: journal.example.org/1234</w:t></w:r></w:p>
            """)));

        var parsed = await parser.ParseAsync(extraction.ExtractedText);
        var names = parsed.Entries.Select(entry => entry.CompoundName).ToList();

        // Real dosing lines survive (paragraph line and table data row).
        Assert.Contains("BPC-157", names);
        Assert.Contains("GHK-Cu", names);

        // Structural segments must never be emitted as compounds.
        Assert.DoesNotContain(names, name =>
            StructuralMarkers.Any(marker =>
                string.Equals(name, marker, StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith(marker + " ", StringComparison.OrdinalIgnoreCase)));
        Assert.All(parsed.Entries, entry => Assert.False(
            string.Equals(entry.CompoundName, "Unknown", StringComparison.OrdinalIgnoreCase)));
    }

    private static ProtocolParser CreateParser()
    {
        var knowledgeSource = new LocalKnowledgeSource();
        return new ProtocolParser(
            knowledgeSource,
            new BlendDecomposerService(),
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static ProtocolIngestionRequest Request(byte[] bytes) => new(
        ProtocolInputType.FileUpload,
        null,
        null,
        "protocol.docx",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        bytes);

    private static byte[] BuildDocx(string body)
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
            AddEntry(archive, "word/document.xml", $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>
                    {{body}}
                  </w:body>
                </w:document>
                """);
        }

        return memory.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
