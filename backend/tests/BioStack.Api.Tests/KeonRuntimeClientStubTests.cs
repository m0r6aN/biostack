namespace BioStack.Api.Tests;

using BioStack.Infrastructure.Keon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task IssueReceipt_WhenRuntimeOffline_FailsClosedWithoutKeonAuthorityClaim()
    {
        var sut = new KeonRuntimeClientStub(new KeonRuntimeOptions { StubAllowAll = true });
        var request = new ReceiptRequest(
            SubjectUri: "biostack://protocol/123",
            TenantId: "biostack-public",
            ActorId: "user-456",
            Decision: "commentary-only",
            InputHash: "abc123",
            EvidenceRefs: [],
            EffectStatus: "non-effecting",
            ReceiptClass: ReceiptClass.ProtocolReviewCompleted);

        var error = await Assert.ThrowsAsync<KeonRuntimeUnavailableException>(
            () => sut.IssueReceiptAsync(request));

        Assert.Contains("no Decision Receipt was issued", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("keon://", error.Message, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void AddKeonRuntime_WhenLiveModeDisabled_ResolvesProductionStubNeverTestClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{KeonRuntimeOptions.SectionName}:LiveMode"] = "false",
                [$"{KeonRuntimeOptions.SectionName}:StubAllowAll"] = "true",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddKeonRuntime(configuration);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IKeonRuntimeClient>();
        Assert.IsType<KeonRuntimeClientStub>(client);
        Assert.IsNotType<TestKeonRuntimeClient>(client);
        Assert.NotEqual(typeof(TestKeonRuntimeClient).Assembly, client.GetType().Assembly);
    }
}
