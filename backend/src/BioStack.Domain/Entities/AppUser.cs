namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

/// <summary>
/// Represents an authenticated user, identified by their OAuth provider identity.
/// The role is deliberately not exposed via any public API — Admin status is
/// communicated to the frontend only through the JWT claims we issue.
/// </summary>
public sealed class AppUser
{
    public Guid Id { get; set; }

    /// <summary>e.g. "google|117263485..."  or "github|1234567"</summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>e.g. "google", "github", "facebook", "apple"</summary>
    public string Provider { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>URL to the OAuth provider's profile picture.</summary>
    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;

    // Profiles owned by this user
    public ICollection<PersonProfile> Profiles { get; set; } = new List<PersonProfile>();
}
