namespace BioStack.Api.Tests.Unit.Keon;

using System.Text.Json;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Keon;
using Xunit;

/// <summary>
/// Lane G — proves issued receipts carry a real actor, tenant, receipt class, and
/// non-empty evidence refs, and that all of it is persisted to the Governed Spine.
/// </summary>
[Trait("Category", "Unit")]
public class RuntimeReceiptFactoryTests
{
    private static (RuntimeReceiptFactory factory, RecordingSpine spine) Build()
    {
        var spine = new RecordingSpine();
        var factory = new RuntimeReceiptFactory(new EchoKeonClient(), spine);
        return (factory, spine);
    }

    [Fact]
    public async Task IssueAndAppend_UserActor_PersistsRealActorAndConsumerTenant()
    {
        var (factory, spine) = Build();
        var userId = Guid.NewGuid();

        var context = new ReceiptContext(
            ReceiptClass: ReceiptClass.ProtocolReviewCompleted,
            SubjectUri: "protocol:abc/review",
            Actor: ReceiptActor.User(userId),
            EvidenceRefs: [ReceiptRefs.Protocol(Guid.NewGuid())],
            Decision: "commentary-only",
            EffectStatus: "commentary-only",
            InputHashSeed: "seed-1");

        var receipt = await factory.IssueAndAppendAsync(context);

        // Actor is the acting user — not a hardcoded "biostack-system".
        Assert.Equal($"user:{userId}", receipt.ActorId);
        Assert.Equal(ReceiptActor.ConsumerTenant, receipt.TenantId);
        Assert.NotEqual("biostack-system", receipt.ActorId);

        var entry = Assert.Single(spine.Appended);
        Assert.Equal($"user:{userId}", entry.ActorId);
        Assert.Equal(ReceiptActor.ConsumerTenant, entry.TenantId);
        Assert.Equal(ReceiptClass.ProtocolReviewCompleted, entry.ReceiptClass);
    }

    [Fact]
    public async Task IssueAndAppend_SystemActor_UsesSystemTenant()
    {
        var (factory, spine) = Build();

        var context = new ReceiptContext(
            ReceiptClass: ReceiptClass.SourceArtifactPromoted,
            SubjectUri: "staged-artifact:xyz",
            Actor: ReceiptActor.System("transcript-worker"),
            EvidenceRefs: [ReceiptRefs.StagedArtifact("xyz")],
            Decision: "non-effecting",
            EffectStatus: "non-effecting",
            InputHashSeed: "seed-sys");

        var receipt = await factory.IssueAndAppendAsync(context);

        Assert.Equal("system:transcript-worker", receipt.ActorId);
        Assert.Equal(ReceiptActor.SystemTenant, receipt.TenantId);
        Assert.True(ReceiptActor.System("transcript-worker").IsSystem);
        Assert.False(ReceiptActor.User(Guid.NewGuid()).IsSystem);
    }

    [Fact]
    public async Task IssueAndAppend_PersistsEvidenceRefsAndClass_RoundTrips()
    {
        var (factory, spine) = Build();
        var refs = new List<string>
        {
            ReceiptRefs.Compound("bpc-157"),
            ReceiptRefs.Compound("tb-500"),
            ReceiptRefs.CompoundGraph("hash123"),
        };

        await factory.IssueAndAppendAsync(new ReceiptContext(
            ReceiptClass: ReceiptClass.DeliberationStackReviewCompleted,
            SubjectUri: "stack-review:graph-1",
            Actor: ReceiptActor.User(Guid.NewGuid()),
            EvidenceRefs: refs,
            Decision: "commentary-only",
            EffectStatus: "commentary-only",
            InputHashSeed: "graph-1"));

        var entry = Assert.Single(spine.Appended);
        Assert.Equal(ReceiptClass.DeliberationStackReviewCompleted, entry.ReceiptClass);

        var persisted = JsonSerializer.Deserialize<List<string>>(entry.EvidenceRefsJson)!;
        Assert.Equal(refs, persisted);
        Assert.NotEmpty(persisted);
    }

    [Fact]
    public async Task IssueAndAppend_InputHash_IsDeterministicAndPrefixed()
    {
        var (factory1, spine1) = Build();
        var (factory2, spine2) = Build();

        ReceiptContext Ctx() => new(
            ReceiptClass: ReceiptClass.ProtocolReviewCompleted,
            SubjectUri: "protocol:abc/review",
            Actor: ReceiptActor.User(Guid.Empty),
            EvidenceRefs: [ReceiptRefs.Protocol(Guid.Empty)],
            Decision: "commentary-only",
            EffectStatus: "commentary-only",
            InputHashSeed: "stable-seed");

        await factory1.IssueAndAppendAsync(Ctx());
        await factory2.IssueAndAppendAsync(Ctx());

        var h1 = spine1.Appended[0].InputHash;
        var h2 = spine2.Appended[0].InputHash;

        Assert.StartsWith("sha256:", h1);
        Assert.Equal(h1, h2); // deterministic for the same seed
    }

    // ── Fakes ──────────────────────────────────────────────────────────────────

    private sealed class EchoKeonClient : IKeonRuntimeClient
    {
        public Task<KeonHealthStatus> CheckHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(new KeonHealthStatus(true, KeonRuntimeMode.Live, null));

        public Task<PolicyGateResult> PolicyCheckAsync(PolicyGateRequest request, CancellationToken ct = default) =>
            Task.FromResult(new PolicyGateResult(PolicyDecision.Allowed, null, null, null, new PolicyHash("h", "v1")));

        public Task<DecisionReceipt> IssueReceiptAsync(ReceiptRequest request, CancellationToken ct = default) =>
            Task.FromResult(new DecisionReceipt(
                ReceiptUri: $"keon://receipt/test-{Guid.NewGuid():N}",
                SubjectUri: request.SubjectUri,
                TenantId: request.TenantId,
                ActorId: request.ActorId,
                TimestampUtc: DateTime.UtcNow,
                Decision: request.Decision,
                PolicyHash: new PolicyHash("test-policy", "v1"),
                InputHash: request.InputHash,
                EvidenceRefs: request.EvidenceRefs,
                EffectStatus: request.EffectStatus,
                ReceiptClass: request.ReceiptClass));

        public Task<DecisionReceipt?> GetReceiptAsync(string receiptUri, CancellationToken ct = default) =>
            Task.FromResult<DecisionReceipt?>(null);

        public Task<EvidenceGateResult> CheckEvidenceGateAsync(EvidenceGateRequest request, CancellationToken ct = default) =>
            Task.FromResult(new EvidenceGateResult(EvidenceVisibilityTier.UserFacing, null, new PolicyHash("h", "v1")));
    }

    private sealed class RecordingSpine : ISpineRepository
    {
        public List<SpineEntry> Appended { get; } = [];

        public Task<SpineEntry> AppendAsync(SpineEntry entry, CancellationToken ct = default)
        {
            Appended.Add(entry);
            return Task.FromResult(entry);
        }

        public Task<SpineEntry?> GetByReceiptUriAsync(string receiptUri, CancellationToken ct = default) =>
            Task.FromResult(Appended.FirstOrDefault(e => e.ReceiptUri == receiptUri));

        public Task<IReadOnlyList<SpineEntry>> GetBySubjectAsync(string subjectUri, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SpineEntry>>(Appended.Where(e => e.SubjectUri == subjectUri).ToList());

        public Task<IReadOnlyList<SpineEntry>> GetByActorAsync(string actorId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SpineEntry>>(Appended.Where(e => e.ActorId == actorId).ToList());
    }
}
