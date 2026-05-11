namespace BioStack.Infrastructure.Keon;

/// <summary>
/// Typed client for the Keon Runtime governance layer.
/// All BioStack callers interact through this interface only.
/// DOCTRINE: On any failure, implementations must fail-closed (deny) for
/// effect-bearing operations and return a degraded envelope for commentary.
/// </summary>
public interface IKeonRuntimeClient
{
    Task<KeonHealthStatus> CheckHealthAsync(CancellationToken ct = default);

    /// <summary>
    /// Classify and policy-check a text fragment before rendering.
    /// On Keon failure: returns Blocked with BlockReason "keon-offline" (fail-closed).
    /// </summary>
    Task<PolicyGateResult> PolicyCheckAsync(PolicyGateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Persist a Decision Receipt on the Governed Spine.
    /// On Keon failure: throws KeonRuntimeUnavailableException (caller must halt the effect).
    /// </summary>
    Task<DecisionReceipt> IssueReceiptAsync(ReceiptRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieve a Decision Receipt by URI.
    /// Returns null if the receipt does not exist (not found is not a failure).
    /// </summary>
    Task<DecisionReceipt?> GetReceiptAsync(string receiptUri, CancellationToken ct = default);

    /// <summary>
    /// Check whether a compound's evidence tier permits display on a given surface.
    /// On Keon failure: returns Blocked (fail-closed).
    /// </summary>
    Task<EvidenceGateResult> CheckEvidenceGateAsync(EvidenceGateRequest request, CancellationToken ct = default);
}

/// <summary>
/// Thrown when Keon Runtime is unavailable and an effect-bearing operation cannot proceed.
/// Callers that receive this exception must NOT proceed with the effect.
/// </summary>
public sealed class KeonRuntimeUnavailableException(string message) : Exception(message);
