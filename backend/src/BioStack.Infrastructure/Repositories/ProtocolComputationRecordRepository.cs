namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ProtocolComputationRecordRepository : Repository<ProtocolComputationRecord>, IProtocolComputationRecordRepository
{
    public ProtocolComputationRecordRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProtocolComputationRecord>> GetByProtocolIdsAsync(IEnumerable<Guid> protocolIds, CancellationToken cancellationToken = default)
    {
        var ids = protocolIds.ToList();

        return await _dbSet
            .Where(record => ids.Contains(record.ProtocolId))
            .OrderBy(record => record.TimestampUtc)
            .ToListAsync(cancellationToken);
    }
}

public interface IProtocolComputationRecordRepository : IRepository<ProtocolComputationRecord>
{
    Task<IEnumerable<ProtocolComputationRecord>> GetByProtocolIdsAsync(IEnumerable<Guid> protocolIds, CancellationToken cancellationToken = default);
}
