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

    public async Task<IEnumerable<Protocol>> GetLineageAsync(Protocol protocol, CancellationToken cancellationToken = default)
    {
        var rootId = protocol.OriginProtocolId ?? protocol.Id;

        return await _dbSet
            .Include(version => version.Items)
                .ThenInclude(item => item.CompoundRecord)
            .Where(version => version.Id == rootId || version.OriginProtocolId == rootId)
            .OrderBy(version => version.Version)
            .ThenBy(version => version.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetMaxVersionInLineageAsync(Protocol protocol, CancellationToken cancellationToken = default)
    {
        var rootId = protocol.OriginProtocolId ?? protocol.Id;

        return await _dbSet
            .Where(version => version.Id == rootId || version.OriginProtocolId == rootId)
            .MaxAsync(version => (int?)version.Version, cancellationToken) ?? protocol.Version;
    }
}

public interface IProtocolRepository : IRepository<Protocol>
{
    Task<IEnumerable<Protocol>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<Protocol?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Protocol>> GetLineageAsync(Protocol protocol, CancellationToken cancellationToken = default);
    Task<int> GetMaxVersionInLineageAsync(Protocol protocol, CancellationToken cancellationToken = default);
}
