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

public sealed class SpreadsheetProtocolExtractorPackageTests
{
    private const string WorksheetType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";

    // Golden captured from the unmodified extractor on the relative-target workbook (normalised to \n).
    private const string GoldenExtractedText = "Sheet: Stack\nCompound: BPC-157 | Dose: 500mcg | Frequency: daily";

    [Fact]
    public async Task T1_DuplicateRelationshipId_IsFriendlyIngestionException()
    {
        var rels = Rels(Rel("rId1", "worksheets/sheet1.xml"), Rel("rId1", "worksheets/sheet1.xml"));

        var ex = await AssertIngestionFailure(BuildXlsx(rels));

        AssertFriendly(ex.Message);
    }

    [Fact]
    public async Task T2_RelationshipIdsDifferingByCase_IsFriendlyIngestionException()
    {
        var rels = Rels(Rel("rId1", "worksheets/sheet1.xml"), Rel("RID1", "worksheets/sheet1.xml"));

        var ex = await AssertIngestionFailure(BuildXlsx(rels));

        AssertFriendly(ex.Message);
    }

    [Fact]
    public async Task T3_RelativeTarget_ProducesGolden()
    {
        var result = await Ingest(BuildXlsx(Rels(Rel("rId1", "worksheets/sheet1.xml"))));

        Assert.Equal(GoldenExtractedText, Normalize(result.ExtractedText));
    }

    [Fact]
    public async Task T3_AbsoluteTarget_ProducesGolden()
    {
        var result = await Ingest(BuildXlsx(Rels(Rel("rId1", "/xl/worksheets/sheet1.xml"))));

        Assert.Equal(GoldenExtractedText, Normalize(result.ExtractedText));
    }

    [Theory]
    [InlineData("../xl/worksheets/sheet1.xml")]
    [InlineData("/xl/../xl/worksheets/sheet1.xml")]
    public async Task T4_DotDotTargets_ProduceGolden(string target)
    {
        var result = await Ingest(BuildXlsx(Rels(Rel("rId1", target))));

        Assert.Equal(GoldenExtractedText, Normalize(result.ExtractedText));
    }

    [Fact]
    public async Task T5_TargetEscapingPackageRoot_IsFriendlyAndDoesNotReadRootPart()
    {
        var evil = SheetXml("EVIL", "LEAKED");
        var bytes = BuildXlsx(Rels(Rel("rId1", "../../evil.xml")), extraEntries: [("evil.xml", evil)]);

        var ex = await AssertIngestionFailure(bytes);

        AssertFriendly(ex.Message);
        Assert.DoesNotContain("LEAKED", ex.Message);
    }

    [Fact]
    public async Task T6_MissingWorksheetPart_IsFriendlyIngestionException()
    {
        var ex = await AssertIngestionFailure(BuildXlsx(Rels(Rel("rId1", "worksheets/missing.xml"))));

        AssertFriendly(ex.Message);
    }

    [Fact]
    public async Task T7a_MissingWorkbookPart_IsFriendlyIngestionException()
    {
        var ex = await AssertIngestionFailure(BuildXlsx(Rels(Rel("rId1", "worksheets/sheet1.xml")), includeWorkbook: false));

        AssertFriendly(ex.Message);
    }

    [Fact]
    public async Task T7b_MissingWorkbookRels_IsFriendlyIngestionException()
    {
        var ex = await AssertIngestionFailure(BuildXlsx(Rels(Rel("rId1", "worksheets/sheet1.xml")), includeRels: false));

        AssertFriendly(ex.Message);
    }

    [Theory]
    [InlineData("<Relationship Id=\"rId9\" Type=\"x\" Target=\"../../x\"/>")]
    [InlineData("<Relationship Id=\"rId9\" Type=\"x\" Target=\"http://example.com/a\" TargetMode=\"External\"/>")]
    [InlineData("<Relationship Id=\"rId9\" Type=\"x\" Target=\"\"/>")]
    public async Task T8_UnreferencedOddRelationship_DoesNotFailUpload(string oddRelationship)
    {
        var rels = Rels(Rel("rId1", "worksheets/sheet1.xml"), oddRelationship);

        var result = await Ingest(BuildXlsx(rels));

        Assert.Equal(GoldenExtractedText, Normalize(result.ExtractedText));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    private static void AssertFriendly(string message)
    {
        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.DoesNotContain("Exception", message);
        Assert.DoesNotContain("   at ", message);
        Assert.DoesNotContain("BioStack.", message);
        Assert.DoesNotContain("xl/", message);
        Assert.DoesNotContain(".xml", message);
        Assert.DoesNotContain(".rels", message);
    }

    private static async Task<ProtocolIngestionException> AssertIngestionFailure(byte[] bytes)
    {
        var thrown = await Record.ExceptionAsync(() => Ingest(bytes));
        Assert.NotNull(thrown);
        Assert.True(
            thrown!.GetType() == typeof(ProtocolIngestionException),
            $"Expected ProtocolIngestionException but observed {thrown.GetType().FullName}: {thrown.Message}");
        return (ProtocolIngestionException)thrown;
    }

    private static Task<ProtocolIngestionResult> Ingest(byte[] bytes)
    {
        var service = CreateService(new SpreadsheetProtocolExtractor());
        var request = new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload,
            null,
            null,
            "protocol.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            bytes);
        return service.IngestAsync(request);
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

    private static string Rel(string id, string target) =>
        $"<Relationship Id=\"{id}\" Type=\"{WorksheetType}\" Target=\"{target}\"/>";

    private static string Rels(params string[] relationships) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        string.Concat(relationships) +
        "</Relationships>";

    private static string SheetXml(string header, string value) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <sheetData>
            <row r="1"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c><c r="C1" t="s"><v>2</v></c></row>
            <row r="2"><c r="A2" t="s"><v>3</v></c><c r="B2" t="s"><v>4</v></c><c r="C2" t="s"><v>5</v></c></row>
          </sheetData>
        </worksheet>
        """;

    private static byte[] BuildXlsx(
        string workbookRels,
        bool includeWorkbook = true,
        bool includeRels = true,
        (string Path, string Content)[]? extraEntries = null)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                </Types>
                """);
            AddEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            if (includeWorkbook)
            {
                AddEntry(archive, "xl/workbook.xml", """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                      <sheets>
                        <sheet name="Stack" sheetId="1" r:id="rId1"/>
                      </sheets>
                    </workbook>
                    """);
            }

            if (includeRels)
            {
                AddEntry(archive, "xl/_rels/workbook.xml.rels", workbookRels);
            }

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
            AddEntry(archive, "xl/worksheets/sheet1.xml", SheetXml("Compound", "BPC-157"));

            foreach (var (path, content) in extraEntries ?? [])
            {
                AddEntry(archive, path, content);
            }
        }

        return memory.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
