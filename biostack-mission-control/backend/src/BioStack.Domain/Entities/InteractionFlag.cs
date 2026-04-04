namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class InteractionFlag
{
    public Guid Id { get; set; }
    public List<string> CompoundNames { get; set; } = new();
    public OverlapType OverlapType { get; set; } = OverlapType.Unknown;
    public string PathwayTag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EvidenceConfidence { get; set; } = "Unknown";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
