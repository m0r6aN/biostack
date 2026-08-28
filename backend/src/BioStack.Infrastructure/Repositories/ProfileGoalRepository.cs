namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ProfileGoalRepository : Repository<ProfileGoal>, IProfileGoalRepository
{
    public ProfileGoalRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<List<ProfileGoal>> GetByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(goal => goal.ProfileId == profileId)
            .ToListAsync(cancellationToken);
    }

    public void RemoveRange(IEnumerable<ProfileGoal> goals)
    {
        _dbSet.RemoveRange(goals);
    }
}

public interface IProfileGoalRepository : IRepository<ProfileGoal>
{
    Task<List<ProfileGoal>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    void RemoveRange(IEnumerable<ProfileGoal> goals);
}
