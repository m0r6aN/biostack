namespace BioStack.Domain.Entities;

public sealed class AuthIdentity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = "email";
    public string ValueNormalized { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAtUtc { get; set; }

    public AppUser User { get; set; } = null!;
    public ICollection<AuthChallenge> Challenges { get; set; } = new List<AuthChallenge>();
}
