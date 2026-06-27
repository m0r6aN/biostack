namespace BioStack.Api.Tests.Unit.Governance;

using BioStack.Application.Governance;
using Xunit;

/// <summary>
/// Lane H — the high-risk category gate classifies risky substances/categories and emits
/// warning-first, evidence-limited framing, while leaving ordinary supplements unflagged.
/// </summary>
[Trait("Category", "Unit")]
public class HighRiskCategoryGateTests
{
    private readonly HighRiskCategoryGate _gate = new();

    [Theory]
    [InlineData("Ostarine", HighRiskCategory.Sarm)]
    [InlineData("RAD-140", HighRiskCategory.Sarm)]
    [InlineData("Tamoxifen", HighRiskCategory.Serm)]
    [InlineData("BPC-157", HighRiskCategory.InvestigationalPeptide)]
    [InlineData("Semaglutide", HighRiskCategory.CompoundedGlp1)]
    [InlineData("Testosterone", HighRiskCategory.PrescriptionOnly)]
    [InlineData("Clenbuterol", HighRiskCategory.BannedInSport)]
    public void Assess_FlagsHighRiskSubstance_ByName(string substance, string expectedCategory)
    {
        var result = _gate.Assess([substance]);

        Assert.True(result.IsHighRisk);
        Assert.Contains(expectedCategory, result.Categories);
        Assert.NotEmpty(result.RequiredFramings);
    }

    [Fact]
    public void Assess_OrdinarySupplements_AreNotHighRisk()
    {
        var result = _gate.Assess(["Creatine", "Beta-Alanine", "Magnesium"]);

        Assert.False(result.IsHighRisk);
        Assert.Empty(result.Categories);
        Assert.Empty(result.RequiredFramings);
    }

    [Theory]
    [InlineData("peptide", HighRiskCategory.InvestigationalPeptide)]
    [InlineData("gray-market", HighRiskCategory.GrayMarketPeptide)]
    [InlineData("unknown source", HighRiskCategory.UnknownIdentity)]
    [InlineData("research chemical", HighRiskCategory.UnclearRegulatoryStatus)]
    public void Assess_FlagsHighRisk_ByCategoryHint(string hint, string expectedCategory)
    {
        var result = _gate.Assess(substances: null, categoryHints: [hint]);

        Assert.True(result.IsHighRisk);
        Assert.Contains(expectedCategory, result.Categories);
    }

    [Fact]
    public void Assess_Framings_AreEvidenceLimited_AndNonImperative()
    {
        var result = _gate.Assess(["Ostarine"]);

        Assert.All(result.RequiredFramings, f =>
        {
            // No imperative usage language in required framings.
            Assert.DoesNotContain("you should", f, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("take ", f, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Contains(result.RequiredFramings, f =>
            f.Contains("educational", StringComparison.OrdinalIgnoreCase));
    }
}
