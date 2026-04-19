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

    public async Task<PersonProfile?> GetOwnedByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(profile => profile.Id == id && profile.OwnerId == ownerId, cancellationToken);
    }

    public async Task<IEnumerable<PersonProfile>> GetAllByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(profile => profile.OwnerId == ownerId)
            .OrderByDescending(profile => profile.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

public interface IPersonProfileRepository : IRepository<PersonProfile>
{
    Task<PersonProfile?> GetByIdWithNavigationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PersonProfile?> GetOwnedByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PersonProfile>> GetAllByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
}
