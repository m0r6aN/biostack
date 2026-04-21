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
}
