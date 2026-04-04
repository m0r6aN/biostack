namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class CheckInService : ICheckInService
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly IPersonProfileRepository _profileRepository;
    private readonly ITimelineEventRepository _timelineRepository;

    public CheckInService(
        ICheckInRepository checkInRepository,
        IPersonProfileRepository profileRepository,
        ITimelineEventRepository timelineRepository)
    {
        _checkInRepository = checkInRepository;
        _profileRepository = profileRepository;
        _timelineRepository = timelineRepository;
    }

    public async Task<CheckInResponse> CreateCheckInAsync(Guid personId, CreateCheckInRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(personId, cancellationToken);
        if (profile is null)
            throw new InvalidOperationException($"Profile with ID {personId} not found");

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Date = request.Date,
            Weight = request.Weight,
            SleepQuality = request.SleepQuality,
            Energy = request.Energy,
            Appetite = request.Appetite,
            Recovery = request.Recovery,
            Focus = request.Focus,
            ThoughtClarity = request.ThoughtClarity,
            SkinQuality = request.SkinQuality,
            DigestiveHealth = request.DigestiveHealth,
            Strength = request.Strength,
            Endurance = request.Endurance,
            JointPain = request.JointPain,
            Eyesight = request.Eyesight,
            SideEffects = request.SideEffects,
            PhotoUrls = request.PhotoUrls != null ? string.Join(";", request.PhotoUrls) : string.Empty,
            GiSymptoms = request.GiSymptoms,
            Mood = request.Mood,
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _checkInRepository.AddAsync(checkIn, cancellationToken);

        var timelineEvent = new TimelineEvent
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            EventType = EventType.CheckInCreated,
            Title = "Check-in recorded",
            Description = $"Weight: {request.Weight} kg, Energy: {request.Energy}/10, Sleep: {request.SleepQuality}/10" + 
                          (string.IsNullOrWhiteSpace(request.SideEffects) ? "" : $", Side Effects: {request.SideEffects}"),
            OccurredAtUtc = request.Date,
            RelatedEntityId = checkIn.Id,
            RelatedEntityType = "CheckIn"
        };
        await _timelineRepository.AddAsync(timelineEvent, cancellationToken);

        await _checkInRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(checkIn);
    }

    public async Task<IEnumerable<CheckInResponse>> GetCheckInsByProfileAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var checkIns = await _checkInRepository.GetByPersonIdAsync(personId, cancellationToken);
        return checkIns.Select(MapToResponse);
    }

    private static CheckInResponse MapToResponse(CheckIn checkIn)
    {
        return new CheckInResponse(
            checkIn.Id,
            checkIn.PersonId,
            checkIn.Date,
            checkIn.Weight,
            checkIn.SleepQuality,
            checkIn.Energy,
            checkIn.Appetite,
            checkIn.Recovery,
            checkIn.Focus,
            checkIn.ThoughtClarity,
            checkIn.SkinQuality,
            checkIn.DigestiveHealth,
            checkIn.Strength,
            checkIn.Endurance,
            checkIn.JointPain,
            checkIn.Eyesight,
            checkIn.SideEffects,
            string.IsNullOrEmpty(checkIn.PhotoUrls) ? Array.Empty<string>() : checkIn.PhotoUrls.Split(';', StringSplitOptions.RemoveEmptyEntries),
            checkIn.GiSymptoms,
            checkIn.Mood,
            checkIn.Notes,
            checkIn.CreatedAtUtc
        );
    }
}

public interface ICheckInService
{
    Task<CheckInResponse> CreateCheckInAsync(Guid personId, CreateCheckInRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInResponse>> GetCheckInsByProfileAsync(Guid personId, CancellationToken cancellationToken = default);
}
