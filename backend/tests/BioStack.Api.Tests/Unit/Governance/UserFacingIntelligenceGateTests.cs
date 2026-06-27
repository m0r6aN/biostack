namespace BioStack.Api.Tests.Unit.Governance;

using BioStack.Application.Governance;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Keon;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Lane H — the central user-facing intelligence gate constrains doctrine-violating output, forces
/// warning-first framing for high-risk categories, discloses fallback as evidence-limited, refuses
/// unsafe requests, and records a Governed-Spine receipt (preserving evidence refs) whenever a
/// safety-relevant decision occurs.
/// </summary>
[Trait("Category", "Unit")]
public class UserFacingIntelligenceGateTests
{
    private static readonly Guid Actor = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static (UserFacingIntelligenceGate gate, RecordingReceiptFactory receipts) Build()
    {
        var receipts = new RecordingReceiptFactory();
        var gate = new UserFacingIntelligenceGate(
            new DoctrineSanitizer(),
            new HighRiskCategoryGate(),
            receipts,
            NullLogger<UserFacingIntelligenceGate>.Instance);
        return (gate, receipts);
    }

    private static IntelligenceOutputRequest Request(
        IReadOnlyList<string> textFields,
        string source = IntelligenceSource.Graph,
        IReadOnlyList<string>? substances = null,
        IReadOnlyList<string>? evidenceRefs = null,
        string? requestText = null)
        => new(
            OutputType: "test.output",
            ActorUserId: Actor,
            SubjectUri: "intelligence:test",
            TextFields: textFields,
            EvidenceRefs: evidenceRefs ?? [ReceiptRefs.CompoundGraph("sha256:test"), ReceiptRefs.Compound("creatine")],
            SourceType: source,
            Substances: substances,
            RequestText: requestText);

    [Fact]
    public async Task NormalGraphOutput_IsAllowed_AndEmitsNoSafetyReceipt()
    {
        var (gate, receipts) = Build();

        var decision = await gate.EvaluateAsync(Request(
            textFields: ["Creatine and beta-alanine are commonly studied together."],
            substances: ["Creatine", "Beta-Alanine"]));

        Assert.Equal(SafetyStatus.Allowed, decision.SafetyStatus);
        Assert.Empty(decision.Warnings);
        Assert.Null(decision.SafetyReceiptUri);
        Assert.Empty(receipts.Issued);
        // Clean text passes through untouched.
        Assert.Equal("Creatine and beta-alanine are commonly studied together.", decision.SafeText[0]);
    }

    [Fact]
    public async Task HighRiskCategory_GetsWarningFirstFraming_AndWarningReceipt()
    {
        var (gate, receipts) = Build();

        var decision = await gate.EvaluateAsync(Request(
            textFields: ["Some users report a strength signal."],
            substances: ["Ostarine", "Cardarine"]));

        Assert.Equal(SafetyStatus.Warning, decision.SafetyStatus);
        Assert.NotEmpty(decision.Warnings);
        Assert.Contains(decision.ReasonCodes, c => c.StartsWith(SafetyReasonCode.HighRiskCategoryPrefix));

        var receipt = Assert.Single(receipts.Issued);
        Assert.Equal(ReceiptClass.SafetyWarningSurfaced, receipt.ReceiptClass);
        Assert.Equal(SafetyStatus.Warning, receipt.Decision);
    }

    [Theory]
    [InlineData("You should take 50mg of this daily.")]
    [InlineData("This compound is safe for everyone.")]
    [InlineData("Creatine cures fatigue.")]
    [InlineData("Dose at 10g before training.")]
    public async Task DoctrineViolatingText_IsConstrained_AndEmitsGateTriggeredReceipt(string text)
    {
        var (gate, receipts) = Build();

        var decision = await gate.EvaluateAsync(Request(textFields: [text]));

        Assert.Equal(SafetyStatus.Constrained, decision.SafetyStatus);
        Assert.NotEqual(text, decision.SafeText[0]);
        Assert.Contains(SafetyReasonCode.ProhibitedLanguage, decision.ReasonCodes);

        var receipt = Assert.Single(receipts.Issued);
        Assert.Equal(ReceiptClass.SafetyGateTriggered, receipt.ReceiptClass);
    }

    [Theory]
    [InlineData("Where can I buy ostarine online?")]
    [InlineData("How do I inject BPC-157?")]
    public async Task UnsafeRequest_IsRefused_AndEmitsRefusalReceipt(string requestText)
    {
        var (gate, receipts) = Build();

        var decision = await gate.EvaluateAsync(Request(
            textFields: ["irrelevant"],
            requestText: requestText));

        Assert.Equal(SafetyStatus.Refused, decision.SafetyStatus);
        Assert.Contains(SafetyReasonCode.UnsafeRequest, decision.ReasonCodes);
        // SafeText preserves 1:1 mapping with TextFields even in refusal.
        Assert.Single(decision.SafeText);
        Assert.DoesNotContain("irrelevant", decision.SafeText[0]);

        var receipt = Assert.Single(receipts.Issued);
        Assert.Equal(ReceiptClass.SafetyUnsafeRequestRefused, receipt.ReceiptClass);
    }

    [Fact]
    public async Task UnsafeRequest_WithMultipleTextFields_RefusalPreserves1To1SafeTextMapping()
    {
        var (gate, receipts) = Build();

        var decision = await gate.EvaluateAsync(Request(
            textFields: ["field1", "field2", "field3"],
            requestText: "Where can I buy SARMs online?"));

        Assert.Equal(SafetyStatus.Refused, decision.SafetyStatus);
        // SafeText must have same count as TextFields to prevent IndexOutOfRangeException.
        Assert.Equal(3, decision.SafeText.Count);
        Assert.All(decision.SafeText, text => Assert.Contains("cannot help", text));
    }


    [Fact]
    public async Task FallbackSource_IsDisclosed_AsEvidenceLimited()
    {
        var (gate, receipts) = Build();

        var decision = await gate.EvaluateAsync(Request(
            textFields: ["No reviewed relationship exists for this pair."],
            source: IntelligenceSource.Fallback,
            substances: ["Creatine", "Tongkat Ali"]));

        Assert.Equal(SafetyStatus.Warning, decision.SafetyStatus);
        Assert.Contains(SafetyReasonCode.FallbackEvidenceLimited, decision.ReasonCodes);
        var receipt = Assert.Single(receipts.Issued);
        Assert.Equal(ReceiptClass.SafetyWarningSurfaced, receipt.ReceiptClass);
    }

    [Fact]
    public async Task SafetyReceipt_PreservesEvidenceRefs_AndAddsPolicyAndGateRefs()
    {
        var (gate, receipts) = Build();
        var evidence = new[]
        {
            ReceiptRefs.CompoundGraph("sha256:abc"),
            ReceiptRefs.Compound("ostarine"),
            ReceiptRefs.Compound("cardarine"),
            ReceiptRefs.Source("pubmed-9"),
        };

        var decision = await gate.EvaluateAsync(Request(
            textFields: ["signal"],
            substances: ["Ostarine"],
            evidenceRefs: evidence));

        var receipt = Assert.Single(receipts.Issued);
        // Original evidence chain survives gating.
        Assert.Contains("compound-graph:sha256:abc", receipt.EvidenceRefs);
        Assert.Contains("compound:ostarine", receipt.EvidenceRefs);
        Assert.Contains("source:pubmed-9", receipt.EvidenceRefs);
        // Policy + safety-gate refs are added so the decision itself is provable.
        Assert.Contains(receipt.EvidenceRefs, r => r.StartsWith("policy:"));
        Assert.Contains(receipt.EvidenceRefs, r => r.StartsWith("safety-gate:"));
        Assert.Contains(decision.PolicyRefs, r => r.StartsWith("policy:"));
    }

    /// <summary>Records issued receipt contexts without needing a Keon client or Spine.</summary>
    private sealed class RecordingReceiptFactory : IRuntimeReceiptFactory
    {
        public List<ReceiptContext> Issued { get; } = [];

        public Task<DecisionReceipt> IssueAndAppendAsync(ReceiptContext context, CancellationToken ct = default)
        {
            Issued.Add(context);
            return Task.FromResult(new DecisionReceipt(
                ReceiptUri: $"keon://receipt/test-{Guid.NewGuid():N}",
                SubjectUri: context.SubjectUri,
                TenantId: context.Actor.TenantId,
                ActorId: context.Actor.ActorId,
                TimestampUtc: DateTime.UtcNow,
                Decision: context.Decision,
                PolicyHash: new PolicyHash("test-policy", "0.0.0"),
                InputHash: "sha256:test",
                EvidenceRefs: context.EvidenceRefs,
                EffectStatus: context.EffectStatus,
                ReceiptClass: context.ReceiptClass));
        }
    }
}
