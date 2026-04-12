namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class ProtocolRun
{
    public Guid Id { get; set; }
    public Guid ProtocolId { get; set; }
    public Guid PersonId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public ProtocolRunStatus Status { get; set; } = ProtocolRunStatus.Active;
    public string Notes { get; set; } = string.Empty;

    public Protocol? Protocol { get; set; }
    public PersonProfile? PersonProfile { get; set; }
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}
