namespace BioStack.Infrastructure.Keon;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using Microsoft.Extensions.Logging;

/// <summary>
/// Inputs for issuing one governed Decision Receipt (Lane G).
/// </summary>
/// <param name="ReceiptClass">Taxonomy class from <see cref="Keon.ReceiptClass"/>.</param>
/// <param name="SubjectUri">What was governed, e.g. <c>protocol:{id}/review</c>.</param>
/// <param name="Actor">Who initiated the effect (carries actor id + tenant).</param>
/// <param name="EvidenceRefs">
/// Stable refs (built via <see cref="ReceiptRefs"/>) for the evidence/policy/protocol that
/// justified the decision. MUST be non-empty when evidence was available.
/// </param>
/// <param name="Decision">The decision recorded, e.g. <c>commentary-only</c>.</param>
/// <param name="EffectStatus"><c>commentary-only</c> or <c>non-effecting</c>.</param>
/// <param name="InputHashSeed">
/// Deterministic seed hashed into <c>InputHash</c> (e.g. an entity id or canonical payload).
/// </param>
public sealed record ReceiptContext(
    string ReceiptClass,
    string SubjectUri,
    ReceiptActor Actor,
    IReadOnlyList<string> EvidenceRefs,
    string Decision,
    string EffectStatus,
    string InputHashSeed);

/// <summary>
/// Known effect-status values and the doctrine boundary between them.
///
/// Non-effect-bearing receipts record reasoning/commentary/safety decisions — they may degrade
/// when Keon is unavailable. Anything else is effect-bearing and MUST fail closed.
/// The set is an ALLOWLIST: an unrecognised status is treated as effect-bearing.
/// </summary>
public static class ReceiptEffectStatus
{
    public const string NonEffecting = "non-effecting";
    public const string CommentaryOnly = "commentary-only";

    private static readonly HashSet<string> NonEffectBearing =
        new(StringComparer.Ordinal) { NonEffecting, CommentaryOnly };

    public static bool IsNonEffectBearing(string effectStatus)
        => !string.IsNullOrWhiteSpace(effectStatus) && NonEffectBearing.Contains(effectStatus);
}

/// <summary>Outcome of a best-effort (non-effecting) receipt issuance.</summary>
public enum ReceiptIssuanceStatus
{
    /// <summary>Keon issued an authoritative receipt and it was appended to the Spine.</summary>
    Anchored,

    /// <summary>
    /// Keon was unavailable. A clearly-labelled local provenance row was written instead.
    /// NOT Keon-authoritative — distinguishable by URI scheme and policy hash.
    /// </summary>
    Unanchored,

    /// <summary>Keon was unavailable and no local row was written.</summary>
    NotRecorded,
}

/// <summary>Result of <see cref="IRuntimeReceiptFactory.TryIssueAndAppendAsync"/>.</summary>
public sealed record ReceiptIssuanceResult(
    ReceiptIssuanceStatus Status,
    string? ReceiptUri,
    string? DegradationReason)
{
    public bool IsAnchored => Status == ReceiptIssuanceStatus.Anchored;
}

/// <summary>
/// Centralized construction + issuance of Decision Receipts (Lane G).
///
/// Single place that turns a <see cref="ReceiptContext"/> into a Keon-issued
/// <see cref="DecisionReceipt"/> and appends it to the Governed Spine. Callers no longer
/// hand-assemble <see cref="ReceiptRequest"/> / <see cref="SpineEntry"/>, so actor, tenant,
/// receipt class, and evidence refs are wired consistently across receipt families.
/// </summary>
public interface IRuntimeReceiptFactory
{
    /// <summary>
    /// Issue a receipt via Keon and append it to the Spine. Throws
    /// <see cref="KeonRuntimeUnavailableException"/> if Keon cannot issue (caller must halt).
    ///
    /// This is the ONLY correct path for effect-bearing receipts — it fails closed.
    /// </summary>
    Task<DecisionReceipt> IssueAndAppendAsync(ReceiptContext context, CancellationToken ct = default);

    /// <summary>
    /// Best-effort issuance for NON-EFFECTING provenance receipts (safety warnings, constraints,
    /// refusals). Never throws for an unavailable runtime: degrades to an unanchored local Spine
    /// row, or to no record at all, and reports which happened.
    ///
    /// Throws <see cref="InvalidOperationException"/> if handed an effect-bearing context — the
    /// doctrine boundary is enforced here so this cannot be used to launder an effect.
    /// </summary>
    Task<ReceiptIssuanceResult> TryIssueAndAppendAsync(ReceiptContext context, CancellationToken ct = default);
}

internal sealed class RuntimeReceiptFactory(
    IKeonRuntimeClient keon,
    ISpineRepository spine,
    KeonRuntimeOptions options,
    ILogger<RuntimeReceiptFactory> logger) : IRuntimeReceiptFactory
{
    /// <summary>URI scheme for locally-recorded, non-Keon-authoritative provenance rows.</summary>
    private const string UnanchoredUriPrefix = "biostack://unanchored-receipt/";

    /// <summary>Policy hash marker making an unanchored row unmistakable in the Spine.</summary>
    private static readonly PolicyHash UnanchoredPolicyHash = new("unanchored-local", "0.0.0");

    public async Task<DecisionReceipt> IssueAndAppendAsync(ReceiptContext context, CancellationToken ct = default)
    {
        var request = new ReceiptRequest(
            SubjectUri: context.SubjectUri,
            TenantId: context.Actor.TenantId,
            ActorId: context.Actor.ActorId,
            Decision: context.Decision,
            InputHash: HashSeed(context.InputHashSeed),
            EvidenceRefs: context.EvidenceRefs,
            EffectStatus: context.EffectStatus,
            ReceiptClass: context.ReceiptClass);

        var receipt = await keon.IssueReceiptAsync(request, ct);

        await spine.AppendAsync(new SpineEntry
        {
            ReceiptUri = receipt.ReceiptUri,
            SubjectUri = receipt.SubjectUri,
            TenantId = receipt.TenantId,
            ActorId = receipt.ActorId,
            TimestampUtc = receipt.TimestampUtc,
            Decision = receipt.Decision,
            ReceiptClass = string.IsNullOrEmpty(receipt.ReceiptClass) ? context.ReceiptClass : receipt.ReceiptClass,
            PolicyHashValue = receipt.PolicyHash.Value,
            PolicyHashVersion = receipt.PolicyHash.Version,
            InputHash = receipt.InputHash,
            EvidenceRefsJson = JsonSerializer.Serialize(receipt.EvidenceRefs),
            EffectStatus = receipt.EffectStatus,
        }, ct);

        return receipt;
    }

    public async Task<ReceiptIssuanceResult> TryIssueAndAppendAsync(
        ReceiptContext context,
        CancellationToken ct = default)
    {
        // Doctrine boundary: effect-bearing receipts must never take the degrading path.
        if (!ReceiptEffectStatus.IsNonEffectBearing(context.EffectStatus))
        {
            throw new InvalidOperationException(
                $"TryIssueAndAppendAsync accepts non-effecting receipts only. EffectStatus " +
                $"'{context.EffectStatus}' is effect-bearing (or unrecognised) and MUST fail closed " +
                $"via IssueAndAppendAsync.");
        }

        try
        {
            var receipt = await IssueAndAppendAsync(context, ct);
            return new ReceiptIssuanceResult(ReceiptIssuanceStatus.Anchored, receipt.ReceiptUri, null);
        }
        catch (KeonRuntimeUnavailableException ex)
        {
            return await RecordUnanchoredAsync(context, ex.Message, ct);
        }
    }

    /// <summary>
    /// Write a clearly-labelled local provenance row so the safety decision is not lost when Keon
    /// cannot anchor it. Never throws — a provenance failure must not break a safety response.
    /// </summary>
    private async Task<ReceiptIssuanceResult> RecordUnanchoredAsync(
        ReceiptContext context,
        string reason,
        CancellationToken ct)
    {
        if (!options.AllowUnanchoredSafetyReceipts)
        {
            logger.LogError(
                "Keon unavailable and unanchored receipts are disabled — safety decision NOT recorded. "
                + "ReceiptClass={ReceiptClass} Subject={Subject} Reason={Reason}",
                context.ReceiptClass, context.SubjectUri, reason);

            return new ReceiptIssuanceResult(ReceiptIssuanceStatus.NotRecorded, null, reason);
        }

        var uri = UnanchoredUriPrefix + Guid.NewGuid().ToString("N");

        try
        {
            await spine.AppendAsync(new SpineEntry
            {
                ReceiptUri = uri,
                SubjectUri = context.SubjectUri,
                TenantId = context.Actor.TenantId,
                ActorId = context.Actor.ActorId,
                TimestampUtc = DateTime.UtcNow,
                Decision = context.Decision,
                ReceiptClass = context.ReceiptClass,
                PolicyHashValue = UnanchoredPolicyHash.Value,
                PolicyHashVersion = UnanchoredPolicyHash.Version,
                InputHash = HashSeed(context.InputHashSeed),
                EvidenceRefsJson = JsonSerializer.Serialize(context.EvidenceRefs),
                EffectStatus = context.EffectStatus,
            }, ct);

            logger.LogWarning(
                "Keon unavailable — recorded UNANCHORED provenance row (not Keon-authoritative). "
                + "ReceiptUri={ReceiptUri} ReceiptClass={ReceiptClass} Reason={Reason}",
                uri, context.ReceiptClass, reason);

            return new ReceiptIssuanceResult(ReceiptIssuanceStatus.Unanchored, uri, reason);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Keon unavailable AND unanchored Spine append failed — safety decision NOT recorded. "
                + "ReceiptClass={ReceiptClass} Subject={Subject}",
                context.ReceiptClass, context.SubjectUri);

            return new ReceiptIssuanceResult(ReceiptIssuanceStatus.NotRecorded, null, reason);
        }
    }

    private static string HashSeed(string seed)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return "sha256:" + Convert.ToHexStringLower(bytes);
    }
}
