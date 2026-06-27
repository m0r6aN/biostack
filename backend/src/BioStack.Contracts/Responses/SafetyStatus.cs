namespace BioStack.Contracts.Responses;

/// <summary>
/// Outcome of the user-facing intelligence safety gate (Lane H), surfaced on response contracts so
/// clients can render warning-first framing and prove a governed safety decision occurred.
/// Ordered by severity: <see cref="Allowed"/> &lt; <see cref="Warning"/> &lt; <see cref="Constrained"/>
/// &lt; <see cref="Refused"/>.
/// </summary>
public static class SafetyStatus
{
    /// <summary>Output passed the gate unchanged — no warning, constraint, or refusal needed.</summary>
    public const string Allowed = "allowed";

    /// <summary>Output is allowed but carries required warning-first / evidence-limit framing.</summary>
    public const string Warning = "warning";

    /// <summary>Output text was rewritten/neutralized because it tripped doctrine (e.g. imperative or dosing language).</summary>
    public const string Constrained = "constrained";

    /// <summary>The request itself was unsafe (sourcing/injection/dosing instruction) and was refused.</summary>
    public const string Refused = "refused";
}
