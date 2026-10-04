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

// BIO-ANALYZER-003: an unrecognized single plain word plus a frequency word, with no dose,
// is prose and must not become a compound. Real parser/extractors only; no mocks.
public sealed class ProtocolSingleWordFrequencyGateTests
{
    // ---- T1 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("Review daily")]
    [InlineData("Hydrate weekly")]
    [InlineData("Stretch morning")]
    [InlineData("Meditate nightly")]
    [InlineData("Journal evening")]
    [InlineData("REVIEW DAILY")]
    public async Task T1_SingleWordFrequencyProse_EmitsNoEntries(string segment) =>
        await AssertNoEntriesAsync(segment);

    // ---- T2 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("- Review daily")]
    [InlineData("Review daily | Notes")]
    [InlineData("Review: daily")]
    [InlineData("Review daily.")]
    [InlineData("Stretch. Morning")]
    [InlineData("Hydrate/Stretch daily")]
    public async Task T2_ShapeVariants_EmitNoEntries(string segment) =>
        await AssertNoEntriesAsync(segment);

    // ---- T3 -------------------------------------------------------------------------------

    [Fact]
    public async Task T3_UnrecognizedNameWithDose_IsStillEmitted()
    {
        var parsed = await CreateParser().ParseAsync("Zorbatide 2mg weekly");

        var entry = Assert.Single(parsed.Entries);
        Assert.Equal("Zorbatide", entry.CompoundName);
        Assert.Equal(2d, entry.Dose);
        Assert.Equal("mg", entry.Unit);
        Assert.Equal("weekly", entry.Frequency);
    }

    // ---- T4 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("Retatrutide weekly", "Retatrutide")]
    [InlineData("retatrutide weekly", "Retatrutide")]
    [InlineData("BPC-157 daily", "BPC-157")]
    public async Task T4_KnownCompoundFrequencyOnly_IsStillEmitted(string segment, string expectedName)
    {
        var parsed = await CreateParser().ParseAsync(segment);

        var entry = Assert.Single(parsed.Entries);
        Assert.Equal(expectedName, entry.CompoundName);
    }

    // ---- T5 -------------------------------------------------------------------------------

    [Fact]
    public async Task T5_DigitOrHyphenUnknownToken_FrequencyOnly_IsStillEmitted()
    {
        var parsed = await CreateParser().ParseAsync("Zorb-12 weekly");

        var entry = Assert.Single(parsed.Entries);
        Assert.Equal("Zorb-12", entry.CompoundName);
    }

    // ---- T6 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("Blood Flow Peptide weekly")]
    [InlineData("Hydration Routine daily")]
    public async Task T6_MultiTokenNames_AreNotWidened(string segment)
    {
        var parsed = await CreateParser().ParseAsync(segment);

        Assert.True(
            parsed.Entries.Count == 1,
            Describe(segment, parsed));
    }

    // ---- T7 -------------------------------------------------------------------------------

    [Fact]
    public async Task T7_ProseDocx_KeepsOnlyRealCompound()
    {
        var docx = BuildDocx("""
            <w:p><w:r><w:t>Background Overview</w:t></w:r></w:p>
            <w:p><w:r><w:t>Review daily</w:t></w:r></w:p>
            <w:p><w:r><w:t>Hydrate weekly</w:t></w:r></w:p>
            <w:p><w:r><w:t>Discussion Points</w:t></w:r></w:p>
            <w:p><w:r><w:t>BPC-157 500mcg daily</w:t></w:r></w:p>
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

        var names = result.Protocol.Select(e => e.CompoundName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.True(
            names.SetEquals(new[] { "BPC-157" }),
            $"Expected exactly {{BPC-157}} but got: {string.Join(" | ", names)}");

        var issueCompounds = result.Issues.SelectMany(issue => issue.Compounds).ToList();
        Assert.True(
            !issueCompounds.Any(c =>
                c.Contains("Review", StringComparison.OrdinalIgnoreCase) ||
                c.Contains("Hydrate", StringComparison.OrdinalIgnoreCase)),
            $"Issues reference prose: {string.Join(" | ", issueCompounds)}");
    }

    // ---- helpers --------------------------------------------------------------------------

    private static async Task AssertNoEntriesAsync(string segment)
    {
        var parsed = await CreateParser().ParseAsync(segment);

        Assert.True(parsed.Entries.Count == 0, Describe(segment, parsed));
    }

    private static string Describe(string segment, ProtocolParseResult parsed) =>
        $"Segment '{segment}' emitted: {string.Join(" | ", parsed.Entries.Select(e => e.CompoundName))}";

    private static ProtocolParser CreateParser() =>
        new(new LocalKnowledgeSource(), new BlendDecomposerService(), new MemoryCache(new MemoryCacheOptions()));

    private static ProtocolAnalysisCache CreateCache() => new(
        new MemoryCache(new MemoryCacheOptions()),
        new MemoryDistributedCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions())),
        NullLogger<ProtocolAnalysisCache>.Instance);

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

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
