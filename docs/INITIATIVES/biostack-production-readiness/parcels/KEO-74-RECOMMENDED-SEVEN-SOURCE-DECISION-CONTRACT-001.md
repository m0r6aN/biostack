# Parcel: KEO-74-RECOMMENDED-SEVEN-SOURCE-DECISION-CONTRACT-001

Status: Contract implemented and validated locally; awaiting four-role, source-by-source human review.

## Goal

Create a schema-validated, registry-hash-bound decision packet for the seven official sources selected for BioStack's first acquisition lane, without approving or activating any source.

## Initiative

BioStack Production Readiness & Monetization

## Project Track

M3 - Data & Intelligence Coverage / governed source registry

## Wave

W1 Contracts

## Branch

`codex/keo-74-source-decisions-20260725`

## Worktree

`D:\Repos\BioStack-keo74-source-decisions-20260725`

## Dependencies

- KEO-74 source-registry v2 scaffold on `origin/main@9a74df2279383b3ea8f61094b5ef164c0c6a3950`
- Human owner assignment supplied 2026-07-25

## Integration Surfaces

- Source decision packet -> source-registry activation
- Source decision packet -> future official-source intake

## Security Gate

Security review required before any acquisition is enabled.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Schemas/source-authorization-decision.schema.json`
- `backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/ResearchArtifactKind.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/ResearchSchemaFilesTests.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/ResearchArtifactValidatorTests.cs`
- `research/source-authorization/recommended-seven-source-decisions.v1.json`
- `research/routing-events/keo-74-source-decisions-20260725.json`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-74-RECOMMENDED-SEVEN-SOURCE-DECISION-CONTRACT-001.md`

If a required file is not listed, stop and request a spec amendment before editing or creating it.

## Forbidden

- Do not edit `pilot-source-registry.json`.
- Do not mark any approval as granted.
- Do not set `activationReady=true`.
- Do not assert a legal conclusion or license approval.
- Do not enable operations or acquisition.
- Do not retrieve source data.
- Do not change API, database, canonical ingest, intake, promotion, or deployment behavior.

## Out of Scope

Source activation, API credentials, production acquisition, canonical promotion, the legacy admin-ingest bypass, and processing the queued compound gaps.

## Existing Patterns To Follow

- `backend/src/BioStack.KnowledgeWorker/Schemas/source-registry.schema.json` - Draft 2020-12 governance schema.
- `backend/src/BioStack.KnowledgeWorker/Pipeline/ResearchArtifactKind.cs` - research artifact registration.
- `backend/tests/BioStack.KnowledgeWorker.Tests/ResearchArtifactValidatorTests.cs` - real-artifact validation and fail-closed invariants.
- `docs/INITIATIVES/biostack-production-readiness/KEO-74-SOURCE-REGISTRY-V2-GATES.md` - activation boundary.

## Contract

The batch must:

- use record type `source-authorization-decision-batch`;
- bind to source-registry schema `2.0.0` and SHA-256 `0a625778407fc85f3e32ed620b578bf4fe37cd37acb09c938776d9ed82aa7163`;
- identify the four named humans separately from their approval state:
  - product owner: Clint Morgan;
  - legal/rights approver: Johnathan Harper;
  - evidence reviewer: Ellison Nemoy;
  - security/data owner: Pradic Patel;
- contain exactly `fda`, `pubchem`, `pubmed`, `clinicaltrials`, `dailymed`, `nih-ods`, and `nih-nccih`;
- record observed first-party documentation, proposed field/content boundaries, provenance, refresh, remediation, limitations, and unresolved questions;
- keep all four approvals pending per source;
- keep every source `pending-human-signoff` and `activationReady=false`; and
- state explicitly that assignment is not approval.

## Required Tests

- The new schema is bundled and registered as a research artifact kind.
- The real seven-source batch validates against the schema.
- Registry schema version and exact SHA-256 match the current pilot registry.
- The seven source IDs are exact and unique.
- The four owner assignments are exact and unique.
- Every approval is pending, every packet is pending human signoff, and every packet is not activation-ready.
- No packet proposes an enabled acquisition state or an approved rights state.

## Acceptance Criteria

- The packet is complete enough for the four humans to review source-by-source.
- Facts are linked to official first-party documentation and are separated from proposed BioStack policy.
- Schema and invariant tests pass.
- The registry remains byte-for-byte unchanged.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter "FullyQualifiedName~ResearchSchemaFilesTests|FullyQualifiedName~ResearchArtifactValidatorTests"
rtk proxy certutil -hashfile research/input/sources/pilot-source-registry.json SHA256
rtk git diff --check
```

Success means the focused suite passes, the registry hash remains pinned, and only the eight allowed files are changed.

## Evidence Required

- Focused test output.
- Registry SHA-256.
- Diff scope.
- Official first-party URLs embedded in the decision batch.

## Collision Risk

Medium. `ResearchArtifactKind.cs`, the worker project file, and shared validator tests are serialization points.

## PR Notes

- What changed: adds the pending decision contract for seven official sources.
- Why: enables explicit human review without conflating owner assignment with source approval.
- Risk: contract-only; source registry and runtime behavior remain unchanged.
- Verification: focused schema/validator tests plus registry hash and diff checks.
- Evidence: decision batch, schema, parcel, and test output.

## Session Handoff

- Starting commit: `9a74df2279383b3ea8f61094b5ef164c0c6a3950`
- Ending commit: uncommitted changes on `9a74df2279383b3ea8f61094b5ef164c0c6a3950`
- Files changed:
  - `backend/src/BioStack.KnowledgeWorker/Schemas/source-authorization-decision.schema.json`
  - `backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj`
  - `backend/src/BioStack.KnowledgeWorker/Pipeline/ResearchArtifactKind.cs`
  - `backend/tests/BioStack.KnowledgeWorker.Tests/ResearchSchemaFilesTests.cs`
  - `backend/tests/BioStack.KnowledgeWorker.Tests/ResearchArtifactValidatorTests.cs`
  - `research/source-authorization/recommended-seven-source-decisions.v1.json`
  - `research/routing-events/keo-74-source-decisions-20260725.json`
  - `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-74-RECOMMENDED-SEVEN-SOURCE-DECISION-CONTRACT-001.md`
- Commands run:
  - `rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~ResearchSchemaFilesTests`
  - `rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~ResearchArtifactValidatorTests`
  - `rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~ResearchSchemaFilesTests`
  - `rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~ResearchArtifactValidatorTests`
  - `rtk proxy certutil -hashfile research/input/sources/pilot-source-registry.json SHA256`
  - `rtk git diff --check`
  - `rtk git status --short`
- Tests passed: 34 focused tests total: 7 `ResearchSchemaFilesTests` and 27 `ResearchArtifactValidatorTests`.
- Tests failed: 0 in the final focused runs. An initial validator run exposed a test-only null-node assertion error; the assertion was corrected and the full validator group passed.
- Validation notes:
  - Each packet links the reviewed first-party API, bulk-download, terms, policy, disclaimer, or contact surfaces applicable to that source and records concrete rate-limit and rights-boundary observations without treating them as approvals.
  - Registry SHA-256 remained `0a625778407fc85f3e32ed620b578bf4fe37cd37acb09c938776d9ed82aa7163`.
  - `rtk git diff --check` passed.
  - The requested combined filter was split by the RTK shell shim at `|`; the same two test classes were run separately.
  - Restore/build reported the repository's existing `System.Security.Cryptography.Xml` `10.0.9` high-severity advisory warnings; this contract does not change dependencies.
- Decisions needed: source-by-source decisions from the four named humans.
- Blockers: none for contract preparation.
- Next safe action: submit the validated packet for human review.
- Do not touch: source activation, acquisition, canonical ingest, promotion, database, or deployment state.

## Stop-and-Report Rule

If implementation requires a product decision not present in this spec, a file outside Allowed Files, a contract amendment, or an unclear security boundary, stop and report before continuing.
