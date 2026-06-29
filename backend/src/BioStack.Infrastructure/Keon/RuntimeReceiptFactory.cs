namespace BioStack.Infrastructure.Keon;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;

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
    /// </summary>
    Task<DecisionReceipt> IssueAndAppendAsync(ReceiptContext context, CancellationToken ct = default);
}

internal sealed class RuntimeReceiptFactory(
    IKeonRuntimeClient keon,
    ISpineRepository spine) : IRuntimeReceiptFactory
{
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

    private static string HashSeed(string seed)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return "sha256:" + Convert.ToHexStringLower(bytes);
    }
}
