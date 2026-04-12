namespace BioStack.Domain.Entities;

public sealed class ProtocolReviewCompletedEvent
{
    public Guid Id { get; set; }
    public Guid ProtocolId { get; set; }
    public Guid? ProtocolRunId { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    public Protocol? Protocol { get; set; }
    public ProtocolRun? ProtocolRun { get; set; }
}
