namespace BioStack.Domain.Entities;

public sealed class ProviderAccessRequest
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? Owner { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public DateTime ConsentRecordedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
