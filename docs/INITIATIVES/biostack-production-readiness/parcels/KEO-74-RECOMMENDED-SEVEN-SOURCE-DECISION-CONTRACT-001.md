# Parcel: KEO-74-RECOMMENDED-SEVEN-SOURCE-DECISION-CONTRACT-001

Status: Product doctrine and seven-source selection confirmed; awaiting content-class rights decisions and any triggered security/data review before source activation.

## Goal

Create a schema-validated, registry-hash-bound decision packet for the seven official sources selected for BioStack's first acquisition lane, while preserving useful evidence-backed information and applying approval gates only at the stage each owner governs.

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
- Product-owner correction supplied 2026-07-25: help people make informed decisions for safety and success while remaining observational, educational, evidence-aware, and non-prescriptive.
- Ratified doctrine: high risk requires more evidence, explanation, validation, review, and escalation; it does not automatically require less useful information.

## Integration Surfaces

- Source decision packet -> source-registry activation
- Source decision packet -> future official-source intake

## Security Gate

Security/data review is required only when a declared trigger applies; otherwise the source packet records `not-applicable` and must be reassessed if the acquisition method or boundary changes.

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
- Do not fabricate legal/rights, evidence-promotion, or security/data decisions.
- Do not set `activationReady=true`.
- Do not assert a legal conclusion or license approval.
- Do not enable operations or acquisition.
- Do not retrieve source data.
- Do not change API, database, canonical ingest, intake, promotion, or deployment behavior.
- Do not convert evidence, studied ranges, risks, comparisons, or monitoring context into diagnosis, prescribing, individualized directives, medical-authority claims, guaranteed outcomes, or illegal sourcing assistance.

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
- confirm the product doctrine from Clint Morgan without treating that global doctrine decision as a source-specific approval;
- allow source-grounded benefits, risks, mechanisms, regulatory status, evidence quality, studied populations, forms, routes, dose ranges, timing, frequency, duration, outcomes, adverse events, interactions, monitoring considerations, comparisons, contradictions, and uncertainty as informative context;
- prohibit diagnosis, prescription, clinician impersonation, individualized commands, guaranteed safety or outcomes, and illegal sourcing/evasion assistance;
- make approval gates stage-specific:
  - legal/rights blocks source activation for the applicable content class and use;
  - security/data review is conditional on credentials/restricted access, private data, a new egress or storage boundary, untrusted bulk archives/parsers, or executable/active content;
  - evidence review blocks canonical claim promotion;
  - product review governs product-capability behavior;
- record Clint Morgan's seven-source selection and product doctrine as reviewed and approved;
- keep legal/rights and evidence-promotion decisions at `review-required` until their applicable stage;
- record security/data as `not-applicable` only while the planned lane has no declared trigger, with deterministic re-review if the method or boundary changes;
- keep every source `selected-pending-source-activation-review` and `activationReady=false` while rights remain unresolved; and
- state explicitly that a pending later-stage review does not block an earlier authorized stage.

## Required Tests

- The new schema is bundled and registered as a research artifact kind.
- The real seven-source batch validates against the schema.
- Registry schema version and exact SHA-256 match the current pilot registry.
- The seven source IDs are exact and unique.
- The four owner assignments are exact and unique.
- Product doctrine and its non-prescriptive boundary are explicit and schema-enforced.
- Source activation, canonical claim promotion, and product-capability review have distinct approval gates.
- Product selection is reviewed/approved; legal/rights, evidence-promotion, and security/data states remain explicit and stage-specific.
- Approval-state coherence, role/scope wiring, activation prerequisites, rejected-rights behavior, and triggered-security behavior are enforced by schema and negative tests.
- Every current packet remains not activation-ready because rights are unresolved, not because all four roles form a blanket gate.
- The schema can represent later reviewed, approved-with-controls, rejected, not-applicable, enabled, suspended, and stale-labeled states rather than encoding a permanently disabled snapshot.
- Useful source-grounded information is expressly permitted, including studied dose/timing context and risk/uncertainty context.
- Blanket suppression is prohibited.
- No packet proposes an enabled acquisition state or an approved rights state.

## Acceptance Criteria

- The packet is complete enough for content-class rights review, conditional security/data review, and later evidence review at canonical claim promotion.
- Facts are linked to official first-party documentation and are separated from proposed BioStack policy.
- Product policy helps users make informed decisions with cited context while remaining observational, educational, evidence-aware, and non-prescriptive.
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

- What changed: adds a stage-specific decision contract for seven official sources and records the product owner's informed-decision doctrine and source selection.
- Why: enables useful source-grounded information while applying rights, security, evidence, and product review only at their governed stages.
- Risk: contract-only; source registry and runtime behavior remain unchanged, and no acquisition is enabled by this parcel.
- Verification: focused schema/validator tests plus registry hash and diff checks.
- Evidence: decision batch, schema, parcel, and test output.

## Session Handoff

- Starting commit: `9a74df2279383b3ea8f61094b5ef164c0c6a3950`
- Ending commit: amendment pending commit on top of `85a7813a29904ce8b5f69a62c1e2242bc699b7ef`
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
  - `rtk proxy cmd /d /c "set DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1&& set UseSharedCompilation=false&& dotnet test backend\tests\BioStack.KnowledgeWorker.Tests\BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~ResearchSchemaFilesTests --disable-build-servers --logger console;verbosity=minimal"`
  - `rtk proxy cmd /d /c "set DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1&& set UseSharedCompilation=false&& dotnet test backend\tests\BioStack.KnowledgeWorker.Tests\BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~ResearchArtifactValidatorTests --disable-build-servers --logger console;verbosity=minimal"`
  - `rtk proxy certutil -hashfile research/input/sources/pilot-source-registry.json SHA256`
  - `rtk git diff --check`
  - `rtk git status --short`
- Tests passed: 41 focused tests total: 7 `ResearchSchemaFilesTests` and 34 `ResearchArtifactValidatorTests`.
- Tests failed: 0 in the final focused runs.
- Validation notes:
  - Each packet links the reviewed first-party API, bulk-download, terms, policy, disclaimer, or contact surfaces applicable to that source and records concrete rate-limit and rights-boundary observations without treating them as approvals.
  - Tests allow a detected security trigger to remain pending while inactive, then reject activation until the triggered security review is approved; other negative tests reject unresolved rights, incoherent reviewed approvals, cross-wired role scope, and acquisition after a rights rejection.
  - Registry SHA-256 remained `0a625778407fc85f3e32ed620b578bf4fe37cd37acb09c938776d9ed82aa7163`.
  - `rtk git diff --check` passed.
  - The requested combined filter was split by the RTK shell shim at `|`; the same two test classes were run separately.
  - Two RTK-wrapped test invocations left orphaned build processes and timed out without results; only those task-owned processes were stopped, build servers were shut down, and the same focused suites then passed with build-server reuse disabled.
  - Restore/build reported the repository's existing `System.Security.Cryptography.Xml` `10.0.9` high-severity advisory warnings; this contract does not change dependencies.
- Decisions needed before source activation: content-class/use legal-rights decisions from Johnathan Harper. Current public read-only packets have no declared security/data trigger; Pradic Patel's review becomes required if the acquisition method or boundary introduces one.
- Later-stage decision: Ellison Nemoy reviews interpretive claims before canonical promotion. Clint Morgan's seven-source selection and product doctrine are recorded as approved.
- Blockers: none for contract preparation.
- Next safe action: submit the seven source packets for content-class rights review and identify whether each planned acquisition method triggers security/data review, without suppressing useful information at unrelated stages.
- Do not touch: source activation, acquisition, canonical ingest, promotion, database, or deployment state.

## Stop-and-Report Rule

If implementation requires a product decision not present in this spec, a file outside Allowed Files, a contract amendment, or an unclear security boundary, stop and report before continuing.
