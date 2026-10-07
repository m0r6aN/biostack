namespace BioStack.Domain.Entities;

public sealed class PasskeyCredential
{
    public Guid Id { get; set; }
    public Guid IdentityId { get; set; }
    public byte[] CredentialId { get; set; } = [];
    public byte[] PublicKey { get; set; } = [];
    public byte[] UserHandle { get; set; } = [];
    public string CredentialType { get; set; } = "public-key";
    public long SignatureCounter { get; set; }
    public string Transports { get; set; } = string.Empty;
    public Guid AaGuid { get; set; }
    public bool IsBackupEligible { get; set; }
    public bool IsBackedUp { get; set; }
    public string DisplayName { get; set; } = "Passkey";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAtUtc { get; set; }

    public AuthIdentity Identity { get; set; } = null!;
}
