namespace BioStack.Application.Services;

using BioStack.Infrastructure.Repositories;

public sealed class ConsentGate : IConsentGate
{
    /// <summary>
    /// Canonical initial consent version. Bumping this string requires every existing
    /// user to re-accept before they can mutate data again.
    /// </summary>
    public const string CurrentConsentVersion = "bio-observational-v1";

    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IAppUserRepository _userRepository;

    public ConsentGate(ICurrentUserAccessor currentUserAccessor, IAppUserRepository userRepository)
    {
        _currentUserAccessor = currentUserAccessor;
        _userRepository = userRepository;
    }

    public async Task<ConsentStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserAccessor.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        return new ConsentStatus(
            user.ConsentAcceptedAtUtc.HasValue &&
                string.Equals(user.ConsentVersion, CurrentConsentVersion, StringComparison.Ordinal),
            user.ConsentAcceptedAtUtc,
            user.ConsentVersion);
    }

    public async Task<bool> IsConsentGrantedAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken);
        return status.Accepted;
    }

    public async Task<ConsentStatus> RecordAsync(string? requestedVersion, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserAccessor.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        // The request value is compatibility-only. Evidence must always bind to the
        // disclosure version selected by the server, never a client-invented value.
        _ = requestedVersion;
        var version = CurrentConsentVersion;

        // Idempotent: if already accepted for the same or newer version, return existing record.
        if (user.ConsentAcceptedAtUtc.HasValue &&
            string.Equals(user.ConsentVersion, version, StringComparison.Ordinal))
        {
            return new ConsentStatus(true, user.ConsentAcceptedAtUtc, user.ConsentVersion);
        }

        user.ConsentAcceptedAtUtc = DateTime.UtcNow;
        user.ConsentVersion = version;
        await _userRepository.UpdateConsentAsync(user, cancellationToken);

        return new ConsentStatus(true, user.ConsentAcceptedAtUtc, user.ConsentVersion);
    }
}

public interface IConsentGate
{
    Task<ConsentStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> IsConsentGrantedAsync(CancellationToken cancellationToken = default);
    Task<ConsentStatus> RecordAsync(string? requestedVersion, CancellationToken cancellationToken = default);
}

public sealed record ConsentStatus(bool Accepted, DateTime? ConsentAcceptedAtUtc, string? ConsentVersion);
