# Parcel: KEO-78-CANDIDATE-ENVELOPE-003

## Goal

Define and validate a bounded, metadata-only envelope for pre-recorded untrusted candidate declarations without storing raw prompts/output or granting truth, scoring, promotion, or effect authority.

## Initiative

BioStack Production Readiness & Monetization

## Project Track

M3 — Data & Intelligence Coverage / deterministic evaluation harness

## Wave

W3

## Branch

`codex/keo-78-candidate-envelope-contract`

## Worktree

`D:\Repos\BioStack-keo78-candidate-envelope`

## Dependencies

- PR #195 / merge commit `f0f4f752ff46a72e158eb2a4f33f28d227a6dfa5`
- KEO-73, KEO-76, and KEO-77 merged contracts
- KEO-75 remains a full-acceptance dependency but does not block this synthetic contract parcel

## Integration Surfaces

- Pre-recorded candidate metadata -> offline evaluation harness

## Security Gate

Security review required before release. This parcel narrows the untrusted-input boundary; it does not clear privacy, injection, safety, refusal, model, or production gates.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Schemas/evaluation-candidate-output-envelope.schema.json`
- `research/protocol-intelligence/evaluation-candidate-output-envelope.v1.json`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/EvaluationCandidateOutputEnvelopeLoader.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/EvaluationCandidateOutputEnvelopeLoaderTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-78-CANDIDATE-ENVELOPE-003.md`

## Forbidden

- No raw prompts, raw outputs, customer data, PII, credentials, tokens, URLs, or environment values.
- No model, network, database, runtime, tool, receipt, promotion, or deployment execution.
- No semantic scoring, expected-truth comparison, thresholds, baseline, waiver, pass/fail verdict, or production claim.
- No changes to existing KEO-73/76/77 contracts, report builders, runtime registration, DI, worker modes, CI, or deployment files.

## Out of Scope

Candidate invocation, raw-output retention, claims/spans, privacy detectors, semantic evaluation, deterministic comparison metrics, report integration, CI artifact upload, threshold enforcement, and live qualification.

## Existing Patterns To Follow

- `AdversarialQueryCorpusLoader.cs` — schema-first, version-bound, fail-closed offline loader.
- `StructuralEvaluationReportBuilder.cs` — explicit `not_evaluated` and no-live-claim boundary.

## Contract

- Draft 2020-12 closed JSON schema.
- Maximum artifact size 1 MiB and JSON depth 32.
- Synthetic-only metadata: known case IDs, SHA-256 digests, enumerated declarations, receipt codes, and citation source IDs.
- Raw inputs and outputs are structurally prohibited.
- Results are unique and ordinally sorted; partial result sets are allowed and carry no coverage verdict.
- Candidate declarations remain untrusted even when they disagree with KEO-77 expected values.
- Loader performs no model/network access and returns `effectAuthority: none`.

## Required Tests

- Current contract fixture loads with exact version bindings.
- Schema is closed and Draft 2020-12.
- Raw data fields are absent and rejected.
- Unknown cases, unordered results, version mismatch, and oversized input fail closed.
- A structurally valid disagreement remains untrusted data rather than becoming truth.

## Acceptance Criteria

- Focused tests pass serially.
- Full KnowledgeWorker tests pass serially with zero warnings.
- Exactly the five Allowed Files change.
- Diff hygiene passes.
- Security review finds no raw-data, effect-authority, model, network, runtime, or deployment path in scope.

## Verification

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~EvaluationCandidateOutputEnvelopeLoaderTests`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1`
- `git diff --check origin/main...HEAD`

## Evidence Required

- Starting and ending commits.
- Focused and full test totals.
- Security coverage and gaps.
- Draft PR and completed hosted run IDs.

## Collision Risk

Low. All five files are new; no existing contract or serialization point changes.

## Security Review

- Depth/mode: standard, local, passive source review.
- Findings: no blocking finding in this parcel.
- Controls verified: closed schema; 1 MiB and depth-32 parse bounds; exact pinned source paths; IDs/enums/SHA-256 metadata only; raw input/output/customer data prohibited; no model/network/database/tool/runtime path; candidate declarations remain untrusted; effect authority is `none`.
- Coverage gaps: raw-output retention/access design, semantic truth and scoring, source-ID resolution, privacy/secret detector categories, model transport, runtime/CI integration, and live environment behavior were not implemented or cleared.
- Release impact: security clearance remains blocked for future privacy, injection, safety, refusal, model, and production surfaces.

## PR Notes

- What changed: Added a closed, bounded metadata envelope and fail-closed loader for pre-recorded candidate declarations.
- Why: Establish the untrusted input boundary required before deterministic KEO-78 comparison metrics.
- Risk: Downstream code must not treat candidate declarations as truth or authority.
- Verification: Focused/full serial tests, diff inspection, and hosted checks.
- Evidence: Parcel contract, tests, commit, PR, workflows, and Linear comment.

## Session Handoff

- Starting commit: `f0f4f752ff46a72e158eb2a4f33f28d227a6dfa5`
- Ending commit: final parcel commit recorded in GitHub and Linear evidence
- Files changed: the five Allowed Files only
- Commands run: focused and full serial `dotnet test` lanes; scoped diff and security-boundary inspection
- Tests passed: 10 focused tests; 234 full KnowledgeWorker tests; 0 warnings
- Tests failed: 2 initial test-fixture failures corrected; 1 existing provenance-contract failure corrected by pinned source paths; final reruns 0 failed
- Decisions needed: semantic truth, metric definitions, thresholds, baseline/waiver, raw-output retention/access, and pinned candidate/environment
- Blockers: KEO-75 and human policy decisions for full acceptance
- Next safe action: validate, publish a draft PR, and keep KEO-78 In Progress
- Do not touch: existing contracts, raw data, runtime, CI, deployment, model execution, or live environments

## Stop-and-Report Rule

Stop if implementation needs raw content, semantic interpretation, policy thresholds, an existing contract change, or any file outside Allowed Files.
