namespace BioStack.Infrastructure.Keon;

/// <summary>
/// Fail-closed stub for IKeonRuntimeClient.
/// Used when KeonRuntime:LiveMode is false (default).
///
/// Policy checks are BLOCKED unless StubAllowAll=true (dev-only).
/// Receipt issuance always succeeds (returns a synthetic stub receipt).
/// </summary>
internal sealed class KeonRuntimeClientStub(KeonRuntimeOptions options) : IKeonRuntimeClient
{
    private static readonly PolicyHash StubHash = new("stub-policy-v0", "0.0.0");

    public Task<KeonHealthStatus> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(new KeonHealthStatus(false, KeonRuntimeMode.Offline,
            "Keon Runtime not configured — running in stub mode"));

    public Task<PolicyGateResult> PolicyCheckAsync(PolicyGateRequest request, CancellationToken ct = default)
    {
        if (options.StubAllowAll)
            return Task.FromResult(new PolicyGateResult(
                PolicyDecision.Allowed, null, null, null, StubHash));

        return Task.FromResult(new PolicyGateResult(
            PolicyDecision.Blocked, null, null,
            "keon-offline: Keon Runtime unavailable — fail-closed",
            StubHash));
    }

    public Task<DecisionReceipt> IssueReceiptAsync(ReceiptRequest request, CancellationToken ct = default)
    {
        var uri = $"keon://receipt/stub-{Guid.NewGuid():N}";
        return Task.FromResult(new DecisionReceipt(
            ReceiptUri: uri,
            SubjectUri: request.SubjectUri,
            TenantId: request.TenantId,
            ActorId: request.ActorId,
            TimestampUtc: DateTime.UtcNow,
            Decision: request.Decision,
            PolicyHash: StubHash,
            InputHash: request.InputHash,
            EvidenceRefs: request.EvidenceRefs,
            EffectStatus: request.EffectStatus,
            ReceiptClass: request.ReceiptClass));
    }

    public Task<DecisionReceipt?> GetReceiptAsync(string receiptUri, CancellationToken ct = default)
        => Task.FromResult<DecisionReceipt?>(null);

    public Task<EvidenceGateResult> CheckEvidenceGateAsync(EvidenceGateRequest request, CancellationToken ct = default)
    {
        if (options.StubAllowAll)
            return Task.FromResult(new EvidenceGateResult(
                EvidenceVisibilityTier.UserFacing, null, StubHash));

        return Task.FromResult(new EvidenceGateResult(
            EvidenceVisibilityTier.Blocked,
            "keon-offline: Keon Runtime unavailable — fail-closed",
            StubHash));
    }
}
