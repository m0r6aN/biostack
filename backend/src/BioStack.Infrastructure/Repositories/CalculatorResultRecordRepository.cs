namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class CalculatorResultRecordRepository : Repository<CalculatorResultRecord>, ICalculatorResultRecordRepository
{
    public CalculatorResultRecordRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CalculatorResultRecord>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(result => result.PersonId == personId)
            .OrderByDescending(result => result.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

public interface ICalculatorResultRecordRepository : IRepository<CalculatorResultRecord>
{
    Task<IEnumerable<CalculatorResultRecord>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
