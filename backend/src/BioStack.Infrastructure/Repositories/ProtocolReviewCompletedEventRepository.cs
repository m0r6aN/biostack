namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ProtocolReviewCompletedEventRepository : Repository<ProtocolReviewCompletedEvent>, IProtocolReviewCompletedEventRepository
{
    public ProtocolReviewCompletedEventRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProtocolReviewCompletedEvent>> GetByProtocolIdsAsync(IEnumerable<Guid> protocolIds, CancellationToken cancellationToken = default)
    {
        var ids = protocolIds.ToList();

        return await _dbSet
            .Where(@event => ids.Contains(@event.ProtocolId))
            .OrderBy(@event => @event.CompletedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

public interface IProtocolReviewCompletedEventRepository : IRepository<ProtocolReviewCompletedEvent>
{
    Task<IEnumerable<ProtocolReviewCompletedEvent>> GetByProtocolIdsAsync(IEnumerable<Guid> protocolIds, CancellationToken cancellationToken = default);
}
