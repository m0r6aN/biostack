namespace BioStack.Api.Tests;

using BioStack.Infrastructure.Keon;
using Xunit;

[Trait("Category", "Unit")]
public class KeonRuntimeClientStubTests
{
    [Fact]
    public async Task CheckHealth_ReturnsOfflineStatus()
    {
        var sut = new KeonRuntimeClientStub(new KeonRuntimeOptions());
        var result = await sut.CheckHealthAsync();
        Assert.False(result.IsHealthy);
        Assert.Equal(KeonRuntimeMode.Offline, result.Mode);
    }

    [Fact]
    public async Task PolicyCheck_WhenStubAllowAllFalse_ReturnsBlocked()
    {
        var sut = new KeonRuntimeClientStub(new KeonRuntimeOptions { StubAllowAll = false });
        var result = await sut.PolicyCheckAsync(
            new PolicyGateRequest("some text", "test", "biostack-public", "biostack-system"));
        Assert.Equal(PolicyDecision.Blocked, result.Decision);
        Assert.Contains("keon-offline", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PolicyCheck_WhenStubAllowAllTrue_ReturnsAllowed()
    {
        var sut = new KeonRuntimeClientStub(new KeonRuntimeOptions { StubAllowAll = true });
        var result = await sut.PolicyCheckAsync(
            new PolicyGateRequest("some text", "test", "biostack-public", "biostack-system"));
        Assert.Equal(PolicyDecision.Allowed, result.Decision);
    }

    [Fact]
    public async Task IssueReceipt_ReturnsReceiptWithNonEffectingStatus()
    {
        var sut = new KeonRuntimeClientStub(new KeonRuntimeOptions());
        var request = new ReceiptRequest(
            SubjectUri: "biostack://protocol/123",
            TenantId: "biostack-public",
            ActorId: "user-456",
            Decision: "commentary-only",
            InputHash: "abc123",
            EvidenceRefs: [],
            EffectStatus: "non-effecting",
            ReceiptClass: ReceiptClass.ProtocolReviewCompleted);
        var receipt = await sut.IssueReceiptAsync(request);
        Assert.StartsWith("keon://receipt/stub-", receipt.ReceiptUri);
        Assert.Equal("non-effecting", receipt.EffectStatus);
        Assert.Equal(ReceiptClass.ProtocolReviewCompleted, receipt.ReceiptClass);
    }

    [Fact]
    public async Task GetReceipt_ForUnknownUri_ReturnsNull()
    {
        var sut = new KeonRuntimeClientStub(new KeonRuntimeOptions());
        var result = await sut.GetReceiptAsync("keon://receipt/does-not-exist");
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckEvidenceGate_WhenStubAllowAllFalse_ReturnsBlocked()
    {
        var sut = new KeonRuntimeClientStub(new KeonRuntimeOptions { StubAllowAll = false });
        var result = await sut.CheckEvidenceGateAsync(
            new EvidenceGateRequest("bpc-157", "limited", "compound-dossier"));
        Assert.Equal(EvidenceVisibilityTier.Blocked, result.VisibilityTier);
    }

    [Fact]
    public async Task CheckEvidenceGate_WhenStubAllowAllTrue_ReturnsUserFacing()
    {
        var sut = new KeonRuntimeClientStub(new KeonRuntimeOptions { StubAllowAll = true });
        var result = await sut.CheckEvidenceGateAsync(
            new EvidenceGateRequest("bpc-157", "moderate", "compound-dossier"));
        Assert.Equal(EvidenceVisibilityTier.UserFacing, result.VisibilityTier);
    }
}
