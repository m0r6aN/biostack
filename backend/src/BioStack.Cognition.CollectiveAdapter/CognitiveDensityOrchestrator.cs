namespace Keon.Collective;

/// <summary>
/// Rule-based stub orchestrator for keon.collective cognitive-density v1.
/// Implements four perspective reviewers (Optimizer, Skeptic, Regulator, Historian)
/// and a non-executable ContradictionReview.
/// Replace with LLM-backed implementation when the Collective runtime is available.
/// </summary>
internal sealed class CognitiveDensityOrchestrator : ICognitiveDensityOrchestrator
{
    private const string BioStackPatternPrefix = "BioStackKnownPattern: ";

    public Task<CognitiveDensityEnvelope> RunAsync(
        CollectiveIntent intent,
        TemporalEchoBranch? seedBranch = null,
        BranchRefinementOptions? refinementOptions = null,
        ClaimGraph? claimGraph = null,
        IReadOnlyList<BranchCollapseRecord>? historicalCollapses = null,
        CancellationToken ct = default)
    {
        var nodes  = claimGraph?.Nodes  ?? [];
        var collapses = historicalCollapses ?? [];
        var risk   = seedBranch?.RiskScore  ?? 0m;
        var planJson = seedBranch?.PlanPayloadJson ?? string.Empty;

        var perspectives = new Dictionary<PerspectiveKind, PerspectiveReview>
        {
            [PerspectiveKind.Optimizer]  = BuildOptimizer(intent, planJson, nodes),
            [PerspectiveKind.Skeptic]    = BuildSkeptic(nodes),
            [PerspectiveKind.Regulator]  = BuildRegulator(nodes, risk),
            [PerspectiveKind.Historian]  = BuildHistorian(collapses),
        };

        var nodeCount = nodes.Count;
        var edgeCount = claimGraph?.Edges?.Count ?? 0;

        var envelope = new CognitiveDensityEnvelope(
            BranchPerspectiveReview: new BranchPerspectiveReview(perspectives),
            ContradictionReview: new ContradictionReview(
                CounterPlanNarrative: "No executable counter-position generated. This is commentary only.",
                CounterPlanIsExecutable: false,
                IsExecutable: false),
            ConfidenceProfile: new ConfidenceProfile(
                Model: "keon.collective-stub-v1",
                Epistemic: EpistemicLabel(risk),
                EvidenceSupport: EvidenceSupportLabel(nodes),
                ContradictionDensity: "low",
                CalibrationVersion: "1.0.0"),
            ReasoningGraphRef: new ReasoningGraphRef(
                GraphId: $"rg::{intent.IntentId.Value}",
                NodeCount: nodeCount,
                EdgeCount: edgeCount));

        return Task.FromResult(envelope);
    }

    // ── Optimizer ─────────────────────────────────────────────────────────────
    private static PerspectiveReview BuildOptimizer(
        CollectiveIntent intent, string planJson, IReadOnlyList<ClaimNode> nodes)
    {
        var findings = new List<PerspectiveFinding>();

        // OPT-001: goal alignment — always emitted
        findings.Add(new PerspectiveFinding("OPT-001", "GoalAlignment",
            $"Stack deliberation intent '{intent.Goal}' carries {nodes.Count} evidence-tagged claim(s). " +
            "Deliberation inputs are complete for commentary review.",
            FindingSeverity.Info));

        // OPT-003: PlanCompleteness — only emitted when PlanPayloadJson is absent/empty.
        // The translator MUST set PlanPayloadJson; if it does, this finding is suppressed.
        if (string.IsNullOrWhiteSpace(planJson))
        {
            findings.Add(new PerspectiveFinding("OPT-003", "PlanCompleteness",
                "PlanPayloadJson is absent. Sequencing and dose-form context cannot be evaluated.",
                FindingSeverity.Warning));
        }

        return new PerspectiveReview(PerspectiveKind.Optimizer, findings,
            "Goal-alignment and sequencing review complete.");
    }

    // ── Skeptic ───────────────────────────────────────────────────────────────
    private static PerspectiveReview BuildSkeptic(IReadOnlyList<ClaimNode> nodes)
    {
        var findings = new List<PerspectiveFinding>();

        foreach (var node in nodes)
        {
            foreach (var ev in node.EvidenceRefs)
            {
                if (ev.SourceKind.Contains("Anecdotal", StringComparison.OrdinalIgnoreCase) ||
                    ev.SourceKind.Contains("Limited",   StringComparison.OrdinalIgnoreCase) ||
                    ev.SourceKind.Contains("None",      StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new PerspectiveFinding("SKP-001", "AttributionRisk",
                        $"Claim '{node.ClaimId.Value}' carries {ev.SourceKind} evidence " +
                        $"(ref: {ev.CanonicalReference}). Attribution confidence is limited.",
                        FindingSeverity.Warning));
                    break; // one finding per node is sufficient
                }
            }
        }

        // Ensure at least one skeptic finding when there are any claims
        if (findings.Count == 0 && nodes.Count > 0)
        {
            findings.Add(new PerspectiveFinding("SKP-002", "EvidenceGap",
                "No strong evidence tiers detected across claim set. " +
                "All claims carry observational-only attribution.",
                FindingSeverity.Warning));
        }

        return new PerspectiveReview(PerspectiveKind.Skeptic, findings,
            "Evidence gap and attribution risk review complete.");
    }

    // ── Regulator ─────────────────────────────────────────────────────────────
    private static PerspectiveReview BuildRegulator(IReadOnlyList<ClaimNode> nodes, decimal riskScore)
    {
        var findings = new List<PerspectiveFinding>();

        if (riskScore > 0.4m)
        {
            findings.Add(new PerspectiveFinding("REG-001", "ProviderReviewPressure",
                $"Composite risk score {riskScore:P0} exceeds threshold. " +
                "This stack warrants provider review before use. " +
                "This is commentary; the deterministic safety panel is authoritative.",
                FindingSeverity.Warning));
        }

        foreach (var node in nodes.Where(n => n.Assumptions.Count > 0))
        {
            findings.Add(new PerspectiveFinding("REG-002", "ClaimRisk",
                $"Claim '{node.ClaimId.Value}' has {node.Assumptions.Count} unresolved assumption(s). " +
                "Claim confidence is reduced pending resolution.",
                FindingSeverity.Info));
        }

        return new PerspectiveReview(PerspectiveKind.Regulator, findings,
            "Claim risk and provider-review framing complete.");
    }

    // ── Historian ─────────────────────────────────────────────────────────────
    private static PerspectiveReview BuildHistorian(IReadOnlyList<BranchCollapseRecord> collapses)
    {
        var findings = new List<PerspectiveFinding>();

        var patternRecords = collapses
            .Where(c => c.SelectionRationale.StartsWith(BioStackPatternPrefix, StringComparison.Ordinal))
            .ToList();

        foreach (var record in patternRecords)
        {
            // Narrative intentionally surfaces the full rationale so downstream UI can
            // display pattern name and description (which contain "pairing", "regenerative", etc.)
            findings.Add(new PerspectiveFinding("HST-001", "PatternMemory",
                $"Pattern memory from BioStack: {record.SelectionRationale[BioStackPatternPrefix.Length..]}",
                FindingSeverity.Info));
        }

        if (findings.Count == 0)
        {
            findings.Add(new PerspectiveFinding("HST-000", "NoPatternHistory",
                "No BioStack known-pattern history matched this stack combination. " +
                "Pattern recognition for v1 relies on BioStack KnownPatterns rendered in the UI.",
                FindingSeverity.Info));
        }

        return new PerspectiveReview(PerspectiveKind.Historian, findings,
            "Pattern recognition and collapse history review complete.");
    }

    private static string EpistemicLabel(decimal risk) =>
        risk > 0.6m ? "uncertain" : risk > 0.3m ? "partial" : "moderate";

    private static string EvidenceSupportLabel(IReadOnlyList<ClaimNode> nodes)
    {
        var count = nodes.Sum(n => n.EvidenceRefs.Count);
        return count == 0 ? "none" : count < 3 ? "sparse" : "moderate";
    }
}
