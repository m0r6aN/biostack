namespace BioStack.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;

public sealed class PersonProfileRepository : Repository<PersonProfile>, IPersonProfileRepository
{
    public PersonProfileRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<PersonProfile?> GetByIdWithNavigationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Compounds)
            .Include(p => p.CheckIns)
            .Include(p => p.ProtocolPhases)
            .Include(p => p.TimelineEvents)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}

public interface IPersonProfileRepository : IRepository<PersonProfile>
{
    Task<PersonProfile?> GetByIdWithNavigationAsync(Guid id, CancellationToken cancellationToken = default);
}
