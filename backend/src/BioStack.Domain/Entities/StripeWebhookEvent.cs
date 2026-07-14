namespace BioStack.Domain.Entities;

public sealed class StripeWebhookEvent
{
    public Guid Id { get; set; }
    public string StripeEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ProcessingStatus { get; set; } = StripeWebhookProcessingStatuses.Processed;
    public string? FailureCode { get; set; }
    public int AttemptCount { get; set; } = 1;
    public DateTime LastAttemptAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class StripeWebhookProcessingStatuses
{
    public const string Processed = "processed";
    public const string Quarantined = "quarantined";
}
