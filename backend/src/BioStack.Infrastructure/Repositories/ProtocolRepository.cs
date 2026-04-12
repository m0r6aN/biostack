namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ProtocolRepository : Repository<Protocol>, IProtocolRepository
{
    public ProtocolRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Protocol>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(protocol => protocol.Items)
                .ThenInclude(item => item.CompoundRecord)
            .Where(protocol => protocol.PersonId == personId)
            .OrderByDescending(protocol => protocol.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Protocol?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(protocol => protocol.Items)
                .ThenInclude(item => item.CompoundRecord)
            .FirstOrDefaultAsync(protocol => protocol.Id == id, cancellationToken);
    }
}

public interface IProtocolRepository : IRepository<Protocol>
{
    Task<IEnumerable<Protocol>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<Protocol?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
}
