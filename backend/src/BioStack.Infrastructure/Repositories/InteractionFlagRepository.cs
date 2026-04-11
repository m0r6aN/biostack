namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;

public sealed class InteractionFlagRepository : Repository<InteractionFlag>, IInteractionFlagRepository
{
    public InteractionFlagRepository(BioStackDbContext context) : base(context)
    {
    }
}

public interface IInteractionFlagRepository : IRepository<InteractionFlag>
{
}
