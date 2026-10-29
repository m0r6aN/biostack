namespace BioStack.Application.Services;

using System.Text.Json;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;

/// <summary>Runs the internal catalog match for one stored lookup. Idempotent: only a pending lookup is processed.</summary>
public interface ICatalogLookupProcessor
{
    Task ProcessAsync(Guid lookupId, CancellationToken ct = default);
}

public sealed class CatalogLookupProcessor : ICatalogLookupProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICatalogLookupRepository _lookups;
    private readonly ICompoundRecordRepository _compounds;
    private readonly ICatalogItemResolver _catalog;
    private readonly INoticeService _notices;

    public CatalogLookupProcessor(
        ICatalogLookupRepository lookups, ICompoundRecordRepository compounds, ICatalogItemResolver catalog, INoticeService notices)
    {
        _lookups = lookups;
        _compounds = compounds;
        _catalog = catalog;
        _notices = notices;
    }

    public static string NoticeKey(Guid lookupId) => $"catalog-lookup:{lookupId:N}";

    public async Task ProcessAsync(Guid lookupId, CancellationToken ct = default)
    {
        var lookup = await _lookups.FindByIdAsync(lookupId, ct);
        if (lookup is null || lookup.Status != CatalogLookupStatus.Pending) return;

        var now = DateTime.UtcNow;
        CompoundRecord? compound = lookup.CompoundRecordId is { } compoundId
            ? await _compounds.GetByIdAsync(compoundId, ct)
            : null;
        if (compound is not null && compound.PersonId != lookup.PersonId) compound = null;

        // The person linked the item themselves while the lookup waited: nothing left to offer.
        if (compound is not null && !string.IsNullOrWhiteSpace(compound.CatalogSlug))
        {
            lookup.Status = CatalogLookupStatus.Linked;
            lookup.CompletedAtUtc = now;
            lookup.LastCheckedAtUtc = now;
            await _lookups.SaveChangesAsync(ct);
            return;
        }

        var catalog = await _catalog.ListAsync(ct);
        var matches = CatalogMatcher.Match(lookup.QueryName, catalog);

        lookup.LastCheckedAtUtc = now;
        if (matches.Count == 0)
        {
            lookup.Status = CatalogLookupStatus.NoMatch;
            lookup.CompletedAtUtc = now;
            lookup.CandidatesJson = null;
            lookup.MatchSlug = lookup.MatchKind = lookup.MatchLabel = null;
            lookup.MatchScore = null;
            await _lookups.SaveChangesAsync(ct);
            return;
        }

        var top = matches[0];
        lookup.Status = CatalogLookupStatus.Matched;
        lookup.MatchSlug = top.Slug;
        lookup.MatchKind = top.Kind;
        lookup.MatchLabel = top.Label;
        lookup.MatchScore = top.Score;
        lookup.CandidatesJson = JsonSerializer.Serialize(matches, JsonOptions);
        await _lookups.SaveChangesAsync(ct);

        // A waiting offer is also left as a note, so a person who left the page sees it at their next sign-in.
        // Only when there is an item to link: an offer with nothing to attach it to would be a dead end.
        if (compound is not null)
        {
            await _notices.UpsertAsync(
                lookup.UserId,
                UserNoticeKind.CatalogMatch,
                NoticeKey(lookup.Id),
                "notice.catalog_match",
                new { queryName = lookup.QueryName, candidateName = top.Name, candidateSlug = top.Slug, label = top.Label },
                "/my-protocol?tab=taking",
                lookup.Id,
                ct);
        }
    }
}
