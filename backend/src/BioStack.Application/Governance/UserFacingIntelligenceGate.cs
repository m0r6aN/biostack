namespace BioStack.Application.Governance;

using System.Text.RegularExpressions;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Keon;
using Microsoft.Extensions.Logging;

/// <summary>
/// Stable reason codes recorded on a gate decision (Lane H), explaining why a status was assigned.
/// </summary>
public static class SafetyReasonCode
{
    public const string ProhibitedLanguage = "prohibited-language";
    public const string FallbackEvidenceLimited = "fallback-evidence-limited";
    public const string UnsafeRequest = "unsafe-request";
    public const string HighRiskCategoryPrefix = "high-risk-category:";
}

/// <summary>
/// One request to gate a user-facing intelligence output before it is serialized (Lane H).
/// </summary>
/// <param name="OutputType">What kind of output this is, e.g. <c>intelligence.compatibility</c> (for logging/receipt subject context).</param>
/// <param name="ActorUserId">The authenticated user the output is being produced for.</param>
/// <param name="SubjectUri">Receipt subject URI for any safety receipt issued.</param>
/// <param name="TextFields">User-facing text fragments to screen/constrain (e.g. relationship reasons). Order is preserved in the decision.</param>
/// <param name="EvidenceRefs">Evidence refs that justified the output (e.g. <c>compound-graph:{hash}</c>, <c>compound:{slug}</c>, <c>source:{id}</c>) — preserved on the safety receipt.</param>
/// <param name="SourceType"><see cref="IntelligenceSource.Graph"/> or <see cref="IntelligenceSource.Fallback"/>.</param>
/// <param name="Substances">Substance display names/slugs involved, for high-risk classification.</param>
/// <param name="CategoryHints">Optional explicit category labels (e.g. a payload's compound Category).</param>
/// <param name="GraphArtifactHash">Graph artifact hash when graph-backed (for the input-hash seed).</param>
/// <param name="ProfileId">Optional profile the output is personalized for.</param>
/// <param name="RequestText">Optional user-supplied prompt/free-text to screen for an unsafe request (sourcing/injection/dosing).</param>
public sealed record IntelligenceOutputRequest(
    string OutputType,
    Guid ActorUserId,
    string SubjectUri,
    IReadOnlyList<string> TextFields,
    IReadOnlyList<string> EvidenceRefs,
    string SourceType,
    IReadOnlyList<string>? Substances = null,
    IReadOnlyList<string>? CategoryHints = null,
    string? GraphArtifactHash = null,
    Guid? ProfileId = null,
    string? RequestText = null);

/// <summary>
/// The gate's decision for one output. <see cref="SafeText"/> is the constrained replacement for
/// <see cref="IntelligenceOutputRequest.TextFields"/> in the same order.
/// </summary>
public sealed record IntelligenceOutputDecision(
    string SafetyStatus,
    IReadOnlyList<string> SafeText,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> PolicyRefs,
    IReadOnlyList<string> SafetyGateRefs,
    string? SafetyReceiptUri,
    IReadOnlyList<string> ReasonCodes);

/// <summary>
/// Centralized Lane H safety gate for user-facing intelligence outputs. Every recommendation-shaped
/// or educational intelligence response should pass through here before serialization so that:
///   • imperative / dosing / certainty language is constrained (rewritten) — shared doctrine with
///     <see cref="DoctrineSanitizer"/>;
///   • high-risk categories force warning-first, evidence-limited framing — <see cref="HighRiskCategoryGate"/>;
///   • fallback (non-graph) intelligence is disclosed as low-confidence;
///   • unsafe requests (sourcing / injection / dosing instructions) are refused with safe text;
///   • a Governed-Spine receipt records any warning / constraint / refusal, preserving the evidence refs.
///
/// Keon's <see cref="PolicyGate"/> remains the live-mode escalation path (it is fail-closed and would
/// block all output in stub mode, so it is intentionally not the inline output enforcer); this gate
/// applies the same doctrine deterministically and locally, and records a <c>policy:</c> ref so the
/// policy evaluation is itself receipt-provable.
/// </summary>
public interface IUserFacingIntelligenceGate
{
    Task<IntelligenceOutputDecision> EvaluateAsync(IntelligenceOutputRequest request, CancellationToken ct = default);
}

public sealed class UserFacingIntelligenceGate(
    DoctrineSanitizer sanitizer,
    HighRiskCategoryGate highRisk,
    IRuntimeReceiptFactory receipts,
    ILogger<UserFacingIntelligenceGate> logger) : IUserFacingIntelligenceGate
{
    /// <summary>Stable id of the doctrine ruleset enforced by this gate, recorded as a policy ref.</summary>
    private const string DoctrinePolicyId = "biostack-doctrine-v1";

    // Patterns that mark a *request* (user input) as unsafe to act on at all — sourcing/procurement,
    // injection/administration how-to, and dosing-instruction seeking. Output constraints are handled
    // separately by the shared DoctrineSanitizer doctrine.
    private static readonly Regex[] UnsafeRequestPatterns =
    [
        new(@"\bwhere\s+(can|do|to)\b.*\b(buy|get|order|source|purchase)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bhow\s+(do|to|can)\b.*\b(inject|administer|reconstitute|dose)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\b(buy|order|source)\b.*\b(online|vendor|supplier|gray\s*market|grey\s*market)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\binject(ion|ing)?\b.*\b(protocol|schedule|site|how)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    private const string ConstrainedReplacement =
        "BioStack cannot present this as guidance. This is educational, observational context only — not medical advice, dosing, or instruction. Consider discussing with a qualified professional.";

    private const string RefusalText =
        "BioStack cannot help with sourcing, dosing, or administration instructions. BioStack provides educational, observational context only — not medical advice. Consider discussing your goals with a qualified professional.";

    private const string EvidenceLimitedFraming =
        "This is not drawn from BioStack's reviewed evidence graph; treat it as low-confidence, evidence-limited context.";

    public async Task<IntelligenceOutputDecision> EvaluateAsync(
        IntelligenceOutputRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var decisionId = Guid.NewGuid().ToString("N");
        var reasonCodes = new List<string>();
        var warnings = new List<string>();
        var policyRefs = new List<string> { ReceiptRefs.Policy(DoctrinePolicyId) };
        var safetyGateRefs = new List<string> { ReceiptRefs.SafetyGate(decisionId) };

        string status;
        IReadOnlyList<string> safeText;

        if (!string.IsNullOrWhiteSpace(request.RequestText) && IsUnsafeRequest(request.RequestText))
        {
            // The request itself asks for prohibited guidance — refuse outright with safe text.
            status = SafetyStatus.Refused;
            reasonCodes.Add(SafetyReasonCode.UnsafeRequest);
            warnings.Add(RefusalText);
            safeText = [RefusalText];
        }
        else
        {
            status = SafetyStatus.Allowed;

            // 1. Constrain each output text field that trips the shared doctrine.
            var rewritten = new List<string>(request.TextFields.Count);
            foreach (var field in request.TextFields)
            {
                if (!string.IsNullOrWhiteSpace(field) && sanitizer.ContainsBannedPhrase(field))
                {
                    rewritten.Add(ConstrainedReplacement);
                    if (!reasonCodes.Contains(SafetyReasonCode.ProhibitedLanguage))
                        reasonCodes.Add(SafetyReasonCode.ProhibitedLanguage);
                    status = Escalate(status, SafetyStatus.Constrained);
                }
                else
                {
                    rewritten.Add(field);
                }
            }
            safeText = rewritten;

            // 2. High-risk categories force warning-first framing.
            var assessment = highRisk.Assess(request.Substances, request.CategoryHints);
            if (assessment.IsHighRisk)
            {
                warnings.AddRange(assessment.RequiredFramings);
                reasonCodes.AddRange(assessment.Categories.Select(c => SafetyReasonCode.HighRiskCategoryPrefix + c));
                status = Escalate(status, SafetyStatus.Warning);
            }

            // 3. Fallback (non-graph) intelligence is disclosed as evidence-limited.
            if (string.Equals(request.SourceType, IntelligenceSource.Fallback, StringComparison.Ordinal))
            {
                warnings.Add(EvidenceLimitedFraming);
                if (!reasonCodes.Contains(SafetyReasonCode.FallbackEvidenceLimited))
                    reasonCodes.Add(SafetyReasonCode.FallbackEvidenceLimited);
                status = Escalate(status, SafetyStatus.Warning);
            }
        }

        var safetyReceiptUri = await MaybeIssueReceiptAsync(
            request, status, decisionId, reasonCodes, policyRefs, safetyGateRefs, ct);

        logger.LogInformation(
            "Intelligence output gated. OutputType={OutputType} Status={Status} Reasons={Reasons} ReceiptIssued={ReceiptIssued}",
            request.OutputType, status, string.Join(",", reasonCodes), safetyReceiptUri is not null);

        return new IntelligenceOutputDecision(
            SafetyStatus: status,
            SafeText: safeText,
            Warnings: warnings,
            PolicyRefs: policyRefs,
            SafetyGateRefs: safetyGateRefs,
            SafetyReceiptUri: safetyReceiptUri,
            ReasonCodes: reasonCodes);
    }

    /// <summary>
    /// Issue a Governed-Spine receipt only when a safety-relevant decision occurred (warning,
    /// constraint, or refusal). Allowed output emits nothing here — the caller records its own
    /// intelligence receipt (e.g. <c>intelligence.graph-artifact.used</c>).
    /// </summary>
    private async Task<string?> MaybeIssueReceiptAsync(
        IntelligenceOutputRequest request,
        string status,
        string decisionId,
        IReadOnlyList<string> reasonCodes,
        IReadOnlyList<string> policyRefs,
        IReadOnlyList<string> safetyGateRefs,
        CancellationToken ct)
    {
        var receiptClass = status switch
        {
            SafetyStatus.Refused => ReceiptClass.SafetyUnsafeRequestRefused,
            SafetyStatus.Constrained => ReceiptClass.SafetyGateTriggered,
            SafetyStatus.Warning => ReceiptClass.SafetyWarningSurfaced,
            _ => null,
        };

        if (receiptClass is null)
            return null;

        // Preserve the caller's evidence chain (compound-graph / compound / source refs) and add the
        // policy + safety-gate refs so the decision is itself provable.
        var evidenceRefs = request.EvidenceRefs
            .Concat(policyRefs)
            .Concat(safetyGateRefs)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var receipt = await receipts.IssueAndAppendAsync(new ReceiptContext(
            ReceiptClass: receiptClass,
            SubjectUri: request.SubjectUri,
            Actor: ReceiptActor.User(request.ActorUserId),
            EvidenceRefs: evidenceRefs,
            Decision: status,
            EffectStatus: "non-effecting",
            InputHashSeed: $"{decisionId}|{request.SubjectUri}|{string.Join(',', reasonCodes)}"), ct);

        return receipt.ReceiptUri;
    }

    internal bool IsUnsafeRequest(string requestText)
        => sanitizer.ContainsBannedPhrase(requestText)
           || UnsafeRequestPatterns.Any(p => p.IsMatch(requestText));

    // Severity ordering: allowed < warning < constrained < refused. Never downgrade.
    private static string Escalate(string current, string candidate)
        => Rank(candidate) > Rank(current) ? candidate : current;

    private static int Rank(string status) => status switch
    {
        SafetyStatus.Allowed => 0,
        SafetyStatus.Warning => 1,
        SafetyStatus.Constrained => 2,
        SafetyStatus.Refused => 3,
        _ => 0,
    };
}
