namespace BioStack.Application.Services;

using System.Text.Json;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

/// <summary>The typed name can't be looked up (too short or long, or not a name). <see cref="Code"/> is the wire reason.</summary>
public sealed class CatalogLookupValidationException : Exception
{
    public CatalogLookupValidationException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}

/// <summary>The person asked about too many new names today.</summary>
public sealed class CatalogLookupLimitException : Exception
{
    public CatalogLookupLimitException(string message) : base(message) { }
}

/// <summary>The lookup can't be answered that way right now (for example nothing to link, or it is not waiting for an answer).</summary>
public sealed class CatalogLookupConflictException : Exception
{
    public CatalogLookupConflictException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}

public interface ICatalogLookupService
{
    Task<CatalogLookupResponse> CreateAsync(Guid profileId, string cleanedName, Guid? compoundId, CancellationToken ct = default);
    /// <summary>Null when the lookup does not exist or belongs to someone else.</summary>
    Task<CatalogLookupResponse?> GetAsync(Guid lookupId, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogLookupResponse>> ListOpenAsync(Guid profileId, CancellationToken ct = default);
    Task<CatalogLookupResponse?> LinkAsync(Guid lookupId, string slug, CancellationToken ct = default);
    Task<CatalogLookupResponse?> DeclineAsync(Guid lookupId, CancellationToken ct = default);
}

/// <summary>
/// The user-owned side of "is this already in BioStack?". Phase 3 is internal only: the name is matched against the public
/// catalog on the server and goes nowhere else. Linking is always the person's own confirmed action.
/// </summary>
public sealed class CatalogLookupService : ICatalogLookupService
{
    public const int MaxNewLookupsPerDay = 30;
    public const int MinNameLength = 2;
    public const int MaxNameLength = 60;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICatalogLookupRepository _lookups;
    private readonly ICompoundRecordRepository _compounds;
    private readonly IOwnershipGuard _ownership;
    private readonly ICompoundService _compoundService;
    private readonly INoticeService _notices;
    private readonly ICatalogLookupQueue _queue;

    public CatalogLookupService(
        ICatalogLookupRepository lookups,
        ICompoundRecordRepository compounds,
        IOwnershipGuard ownership,
        ICompoundService compoundService,
        INoticeService notices,
        ICatalogLookupQueue queue)
    {
        _lookups = lookups;
        _compounds = compounds;
        _ownership = ownership;
        _compoundService = compoundService;
        _notices = notices;
        _queue = queue;
    }

    public async Task<CatalogLookupResponse> CreateAsync(Guid profileId, string cleanedName, Guid? compoundId, CancellationToken ct = default)
    {
        await _ownership.EnsureProfileOwnedAsync(profileId, ct);
        var userId = _ownership.CurrentUserId;

        var name = (cleanedName ?? string.Empty).Trim();
        var key = CatalogMatcher.Key(name);
        if (name.Length < MinNameLength || name.Length > MaxNameLength || key.Length < MinNameLength || !name.Any(char.IsLetter))
            throw new CatalogLookupValidationException("invalid_name", "Enter the name of one item, between 2 and 60 characters.");

        if (compoundId is { } id)
        {
            var compound = await _compounds.GetByIdAsync(id, ct);
            if (compound is null || compound.PersonId != profileId)
                throw new InvalidOperationException($"Compound with ID {id} not found");
        }

        var existing = await _lookups.FindByUserAndKeyAsync(userId, key, ct);
        if (existing is not null)
        {
            if (compoundId is { } attachTo && existing.CompoundRecordId != attachTo)
            {
                // The same name asked about again for another of the person's items: the internal check is free and local,
                // so it simply runs again for the new item. (Phases with outside lookups must not do this.)
                existing.CompoundRecordId = attachTo;
                existing.PersonId = profileId;
                existing.Status = CatalogLookupStatus.Pending;
                existing.CompletedAtUtc = null;
                existing.CandidatesJson = null;
                existing.MatchSlug = existing.MatchKind = existing.MatchLabel = null;
                existing.MatchScore = null;
                await _lookups.SaveChangesAsync(ct);
                await _notices.ResolveAsync(userId, CatalogLookupProcessor.NoticeKey(existing.Id), ct);
                _queue.TryEnqueue(existing.Id);
            }

            return Map(existing);
        }

        if (await _lookups.CountCreatedSinceAsync(userId, DateTime.UtcNow.AddHours(-24), ct) >= MaxNewLookupsPerDay)
            throw new CatalogLookupLimitException("You have checked a lot of new names today. Try again tomorrow; your items are saved either way.");

        var lookup = new CatalogLookup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PersonId = profileId,
            CompoundRecordId = compoundId,
            NameKey = key,
            QueryName = name,
            Status = CatalogLookupStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _lookups.AddAsync(lookup, ct);
        try
        {
            await _lookups.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two requests for the same new name raced: the unique (user, name) key kept one. Return the winner.
            _lookups.ClearTracking();
            var winner = await _lookups.FindByUserAndKeyAsync(userId, key, ct);
            if (winner is null) throw;
            return Map(winner);
        }

        _queue.TryEnqueue(lookup.Id);
        return Map(lookup);
    }

    public async Task<CatalogLookupResponse?> GetAsync(Guid lookupId, CancellationToken ct = default)
    {
        var lookup = await OwnedAsync(lookupId, ct);
        return lookup is null ? null : Map(lookup);
    }

    public async Task<IReadOnlyList<CatalogLookupResponse>> ListOpenAsync(Guid profileId, CancellationToken ct = default)
    {
        await _ownership.EnsureProfileOwnedAsync(profileId, ct);
        return (await _lookups.ListOpenAsync(_ownership.CurrentUserId, profileId, ct)).Select(Map).ToList();
    }

    public async Task<CatalogLookupResponse?> LinkAsync(Guid lookupId, string slug, CancellationToken ct = default)
    {
        var lookup = await OwnedAsync(lookupId, ct);
        if (lookup is null) return null;
        if (lookup.Status == CatalogLookupStatus.Linked) return Map(lookup);
        if (lookup.Status != CatalogLookupStatus.Matched)
            throw new CatalogLookupConflictException("not_waiting", "This lookup is not waiting for an answer.");

        var wanted = CatalogItemKey(slug);
        var offered = Candidates(lookup).FirstOrDefault(c => CatalogItemKey(c.Slug) == wanted);
        if (offered is null)
            throw new CatalogLookupConflictException("not_offered", "That item was not one of the matches offered.");

        if (lookup.CompoundRecordId is not { } compoundId)
            throw new CatalogLookupConflictException("no_item", "The item this lookup was for is no longer in your list.");

        // The service validates the slug against the live catalog and derives the state itself.
        await _compoundService.LinkCatalogAsync(lookup.PersonId, compoundId, offered.Slug, ct);

        lookup.Status = CatalogLookupStatus.Linked;
        lookup.CompletedAtUtc = DateTime.UtcNow;
        await _lookups.SaveChangesAsync(ct);
        await _notices.ResolveAsync(lookup.UserId, CatalogLookupProcessor.NoticeKey(lookup.Id), ct);
        return Map(lookup);
    }

    public async Task<CatalogLookupResponse?> DeclineAsync(Guid lookupId, CancellationToken ct = default)
    {
        var lookup = await OwnedAsync(lookupId, ct);
        if (lookup is null) return null;
        if (lookup.Status == CatalogLookupStatus.Declined) return Map(lookup);
        if (lookup.Status != CatalogLookupStatus.Matched)
            throw new CatalogLookupConflictException("not_waiting", "This lookup is not waiting for an answer.");

        lookup.Status = CatalogLookupStatus.Declined;
        lookup.CompletedAtUtc = DateTime.UtcNow;
        await _lookups.SaveChangesAsync(ct);
        await _notices.ResolveAsync(lookup.UserId, CatalogLookupProcessor.NoticeKey(lookup.Id), ct);
        return Map(lookup);
    }

    private async Task<CatalogLookup?> OwnedAsync(Guid lookupId, CancellationToken ct)
    {
        var lookup = await _lookups.FindByIdAsync(lookupId, ct);
        return lookup is not null && lookup.UserId == _ownership.CurrentUserId ? lookup : null;
    }

    private static string CatalogItemKey(string? slug) => (slug ?? string.Empty).Trim().ToLowerInvariant();

    private static IReadOnlyList<CatalogMatch> Candidates(CatalogLookup lookup)
    {
        if (string.IsNullOrWhiteSpace(lookup.CandidatesJson)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<CatalogMatch>>(lookup.CandidatesJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static CatalogLookupResponse Map(CatalogLookup lookup) => new(
        lookup.Id,
        lookup.PersonId,
        lookup.CompoundRecordId,
        lookup.QueryName,
        lookup.Status,
        Candidates(lookup).Select(c => new CatalogCandidateResponse(c.Slug, c.Name, c.Kind, c.Label, c.Reason)).ToList(),
        lookup.CreatedAtUtc,
        lookup.CompletedAtUtc);
}
