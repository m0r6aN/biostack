# Parcel: KEO-78-VERSIONED-REPORT-002

## Goal

Persist the merged structural evaluation snapshot as a deterministic, versioned offline report that marks unsupported metrics as `not_evaluated` and makes no regression or production-readiness claim.

## Initiative

BioStack Production Readiness & Monetization

## Project Track

M3 — Data & Intelligence Coverage / deterministic evaluation harness

## Wave

W2

## Branch

`codex/keo-78-versioned-report-artifact`

## Worktree

`D:\Repos\BioStack-keo78-versioned-report-artifact`

## Dependencies

- PR #194 / merge commit `9b4370646eeb2917f91d91ff7f70c511192479ee`
- KEO-73, KEO-76, and KEO-77 merged artifact contracts
- KEO-75 remains an acceptance dependency but does not block this structural-only parcel

## Integration Surfaces

- Offline evaluation artifact contract only

## Security Gate

Security review required before release for privacy-leakage, prompt-injection, safety, and refusal metrics. This parcel supplies no such semantic determination.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/StructuralEvaluationReportBuilder.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-78-VERSIONED-REPORT-002.md`

## Forbidden

- Do not change corpus, taxonomy, fixture, or adversarial schemas.
- Do not change runtime registration, dependency injection, worker modes, CI workflows, deployment files, or generated output.
- Do not invoke a model, network endpoint, database, or live environment.
- Do not define thresholds, baselines, waivers, semantic truth, or an overall pass/fail verdict.
- Do not store raw prompts, model text, customer data, secrets, tokens, URLs, or environment values.

## Out of Scope

Candidate-output envelopes, semantic scoring, regression policy approval, CI artifact upload, live-model execution, latency/cost measurement, and production qualification.

## Existing Patterns To Follow

- `StructuralEvaluationSnapshotBuilder.cs` — merged deterministic structural source.
- `ProtocolIntelligenceEvaluationJob.cs` — versioned JSON report persistence pattern, without inheriting its runtime timestamp or promotion verdict.

## Contract

- Fixed report version and filename.
- Canonical SHA-256 digest over the serialized report payload.
- Structural coverage is `observed`, not `passed`.
- Metrics lacking approved truth and policy are `not_evaluated` with stable reason codes.
- Policy remains `pending-approval`; overall verdict remains `not_evaluated`.
- Report contains no run timestamp and is byte-stable for identical source artifacts.

## Required Tests

- Report contract and status vocabulary.
- Canonical payload digest.
- Stable JSON without runtime claims.
- Versioned deterministic file persistence.

## Acceptance Criteria

- Repeated builds produce identical JSON.
- Repeated writes produce identical bytes at one versioned filename.
- No metric is reported as passed or failed.
- No model/network/live-environment claim appears.
- Focused and full KnowledgeWorker tests pass serially.

## Verification

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~StructuralEvaluationReportBuilderTests`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1`
- `git diff --check origin/main...HEAD`

## Evidence Required

- Exact starting and ending commits.
- Focused and full test results.
- Diff-hygiene result.
- Draft PR and completed hosted-check run IDs.

## Collision Risk

Low. All three files are new; no shared serialization point is changed.

## PR Notes

- What changed: Added deterministic persistence and explicit metric availability states around the merged structural snapshot.
- Why: Advance KEO-78 report persistence without inventing semantic truth or policy.
- Risk: Consumers must not treat `observed` structural facts as a quality or production verdict.
- Verification: Focused and full KnowledgeWorker tests plus hosted checks.
- Evidence: This parcel spec, test output, commit, PR, and workflow runs.

## Session Handoff

- Starting commit: `9b4370646eeb2917f91d91ff7f70c511192479ee`
- Ending commit: final parcel commit recorded in GitHub and Linear evidence (a commit cannot contain its own final hash)
- Files changed: the three Allowed Files only
- Commands run: focused and full serial `dotnet test` commands from Verification; scoped diff hygiene
- Tests passed: 4 focused tests; 224 full KnowledgeWorker tests; 0 warnings
- Tests failed: 0
- Decisions needed: metric owners, truth sources, denominators, thresholds, baseline/waiver policy, retention/access, and live candidate/environment
- Blockers: KEO-75 and human metric-policy decisions for full acceptance
- Next safe action: review the draft PR and retain KEO-78 In Progress until its remaining acceptance gates pass
- Do not touch: runtime, CI, deployment, corpus, schemas, model execution, or live environments

## Stop-and-Report Rule

Stop if implementation requires a threshold, semantic interpretation, candidate-output contract, runtime/CI integration, security decision, or any file outside Allowed Files.
