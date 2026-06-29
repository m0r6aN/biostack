namespace BioStack.Application.Services;

using BioStack.Application.ProtocolIntelligence;
using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;

public interface IProtocolIntelligenceService
{
    ProtocolIntelligenceResponse BuildResponse(
        Guid protocolId,
        ProductTier tier,
        IReadOnlyList<ProtocolIntelligenceReviewedArtifact> reviewedArtifacts);

    ProtocolIntelligenceContractsResponse GetContracts();
}

public sealed class ProtocolIntelligenceService : IProtocolIntelligenceService
{
    private readonly IProtocolIntelligenceGate _gate;
    private readonly IForbiddenOutputScanner _scanner;

    public ProtocolIntelligenceService(
        IProtocolIntelligenceGate gate,
        IForbiddenOutputScanner scanner)
    {
        _gate = gate;
        _scanner = scanner;
    }

    public ProtocolIntelligenceResponse BuildResponse(
        Guid protocolId,
        ProductTier tier,
        IReadOnlyList<ProtocolIntelligenceReviewedArtifact> reviewedArtifacts)
    {
        _ = protocolId;
        var phaseMap = new List<ProtocolIntelligencePhaseMapItem>();
        var relationships = new List<ProtocolIntelligenceRelationshipCard>();
        var ambiguitySignals = new List<ProtocolIntelligenceAmbiguitySignal>();
        var sourceQualityWarnings = new List<ProtocolIntelligenceSourceQualityWarning>();
        var highRiskWarnings = new List<ProtocolIntelligenceHighRiskWarning>();
        var unknowns = new List<string>();
        var upgradeHooks = new List<ProtocolIntelligenceUpgradeHook>();
        var safetyNotes = new List<string>
        {
            "Protocol Intelligence is educational and observational.",
            "Unknown means BioStack does not have a reviewed artifact for this context.",
            "Discuss concerning symptoms, medications, lab results, or high-risk substances with a qualified professional."
        };

        foreach (var artifact in reviewedArtifacts)
        {
            var gateResult = _gate.Evaluate(new PromotionGateRequest(artifact.ArtifactType, artifact.Artifact));
            if (!gateResult.CanPromote)
            {
                continue;
            }

            if (ContainsForbiddenText(artifact.Artifact))
            {
                continue;
            }

            switch (artifact.ArtifactType)
            {
                case "phase_event":
                    if (tier >= ProductTier.Operator)
                    {
                        phaseMap.Add(ToPhaseMap(artifact.Artifact));
                    }
                    else
                    {
                        AddOperatorHook(upgradeHooks, "protocol_phase_map", "Unlock phase maps with Operator.");
                    }
                    break;
                case "relationship_artifact":
                    if (tier >= ProductTier.Operator)
                    {
                        relationships.Add(ToRelationship(artifact.Artifact));
                    }
                    else
                    {
                        AddOperatorHook(upgradeHooks, "reviewed_relationship_graph", "Unlock reviewed protocol relationships and source-quality context with Operator.");
                    }
                    break;
                case "source_quality_artifact":
                    if (tier >= ProductTier.Operator)
                    {
                        sourceQualityWarnings.Add(ToSourceQuality(artifact.Artifact));
                    }
                    else
                    {
                        sourceQualityWarnings.Add(ToLimitedSourceQuality(artifact.Artifact));
                        AddOperatorHook(upgradeHooks, "source_quality_tracker", "Unlock reviewed protocol relationships and source-quality context with Operator.");
                    }
                    break;
                case "side_effect_ambiguity_artifact":
                    if (tier >= ProductTier.Commander)
                    {
                        ambiguitySignals.Add(ToAmbiguity(artifact.Artifact));
                    }
                    else
                    {
                        AddCommanderHook(upgradeHooks, "side_effect_ambiguity_detector", "Unlock side-effect ambiguity and longitudinal Protocol Intelligence with Commander.");
                    }
                    break;
                case "high_risk_warning_artifact":
                    highRiskWarnings.Add(ToHighRisk(artifact.Artifact));
                    break;
            }
        }

        if (phaseMap.Count == 0 && relationships.Count == 0 && ambiguitySignals.Count == 0 &&
            sourceQualityWarnings.Count == 0 && highRiskWarnings.Count == 0)
        {
            unknowns.Add("No reviewed Protocol Intelligence artifact exists for this protocol context.");
        }

        var status = unknowns.Count > 0 ? "Unknown" : "Available";
        return new ProtocolIntelligenceResponse(
            Status: status,
            PhaseMap: phaseMap,
            Relationships: relationships,
            AmbiguitySignals: ambiguitySignals,
            SourceQualityWarnings: sourceQualityWarnings,
            HighRiskWarnings: highRiskWarnings,
            Unknowns: unknowns,
            SafetyNotes: safetyNotes,
            UpgradeHooks: upgradeHooks.DistinctBy(hook => hook.FeatureCode).ToArray());
    }

    public ProtocolIntelligenceContractsResponse GetContracts()
    {
        if (_gate is not ProtocolIntelligenceGate)
        {
            return new ProtocolIntelligenceContractsResponse(
                new Dictionary<string, string>(),
                [],
                [],
                []);
        }

        var loader = new ProtocolIntelligenceArtifactLoader();
        var artifacts = loader.Load();
        return new ProtocolIntelligenceContractsResponse(
            artifacts.ArtifactVersions,
            artifacts.SupportedRelationshipIds,
            artifacts.AllBlockedOutputs.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            artifacts.AvailableObservabilityModules);
    }

    private bool ContainsForbiddenText(Dictionary<string, object?> artifact)
        => artifact.Values.Select(ValueToString).Any(value => _scanner.Scan(value).Count > 0);

    private static ProtocolIntelligencePhaseMapItem ToPhaseMap(Dictionary<string, object?> artifact)
        => new(
            Phase: GetString(artifact, "phaseId", "unknown"),
            Label: GetString(artifact, "observedChange", "Reviewed phase event"),
            EvidenceTier: GetString(artifact, "evidenceTier", "unknown"),
            Confidence: GetString(artifact, "confidence", "unknown"),
            SourceRefsCount: CountItems(artifact, "sourceRefs"),
            ReviewStatus: GetString(artifact, "reviewStatus", "approved"),
            UserFacingBoundary: Boundary(artifact));

    private static ProtocolIntelligenceRelationshipCard ToRelationship(Dictionary<string, object?> artifact)
        => new(
            RelationshipType: GetString(artifact, "relationshipType", "unknown"),
            Subject: GetString(artifact, "subject", "unknown"),
            Object: GetString(artifact, "object", "unknown"),
            EvidenceTier: GetString(artifact, "evidenceTier", "unknown"),
            Confidence: GetString(artifact, "confidence", "unknown"),
            SourceRefsCount: CountItems(artifact, "sourceRefs"),
            ReviewStatus: GetString(artifact, "reviewStatus", "approved"),
            UserFacingExplanation: GetString(artifact, "userFacingExplanation", "Reviewed relationship."),
            UserFacingBoundary: Boundary(artifact));

    private static ProtocolIntelligenceAmbiguitySignal ToAmbiguity(Dictionary<string, object?> artifact)
        => new(
            SymptomOrOutcome: GetString(artifact, "symptomOrOutcome", "unknown"),
            OnsetWindow: GetString(artifact, "onsetWindow", "unknown"),
            RecentChanges: GetStringArray(artifact, "recentChanges"),
            OverlapDomains: GetStringArray(artifact, "overlapDomains"),
            EvidenceTier: GetString(artifact, "evidenceTier", "unknown"),
            Confidence: GetString(artifact, "confidence", "unknown"),
            SourceRefsCount: CountItems(artifact, "sourceRefs"),
            ReviewStatus: GetString(artifact, "reviewStatus", "approved"),
            UserFacingBoundary: Boundary(artifact));

    private static ProtocolIntelligenceSourceQualityWarning ToSourceQuality(Dictionary<string, object?> artifact)
        => new(
            Subject: GetString(artifact, "subject", "unknown"),
            SourceClass: GetString(artifact, "sourceClass", "unknown"),
            BlockedOutputs: GetStringArray(artifact, "blockedOutputs"),
            EvidenceTier: GetString(artifact, "evidenceTier", "source_dependent"),
            Confidence: GetString(artifact, "confidence", GetString(artifact, "identityConfidence", "unknown")),
            SourceRefsCount: Math.Max(CountItems(artifact, "sourceRefs"), CountItems(artifact, "authorityRefs")),
            ReviewStatus: GetString(artifact, "reviewStatus", "approved"),
            UserFacingBoundary: Boundary(artifact));

    private static ProtocolIntelligenceSourceQualityWarning ToLimitedSourceQuality(Dictionary<string, object?> artifact)
        => ToSourceQuality(artifact) with
        {
            Confidence = "unknown",
            EvidenceTier = "unknown",
            SourceRefsCount = 0,
            UserFacingBoundary = "Source-quality warning available; detailed reviewed context requires Operator."
        };

    private static ProtocolIntelligenceHighRiskWarning ToHighRisk(Dictionary<string, object?> artifact)
        => new(
            Category: GetString(artifact, "category", "high_risk"),
            RequiredWarnings: GetStringArray(artifact, "requiredWarnings"),
            BlockedOutputs: GetStringArray(artifact, "blockedOutputs"),
            EvidenceTier: GetString(artifact, "evidenceTier", "warning_first"),
            Confidence: GetString(artifact, "confidence", "reviewed"),
            SourceRefsCount: CountItems(artifact, "sourceRefs"),
            ReviewStatus: GetString(artifact, "reviewStatus", "approved"),
            UserFacingBoundary: Boundary(artifact));

    private static void AddOperatorHook(List<ProtocolIntelligenceUpgradeHook> hooks, string featureCode, string message)
        => hooks.Add(new ProtocolIntelligenceUpgradeHook("Operator", featureCode, message));

    private static void AddCommanderHook(List<ProtocolIntelligenceUpgradeHook> hooks, string featureCode, string message)
        => hooks.Add(new ProtocolIntelligenceUpgradeHook("Commander", featureCode, message));

    private static string Boundary(Dictionary<string, object?> artifact)
        => GetString(artifact, "userFacingBoundary", GetString(artifact, "boundaryText", "Observation prompt only."));

    private static string GetString(Dictionary<string, object?> artifact, string key, string fallback)
        => artifact.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(ValueToString(value))
            ? ValueToString(value)
            : fallback;

    private static IReadOnlyList<string> GetStringArray(Dictionary<string, object?> artifact, string key)
    {
        if (!artifact.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        if (value is string text)
        {
            return string.IsNullOrWhiteSpace(text) ? [] : [text];
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object?>()
                .Select(ValueToString)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();
        }

        return [ValueToString(value)];
    }

    private static int CountItems(Dictionary<string, object?> artifact, string key)
        => GetStringArray(artifact, key).Count;

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
