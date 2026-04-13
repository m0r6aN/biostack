namespace BioStack.Domain.Entities;

public sealed class AuthChallenge
{
    public Guid Id { get; set; }
    public Guid IdentityId { get; set; }
    public string Channel { get; set; } = "email";
    public string ChallengeType { get; set; } = "magic_link";
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int AttemptCount { get; set; }
    public string? IpAddress { get; set; }
    public string RedirectPath { get; set; } = "/mission-control";

    public AuthIdentity Identity { get; set; } = null!;
}
