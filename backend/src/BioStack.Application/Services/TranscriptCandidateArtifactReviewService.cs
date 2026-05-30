namespace BioStack.Application.Services;

public interface ITranscriptCandidateArtifactReviewService
{
    TranscriptCandidateArtifactReviewModel BuildReviewModel(TranscriptCandidateArtifactDescriptor descriptor);
}

public sealed record TranscriptCandidateArtifactReviewModel(
    string ArtifactId,
    string ReviewState,
    string Canonicality,
    string SourceType,
    string SourceUrl,
    IReadOnlyDictionary<string, string> SourceMetadata,
    string Provider,
    bool IsDeterministicFixture,
    int SegmentCount,
    string SegmentSnapshotSignature);

public sealed class TranscriptCandidateArtifactReviewService : ITranscriptCandidateArtifactReviewService
{
    private const string PendingReviewState = "pending_review";
    private const string NonCanonical = "non_canonical";

    public TranscriptCandidateArtifactReviewModel BuildReviewModel(TranscriptCandidateArtifactDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        EnsureValidDescriptor(descriptor);

        var sortedMetadata = descriptor.SourceMetadata
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

        var artifactId = BuildArtifactId(descriptor, sortedMetadata);

        return new TranscriptCandidateArtifactReviewModel(
            ArtifactId: artifactId,
            ReviewState: PendingReviewState,
            Canonicality: NonCanonical,
            SourceType: descriptor.SourceType,
            SourceUrl: descriptor.SourceUrl,
            SourceMetadata: sortedMetadata,
            Provider: descriptor.Provider,
            IsDeterministicFixture: descriptor.IsDeterministicFixture,
            SegmentCount: descriptor.SegmentCount,
            SegmentSnapshotSignature: descriptor.SegmentSnapshotSignature);
    }

    private static void EnsureValidDescriptor(TranscriptCandidateArtifactDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.ArtifactKind))
        {
            throw new ArgumentException("ArtifactKind is required.", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.Canonicality))
        {
            throw new ArgumentException("Canonicality is required.", nameof(descriptor));
        }

        if (!string.Equals(descriptor.Canonicality, NonCanonical, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Only non-canonical transcript candidate descriptors are supported. Actual canonicality: '{descriptor.Canonicality}'.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.StageStatus))
        {
            throw new ArgumentException("StageStatus is required.", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.SourceType))
        {
            throw new ArgumentException("SourceType is required.", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.SourceUrl))
        {
            throw new ArgumentException("SourceUrl is required.", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.Provider))
        {
            throw new ArgumentException("Provider is required.", nameof(descriptor));
        }

        if (descriptor.SegmentCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(descriptor), "SegmentCount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.SegmentSnapshotSignature))
        {
            throw new ArgumentException("SegmentSnapshotSignature is required.", nameof(descriptor));
        }

        ArgumentNullException.ThrowIfNull(descriptor.SourceMetadata);
    }

    private static string BuildArtifactId(
        TranscriptCandidateArtifactDescriptor descriptor,
        IReadOnlyDictionary<string, string> sortedMetadata)
    {
        if (!string.IsNullOrWhiteSpace(descriptor.SegmentSnapshotSignature))
        {
            return $"transcript-candidate:{descriptor.SegmentSnapshotSignature}";
        }

        var metadataPairs = sortedMetadata
            .Select(kvp => $"{kvp.Key}={kvp.Value}");

        var hashInput = string.Join(
            "\n",
            new[]
            {
                descriptor.ArtifactKind,
                descriptor.Canonicality,
                descriptor.StageStatus,
                descriptor.SourceType,
                descriptor.SourceUrl,
                descriptor.Provider,
                descriptor.SegmentCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                descriptor.SegmentSnapshotSignature,
                string.Join("|", metadataPairs),
            });

        var bytes = System.Text.Encoding.UTF8.GetBytes(hashInput);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return $"transcript-candidate:{Convert.ToHexString(hash)}";
    }
}
