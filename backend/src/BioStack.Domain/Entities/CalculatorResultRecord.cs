namespace BioStack.Domain.Entities;

public sealed class CalculatorResultRecord
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid? CompoundRecordId { get; set; }
    public string CalculatorKind { get; set; } = string.Empty;
    public string InputsJson { get; set; } = "{}";
    public string OutputsJson { get; set; } = "{}";
    public string Unit { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string DisplaySummary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public PersonProfile? PersonProfile { get; set; }
    public CompoundRecord? CompoundRecord { get; set; }
}
