namespace BioStack.Infrastructure.Persistence.Entities;

public sealed class StagedTranscriptCandidateReviewEntity
{
    public string ArtifactId { get; set; } = string.Empty;
    public string Canonicality { get; set; } = string.Empty;
    public string ReviewState { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceMetadataJson { get; set; } = "{}";
    public string Provider { get; set; } = string.Empty;
    public bool IsDeterministicFixture { get; set; }
    public int SegmentCount { get; set; }
    public string SegmentSnapshotSignature { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public string? TargetCanonicalName { get; set; }
    public Guid? PromotedKnowledgeEntryId { get; set; }
    public string? PromotedAtUtc { get; set; }
}
