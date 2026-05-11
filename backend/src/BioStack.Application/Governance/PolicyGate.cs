namespace BioStack.Application.Governance;

using System.Text.RegularExpressions;
using BioStack.Infrastructure.Keon;
using Microsoft.Extensions.Logging;

/// <summary>
/// Application-layer result from the Policy Gate.
/// Wraps <see cref="PolicyGateResult"/> and adds <see cref="LocallyClassified"/>
/// to distinguish local pre-classification from Keon-delegated decisions.
/// </summary>
public sealed record PolicyGateCheckResult(
    PolicyDecision Decision,
    string? DisclaimerText,
    string? RewrittenText,
    string? BlockReason,
    PolicyHash PolicyHash,
    bool LocallyClassified);

/// <summary>
/// KE-3 Policy Gate: classify and policy-check a text fragment before rendering.
/// Wraps IKeonRuntimeClient.PolicyCheckAsync with:
///   - Input validation (empty → ArgumentException)
///   - A local pre-classifier for obvious prohibited phrases (no Keon call needed)
///   - Structured logging of every gate result
/// </summary>
public sealed class PolicyGate(
    IKeonRuntimeClient keon,
    ILogger<PolicyGate> logger)
{
    // Banned-phrase patterns mirror DoctrineSanitizer's patterns.
    // Both classes share the same doctrine but are not coupled in code:
    // changes to one must be applied to the other to keep them consistent.
    private static readonly Regex[] ProhibitedPatterns =
    [
        new(@"\byou\s+should\b",             RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\byou\s+must\b",               RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\btake\s+\d+\s*(mg|mcg|g)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bdose\s+at\b",                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bis\s+safe\b",                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bwill\s+treat\b",             RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bcures?\b",                   RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bproven\s+to\b",              RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bstop\s+taking\b",            RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    private static readonly PolicyHash ZeroedPolicyHash = new("local-classifier-v0", "0.0.0");

    /// <summary>
    /// Classify and policy-check a text fragment.
    /// Returns blocked if Keon is offline (fail-closed).
    /// Throws <see cref="ArgumentException"/> if <paramref name="text"/> is null or whitespace.
    /// </summary>
    public async Task<PolicyGateCheckResult> CheckAsync(
        string text,
        string context,
        string tenantId,
        string actorId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("text must not be empty or whitespace.", nameof(text));

        // Local pre-classify: if obviously prohibited, block without calling Keon.
        var localClass = LocalPreClassify(text);
        if (localClass == LanguageClassification.Prohibited)
        {
            logger.LogWarning(
                "PolicyGate blocked by local pre-classifier. Context={Context} TenantId={TenantId} ActorId={ActorId}",
                context, tenantId, actorId);

            return new PolicyGateCheckResult(
                Decision: PolicyDecision.Blocked,
                DisclaimerText: null,
                RewrittenText: null,
                BlockReason: "local-classifier: prohibited language detected",
                PolicyHash: ZeroedPolicyHash,
                LocallyClassified: true);
        }

        // Delegate to Keon (stub or live). Keon's contract guarantees fail-closed on error.
        var request = new PolicyGateRequest(text, context, tenantId, actorId);
        var result = await keon.PolicyCheckAsync(request, ct);

        logger.LogInformation(
            "PolicyGate result: Decision={Decision} LocallyClassified=false Context={Context} TenantId={TenantId} ActorId={ActorId}",
            result.Decision, context, tenantId, actorId);

        return new PolicyGateCheckResult(
            Decision: result.Decision,
            DisclaimerText: result.DisclaimerText,
            RewrittenText: result.RewrittenText,
            BlockReason: result.BlockReason,
            PolicyHash: result.PolicyHash,
            LocallyClassified: false);
    }

    /// <summary>
    /// Local pre-classifier: returns <see cref="LanguageClassification.Prohibited"/> for
    /// obvious banned imperative medical phrases so tests don't need a live Keon runtime.
    /// Returns <see langword="null"/> for all other text (defers classification to Keon).
    /// </summary>
    internal LanguageClassification? LocalPreClassify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        foreach (var pattern in ProhibitedPatterns)
        {
            if (pattern.IsMatch(text))
                return LanguageClassification.Prohibited;
        }

        return null;
    }
}
