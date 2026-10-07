namespace BioStack.Domain.Governance;

public sealed class SpineEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ReceiptUri { get; init; } = null!;   // "keon://receipt/{id}"
    public string SubjectUri { get; init; } = null!;   // what was governed
    public string TenantId { get; init; } = null!;
    public string ActorId { get; init; } = null!;
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string Decision { get; init; } = null!;     // "commentary-only" | "non-effecting"
    // Taxonomy class — see Keon.ReceiptClass. Defaults to the "legacy.unclassified" sentinel so
    // historical/unset rows are distinguishable from genuinely classified receipts.
    public string ReceiptClass { get; init; } = "legacy.unclassified";
    public string PolicyHashValue { get; init; } = null!;
    public string PolicyHashVersion { get; init; } = null!;
    public string InputHash { get; init; } = null!;
    public string EvidenceRefsJson { get; init; } = "[]";  // JSON array of strings
    public string EffectStatus { get; init; } = null!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // ── Tamper-evidence (F3) ────────────────────────────────────────────────
    // Append-only was previously enforced only by an application-layer existence check plus a
    // unique index on ReceiptUri. That prevents duplicates; it does not detect an out-of-band
    // UPDATE or DELETE. In a local-first product the database file sits on the user's — or a
    // provider's — own disk, so "Receipt Supremacy" rested on a ledger its holder could silently
    // rewrite. These three fields make the Spine a hash chain: every entry commits to its
    // predecessor, so altering or removing any row invalidates every row after it.

    /// <summary>
    /// Position in the chain. Genesis is 0. Unique, so two concurrent appends cannot both
    /// claim the same slot — the loser fails at the database and retries.
    /// </summary>
    public long SequenceNumber { get; init; }

    /// <summary>
    /// <see cref="EntryHash"/> of the preceding entry, or
    /// <see cref="SpineChain.GenesisPreviousHash"/> for the first entry. Unique, so an entry can
    /// have at most one successor — a forked chain cannot be written.
    /// </summary>
    public string PreviousEntryHash { get; init; } = SpineChain.GenesisPreviousHash;

    /// <summary>
    /// SHA-256 over this entry's governed fields AND <see cref="PreviousEntryHash"/>.
    /// Recomputed during verification; a mismatch means the row was altered after the fact.
    /// </summary>
    public string EntryHash { get; init; } = null!;
}
