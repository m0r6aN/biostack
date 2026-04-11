namespace BioStack.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;

public sealed class ProtocolPhaseRepository : Repository<ProtocolPhase>, IProtocolPhaseRepository
{
    public ProtocolPhaseRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProtocolPhase>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.PersonId == personId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }
}

public interface IProtocolPhaseRepository : IRepository<ProtocolPhase>
{
    Task<IEnumerable<ProtocolPhase>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
