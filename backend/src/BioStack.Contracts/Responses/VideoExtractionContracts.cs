namespace BioStack.Contracts.Responses;

/// <summary>
/// Normalized extraction output produced by the Video Extraction Agent.
/// This output is review-only and must not be treated as canonical BioStack guidance.
/// </summary>
public sealed record VideoExtractionOutput(
    VideoExtractionSourceMetadata SourceMetadata,
    TranscriptQualityDescriptor TranscriptQuality,
    string CoreThesis,
    IReadOnlyList<ExtractedSourceClaim> Claims,
    IReadOnlyList<string> CompoundsMentioned,
    IReadOnlyList<string> BiomarkersOrLabsMentioned,
    IReadOnlyList<string> ProtocolPhases,
    IReadOnlyList<string> SafetyFlags,
    IReadOnlyList<string> EvidenceGaps,
    IReadOnlyList<RawArtifactReference> RawArtifactRefs);

public sealed record VideoExtractionSourceMetadata(
    string SourceType,
    string SourceUrl,
    string? Platform,
    string? ChannelId,
    string? ChannelName,
    string? VideoId,
    string? VideoTitle,
    DateTimeOffset? PublishedAtUtc);

public sealed record TranscriptQualityDescriptor(
    string TranscriptSource, // native | asr_fallback | unavailable
    string Quality,          // high | medium | low | unknown
    decimal? Confidence,
    decimal? CoveragePercent,
    string? Language,
    bool IsPartial);

public sealed record ExtractedSourceClaim(
    string ClaimId,
    string ClaimText,
    string ClaimType,
    SourceAttribution Attribution,
    bool IsDosingClaim,
    bool IsDiseaseClaim,
    bool IsSafetyClaim,
    bool RequiresMedicalReview,
    string ClaimOrigin, // must be "source-claim"
    IReadOnlyList<string> Tags);

public sealed record SourceAttribution(
    string SourceUrl,
    string? VideoId,
    string? ChannelId,
    string? TimestampStart,
    string? TimestampEnd,
    IReadOnlyList<string> ArtifactRefIds);

public sealed record RawArtifactReference(
    string ArtifactRefId,
    string Kind,  // transcript | extraction-json | metadata | diagnostics
    string Uri,
    string? Hash,
    long? ByteLength);
