namespace BioStack.Domain.Entities;

/// <summary>
/// One amount the user says they actually took of one compound. Always the user's own entry:
/// BioStack never suggests, prefills or computes the amount. Removed with its compound or profile.
/// </summary>
public sealed class DoseLogEntry
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid CompoundRecordId { get; set; }

    /// <summary>The calendar day the user logged this for (their own local date, stored as a UTC midnight date).</summary>
    public DateTime LogDate { get; set; }

    /// <summary>When the user says they took it (UTC).</summary>
    public DateTime TakenAtUtc { get; set; } = DateTime.UtcNow;

    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public PersonProfile? PersonProfile { get; set; }
    public CompoundRecord? CompoundRecord { get; set; }
}
