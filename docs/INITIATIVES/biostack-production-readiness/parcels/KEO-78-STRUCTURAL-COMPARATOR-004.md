# Parcel: KEO-78-STRUCTURAL-COMPARATOR-004

## Goal

Produce deterministic, metadata-only structural comparisons between validated KEO-77 expected declarations and validated pre-recorded KEO-78 candidate declarations.

## Initiative

BioStack Production Readiness & Monetization

## Project Track

M3 — Data & Intelligence Coverage / deterministic evaluation harness

## Wave

W3 thin vertical spike

## Branch

`codex/keo-78-structural-comparator`

## Worktree

`D:\Repos\BioStack-keo78-structural-comparator`

## Dependencies

- PR #196 candidate-output envelope, merged as `f055d7f73e3c2d20a90eac39681c4fe04855dc95`
- PR #197 KEO-77 expected projection, merged as `24f6a185248763b73b105e997391941dd40b92ed`

## Integration Surfaces

- Validated KEO-77 expected metadata -> deterministic KEO-78 comparison
- Validated untrusted candidate metadata -> deterministic KEO-78 comparison

## Security Gate

Security review required before merge. This parcel compares safety/refusal/provenance metadata but cannot clear semantic, privacy, model, runtime, deployment, or production gates.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/StructuralEvaluationComparator.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationComparatorTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-78-STRUCTURAL-COMPARATOR-004.md`

## Forbidden

- No changes to KEO-73/76/77 contracts, corpus/schema/fixture values, candidate-envelope contract, report/snapshot builders, runtime registration, DI, worker modes, CI, deployment, or generated artifacts.
- No raw prompts, outputs, claims, health/customer data, PII, credentials, tokens, production values, or operational unsafe guidance.
- No model, network, database, runtime, receipt, promotion, deployment, or live-environment execution.
- No semantic interpretation, factuality/safety-quality claim, rate, score, threshold, baseline, waiver, regression gate, pass/fail verdict, or production claim.

## Out of Scope

Report integration, artifact writing, CI upload, raw-output retention, claim/span evaluation, privacy detectors, semantic metrics, threshold enforcement, runtime invocation, staging/production validation, and final KEO-78 acceptance.

## Existing Patterns To Follow

- `EvaluationCandidateOutputEnvelopeLoader.cs` — bounded untrusted metadata contract.
- `AdversarialQueryCorpusLoader.cs` — validated expected-declaration projection.
- `StructuralEvaluationSnapshotBuilder.cs` — deterministic offline observation with explicit limitations.
- `StructuralEvaluationReportBuilder.cs` — `not_evaluated` and no-verdict boundary.

## Contract

- Compare the eight fields shared by expected and candidate declarations using ordinal equality and exact ordered-ID-list equality.
- Record per-case field matches and per-field compared/exact/mismatch counts.
- Bind the comparison to the expected-corpus version, candidate-envelope version, and candidate-configuration SHA-256.
- Record missing candidate case IDs and `partial` versus `complete` structural candidate coverage.
- Partial results are valid and carry no corpus-wide verdict.
- Candidate declarations remain untrusted even when every structural field matches.
- `OverallVerdict` remains `not_evaluated`; comparator effect authority remains `none`.
- The comparator performs no model or network access and inspects no raw text.

## Required Tests

- Current four-result contract fixture produces exact structural equality with partial corpus coverage and no verdict.
- A single scalar mismatch changes only its exact field/case counts.
- Ordered ID-list differences are detected structurally.
- Unknown candidate cases and version mismatches fail closed.
- Inconsistent expected projections and candidate trust/runtime flags fail closed.

## Acceptance Criteria

- Focused tests pass serially.
- Full KnowledgeWorker tests pass serially with zero warnings.
- Exactly the three allowed files change.
- Scoped diff hygiene passes.
- Standard passive security review finds no raw-data, trust-escalation, effect-authority, model, network, runtime, CI, or deployment path.

## Verification

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~StructuralEvaluationComparatorTests`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1`
- `git diff --check origin/main...HEAD`

## Evidence Required

- Starting and ending commits.
- Exact changed paths.
- Focused and full test totals.
- Security findings and coverage gaps.
- Draft PR and completed hosted run IDs.
- Linear KEO-78 evidence comment.

## Collision Risk

Low. All three files are new; existing contracts and report serialization points are read-only.

## Validation Results

- Focused `StructuralEvaluationComparatorTests`: 6 passed, 0 failed, 0 warnings.
- Full `BioStack.KnowledgeWorker.Tests`: 241 passed, 0 failed, 0 warnings.
- Initial pre-hardening runs also passed (5 focused / 240 full); no failed retry cycle occurred.

## Security Review

- Depth/mode: standard, local, passive source and diff review.
- Finding: no blocking finding in this bounded comparator.
- Verified controls: both inputs come from closed, fail-closed loaders; versions and candidate effect/trust flags are checked again; expected and candidate IDs must be unique and ordinally sorted; the expected projection must exactly match corpus case IDs; comparison output is bound to corpus, envelope, candidate-configuration, and per-case output digests; only enums, booleans, and identifier lists are compared; partial coverage is explicit; candidate declarations remain untrusted; the verdict remains `not_evaluated`; effect authority remains `none`; no raw text, model, network, database, runtime, CI, deployment, or live path was introduced.
- Residual risk: a downstream consumer could mislabel structural equality as semantic correctness unless it preserves the comparison scope and limitations.
- Coverage gaps: semantic factuality, safety/refusal quality, privacy leakage, source resolution, rates and thresholds, model transport, report/CI integration, runtime behavior, staging, production, and final qualification remain uncleared.

## Retry Limit and Escalation

Retry at most three materially similar failed validation cycles. Stop if implementation requires raw content, semantic policy, thresholds, report wiring, a contract amendment, or any path outside Allowed Files.

## PR Notes

- What changed: added a pure structural comparator and bounded tests.
- Why: validate the merged expected/candidate metadata boundary before report integration.
- Risk: downstream consumers could overstate exact structural equality as semantic correctness.
- Verification: focused/full serial tests, passive security review, and scoped diff inspection.
- Evidence: parcel record, commit, PR, hosted workflows, and Linear comment.

## Session Handoff

- Starting commit: `24f6a185248763b73b105e997391941dd40b92ed`
- Ending commit: recorded in the draft PR and Linear evidence after final validation
- Files changed: the three Allowed Files only
- Commands run: focused/full serial test lanes and scoped diff/security inspection
- Tests passed: 6 focused; 241 full; 0 warnings
- Tests failed: 0 final; no failed retry cycle
- Decisions needed: semantic metric definitions, threshold/baseline/waiver policy, raw-output access, and pinned candidate/environment
- Blockers: KEO-75 and human policy decisions for full KEO-78 acceptance
- Next safe action: validate and publish a draft PR; after merge, consider a separate report-integration parcel
- Do not touch: approved contracts, raw data, report/snapshot builders, runtime, CI, deployment, models, or live environments

## Stop-and-Report Rule

Stop if implementation needs a product or policy decision, a contract change, semantic interpretation, thresholds, raw content, report wiring, or a file outside Allowed Files.
