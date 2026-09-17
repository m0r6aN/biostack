namespace BioStack.Application.Services;

using System.Collections.Generic;
using System.Linq;
using BioStack.Contracts.Responses;

/// <summary>
/// Single projection point for per-pair interaction reasoning (owner ruling 2026-09-16, B3:
/// "Per-pair interaction reasoning is gated behind reviewed_relationship_graph on every surface").
///
/// Every caller that returns <see cref="InteractionIntelligenceResponse"/> to a client — protocol
/// responses, current-stack intelligence, and any future surface — must route the value through this
/// projector instead of gating individually at the call site. With the entitlement, the full
/// reasoning-bearing shape passes through unchanged. Without it, the caller gets only pair names and
/// a severity value: no mechanism, direction, consequence, evidence narrative, or source text.
///
/// Callers must fail closed: if entitlement cannot be determined, pass hasReasoningAccess: false.
/// </summary>
public static class InteractionIntelligenceProjection
{
    /// <summary>
    /// Returns <paramref name="full"/> unchanged when <paramref name="hasReasoningAccess"/> is true,
    /// otherwise a <see cref="ReducedInteractionIntelligenceResponse"/> carrying only pair
    /// names/ids and severity. Declared to return <see cref="object"/> so callers can assign it
    /// directly to an <c>object</c>-typed response property and let System.Text.Json serialize
    /// whichever shape was actually produced.
    /// </summary>
    public static object Project(InteractionIntelligenceResponse full, bool hasReasoningAccess)
    {
        if (hasReasoningAccess)
        {
            return full;
        }

        var pairs = full.Interactions
            .Select(interaction => new InteractionPairSummaryResponse(
                interaction.CompoundA,
                interaction.CompoundB,
                interaction.Type))
            .ToList();

        return new ReducedInteractionIntelligenceResponse(full.Summary, pairs);
    }

    /// <summary>
    /// Fail-closed check for the reviewed_relationship_graph entitlement, shared by every caller
    /// that needs to decide whether to pass <c>true</c> into <see cref="Project"/> (owner ruling
    /// 2026-09-16, extended 2026-09-16 to the public
    /// <c>POST /api/v1/knowledge/interaction-check</c> surface: "The public view should definitely
    /// be the same as observed [Observer]"). Any exception while determining entitlement — no
    /// current-user context (the normal case for an anonymous caller), DB failure, anything —
    /// resolves to "no access" rather than propagating, so a caller that cannot prove entitlement
    /// always gets the reduced shape.
    /// </summary>
    public static async Task<bool> HasReasoningAccessAsync(IFeatureGate featureGate, CancellationToken cancellationToken)
    {
        try
        {
            return await featureGate.IsEnabledAsync(FeatureCodes.ReviewedRelationshipGraph, cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}
