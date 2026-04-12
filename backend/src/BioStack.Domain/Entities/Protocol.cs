namespace BioStack.Domain.Entities;

public sealed class Protocol
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public PersonProfile? PersonProfile { get; set; }
    public ICollection<ProtocolItem> Items { get; set; } = new List<ProtocolItem>();
}
