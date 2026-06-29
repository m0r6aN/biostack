namespace BioStack.Application.Services;

using BioStack.Application.Governance;

/// <summary>
/// KE-4: Supported evidence tier code constants for transcript source metadata.
/// These are string keys stored in SourceMetadata["evidenceTier"] — distinct from
/// the domain EvidenceTier enum used on KnowledgeEntry.
/// </summary>
public static class EvidenceTierCode
{
    public const string Observational = "observational";
    public const string Mechanistic = "mechanistic";
    public const string ClinicalStudy = "clinical_study";
    public const string SystematicReview = "systematic_review";
    public const string Rct = "rct";
}

/// <summary>
/// Input value object for the evidence gate.
/// </summary>
public sealed record EvidenceGateRequest(
    string ReviewState,
    string? TargetCanonicalName,
    bool IsDeterministicFixture,
    IReadOnlyDictionary<string, string> SourceMetadata);

/// <summary>
/// Result value object returned by the evidence gate.
/// IsGateOpen == true means the record may proceed to promotion.
/// </summary>
public sealed record EvidenceGateResult(
    bool IsGateOpen,
    string? RejectionCode,
    string? RejectionReason);

/// <summary>
/// KE-4 Evidence Gate contract.
/// Stateless, deterministic, fail-closed.
/// </summary>
public interface IEvidenceGate
{
    EvidenceGateResult Evaluate(EvidenceGateRequest request);
}

/// <summary>
/// Thrown when the evidence gate rejects a promotion candidate.
/// Inherits from InvalidOperationException so the existing
/// catch (InvalidOperationException) → 409 handler in AdminEndpoints
/// picks it up automatically — no endpoint changes required.
/// </summary>
public sealed class EvidenceGateViolationException : InvalidOperationException
{
    public string RejectionCode { get; }

    public EvidenceGateViolationException(string rejectionCode, string message)
        : base(message)
    {
        RejectionCode = rejectionCode;
    }
}

/// <summary>
/// KE-4 Evidence Gate implementation.
/// Validates that a staged transcript candidate review record
/// carries sufficient evidence metadata before promotion is allowed.
///
/// Checks (in order, fail-closed):
///   1. Not a deterministic fixture
///   2. ReviewState == review_approved_for_promotion
///   3. TargetCanonicalName is set
///   4. 'evidenceTier' key present and non-empty
///   5. evidenceTier is a supported tier
///   6. 'citations' key present and non-empty
///   7. Tiers requiring mechanism: 'mechanismSummary' present and non-empty
///   8. No banned safety-recommendation language in scanned text fields
/// </summary>
public sealed class EvidenceGate : IEvidenceGate
{
    private static readonly IReadOnlySet<string> SupportedTiers = new HashSet<string>(StringComparer.Ordinal)
    {
        EvidenceTierCode.Observational,
        EvidenceTierCode.Mechanistic,
        EvidenceTierCode.ClinicalStudy,
        EvidenceTierCode.SystematicReview,
        EvidenceTierCode.Rct,
    };

    /// <summary>
    /// Tiers for which a mechanismSummary is mandatory.
    /// </summary>
    private static readonly IReadOnlySet<string> TiersRequiringMechanism = new HashSet<string>(StringComparer.Ordinal)
    {
        EvidenceTierCode.Mechanistic,
        EvidenceTierCode.ClinicalStudy,
        EvidenceTierCode.SystematicReview,
        EvidenceTierCode.Rct,
    };

    /// <summary>
    /// Metadata keys whose values are scanned for unsafe recommendation language.
    /// </summary>
    private static readonly string[] SafetyCheckedKeys = ["mechanismSummary", "rationaleText", "summary"];

    // DoctrineSanitizer is stateless; allocate once per gate instance.
    private static readonly DoctrineSanitizer Sanitizer = new();

    public EvidenceGateResult Evaluate(EvidenceGateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check 1 — fixture guard.
        if (request.IsDeterministicFixture)
        {
            return Reject(
                "deterministic_fixture_not_promotable",
                "Deterministic fixture records cannot pass the evidence gate.");
        }

        // Check 2 — review state guard.
        if (!string.Equals(
                request.ReviewState,
                TranscriptCandidateReviewState.ReviewApprovedForPromotion,
                StringComparison.Ordinal))
        {
            return Reject(
                "review_state_not_approved",
                $"Evidence gate requires review state '{TranscriptCandidateReviewState.ReviewApprovedForPromotion}'. " +
                $"Current: '{request.ReviewState}'.");
        }

        // Check 3 — target assigned guard.
        if (string.IsNullOrWhiteSpace(request.TargetCanonicalName))
        {
            return Reject(
                "promotion_target_not_assigned",
                "Evidence gate requires a promotion target to be assigned.");
        }

        // Check 4 — evidenceTier present.
        request.SourceMetadata.TryGetValue("evidenceTier", out var evidenceTier);
        if (string.IsNullOrWhiteSpace(evidenceTier))
        {
            return Reject(
                "missing_evidence_tier",
                "Evidence gate requires 'evidenceTier' in source metadata.");
        }

        // Check 5 — evidenceTier is supported.
        if (!SupportedTiers.Contains(evidenceTier))
        {
            return Reject(
                "unsupported_evidence_tier",
                $"Evidence tier '{evidenceTier}' is not a supported evidence tier.");
        }

        // Check 6 — at least one citation.
        request.SourceMetadata.TryGetValue("citations", out var citations);
        if (string.IsNullOrWhiteSpace(citations))
        {
            return Reject(
                "missing_citations",
                "Evidence gate requires at least one citation in source metadata key 'citations'.");
        }

        // Check 7 — mechanism required for certain tiers.
        if (TiersRequiringMechanism.Contains(evidenceTier))
        {
            request.SourceMetadata.TryGetValue("mechanismSummary", out var mechanism);
            if (string.IsNullOrWhiteSpace(mechanism))
            {
                return Reject(
                    "missing_mechanism_summary",
                    $"Evidence tier '{evidenceTier}' requires a 'mechanismSummary' in source metadata.");
            }
        }

        // Check 8 — safety language scan.
        foreach (var key in SafetyCheckedKeys)
        {
            if (!request.SourceMetadata.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text))
                continue;

            if (Sanitizer.ContainsBannedPhrase(text))
            {
                return Reject(
                    "unsafe_recommendation_language",
                    $"Source metadata field '{key}' contains unsafe recommendation language.");
            }
        }

        return new EvidenceGateResult(IsGateOpen: true, RejectionCode: null, RejectionReason: null);
    }

    private static EvidenceGateResult Reject(string code, string reason)
        => new(IsGateOpen: false, RejectionCode: code, RejectionReason: reason);
}
