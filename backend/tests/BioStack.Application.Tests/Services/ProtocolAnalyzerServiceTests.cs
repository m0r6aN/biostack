namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class ProtocolAnalyzerServiceTests
{
    private readonly IProtocolAnalyzerService _service;

    public ProtocolAnalyzerServiceTests()
    {
        var knowledgeSource = new LocalKnowledgeSource();
        var parser = new ProtocolParser(knowledgeSource, new BlendDecomposerService(), new MemoryCache(new MemoryCacheOptions()));
        var interactionIntelligence = new InteractionIntelligenceService(
            knowledgeSource,
            MockInteractionHintRepository.Empty().Object);
        var memory = new MemoryCache(new MemoryCacheOptions());
        var distributed = new MemoryDistributedCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));
        var cache = new ProtocolAnalysisCache(memory, distributed, NullLogger<ProtocolAnalysisCache>.Instance);
        var suggestions = new ProtocolSuggestionService();
        var normalization = new ProtocolNormalizationService();
        var fingerprint = new ProtocolFingerprintService();
        var ingestion = new ProtocolIngestionService(
            new IProtocolTextExtractor[] { new PlainTextProtocolExtractor() },
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
            suggestions,
            new CounterfactualEngine(interactionIntelligence, new CounterfactualCandidateService(knowledgeSource), new CounterfactualExplainerService()),
            new NullProtocolAnalysisPersistenceHook(),
            NullLogger<ProtocolAnalyzerService>.Instance);
    }

    [Fact]
    public async Task AnalyzeAsync_ParsesSimpleInput()
    {
        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily"));

        Assert.Single(result.Protocol);
        Assert.Equal("BPC-157", result.Protocol[0].CompoundName);
        Assert.Equal(500, result.Protocol[0].Dose);
        Assert.Equal("mcg", result.Protocol[0].Unit);
        Assert.Equal("daily", result.Protocol[0].Frequency);
        Assert.Equal(50, result.ScoreExplanation.BaseScore);
        Assert.InRange(result.Score, 1, 100);
        Assert.Equal("Paste", result.InputType);
    }

    [Fact]
    public async Task AnalyzeAsync_FlagsOverlapScenario()
    {
        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest("Semaglutide weekly + Tirzepatide weekly"));

        Assert.NotEmpty(result.Protocol);
        Assert.Contains(result.Issues, issue => issue.Type is "inefficiency" or "overlap");
        Assert.NotEmpty(result.Suggestions);
    }

    [Fact]
    public async Task AnalyzeAsync_DecomposesKnownBlend()
    {
        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest("Triple Threat Blend (NAD+, MOTS-c, 5-Amino-1MQ) 10mg/1mg/1mg 3-4 days per week"));

        Assert.Contains(result.DecomposedBlends, blend => blend.BlendName == "Triple Threat Blend");
        Assert.Contains(result.Protocol, entry => entry.CompoundName == "NAD+");
        Assert.Contains(result.Protocol, entry => entry.CompoundName == "MOTS-C");
    }
}
