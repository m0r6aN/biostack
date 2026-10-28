namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Domain.ValueObjects;
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
        var name = ValidateName(request.Name);
        var dose = ValidateDose(request.DoseAmount, request.DoseUnit, request.DoseFrequency, request.DoseTime);
        var frequency = ValidateFrequency(request.FrequencyKind, request.FrequencyIntervalDays, request.FrequencyWeekdays,
            request.FrequencyTimesPerWeek, request.FrequencyStartDate, request.FrequencyTimesOfDay);
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        await EnsureActiveCompoundLimitAsync(personId, request.Status, excludedCompoundId: null, cancellationToken);

        var compound = new CompoundRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Name = name,
            Category = request.Category,
            StartDate = ToUtc(request.StartDate),
            EndDate = ToUtc(request.EndDate),
            Status = request.Status,
            Notes = request.Notes,
            SourceType = request.SourceType,
            Goal = request.Goal,
            Source = request.Source,
            PricePaid = request.PricePaid,
            DoseAmount = dose.Amount,
            DoseUnit = dose.Unit,
            DoseFrequency = dose.Frequency,
            DoseTime = dose.Time,
            FrequencyKind = frequency?.Kind,
            FrequencyIntervalDays = frequency?.IntervalDays,
            FrequencyWeekdays = UserFrequency.FormatWeekdays(frequency?.Weekdays ?? []),
            FrequencyTimesPerWeek = frequency?.TimesPerWeek,
            FrequencyStartDate = frequency?.StartDate,
            FrequencyTimesOfDay = UserFrequency.FormatTimes(frequency?.TimesOfDay ?? []),
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
                Title = $"Started {name}",
                Description = request.Notes,
                OccurredAtUtc = compound.StartDate!.Value,
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
        var name = ValidateName(request.Name);
        var dose = ValidateDose(request.DoseAmount, request.DoseUnit, request.DoseFrequency, request.DoseTime);
        var frequency = ValidateFrequency(request.FrequencyKind, request.FrequencyIntervalDays, request.FrequencyWeekdays,
            request.FrequencyTimesPerWeek, request.FrequencyStartDate, request.FrequencyTimesOfDay);
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var compound = await _compoundRepository.GetByIdAsync(id, cancellationToken);
        if (compound is null || compound.PersonId != personId)
            throw new InvalidOperationException($"Compound with ID {id} not found");
        await EnsureActiveCompoundLimitAsync(personId, request.Status, compound.Id, cancellationToken);

        compound.Name = name;
        compound.Category = request.Category;
        compound.StartDate = ToUtc(request.StartDate);
        compound.EndDate = ToUtc(request.EndDate);
        compound.Status = request.Status;
        compound.Notes = request.Notes;
        compound.SourceType = request.SourceType;
        compound.Goal = request.Goal;
        compound.Source = request.Source;
        compound.PricePaid = request.PricePaid;
        compound.DoseAmount = dose.Amount;
        compound.DoseUnit = dose.Unit;
        compound.DoseFrequency = dose.Frequency;
        compound.DoseTime = dose.Time;
        compound.FrequencyKind = frequency?.Kind;
        compound.FrequencyIntervalDays = frequency?.IntervalDays;
        compound.FrequencyWeekdays = UserFrequency.FormatWeekdays(frequency?.Weekdays ?? []);
        compound.FrequencyTimesPerWeek = frequency?.TimesPerWeek;
        compound.FrequencyStartDate = frequency?.StartDate;
        compound.FrequencyTimesOfDay = UserFrequency.FormatTimes(frequency?.TimesOfDay ?? []);
        compound.UpdatedAtUtc = DateTime.UtcNow;

        await _compoundRepository.UpdateAsync(compound, cancellationToken);
        await _compoundRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(compound);
    }

    // Guards against a blank or whitespace-only compound name reaching storage
    // (e.g. an accidental Enter key mid-form). Returns the trimmed name.
    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Compound name is required.", nameof(name));
        }

        return name.Trim();
    }

    // The dose is the user's own entry. This only checks that it is a storable, positive number in an
    // allowed unit; there are intentionally no range checks or warnings (nothing here may read as a
    // recommended dose). Throws DoseValidationException, mapped to a 400 by the endpoints.
    private static (decimal? Amount, string? Unit, string? Frequency, string? Time) ValidateDose(
        decimal? amount, string? unit, string? frequency, string? time)
    {
        var error = UserDose.Validate(
            amount, unit, frequency, time,
            out var normalizedAmount, out var normalizedUnit, out var normalizedFrequency, out var normalizedTime);
        if (error is { } failure)
        {
            throw new DoseValidationException(failure.Field, failure.Message);
        }

        return (normalizedAmount, normalizedUnit, normalizedFrequency, normalizedTime);
    }

    // The frequency is the user's own choice. Validation only checks the combination makes sense.
    private static FrequencySpec? ValidateFrequency(
        string? kind, int? intervalDays, IReadOnlyList<int>? weekdays, int? timesPerWeek, DateTime? startDate, IReadOnlyList<string>? timesOfDay)
    {
        var error = UserFrequency.Validate(
            new FrequencyInput(kind, intervalDays, weekdays, timesPerWeek, startDate, timesOfDay), out var spec);
        if (error is { } failure)
        {
            throw new DoseValidationException(failure.Field, failure.Message);
        }

        return spec;
    }

    // Calendar-only values remain midnight; unspecified timestamps are interpreted
    // as UTC so PostgreSQL timestamptz receives an explicit UTC value.
    private static DateTime? ToUtc(DateTime? value) => value?.Kind switch
    {
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        _ => value
    };

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
            compound.PricePaid,
            compound.DoseAmount,
            compound.DoseUnit,
            compound.DoseFrequency,
            compound.DoseTime,
            compound.FrequencyKind,
            compound.FrequencyIntervalDays,
            UserFrequency.ParseWeekdays(compound.FrequencyWeekdays),
            compound.FrequencyTimesPerWeek,
            compound.FrequencyStartDate,
            UserFrequency.ParseTimes(compound.FrequencyTimesOfDay)
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

/// <summary>A user-entered dose field failed validation. <see cref="Field"/> is the camelCase wire field name.</summary>
public sealed class DoseValidationException : Exception
{
    public DoseValidationException(string field, string message) : base(message)
    {
        Field = field;
    }

    public string Field { get; }
}
