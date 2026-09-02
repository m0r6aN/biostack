# Session Handoffs

## Parser

- Branch/commit: `codex/test-repro-parser` / `817f6f3331c2b7c3410da63289c02fb27a98ed74`.
- Files: the two parser reproduction files allowed by `PARCELS.md`; worktree clean.
- Verification: targeted test command exits 1 with 7 assertion-level failures and no harness failures.
- Residual: regex denial-of-service timing was not tested; cancellation non-enforcement was reproduced safely instead.

## Interaction

- Branch/commit: `codex/test-repro-interaction` / `56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f`.
- Files: `InteractionIntelligenceReproductionTests.cs`; worktree clean.
- Verification: targeted test command exits 1 with 4 assertion-level failures; nearby existing interaction tests pass.
- Residual: finite out-of-range confidence is already clamped; the reproduced defect is specifically non-finite `NaN` propagation.

## Evidence/provenance

- Branch/commit: `codex/test-repro-evidence` / `2c9d6cabce4bad853a63365c03e55c2fc612cb70`.
- Files: `EvidenceProvenanceReproductionTests.cs`; worktree clean.
- Verification: targeted test command exits 1 with 2 assertion-level failures; 36 adjacent evidence/staging tests pass.
- Residual: no endpoint-coverage architecture test was added because a precise proof required broader integration infrastructure or brittle source-text inspection.

## Sidecar lifecycle

- Branch/commit: `codex/test-repro-sidecar` / `82295c3f36b412b9917eaf047a70b10ee2a67cdc`.
- Files: the two sidecar reproduction files allowed by `PARCELS.md`; worktree clean.
- Verification: targeted pytest exits 1 with 2 assertion-level failures; 19 adjacent existing tests pass.
- Boundary: synchronized synthetic fakes only; no network access.

## Outbound-data boundary

- Branch/commit: `codex/test-repro-outbound` / `c3e30a93e64be5a2662bb9040a52105d3dc909c9`.
- Files: OCR and frontend reproduction files; worktree clean.
- Verification: each targeted command exits 1 with one assertion-level failure after an intercepting fake records one request.
- Boundary: synthetic data and local fakes only; results prove latent/config-dependent reachability, not current production configuration or exposure.
- Residual: Collective is inconclusive. Its Allowed File names a nonexistent `BioStack.Cognition.Tests` project, while the compiling Cognition tests are outside the parcel contract.
