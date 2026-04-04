namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record TimelineEventResponse(
    Guid Id,
    Guid PersonId,
    EventType EventType,
    string Title,
    string Description,
    DateTime OccurredAtUtc,
    Guid? RelatedEntityId,
    string RelatedEntityType
);
