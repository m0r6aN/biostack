namespace BioStack.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;

public sealed class AppUserRepository : IAppUserRepository
{
    private readonly BioStackDbContext _context;

    public AppUserRepository(BioStackDbContext context) => _context = context;

    public async Task<AppUser?> FindByProviderAsync(string provider, string providerKey, CancellationToken ct = default)
        => await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderKey == providerKey, ct);

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AppUsers.FindAsync(new object[] { id }, ct);

    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct = default)
        => await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);

    public async Task<AppUser> UpsertAsync(AppUser user, CancellationToken ct = default)
    {
        var existing = await FindByProviderAsync(user.Provider, user.ProviderKey, ct);
        if (existing is null)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
            _context.AppUsers.Add(user);
        }
        else
        {
            existing.Email        = user.Email;
            existing.DisplayName  = user.DisplayName;
            existing.AvatarUrl    = user.AvatarUrl;
            existing.StripeCustomerId = user.StripeCustomerId;
            existing.LastSeenAtUtc = DateTime.UtcNow;
            // Role is never overwritten by the provider data
        }

        await _context.SaveChangesAsync(ct);
        return existing ?? user;
    }
}

public interface IAppUserRepository
{
    Task<AppUser?> FindByProviderAsync(string provider, string providerKey, CancellationToken ct = default);
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppUser>  UpsertAsync(AppUser user, CancellationToken ct = default);
}
