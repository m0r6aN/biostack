namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;

public sealed class OwnershipGuard : IOwnershipGuard
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IPersonProfileRepository _profileRepository;

    public OwnershipGuard(
        ICurrentUserAccessor currentUserAccessor,
        IPersonProfileRepository profileRepository)
    {
        _currentUserAccessor = currentUserAccessor;
        _profileRepository = profileRepository;
    }

    public Guid CurrentUserId => _currentUserAccessor.GetCurrentUserId();

    public async Task<PersonProfile> GetOwnedProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetOwnedByIdAsync(profileId, CurrentUserId, cancellationToken);
        if (profile is null)
            throw new InvalidOperationException($"Profile with ID {profileId} not found");

        return profile;
    }

    public async Task EnsureProfileOwnedAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _ = await GetOwnedProfileAsync(profileId, cancellationToken);
    }
}

public interface IOwnershipGuard
{
    Guid CurrentUserId { get; }
    Task<PersonProfile> GetOwnedProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task EnsureProfileOwnedAsync(Guid profileId, CancellationToken cancellationToken = default);
}
