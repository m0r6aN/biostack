namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public sealed class ProtocolAnalyzerCachingTests
{
    [Fact]
    public async Task ParseCacheHit_AvoidsParserWork()
    {
        var parser = CreateParserMock();
        var analyzer = CreateAnalyzer(parser: parser.Object);

        await analyzer.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily"));
        await analyzer.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily"));

        parser.Verify(service => service.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalysisCacheHit_AvoidsRepeatedScoring()
    {
        var interaction = CreateInteractionMock();
        var analyzer = CreateAnalyzer(interaction: interaction.Object);

        await analyzer.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily"));
        await analyzer.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily"));

        interaction.Verify(service => service.EvaluateAsync(It.IsAny<IReadOnlyList<KnowledgeEntry>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CounterfactualCacheHit_AvoidsRepeatedOptimization()
    {
        var engine = CreateEngineMock();
        var analyzer = CreateAnalyzer(engine: engine.Object);

        await analyzer.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily"));
        await analyzer.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily"));

        engine.Verify(service => service.OptimizeAsync(It.IsAny<List<ProtocolEntryResponse>>(), It.IsAny<IReadOnlyList<KnowledgeEntry>>(), It.IsAny<OptimizationContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IProtocolAnalyzerService CreateAnalyzer(
        IProtocolParser? parser = null,
        IInteractionIntelligenceService? interaction = null,
        ICounterfactualEngine? engine = null,
        IKnowledgeSource? knowledgeSource = null)
    {
        var knowledge = knowledgeSource ?? CreateKnowledgeSourceMock().Object;
        var normalization = new ProtocolNormalizationService();
        var fingerprint = new ProtocolFingerprintService();
        var cache = new ProtocolAnalysisCache(
            new MemoryCache(new MemoryCacheOptions()),
            new MemoryDistributedCache(new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions())),
            NullLogger<ProtocolAnalysisCache>.Instance);
        var ingestion = new ProtocolIngestionService(
            new IProtocolTextExtractor[] { new PlainTextProtocolExtractor() },
            normalization,
            fingerprint,
            cache,
            NullLogger<ProtocolIngestionService>.Instance);

        return new ProtocolAnalyzerService(
            parser ?? CreateParserMock().Object,
            ingestion,
            normalization,
            fingerprint,
            cache,
            knowledge,
            interaction ?? CreateInteractionMock().Object,
            new ProtocolSuggestionService(),
            engine ?? CreateEngineMock().Object,
            new NullProtocolAnalysisPersistenceHook(),
            NullLogger<ProtocolAnalyzerService>.Instance);
    }

    private static Mock<IProtocolParser> CreateParserMock()
    {
        var parser = new Mock<IProtocolParser>();
        parser
            .Setup(service => service.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProtocolParseResult(
                new List<ProtocolEntryResponse> { new("BPC-157", 500, "mcg", "daily", string.Empty) },
                new Dictionary<string, KnowledgeEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    ["BPC-157"] = new KnowledgeEntry { CanonicalName = "BPC-157", Benefits = new List<string>(), Pathways = new List<string>() }
                },
                new List<ProtocolBlendExpansionResponse>()));
        return parser;
    }

    private static Mock<IKnowledgeSource> CreateKnowledgeSourceMock()
    {
        var knowledge = new Mock<IKnowledgeSource>();
        knowledge
            .Setup(service => service.GetCompoundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => new KnowledgeEntry { CanonicalName = name, Benefits = new List<string>(), Pathways = new List<string>() });
        knowledge
            .Setup(service => service.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntry>());
        return knowledge;
    }

    private static Mock<IInteractionIntelligenceService> CreateInteractionMock()
    {
        var interaction = new Mock<IInteractionIntelligenceService>();
        interaction
            .Setup(service => service.EvaluateAsync(It.IsAny<IReadOnlyList<KnowledgeEntry>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InteractionIntelligenceResponse(
                new InteractionSummaryResponse(1, 0, 0),
                new ProtocolInteractionScoreResponse(0.59, 0, 0),
                60.62,
                new List<InteractionFindingResponse>(),
                new List<InteractionResultResponse>(),
                new List<InteractionCounterfactualResponse>(),
                new List<InteractionSwapRecommendationResponse>()));
        return interaction;
    }

    private static Mock<ICounterfactualEngine> CreateEngineMock()
    {
        var engine = new Mock<ICounterfactualEngine>();
        engine
            .Setup(service => service.OptimizeAsync(It.IsAny<List<ProtocolEntryResponse>>(), It.IsAny<IReadOnlyList<KnowledgeEntry>>(), It.IsAny<OptimizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CounterfactualResultDto(60, new List<InteractionCounterfactualResponse>(), new List<InteractionSwapRecommendationResponse>(), null, new List<GoalAwareOptimizationResponse>()));
        return engine;
    }
}
