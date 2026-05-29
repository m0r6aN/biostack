namespace BioStack.Application.Services;

public interface ITranscriptCandidateArtifactStagingService
{
    TranscriptCandidateArtifactDescriptor Stage(TranscriptSourceMaterialResult sourceMaterial);
}

public sealed record TranscriptCandidateArtifactDescriptor(
    string ArtifactKind,
    string Canonicality,
    string StageStatus,
    string SourceType,
    string SourceUrl,
    string Provider,
    string RetrievedAtIsoUtc,
    bool IsDeterministicFixture,
    int SegmentCount,
    string SegmentSnapshotSignature,
    IReadOnlyDictionary<string, string> SourceMetadata);

public sealed class TranscriptCandidateArtifactStagingService : ITranscriptCandidateArtifactStagingService
{
    public TranscriptCandidateArtifactDescriptor Stage(TranscriptSourceMaterialResult sourceMaterial)
    {
        ArgumentNullException.ThrowIfNull(sourceMaterial);
        ArgumentNullException.ThrowIfNull(sourceMaterial.SourceReference);
        ArgumentNullException.ThrowIfNull(sourceMaterial.Segments);
        ArgumentNullException.ThrowIfNull(sourceMaterial.Metadata);

        var sortedMetadata = sourceMaterial.Metadata
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

        var segmentSignatureParts = sourceMaterial.Segments
            .OrderBy(segment => segment.Sequence)
            .Select(segment =>
                $"{segment.Sequence}|{segment.StartSeconds?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}|{segment.DurationSeconds?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}|{segment.Text}")
            .ToList();

        var signatureInput = string.Join("\n", segmentSignatureParts);
        var signatureBytes = System.Text.Encoding.UTF8.GetBytes(signatureInput);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(signatureBytes);
        var signature = Convert.ToHexString(hashBytes);

        return new TranscriptCandidateArtifactDescriptor(
            ArtifactKind: "transcript_source_material_candidate",
            Canonicality: "non_canonical",
            StageStatus: "staged_candidate",
            SourceType: sourceMaterial.SourceReference.SourceType,
            SourceUrl: sourceMaterial.SourceReference.SourceUrl,
            Provider: sourceMaterial.Provider,
            RetrievedAtIsoUtc: sourceMaterial.RetrievedAtIsoUtc,
            IsDeterministicFixture: sourceMaterial.IsDeterministicFixture,
            SegmentCount: sourceMaterial.Segments.Count,
            SegmentSnapshotSignature: signature,
            SourceMetadata: sortedMetadata);
    }
}
