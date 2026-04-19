namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class ProtocolPhaseService : IProtocolPhaseService
{
    private readonly IProtocolPhaseRepository _phaseRepository;
    private readonly ITimelineEventRepository _timelineRepository;
    private readonly IOwnershipGuard _ownershipGuard;

    public ProtocolPhaseService(
        IProtocolPhaseRepository phaseRepository,
        ITimelineEventRepository timelineRepository,
        IOwnershipGuard ownershipGuard)
    {
        _phaseRepository = phaseRepository;
        _timelineRepository = timelineRepository;
        _ownershipGuard = ownershipGuard;
    }

    public async Task<ProtocolPhaseResponse> CreatePhaseAsync(Guid personId, CreateProtocolPhaseRequest request, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);

        var phase = new ProtocolPhase
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _phaseRepository.AddAsync(phase, cancellationToken);

        if (request.StartDate.HasValue)
        {
            var startEvent = new TimelineEvent
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                EventType = EventType.ProtocolPhaseStarted,
                Title = $"Started protocol phase: {request.Name}",
                Description = request.Notes,
                OccurredAtUtc = request.StartDate.Value,
                RelatedEntityId = phase.Id,
                RelatedEntityType = "ProtocolPhase"
            };
            await _timelineRepository.AddAsync(startEvent, cancellationToken);
        }

        await _phaseRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(phase);
    }

    public async Task<IEnumerable<ProtocolPhaseResponse>> GetPhasesByProfileAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var phases = await _phaseRepository.GetByPersonIdAsync(personId, cancellationToken);
        return phases.Select(MapToResponse);
    }

    private static ProtocolPhaseResponse MapToResponse(ProtocolPhase phase)
    {
        return new ProtocolPhaseResponse(
            phase.Id,
            phase.PersonId,
            phase.Name,
            phase.StartDate,
            phase.EndDate,
            phase.Notes,
            phase.CreatedAtUtc,
            phase.UpdatedAtUtc
        );
    }
}

public interface IProtocolPhaseService
{
    Task<ProtocolPhaseResponse> CreatePhaseAsync(Guid personId, CreateProtocolPhaseRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProtocolPhaseResponse>> GetPhasesByProfileAsync(Guid personId, CancellationToken cancellationToken = default);
}
