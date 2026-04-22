namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

/// <summary>
/// Represents an authenticated user. Legacy provider fields are preserved for
/// existing rows, but new sign-ins are backed by first-party auth identities.
/// </summary>
public sealed class AppUser
{
    public Guid Id { get; set; }

    /// <summary>Legacy provider key. Email auth stores the normalized address.</summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>Legacy provider. New email auth rows use "email".</summary>
    public string Provider { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional profile picture URL retained for existing users.</summary>
    public string? AvatarUrl { get; set; }

    public string StripeCustomerId { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;

    // Profiles owned by this user
    public ICollection<PersonProfile> Profiles { get; set; } = new List<PersonProfile>();
    public ICollection<AuthIdentity> AuthIdentities { get; set; } = new List<AuthIdentity>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
