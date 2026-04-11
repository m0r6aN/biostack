namespace BioStack.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;

public sealed class TimelineEventRepository : Repository<TimelineEvent>, ITimelineEventRepository
{
    public TimelineEventRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TimelineEvent>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.PersonId == personId)
            .OrderByDescending(t => t.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }
}

public interface ITimelineEventRepository : IRepository<TimelineEvent>
{
    Task<IEnumerable<TimelineEvent>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
