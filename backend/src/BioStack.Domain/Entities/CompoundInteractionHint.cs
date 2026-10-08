namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class CompoundInteractionHint
{
    public Guid Id { get; set; }
    public string CompoundA { get; set; } = string.Empty;
    public string CompoundB { get; set; } = string.Empty;
    public InteractionType InteractionType { get; set; } = InteractionType.Neutral;
    public decimal Strength { get; set; }
    public List<string>? MechanismOverlap { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Provenance surface (BIO-PAIRWISE-004 / PW-004-A1). Additive and nullable so existing rows
    // remain valid without backfill. IsSourced is the explicit sourced/unsourced discriminator and
    // defaults to false (unsourced) — the fourteen hand-authored catalog rows carry no citation of
    // any kind and must never be rendered publicly or counted as evidence, per the 2026-09-17 owner
    // ratification. SourceReference is the citation/source identifier once one exists; it carries no
    // meaning while IsSourced is false. The discriminator, not row deletion or a table, decides
    // whether a hint is quarantine-eligible evidence.
    public bool IsSourced { get; set; }
    public string? SourceReference { get; set; }
}
