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
}
