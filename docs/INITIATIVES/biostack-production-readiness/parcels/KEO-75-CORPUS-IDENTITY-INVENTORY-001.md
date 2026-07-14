# Parcel: KEO-75-CORPUS-IDENTITY-INVENTORY-001

## Goal

Freeze a deterministic, schema-validated identity and provenance inventory across the current seed, pilot candidate, evidence-packet, and governed source-registry artifacts without acquiring sources, evaluating claims, or granting promotion authority.

## Initiative and Linear Issue

- Initiative: BioStack Production Readiness & Monetization
- Issue: KEO-75
- Track: M3 — Data & Intelligence Coverage / governed corpus preparation

## Starting State

- Starting branch: `main`
- Immutable starting commit: `e99c598fd6a0d64383f651f0ab1d5ddc2df00d8a`
- Parcel branch: `codex/keo-75-corpus-inventory`
- Isolated worktree: `D:\Repos\BioStack-keo75-corpus-inventory`

## Objective and Expected Artifact

- Add a deterministic metadata-only inventory builder.
- Validate all current seed records, the pilot candidate batch, all pilot evidence packets, and the source registry against their repository schemas before inventorying them.
- Record current set overlap/differences, candidate/evidence correspondence, ambiguous identity/external-ID collisions, source-registry readiness counts, and packet authorization counts.
- Produce no generated repository artifact, claim text, source acquisition, or promotion decision.

## Dependencies and Human Gates

- KEO-73 remains In Progress and lacks approved owners/coverage targets for full corpus acceptance.
- KEO-74 remains In Progress; all source entries require human legal/rights/operations approval before acquisition or use.
- KEO-75 remains Backlog. This parcel is dependency-safe preparation only and does not change that state.
- Merge remains human-controlled.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/CorpusIdentityInventoryBuilder.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-75-CORPUS-IDENTITY-INVENTORY-001.md`

## Prohibited Actions and Do-Not-Touch Boundaries

- No changes to schemas, seeds, candidates, evidence packets, source registry, taxonomy, coverage matrix, loaders, authorizers, promotion, runtime, DI, API, frontend, CI, deployment, or live environments.
- No browsing, source acquisition, model/agent extraction, new compound/biomarker/medication entry, generated claim, raw customer data, PII, secrets, or credentials.
- No legal/license/source approval, source activation, acquisition enablement, evidence-tier decision, human review decision, promotion eligibility, semantic truth, safety conclusion, threshold, or production claim.
- Do not persist claim statements, dosing guidance, source URLs, raw packets, or full seed records in the inventory output.

## Acceptance Criteria

- Every input is schema-valid before projection.
- The inventory includes identity/provenance metadata only and is deterministic, stable, sorted, and timestamp-free.
- Current repository counts and overlap/difference sets are recorded by tests.
- Candidate/evidence correspondence is explicit.
- Source readiness remains decomposed into observed rights, operations, acquisition, and authorization counts without granting authority.
- Model/network flags remain false and limitations prohibit semantic or release interpretation.
- Focused and full KnowledgeWorker test lanes pass serially.
- Exactly the three Allowed Files change and scoped diff hygiene passes.
- Standard passive security review finds no raw-claim/data persistence, authorization escalation, model/network, runtime, or deployment path.

## Validation Commands

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~CorpusIdentityInventoryBuilderTests`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1`
- `git diff --check origin/main...HEAD`

## Retry Limit and Escalation Rule

Retry at most three materially similar failed validation cycles. Stop immediately if implementation requires source browsing/acquisition, raw claim content, taxonomy/coverage targets, a source approval, promotion policy, a model/network call, or any file outside Allowed Files.

## Required Session Handoff

Record starting/ending commits, exact changed paths, observed counts/collisions, tests, security review, draft PR, hosted checks, remaining human gates, and next safe action. Preserve KEO-75 as Backlog and the release as NO-GO / HOLD.

## Status

Ready for draft-PR publication.

## Validation Results

- Focused `CorpusIdentityInventoryBuilderTests`: 3 passed, 0 failed, 0 skipped.
- Full `BioStack.KnowledgeWorker.Tests`: 244 passed, 0 failed, 0 skipped.
- KnowledgeWorker build: succeeded with 0 warnings and 0 errors.
- Current validated inventory: 49 seed records, 16 pilot candidates, 16 evidence packets, and 13 source-registry entries.
- Exact canonical-ID overlap: 10; seed-only IDs: 39; candidate-only IDs: 6.
- Candidate/evidence correspondence: zero candidates missing evidence and zero evidence packets without candidates.
- Observed source state: 0 rights-approved, 0 operations-active, 0 acquisition-enabled, and 0 registry-authorized evidence packets.
- Identity collision report: two tokens (`creatine` and `creatine-monohydrate`) map candidate `creatine` and seed `creatine-monohydrate`; external-identifier collision count is zero.
- Stable JSON projection is timestamp-free, model/network flags are false, and tests confirm no claim, statement, dosing-guidance, or source-URL fields are serialized.
- One validation correction occurred: the prior read-only audit described 11 loosely mapped entities, while the implemented exact canonical-ID contract observed 10. The frozen test now records exact canonical identity and leaves alias reconciliation visible as collision evidence rather than silently merging it.

## Security Review

- Depth/mode: standard, local, passive source and diff review.
- Finding: no blocking security finding in this bounded inventory parcel.
- Verified controls: all inputs are repository-local and schema-validated before projection; the output type contains counts, canonical IDs, normalized collision keys, source-state counts, booleans, and limitations only; it does not serialize raw evidence packets, claim statements, dosing guidance, source URLs, customer data, PII, secrets, tokens, or credentials; source authorization is observed through the existing fail-closed authorizer without changing packets or registry state; model/network flags are false; no DI, runtime, database, API, frontend, CI, deployment, or live-environment path is added.
- Residual risk: normalized-token collisions require human identity resolution and must not be auto-merged; exact canonical overlap does not prove entity equivalence, corpus completeness, evidence correctness, legal rights, or promotion eligibility.
- Coverage gaps: taxonomy owners/targets, biomarker/medication extensions, source legal/rights approval, source activation/acquisition, semantic claim review, duplicate/conflict resolution decisions, promotion, runtime, staging, production, and final qualification remain uncleared.

## Session Handoff

- Starting commit: `e99c598fd6a0d64383f651f0ab1d5ddc2df00d8a`.
- Ending commit and draft PR: to be recorded after final scoped diff validation and publication.
- Files changed: exactly the three Allowed Files.
- Tests passed: 3 focused and 244 full.
- Tests failed: 0 product tests; one corrected audit-assumption mismatch as recorded above.
- Decisions needed: KEO-73 owners/targets and KEO-74 human source approvals remain required before acquisition or corpus expansion.
- Blockers: KEO-75 remains Backlog; no source is currently authorized and the creatine identity mapping requires human resolution before any merge/promotion behavior.
- Next safe action: inspect the exact three-file diff, publish a draft PR, verify hosted checks, and reconcile evidence to KEO-75 without changing its status.
- Do not touch: corpus inputs, schemas, source registry, claims, promotion/runtime paths, models, network, deployment, production, credentials, or the user's primary dirty worktree.
