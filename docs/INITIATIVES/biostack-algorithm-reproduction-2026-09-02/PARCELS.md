# Parcels

## Parser

- Branch: `codex/test-repro-parser`
- Allowed Files:
  - `backend/tests/BioStack.Application.Tests/Services/ProtocolParserReproductionTests.cs`
  - `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionPdfReproductionTests.cs`
- Claims: alias token boundaries, multi-compound preservation, bounded regex behavior, culture-invariant decimals, leading digits, units/precision, and micro-sign handling.
- Command: targeted `dotnet test` filters for the two reproduction classes.

## Interaction

- Branch: `codex/test-repro-interaction`
- Allowed Files:
  - `backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs`
  - `backend/tests/BioStack.Application.Tests/Services/CounterfactualEngineReproductionTests.cs` (reserved exact lane; no separate counterfactual claim survived shaping, so no file was added)
- Claims: avoid/review precedence, canonical self-pairs, and finite bounded confidence.
- Command: targeted `dotnet test` filters for the two reproduction classes.

## Evidence/provenance

- Branch: `codex/test-repro-evidence`
- Allowed Files:
  - `backend/tests/BioStack.Application.Tests/ScientificResearch/EvidenceProvenanceReproductionTests.cs`
  - `backend/tests/BioStack.Api.Tests/Architecture/UserFacingGateCoverageReproductionTests.cs` (reserved exact lane; no precise proof was added and endpoint-wide coverage remains inconclusive)
- Claims: failed/synthetic research provenance cannot be promoted to source-attributed user-facing evidence; record any gate-coverage result as runtime or static evidence precisely.
- Command: targeted `dotnet test` filters for the two reproduction classes.

## Sidecar lifecycle

- Branch: `codex/test-repro-sidecar`
- Allowed Files:
  - `backend/research-sidecar/tests/test_runner_terminal_state_reproduction.py`
  - `backend/research-sidecar/tests/test_request_constraint_reproduction.py`
- Claims: terminal-state overwrite after timeout and unenforced request bounds.
- Command: targeted `pytest` on the two reproduction files.

## Outbound-data boundary

- Branch: `codex/test-repro-outbound`
- Allowed Files:
  - `backend/tests/BioStack.Application.Tests/Services/ProtocolOcrOutboundBoundaryReproductionTests.cs`
  - `backend/tests/BioStack.Cognition.Tests/CollectiveOutboundBoundaryReproductionTests.cs` (reserved exact lane; the project/file is absent from the execution tree, so Collective behavior is explicitly inconclusive and no substitute path is authorized)
  - `frontend/src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts`
- Claims: config-only OCR/Collective transmission and unauthenticated suggest-route relay. Preserve the distinction between latent/config-dependent reachability and active production exposure.
- Commands: targeted `dotnet test` filters plus targeted `npm test -- <file>`.
