namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using BioStack.Infrastructure.Knowledge;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class CompoundService : ICompoundService
{
    private readonly ICompoundRecordRepository _compoundRepository;
    private readonly IPersonProfileRepository _profileRepository;
    private readonly ITimelineEventRepository _timelineRepository;
    private readonly ICalculatorResultRecordRepository _calculatorResultRepository;
    private readonly IKnowledgeSource _knowledgeSource;

    public CompoundService(
        ICompoundRecordRepository compoundRepository,
        IPersonProfileRepository profileRepository,
        ITimelineEventRepository timelineRepository,
        ICalculatorResultRecordRepository calculatorResultRepository,
        IKnowledgeSource knowledgeSource)
    {
        _compoundRepository = compoundRepository;
        _profileRepository = profileRepository;
        _timelineRepository = timelineRepository;
        _calculatorResultRepository = calculatorResultRepository;
        _knowledgeSource = knowledgeSource;
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

        var knowledgeEntry = await ResolveKnowledgeEntryAsync(request.KnowledgeEntryId, request.Name, cancellationToken);
        if (knowledgeEntry is not null)
        {
            compound.KnowledgeEntryId = knowledgeEntry.Id;
            compound.CanonicalName = knowledgeEntry.CanonicalName;
            compound.Name = knowledgeEntry.CanonicalName;
            compound.Category = knowledgeEntry.Classification;
        }

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

        if (request.CalculatorResultId.HasValue)
        {
            var calculatorResult = await _calculatorResultRepository.GetByIdAsync(request.CalculatorResultId.Value, cancellationToken);
            if (calculatorResult is not null && calculatorResult.PersonId == personId)
            {
                calculatorResult.CompoundRecordId = compound.Id;
                calculatorResult.UpdatedAtUtc = DateTime.UtcNow;
                await _calculatorResultRepository.UpdateAsync(calculatorResult, cancellationToken);
            }
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

        var knowledgeEntry = await ResolveKnowledgeEntryAsync(request.KnowledgeEntryId, request.Name, cancellationToken);
        if (knowledgeEntry is not null)
        {
            compound.KnowledgeEntryId = knowledgeEntry.Id;
            compound.CanonicalName = knowledgeEntry.CanonicalName;
            compound.Name = knowledgeEntry.CanonicalName;
            compound.Category = knowledgeEntry.Classification;
        }
        else
        {
            compound.KnowledgeEntryId = null;
            compound.CanonicalName = string.Empty;
        }

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

    private async Task<KnowledgeEntry?> ResolveKnowledgeEntryAsync(Guid? knowledgeEntryId, string name, CancellationToken cancellationToken)
    {
        var entries = await _knowledgeSource.GetAllCompoundsAsync(cancellationToken);

        if (knowledgeEntryId.HasValue)
        {
            var byId = entries.FirstOrDefault(entry => entry.Id == knowledgeEntryId.Value);
            if (byId is not null)
            {
                return byId;
            }
        }

        return entries.FirstOrDefault(entry => IsKnowledgeMatch(entry, name));
    }

    private static bool IsKnowledgeMatch(KnowledgeEntry entry, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return string.Equals(entry.CanonicalName, name, StringComparison.OrdinalIgnoreCase) ||
            entry.Aliases.Any(alias => string.Equals(alias, name, StringComparison.OrdinalIgnoreCase));
    }

    private static CompoundResponse MapToResponse(CompoundRecord compound)
    {
        var canonicalName = !string.IsNullOrWhiteSpace(compound.CanonicalName)
            ? compound.CanonicalName
            : compound.KnowledgeEntry?.CanonicalName ?? string.Empty;

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
            compound.KnowledgeEntryId.HasValue,
            compound.KnowledgeEntryId,
            canonicalName,
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
