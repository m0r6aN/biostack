namespace BioStack.Infrastructure.Keon;

// ── Policy Gate ───────────────────────────────────────────────────────────────

public sealed record PolicyHash(string Value, string Version);

/// <summary>
/// Possible outcomes of a policy classification check.
/// These map to Keon Runtime's classification API.
/// </summary>
public enum PolicyDecision
{
    Allowed,
    AllowedWithDisclaimer,
    RewriteRequired,
    Blocked,
    EscalateToProviderReview
}

public sealed record PolicyGateRequest(
    string Text,
    string Context,
    string TenantId,
    string ActorId);

public sealed record PolicyGateResult(
    PolicyDecision Decision,
    string? DisclaimerText,
    string? RewrittenText,
    string? BlockReason,
    PolicyHash PolicyHash);

// ── Decision Receipts ─────────────────────────────────────────────────────────

/// <summary>
/// Immutable record of a governed effect or commentary-only classification.
/// Receipt URIs follow the pattern: keon://receipt/{id}
/// </summary>
public sealed record DecisionReceipt(
    string ReceiptUri,
    string SubjectUri,
    string TenantId,
    string ActorId,
    DateTime TimestampUtc,
    string Decision,
    PolicyHash PolicyHash,
    string InputHash,
    IReadOnlyList<string> EvidenceRefs,
    string EffectStatus,            // "commentary-only" | "non-effecting"
    string ReceiptClass = "");     // taxonomy class, e.g. "deliberation.stack-review.completed"

public sealed record ReceiptRequest(
    string SubjectUri,
    string TenantId,
    string ActorId,
    string Decision,
    string InputHash,
    IReadOnlyList<string> EvidenceRefs,
    string EffectStatus,
    string ReceiptClass = "");     // see <see cref="ReceiptClass"/>

// ── Evidence Gate ─────────────────────────────────────────────────────────────

public enum EvidenceVisibilityTier
{
    UserFacing,
    LimitedFraming,
    GapsOnly,
    Blocked
}

public sealed record EvidenceGateRequest(
    string CompoundSlug,
    string EvidenceTier,
    string TargetSurface);

public sealed record EvidenceGateResult(
    EvidenceVisibilityTier VisibilityTier,
    string? RequiredFraming,
    PolicyHash PolicyHash);

// ── Health ────────────────────────────────────────────────────────────────────

public enum KeonRuntimeMode { Live, Degraded, Offline }

public sealed record KeonHealthStatus(
    bool IsHealthy,
    KeonRuntimeMode Mode,
    string? Message);
