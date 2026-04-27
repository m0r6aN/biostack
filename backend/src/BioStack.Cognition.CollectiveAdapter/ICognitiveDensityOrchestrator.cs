// ─────────────────────────────────────────────────────────────────────────────
// ADAPTER SHIM — BioStack.Cognition.CollectiveAdapter
//
// This file is a LOCAL CONTRACT MIRROR of the keon.collective package interface.
// It is NOT the real Keon.Collective package. It exists only to allow BioStack
// to compile and test while the real Keon.Collective.Core NuGet package is not
// yet published or available on this machine.
//
// SWAP PATH: When Keon.Collective.Core is available, replace the ProjectReference
// in BioStack.Cognition.csproj with:
//   <PackageReference Include="Keon.Collective.Core" Version="x.y.z" />
// and delete this project entirely.
//
// The namespace (Keon.Collective) is intentionally identical to the real package
// so that no using-statement changes are required at swap time.
// ─────────────────────────────────────────────────────────────────────────────

namespace Keon.Collective;

/// <summary>
/// Orchestrates multi-perspective deliberation over a branch and claim graph.
/// The orchestrator is non-executable commentary infrastructure — it must NOT
/// call any effect surface, gateway, or action executor.
/// </summary>
public interface ICognitiveDensityOrchestrator
{
    Task<CognitiveDensityEnvelope> RunAsync(
        CollectiveIntent intent,
        TemporalEchoBranch? seedBranch = null,
        BranchRefinementOptions? refinementOptions = null,
        ClaimGraph? claimGraph = null,
        IReadOnlyList<BranchCollapseRecord>? historicalCollapses = null,
        CancellationToken ct = default);
}
