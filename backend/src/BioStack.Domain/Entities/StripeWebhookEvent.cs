namespace BioStack.Domain.Entities;

public sealed class StripeWebhookEvent
{
    public Guid Id { get; set; }
    public string StripeEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
