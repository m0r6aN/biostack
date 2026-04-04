namespace BioStack.Application.Services;

using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Responses;

public sealed class TimelineService : ITimelineService
{
    private readonly ITimelineEventRepository _timelineRepository;
    private readonly IPersonProfileRepository _profileRepository;

    public TimelineService(
        ITimelineEventRepository timelineRepository,
        IPersonProfileRepository profileRepository)
    {
        _timelineRepository = timelineRepository;
        _profileRepository = profileRepository;
    }

    public async Task<IEnumerable<TimelineEventResponse>> GetTimelineAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(personId, cancellationToken);
        if (profile is null)
            throw new InvalidOperationException($"Profile with ID {personId} not found");

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
