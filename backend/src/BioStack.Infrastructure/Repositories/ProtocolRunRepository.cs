namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ProtocolRunRepository : Repository<ProtocolRun>, IProtocolRunRepository
{
    public ProtocolRunRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<ProtocolRun?> GetActiveByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(run => run.Protocol)
            .FirstOrDefaultAsync(run => run.PersonId == personId && run.Status == ProtocolRunStatus.Active, cancellationToken);
    }

    public async Task<ProtocolRun?> GetActiveByProtocolIdAsync(Guid protocolId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(run => run.Protocol)
            .FirstOrDefaultAsync(run => run.ProtocolId == protocolId && run.Status == ProtocolRunStatus.Active, cancellationToken);
    }

    public async Task<ProtocolRun?> GetLatestByProtocolIdAsync(Guid protocolId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(run => run.Protocol)
            .Where(run => run.ProtocolId == protocolId)
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProtocolRun>> GetByProtocolIdsAsync(IEnumerable<Guid> protocolIds, CancellationToken cancellationToken = default)
    {
        var ids = protocolIds.ToList();

        return await _dbSet
            .Include(run => run.Protocol)
            .Where(run => ids.Contains(run.ProtocolId))
            .OrderBy(run => run.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProtocolRun?> GetWithProtocolAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(run => run.Protocol)
                .ThenInclude(protocol => protocol!.Items)
                    .ThenInclude(item => item.CompoundRecord)
            .FirstOrDefaultAsync(run => run.Id == runId, cancellationToken);
    }
}

public interface IProtocolRunRepository : IRepository<ProtocolRun>
{
    Task<ProtocolRun?> GetActiveByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<ProtocolRun?> GetActiveByProtocolIdAsync(Guid protocolId, CancellationToken cancellationToken = default);
    Task<ProtocolRun?> GetLatestByProtocolIdAsync(Guid protocolId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProtocolRun>> GetByProtocolIdsAsync(IEnumerable<Guid> protocolIds, CancellationToken cancellationToken = default);
    Task<ProtocolRun?> GetWithProtocolAsync(Guid runId, CancellationToken cancellationToken = default);
}
