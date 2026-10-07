namespace BioStack.Application.Tests.Governance;

using BioStack.Application.Governance;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Keon;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// F1 regression: a safety-relevant response must survive an unavailable Keon runtime.
///
/// Before this guard, the default configuration (KeonRuntime:LiveMode=false) bound the
/// fail-closed stub, whose IssueReceiptAsync throws. Because a receipt is issued only for
/// Warning / Constrained / Refused outcomes, the endpoint returned HTTP 500 precisely when
/// the safety gate did its job — and succeeded on benign input.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Contract", "GuidanceContentContract.v1")]
public sealed class UserFacingIntelligenceGateDegradationTests
{
    /// <summary>Worst-case factory: faults on every call, including the degrading path.</summary>
    private sealed class ThrowingReceiptFactory : IRuntimeReceiptFactory
    {
        public Task<DecisionReceipt> IssueAndAppendAsync(ReceiptContext context, CancellationToken ct = default)
            => Task.FromException<DecisionReceipt>(
                new KeonRuntimeUnavailableException("Keon Runtime unavailable — no Decision Receipt was issued."));

        public Task<ReceiptIssuanceResult> TryIssueAndAppendAsync(ReceiptContext context, CancellationToken ct = default)
            => Task.FromException<ReceiptIssuanceResult>(
                new KeonRuntimeUnavailableException("Keon Runtime unavailable — no Decision Receipt was issued."));
    }

    private static UserFacingIntelligenceGate CreateSut()
        => new(
            new DoctrineSanitizer(),
            new HighRiskCategoryGate(),
            new ThrowingReceiptFactory(),
            NullLogger<UserFacingIntelligenceGate>.Instance);

    private static IntelligenceOutputRequest HighRiskRequest(string substance)
        => new(
            OutputType: "intelligence.compatibility",
            ActorUserId: Guid.NewGuid(),
            SubjectUri: $"compound:{substance}",
            TextFields: ["Observed alongside other recovery compounds."],
            EvidenceRefs: ["compound:" + substance],
            SourceType: IntelligenceSource.Graph,
            Substances: [substance]);

    [Theory]
    [InlineData("BPC-157")]
    [InlineData("RAD-140")]
    [InlineData("tirzepatide")]
    public async Task HighRisk_output_survives_unavailable_keon_runtime(string substance)
    {
        var sut = CreateSut();

        var decision = await sut.EvaluateAsync(HighRiskRequest(substance));

        // The safety warning must still reach the user; only the receipt is lost.
        Assert.NotEqual(SafetyStatus.Allowed, decision.SafetyStatus);
        Assert.NotEmpty(decision.Warnings);
        Assert.Null(decision.SafetyReceiptUri);
    }

    [Fact]
    public async Task Fallback_sourced_output_survives_unavailable_keon_runtime()
    {
        var sut = CreateSut();

        var decision = await sut.EvaluateAsync(new IntelligenceOutputRequest(
            OutputType: "intelligence.compatibility",
            ActorUserId: Guid.NewGuid(),
            SubjectUri: "compound:creatine",
            TextFields: ["General observational context."],
            EvidenceRefs: [],
            SourceType: IntelligenceSource.Fallback));

        Assert.Equal(SafetyStatus.Warning, decision.SafetyStatus);
        Assert.Contains(SafetyReasonCode.FallbackEvidenceLimited, decision.ReasonCodes);
        Assert.Null(decision.SafetyReceiptUri);
    }

    [Fact]
    public async Task Prohibited_language_is_still_constrained_when_keon_is_unavailable()
    {
        var sut = CreateSut();

        var decision = await sut.EvaluateAsync(new IntelligenceOutputRequest(
            OutputType: "intelligence.compatibility",
            ActorUserId: Guid.NewGuid(),
            SubjectUri: "compound:creatine",
            TextFields: ["You should take 5 mg daily."],
            EvidenceRefs: [],
            SourceType: IntelligenceSource.Graph));

        Assert.Equal(SafetyStatus.Constrained, decision.SafetyStatus);
        Assert.Contains(SafetyReasonCode.ProhibitedLanguage, decision.ReasonCodes);
        Assert.DoesNotContain("You should", decision.SafeText[0], StringComparison.OrdinalIgnoreCase);
    }
}
