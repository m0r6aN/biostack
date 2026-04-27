namespace BioStack.Cognition;

using BioStack.Cognition.Models;
using Keon.Collective;

/// <summary>
/// Runs the Stack Review Board: translates a BioStack envelope into deliberation
/// inputs and invokes the keon.collective orchestrator for commentary review.
///
/// DOCTRINE:
///   - The review is COMMENTARY ALONGSIDE the deterministic safety panel.
///   - It must NOT suppress, override, or contradict safety findings.
///   - It must NOT mark a stack safe, prescribe, or auto-modify a protocol.
///   - The counter-position is NEVER executable.
/// </summary>
public interface IStackReviewBoardService
{
    Task<CognitiveDensityEnvelope> ReviewStackAsync(
        StackDeliberationEnvelope envelope,
        CancellationToken ct = default);
}
