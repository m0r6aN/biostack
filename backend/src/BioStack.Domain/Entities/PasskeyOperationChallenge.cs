namespace BioStack.Domain.Entities;

public sealed class PasskeyOperationChallenge
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string RequestIdHash { get; set; } = string.Empty;
    public string OptionsJson { get; set; } = string.Empty;
    public string RedirectPath { get; set; } = "/mission-control";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? IpAddress { get; set; }

    public AppUser? User { get; set; }
}
