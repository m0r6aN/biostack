namespace BioStack.Application.Services;

using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Responses;

public sealed class TimelineService : ITimelineService
{
    private readonly ITimelineEventRepository _timelineRepository;
    private readonly IOwnershipGuard _ownershipGuard;

    public TimelineService(
        ITimelineEventRepository timelineRepository,
        IOwnershipGuard ownershipGuard)
    {
        _timelineRepository = timelineRepository;
        _ownershipGuard = ownershipGuard;
    }

    public async Task<IEnumerable<TimelineEventResponse>> GetTimelineAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);

        var events = await _timelineRepository.GetByPersonIdAsync(personId, cancellationToken);
        return events.Select(MapToResponse);
    }

    private static TimelineEventResponse MapToResponse(BioStack.Domain.Entities.TimelineEvent timelineEvent)
    {
        return new TimelineEventResponse(
            timelineEvent.Id,
            timelineEvent.PersonId,
            timelineEvent.EventType,
            timelineEvent.Title,
            timelineEvent.Description,
            timelineEvent.OccurredAtUtc,
            timelineEvent.RelatedEntityId,
            timelineEvent.RelatedEntityType
        );
    }
}

public interface ITimelineService
{
    Task<IEnumerable<TimelineEventResponse>> GetTimelineAsync(Guid personId, CancellationToken cancellationToken = default);
}
