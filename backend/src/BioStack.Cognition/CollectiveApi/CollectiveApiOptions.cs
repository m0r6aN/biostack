namespace BioStack.Cognition.CollectiveApi;

/// <summary>
/// Configuration for the Keon Collective API integration.
///
/// Integration boundary per Collective API Integration Guide:
///   BioStack server → Keon Control /api/collective/* → Collective Host
///
/// Do NOT expose these values browser-side.
/// Tenant and actor context are bound from trusted server state, not from
/// browser-supplied fields.
/// </summary>
public sealed class CollectiveApiOptions
{
    public const string SectionName = "KeonCollective";

    /// <summary>Base URL for Keon Control (no trailing slash required).
    /// Maps to KEON_COLLECTIVE_HOST_BASE_URL in the integration guide.</summary>
    public string ControlBaseUrl { get; set; } = string.Empty;

    /// <summary>Optional full Authorization header override (e.g. "Bearer token123").
    /// Maps to KEON_COLLECTIVE_HOST_AUTHORIZATION.</summary>
    public string? AuthorizationHeader { get; set; }

    /// <summary>Optional bearer token — used as "Bearer &lt;token&gt;".
    /// Maps to KEON_COLLECTIVE_HOST_BEARER_TOKEN.</summary>
    public string? BearerToken { get; set; }

    /// <summary>HTTP timeout in milliseconds per request. Defaults to 10 000.
    /// Maps to KEON_COLLECTIVE_HOST_TIMEOUT_MS.</summary>
    public int TimeoutMs { get; set; } = 10_000;

    /// <summary>When true, routes calls to the live Collective Host via Keon Control.
    /// When false (default), the rule-based stub orchestrator is used.</summary>
    public bool LiveMode { get; set; } = false;

    /// <summary>
    /// Maximum GET-poll attempts when the Collective Host returns 202.
    /// Default 3 × PollDelayMs 500ms = 1.5s poll budget + TimeoutMs per request.
    /// Callers should always pass a request-scoped CancellationToken to RunAsync
    /// so that an HTTP timeout from the client side also aborts the poll loop.
    /// </summary>
    public int PollMaxAttempts { get; set; } = 3;

    /// <summary>Milliseconds to wait between poll attempts. Default 500ms.</summary>
    public int PollDelayMs { get; set; } = 500;
}
