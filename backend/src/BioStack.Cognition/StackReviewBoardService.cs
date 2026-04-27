namespace BioStack.Cognition;

using BioStack.Cognition.Models;
using Keon.Collective;

/// <summary>
/// Orchestrates the Stack Review Board pipeline:
///   1. Translate the BioStack envelope → keon.collective inputs.
///   2. Run the cognitive-density orchestrator for commentary.
///   3. Return the CognitiveDensityEnvelope to the caller.
///
/// DOCTRINE: This service does NOT call any effect surface, reality surface,
/// gateway, or governed-execute path. It is observational commentary only.
/// </summary>
public sealed class StackReviewBoardService : IStackReviewBoardService
{
    private readonly IStackDeliberationTranslator _translator;
    private readonly ICognitiveDensityOrchestrator _orchestrator;

    // Default contexts used when the caller does not supply tenant/actor info.
    private static readonly TenantContext DefaultTenant = new("biostack-public");
    private static readonly ActorContext DefaultActor   = new("biostack-system", "Service");

    public StackReviewBoardService(
        IStackDeliberationTranslator translator,
        ICognitiveDensityOrchestrator orchestrator)
    {
        _translator  = translator;
        _orchestrator = orchestrator;
    }

    public async Task<CognitiveDensityEnvelope> ReviewStackAsync(
        StackDeliberationEnvelope envelope,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var correlation = new CorrelationContext(Guid.NewGuid().ToString("N"));

        var inputs = _translator.Translate(
            envelope,
            DefaultTenant,
            DefaultActor,
            correlation);

        return await _orchestrator.RunAsync(
            inputs.Intent,
            inputs.SeedBranch,
            refinementOptions: null,
            inputs.ClaimGraph,
            inputs.HistoricalCollapses,
            ct);
    }
}
