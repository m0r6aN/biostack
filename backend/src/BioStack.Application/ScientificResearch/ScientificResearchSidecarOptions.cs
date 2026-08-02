namespace BioStack.Application.ScientificResearch;

/// <summary>
/// Configuration for the Python scientific research sidecar HTTP client.
/// </summary>
public sealed class ScientificResearchSidecarOptions
{
    public const string SectionName = "ScientificResearchSidecar";

    /// <summary>Base URL for the sidecar (no trailing slash required), e.g. http://127.0.0.1:8080.</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:8080";

    /// <summary>Bearer token matching BIOSTACK_RESEARCH_SERVICE_TOKEN when configured.</summary>
    public string? ServiceToken { get; set; }

    /// <summary>HTTP timeout per request, in milliseconds. Default 30_000.</summary>
    public int TimeoutMs { get; set; } = 30_000;

    /// <summary>
    /// When true, routes to the live sidecar HTTP client.
    /// When false (default), uses the disabled stub so BioStack remains operational.
    /// </summary>
    public bool Enabled { get; set; } = false;
}
