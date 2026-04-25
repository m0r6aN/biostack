namespace BioStack.Application.Services;

using BioStack.Contracts.Requests;

public sealed record ProtocolIngestionRequest(
    ProtocolInputType InputType,
    string? InputText,
    string? LinkUrl,
    string? SourceName,
    string? ContentType,
    byte[]? SourceBytes);

public sealed record ProtocolIngestionResult(
    string ExtractedText,
    string NormalizedText,
    ProtocolInputType InputType,
    string? SourceName,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ProtocolIngestionArtifact> Artifacts,
    bool LowConfidence,
    string IngestionFingerprint,
    string ParseFingerprint);

public sealed record ProtocolIngestionArtifact(
    string Kind,
    string Label,
    string Preview);

public sealed record ProtocolExtractionResult(
    string ExtractedText,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ProtocolIngestionArtifact> Artifacts,
    bool LowConfidence);

public sealed record ProtocolOcrResult(
    string ExtractedText,
    IReadOnlyList<string> Warnings,
    bool LowConfidence);

public sealed record IngestionCacheDto(
    string ExtractedText,
    string NormalizedText,
    ProtocolInputType InputType,
    string? SourceName,
    List<string> Warnings,
    List<ProtocolIngestionArtifactDto> Artifacts,
    bool LowConfidence,
    string IngestionFingerprint,
    string ParseFingerprint);

public sealed record ProtocolIngestionArtifactDto(
    string Kind,
    string Label,
    string Preview);
