namespace BioStack.Cognition;

using BioStack.Cognition.Models;
using Keon.Collective;

/// <summary>
/// Translates a BioStack StackDeliberationEnvelope into deliberation inputs
/// for the keon.collective orchestrator.
///
/// DOCTRINE: Translator is a translator ONLY.
/// It does NOT call MCP gateway, runtime, governed-execute, or any effect surface.
/// It does NOT set IsEffectBearing = true on any artifact it builds.
/// </summary>
public interface IStackDeliberationTranslator
{
    StackDeliberationInputs Translate(
        StackDeliberationEnvelope envelope,
        TenantContext tenant,
        ActorContext actor,
        CorrelationContext correlation);
}

/// <summary>
/// Deliberation inputs produced by the translator, ready for the orchestrator.
/// </summary>
public sealed record StackDeliberationInputs(
    CollectiveIntent Intent,
    TemporalEchoBranch SeedBranch,
    ClaimGraph ClaimGraph,
    IReadOnlyList<BranchCollapseRecord> HistoricalCollapses);
