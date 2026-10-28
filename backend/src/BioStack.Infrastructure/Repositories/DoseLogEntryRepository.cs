namespace BioStack.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;

public sealed class DoseLogEntryRepository : Repository<DoseLogEntry>, IDoseLogEntryRepository
{
    public DoseLogEntryRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<DoseLogEntry>> GetByPersonIdAsync(
        Guid personId,
        DateTime? fromLogDate,
        DateTime? toLogDate,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(entry => entry.CompoundRecord).Where(entry => entry.PersonId == personId);
        if (fromLogDate is { } from)
        {
            query = query.Where(entry => entry.LogDate >= from);
        }

        if (toLogDate is { } to)
        {
            query = query.Where(entry => entry.LogDate <= to);
        }

        // Fetch then order in memory: SQLite cannot ORDER BY DateTime offsets and the set is bounded by limit.
        var rows = await query.ToListAsync(cancellationToken);
        return rows
            .OrderByDescending(entry => entry.LogDate)
            .ThenByDescending(entry => entry.TakenAtUtc)
            .Take(limit)
            .ToList();
    }
}

public interface IDoseLogEntryRepository : IRepository<DoseLogEntry>
{
    Task<IReadOnlyList<DoseLogEntry>> GetByPersonIdAsync(
        Guid personId,
        DateTime? fromLogDate,
        DateTime? toLogDate,
        int limit,
        CancellationToken cancellationToken = default);
}
