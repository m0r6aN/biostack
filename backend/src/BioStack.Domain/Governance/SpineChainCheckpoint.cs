namespace BioStack.Domain.Governance;

/// <summary>
/// F3+: a signed snapshot of the Governed Spine chain head.
///
/// The hash chain (F3) is tamper-EVIDENT: a holder with write access to the database can still
/// rewrite every row consistently. A checkpoint captures (sequence, head hash) and signs it with
/// a key that must not live in the same SQLite file. Rewriting the ledger without that key makes
/// signature verification fail — the first step from "evident" toward "proof" when the signing
/// key is held outside the holder's control (server secret, HSM, or offline export).
/// </summary>
public sealed class SpineChainCheckpoint
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Chain head sequence number at checkpoint time.</summary>
    public long SequenceNumber { get; init; }

    /// <summary><see cref="SpineEntry.EntryHash"/> of the head entry.</summary>
    public string HeadEntryHash { get; init; } = null!;

    public DateTime CheckpointedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Who/what produced the checkpoint: <c>local-hmac</c>, <c>server-hmac</c>, or
    /// <c>unsigned-local</c> (no signing key configured — storage only, not an external anchor).
    /// </summary>
    public string Source { get; init; } = "unsigned-local";

    /// <summary>Algorithm used for <see cref="Signature"/> (e.g. <c>HMAC-SHA256</c> or empty).</summary>
    public string SignatureAlgorithm { get; init; } = string.Empty;

    /// <summary>
    /// HMAC over the checkpoint payload, or empty when unsigned.
    /// Format: <c>sha256:hex</c> when algorithm is HMAC-SHA256.
    /// </summary>
    public string Signature { get; init; } = string.Empty;

    /// <summary>Optional operator note (export destination, ticket id, etc.).</summary>
    public string? Note { get; init; }
}
