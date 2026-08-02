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

    /// <summary>
    /// When Keon cannot issue an authoritative receipt, allow a clearly-labelled local
    /// "unanchored" Spine row to be written for NON-EFFECTING provenance receipts only
    /// (safety warnings, constraints, refusals).
    ///
    /// Default true: preserves the audit trail and keeps the user's safety warning visible.
    /// Set false to degrade to "warning surfaced, nothing recorded".
    ///
    /// NEVER applies to effect-bearing receipts — those always fail closed.
    /// </summary>
    public bool AllowUnanchoredSafetyReceipts { get; set; } = true;

    /// <summary>
    /// Escape hatch for running a stubbed (ungoverned) Keon runtime in Production.
    /// Startup fails fast unless this is explicitly set, so a misconfigured deploy cannot
    /// silently serve traffic without a live governance runtime.
    /// </summary>
    public bool AllowStubInProduction { get; set; } = false;
}
