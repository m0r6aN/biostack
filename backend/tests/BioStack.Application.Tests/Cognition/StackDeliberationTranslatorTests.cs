namespace BioStack.Application.Tests.Cognition;

using System.Reflection;
using BioStack.Cognition;
using BioStack.Cognition.Models;
using Keon.Collective;
using Xunit;

/// <summary>
/// Canonical fixture: BPC-157 + TB-500, goal = recovery.
/// Tests all 11 assertions from the Stack Review Board spec (tests 1–8 + 11).
/// Tests 9–10 are frontend UI tests in StackReviewBoard.test.tsx.
/// </summary>
public sealed class StackDeliberationTranslatorTests
{
    // ── Fixture ───────────────────────────────────────────────────────────────
    private static readonly StackDeliberationEnvelope Fixture = new(
        Goal: "recovery",
        Compounds:
        [
            new CompoundRef("bpc-157", "BPC-157", "Injectable", "Peptide"),
            new CompoundRef("tb-500",  "TB-500",  "Injectable", "Peptide"),
        ],
        Pathways: ["tissue-repair", "angiogenesis", "wound-healing"],
        EvidenceTiers: new Dictionary<string, EvidenceTier>
        {
            ["bpc-157"] = EvidenceTier.Anecdotal,
            ["tb-500"]  = EvidenceTier.Anecdotal,
        },
        DeterministicFindings:
        [
            new DeterministicFinding("f001", "SYN-001", "Synergy",
                "BPC-157 and TB-500 share tissue-repair pathway with complementary mechanisms.",
                ["bpc-157", "tb-500"], ["tissue-repair", "angiogenesis"],
                RiskScoreContribution: 0.05m, UtilityScoreContribution: 0.30m,
                EvidenceTier: EvidenceTier.Anecdotal,
                QualifiesFindingId: null, ConflictsWithFindingId: null),
            new DeterministicFinding("f002", "SAF-001", "ProviderReview",
                "Stack involves injectable peptides requiring provider review.",
                ["bpc-157", "tb-500"], ["tissue-repair", "administration"],
                RiskScoreContribution: 0.15m, UtilityScoreContribution: 0.10m,
                EvidenceTier: EvidenceTier.Anecdotal,
                QualifiesFindingId: null, ConflictsWithFindingId: null),
        ],
        KnownPatterns:
        [
            new KnownPattern(
                "bpc157-tb500-recovery",
                "BPC-157 + TB-500 Regenerative Pairing",
                ["bpc-157", "tb-500"],
                "Classic regenerative pairing combining BPC-157 and TB-500 for tissue recovery protocols."),
        ],
        MissingInputs: ["duration", "dose schedule"],
        ProviderReviewPressure: 0.75m,
        SafetyBoundaryText: "educational and observational only");

    private static readonly TenantContext Tenant      = new("test-tenant");
    private static readonly ActorContext  Actor       = new("test-actor");
    private static readonly CorrelationContext Corr   = new("test-corr");

    private static StackDeliberationTranslator MakeTranslator() => new();
    private static ICognitiveDensityOrchestrator MakeOrchestrator() =>
        (ICognitiveDensityOrchestrator)Activator.CreateInstance(
            typeof(ICognitiveDensityOrchestrator).Assembly
                .GetTypes()
                .Single(t => !t.IsInterface && !t.IsAbstract &&
                             t.IsAssignableTo(typeof(ICognitiveDensityOrchestrator))))!;

    // ── Tests 1 & 2: IsEffectBearing invariant ─────────────────────────────
    [Fact]
    public void T1_AllClaimNodes_IsEffectBearing_False()
    {
        var inputs = MakeTranslator().Translate(Fixture, Tenant, Actor, Corr);
        Assert.All(inputs.ClaimGraph.Nodes, n => Assert.False(n.IsEffectBearing,
            $"ClaimNode {n.ClaimId.Value} must have IsEffectBearing == false"));
    }

    [Fact]
    public void T2_AllAssumptionRefs_IsEffectBearing_False()
    {
        var inputs = MakeTranslator().Translate(Fixture, Tenant, Actor, Corr);
        var allAssumptions = inputs.ClaimGraph.Nodes.SelectMany(n => n.Assumptions);
        Assert.All(allAssumptions, a => Assert.False(a.IsEffectBearing,
            $"AssumptionRef '{a.Description}' must have IsEffectBearing == false"));
    }

    // ── Tests 3–8: Orchestrator output assertions ──────────────────────────
    private async Task<CognitiveDensityEnvelope> GetEnvelope()
    {
        var translator  = MakeTranslator();
        var orchestrator = MakeOrchestrator();
        var service = new StackReviewBoardService(translator, orchestrator);
        return await service.ReviewStackAsync(Fixture);
    }

    [Fact]
    public async Task T3_Historian_Contains_RegenerativeOrPairingOrPattern()
    {
        var envelope = await GetEnvelope();
        var historian = envelope.BranchPerspectiveReview.PerspectiveReviews[PerspectiveKind.Historian];
        var hasMatch = historian.Findings.Any(f =>
            ContainsAny(f.Narrative, "regenerative", "pairing", "pattern") ||
            ContainsAny(f.Category,  "regenerative", "pairing", "pattern"));
        Assert.True(hasMatch,
            $"Historian findings must mention 'regenerative', 'pairing', or 'pattern'. " +
            $"Found: {string.Join("; ", historian.Findings.Select(f => f.Narrative))}");
    }

    [Fact]
    public async Task T4_Optimizer_DoesNotEmit_OPT003()
    {
        var envelope = await GetEnvelope();
        var optimizer = envelope.BranchPerspectiveReview.PerspectiveReviews[PerspectiveKind.Optimizer];
        Assert.DoesNotContain(optimizer.Findings, f => f.FindingId == "OPT-003");
    }

    [Fact]
    public async Task T5_Skeptic_EmitsAtLeastOneFinding()
    {
        var envelope = await GetEnvelope();
        var skeptic = envelope.BranchPerspectiveReview.PerspectiveReviews[PerspectiveKind.Skeptic];
        Assert.NotEmpty(skeptic.Findings);
    }

    [Fact]
    public async Task T6_Regulator_EmitsAtLeastOneREGFinding_WhenProviderReviewPressureHigh()
    {
        // Fixture has ProviderReviewPressure = 0.75 > 0.5
        var envelope = await GetEnvelope();
        var regulator = envelope.BranchPerspectiveReview.PerspectiveReviews[PerspectiveKind.Regulator];
        var regCount = regulator.Findings.Count(f => f.FindingId.StartsWith("REG-"));
        Assert.True(regCount >= 1,
            $"Expected >= 1 REG-* findings when ProviderReviewPressure > 0.5, found {regCount}");
    }

    [Fact]
    public async Task T7_ContradictionReview_CounterPlanIsExecutable_False()
    {
        var envelope = await GetEnvelope();
        Assert.False(envelope.ContradictionReview.CounterPlanIsExecutable);
    }

    [Fact]
    public async Task T8_ContradictionReview_IsExecutable_False()
    {
        var envelope = await GetEnvelope();
        Assert.False(envelope.ContradictionReview.IsExecutable);
    }

    // ── Test 11: Boundary reflection — no forbidden effect surface names ───
    [Fact]
    public void T11_Translator_And_Service_Have_No_ForbiddenEffectMembers()
    {
        var cognitionAssembly = typeof(StackDeliberationTranslator).Assembly;
        var forbiddenPatterns = new[]
        {
            "Reality", "GatewayMock", "EffectRealization", "EffectEligibilityGate",
            "GovernedExecute", "ExecuteEffectAsync",
        };

        var types = new[] { typeof(StackDeliberationTranslator), typeof(StackReviewBoardService) };
        foreach (var type in types)
        {
            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Static |
                                          BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var member in members)
            {
                foreach (var pattern in forbiddenPatterns)
                {
                    Assert.False(member.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase),
                        $"{type.Name}.{member.Name} contains forbidden pattern '{pattern}'");
                }
            }
        }

        // Also assert the assembly itself has no types matching forbidden patterns
        var allTypes = cognitionAssembly.GetTypes();
        foreach (var t in allTypes)
        {
            foreach (var pattern in forbiddenPatterns)
            {
                Assert.False(t.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase),
                    $"Type '{t.FullName}' in BioStack.Cognition contains forbidden pattern '{pattern}'");
            }
        }
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
}
