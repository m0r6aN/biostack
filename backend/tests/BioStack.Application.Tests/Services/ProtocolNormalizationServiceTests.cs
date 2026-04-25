namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using Xunit;

public sealed class ProtocolNormalizationServiceTests
{
    private readonly ProtocolNormalizationService _sut = new();

    // ─── NormalizeExtractedText ───────────────────────────────────────────────

    [Fact]
    public void NormalizeExtractedText_ReturnsEmpty_WhenInputIsEmpty()
    {
        Assert.Equal(string.Empty, _sut.NormalizeExtractedText(string.Empty));
    }

    [Fact]
    public void NormalizeExtractedText_ReturnsEmpty_WhenInputIsWhitespace()
    {
        Assert.Equal(string.Empty, _sut.NormalizeExtractedText("   "));
    }

    [Fact]
    public void NormalizeExtractedText_ReplacesMicrogramSymbolWithMcg()
    {
        var result = _sut.NormalizeExtractedText("BPC-157 500μg daily");
        Assert.Contains("mcg", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("μg", result);
    }

    [Fact]
    public void NormalizeExtractedText_ReplacesBulletWithDash()
    {
        var result = _sut.NormalizeExtractedText("• BPC-157\n• TB-500");
        Assert.Contains("- BPC-157", result);
        Assert.DoesNotContain("•", result);
    }

    [Fact]
    public void NormalizeExtractedText_CollapsesDuplicateSpaces()
    {
        var result = _sut.NormalizeExtractedText("BPC-157   500mcg  daily");
        Assert.DoesNotContain("  ", result);
    }

    [Fact]
    public void NormalizeExtractedText_RemovesBlankLines()
    {
        var input = "Line 1\n\n\n\nLine 2";
        var result = _sut.NormalizeExtractedText(input);
        Assert.DoesNotContain("\n\n\n", result);
    }

    [Fact]
    public void NormalizeExtractedText_TrimsLeadingTrailingWhitespacePerLine()
    {
        var input = "  BPC-157  \n  TB-500  ";
        var result = _sut.NormalizeExtractedText(input);
        Assert.Contains("BPC-157", result);
        Assert.DoesNotContain("  BPC-157", result);
    }

    // ─── BuildAnalysisContext ─────────────────────────────────────────────────

    [Fact]
    public void BuildAnalysisContext_TrimsGoalAndSex()
    {
        var ctx = _sut.BuildAnalysisContext("  recovery  ", " male ", null, null, null);
        Assert.Equal("recovery", ctx.Goal);
        Assert.Equal("male", ctx.Sex);
    }

    [Theory]
    [InlineData(null, "unknown")]
    [InlineData(25, "18-29")]
    [InlineData(30, "30-39")]
    [InlineData(35, "30-39")]
    [InlineData(40, "40-49")]
    [InlineData(50, "50-59")]
    [InlineData(60, "60-plus")]
    [InlineData(75, "60-plus")]
    public void BuildAnalysisContext_CorrectlyBandsAge(int? age, string expectedBand)
    {
        var ctx = _sut.BuildAnalysisContext(null, null, age, null, null);
        Assert.Equal(expectedBand, ctx.AgeBand);
    }

    [Theory]
    [InlineData(null, "unknown")]
    [InlineData(130.0, "under-140")]
    [InlineData(150.0, "140-179")]
    [InlineData(180.0, "180-219")]
    [InlineData(220.0, "220-plus")]
    [InlineData(300.0, "220-plus")]
    public void BuildAnalysisContext_CorrectlyBandsWeight(double? weight, string expectedBand)
    {
        var ctx = _sut.BuildAnalysisContext(null, null, null, weight, null);
        Assert.Equal(expectedBand, ctx.WeightBand);
    }

    [Fact]
    public void BuildAnalysisContext_DeduplicatesAndSortsStackContext()
    {
        var ctx = _sut.BuildAnalysisContext(null, null, null, null,
            new[] { "TB-500", "BPC-157", "tb-500" });

        // Deduplication is case-insensitive; order is alphabetical
        Assert.Equal(2, ctx.ExistingStackContext.Count);
        Assert.Equal("BPC-157", ctx.ExistingStackContext[0]);
    }

    [Fact]
    public void BuildAnalysisContext_FiltersBlankStackContextItems()
    {
        var ctx = _sut.BuildAnalysisContext(null, null, null, null,
            new[] { "BPC-157", "", "  " });
        Assert.Single(ctx.ExistingStackContext);
    }

    // ─── BuildOptimizationContext ─────────────────────────────────────────────

    [Fact]
    public void BuildOptimizationContext_DefaultsMaxCompoundsToFive()
    {
        var ctx = _sut.BuildOptimizationContext(null, null, null, null, null);
        Assert.Equal(5, ctx.MaxCompounds);
    }

    [Fact]
    public void BuildOptimizationContext_UsesProvidedMaxCompounds()
    {
        var ctx = _sut.BuildOptimizationContext(null, 3, null, null, null);
        Assert.Equal(3, ctx.MaxCompounds);
    }

    [Fact]
    public void BuildOptimizationContext_DeduplicatesRequiredCompoundIds()
    {
        var ctx = _sut.BuildOptimizationContext(null, null,
            new[] { "BPC-157", "bpc-157" }, null, null);
        Assert.Single(ctx.RequiredCompoundIds);
    }

    [Fact]
    public void BuildOptimizationContext_FiltersBlankExcludedIds()
    {
        var ctx = _sut.BuildOptimizationContext(null, null, null,
            new[] { "TB-500", "", "  " }, null);
        Assert.Single(ctx.ExcludedCompoundIds);
        Assert.Equal("TB-500", ctx.ExcludedCompoundIds[0]);
    }

    [Fact]
    public void BuildOptimizationContext_TrimsGoal()
    {
        var ctx = _sut.BuildOptimizationContext("  longevity  ", null, null, null, null);
        Assert.Equal("longevity", ctx.Goal);
    }
}
