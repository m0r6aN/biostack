namespace BioStack.Application.Services;

public sealed class YouTubeTranscriptProviderOptions
{
    public const string SectionName = "TranscriptProviders:YouTube";

    public bool Enabled { get; set; } = false;
}
