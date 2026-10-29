namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public interface ICatalogLookupRepository
{
    Task<CatalogLookup?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<CatalogLookup?> FindByUserAndKeyAsync(Guid userId, string nameKey, CancellationToken ct = default);
    /// <summary>Lookups the person still has to answer or that are still being checked (pending or matched), newest first.</summary>
    Task<IReadOnlyList<CatalogLookup>> ListOpenAsync(Guid userId, Guid personId, CancellationToken ct = default);
    Task<int> CountCreatedSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> ListPendingIdsAsync(DateTime createdBeforeUtc, int take, CancellationToken ct = default);
    Task AddAsync(CatalogLookup lookup, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    /// <summary>Drops tracked entities after a failed save so the caller can re-read.</summary>
    void ClearTracking();
}

public sealed class CatalogLookupRepository : ICatalogLookupRepository
{
    private readonly BioStackDbContext _context;

    public CatalogLookupRepository(BioStackDbContext context) => _context = context;

    public Task<CatalogLookup?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _context.CatalogLookups.FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<CatalogLookup?> FindByUserAndKeyAsync(Guid userId, string nameKey, CancellationToken ct = default)
        => _context.CatalogLookups.FirstOrDefaultAsync(l => l.UserId == userId && l.NameKey == nameKey, ct);

    public async Task<IReadOnlyList<CatalogLookup>> ListOpenAsync(Guid userId, Guid personId, CancellationToken ct = default)
        => await _context.CatalogLookups.AsNoTracking()
            .Where(l => l.UserId == userId && l.PersonId == personId
                && (l.Status == CatalogLookupStatus.Pending || l.Status == CatalogLookupStatus.Matched)
                // An item the person already linked (for example from the edit form) has nothing left to offer.
                && !_context.CompoundRecords.Any(c => c.Id == l.CompoundRecordId && c.CatalogSlug != null))
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<int> CountCreatedSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken ct = default)
        => _context.CatalogLookups.CountAsync(l => l.UserId == userId && l.CreatedAtUtc >= sinceUtc, ct);

    public async Task<IReadOnlyList<Guid>> ListPendingIdsAsync(DateTime createdBeforeUtc, int take, CancellationToken ct = default)
        => await _context.CatalogLookups.AsNoTracking()
            .Where(l => l.Status == CatalogLookupStatus.Pending && l.CreatedAtUtc <= createdBeforeUtc)
            .OrderBy(l => l.CreatedAtUtc)
            .Select(l => l.Id)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(CatalogLookup lookup, CancellationToken ct = default)
        => await _context.CatalogLookups.AddAsync(lookup, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

    public void ClearTracking() => _context.ChangeTracker.Clear();
}
