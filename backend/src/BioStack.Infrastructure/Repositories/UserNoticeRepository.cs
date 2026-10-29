namespace BioStack.Infrastructure.Repositories;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public interface IUserNoticeRepository
{
    Task<UserNotice?> FindByDedupeKeyAsync(Guid userId, string dedupeKey, CancellationToken ct = default);
    Task<UserNotice?> FindAsync(Guid userId, Guid id, CancellationToken ct = default);
    /// <summary>Newest first, never including dismissed notes.</summary>
    Task<IReadOnlyList<UserNotice>> ListAsync(Guid userId, bool unreadOnly, int take, CancellationToken ct = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserNotice notice, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
    /// <summary>Removes read or dismissed notes older than the cutoff, then the oldest notes beyond <paramref name="cap"/>.</summary>
    Task PruneAsync(Guid userId, DateTime readCutoffUtc, int cap, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    void ClearTracking();
}

public sealed class UserNoticeRepository : IUserNoticeRepository
{
    private readonly BioStackDbContext _context;

    public UserNoticeRepository(BioStackDbContext context) => _context = context;

    public Task<UserNotice?> FindByDedupeKeyAsync(Guid userId, string dedupeKey, CancellationToken ct = default)
        => _context.UserNotices.FirstOrDefaultAsync(n => n.UserId == userId && n.DedupeKey == dedupeKey, ct);

    public Task<UserNotice?> FindAsync(Guid userId, Guid id, CancellationToken ct = default)
        => _context.UserNotices.FirstOrDefaultAsync(n => n.UserId == userId && n.Id == id, ct);

    public async Task<IReadOnlyList<UserNotice>> ListAsync(Guid userId, bool unreadOnly, int take, CancellationToken ct = default)
    {
        var query = _context.UserNotices.AsNoTracking().Where(n => n.UserId == userId && n.DismissedAtUtc == null);
        if (unreadOnly) query = query.Where(n => n.ReadAtUtc == null);
        return await query.OrderByDescending(n => n.UpdatedAtUtc).ThenByDescending(n => n.Id).Take(take).ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default)
        => _context.UserNotices.CountAsync(n => n.UserId == userId && n.ReadAtUtc == null && n.DismissedAtUtc == null, ct);

    public async Task AddAsync(UserNotice notice, CancellationToken ct = default)
        => await _context.UserNotices.AddAsync(notice, ct);

    public Task<int> MarkAllReadAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default)
        => _context.UserNotices
            .Where(n => n.UserId == userId && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(u => u.SetProperty(n => n.ReadAtUtc, nowUtc), ct);

    public async Task PruneAsync(Guid userId, DateTime readCutoffUtc, int cap, CancellationToken ct = default)
    {
        await _context.UserNotices
            .Where(n => n.UserId == userId && (n.ReadAtUtc != null || n.DismissedAtUtc != null) && n.UpdatedAtUtc < readCutoffUtc)
            .ExecuteDeleteAsync(ct);

        var total = await _context.UserNotices.CountAsync(n => n.UserId == userId, ct);
        if (total <= cap) return;

        var excess = await _context.UserNotices.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.ReadAtUtc == null && n.DismissedAtUtc == null ? 1 : 0)
            .ThenBy(n => n.UpdatedAtUtc)
            .Select(n => n.Id)
            .Take(total - cap)
            .ToListAsync(ct);
        await _context.UserNotices.Where(n => excess.Contains(n.Id)).ExecuteDeleteAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

    public void ClearTracking() => _context.ChangeTracker.Clear();
}
