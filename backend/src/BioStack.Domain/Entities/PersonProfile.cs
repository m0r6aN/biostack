namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class PersonProfile
{
    /// <summary>The AppUser who owns this profile. Null for legacy/seed data.</summary>
    public Guid? OwnerId { get; set; }
    public AppUser? Owner { get; set; }
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Sex Sex { get; set; } = Sex.Unspecified;
    public int? Age { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public decimal Weight { get; set; }
    public string GoalSummary { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CompoundRecord> Compounds { get; set; } = new List<CompoundRecord>();
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
    public ICollection<ProtocolPhase> ProtocolPhases { get; set; } = new List<ProtocolPhase>();
    public ICollection<TimelineEvent> TimelineEvents { get; set; } = new List<TimelineEvent>();
}
