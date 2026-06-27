namespace BioStack.Application.ProtocolIntelligence;

public interface IProtocolIntelligenceGate
{
    PromotionGateResult Evaluate(PromotionGateRequest request);
}

public sealed class ProtocolIntelligenceGate : IProtocolIntelligenceGate
{
    private static readonly string[] UserFacingScanFields =
    [
        "userFacingExplanation",
        "summary",
        "rationale",
        "boundaryText",
        "userFacingBoundary",
        "boundary"
    ];

    private static readonly string[] HumanReviewSignals =
    [
        "high_risk", "regulatory", "safety", "prescription", "hormone", "glp1", "glp-1",
        "sarm", "serm", "peptide", "source_quality", "source-quality", "adverse", "contradiction",
        "gray_market", "research_chemical", "compounded", "banned_in_sport"
    ];

    private readonly IProtocolIntelligenceArtifactLoader _loader;
    private readonly IForbiddenOutputScanner _scanner;

    public ProtocolIntelligenceGate(
        IProtocolIntelligenceArtifactLoader loader,
        IForbiddenOutputScanner scanner)
    {
        _loader = loader;
        _scanner = scanner;
    }

    public PromotionGateResult Evaluate(PromotionGateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var artifacts = _loader.Load();
        var blockingReasons = new List<string>();
        var missing = new List<string>();
        var forbiddenMatches = new HashSet<string>(StringComparer.Ordinal);

        if (!artifacts.PromotionTargets.TryGetValue(request.ArtifactType, out var target))
        {
            return new PromotionGateResult(false, ["unknown_artifact_type"], [], [], false);
        }

        foreach (var field in target.RequiredFields)
        {
            if (!IsPresent(request.Artifact, field))
            {
                missing.Add(field);
            }
        }

        if (request.ArtifactType == "relationship_artifact")
        {
            foreach (var field in new[] { "evidenceTier", "confidence", "reviewStatus", "userFacingExplanation" })
            {
                if (!IsPresent(request.Artifact, field) && !missing.Contains(field, StringComparer.Ordinal))
                {
                    missing.Add(field);
                }
            }

            if (!HasAny(request.Artifact, "sourceRefs") && !missing.Contains("sourceRefs", StringComparer.Ordinal))
            {
                missing.Add("sourceRefs");
            }
        }

        var reviewStatus = GetString(request.Artifact, "reviewStatus");
        var requiresHumanReview = RequiresHumanReview(request, target);
        if (!string.Equals(reviewStatus, "approved", StringComparison.Ordinal))
        {
            blockingReasons.Add("review_status_not_approved");
            if (requiresHumanReview)
            {
                blockingReasons.Add("human_review_required");
            }
        }

        if (missing.Count > 0)
        {
            blockingReasons.Add("required_fields_missing");
        }

        if (target.ForbiddenOutputScanRequired)
        {
            foreach (var field in UserFacingScanFields)
            {
                foreach (var match in _scanner.Scan(GetString(request.Artifact, field)))
                {
                    forbiddenMatches.Add(match);
                }
            }
        }

        if (forbiddenMatches.Count > 0)
        {
            blockingReasons.Add("forbidden_output_match");
        }

        return new PromotionGateResult(
            CanPromote: blockingReasons.Count == 0,
            BlockingReasons: blockingReasons.Distinct(StringComparer.Ordinal).ToArray(),
            RequiredFieldsMissing: missing.Distinct(StringComparer.Ordinal).ToArray(),
            ForbiddenOutputMatches: forbiddenMatches.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            RequiresHumanReview: requiresHumanReview);
    }

    private static bool RequiresHumanReview(PromotionGateRequest request, PromotionTargetContract target)
    {
        if (target.ReviewGate.Contains("human_review", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var signalText = string.Join(" ", request.ClaimTags ?? [], request.ArtifactType, string.Join(" ", request.Artifact.Values.Select(ValueToString)));
        return HumanReviewSignals.Any(signal => signalText.Contains(signal, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPresent(Dictionary<string, object?> artifact, string key)
    {
        if (!artifact.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        if (value is string text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        if (value is System.Collections.IEnumerable && value is not string)
        {
            return true;
        }

        return true;
    }

    private static bool HasAny(Dictionary<string, object?> artifact, string key)
    {
        if (!artifact.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        if (value is string text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (!string.IsNullOrWhiteSpace(ValueToString(item)))
                {
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    private static string? GetString(Dictionary<string, object?> artifact, string key)
        => artifact.TryGetValue(key, out var value) ? ValueToString(value) : null;

    private static string ValueToString(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return text;
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            return string.Join(" ", enumerable.Cast<object?>().Select(ValueToString));
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
