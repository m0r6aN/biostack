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
                new List<string> { "tissue-repair", "angiogenesis" },
                EvidenceTier.Strong,
                avoidWith: new List<string> { "TB-500" }),
            MakePeptide("Clean-Repair", new List<string> { "tissue-repair", "wound-healing" }, EvidenceTier.Strong)
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
            // Replaces BPC-157: removes the tissue-repair overlap with TB-500.
            MakePeptide("BpcReplacer-A", new List<string> { "gi-protective", "wound-healing" }, EvidenceTier.Strong),
            // Replaces TB-500: shares cell-migration with TB but no overlap with BPC after swap.
            MakePeptide("TbReplacer-B", new List<string> { "cell-migration", "bone-density" }, EvidenceTier.Strong)
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
            MakePeptide("Clean-Repair", new List<string> { "gi-protective", "wound-healing" }, EvidenceTier.Strong)
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
            MakePeptide("Clean-Repair", new List<string> { "gi-protective", "wound-healing" }, EvidenceTier.Strong)
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
            MakePeptide("Clean-Repair", new List<string> { "gi-protective", "wound-healing" }, EvidenceTier.Strong)
        };

        var (service, stack) = BuildTestRig(candidates);
        var result = await service.EvaluateAsync(stack, CancellationToken.None);

        Assert.NotEmpty(result.Counterfactuals);
        Assert.Equal(stack.Count, result.Counterfactuals.Count);
    }
}
