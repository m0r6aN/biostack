namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// Golden regression for the "Analyze Any Protocol" graceful-degradation failure.
//
// The fixture (BioStack_Protocol_Printable_Packet_v2.docx) is a real, multi-section
// provider-review packet: a title, subtitle, version note, several prose sections,
// phase tables, a total-materials table, support-stack table, baseline-labs tables,
// a safety/evidence section, and a reference list.
//
// Observed failure on this exact input (see docs/Analyze Any Protocol _ BioStack.pdf):
//   - "102 found, 4 normalized" / "98 compounds could not be fully normalized"
//   - Nearly every parsed row reads "Dose: Unknown, Frequency: Unknown"
//   - The "what this means" narrative read:
//       "...harder to evaluate cleanly because BioStack Protocol Packet,
//        Provider-review draft..., Version 2 adds Epitalon... create overlapping signals."
//     i.e. the document title / subtitle / version note were emitted as "compounds"
//     and sliced straight into user-facing copy.
//
// Root cause: ProtocolParser emits one entry per text segment with no gate that
// asks "is this actually a compound line?" — headers, prose, table-structure rows,
// notes, and citations all survive as fake compounds, poisoning entry count,
// unknown-compound count, issues, and the score narrative.
//
// These assertions encode the *correct* graceful behavior. They MUST FAIL against
// today's parser and pass once the recognition gate (remediation Step 1) lands.
public sealed class ProtocolAnalyzerDocxPacketGoldenTests
{
    // Tokens lifted verbatim from the packet's non-compound structure (titles,
    // section headers, prose, metadata, citations). None of these is a substring
    // of any real compound name in the document, so a correctly-gated parser will
    // never surface an entry or issue-compound containing one.
    private static readonly string[] NonCompoundMarkers =
    {
        "Protocol Packet",
        "Provider-review",
        "Provider-Review",
        "Version 2",
        "Quick-Reference",
        "Phase",
        "Decision Gates",
        "Total Materials",
        "Support Stack",
        "Baseline",
        "Blood Work",
        "Tracking",
        "Reference",
        "Evidence",
        "http",
        "Note",
        "Weeks",
        "Goal",
    };

    private readonly IProtocolAnalyzerService _service;

    public ProtocolAnalyzerDocxPacketGoldenTests()
    {
        var knowledgeSource = new LocalKnowledgeSource();
        var parser = new ProtocolParser(knowledgeSource, new BlendDecomposerService(), new MemoryCache(new MemoryCacheOptions()));
        var interactionIntelligence = new InteractionIntelligenceService(
            knowledgeSource,
            MockInteractionHintRepository.Empty().Object);
        var memory = new MemoryCache(new MemoryCacheOptions());
        var distributed = new MemoryDistributedCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));
        var cache = new ProtocolAnalysisCache(memory, distributed, NullLogger<ProtocolAnalysisCache>.Instance);
        var normalization = new ProtocolNormalizationService();
        var fingerprint = new ProtocolFingerprintService();
        var ingestion = new ProtocolIngestionService(
            new IProtocolTextExtractor[] { new PlainTextProtocolExtractor(), new DocxProtocolExtractor() },
            normalization,
            fingerprint,
            cache,
            NullLogger<ProtocolIngestionService>.Instance);
        _service = new ProtocolAnalyzerService(
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
            NullLogger<ProtocolAnalyzerService>.Instance);
    }

    private async Task<Contracts.Responses.AnalyzeProtocolResponse> AnalyzePacketAsync()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "BioStack_Protocol_Printable_Packet_v2.docx");
        Assert.True(File.Exists(fixturePath), $"Golden fixture missing: {fixturePath}");

        var bytes = await File.ReadAllBytesAsync(fixturePath);
        var request = new AnalyzeProtocolRequest(
            ProtocolInputType.FileUpload,
            SourceName: "BioStack_Protocol_Printable_Packet_v2.docx");
        var ingestionRequest = new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload,
            InputText: null,
            LinkUrl: null,
            SourceName: "BioStack_Protocol_Printable_Packet_v2.docx",
            ContentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            SourceBytes: bytes);

        return await _service.AnalyzeAsync(request, ingestionRequest);
    }

    // The headline failure: section headers, prose, and document metadata must
    // never be emitted as compounds.
    [Fact]
    public async Task AnalyzePacket_DoesNotEmitDocumentStructureAsCompounds()
    {
        var result = await AnalyzePacketAsync();

        var leaked = result.Protocol
            .Where(entry => NonCompoundMarkers.Any(marker =>
                entry.CompoundName.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => entry.CompoundName)
            .ToList();

        Assert.True(
            leaked.Count == 0,
            $"Non-compound document text leaked into parsed entries: {string.Join(" | ", leaked)}");
    }

    // 102 entries for a ~10-compound document is the noise explosion. A graceful
    // parse stays bounded.
    [Fact]
    public async Task AnalyzePacket_KeepsEntryCountBounded()
    {
        var result = await AnalyzePacketAsync();

        Assert.True(
            result.Protocol.Count <= 20,
            $"Expected a bounded compound list (<= 20) but parsed {result.Protocol.Count} entries.");
    }

    // "98 compounds could not be fully normalized" is the user-visible symptom of
    // the same noise. Under the test knowledge base, GHK-Cu/KPV/Epitalon/Dihexa/
    // Semax/Selank are legitimately unknown-but-real peptides, so allow headroom —
    // but 98 is the bug.
    [Fact]
    public async Task AnalyzePacket_DoesNotFloodUnknownCompounds()
    {
        var result = await AnalyzePacketAsync();

        Assert.True(
            result.UnknownCompounds.Count <= 10,
            $"Expected a sane unknown-compound count (<= 10) but got {result.UnknownCompounds.Count}: " +
            string.Join(" | ", result.UnknownCompounds));
    }

    // Guard against over-correction: the real compounds that ARE in the test
    // knowledge base and appear with doses in the packet must still be recognized.
    [Theory]
    [InlineData("NAD+")]
    [InlineData("MOTS-C")]
    [InlineData("BPC-157")]
    [InlineData("Retatrutide")]
    public async Task AnalyzePacket_StillRecognizesKnownCompounds(string canonicalName)
    {
        var result = await AnalyzePacketAsync();

        Assert.Contains(result.Protocol, entry =>
            string.Equals(entry.CompoundName, canonicalName, StringComparison.OrdinalIgnoreCase));
    }

    // The narrative-poisoning failure at its source: issue compound lists feed the
    // "what this means" copy. No issue may reference document structure as a compound.
    [Fact]
    public async Task AnalyzePacket_IssueCompoundsContainNoDocumentStructure()
    {
        var result = await AnalyzePacketAsync();

        var leaked = result.Issues
            .SelectMany(issue => issue.Compounds)
            .Where(compound => NonCompoundMarkers.Any(marker =>
                compound.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(
            leaked.Count == 0,
            $"Issue compound lists reference non-compound document text: {string.Join(" | ", leaked)}");
    }

    [Fact]
    public async Task AnalyzePacket_ReportsNonHighParseConfidenceButRemainsScored()
    {
        var result = await AnalyzePacketAsync();

        Assert.Contains(result.ParseConfidence, new[] { "low", "medium" });
        Assert.True(result.Scored);
        Assert.True(result.RecognizedCompoundCount > 0);
        Assert.True(result.ParsedCompoundCount >= result.RecognizedCompoundCount);
    }

    [Fact]
    public async Task AnalyzePacket_IssueCompoundsContainOnlyRecognizedNames()
    {
        var result = await AnalyzePacketAsync();
        var recognizedNames = result.Protocol
            .Where(entry => entry.Recognized)
            .Select(entry => entry.CompoundName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unrecognizedIssueCompounds = result.Issues
            .SelectMany(issue => issue.Compounds)
            .Where(compound => !recognizedNames.Contains(compound))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(
            unrecognizedIssueCompounds.Count == 0,
            $"Issue compound lists reference unrecognized names: {string.Join(" | ", unrecognizedIssueCompounds)}");
    }
}
