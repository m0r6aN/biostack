namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class CompoundInteractionHintRepository : Repository<CompoundInteractionHint>, ICompoundInteractionHintRepository
{
    public CompoundInteractionHintRepository(BioStackDbContext context) : base(context)
    {
    }

    public async Task<CompoundInteractionHint?> FindPairAsync(string compoundA, string compoundB, CancellationToken cancellationToken = default)
    {
        var (normalizedA, normalizedB) = NormalizePair(compoundA, compoundB);

        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                hint => hint.CompoundA == normalizedA && hint.CompoundB == normalizedB,
                cancellationToken);
    }

    public new async Task<List<CompoundInteractionHint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(hint => hint.CompoundA)
            .ThenBy(hint => hint.CompoundB)
            .ToListAsync(cancellationToken);
    }

    public static (string CompoundA, string CompoundB) NormalizePair(string compoundA, string compoundB)
    {
        var left = compoundA.Trim();
        var right = compoundB.Trim();

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) <= 0
            ? (left, right)
            : (right, left);
    }
}

public interface ICompoundInteractionHintRepository : IRepository<CompoundInteractionHint>
{
    Task<CompoundInteractionHint?> FindPairAsync(string compoundA, string compoundB, CancellationToken cancellationToken = default);
    new Task<List<CompoundInteractionHint>> GetAllAsync(CancellationToken cancellationToken = default);
}
