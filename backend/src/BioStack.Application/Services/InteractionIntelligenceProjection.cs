namespace BioStack.Application.Services;

using System.Collections.Generic;
using System.Linq;
using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;

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

        // Positive and negative classifications are pair signals, not all hazards.
        // Neither direction nor confidence supplies a qualified severity measurement.
        var pairs = full.Interactions
            .Where(interaction => interaction.Type is InteractionType.Synergistic or InteractionType.Complementary
                or InteractionType.Redundant or InteractionType.Interfering)
            .Select(interaction => new InteractionPairSummaryResponse(
                interaction.CompoundA,
                interaction.CompoundB,
                null))
            .ToList();

        return new ReducedInteractionIntelligenceResponse(pairs);
    }

    /// <summary>
    /// Same boundary as <see cref="Project"/>, for the <see cref="InteractionFlagResponse"/> shape
    /// returned by POST /api/v1/knowledge/overlap-check (owner ruling 2026-09-16, extended
    /// 2026-09-17: "That ruling is extended to POST /api/v1/knowledge/overlap-check"). Without the
    /// entitlement, each flag is reduced to a <see cref="ReducedInteractionFlagResponse"/>: the
    /// pair identity and explicit null (unavailable) severity survive; type, pathway, <c>Description</c>
    /// (the per-pair reasoning sentence) and <c>EvidenceConfidence</c> (free text derived from that
    /// same reasoning) are omitted from the payload entirely, not blanked.
    /// </summary>
    public static object ProjectFlags(List<InteractionFlagResponse> flags, bool hasReasoningAccess)
    {
        if (hasReasoningAccess)
        {
            return flags;
        }

        return flags
            .Where(flag => flag.OverlapType is OverlapType.PathwayOverlap or OverlapType.MechanismicSimilarity
                or OverlapType.PotentialInteraction or OverlapType.AdditiveBenefit)
            .Select(flag => new ReducedInteractionFlagResponse(
                flag.Id,
                flag.CompoundNames,
                null,
                flag.CreatedAtUtc))
            .ToList();
    }

    /// <summary>
    /// Fail-closed check for the reviewed_relationship_graph entitlement that gates per-pair
    /// interaction reasoning (owner ruling 2026-09-16, extended 2026-09-17). Shared so a call site
    /// that cannot inject a scoped service (e.g. a static minimal-API endpoint handler) does not
    /// duplicate the try/catch already established by <c>ProtocolService.HasInteractionReasoningAccessAsync</c>.
    /// Any exception while determining entitlement — no current-user context (anonymous caller), DB
    /// failure, anything — resolves to "no access" rather than propagating.
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
