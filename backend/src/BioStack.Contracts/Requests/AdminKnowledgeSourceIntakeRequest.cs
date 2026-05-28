namespace BioStack.Contracts.Requests;

/// <summary>
/// Admin-submitted intake request for Knowledge Worker source extraction.
/// This request creates review-bound candidate artifacts only and never writes canonical knowledge directly.
/// </summary>
public sealed record AdminKnowledgeSourceIntakeRequest(
    KnowledgeSourceType SourceType,
    string SourceUrl,
    string? OptionalInstructions,
    IReadOnlyList<RequestedOutputArea> RequestedOutputs,
    ChannelIngestionOptions? ChannelOptions);

public enum KnowledgeSourceType
{
    VideoUrl = 1,
    ChannelUrl = 2,
}

public enum RequestedOutputArea
{
    SourceMetadata = 1,
    TranscriptQuality = 2,
    CoreThesis = 3,
    Claims = 4,
    CompoundsMentioned = 5,
    BiomarkersOrLabsMentioned = 6,
    ProtocolPhases = 7,
    SafetyFlags = 8,
    EvidenceGaps = 9,
    RawArtifactRefs = 10,
}

/// <summary>
/// Optional controls when SourceType is ChannelUrl.
/// </summary>
public sealed record ChannelIngestionOptions(
    int? MaxVideos,
    DateTimeOffset? PublishedAfterUtc,
    DateTimeOffset? PublishedBeforeUtc);
