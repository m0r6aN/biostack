# Parcel: KEO-78-CORPUS-INVENTORY-REPORT-INTEGRATION-007

## Goal

Embed KEO-75's merged, metadata-only corpus identity inventory in BioStack's deterministic versioned offline evaluation report and record its repository state as observed, without creating semantic, legal, safety, runtime, release, or production claims.

## Initiative

BioStack Production Readiness & Monetization

## Linear Issue and Track

- Issue: KEO-78
- Track: M3 — Data & Intelligence Coverage / deterministic evaluation harness

## Starting State

- Starting branch: `main`
- Immutable starting commit: `db3e1e5bfa3be55241d564e653a007402ce47c8c`
- Parcel branch: `codex/keo-78-corpus-inventory-report`
- Isolated worktree: `D:\Repos\BioStack-keo78-corpus-inventory-report`

## Objective and Expected Artifact

- Add the already validated `CorpusIdentityInventorySnapshot` to the offline structural report payload.
- Bump the report's additive minor version from 1.1.0 to 1.2.0 while retaining the stable v1 report family and filename.
- Record one observed metric for deterministic repository identity, provenance, and authorization state.
- Update the existing read-only CI boundary assertion for the additive report contract and fail-closed inventory values.
- Preserve all semantic and runtime metrics as `not_evaluated` and preserve the overall verdict as `not_evaluated`.

## Dependencies

- PR #201 merged as `db3e1e5bfa3be55241d564e653a007402ce47c8c`.
- The KEO-75 corpus identity inventory is the approved input contract.
- The KEO-78 report version 1.1.0 and existing structural comparison are the approved report contract.

## Integration Surfaces

- KEO-75 repository corpus inventory -> KEO-78 versioned offline report.
- Versioned report builder -> existing offline CLI and CI artifact workflow.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/StructuralEvaluationReportBuilder.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs`
- `.github/workflows/structural-evaluation-report.yml`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-78-CORPUS-INVENTORY-REPORT-INTEGRATION-007.md`

## Prohibited Actions and Do-Not-Touch Boundaries

- No changes to inventory, snapshot, comparator, loader, schema, fixture, candidate, expected-output, runtime, DI, worker, CLI, deployment, Azure, model, billing, auth, or production code.
- The only CI change is the existing offline artifact workflow's version/scope/inventory boundary assertion; no triggers, permissions, actions, artifact contents, secrets, retention, or deployment behavior may change.
- No raw claims, dosing guidance, source URLs, customer data, health data, PII, secrets, credentials, tokens, environment values, or raw model input/output in the report.
- No semantic interpretation, factuality/safety assertion, legal or source approval, acquisition authority, identity resolution, rate, score, threshold, baseline, waiver, regression decision, pass/fail verdict, or production-readiness claim.
- No model, network, database, external provider, live-data, receipt, deployment, restore, promotion, or live-environment execution.

## Contract Amendment

### Contract

`StructuralEvaluationReport` version 1.x / `biostack-structural-evaluation-report.v1.json`

### Reason

PR #201 introduced the fail-closed metadata inventory required to connect KEO-75 repository state to KEO-78's deterministic report without inventing semantic truth.

### Change

- Bump `reportVersion` from 1.1.0 to 1.2.0.
- Change report scope to `offline-structural-declaration-and-corpus-inventory`.
- Add the complete `CorpusIdentityInventorySnapshot` under `payload.corpusInventory` and bind it under the existing canonical payload SHA-256.
- Add `governed_corpus_identity_inventory` with status `observed` and a deterministic reason code.
- Preserve `partial`, `pending-approval`, `not_evaluated`, candidate-untrusted, no-effect, no-model, and no-network boundaries.

### Migration Notes

This is an additive minor-version amendment inside the v1 report family. Consumers pinned to the exact 1.1.0 payload shape must update before consuming 1.2.0.

## Acceptance Criteria

- The report embeds the exact current inventory counts, differences, collision signals, and fail-closed source authorization counts.
- The inventory and its observed metric are included in the canonical payload digest.
- Report serialization remains deterministic and timestamp-free.
- The existing CI artifact workflow asserts report version 1.2.0 and the fail-closed inventory boundary without widening permissions or artifact contents.
- No metric status is `passed` or `failed`; all unsupported semantic/runtime metrics remain `not_evaluated`.
- Focused and full KnowledgeWorker tests pass serially.
- The KnowledgeWorker project builds serially with zero errors.
- Exactly the four allowed files change and scoped diff hygiene passes.
- Standard passive security review finds no raw-data persistence, trust escalation, approval bypass, effect authority, model, network, runtime, CI, deployment, or live-environment path.

## Validation Commands

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~StructuralEvaluationReportBuilderTests`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1`
- `dotnet build backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj -m:1`
- Generate the report locally and apply the workflow's `jq` boundary expression when `jq` is available; otherwise require hosted workflow evidence.
- `git diff --check origin/main...HEAD`

## Human Gates

- Merge remains human-controlled.
- KEO-73 coverage targets/owners, KEO-74 source/legal approvals, KEO-75 identity resolution/promotion, semantic metric policies, thresholds, regression blocking, pinned model/runtime execution, live environments, and final KEO-78 completion remain unapproved and out of scope.

## Retry Limit and Escalation Rule

Retry at most three materially similar failed validation cycles. Stop immediately if implementation requires semantic policy, raw content, a source/legal or identity decision, thresholds, secrets, broader artifacts, a model/network call, deployment behavior, or any file outside Allowed Files.

## Required Session Handoff

Record starting and ending commits, exact changed paths, validation totals/results, security findings and gaps, draft PR, hosted run evidence, remaining gates, and next safe action. Preserve KEO-78 as In Progress and the release as NO-GO / HOLD.

## Status

Ready for draft-PR publication.

## Validation Results

- Focused `StructuralEvaluationReportBuilderTests`: 4 passed, 0 failed, 0 skipped.
- Full `BioStack.KnowledgeWorker.Tests`: 244 passed, 0 failed, 0 skipped.
- `BioStack.KnowledgeWorker` build: succeeded with 0 warnings and 0 errors.
- Standalone report CLI: generated the version 1.2.0 report twice from the pinned repository state.
- Both local reports were 16,309 bytes with SHA-256 `fd1251ea0b25c9bd33658b0768867030bd196fb7268febfac39b7f4420e5010a`.
- Local PowerShell boundary validation passed for version, scope, `not_evaluated` verdict, corpus counts, fail-closed source authorization, and no-model/no-network fields.
- Local `jq` was unavailable, so the exact workflow expression still requires hosted GitHub Actions evidence.
- Scoped diff hygiene passed; exactly the four allowed files changed.
- No product-test validation retry failed. One local validation attempt could not compute hashes because `Get-FileHash` was unavailable in the nested PowerShell environment; the same files were then independently compared with the .NET SHA-256 implementation and matched.

## Security Review

- Depth/mode: standard, local, passive source/config/diff review.
- Finding: no blocking security finding in this bounded metadata/report/CI assertion lane.
- Verified controls: report inputs remain schema-validated repository fixtures; persisted inventory contains canonical metadata, counts, collision signals, and fail-closed source authorization state rather than raw claims, source URLs, health/customer data, or model text; the report and nested comparison retain `not_evaluated`; candidate declarations remain untrusted; effect authority remains `none`; model/network flags remain false; the existing workflow keeps `contents: read`, references no secrets, uploads the same single JSON report, and changes no trigger, action, permission, retention, deployment, or external-provider behavior.
- Residual risk: the report exposes repository canonical IDs and collision owners to users who can access workflow artifacts; exact count assertions intentionally fail on future corpus drift and require an explicit contract update; GitHub artifact visibility/retention remain repository-setting concerns; existing action version tags are not immutable commit pins.
- Coverage gaps: no semantic correctness, factuality, provenance resolution, legal/source approval, safety/refusal quality, privacy detector, threshold/regression policy, identity resolution, model/runtime, staging, production, deployment, or live-environment behavior was reviewed or cleared.

## Session Handoff

- Starting commit: `db3e1e5bfa3be55241d564e653a007402ce47c8c`.
- Ending commit, draft PR, and hosted checks: record after publication.
- Files changed: exactly the four Allowed Files.
- Tests passed: 4 focused and 244 full; build, two-run CLI generation, local boundary validation, stable hash, and diff hygiene passed.
- Tests failed: 0 product tests.
- Decisions needed: KEO-73 coverage targets/owners, KEO-74 source approvals, KEO-75 identity resolution/promotion, semantic policies, thresholds, pinned runtime/model, and live qualification remain human gates.
- Next safe action: commit and publish a draft PR, then require the hosted artifact workflow to validate the exact `jq` boundary assertion.
- Do not touch: source artifacts, loaders, schemas, raw data, runtime, deployment, Azure, production, credentials, or the user's primary dirty worktree.
