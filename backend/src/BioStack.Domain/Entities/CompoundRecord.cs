namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class CompoundRecord
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? KnowledgeEntryId { get; set; }
    public string CanonicalName { get; set; } = string.Empty;
    public CompoundCategory Category { get; set; } = CompoundCategory.Unknown;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CompoundStatus Status { get; set; } = CompoundStatus.Planned;
    public string Notes { get; set; } = string.Empty;
    public SourceType SourceType { get; set; } = SourceType.Manual;
    public string Goal { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal? PricePaid { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public PersonProfile? PersonProfile { get; set; }
    public KnowledgeEntry? KnowledgeEntry { get; set; }
}
