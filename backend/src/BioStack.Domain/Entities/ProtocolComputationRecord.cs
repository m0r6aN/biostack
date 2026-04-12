namespace BioStack.Domain.Entities;

public sealed class ProtocolComputationRecord
{
    public Guid Id { get; set; }
    public Guid ProtocolId { get; set; }
    public Guid? ProtocolRunId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string InputSnapshot { get; set; } = string.Empty;
    public string OutputResult { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public Protocol? Protocol { get; set; }
    public ProtocolRun? ProtocolRun { get; set; }
}
