# Parcel: KEO-78-REPORT-COMPARISON-INTEGRATION-005

## Goal

Embed the merged structural declaration comparison in BioStack's deterministic versioned offline evaluation report without creating semantic, policy, runtime, or release claims.

## Initiative

BioStack Production Readiness & Monetization

## Project Track

M3 — Data & Intelligence Coverage / deterministic evaluation harness

## Wave

Integration

## Branch

`codex/keo-78-report-comparison-integration`

## Worktree

`D:\Repos\BioStack-keo78-report-comparison`

## Dependencies

- PR #195 versioned structural report
- PR #198 structural comparator, merged as `243067b04dc362bfded8f99ea5c91c822d3a7223`

## Integration Surfaces

- Structural snapshot -> versioned evaluation report
- Structural declaration comparison -> versioned evaluation report

## Security Gate

Security review required before merge because validated untrusted candidate metadata is persisted in an evaluation artifact. This parcel cannot clear privacy, semantic, model, runtime, deployment, or production gates.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/StructuralEvaluationReportBuilder.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-78-REPORT-COMPARISON-INTEGRATION-005.md`

## Forbidden

- No changes to loaders, schemas, fixtures, candidate/expected contracts, comparator, snapshot builder, runtime registration, DI, worker modes, CI, deployment, or generated report files.
- No raw prompts, outputs, claims, health/customer data, PII, secrets, credentials, tokens, environment values, or operational unsafe guidance.
- No semantic interpretation, factuality/safety-quality assertion, rate, score, threshold, baseline, waiver, regression gate, pass/fail verdict, or production claim.
- No model, network, database, runtime, receipt, promotion, deployment, or live-environment execution.

## Out of Scope

CI report generation/upload, runtime invocation, raw-output retention, claim/span evaluation, privacy detectors, semantic metrics, threshold enforcement, latency/cost measurement, staging/production validation, and final KEO-78 acceptance.

## Existing Patterns To Follow

- `StructuralEvaluationReportBuilder.cs` — deterministic canonical payload and explicit `not_evaluated` policy.
- `StructuralEvaluationComparator.cs` — candidate-untrusted structural observations and limitations.
- `StructuralEvaluationReportBuilderTests.cs` — stable serialization and canonical SHA-256 evidence.

## Contract Amendment

### Contract

`StructuralEvaluationReport` version 1.x / `biostack-structural-evaluation-report.v1.json`

### Reason

PR #198 introduced the validated comparison consumer required by the report. The previous report recorded only aggregate snapshot structure.

### Change

- Bump `reportVersion` from `1.0.0` to `1.1.0`; retain report kind and the stable `v1` filename/family.
- Change report scope to `offline-structural-and-declaration-comparison`.
- Add the complete `StructuralEvaluationComparison` object to the canonical payload and therefore bind it under the existing payload SHA-256.
- Add `structural_declaration_comparison` as `observed` with a deterministic reason code.
- Update the unevaluated citation/provenance reason to reflect that structured candidate metadata now exists while approved source-resolution/provenance policy does not.
- Preserve `EvaluationStatus: partial`, `PolicyStatus: pending-approval`, and `OverallVerdict: not_evaluated`.

### Affected Tracks and Scenarios

- Track: KEO-78 deterministic evaluation report.
- Scenario: offline report deterministically embeds snapshot and structural comparison against the current repository artifacts.
- Downstream readers of report version 1.x must tolerate the additive `comparison` payload field and inspect `reportVersion`.

### Migration Notes

This is an additive minor-version amendment inside the v1 report family. No runtime or external consumer is registered in the repository. Consumers pinned to the exact 1.0.0 payload shape must update before consuming 1.1.0.

### Status

Approved for this serialized integration parcel; merge remains human-controlled.

## Required Tests

- Current report is version 1.1.0 and embeds the partial, candidate-untrusted, no-verdict comparison.
- Both structural metrics are `observed`; all unsupported semantic/runtime metrics remain `not_evaluated`.
- No metric status is `passed` or `failed`.
- Canonical payload SHA-256 includes the comparison.
- JSON remains stable, timestamp-free, and explicit about both report and comparison verdicts.
- Versioned file writing remains deterministic and uses the existing v1 filename.

## Acceptance Criteria

- Focused report tests pass serially.
- Full KnowledgeWorker tests pass serially with zero warnings.
- Exactly the three allowed files change.
- Scoped diff hygiene passes.
- Standard passive security review finds no raw-data persistence, trust escalation, effect authority, model, network, runtime, CI, or deployment path.

## Verification

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~StructuralEvaluationReportBuilderTests`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1`
- `git diff --check origin/main...HEAD`

## Evidence Required

- Starting and ending commits.
- Exact changed paths.
- Focused and full test totals.
- Security findings and coverage gaps.
- Draft PR and terminal hosted run IDs.
- Linear KEO-78 evidence comment.

## Collision Risk

High serialization risk but isolated execution. The report builder and report payload are shared integration contracts; no other write lane may touch them until this parcel completes.

## Validation Results

- Focused `StructuralEvaluationReportBuilderTests`: 4 passed, 0 failed, 0 warnings.
- Full `BioStack.KnowledgeWorker.Tests`: 241 passed, 0 failed, 0 warnings.
- No failed retry cycle occurred.

## Security Review

- Depth/mode: standard, local, passive source and diff review.
- Finding: no blocking finding in this bounded report integration.
- Verified controls: the report consumes only validated snapshot/comparison records; persisted candidate material is limited to versions, SHA-256 digests, identifiers, enums, booleans, and deterministic counts; candidate declarations remain explicitly untrusted; both nested and report-level verdicts remain `not_evaluated`; policy remains pending; effect authority remains `none`; the canonical payload digest covers the comparison; serialization is stable and timestamp-free; no raw prompt/output, customer data, secret, model, network, database, runtime, CI, deployment, or live-environment path was added.
- Residual risk: downstream readers pinned to the exact 1.0.0 shape must update for the additive 1.1.0 payload, and readers must not interpret `observed` structural equality as semantic correctness or a release pass.
- Coverage gaps: semantic factuality, provenance source resolution, safety/refusal quality, privacy leakage, rates/thresholds/baselines/waivers, model transport, runtime/CI artifact execution, staging, production, and final qualification remain uncleared.

## Retry Limit and Escalation

Retry at most three materially similar failed validation cycles. Stop if integration requires raw content, semantic policy, thresholds, a major report-version break, runtime/CI wiring, or any path outside Allowed Files.

## PR Notes

- What changed: embedded the structural comparison in the v1 report family under report version 1.1.0.
- Why: complete the first deterministic report integration surface after PR #198.
- Risk: downstream consumers could overstate structural equality or assume exact 1.0.0 shape compatibility.
- Verification: focused/full serial tests, canonical digest test, passive security review, and scoped diff inspection.
- Evidence: parcel record, commit, PR, hosted workflows, and Linear comment.

## Session Handoff

- Starting commit: `243067b04dc362bfded8f99ea5c91c822d3a7223`
- Ending commit: recorded in the draft PR and Linear after final validation
- Files changed: the three Allowed Files only
- Commands run: focused/full serial tests and scoped diff/security inspection
- Tests passed: 4 focused; 241 full; 0 warnings
- Tests failed: 0; no failed retry cycle
- Decisions needed: semantic metric definitions, thresholds/baseline/waiver policy, raw-output access, runtime/CI artifact policy, and pinned candidate/environment
- Blockers: KEO-75 and human policy decisions for full KEO-78 acceptance
- Next safe action: validate and publish a draft PR; after merge, stop before semantic metrics or runtime/CI execution without approved policy
- Do not touch: approved loaders/contracts/comparator, raw data, runtime, CI, deployment, models, or live environments

## Stop-and-Report Rule

Stop if implementation needs a product/policy decision, raw content, semantic interpretation, thresholds, a major-version break, runtime/CI wiring, or a file outside Allowed Files.
