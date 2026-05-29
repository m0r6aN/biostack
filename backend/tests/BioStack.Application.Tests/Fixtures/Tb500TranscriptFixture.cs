namespace BioStack.Application.Tests.Fixtures;

using BioStack.Application.Services;

internal static class Tb500TranscriptFixture
{
    internal const string SourceType = "video_url";
    internal const string SourceUrl = "https://www.youtube.com/watch?v=SpzHHYvCNGU";
    internal const string Provider = "fake_transcript_provider";
    internal const string RetrievedAtIsoUtc = "2026-01-15T00:00:00Z";

    internal static readonly IReadOnlyDictionary<string, string> Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["externalReference"] = "youtube:SpzHHYvCNGU",
        ["title"] = "TB500 discussion: training recovery concepts",
        ["channel"] = "Performance Lab Conversations",
        ["language"] = "en",
        ["fixtureId"] = "tb500-static-fixture-v1",
    };

    internal static readonly IReadOnlyList<TranscriptSegment> Segments = new[]
    {
        new TranscriptSegment(
            Sequence: 1,
            Text: "Today we discuss what people mean when they mention TB500 in online recovery conversations.",
            StartSeconds: 0.0,
            DurationSeconds: 7.2),
        new TranscriptSegment(
            Sequence: 2,
            Text: "This episode stays at a high level and focuses on terminology, sourcing quality, and uncertainty.",
            StartSeconds: 7.2,
            DurationSeconds: 7.5),
        new TranscriptSegment(
            Sequence: 3,
            Text: "We do not provide treatment instructions and we are not giving personal medical guidance.",
            StartSeconds: 14.7,
            DurationSeconds: 6.8),
        new TranscriptSegment(
            Sequence: 4,
            Text: "A useful first step is separating anecdotal reports from controlled evidence and documenting assumptions.",
            StartSeconds: 21.5,
            DurationSeconds: 8.4),
        new TranscriptSegment(
            Sequence: 5,
            Text: "Listeners should verify primary sources and discuss decisions with licensed professionals when needed.",
            StartSeconds: 29.9,
            DurationSeconds: 7.3),
    };

    internal static TranscriptSourceReference Reference => new(SourceType, SourceUrl);

    internal static TranscriptSourceMaterialResult CreateResult() =>
        new(
            SourceReference: Reference,
            Provider: Provider,
            Segments: Segments,
            RetrievedAtIsoUtc: RetrievedAtIsoUtc,
            Metadata: Metadata,
            IsDeterministicFixture: true);
}
