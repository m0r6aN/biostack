namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class CompoundRecord
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CompoundCategory Category { get; set; } = CompoundCategory.Unknown;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CompoundStatus Status { get; set; } = CompoundStatus.Planned;
    public string Notes { get; set; } = string.Empty;
    public SourceType SourceType { get; set; } = SourceType.Manual;
    public string Goal { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal? PricePaid { get; set; }

    // User-entered structured dose (all optional). Never suggested, prefilled or computed by BioStack.
    public decimal? DoseAmount { get; set; }
    public string? DoseUnit { get; set; }
    public string? DoseFrequency { get; set; }
    public string? DoseTime { get; set; }

    // User-chosen structured frequency (all optional). When FrequencyKind is null the compound is shown
    // daily as before, labelled with DoseFrequency (the user's own free text).
    public string? FrequencyKind { get; set; }
    public int? FrequencyIntervalDays { get; set; }
    /// <summary>Comma-separated weekday numbers, 0 = Sunday .. 6 = Saturday.</summary>
    public string? FrequencyWeekdays { get; set; }
    public int? FrequencyTimesPerWeek { get; set; }
    public DateTime? FrequencyStartDate { get; set; }
    /// <summary>Comma-separated HH:mm times of day (24-hour, the user's own wall-clock times).</summary>
    public string? FrequencyTimesOfDay { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public PersonProfile? PersonProfile { get; set; }
    public ICollection<DoseLogEntry> DoseLogEntries { get; set; } = new List<DoseLogEntry>();
}
