namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

public sealed class OwnershipGuard : IOwnershipGuard
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IPersonProfileRepository _profileRepository;
    private readonly bool _devBypassEnabled;

    public OwnershipGuard(
        ICurrentUserAccessor currentUserAccessor,
        IPersonProfileRepository profileRepository,
        IOptions<DevBypassOptions> devBypassOptions)
    {
        _currentUserAccessor = currentUserAccessor;
        _profileRepository = profileRepository;
        _devBypassEnabled = devBypassOptions.Value.Enabled;
    }

    public Guid CurrentUserId => _currentUserAccessor.GetCurrentUserId();

    public async Task<PersonProfile> GetOwnedProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = _devBypassEnabled
            ? await _profileRepository.GetByIdWithNavigationAsync(profileId, cancellationToken)
            : await _profileRepository.GetOwnedByIdAsync(profileId, CurrentUserId, cancellationToken);

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
