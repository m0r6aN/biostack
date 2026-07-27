namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using Moq;
using Xunit;

public sealed class KnowledgeServicePublicBoundaryTests
{
    [Fact]
    public async Task GetCompoundAsync_PreservesObservationalEvidence_AndWithholdsPrescriptiveFields()
    {
        var source = new Mock<IKnowledgeSource>();
        source
            .Setup(service => service.GetCompoundAsync("Example", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLegacyEntry());

        var result = await new KnowledgeService(source.Object).GetCompoundAsync("Example");

        Assert.NotNull(result);
        Assert.Equal("Observed mechanism.", result.MechanismSummary);
        Assert.Equal(EvidenceTier.Limited, result.EvidenceTier);
        Assert.Equal(new[] { "Source reference" }, result.SourceReferences);
        Assert.Equal(new[] { "Observed benefit" }, result.Benefits);
        Assert.Equal(new[] { "Reported caution" }, result.AvoidWith);
        Assert.Equal(new[] { "Observed interaction" }, result.DrugInteractions);
        Assert.Empty(result.PairsWellWith);
        Assert.Empty(result.CompatibleBlends);
        Assert.Empty(result.VialCompatibility);
        Assert.Empty(result.RecommendedDosage);
        Assert.Empty(result.StandardDosageRange);
        Assert.Empty(result.MaxReportedDose);
        Assert.Empty(result.Frequency);
        Assert.Empty(result.PreferredTimeOfDay);
        Assert.Empty(result.WeeklyDosageSchedule);
        Assert.Empty(result.IncrementalEscalationSteps);
        Assert.Null(result.TieredDosing);
        Assert.Empty(result.OptimizationProtein);
        Assert.Empty(result.OptimizationCarbs);
        Assert.Empty(result.OptimizationSupplements);
        Assert.Empty(result.OptimizationSleep);
        Assert.Empty(result.OptimizationExercise);
    }

    [Fact]
    public async Task GetAllCompoundsAsync_UsesTheSamePublicProjection()
    {
        var source = new Mock<IKnowledgeSource>();
        source
            .Setup(service => service.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntry> { CreateLegacyEntry() });

        var result = Assert.Single(await new KnowledgeService(source.Object).GetAllCompoundsAsync());

        Assert.Empty(result.RecommendedDosage);
        Assert.Empty(result.PairsWellWith);
        Assert.Equal(new[] { "Reported caution" }, result.AvoidWith);
    }

    [Fact]
    public async Task GetAllCompoundsAsync_DeduplicatesCanonicalNamesWithoutDeletingEvidence()
    {
        var sparse = CreateLegacyEntry();
        sparse.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        sparse.CanonicalName = " Example ";
        sparse.SourceReferences = new List<string>();
        sparse.MechanismSummary = string.Empty;
        sparse.AvoidWith = new List<string> { "Sparse-row caution" };
        sparse.DrugInteractions = new List<string> { "Sparse-row interaction" };

        var evidenceRicher = CreateLegacyEntry();
        evidenceRicher.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        evidenceRicher.CanonicalName = "example";
        evidenceRicher.SourceReferences = new List<string> { "Source A", "Source B" };
        evidenceRicher.MechanismSummary = "Evidence-richer mechanism.";
        evidenceRicher.AvoidWith = new List<string> { "Evidence-row caution" };
        evidenceRicher.DrugInteractions = new List<string> { "Evidence-row interaction" };

        var source = new Mock<IKnowledgeSource>();
        source
            .Setup(service => service.GetAllCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntry> { sparse, evidenceRicher });

        var results = (await new KnowledgeService(source.Object).GetAllCompoundsAsync()).ToList();

        var result = Assert.Single(results);
        Assert.Equal("example", result.CanonicalName);
        Assert.Equal("Evidence-richer mechanism.", result.MechanismSummary);
        Assert.Equal(new[] { "Source A", "Source B" }, result.SourceReferences);
        Assert.Equal(
            new[] { "Sparse-row caution", "Evidence-row caution" },
            result.AvoidWith);
        Assert.Equal(
            new[] { "Sparse-row interaction", "Evidence-row interaction" },
            result.DrugInteractions);
    }

    private static KnowledgeEntry CreateLegacyEntry()
    {
        return new KnowledgeEntry
        {
            CanonicalName = "Example",
            Classification = CompoundCategory.Peptide,
            RegulatoryStatus = "Research",
            MechanismSummary = "Observed mechanism.",
            EvidenceTier = EvidenceTier.Limited,
            SourceReferences = new List<string> { "Source reference" },
            Pathways = new List<string> { "Observed pathway" },
            Benefits = new List<string> { "Observed benefit" },
            PairsWellWith = new List<string> { "Pairing recommendation" },
            AvoidWith = new List<string> { "Reported caution" },
            CompatibleBlends = new List<string> { "Blend recommendation" },
            VialCompatibility = "Co-vial direction",
            RecommendedDosage = "250 mcg",
            StandardDosageRange = "250-500 mcg",
            MaxReportedDose = "500 mcg",
            Frequency = "Twice daily",
            PreferredTimeOfDay = "Morning",
            WeeklyDosageSchedule = new List<string> { "Week 1: 250 mcg" },
            IncrementalEscalationSteps = new List<string> { "Increase after one week" },
            TieredDosing = new TieredDosingData(),
            DrugInteractions = new List<string> { "Observed interaction" },
            OptimizationProtein = "2 g/kg/day",
            OptimizationCarbs = "200 g/day",
            OptimizationSupplements = new List<string> { "Supplement recommendation" },
            OptimizationSleep = "8 hours",
            OptimizationExercise = "Train daily"
        };
    }
}
