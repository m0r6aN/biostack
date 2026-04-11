namespace BioStack.Domain.Entities;

public sealed class CheckIn
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal Weight { get; set; }
    public int SleepQuality { get; set; }
    public int Energy { get; set; }
    public int Appetite { get; set; }
    public int Recovery { get; set; }
    public int? Focus { get; set; }
    public int? ThoughtClarity { get; set; }
    public int? SkinQuality { get; set; }
    public int? DigestiveHealth { get; set; }
    public int? Strength { get; set; }
    public int? Endurance { get; set; }
    public int? JointPain { get; set; }
    public int? Eyesight { get; set; }
    public string SideEffects { get; set; } = string.Empty;
    public string PhotoUrls { get; set; } = string.Empty; // Semicolon-separated URLs
    public string GiSymptoms { get; set; } = string.Empty;
    public string Mood { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public PersonProfile? PersonProfile { get; set; }
}
