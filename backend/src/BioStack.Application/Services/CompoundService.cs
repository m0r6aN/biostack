namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class CompoundService : ICompoundService
{
    private readonly ICompoundRecordRepository _compoundRepository;
    private readonly ITimelineEventRepository _timelineRepository;
    private readonly IOwnershipGuard _ownershipGuard;
    private readonly IFeatureGate _featureGate;

    public CompoundService(
        ICompoundRecordRepository compoundRepository,
        ITimelineEventRepository timelineRepository,
        IOwnershipGuard ownershipGuard,
        IFeatureGate featureGate)
    {
        _compoundRepository = compoundRepository;
        _timelineRepository = timelineRepository;
        _ownershipGuard = ownershipGuard;
        _featureGate = featureGate;
    }

    public async Task<CompoundResponse> CreateCompoundAsync(Guid personId, CreateCompoundRequest request, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        await EnsureActiveCompoundLimitAsync(personId, request.Status, excludedCompoundId: null, cancellationToken);

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
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var compounds = await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken);
        return compounds.Select(MapToResponse);
    }

    public async Task<CompoundResponse> UpdateCompoundAsync(Guid personId, Guid id, UpdateCompoundRequest request, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var compound = await _compoundRepository.GetByIdAsync(id, cancellationToken);
        if (compound is null || compound.PersonId != personId)
            throw new InvalidOperationException($"Compound with ID {id} not found");
        await EnsureActiveCompoundLimitAsync(personId, request.Status, compound.Id, cancellationToken);

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

    private async Task EnsureActiveCompoundLimitAsync(
        Guid personId,
        CompoundStatus requestedStatus,
        Guid? excludedCompoundId,
        CancellationToken cancellationToken)
    {
        if (requestedStatus != CompoundStatus.Active)
            return;

        var limit = await _featureGate.GetLimitAsync(FeatureCodes.ActiveCompounds, cancellationToken);
        if (limit is null)
            return;

        var activeCount = (await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken))
            .Count(compound => compound.Status == CompoundStatus.Active && compound.Id != excludedCompoundId);
        if (activeCount >= limit)
        {
            throw new FeatureLimitExceededException(
                "observer_active_compound_limit",
                $"Observer includes up to {limit} active compounds. Pause or complete one before adding another, or upgrade to Operator.",
                ProductTier.Observer,
                limit);
        }
    }

    public async Task DeleteCompoundAsync(Guid personId, Guid id, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var compound = await _compoundRepository.GetByIdAsync(id, cancellationToken);
        if (compound is null || compound.PersonId != personId)
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
    Task<CompoundResponse> UpdateCompoundAsync(Guid personId, Guid id, UpdateCompoundRequest request, CancellationToken cancellationToken = default);
    Task DeleteCompoundAsync(Guid personId, Guid id, CancellationToken cancellationToken = default);
}
