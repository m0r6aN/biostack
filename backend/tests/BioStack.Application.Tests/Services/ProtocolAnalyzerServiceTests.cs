namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Cognition;
using BioStack.Cognition.Models;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using Keon.Collective;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public sealed class ProtocolAnalyzerServiceTests
{
    private readonly IProtocolAnalyzerService _service;
    private readonly Mock<IStackReviewBoardService> _stackReviewBoard = new();

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
        _stackReviewBoard
            .Setup(service => service.ReviewStackAsync(It.IsAny<StackDeliberationEnvelope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReviewPayload());
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
            _stackReviewBoard.Object,
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
        Assert.Empty(result.KnownPatterns);
        Assert.Empty(result.EmergentPatterns);
        Assert.NotNull(result.StackReviewBoard);
    }

    [Fact]
    public async Task AnalyzeAsync_MapsStackReviewBoardResponse()
    {
        var result = await _service.AnalyzeAsync(new AnalyzeProtocolRequest("BPC-157 500mcg daily"));

        var review = Assert.IsType<StackReviewBoardResponse>(result.StackReviewBoard);
        Assert.Equal("Optimizer", review.BranchPerspectiveReview["Optimizer"].Kind);
        Assert.Equal("Counter-plan narrative", review.ContradictionReview.CounterPlanNarrative);
        Assert.Equal("keon.collective-stub-v1", review.ConfidenceProfile.Model);
        Assert.Equal("rg::test", review.ReasoningGraphRef.GraphId);
    }

    [Fact]
    public async Task AnalyzeAsync_ProjectsHintBackedInteractionsIntoKnownPatternsAndSrbEnvelope()
    {
        var hintRepository = MockInteractionHintRepository.WithHints(new CompoundInteractionHint
        {
            CompoundA = "BPC-157",
            CompoundB = "TB-500",
            InteractionType = InteractionType.Complementary,
            Strength = 0.85m,
            Notes = "Known repair-stack pairing with overlapping recovery intent."
        });

        var knowledgeSource = new LocalKnowledgeSource();
        var parser = new ProtocolParser(knowledgeSource, new BlendDecomposerService(), new MemoryCache(new MemoryCacheOptions()));
        var interactionIntelligence = new InteractionIntelligenceService(knowledgeSource, hintRepository.Object);
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

        StackDeliberationEnvelope? capturedEnvelope = null;
        var stackReviewBoard = new Mock<IStackReviewBoardService>();
        stackReviewBoard
            .Setup(service => service.ReviewStackAsync(It.IsAny<StackDeliberationEnvelope>(), It.IsAny<CancellationToken>()))
            .Callback<StackDeliberationEnvelope, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .ReturnsAsync(CreateReviewPayload());

        var service = new ProtocolAnalyzerService(
            parser,
            ingestion,
            normalization,
            fingerprint,
            cache,
            knowledgeSource,
            interactionIntelligence,
            suggestions,
            new CounterfactualEngine(interactionIntelligence, new CounterfactualCandidateService(knowledgeSource), new CounterfactualExplainerService()),
            stackReviewBoard.Object,
            new NullProtocolAnalysisPersistenceHook(),
            NullLogger<ProtocolAnalyzerService>.Instance);

        var result = await service.AnalyzeAsync(new AnalyzeProtocolRequest(
            ProtocolInputType.Paste,
            InputText: "BPC-157 500mcg daily; TB-500 2mg twice weekly",
            Goal: "healing"));

        var pattern = Assert.Single(result.KnownPatterns);
        Assert.Equal("bpc-157-tb-500-complementary", pattern.PatternId);
        Assert.Equal("BPC-157 + TB-500 Complementary Pairing", pattern.Name);
        Assert.Equal(["bpc-157", "tb-500"], pattern.MatchedCompoundSlugs);
        Assert.Equal("Known repair-stack pairing with overlapping recovery intent.", pattern.Description);

        var envelope = Assert.IsType<StackDeliberationEnvelope>(capturedEnvelope);
        var envelopePattern = Assert.Single(envelope.KnownPatterns);
        Assert.Equal(pattern.PatternId, envelopePattern.PatternId);
        Assert.Equal(pattern.MatchedCompoundSlugs, envelopePattern.MatchedCompoundSlugs);
    }

    [Fact]
    public async Task AnalyzeAsync_KeepsKnownPatternsSeparateFromEmergentPatterns_AndPassesBothIntoSrbEnvelope()
    {
        var entries = new List<KnowledgeEntry>
        {
            CreateKnowledgeEntry("BPC-157", CompoundCategory.Peptide, BioStack.Domain.Enums.EvidenceTier.Limited, "repair and angiogenesis signaling", ["tissue-repair", "angiogenesis"], ["tissue recovery"]),
            CreateKnowledgeEntry("TB-500", CompoundCategory.Peptide, BioStack.Domain.Enums.EvidenceTier.Limited, "repair peptide with angiogenesis support", ["tissue-repair", "angiogenesis"], ["recovery support"]),
            CreateKnowledgeEntry("GHK-Cu", CompoundCategory.Peptide, BioStack.Domain.Enums.EvidenceTier.Moderate, "collagen and wound-healing support", ["tissue-repair", "wound-healing"], ["skin recovery"]),
            CreateKnowledgeEntry("Semaglutide", CompoundCategory.Pharmaceutical, BioStack.Domain.Enums.EvidenceTier.Strong, "glp-1 incretin signaling", ["glp-1", "metabolic"], ["glucose control"]),
            CreateKnowledgeEntry("Tirzepatide", CompoundCategory.Pharmaceutical, BioStack.Domain.Enums.EvidenceTier.Strong, "glp-1 and gip incretin signaling", ["glp-1", "gip", "metabolic"], ["glucose control"]),
        };

        var parseResult = CreateParseResult(entries);
        var interactionResponse = new InteractionIntelligenceResponse(
            new InteractionSummaryResponse(2, 0, 0),
            new ProtocolInteractionScoreResponse(1.62, 0, 0),
            73.4,
            new List<InteractionFindingResponse>(),
            new List<InteractionResultResponse>
            {
                new("BPC-157", "TB-500", InteractionType.Complementary, 0.85d, new List<string> { "tissue-repair", "angiogenesis" }, "Known repair-stack pairing with overlapping recovery intent.", true),
                new("Semaglutide", "Tirzepatide", InteractionType.Redundant, 0.82d, new List<string> { "glp-1", "metabolic" }, "Known incretin overlap worth tracking.", true),
                new("BPC-157", "GHK-Cu", InteractionType.Complementary, 0.76d, new List<string> { "tissue-repair" }, "Distinct mechanisms converging on tissue-repair.", false),
                new("TB-500", "GHK-Cu", InteractionType.Complementary, 0.74d, new List<string> { "tissue-repair" }, "Distinct mechanisms converging on tissue-repair.", false),
            },
            new List<InteractionCounterfactualResponse>(),
            new List<InteractionSwapRecommendationResponse>());

        StackDeliberationEnvelope? capturedEnvelope = null;
        var service = CreateStubbedAnalyzer(parseResult, interactionResponse, envelope => capturedEnvelope = envelope);

        var result = await service.AnalyzeAsync(new AnalyzeProtocolRequest(
            ProtocolInputType.Paste,
            InputText: "stubbed",
            Goal: "healing"));

        Assert.Contains(result.KnownPatterns, pattern => pattern.PatternId == "bpc-157-tb-500-complementary");
        Assert.Contains(result.KnownPatterns, pattern => pattern.PatternId == "semaglutide-tirzepatide-redundant");

        var sharedMotif = Assert.Single(result.EmergentPatterns, pattern => pattern.Id == "emergent-shared-pathway-tissue-repair");
        Assert.Equal("motif", sharedMotif.PatternType);
        Assert.Equal("pathway-overlap", sharedMotif.Basis);
        Assert.Equal(["bpc-157", "ghk-cu", "tb-500"], sharedMotif.Compounds.OrderBy(item => item).ToList());

        Assert.DoesNotContain(result.EmergentPatterns, pattern => result.KnownPatterns.Any(known => known.PatternId == pattern.Id));

        var envelope = Assert.IsType<StackDeliberationEnvelope>(capturedEnvelope);
        Assert.Equal(2, envelope.KnownPatterns.Count);
        Assert.Contains(envelope.KnownPatterns, pattern => pattern.PatternId == "bpc-157-tb-500-complementary");
        Assert.Contains(envelope.EmergentPatterns, pattern => pattern.Id == "emergent-shared-pathway-tissue-repair");
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotFabricateEmergentPatterns_WhenNoQualifyingMotifExists()
    {
        var entries = new List<KnowledgeEntry>
        {
            CreateKnowledgeEntry("Alpha", CompoundCategory.Supplement, BioStack.Domain.Enums.EvidenceTier.Strong, "single-pathway antioxidant support", ["oxidative-balance"], ["antioxidant support"]),
            CreateKnowledgeEntry("Beta", CompoundCategory.Supplement, BioStack.Domain.Enums.EvidenceTier.Strong, "unrelated neurotransmitter support", ["neurotransmitter-balance"], ["focus support"]),
        };

        var parseResult = CreateParseResult(entries);
        var interactionResponse = new InteractionIntelligenceResponse(
            new InteractionSummaryResponse(0, 0, 0),
            new ProtocolInteractionScoreResponse(0, 0, 0),
            50,
            new List<InteractionFindingResponse>(),
            new List<InteractionResultResponse>
            {
                new("Alpha", "Beta", InteractionType.Neutral, 0.21d, new List<string>(), "No strong interaction signal.", false),
            },
            new List<InteractionCounterfactualResponse>(),
            new List<InteractionSwapRecommendationResponse>());

        var service = CreateStubbedAnalyzer(parseResult, interactionResponse);

        var result = await service.AnalyzeAsync(new AnalyzeProtocolRequest(
            ProtocolInputType.Paste,
            InputText: "stubbed",
            Goal: "general wellness"));

        Assert.Empty(result.KnownPatterns);
        Assert.Empty(result.EmergentPatterns);
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

    private static CognitiveDensityEnvelope CreateReviewPayload()
    {
        return new CognitiveDensityEnvelope(
            new BranchPerspectiveReview(new Dictionary<PerspectiveKind, PerspectiveReview>
            {
                [PerspectiveKind.Optimizer] = new(PerspectiveKind.Optimizer, [new PerspectiveFinding("opt-1", "alignment", "Optimizer review", FindingSeverity.Info)], "Optimizer summary"),
                [PerspectiveKind.Skeptic] = new(PerspectiveKind.Skeptic, [new PerspectiveFinding("skp-1", "evidence", "Skeptic review", FindingSeverity.Warning)], "Skeptic summary"),
                [PerspectiveKind.Regulator] = new(PerspectiveKind.Regulator, [new PerspectiveFinding("reg-1", "claim-risk", "Regulator review", FindingSeverity.Warning)], "Regulator summary"),
                [PerspectiveKind.Historian] = new(PerspectiveKind.Historian, [new PerspectiveFinding("his-1", "pattern", "Historian review", FindingSeverity.Info)], "Historian summary"),
            }),
            new ContradictionReview("Counter-plan narrative", false, false),
            new ConfidenceProfile("keon.collective-stub-v1", "bounded", "moderate", "low", "1.0.0"),
            new ReasoningGraphRef("rg::test", 4, 3));
    }

    private static ProtocolAnalyzerService CreateStubbedAnalyzer(
        ProtocolParseResult parseResult,
        InteractionIntelligenceResponse interactionResponse,
        Action<StackDeliberationEnvelope>? onEnvelope = null)
    {
        var parser = new Mock<IProtocolParser>();
        parser
            .Setup(service => service.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parseResult);

        var knowledgeSource = new Mock<IKnowledgeSource>();
        knowledgeSource
            .Setup(service => service.GetCompoundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) =>
                parseResult.KnowledgeByCompound.TryGetValue(name, out var entry) ? entry : null);
        knowledgeSource
            .Setup(service => service.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(parseResult.KnowledgeByCompound.Values.ToList());

        var interaction = new Mock<IInteractionIntelligenceService>();
        interaction
            .Setup(service => service.EvaluateAsync(It.IsAny<IReadOnlyList<KnowledgeEntry>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(interactionResponse);

        var counterfactualEngine = new Mock<ICounterfactualEngine>();
        counterfactualEngine
            .Setup(service => service.OptimizeAsync(It.IsAny<List<ProtocolEntryResponse>>(), It.IsAny<IReadOnlyList<KnowledgeEntry>>(), It.IsAny<OptimizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CounterfactualResultDto(50, new List<InteractionCounterfactualResponse>(), new List<InteractionSwapRecommendationResponse>(), null, new List<GoalAwareOptimizationResponse>()));

        var cache = new ProtocolAnalysisCache(
            new MemoryCache(new MemoryCacheOptions()),
            new MemoryDistributedCache(new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions())),
            NullLogger<ProtocolAnalysisCache>.Instance);
        var normalization = new ProtocolNormalizationService();
        var fingerprint = new ProtocolFingerprintService();
        var ingestion = new ProtocolIngestionService(
            new IProtocolTextExtractor[] { new PlainTextProtocolExtractor() },
            normalization,
            fingerprint,
            cache,
            NullLogger<ProtocolIngestionService>.Instance);

        var stackReviewBoard = new Mock<IStackReviewBoardService>();
        stackReviewBoard
            .Setup(service => service.ReviewStackAsync(It.IsAny<StackDeliberationEnvelope>(), It.IsAny<CancellationToken>()))
            .Callback<StackDeliberationEnvelope, CancellationToken>((envelope, _) => onEnvelope?.Invoke(envelope))
            .ReturnsAsync(CreateReviewPayload());

        return new ProtocolAnalyzerService(
            parser.Object,
            ingestion,
            normalization,
            fingerprint,
            cache,
            knowledgeSource.Object,
            interaction.Object,
            new ProtocolSuggestionService(),
            counterfactualEngine.Object,
            stackReviewBoard.Object,
            new NullProtocolAnalysisPersistenceHook(),
            NullLogger<ProtocolAnalyzerService>.Instance);
    }

    private static ProtocolParseResult CreateParseResult(IEnumerable<KnowledgeEntry> entries)
    {
        var list = entries.ToList();
        return new ProtocolParseResult(
            list.Select(entry => new ProtocolEntryResponse(entry.CanonicalName, 0, string.Empty, "daily", string.Empty)).ToList(),
            list.ToDictionary(entry => entry.CanonicalName, entry => entry, StringComparer.OrdinalIgnoreCase),
            new List<ProtocolBlendExpansionResponse>());
    }

    private static KnowledgeEntry CreateKnowledgeEntry(
        string name,
        CompoundCategory category,
        BioStack.Domain.Enums.EvidenceTier evidenceTier,
        string mechanismSummary,
        List<string> pathways,
        List<string> benefits)
    {
        return new KnowledgeEntry
        {
            CanonicalName = name,
            Classification = category,
            EvidenceTier = evidenceTier,
            MechanismSummary = mechanismSummary,
            Pathways = pathways,
            Benefits = benefits,
        };
    }
}
