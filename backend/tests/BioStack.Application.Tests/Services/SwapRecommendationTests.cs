namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public class SwapRecommendationTests
{
    // Stack baseline: two peptides that overlap on exactly one pathway.
    // This gives a measurable redundancy penalty so candidates that break the
    // overlap produce a composite-score delta above the suppression threshold.
    private static (InteractionIntelligenceService service, List<KnowledgeEntry> stack) BuildTestRig(
        List<KnowledgeEntry> candidatePool)
    {
        var bpc = new KnowledgeEntry
        {
            CanonicalName = "BPC-157",
            Classification = CompoundCategory.Peptide,
            Pathways = new List<string> { "tissue-repair", "angiogenesis", "gi-protective" },
            EvidenceTier = EvidenceTier.Moderate
        };
        var tb = new KnowledgeEntry
        {
            CanonicalName = "TB-500",
            Classification = CompoundCategory.Peptide,
            Pathways = new List<string> { "tissue-repair", "anti-inflammatory", "cell-migration" },
            EvidenceTier = EvidenceTier.Moderate
        };

        var knowledgeSource = new Mock<IKnowledgeSource>();
        knowledgeSource
            .Setup(source => source.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidatePool);
        knowledgeSource
            .Setup(source => source.GetCompoundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) =>
                candidatePool.FirstOrDefault(entry =>
                    string.Equals(entry.CanonicalName, name, StringComparison.OrdinalIgnoreCase)));

        var hintRepository = new Mock<ICompoundInteractionHintRepository>();
        hintRepository
            .Setup(repo => repo.FindPairAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompoundInteractionHint?)null);

        var service = new InteractionIntelligenceService(knowledgeSource.Object, hintRepository.Object);
        return (service, new List<KnowledgeEntry> { bpc, tb });
    }

    private static KnowledgeEntry MakePeptide(
        string name,
        List<string> pathways,
        EvidenceTier evidence = EvidenceTier.Moderate,
        List<string>? avoidWith = null,
        List<string>? pairsWellWith = null,
        List<string>? aliases = null)
    {
        return new KnowledgeEntry
        {
            CanonicalName = name,
            Aliases = aliases ?? new List<string>(),
            Classification = CompoundCategory.Peptide,
            Pathways = pathways,
            EvidenceTier = evidence,
            AvoidWith = avoidWith ?? new List<string>(),
            PairsWellWith = pairsWellWith ?? new List<string>()
        };
    }

    [Fact]
    public async Task Swaps_ExcludeInStackCandidatesEvenWhenAliasMatches()
    {
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide("BPC-157", new List<string> { "tissue-repair" }),
            MakePeptide("PL-14736", new List<string> { "tissue-repair" }, aliases: new List<string> { "BPC-157" }),
            MakePeptide("GHK-Cu", new List<string> { "collagen-synthesis", "tissue-repair" })
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.DoesNotContain(result.Swaps, swap =>
            string.Equals(swap.CandidateCompound, "BPC-157", StringComparison.OrdinalIgnoreCase)
            || string.Equals(swap.CandidateCompound, "PL-14736", StringComparison.OrdinalIgnoreCase)
            || string.Equals(swap.CandidateCompound, "TB-500", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Swaps_ExcludeLowSimilarityAndCrossClassCandidates()
    {
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide("Unrelated-Peptide", new List<string> { "some-random-pathway" }),
            new KnowledgeEntry
            {
                CanonicalName = "Not-A-Peptide",
                Classification = CompoundCategory.Supplement,
                Pathways = new List<string> { "tissue-repair", "angiogenesis" },
                EvidenceTier = EvidenceTier.Strong
            },
            MakePeptide("GHK-Cu", new List<string> { "collagen-synthesis", "tissue-repair" }, EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.DoesNotContain(result.Swaps, swap => swap.CandidateCompound == "Unrelated-Peptide");
        Assert.DoesNotContain(result.Swaps, swap => swap.CandidateCompound == "Not-A-Peptide");
    }

    [Fact]
    public async Task Swaps_ExcludeCandidatesThatIntroduceKnownInterference()
    {
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide(
                "Interfering-Repair",
                new List<string> { "tissue-repair", "angiogenesis", "gi-protective" },
                EvidenceTier.Strong,
                avoidWith: new List<string> { "TB-500" }),
            MakePeptide(
                "Clean-Repair",
                new List<string> { "angiogenesis", "gi-protective", "wound-healing" },
                EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.DoesNotContain(result.Swaps, swap => swap.CandidateCompound == "Interfering-Repair");
    }

    [Fact]
    public async Task Swaps_AreRankedByDeltaScoreDescending()
    {
        var candidates = new List<KnowledgeEntry>
        {
            // Replaces BPC-157: preserves 2/3 of BPC's pathways and removes tissue-repair overlap with TB-500.
            MakePeptide(
                "BpcReplacer-A",
                new List<string> { "angiogenesis", "gi-protective", "wound-healing" },
                EvidenceTier.Strong),
            // Replaces TB-500: preserves 2/3 of TB's pathways and removes tissue-repair overlap with BPC.
            MakePeptide(
                "TbReplacer-B",
                new List<string> { "anti-inflammatory", "cell-migration", "bone-density" },
                EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.NotEmpty(result.Swaps);
        for (var i = 1; i < result.Swaps.Count; i++)
        {
            Assert.True(result.Swaps[i - 1].DeltaScore >= result.Swaps[i].DeltaScore,
                $"swap[{i - 1}].DeltaScore ({result.Swaps[i - 1].DeltaScore}) must be >= swap[{i}].DeltaScore ({result.Swaps[i].DeltaScore})");
        }
    }

    [Fact]
    public async Task Swaps_SuppressRecommendationsBelowMinDeltaThreshold()
    {
        // Candidate mirrors BPC-157's pathways exactly, so swapping preserves the
        // same overlap profile against TB-500 and produces a near-zero delta.
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide(
                "NearlyIdentical",
                new List<string> { "tissue-repair", "angiogenesis", "gi-protective" },
                EvidenceTier.Moderate)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.DoesNotContain(result.Swaps, swap =>
            Math.Abs(swap.DeltaScore) < 2d);
    }

    [Fact]
    public async Task Swaps_ProduceReasonsFromControlledVocabulary()
    {
        var validReasons = new HashSet<string>
        {
            SwapReasonAtoms.ReducesRedundancy,
            SwapReasonAtoms.PreservesSynergy,
            SwapReasonAtoms.LowersInterference,
            SwapReasonAtoms.ImprovesGoalAlignment,
            SwapReasonAtoms.ImprovesSignalClarity,
            SwapReasonAtoms.StrongerEvidence,
            SwapReasonAtoms.LowerEstimatedCost
        };

        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide(
                "Clean-Repair",
                new List<string> { "angiogenesis", "gi-protective", "wound-healing" },
                EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.NotEmpty(result.Swaps);
        foreach (var swap in result.Swaps)
        {
            Assert.InRange(swap.Reasons.Count, 1, 3);
            foreach (var reason in swap.Reasons)
            {
                Assert.Contains(reason, validReasons);
            }

            Assert.Contains(swap.Verdict, new[]
            {
                SwapVerdicts.LikelyImproves,
                SwapVerdicts.LittleExpectedChange,
                SwapVerdicts.LikelyWorsens
            });
        }
    }

    [Fact]
    public async Task Swaps_EmitRecommendationStringReferencingBothCompounds()
    {
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide(
                "Clean-Repair",
                new List<string> { "angiogenesis", "gi-protective", "wound-healing" },
                EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        var swap = Assert.Single(result.Swaps, s => s.CandidateCompound == "Clean-Repair");
        Assert.Contains(swap.OriginalCompound, swap.Recommendation);
        Assert.Contains(swap.CandidateCompound, swap.Recommendation);
    }

    [Fact]
    public async Task Swaps_DoNotBreakRemoveOneCounterfactuals()
    {
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide(
                "Clean-Repair",
                new List<string> { "angiogenesis", "gi-protective", "wound-healing" },
                EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.NotEmpty(result.Counterfactuals);
        Assert.Equal(stack.Count, result.Counterfactuals.Count);
    }

    [Fact]
    public async Task Swaps_ExcludePathwayNotPreservedCandidates_TraceRecordsReason()
    {
        // Weak-overlap candidate shares only 1 of BPC's 3 pathways — below the
        // preservation floor — so it must be excluded with pathway_not_preserved.
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide("Weak-Overlap", new List<string> { "tissue-repair", "unrelated-1" }, EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var eval = await service.EvaluateSwapsWithTraceAsync(stack, CancellationToken.None);

        Assert.DoesNotContain(eval.Recommendations, swap => swap.CandidateCompound == "Weak-Overlap");
        Assert.Contains(eval.Traces, trace =>
            trace.CandidateCompound == "Weak-Overlap"
            && !trace.PassedEligibility
            && trace.ExclusionReason == SwapRecommendationEngine.ExclusionPathwayNotPreserved);
    }

    [Fact]
    public async Task Swaps_SuppressWhenDeltaTrivial_TraceRecordsSuppressionReason()
    {
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide(
                "NearlyIdentical",
                new List<string> { "tissue-repair", "angiogenesis", "gi-protective" },
                EvidenceTier.Moderate)
        };

        var (service, stack) = BuildTestRig(candidates);
        var eval = await service.EvaluateSwapsWithTraceAsync(stack, CancellationToken.None);

        Assert.Empty(eval.Recommendations);
        Assert.Contains(eval.Traces, trace =>
            trace.CandidateCompound == "NearlyIdentical"
            && trace.PassedEligibility
            && !trace.Surfaced
            && trace.SuppressionReason == SwapRecommendationEngine.SuppressionTrivialDelta);
    }

    [Fact]
    public async Task Swaps_NeverEmitLowerEstimatedCostReason()
    {
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide(
                "Clean-Repair",
                new List<string> { "angiogenesis", "gi-protective", "wound-healing" },
                EvidenceTier.Strong),
            MakePeptide(
                "Alt-Repair",
                new List<string> { "tissue-repair", "angiogenesis", "wound-healing" },
                EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.All(result.Swaps, swap =>
            Assert.DoesNotContain(SwapReasonAtoms.LowerEstimatedCost, swap.Reasons));
    }

    [Fact]
    public async Task Swaps_ImprovesGoalAlignmentSuppressedWhenPathwayCoverageWeak()
    {
        // Candidate is pathway-preserving just enough to be eligible (2/3) but we
        // expect no goal-alignment reason unless intended-use score is strong.
        // Because other reason atoms (reduces_redundancy) will fire here, the
        // fallback goal-alignment reason must not be emitted at all.
        var candidates = new List<KnowledgeEntry>
        {
            MakePeptide(
                "Clean-Repair",
                new List<string> { "angiogenesis", "gi-protective", "wound-healing" },
                EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        var swap = Assert.Single(result.Swaps, s => s.CandidateCompound == "Clean-Repair");
        Assert.DoesNotContain(SwapReasonAtoms.ImprovesGoalAlignment, swap.Reasons);
        Assert.Contains(SwapReasonAtoms.ReducesRedundancy, swap.Reasons);
    }

    [Fact]
    public async Task Swaps_RankingPrefersStrongerIntendedUseAndEvidenceOnTies()
    {
        // Both candidates should produce the same composite delta (both remove
        // the tissue-repair overlap). The one that preserves more of BPC's
        // pathways and has a stronger evidence tier should rank first.
        var candidates = new List<KnowledgeEntry>
        {
            // 2/3 preservation, Moderate evidence
            MakePeptide(
                "LowerIntendedUse",
                new List<string> { "angiogenesis", "gi-protective", "wound-healing" },
                EvidenceTier.Moderate),
            // 3/3 preservation but without tissue-repair: use angiogenesis + gi-protective + a third BPC pathway... BPC has 3 pathways, so to get 3/3 we'd need tissue-repair.
            // Use 2/3 but different pair + Strong evidence instead.
            MakePeptide(
                "HigherIntendedUse",
                new List<string> { "angiogenesis", "gi-protective", "cell-signaling" },
                EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.NotEmpty(result.Swaps);
        // Find both candidates in the surfaced swaps
        var higher = result.Swaps.FirstOrDefault(s => s.CandidateCompound == "HigherIntendedUse");
        var lower = result.Swaps.FirstOrDefault(s => s.CandidateCompound == "LowerIntendedUse");
        if (higher is not null && lower is not null)
        {
            var higherIdx = result.Swaps.IndexOf(higher);
            var lowerIdx = result.Swaps.IndexOf(lower);
            Assert.True(higherIdx <= lowerIdx,
                "stronger-evidence candidate should rank no worse than weaker-evidence candidate on equal delta");
        }
        else
        {
            // At minimum the stronger candidate should surface if the pool allows it.
            Assert.NotNull(higher);
        }
    }

    // Regression fixtures for the `preserves_synergy` pair-identity gate (PR #36 review).
    // Peers B and D are Supplements so only the A->X swap clears the classification gate,
    // leaving a single recommendation to assert against.
    private static (List<KnowledgeEntry> current, List<KnowledgeEntry> pool,
        InteractionSummaryResponse baselineSummary,
        List<InteractionResultResponse> baselineInteractions,
        double baselineComposite) BuildPairIdentityRig(EvidenceTier candidateEvidence)
    {
        var original = MakePeptide("A", new List<string> { "p1", "p2", "p3" });
        var peerB = new KnowledgeEntry
        {
            CanonicalName = "B",
            Classification = CompoundCategory.Supplement,
            Pathways = new List<string> { "p1" },
            EvidenceTier = EvidenceTier.Moderate
        };
        var peerD = new KnowledgeEntry
        {
            CanonicalName = "D",
            Classification = CompoundCategory.Supplement,
            Pathways = new List<string> { "p2" },
            EvidenceTier = EvidenceTier.Moderate
        };
        var candidate = MakePeptide("X", new List<string> { "p1", "p2", "p3" }, candidateEvidence);

        var baselineInteractions = new List<InteractionResultResponse>
        {
            new("A", "B", InteractionType.Synergistic, 0.8d, new List<string> { "p1" }, string.Empty, false),
            new("B", "D", InteractionType.Synergistic, 0.8d, new List<string>(), string.Empty, false)
        };

        return (
            new List<KnowledgeEntry> { original, peerB, peerD },
            new List<KnowledgeEntry> { candidate },
            new InteractionSummaryResponse(2, 0, 0),
            baselineInteractions,
            10d);
    }

    private static InteractionIntelligenceResponse BuildVariant(
        double compositeScore,
        InteractionSummaryResponse summary,
        List<InteractionResultResponse> interactions)
    {
        return new InteractionIntelligenceResponse(
            summary,
            new ProtocolInteractionScoreResponse(0d, 0d, 0d),
            compositeScore,
            new List<InteractionFindingResponse>(),
            interactions,
            new List<InteractionCounterfactualResponse>(),
            new List<InteractionSwapRecommendationResponse>());
    }

    [Fact]
    public async Task Swaps_FiresPreservesSynergyWhenBaselinePairSurvives()
    {
        var (current, pool, baselineSummary, baselineInteractions, baselineComposite) =
            BuildPairIdentityRig(EvidenceTier.Moderate);

        // Variant keeps the (B,D) baseline synergy intact after A -> X.
        var variant = BuildVariant(
            15d,
            new InteractionSummaryResponse(2, 0, 0),
            new List<InteractionResultResponse>
            {
                new("X", "B", InteractionType.Synergistic, 0.8d, new List<string> { "p1" }, string.Empty, false),
                new("B", "D", InteractionType.Synergistic, 0.8d, new List<string>(), string.Empty, false)
            });

        var result = await SwapRecommendationEngine.EvaluateAsync(
            current,
            pool,
            baselineComposite,
            baselineSummary,
            baselineInteractions,
            (_, _) => Task.FromResult(variant),
            CancellationToken.None);

        var swap = Assert.Single(result.Recommendations);
        Assert.Equal("A", swap.OriginalCompound);
        Assert.Equal("X", swap.CandidateCompound);
        Assert.Contains(SwapReasonAtoms.PreservesSynergy, swap.Reasons);
    }

    [Fact]
    public async Task Swaps_DoesNotFirePreservesSynergyWhenBaselinePairIsLostButCountIsMaintained()
    {
        // Candidate with Strong evidence so stronger_evidence fires and the swap still surfaces
        // even though preserves_synergy must not.
        var (current, pool, baselineSummary, baselineInteractions, baselineComposite) =
            BuildPairIdentityRig(EvidenceTier.Strong);

        // Variant drops the (B,D) baseline pair but introduces two NEW synergies so the
        // aggregate synergy count is at least as high as baseline - this is the exact
        // scenario where the old count-vs-count gate would falsely fire preserves_synergy.
        var variant = BuildVariant(
            13d,
            new InteractionSummaryResponse(2, 0, 0),
            new List<InteractionResultResponse>
            {
                new("X", "B", InteractionType.Synergistic, 0.8d, new List<string> { "p1" }, string.Empty, false),
                new("X", "D", InteractionType.Synergistic, 0.8d, new List<string> { "p2" }, string.Empty, false)
            });

        var result = await SwapRecommendationEngine.EvaluateAsync(
            current,
            pool,
            baselineComposite,
            baselineSummary,
            baselineInteractions,
            (_, _) => Task.FromResult(variant),
            CancellationToken.None);

        var swap = Assert.Single(result.Recommendations);
        Assert.Equal("A", swap.OriginalCompound);
        Assert.Equal("X", swap.CandidateCompound);
        Assert.DoesNotContain(SwapReasonAtoms.PreservesSynergy, swap.Reasons);
        Assert.Contains(SwapReasonAtoms.StrongerEvidence, swap.Reasons);
    }

}
