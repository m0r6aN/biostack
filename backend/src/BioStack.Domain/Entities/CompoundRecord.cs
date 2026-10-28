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

    // Catalog link and verification (Everything I'm Taking, spec v5 section 4.1). All three are set by the
    // server only: CatalogSlug when the user picks a catalog record, VerificationState and CategorySource
    // derived from that link. A client can never send them. NULL (legacy rows) reads as: catalog when
    // CatalogSlug is set, otherwise unverified; CategorySource NULL reads as "user".
    public string? CatalogSlug { get; set; }
    /// <summary>"catalog" or "unverified" (later phases add "provisional").</summary>
    public string? VerificationState { get; set; }
    /// <summary>"catalog" (Category copied from the linked catalog record) or "user" (display only, never trusted by the cap).</summary>
    public string? CategorySource { get; set; }
    /// <summary>Per-item provider-summary toggle (default on).</summary>
    public bool IncludeInProviderSummary { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public PersonProfile? PersonProfile { get; set; }
    public ICollection<DoseLogEntry> DoseLogEntries { get; set; } = new List<DoseLogEntry>();
}
