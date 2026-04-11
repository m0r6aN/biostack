namespace BioStack.Domain.Entities;

public sealed class LeadCapture
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
