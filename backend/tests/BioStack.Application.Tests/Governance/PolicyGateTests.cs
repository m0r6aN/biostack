namespace BioStack.Application.Tests.Governance;

using BioStack.Application.Governance;
using BioStack.Infrastructure.Keon;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

[Trait("Category", "Unit")]
public class PolicyGateTests
{
    // ── LocalPreClassify ──────────────────────────────────────────────────────

    private static PolicyGate BuildGate(IKeonRuntimeClient? keon = null)
    {
        keon ??= new Mock<IKeonRuntimeClient>().Object;
        return new PolicyGate(keon, NullLogger<PolicyGate>.Instance);
    }

    [Theory]
    [InlineData("you should take this supplement")]
    [InlineData("You Should Take This Supplement")]   // case-insensitive
    [InlineData("you must consult a provider")]
    [InlineData("take 500mg daily for best results")]
    [InlineData("take 250 mcg at bedtime")]
    [InlineData("this is safe for most people")]
    [InlineData("it cures inflammation")]
    [InlineData("This cures the condition")]
    [InlineData("proven to reduce cortisol")]
    [InlineData("stop taking immediately")]
    public void LocalPreClassify_ReturnsProhibited_ForBannedPhrases(string text)
    {
        var gate = BuildGate();
        var result = gate.LocalPreClassify(text);
        Assert.Equal(LanguageClassification.Prohibited, result);
    }

    [Theory]
    [InlineData("Magnesium supports sleep quality")]
    [InlineData("Studies suggest an association between ashwagandha and reduced cortisol.")]
    [InlineData("Observed correlation within normal range.")]
    [InlineData("Evidence-limited commentary only.")]
    public void LocalPreClassify_ReturnsNull_ForNeutralPhrases(string text)
    {
        var gate = BuildGate();
        var result = gate.LocalPreClassify(text);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LocalPreClassify_ReturnsNull_ForEmptyOrWhitespace(string text)
    {
        var gate = BuildGate();
        var result = gate.LocalPreClassify(text);
        Assert.Null(result);
    }

    // ── CheckAsync: ArgumentException on empty text ───────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CheckAsync_ThrowsArgumentException_WhenTextIsEmptyOrWhitespace(string text)
    {
        var gate = BuildGate();
        await Assert.ThrowsAsync<ArgumentException>(
            () => gate.CheckAsync(text, "srb-finding", "t1", "a1"));
    }

    // ── CheckAsync: local pre-classifier blocks before Keon ──────────────────

    [Fact]
    public async Task CheckAsync_BlocksLocally_WhenProhibitedPhrase_WithoutCallingKeon()
    {
        var mockKeon = new Mock<IKeonRuntimeClient>(MockBehavior.Strict);
        // PolicyCheckAsync must NOT be called — Strict mock will throw if it is.
        var gate = new PolicyGate(mockKeon.Object, NullLogger<PolicyGate>.Instance);

        var result = await gate.CheckAsync(
            "you should take 500mg daily",
            "srb-finding", "tenant-1", "actor-1");

        Assert.Equal(PolicyDecision.Blocked, result.Decision);
        Assert.True(result.LocallyClassified, "Should be marked as locally classified");
        Assert.NotNull(result.BlockReason);
        Assert.Contains("local-classifier", result.BlockReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("local-classifier-v0", result.PolicyHash.Value);
    }

    // ── CheckAsync: pass-through from Keon ───────────────────────────────────

    [Fact]
    public async Task CheckAsync_PassesThroughKeonResult_ForNeutralText()
    {
        var expectedHash = new PolicyHash("keon-policy-v1", "1.0.0");
        var keonResult = new PolicyGateResult(
            PolicyDecision.Blocked,
            DisclaimerText: null,
            RewrittenText: null,
            BlockReason: "keon-offline: Keon Runtime unavailable — fail-closed",
            PolicyHash: expectedHash);

        var mockKeon = new Mock<IKeonRuntimeClient>();
        mockKeon
            .Setup(k => k.PolicyCheckAsync(It.IsAny<PolicyGateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(keonResult);

        var gate = new PolicyGate(mockKeon.Object, NullLogger<PolicyGate>.Instance);

        var result = await gate.CheckAsync(
            "Magnesium supports sleep quality",
            "srb-finding", "tenant-1", "actor-1");

        Assert.Equal(PolicyDecision.Blocked, result.Decision);
        Assert.False(result.LocallyClassified, "Keon result should not be locally classified");
        Assert.Equal("keon-policy-v1", result.PolicyHash.Value);
        mockKeon.Verify(
            k => k.PolicyCheckAsync(It.IsAny<PolicyGateRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAsync_ReturnsAllowed_WhenKeonAllows()
    {
        var expectedHash = new PolicyHash("keon-policy-v1", "1.0.0");
        var keonResult = new PolicyGateResult(
            PolicyDecision.Allowed,
            DisclaimerText: null,
            RewrittenText: null,
            BlockReason: null,
            PolicyHash: expectedHash);

        var mockKeon = new Mock<IKeonRuntimeClient>();
        mockKeon
            .Setup(k => k.PolicyCheckAsync(It.IsAny<PolicyGateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(keonResult);

        var gate = new PolicyGate(mockKeon.Object, NullLogger<PolicyGate>.Instance);

        var result = await gate.CheckAsync(
            "Studies suggest an association between supplementation and recovery.",
            "compound-dossier", "tenant-1", "actor-1");

        Assert.Equal(PolicyDecision.Allowed, result.Decision);
        Assert.False(result.LocallyClassified);
        Assert.Null(result.BlockReason);
    }
}
