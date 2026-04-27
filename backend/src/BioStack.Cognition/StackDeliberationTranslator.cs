namespace BioStack.Cognition;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioStack.Cognition.Models;
using Keon.Collective;

/// <summary>
/// Translates a BioStack <see cref="StackDeliberationEnvelope"/> into the
/// keon.collective deliberation input types.
///
/// DOCTRINE (invariants):
///   - Every ClaimNode.IsEffectBearing  == false
///   - Every AssumptionRef.IsEffectBearing == false
///   - No call to any effect surface, gateway, or executor.
/// </summary>
public sealed class StackDeliberationTranslator : IStackDeliberationTranslator
{
    public StackDeliberationInputs Translate(
        StackDeliberationEnvelope envelope,
        TenantContext tenant,
        ActorContext actor,
        CorrelationContext correlation)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var compoundsCommaList = string.Join(", ", envelope.Compounds.Select(c => c.DisplayName));
        var now = DateTime.UtcNow;

        // ── Intent ───────────────────────────────────────────────────────────
        var intent = new CollectiveIntent(
            IntentId: new IntentId(Guid.NewGuid().ToString("N")),
            Goal: $"BioStack deliberation: {compoundsCommaList} for goal={envelope.Goal}",
            IntentPayloadJson: JsonSerializer.Serialize(envelope),
            TenantContext: tenant,
            ActorContext: actor,
            CorrelationContext: correlation);

        // ── Claim graph ──────────────────────────────────────────────────────
        var (nodes, edges) = BuildClaimGraph(envelope, intent, now);
        var claimGraph = new ClaimGraph(nodes, edges);

        // ── Seed branch ──────────────────────────────────────────────────────
        var stackHash = ComputeStackHash(envelope);
        var seedBranch = BuildSeedBranch(envelope, intent, nodes, stackHash, now);

        // ── Historical collapses (pattern memory) ────────────────────────────
        var slugSet = envelope.Compounds.Select(c => c.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var historicalCollapses = BuildHistoricalCollapses(envelope, intent, seedBranch.BranchId, slugSet, now);

        return new StackDeliberationInputs(intent, seedBranch, claimGraph, historicalCollapses);
    }

    // ── Seed branch ──────────────────────────────────────────────────────────
    private static TemporalEchoBranch BuildSeedBranch(
        StackDeliberationEnvelope envelope,
        CollectiveIntent intent,
        IReadOnlyList<ClaimNode> nodes,
        string stackHash,
        DateTime now)
    {
        _ = now; // reserved for future use

        var claimRefs = nodes.Select(n => n.ClaimId).ToList();
        var planPayload = JsonSerializer.Serialize(new
        {
            compounds = envelope.Compounds,
            goal = envelope.Goal,
            pathways = envelope.Pathways,
        });

        return new TemporalEchoBranch(
            BranchId: $"biostack-stack-{stackHash}",
            Hypothesis: BuildHypothesis(envelope),
            PlanPayloadJson: planPayload,
            UtilityScore: ComputeUtilityScore(envelope),
            RiskScore: ComputeRiskScore(envelope),
            ClaimRefs: claimRefs,
            Participants: [],
            State: TemporalEchoState.Evaluated,
            LineageDepth: 0);
    }

    // ── Claim graph ──────────────────────────────────────────────────────────
    private static (List<ClaimNode> nodes, List<ClaimEdge> edges) BuildClaimGraph(
        StackDeliberationEnvelope envelope,
        CollectiveIntent intent,
        DateTime now)
    {
        var nodes = envelope.DeterministicFindings.Select(f => BuildClaimNode(f, envelope, intent, now)).ToList();
        var edges = BuildEdges(envelope.DeterministicFindings, nodes);
        return (nodes, edges);
    }

    private static ClaimNode BuildClaimNode(
        DeterministicFinding f,
        StackDeliberationEnvelope envelope,
        CollectiveIntent intent,
        DateTime now)
    {
        var assumptions = envelope.MissingInputs
            .Select(input => new AssumptionRef(
                Description: input,
                IsEffectBearing: false,     // INVARIANT
                ResolvingClaimId: null))
            .ToList();

        var evidenceRefs = new List<EvidenceRef>
        {
            new EvidenceRef(
                new EvidenceRefId($"evi::{f.FindingId}"),
                SourceKind: $"kb-tier-{f.EvidenceTier}",
                CanonicalReference: $"biostack://finding/{f.FindingId}",
                ReferencedAtUtc: now),
        };

        return new ClaimNode(
            ClaimId: new ClaimId($"finding::{f.FindingId}"),
            OwningBranchId: $"biostack-stack-{ComputeStackHashFromEnvelope(envelope)}",
            IntentId: intent.IntentId,
            Content: f.Narrative,
            EvidenceRefs: evidenceRefs,
            Assumptions: assumptions,
            IsEffectBearing: false,         // INVARIANT
            CreatedUtc: now);
    }

    private static List<ClaimEdge> BuildEdges(
        IReadOnlyList<DeterministicFinding> findings,
        IReadOnlyList<ClaimNode> nodes)
    {
        var edges = new List<ClaimEdge>();
        var nodeById = nodes.ToDictionary(n => n.ClaimId.Value.Replace("finding::", ""));

        for (int i = 0; i < findings.Count; i++)
        {
            var fi = findings[i];

            // Qualifies (Refines) edge
            if (fi.QualifiesFindingId is { } qualifies && nodeById.TryGetValue(qualifies, out var qualTarget))
                edges.Add(new ClaimEdge(nodes[i].ClaimId, qualTarget.ClaimId, ClaimEdgeKind.Refines));

            // Conflicts (Challenges) edge
            if (fi.ConflictsWithFindingId is { } conflicts && nodeById.TryGetValue(conflicts, out var conflictTarget))
                edges.Add(new ClaimEdge(nodes[i].ClaimId, conflictTarget.ClaimId, ClaimEdgeKind.Challenges));

            // Supports edges: findings sharing pathway tags
            for (int j = i + 1; j < findings.Count; j++)
            {
                var fj = findings[j];
                if (fi.PathwayTags.Any(t => fj.PathwayTags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                    edges.Add(new ClaimEdge(nodes[i].ClaimId, nodes[j].ClaimId, ClaimEdgeKind.Supports));
            }
        }

        return edges;
    }

    // ── Historical collapses (pattern memory) ────────────────────────────────
    private static List<BranchCollapseRecord> BuildHistoricalCollapses(
        StackDeliberationEnvelope envelope,
        CollectiveIntent intent,
        string branchId,
        HashSet<string> slugSet,
        DateTime now)
    {
        return envelope.KnownPatterns
            .Where(p => p.MatchedCompoundSlugs.All(s => slugSet.Contains(s)))
            .Select(p => new BranchCollapseRecord(
                CollapseId: new CollapseId($"biostack-pattern::{p.PatternId}"),
                IntentId: intent.IntentId,
                CandidateBranchIds: [branchId],
                SelectedBranchId: branchId,
                Disposition: BranchCollapseDisposition.Contested,
                SelectionRationale: $"BioStackKnownPattern: {p.Name} — {p.Description}",
                ComparativeHeatSummary: "synthesized from BioStack pattern memory",
                ComparativeUtilitySummary: "synthesized from BioStack pattern memory",
                ChallengeSummary: "non-executable pattern memory entry",
                WitnessDigestId: null,
                TimestampUtc: now))
            .ToList();
    }

    // ── Scoring helpers ──────────────────────────────────────────────────────
    private static decimal ComputeUtilityScore(StackDeliberationEnvelope envelope)
    {
        var raw = envelope.DeterministicFindings.Sum(f => f.UtilityScoreContribution);
        return Math.Clamp(raw, 0m, 1m);
    }

    private static decimal ComputeRiskScore(StackDeliberationEnvelope envelope)
    {
        var findingRisk = envelope.DeterministicFindings.Sum(f => f.RiskScoreContribution);
        var missingPenalty = Math.Min(0.30m, 0.10m * envelope.MissingInputs.Count);
        var providerPenalty = 0.20m * envelope.ProviderReviewPressure;
        var tierPenalty = envelope.EvidenceTiers.Values.Sum(t => TierPenalty(t));
        return Math.Clamp(findingRisk + missingPenalty + providerPenalty + tierPenalty, 0m, 1m);
    }

    private static decimal TierPenalty(EvidenceTier tier) => tier switch
    {
        EvidenceTier.None      =>  0.20m,
        EvidenceTier.Anecdotal =>  0.10m,
        EvidenceTier.Limited   =>  0.05m,
        EvidenceTier.Moderate  =>  0.00m,
        EvidenceTier.Strong    =>  0.00m, // -0.05 capped at 0
        _                      =>  0.00m,
    };

    // ── Utility helpers ──────────────────────────────────────────────────────
    private static string BuildHypothesis(StackDeliberationEnvelope envelope)
    {
        var names = string.Join(" + ", envelope.Compounds.Select(c => c.DisplayName));
        return $"{names} stack oriented to {envelope.Goal}";
    }

    private static string ComputeStackHash(StackDeliberationEnvelope envelope) =>
        ComputeStackHashFromEnvelope(envelope);

    private static string ComputeStackHashFromEnvelope(StackDeliberationEnvelope envelope)
    {
        var key = string.Concat(
            envelope.Goal.ToLowerInvariant(),
            string.Concat(envelope.Compounds.OrderBy(c => c.Slug).Select(c => c.Slug)));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
    }
}
