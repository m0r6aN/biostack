namespace BioStack.Application.Tests.Services;

using System.IO.Compression;
using System.Text;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// BIO-ANALYZER-001: uploaded-protocol graceful-failure guarantees.
//   1. Ordinary uploads fail gracefully (no wall of fake compounds, clean friendly errors).
//   2. Table/field labels ("Frequency", "Dose", ...) and prose never become compounds.
// Real parser/extractors only; no mocks.
public sealed class ProtocolUploadGracefulFailureTests
{
    private static readonly string[] HeaderLabels =
        ["Compound", "Dose", "Frequency", "Route", "Duration", "Timing", "Notes"];

    // Mirror of the closed C1 set (ProtocolParser.StructuralLabelWords).
    private static readonly string[] StructuralWords =
    [
        "Frequency", "Frequencies", "Compound", "Compounds", "Dose", "Doses", "Dosage", "Dosages", "Dosing",
        "Route", "Timing", "Time", "Duration", "Durations", "Administration", "Directions", "Schedule",
        "Schedules", "Protocol", "Goal", "Goals", "Note", "Notes", "Reference", "References", "Version",
        "Tracking", "Baseline", "Evidence", "Phase", "Support", "Stack", "Materials", "Blood", "Work", "Week",
        "Weeks", "Day", "Days", "Month", "Months", "Name", "Item", "Product", "Amount", "Unit", "Units", "Total",
        "Strength", "Quantity", "Concentration", "Injection", "Vial", "Regimen", "Cycle", "Medication",
        "Substance", "Drug", "Agent", "Peptide", "Supplement", "Starting", "Maintenance", "Target", "Max", "Min",
        "Current", "Per", "Mg", "Mcg", "Ml", "Iu",
    ];

    private static readonly string[] CompoundHeaderShapes =
    [
        "Dose/Frequency: daily",
        "Frequency (per week): daily",
        "Dose (mg): 5 mg",
        "Injection Frequency: daily",
        "Dosing Schedule: weekly",
        "FREQUENCY: daily",
    ];

    public static TheoryData<string> LabelSegments()
    {
        var data = new TheoryData<string>();
        foreach (var word in StructuralWords)
        {
            data.Add($"{word}: daily");
            data.Add($"{word}: 500mcg");
        }

        foreach (var shape in CompoundHeaderShapes)
        {
            data.Add(shape);
        }

        return data;
    }

    // ---- T1 -------------------------------------------------------------------------------

    [Fact]
    public async Task T1_CsvUpload_HeaderLabelsAreNotCompounds()
    {
        const string csv =
            "Compound,Dose,Frequency,Route,Duration,Timing,Notes\n" +
            "BPC-157,500mcg,daily,SubQ,4 weeks,morning,Rotate sites\n" +
            "Retatrutide,2mg,weekly,SubQ,12 weeks,evening,Titrate slowly\n";

        await AssertNoHeaderLeakAsync(
            "protocol.csv", "text/csv", Encoding.UTF8.GetBytes(csv));
    }

    // ---- T2 -------------------------------------------------------------------------------

    [Fact]
    public async Task T2_XlsxUpload_HeaderLabelsAreNotCompounds()
    {
        await AssertNoHeaderLeakAsync(
            "protocol.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CreateXlsx(
                ["Compound", "Dose", "Frequency", "Route", "Duration", "Timing", "Notes"],
                ["BPC-157", "500mcg", "daily", "SubQ", "4 weeks", "morning", "Rotate sites"],
                ["Retatrutide", "2mg", "weekly", "SubQ", "12 weeks", "evening", "Titrate slowly"]));
    }

    [Fact]
    public async Task T2_CsvUpload_AllCapsHeaderLabelsAreNotCompounds()
    {
        const string csv =
            "COMPOUND,DOSE,FREQUENCY,ROUTE,DURATION,TIMING,NOTES\n" +
            "BPC-157,500mcg,daily,SubQ,4 weeks,morning,Rotate sites\n" +
            "Retatrutide,2mg,weekly,SubQ,12 weeks,evening,Titrate slowly\n";

        await AssertNoHeaderLeakAsync(
            "protocol.csv", "text/csv", Encoding.UTF8.GetBytes(csv));
    }

    // ---- T3 -------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(LabelSegments))]
    public async Task T3_LabelSegment_EmitsNoEntries(string segment)
    {
        var parsed = await CreateParser().ParseAsync(segment);

        Assert.True(
            parsed.Entries.Count == 0,
            $"Segment '{segment}' leaked entries: {string.Join(" | ", parsed.Entries.Select(e => e.CompoundName))}");
    }

    // ---- T4 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("BPC-157 500mcg daily", "BPC-157")]
    [InlineData("Blood Flow Peptide 500mcg daily", "Blood Flow Peptide")]
    [InlineData("Peptide Alpha 2mg weekly", "Peptide Alpha")]
    [InlineData("Frequency Booster 2mg daily", "Frequency Booster")]
    public async Task T4_RealCompoundNames_AreStillEmitted(string line, string expectedName)
    {
        var parsed = await CreateParser().ParseAsync(line);

        var entry = Assert.Single(parsed.Entries);
        Assert.Equal(expectedName, entry.CompoundName, ignoreCase: true);
    }

    // ---- T5 -------------------------------------------------------------------------------

    [Fact]
    public async Task T5_ProseOnlyDocx_ProducesNoEntriesAndIsNotScored()
    {
        const string Citation = "Smith J et al. Journal of Peptide Research 2019. https://journal.example.org/1234";
        const string Narrative = "This document gives general background on tissue recovery research.";
        var docx = BuildDocx($"""
            <w:p><w:r><w:t>Background Overview</w:t></w:r></w:p>
            <w:p><w:r><w:t>{Narrative}</w:t></w:r></w:p>
            <w:p><w:r><w:t>Discussion Points</w:t></w:r></w:p>
            <w:p><w:r><w:t>Note: discuss any changes with your provider before starting.</w:t></w:r></w:p>
            <w:p><w:r><w:t>{Citation}</w:t></w:r></w:p>
            """);

        var service = CreateAnalyzer();
        var request = new AnalyzeProtocolRequest(ProtocolInputType.FileUpload, SourceName: "prose.docx");
        var ingestion = new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload,
            null,
            null,
            "prose.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            docx);

        var result = await service.AnalyzeAsync(request, ingestion);

        Assert.True(
            result.Protocol.Count == 0,
            $"Prose leaked entries: {string.Join(" | ", result.Protocol.Select(e => e.CompoundName))}");
        Assert.False(result.Scored);
        Assert.Equal("none", result.ParseConfidence);

        var fragments = new[] { "Background", "Overview", "Discussion", "Smith", "Journal", "journal.example.org", "tissue recovery", "provider" };
        var issueCompounds = result.Issues.SelectMany(issue => issue.Compounds).ToList();
        Assert.True(
            !issueCompounds.Any(c => fragments.Any(f => c.Contains(f, StringComparison.OrdinalIgnoreCase))),
            $"Issues reference prose: {string.Join(" | ", issueCompounds)}");
    }

    // ---- T6 -------------------------------------------------------------------------------

    [Fact]
    public async Task T6_RealProtocolPdf_KeepsOnlyRealCompounds()
    {
        var pdf = BuildTextPdf(
            "Healing Stack Summary",
            "This plan supports tissue recovery over the coming months.",
            @"Smith J. Journal of Peptide Research \(2019\) 12:34-40",
            "BPC-157 500mcg daily",
            "Retatrutide 2mg weekly");

        var service = CreateIngestionService(new PdfProtocolExtractor());
        var ingestion = await service.IngestAsync(new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload, null, null, "protocol.pdf", "application/pdf", pdf));

        // Proves the citation and narrative lines were actually read (exclusions are not vacuous).
        Assert.Contains("Journal of Peptide Research (2019)", ingestion.NormalizedText);
        Assert.Contains("This plan supports tissue recovery over the coming months.", ingestion.NormalizedText);

        var parsed = await CreateParser().ParseAsync(ingestion.NormalizedText);
        var names = parsed.Entries.Select(e => e.CompoundName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(
            names.SetEquals(new[] { "BPC-157", "Retatrutide" }),
            $"Expected exactly {{BPC-157, Retatrutide}} but got: {string.Join(" | ", names)}");
    }

    // ---- T7 -------------------------------------------------------------------------------

    [Fact]
    public async Task T7_NoTextPdf_ThrowsFriendlyIngestionException()
    {
        var service = CreateIngestionService(new PdfProtocolExtractor());

        var ex = await Assert.ThrowsAsync<ProtocolIngestionException>(() => service.IngestAsync(
            new ProtocolIngestionRequest(
                ProtocolInputType.FileUpload, null, null, "no-text.pdf", "application/pdf", BuildNoTextPdf())));

        Assert.Contains("readable text", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("BioStack.", ex.Message, StringComparison.Ordinal);
    }

    // ---- helpers --------------------------------------------------------------------------

    private static async Task AssertNoHeaderLeakAsync(string fileName, string contentType, byte[] bytes)
    {
        var service = CreateIngestionService(new SpreadsheetProtocolExtractor());
        var ingestion = await service.IngestAsync(new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload, null, null, fileName, contentType, bytes));

        var parsed = await CreateParser().ParseAsync(ingestion.NormalizedText);
        var names = parsed.Entries.Select(e => e.CompoundName).ToList();
        var listing = string.Join(" | ", names);

        Assert.True(names.Contains("BPC-157", StringComparer.OrdinalIgnoreCase), $"BPC-157 missing. Parsed: {listing}");
        Assert.True(names.Contains("Retatrutide", StringComparer.OrdinalIgnoreCase), $"Retatrutide missing. Parsed: {listing}");

        var leaked = names.Where(n => HeaderLabels.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
        Assert.True(
            leaked.Count == 0,
            $"Header labels leaked as compounds: {string.Join(" | ", leaked)}. All parsed: {listing}");
    }

    private static ProtocolParser CreateParser() =>
        new(new LocalKnowledgeSource(), new BlendDecomposerService(), new MemoryCache(new MemoryCacheOptions()));

    private static ProtocolAnalysisCache CreateCache() => new(
        new MemoryCache(new MemoryCacheOptions()),
        new MemoryDistributedCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions())),
        NullLogger<ProtocolAnalysisCache>.Instance);

    private static ProtocolIngestionService CreateIngestionService(params IProtocolTextExtractor[] extractors) => new(
        extractors,
        new ProtocolNormalizationService(),
        new ProtocolFingerprintService(),
        CreateCache(),
        NullLogger<ProtocolIngestionService>.Instance);

    private static IProtocolAnalyzerService CreateAnalyzer()
    {
        var knowledgeSource = new LocalKnowledgeSource();
        var parser = CreateParser();
        var interactionIntelligence = new InteractionIntelligenceService(
            knowledgeSource,
            MockInteractionHintRepository.Empty().Object);
        var cache = CreateCache();
        var normalization = new ProtocolNormalizationService();
        var fingerprint = new ProtocolFingerprintService();
        var ingestion = new ProtocolIngestionService(
            new IProtocolTextExtractor[] { new PlainTextProtocolExtractor(), new DocxProtocolExtractor() },
            normalization,
            fingerprint,
            cache,
            NullLogger<ProtocolIngestionService>.Instance);
        return new ProtocolAnalyzerService(
            parser,
            ingestion,
            normalization,
            fingerprint,
            cache,
            knowledgeSource,
            interactionIntelligence,
            new ProtocolSuggestionService(),
            new CounterfactualEngine(interactionIntelligence, new CounterfactualCandidateService(knowledgeSource), new CounterfactualExplainerService()),
            new NullProtocolAnalysisPersistenceHook(),
            ProtocolAnalyzerServiceTests.AllowAllFeatureGate(ProductTier.Operator).Object,
            new BioStack.Application.Evidence.ProtocolEvidenceContextComparer(
                new BioStack.Domain.Evidence.EvidenceContextComparisonService()),
            NullLogger<ProtocolAnalyzerService>.Instance);
    }

    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    // One uncompressed content stream; each line is its own BT ... Tj ET block.
    // Callers escape literal parentheses as \( and \).
    private static byte[] BuildTextPdf(params string[] lines)
    {
        var content = string.Join("\n", lines.Select(line => $"BT /F1 12 Tf 72 720 Td ({line}) Tj ET"));
        return Latin1.GetBytes(
            "%PDF-1.4\n" +
            "1 0 obj /Type /Page endobj\n" +
            $"2 0 obj << /Length {content.Length} >>\nstream\n{content}\nendstream\nendobj\n" +
            "%%EOF");
    }

    private static byte[] BuildNoTextPdf()
    {
        const string content = "q 100 0 0 100 0 0 cm 0 0 1 rg 0 0 50 50 re f Q";
        return Latin1.GetBytes(
            "%PDF-1.4\n" +
            "1 0 obj /Type /Page endobj\n" +
            $"2 0 obj << /Length {content.Length} >>\nstream\n{content}\nendstream\nendobj\n" +
            "%%EOF");
    }

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
            AddEntry(archive, "word/document.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" +
                body +
                "</w:body></w:document>");
        }

        return memory.ToArray();
    }

    // Single-sheet XLSX with inline strings (first row = header).
    private static byte[] CreateXlsx(params string[][] rows)
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

            var strings = new List<string>();
            var sheet = new StringBuilder();
            for (var r = 0; r < rows.Length; r++)
            {
                sheet.Append($"<row r=\"{r + 1}\">");
                for (var c = 0; c < rows[r].Length; c++)
                {
                    var index = strings.IndexOf(rows[r][c]);
                    if (index < 0)
                    {
                        strings.Add(rows[r][c]);
                        index = strings.Count - 1;
                    }

                    sheet.Append($"<c r=\"{(char)('A' + c)}{r + 1}\" t=\"s\"><v>{index}</v></c>");
                }

                sheet.Append("</row>");
            }

            AddEntry(archive, "xl/sharedStrings.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                string.Concat(strings.Select(s => $"<si><t>{s}</t></si>")) +
                "</sst>");
            AddEntry(archive, "xl/worksheets/sheet1.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                sheet +
                "</sheetData></worksheet>");
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
