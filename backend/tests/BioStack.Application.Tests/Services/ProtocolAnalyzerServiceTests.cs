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

    // Regression for the GLOW Blend healing-stack trust failure:
    //   - "8 weeks on, 8 weeks off" must never be parsed as a compound.
    //   - BPC-157 500mcg daily must keep its dose and frequency even when the
    //     blend header line emits a same-named entry with no dose.
    //   - TB-500 2mg twice weekly must keep its dose and frequency for the
    //     same reason.
    //   - The optimizer must not surface a "remove BPC-157" recommendation
    //     when the variant score does not meaningfully improve.
    [Fact]
    public async Task AnalyzeAsync_GlowBlendHealingStack_DoesNotEmitCycleAsCompoundAndPreservesDoses()
    {
        var input = string.Join(
            '\n',
            "GLOW Blend (GHK-cu, BPC-157, TB-500)",
            "BPC-157 500mcg daily",
            "TB-500 2mg twice weekly",
            "8 weeks on, 8 weeks off");

        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest(
            ProtocolInputType.Paste,
            InputText: input,
            Goal: "healing"));

        Assert.DoesNotContain(result.Protocol, entry =>
            entry.CompoundName.Contains("weeks", StringComparison.OrdinalIgnoreCase));

        var bpc = Assert.Single(result.Protocol, entry =>
            string.Equals(entry.CompoundName, "BPC-157", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(500d, bpc.Dose);
        Assert.Equal("mcg", bpc.Unit);
        Assert.Equal("daily", bpc.Frequency);

        var tb500 = Assert.Single(result.Protocol, entry =>
            string.Equals(entry.CompoundName, "TB-500", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2d, tb500.Dose);
        Assert.Equal("mg", tb500.Unit);
        Assert.Equal("twice weekly", tb500.Frequency);

        Assert.Contains(result.DecomposedBlends, blend =>
            string.Equals(blend.BlendName, "GLOW Blend", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(result.Counterfactuals.BestRemoveOne, candidate =>
            string.Equals(candidate.RemovedCompound, "BPC-157", StringComparison.OrdinalIgnoreCase)
            && candidate.DeltaScore < 3d);
    }

    // The healing-domain Complementary classification (BPC-157, TB-500, GHK-Cu
    // converging on tissue-repair / angiogenesis via distinct mechanisms) must
    // reach the analyzer response surface. The public response does not expose
    // the raw InteractionResult list, so we assert via two faithful proxies:
    //   1. No `redundancy` issue is emitted for the BPC-157 + TB-500 pair —
    //      the pair must no longer be misread as Redundant.
    //   2. No `inefficiency` issue is emitted — Complementary pairs count
    //      toward Synergies > 0, so the "lacks complementary signal" gate
    //      must not fire on a healing stack of this shape.
    [Fact]
    public async Task AnalyzeAsync_GlowBlendHealingStack_ClassifiesBpcAndTb500AsComplementary()
    {
        var input = string.Join(
            '\n',
            "GLOW Blend (GHK-cu, BPC-157, TB-500)",
            "BPC-157 500mcg daily",
            "TB-500 2mg twice weekly",
            "8 weeks on, 8 weeks off");

        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest(
            ProtocolInputType.Paste,
            InputText: input,
            Goal: "healing"));

        Assert.DoesNotContain(result.Issues, issue =>
            string.Equals(issue.Type, "redundancy", StringComparison.OrdinalIgnoreCase)
            && issue.Compounds.Any(c => string.Equals(c, "BPC-157", StringComparison.OrdinalIgnoreCase))
            && issue.Compounds.Any(c => string.Equals(c, "TB-500", StringComparison.OrdinalIgnoreCase)));

        Assert.DoesNotContain(result.Issues, issue =>
            string.Equals(issue.Type, "inefficiency", StringComparison.OrdinalIgnoreCase));

        Assert.True(result.ScoreExplanation.Synergy > 0,
            "Complementary healing-domain pairs must register a positive synergy score on the response.");
    }
}
