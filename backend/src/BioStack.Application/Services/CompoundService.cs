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
    private readonly ICatalogItemResolver _catalog;

    public CompoundService(
        ICompoundRecordRepository compoundRepository,
        ITimelineEventRepository timelineRepository,
        IOwnershipGuard ownershipGuard,
        IFeatureGate featureGate,
        ICatalogItemResolver catalog)
    {
        _compoundRepository = compoundRepository;
        _timelineRepository = timelineRepository;
        _ownershipGuard = ownershipGuard;
        _featureGate = featureGate;
        _catalog = catalog;
    }

    public async Task<CompoundResponse> CreateCompoundAsync(Guid personId, CreateCompoundRequest request, CancellationToken cancellationToken = default)
    {
        var name = ValidateName(request.Name);
        var dose = ValidateDose(request.DoseAmount, request.DoseUnit, request.DoseFrequency, request.DoseTime);
        var frequency = ValidateFrequency(request.FrequencyKind, request.FrequencyIntervalDays, request.FrequencyWeekdays,
            request.FrequencyTimesPerWeek, request.FrequencyStartDate, request.FrequencyTimesOfDay);
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var link = await ResolveLinkAsync(request.CatalogSlug, request.Category, current: null, cancellationToken);

        var compound = new CompoundRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Name = name,
            Category = link.Category,
            CatalogSlug = link.Slug,
            VerificationState = link.State,
            CategorySource = link.CategorySource,
            IncludeInProviderSummary = request.IncludeInProviderSummary ?? true,
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

        // The cap is checked once, with the state the server just derived (never a client-sent category).
        await EnsureActiveCompoundLimitAsync(personId, compound, excludedCompoundId: null, cancellationToken);

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

    public async Task<TakingResponse> GetTakingAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var compounds = (await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken)).ToList();
        var catalog = await _catalog.ListAsync(cancellationToken);
        var bySlug = catalog.ToDictionary(item => item.Slug, StringComparer.Ordinal);

        // name/alias -> catalog items, exact normalized match only.
        var byName = new Dictionary<string, List<(CatalogItem Item, string MatchedOn)>>(StringComparer.Ordinal);
        void Index(string? text, CatalogItem item, string matchedOn)
        {
            var key = NormalizeName(text);
            if (key.Length == 0) return;
            if (!byName.TryGetValue(key, out var list)) byName[key] = list = new();
            if (list.All(entry => entry.Item.Slug != item.Slug)) list.Add((item, matchedOn));
        }
        foreach (var item in catalog)
        {
            Index(item.Name, item, "name");
            foreach (var alias in item.Aliases) Index(alias, item, "alias");
        }

        bool IsCounted(CompoundRecord compound) => compound.Status == CompoundStatus.Active && !ObserverCapRules.IsExempt(
            compound,
            !string.IsNullOrWhiteSpace(compound.CatalogSlug) && bySlug.TryGetValue(KnowledgeCatalogItemResolver.Normalize(compound.CatalogSlug), out var live)
                ? live.Category
                : compound.Category);

        var items = compounds
            .Select(compound =>
            {
                var ended = compound.Status is CompoundStatus.Completed or CompoundStatus.Discontinued;
                IReadOnlyList<CatalogSuggestionResponse> suggestions = string.IsNullOrWhiteSpace(compound.CatalogSlug) && !ended
                    && byName.TryGetValue(NormalizeName(compound.Name), out var matches)
                    ? matches.Select(match => new CatalogSuggestionResponse(match.Item.Slug, match.Item.Name, match.Item.Category, match.MatchedOn)).ToList()
                    : [];
                return new TakingItemResponse(MapToResponse(compound), IsCounted(compound), suggestions);
            })
            .ToList();

        var limit = await _featureGate.GetLimitAsync(FeatureCodes.ActiveCompounds, cancellationToken);
        return new TakingResponse(items, new TakingCapResponse(limit, items.Count(item => item.CountsTowardCap)));
    }

    /// <summary>
    /// Links one of the user's own items to a catalog record the person just confirmed. The state and category come from the
    /// catalog, never from the client. Linking cannot raise the Observer count (an unverified item already counts), so the
    /// cap is not checked here.
    /// </summary>
    public async Task<CompoundResponse> LinkCatalogAsync(Guid personId, Guid id, string catalogSlug, CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(personId, cancellationToken);
        var compound = await _compoundRepository.GetByIdAsync(id, cancellationToken);
        if (compound is null || compound.PersonId != personId)
            throw new InvalidOperationException($"Compound with ID {id} not found");

        var link = await ResolveLinkAsync(catalogSlug, compound.Category, compound, cancellationToken);
        compound.Category = link.Category;
        compound.CatalogSlug = link.Slug;
        compound.VerificationState = link.State;
        compound.CategorySource = link.CategorySource;
        compound.UpdatedAtUtc = DateTime.UtcNow;

        await _compoundRepository.UpdateAsync(compound, cancellationToken);
        await _compoundRepository.SaveChangesAsync(cancellationToken);
        return MapToResponse(compound);
    }

    private static string NormalizeName(string? text) =>
        string.Join(' ', (text ?? string.Empty).Trim().ToLowerInvariant().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));

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
        var link = await ResolveLinkAsync(request.CatalogSlug, request.Category, compound, cancellationToken);

        compound.Name = name;
        compound.Category = link.Category;
        compound.CatalogSlug = link.Slug;
        compound.VerificationState = link.State;
        compound.CategorySource = link.CategorySource;
        compound.IncludeInProviderSummary = request.IncludeInProviderSummary ?? compound.IncludeInProviderSummary;
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

        // Create, edit and reactivation all pass through here with the state the server derived.
        await EnsureActiveCompoundLimitAsync(personId, compound, compound.Id, cancellationToken);

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

    private readonly record struct CatalogLink(string? Slug, string? State, string? CategorySource, CompoundCategory Category);

    // Server-side derivation of catalog link, verification state and category source. The client never
    // supplies the state or the source; a client category is used only for an unlinked (unverified) item,
    // where it is display-only and never trusted by the cap. When the item is linked the category comes
    // from the catalog record. On update, a null slug keeps the existing link and "" removes it.
    private async Task<CatalogLink> ResolveLinkAsync(
        string? requestedSlug, CompoundCategory requestedCategory, CompoundRecord? current, CancellationToken cancellationToken)
    {
        if (requestedSlug is null && current is not null && !string.IsNullOrWhiteSpace(current.CatalogSlug))
        {
            return new CatalogLink(current.CatalogSlug, ObserverCapRules.StateCatalog, ObserverCapRules.SourceCatalog, current.Category);
        }

        if (string.IsNullOrWhiteSpace(requestedSlug))
        {
            return new CatalogLink(null, ObserverCapRules.StateUnverified, ObserverCapRules.SourceUser, requestedCategory);
        }

        var item = await _catalog.FindBySlugAsync(requestedSlug, cancellationToken);
        if (item is null)
        {
            throw new DoseValidationException("catalogSlug", "That catalog item was not found.");
        }

        return new CatalogLink(item.Slug, ObserverCapRules.StateCatalog, ObserverCapRules.SourceCatalog, item.Category);
    }

    private async Task EnsureActiveCompoundLimitAsync(
        Guid personId,
        CompoundRecord candidate,
        Guid? excludedCompoundId,
        CancellationToken cancellationToken)
    {
        if (candidate.Status != CompoundStatus.Active)
            return;

        var limit = await _featureGate.GetLimitAsync(FeatureCodes.ActiveCompounds, cancellationToken);
        if (limit is null)
            return;

        var active = (await _compoundRepository.GetByPersonIdAsync(personId, cancellationToken))
            .Where(compound => compound.Status == CompoundStatus.Active && compound.Id != excludedCompoundId)
            .ToList();

        // Live catalog categories for linked rows; a link whose record can no longer be resolved falls back
        // to the category stored when it was linked (set by the server from the catalog, never by the client).
        var linkedSlugs = active.Append(candidate)
            .Where(compound => !string.IsNullOrWhiteSpace(compound.CatalogSlug))
            .Select(compound => compound.CatalogSlug!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var catalog = linkedSlugs.Count == 0
            ? new Dictionary<string, CatalogItem>()
            : (await _catalog.FindBySlugsAsync(linkedSlugs, cancellationToken)).ToDictionary(pair => pair.Key, pair => pair.Value);

        bool Exempt(CompoundRecord compound) => ObserverCapRules.IsExempt(
            compound,
            !string.IsNullOrWhiteSpace(compound.CatalogSlug)
                && catalog.TryGetValue(KnowledgeCatalogItemResolver.Normalize(compound.CatalogSlug), out var live)
                ? live.Category
                : compound.Category);

        if (Exempt(candidate))
            return;

        var counted = active.Count(compound => !Exempt(compound));
        if (counted >= limit)
        {
            throw new FeatureLimitExceededException(
                "observer_active_compound_limit",
                $"Observer includes up to {limit} active research or unconfirmed items. Confirmed supplements, vitamins, minerals and amino acids don't count. Pause or complete one before adding another, or upgrade to Operator.",
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
            UserFrequency.ParseTimes(compound.FrequencyTimesOfDay),
            compound.CatalogSlug,
            ObserverCapRules.EffectiveState(compound.CatalogSlug, compound.VerificationState),
            ObserverCapRules.EffectiveCategorySource(compound.CategorySource),
            compound.IncludeInProviderSummary
        );
    }
}

public interface ICompoundService
{
    Task<CompoundResponse> CreateCompoundAsync(Guid personId, CreateCompoundRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CompoundResponse>> GetCompoundsByProfileAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<CompoundResponse> UpdateCompoundAsync(Guid personId, Guid id, UpdateCompoundRequest request, CancellationToken cancellationToken = default);
    Task<TakingResponse> GetTakingAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<CompoundResponse> LinkCatalogAsync(Guid personId, Guid id, string catalogSlug, CancellationToken cancellationToken = default);
    Task DeleteCompoundAsync(Guid personId, Guid id, CancellationToken cancellationToken = default);
}

/// <summary>A user-entered field failed validation (a dose field, or <c>catalogSlug</c>). <see cref="Field"/> is the camelCase wire field name.</summary>
public sealed class DoseValidationException : Exception
{
    public DoseValidationException(string field, string message) : base(message)
    {
        Field = field;
    }

    public string Field { get; }
}
