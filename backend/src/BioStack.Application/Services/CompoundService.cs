namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class CompoundService : ICompoundService
{
    private readonly ICompoundRecordRepository _compoundRepository;
    private readonly IPersonProfileRepository _profileRepository;
    private readonly ITimelineEventRepository _timelineRepository;

    public CompoundService(
        ICompoundRecordRepository compoundRepository,
        IPersonProfileRepository profileRepository,
        ITimelineEventRepository timelineRepository)
    {
        _compoundRepository = compoundRepository;
        _profileRepository = profileRepository;
        _timelineRepository = timelineRepository;
    }

    public async Task<CompoundResponse> CreateCompoundAsync(Guid personId, CreateCompoundRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(personId, cancellationToken);
        if (profile is null)
            throw new InvalidOperationException($"Profile with ID {personId} not found");

        var compound = new CompoundRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Name = request.Name,
            Category = request.Category,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            Notes = request.Notes,
            SourceType = request.SourceType,
            Goal = request.Goal,
            Source = request.Source,
            PricePaid = request.PricePaid,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _compoundRepository.AddAsync(compound, cancellationToken);

        if (request.StartDate.HasValue)
        {
            var startEvent = new TimelineEvent
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                EventType = EventType.CompoundStarted,
                Title = $"Started {request.Name}",
                Description = request.Notes,
                OccurredAtUtc = request.StartDate.Value,
                RelatedEntityId = compound.Id,
                RelatedEntityType = "CompoundRecord"
            };
            await _timelineRepository.AddAsync(startEvent, cancellationToken);
        }

        await _compoundRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(compound);
    }

    public async Task<IEnumerable<CompoundResponse>> GetCompoundsByProfileAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var compounds = await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken);
        return compounds.Select(MapToResponse);
    }

    public async Task<CompoundResponse> UpdateCompoundAsync(Guid id, UpdateCompoundRequest request, CancellationToken cancellationToken = default)
    {
        var compound = await _compoundRepository.GetByIdAsync(id, cancellationToken);
        if (compound is null)
            throw new InvalidOperationException($"Compound with ID {id} not found");

        compound.Name = request.Name;
        compound.Category = request.Category;
        compound.StartDate = request.StartDate;
        compound.EndDate = request.EndDate;
        compound.Status = request.Status;
        compound.Notes = request.Notes;
        compound.SourceType = request.SourceType;
        compound.Goal = request.Goal;
        compound.Source = request.Source;
        compound.PricePaid = request.PricePaid;
        compound.UpdatedAtUtc = DateTime.UtcNow;

        await _compoundRepository.UpdateAsync(compound, cancellationToken);
        await _compoundRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(compound);
    }

    public async Task DeleteCompoundAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var compound = await _compoundRepository.GetByIdAsync(id, cancellationToken);
        if (compound is null)
            throw new InvalidOperationException($"Compound with ID {id} not found");

        await _compoundRepository.DeleteAsync(compound, cancellationToken);
        await _compoundRepository.SaveChangesAsync(cancellationToken);
    }

    private static CompoundResponse MapToResponse(CompoundRecord compound)
    {
        return new CompoundResponse(
            compound.Id,
            compound.PersonId,
            compound.Name,
            compound.Category,
            compound.StartDate,
            compound.EndDate,
            compound.Status,
            compound.Notes,
            compound.SourceType,
            compound.CreatedAtUtc,
            compound.UpdatedAtUtc,
            compound.Goal,
            compound.Source,
            compound.PricePaid
        );
    }
}

public interface ICompoundService
{
    Task<CompoundResponse> CreateCompoundAsync(Guid personId, CreateCompoundRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CompoundResponse>> GetCompoundsByProfileAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<CompoundResponse> UpdateCompoundAsync(Guid id, UpdateCompoundRequest request, CancellationToken cancellationToken = default);
    Task DeleteCompoundAsync(Guid id, CancellationToken cancellationToken = default);
}
