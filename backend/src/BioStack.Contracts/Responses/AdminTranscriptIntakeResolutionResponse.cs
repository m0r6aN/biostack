namespace BioStack.Contracts.Responses;

public sealed record AdminTranscriptIntakeResolutionResponse(
    Guid IntakeRequestId,
    string SourceType,
    string SourceUrl,
    string Provider,
    string Status,
    string ResultCode,
    int? SegmentCount,
    bool? IsDeterministicFixture,
    IReadOnlyDictionary<string, string>? ProviderMetadata);
