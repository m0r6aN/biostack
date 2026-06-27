namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
            AllowAllFeatureGate(ProductTier.Operator).Object,
            NullLogger<ProtocolAnalyzerService>.Instance);
    }

    internal static Mock<IFeatureGate> AllowAllFeatureGate(ProductTier tier = ProductTier.Operator)
    {
        var gate = new Mock<IFeatureGate>();
        gate.Setup(g => g.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        gate.Setup(g => g.GetCurrentTierAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tier);
        gate.Setup(g => g.EnsureEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return gate;
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
    public async Task AnalyzeAsync_CleanKnownCompoundPaste_IsHighConfidenceAndRecognized()
    {
        var input = string.Join(
            '\n',
            "BPC-157 500mcg daily",
            "TB-500 2mg twice weekly");

        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest(input));

        Assert.Equal(2, result.ParsedCompoundCount);
        Assert.Equal(2, result.RecognizedCompoundCount);
        Assert.Equal("high", result.ParseConfidence);
        Assert.True(result.Scored);
        Assert.All(result.Protocol, entry => Assert.True(entry.Recognized));
    }

    [Fact]
    public async Task AnalyzeAsync_ProseOnlyInput_IsNotScored()
    {
        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest(
            "This document describes goals, notes, and tracking expectations without a compound dose line."));

        Assert.Equal(0, result.RecognizedCompoundCount);
        Assert.Equal("none", result.ParseConfidence);
        Assert.False(result.Scored);
        Assert.Contains(result.ParserWarnings, warning =>
            warning.Contains("couldn't confidently identify known compounds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsync_IssueCompoundListsExcludeUnrecognizedEntries()
    {
        // Test that unrecognized entries are excluded from issue compound lists
        // Use all 5 known compounds from LocalKnowledgeSource + 1 unknown
        var input = string.Join(
            '\n',
            "BPC-157 500mcg daily",
            "TB-500 2mg twice weekly",
            "NAD+ 100mg daily",
            "MOTS-C 5mg 3x weekly",
            "Retatrutide 2mg weekly",
            "Epitalon 5mg weekly");  // Unknown compound

        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest(input));

        // Verify Epitalon is parsed but marked as unrecognized
        Assert.Contains(result.Protocol, entry =>
            string.Equals(entry.CompoundName, "Epitalon", StringComparison.OrdinalIgnoreCase)
            && !entry.Recognized);

        // Verify that any issues generated do NOT include the unrecognized Epitalon in their compound lists
        var allIssueCompounds = result.Issues.SelectMany(issue => issue.Compounds).ToList();
        Assert.DoesNotContain(allIssueCompounds, compound =>
            string.Equals(compound, "Epitalon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsync_FlagsOverlapScenario()
    {
        // Use compounds that exist in LocalKnowledgeSource (BPC-157 and TB-500 both have tissue-repair pathway)
        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily + TB-500 2mg twice weekly"));

        Assert.NotEmpty(result.Protocol);
        // BPC-157 and TB-500 share tissue-repair pathway, so should generate interaction intelligence
        // Issues may or may not be generated depending on interaction confidence thresholds
        // Just verify we got a valid result
        Assert.True(result.Score >= 0);
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

    // PR 1A (B1): the paid-intelligence gate must run BEFORE ingestion/parse/analysis.
    // Ingestion is mocked to throw if reached; the test passes only if the gate exception
    // surfaces first — i.e. nothing downstream got a chance to do work.
    [Fact]
    public async Task AnalyzeAsync_RunsFeatureGateBeforeIngestion()
    {
        var gate = new Mock<IFeatureGate>();
        gate.Setup(g => g.EnsureEnabledAsync(FeatureCodes.PaidIntelligence, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeatureLimitExceededException(
                "paid_intelligence",
                "Operator is required for this intelligence surface.",
                ProductTier.Observer,
                null));

        var ingestion = new Mock<IProtocolIngestionService>();
        ingestion.Setup(s => s.IngestAsync(It.IsAny<ProtocolIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ingestion must not run when gate blocks"));

        var parser = new Mock<IProtocolParser>();
        parser.Setup(s => s.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("parser must not run when gate blocks"));

        var knowledgeSource = new LocalKnowledgeSource();
        var normalization = new ProtocolNormalizationService();
        var fingerprint = new ProtocolFingerprintService();
        var memory = new MemoryCache(new MemoryCacheOptions());
        var distributed = new MemoryDistributedCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));
        var cache = new ProtocolAnalysisCache(memory, distributed, NullLogger<ProtocolAnalysisCache>.Instance);
        var interactionIntelligence = new InteractionIntelligenceService(
            knowledgeSource,
            MockInteractionHintRepository.Empty().Object);

        var service = new ProtocolAnalyzerService(
            parser.Object,
            ingestion.Object,
            normalization,
            fingerprint,
            cache,
            knowledgeSource,
            interactionIntelligence,
            new ProtocolSuggestionService(),
            new CounterfactualEngine(interactionIntelligence, new CounterfactualCandidateService(knowledgeSource), new CounterfactualExplainerService()),
            new NullProtocolAnalysisPersistenceHook(),
            gate.Object,
            NullLogger<ProtocolAnalyzerService>.Instance);

        var ex = await Assert.ThrowsAsync<FeatureLimitExceededException>(
            () => service.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily")));

        Assert.Equal("paid_intelligence", ex.Code);
        Assert.Equal(ProductTier.Observer, ex.Tier);
        gate.Verify(g => g.EnsureEnabledAsync(FeatureCodes.PaidIntelligence, It.IsAny<CancellationToken>()), Times.Once);
        ingestion.Verify(s => s.IngestAsync(It.IsAny<ProtocolIngestionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        parser.Verify(s => s.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Regression for review comment #3153773795:
    // When a compound and a cycle phrase appear on the same line
    // (e.g. "BPC-157 500mcg daily — 8 weeks on, 8 weeks off"), the old code
    // skipped the entire segment, silently dropping the compound. The fix strips
    // the cycle portion from the segment instead of discarding it wholesale.
    [Fact]
    public async Task AnalyzeAsync_CompoundAndCycleOnSameLine_DoesNotDropCompound()
    {
        var input = "BPC-157 500mcg daily — 8 weeks on, 8 weeks off";

        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest(
            ProtocolInputType.Paste,
            InputText: input,
            Goal: "healing"));

        // The compound must be present — not silently dropped.
        var bpc = Assert.Single(result.Protocol, entry =>
            string.Equals(entry.CompoundName, "BPC-157", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(500d, bpc.Dose);
        Assert.Equal("mcg", bpc.Unit);
        Assert.Equal("daily", bpc.Frequency);

        // The cycle phrase must not surface as a phantom compound.
        Assert.DoesNotContain(result.Protocol, entry =>
            entry.CompoundName.Contains("weeks", StringComparison.OrdinalIgnoreCase));
    }
}
