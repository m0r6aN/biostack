namespace BioStack.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;

public sealed class CheckInRepository : Repository<CheckIn>, ICheckInRepository
{
    public CheckInRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CheckIn>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.PersonId == personId)
            .OrderByDescending(c => c.Date)
            .ToListAsync(cancellationToken);
    }
}

public interface ICheckInRepository : IRepository<CheckIn>
{
    Task<IEnumerable<CheckIn>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
