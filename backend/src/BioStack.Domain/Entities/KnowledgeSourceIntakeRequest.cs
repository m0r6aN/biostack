namespace BioStack.Domain.Entities;

public sealed class KnowledgeSourceIntakeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourceType { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string? OptionalInstructions { get; set; }
    public List<string> RequestedOutputs { get; set; } = new();
    public int? MaxVideos { get; set; }
    public DateTimeOffset? PublishedAfterUtc { get; set; }
    public DateTimeOffset? PublishedBeforeUtc { get; set; }
    public string Status { get; set; } = "queued";
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
