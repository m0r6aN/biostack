namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using Xunit;

public sealed class BlendDecomposerServiceTests
{
    private readonly BlendDecomposerService _sut = new();

    // ─── Non-blend inputs ────────────────────────────────────────────────────

    [Fact]
    public void Decompose_ReturnsNone_WhenInputIsEmpty()
    {
        var result = _sut.Decompose(string.Empty);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void Decompose_ReturnsNone_WhenInputIsWhitespace()
    {
        var result = _sut.Decompose("   ");
        Assert.Empty(result.Components);
    }

    [Fact]
    public void Decompose_ReturnsNone_WhenInputHasNoBlendWord()
    {
        var result = _sut.Decompose("BPC-157 500mcg daily");
        Assert.Empty(result.Components);
    }

    // ─── Parenthetical component extraction ──────────────────────────────────

    [Fact]
    public void Decompose_ExtractsComponentsFromParentheses()
    {
        var result = _sut.Decompose("Recovery blend (BPC-157, TB-500, GHK-cu)");
        Assert.Equal(3, result.Components.Count);
        Assert.Contains("BPC-157", result.Components);
        Assert.Contains("TB-500", result.Components);
        Assert.Contains("GHK-cu", result.Components);
    }

    [Fact]
    public void Decompose_UsesBlendNameAsExtractedName()
    {
        var result = _sut.Decompose("recovery blend (BPC-157, TB-500)");
        Assert.Contains("blend", result.BlendName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decompose_TrimsComponentNames()
    {
        var result = _sut.Decompose("Test blend ( NAD+ ,  MOTS-c )");
        Assert.Contains("NAD+", result.Components);
        Assert.Contains("MOTS-c", result.Components);
    }

    // ─── Known blends ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Triple Threat Blend")]
    [InlineData("triple threat blend")]
    [InlineData("TRIPLE THREAT BLEND")]
    public void Decompose_ResolvesTripleThreatBlend_CaseInsensitive(string input)
    {
        var result = _sut.Decompose(input);
        Assert.Equal(3, result.Components.Count);
        Assert.Contains("NAD+", result.Components);
        Assert.Contains("MOTS-c", result.Components);
        Assert.Contains("5-Amino-1MQ", result.Components);
    }

    [Fact]
    public void Decompose_ResolvesGlowBlend()
    {
        var result = _sut.Decompose("GLOW Blend");
        Assert.Equal(3, result.Components.Count);
        Assert.Contains("GHK-cu", result.Components);
        Assert.Contains("BPC-157", result.Components);
        Assert.Contains("TB-500", result.Components);
    }

    [Fact]
    public void Decompose_ResolvesKlowBlend()
    {
        var result = _sut.Decompose("KLOW Blend");
        Assert.Equal(4, result.Components.Count);
        Assert.Contains("KPV", result.Components);
    }

    [Fact]
    public void Decompose_ResolvesTesamorelinIpamorelinBlend()
    {
        var result = _sut.Decompose("Tesamorelin/Ipamorelin Blend");
        Assert.Equal(2, result.Components.Count);
        Assert.Contains("Tesamorelin", result.Components);
        Assert.Contains("Ipamorelin", result.Components);
    }

    // ─── Slash-separated name decomposition ──────────────────────────────────

    [Fact]
    public void Decompose_ExtractsSlashSeparatedComponents()
    {
        var result = _sut.Decompose("CJC-1295/Ipamorelin Blend 2mg/200mcg daily");
        Assert.Equal(2, result.Components.Count);
        Assert.Contains("CJC-1295", result.Components);
        Assert.Contains("Ipamorelin", result.Components);
    }

    [Fact]
    public void Decompose_ReturnsNone_WhenBlendPrefixHasNoSlashAndIsUnknown()
    {
        var result = _sut.Decompose("Mystical recovery blend");
        // No parentheses, not in known blends, no slash — should return None
        Assert.Empty(result.Components);
    }

    // ─── BlendDecompositionResult static None property ───────────────────────

    [Fact]
    public void None_HasEmptyBlendNameAndComponents()
    {
        var none = BlendDecompositionResult.None;
        Assert.Equal(string.Empty, none.BlendName);
        Assert.Empty(none.Components);
    }

    // ─── Parenthetical takes priority over known blend lookup ─────────────────

    [Fact]
    public void Decompose_UsesParentheticalComponents_EvenForKnownBlend()
    {
        // If the user provides explicit parenthetical components, use those
        var result = _sut.Decompose("Triple Threat Blend (CompoundA, CompoundB)");
        Assert.Equal(2, result.Components.Count);
        Assert.Contains("CompoundA", result.Components);
        Assert.Contains("CompoundB", result.Components);
    }
}
