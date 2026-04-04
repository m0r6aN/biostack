namespace BioStack.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;

public sealed class CompoundRecordRepository : Repository<CompoundRecord>, ICompoundRecordRepository
{
    public CompoundRecordRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CompoundRecord>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.PersonId == personId)
            .ToListAsync(cancellationToken);
    }
}

public interface ICompoundRecordRepository : IRepository<CompoundRecord>
{
    Task<IEnumerable<CompoundRecord>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
