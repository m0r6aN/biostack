# Parcel: KEO-74-OFFLINE-SOURCE-ACQUISITION-PLANNING-003

Status: Implemented, verified locally, and independently reviewed PASS.

## Goal

Create a transport-free, in-memory acquisition-planning seam for the seven selected official sources while every real source remains legally and operationally disabled.

## Initiative

BioStack Production Readiness & Monetization

## Project Track

M3 - Data & Intelligence Coverage / governed source acquisition

## Wave

W1 Contracts and offline planning

## Branch

`codex/keo-74-offline-source-planning-20260725`

## Worktree

`D:\Repos\BioStack-keo74-offline-source-planning-20260725`

## Dependencies

- `main@c445832`
- KEO-74 source-registry v2 activation gates
- PR #207 market-interest research requests
- PR #208 stage-specific seven-source authorization decisions
- Existing `ResearchRequestIndex` and `SourceRegistryAuthorizer`

## Integration Surfaces

- Research request batch -> in-memory source acquisition intents
- Source authorization decision batch + source registry -> acquisition readiness
- Future authorized source transport -> ready acquisition intents

## Security Gate

No security review is required for this transport-free parcel. A future transport or storage parcel must re-evaluate the declared security/data triggers before activation.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceRegistryActivationPolicy.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceRegistryAuthorizer.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceAcquisitionPlanning.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/RecommendedOfficialSourcePlanningAdapters.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/Fixtures/source-acquisition-planning.sample.json`
- `backend/tests/BioStack.KnowledgeWorker.Tests/SourceAcquisitionPlanningTests.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/ResearchEvidenceProcessingTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-74-OFFLINE-SOURCE-ACQUISITION-PLANNING-003.md`

If a required file is not listed, stop and request a spec amendment before editing or creating it.

## Forbidden

- No HTTP, network, browser, bulk download, or source retrieval.
- No filesystem output, source-payload storage, database, or persistence behavior.
- No DI, `Program.cs`, `WorkerOptions`, `ResearchJob`, API, or worker-runtime wiring.
- No source-registry, source-decision, schema, credential, or deployment changes.
- No evidence-packet generation, canonical ingest, claim promotion, or user-facing guidance.
- No real endpoint templates or unreviewed source-specific request parameters.

## Out of Scope

Legal/rights decisions, source activation, credentials, live transports, source-specific payload normalization, storage, refresh execution, evidence extraction, evidence review, canonical promotion, and deployment.

## Existing Patterns To Follow

- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceRegistryAuthorizer.cs` - exact-reference resolution and activation prerequisites.
- `backend/src/BioStack.KnowledgeWorker/Pipeline/ResearchRequestIndex.cs` - normalized research target input.
- `backend/tests/BioStack.KnowledgeWorker.Tests/ResearchEvidenceProcessingTests.cs` - source authorization regression behavior.
- `research/source-authorization/recommended-seven-source-decisions.v1.json` - selected sources and stage-specific gates.
- `docs/INITIATIVES/biostack-production-readiness/KEO-74-SOURCE-REGISTRY-V2-GATES.md` - canonical activation boundary.

## Contract

The parcel must:

- extract source-registry acquisition prerequisites into one reusable pure evaluator;
- preserve existing `SourceRegistryAuthorizer` behavior;
- define transport-free acquisition target, intent, plan, adapter, and builder contracts;
- plan for exactly `fda`, `pubchem`, `pubmed`, `clinicaltrials`, `dailymed`, `nih-ods`, and `nih-nccih`;
- normalize compound name and aliases into deterministic search terms without creating a network request;
- retain request ID, compound name, selected source, adapter version, candidate method, authorized field uses, provenance requirements, registry schema version, and decision-bound registry hash;
- require the caller to supply the SHA-256 of the exact registry bytes it parsed, and block all intents unless it is lowercase 64-hex and exactly matches the decision binding;
- mark an intent ready only when both the source registry and the complete applicable source-decision activation gates permit acquisition;
- require each applicable product, legal, and triggered-security approval to carry the expected assignee, scope, blocking stage, reviewed approval decision, valid decision timestamp, and nonempty decision notes;
- require the no-trigger security review to be exactly not applicable with an explicit null decision and null decision timestamp; a reviewed approval without a trigger is contradictory and remains blocked;
- use the activated registry method for ready intents and require an exact decision/registry method match;
- use the nonempty decision/registry intersection for ready authorized field uses, avoiding both unauthorized expansion and unnecessary suppression of a narrower approved scope;
- use the decision/registry union for ready provenance requirements so neither side's traceability obligations are lost;
- invalidate a duplicated registry source identity and every alias attached to any duplicate entry;
- keep evidence-promotion review independent from acquisition readiness;
- block a source with a detected security/data trigger until its source-activation review is approved; and
- perform no I/O beyond reading test and repository fixtures in tests.

`Ready` is an in-memory planning disposition. It is not source activation, a retrieval command, or authorization to perform an external effect.

## Required Tests

- The real pilot registry leaves all seven selected sources non-acquiring.
- The real market-interest requests, real authorization decisions, and real pilot registry produce only blocked intents.
- Request and provenance lineage remain attached to each intent.
- The planning-adapter catalog contains exactly seven unique selected source IDs.
- Synthetic authorized source state produces ready intents while evidence promotion remains review-required.
- Every incomplete decision rights, operations, acquisition, API/robots, refresh, and required-field layer remains blocked even when the registry is active.
- Invalid, stale, or mismatched registry SHA-256 bindings remain blocked, including same-version registry mutation.
- Ready output uses the activated registry method, safe field intersection, and provenance union.
- Duplicate registry identities invalidate every associated alias.
- A detected security/data trigger blocks only its affected source while review is pending.
- Duplicate or incomplete adapter catalogs fail closed.
- Existing source-registry authorizer exact-alias, ambiguous-alias, activation, and field-use behavior remains green.

## Acceptance Criteria

- No production source is enabled or represented as ready.
- Synthetic fixtures are visibly non-authoritative and cause no external effect.
- Planner output is deterministic for the same inputs.
- No transport, endpoint, persistence, evidence, or canonical-promotion code is introduced.
- Focused planning and evidence-processing tests pass.
- `git diff --check` passes.
- Only allowed files change.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~SourceAcquisitionPlanningTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~ResearchEvidenceProcessingTests --disable-build-servers
rtk git diff --check
rtk git status --short
```

Success means both focused suites pass, all real-source intents remain blocked, and the diff is limited to the allowlist.

## Evidence Required

- Focused test output.
- Real-artifact blocked-intent assertion.
- Synthetic authorized-state ready-intent assertion.
- Diff scope and `git diff --check`.

## Collision Risk

Medium. `SourceRegistryAuthorizer.cs` and its regression tests are shared source-governance surfaces. New planning files are otherwise isolated.

## Known Contract Limitation

The existing source-decision schema requires at least one `unresolvedQuestions` entry but does not type questions by activation stage. This planner does not invent a magic text prefix or infer stage from prose. It instead evaluates every structured source-activation field fail-closed. A future schema parcel may add typed question stages if question-level activation blocking is required.

The planner performs readiness checks only and deliberately does not load schema files. Callers must run full source-decision and source-registry schema validation before invoking it; adding schema loading here would introduce filesystem I/O into the transport-free seam.

## PR Notes

- What changed: extracts reusable source activation evaluation and adds transport-free planning for the seven selected official sources.
- Why: prepares deterministic acquisition work without bypassing current legal, security, persistence, or promotion gates.
- Risk: no runtime wiring or external effect; behavior risk is limited to refactoring the existing source-authorizer gate logic.
- Verification: focused planning tests, existing evidence-processing regressions, and diff checks.
- Evidence: test output and this parcel handoff.

## Session Handoff

- Starting commit: `c445832`
- Ending commit: uncommitted changes on `c445832`
- Files changed:
  - `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceRegistryActivationPolicy.cs`
  - `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceRegistryAuthorizer.cs`
  - `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceAcquisitionPlanning.cs`
  - `backend/src/BioStack.KnowledgeWorker/Pipeline/RecommendedOfficialSourcePlanningAdapters.cs`
  - `backend/tests/BioStack.KnowledgeWorker.Tests/Fixtures/source-acquisition-planning.sample.json`
  - `backend/tests/BioStack.KnowledgeWorker.Tests/SourceAcquisitionPlanningTests.cs`
  - `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-74-OFFLINE-SOURCE-ACQUISITION-PLANNING-003.md`
- Commands run:
  - focused planning tests with restore/build, then with `--no-restore`
  - focused `ResearchEvidenceProcessingTests` regression suite
  - full `BioStack.KnowledgeWorker.Tests` suite
  - `git diff --check`, `git status --short`, and a no-network/no-persistence implementation scan
- Tests passed:
  - `SourceAcquisitionPlanningTests`: 58 passed, 0 failed, 0 skipped
  - `ResearchEvidenceProcessingTests`: 62 passed, 0 failed, 0 skipped
  - full `BioStack.KnowledgeWorker.Tests` suite: exit 0
- Tests failed: 0 in final verification. Independent review defects in decision-layer validation, byte-hash binding, registry-authoritative output, and duplicate-identity alias invalidation were corrected and covered by regressions before this verification.
- Warnings: existing `System.Security.Cryptography.Xml 10.0.9` NU1903 advisory only; this parcel does not change the dependency.
- Decisions needed: none for offline planning
- Blockers: none
- Next safe action: commit, push, and open a draft PR for review
- Do not touch: live retrieval, persistence, registry activation, evidence generation, canonical promotion, or runtime wiring

## Stop-and-Report Rule

If implementation requires a file outside Allowed Files, a contract amendment, network or persistence behavior, a rights or security decision, or any production/runtime wiring, stop and report before continuing.
