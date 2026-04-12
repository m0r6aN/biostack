namespace BioStack.Domain.Entities;

public sealed class Protocol
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public Guid? ParentProtocolId { get; set; }
    public Guid? OriginProtocolId { get; set; }
    public Guid? EvolvedFromRunId { get; set; }
    public bool IsDraft { get; set; }
    public string EvolutionContext { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public PersonProfile? PersonProfile { get; set; }
    public Protocol? ParentProtocol { get; set; }
    public Protocol? OriginProtocol { get; set; }
    public ProtocolRun? EvolvedFromRun { get; set; }
    public ICollection<ProtocolItem> Items { get; set; } = new List<ProtocolItem>();
    public ICollection<ProtocolRun> Runs { get; set; } = new List<ProtocolRun>();
    public ICollection<Protocol> ChildVersions { get; set; } = new List<Protocol>();
}
