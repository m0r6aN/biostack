namespace BioStack.Domain.Entities;

public sealed class ProtocolItem
{
    public Guid Id { get; set; }
    public Guid ProtocolId { get; set; }
    public Guid CompoundRecordId { get; set; }
    public Guid? CalculatorResultId { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Protocol? Protocol { get; set; }
    public CompoundRecord? CompoundRecord { get; set; }
}
