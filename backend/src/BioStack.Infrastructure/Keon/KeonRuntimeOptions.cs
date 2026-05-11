namespace BioStack.Infrastructure.Keon;

public sealed class KeonRuntimeOptions
{
    public const string SectionName = "KeonRuntime";

    /// <summary>Base URL for the Keon Runtime API (no trailing slash required).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Bearer token for Keon Runtime auth.</summary>
    public string? BearerToken { get; set; }

    /// <summary>HTTP timeout per request, in milliseconds. Default 5 000.</summary>
    public int TimeoutMs { get; set; } = 5_000;

    /// <summary>
    /// When true, routes calls to the live Keon Runtime.
    /// When false (default), uses the fail-closed stub.
    /// </summary>
    public bool LiveMode { get; set; } = false;

    /// <summary>
    /// Dev-only: when true, the stub allows all policy checks instead of blocking.
    /// Has no effect in LiveMode. NEVER set true in production.
    /// </summary>
    public bool StubAllowAll { get; set; } = false;
}
