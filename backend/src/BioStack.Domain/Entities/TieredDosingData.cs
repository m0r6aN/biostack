namespace BioStack.Domain.Entities;

public sealed class DoseTier
{
    public string StartDose { get; set; } = string.Empty;
    public string Escalation { get; set; } = string.Empty;
    public string MaxDose { get; set; } = string.Empty;
    public List<string> WeeklySchedule { get; set; } = new();
    public string SafetyNotes { get; set; } = string.Empty;
}

public sealed class TieredDosingData
{
    public DoseTier? Beginner { get; set; }
    public DoseTier? Moderate { get; set; }
    public DoseTier? Advanced { get; set; }
}
