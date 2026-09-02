# Session Handoffs

## Parser

- Branch/commit: `codex/test-repro-parser` / `817f6f3331c2b7c3410da63289c02fb27a98ed74`.
- Files: the two parser reproduction files allowed by `PARCELS.md`; worktree clean.
- Verification: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ProtocolParserReproductionTests|FullyQualifiedName~ProtocolIngestionPdfReproductionTests" --verbosity minimal` exits 1 with 7 assertion-level failures and no harness failures: `AliasInsideUnrelatedTokens` (non-empty result), `CommaSeparatedCompounds` (1 vs 2 entries), `DotDecimalDose` (0 vs 0.5), `LeadingDecimalDose` (25 vs 0.25), `LeadingDigitCompoundName` (missing `5-`), `UnicodeMicroSign` (0 dose), and `AlreadyCancelledRequest` (no cancellation exception).
- Residual: regex denial-of-service timing was not tested; cancellation non-enforcement was reproduced safely instead.

## Interaction

- Branch/commit: `codex/test-repro-interaction` / `56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f`.
- Files: `InteractionIntelligenceReproductionTests.cs`; worktree clean.
- Verification: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~InteractionIntelligenceReproductionTests" --verbosity minimal` exits 1 with 4 assertion-level failures; nearby existing interaction tests pass: safety precedence returned `Synergistic`, NeedsReview was emitted as reviewed intelligence, canonical/alias inputs produced 1 self-pair, and confidence propagated `NaN`.
- Residual: finite out-of-range confidence is already clamped; the reproduced defect is specifically non-finite `NaN` propagation.

## Evidence/provenance

- Branch/commit: `codex/test-repro-evidence` / `2c9d6cabce4bad853a63365c03e55c2fc612cb70`.
- Files: `EvidenceProvenanceReproductionTests.cs`; worktree clean.
- Verification: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~EvidenceProvenanceReproductionTests" --verbosity minimal` exits 1 with 2 assertion-level failures; 36 adjacent evidence/staging tests pass: failed artifact opened the evidence gate, and synthetic job/workflow/tool labels satisfied source attribution.
- Residual: no endpoint-coverage architecture test was added because a precise proof required broader integration infrastructure or brittle source-text inspection.

## Sidecar lifecycle

- Branch/commit: `codex/test-repro-sidecar` / `82295c3f36b412b9917eaf047a70b10ee2a67cdc`.
- Files: the two sidecar reproduction files allowed by `PARCELS.md`; worktree clean.
- Verification: `python -m pytest backend/research-sidecar/tests/test_runner_terminal_state_reproduction.py backend/research-sidecar/tests/test_request_constraint_reproduction.py` exits 1 with 2 assertion-level failures; 19 adjacent existing tests pass: late worker overwrote `failed` with `pending_review`, and 2 accepted sources exceeded the requested maximum of 1.
- Boundary: synchronized synthetic fakes only; no network access.

## Outbound-data boundary

- Branch/commit: `codex/test-repro-outbound` / `c3e30a93e64be5a2662bb9040a52105d3dc909c9`.
- Files: OCR and frontend reproduction files; worktree clean.
- Verification: historical dependency-complete run recorded each targeted command exiting 1 with one assertion-level failure after an intercepting fake recorded one request. Coordinator rerun: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ProtocolOcrOutboundBoundaryReproductionTests" --verbosity minimal` exits 1 with the intended OCR assertion; `npm test -- src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts` exits 1 during Vitest startup because this worktree has no local `node_modules` (`vitest/config` and `@vitejs/plugin-react` missing), so no frontend assertion was collected in this environment.
- Boundary: synthetic data and local fakes only; results prove latent/config-dependent reachability, not current production configuration or exposure.
- Residual: Collective is inconclusive. Its Allowed File names a nonexistent `BioStack.Cognition.Tests` project, while the compiling Cognition tests are outside the parcel contract.
