namespace BioStack.Api.Endpoints;

public sealed record AdminStagedTranscriptCandidateReviewResponse(
    string ArtifactId,
    string Canonicality,
    string ReviewState,
    string SourceType,
    string SourceUrl,
    string Provider,
    bool IsDeterministicFixture,
    int SegmentCount,
    string SegmentSnapshotSignature,
    IReadOnlyDictionary<string, string> SourceMetadata,
    string CreatedAtUtc,
    string UpdatedAtUtc);
