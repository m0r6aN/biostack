namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class TimelineEvent
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public EventType EventType { get; set; } = EventType.Unknown;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? RelatedEntityId { get; set; }
    public string RelatedEntityType { get; set; } = string.Empty;

    public PersonProfile? PersonProfile { get; set; }
}
