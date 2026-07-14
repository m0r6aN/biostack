# Parcel: KEO-78-CI-REPORT-ARTIFACT-006

## Goal

Generate BioStack's deterministic structural evaluation report from validated synthetic repository fixtures in CI and retain only the versioned JSON report as a GitHub workflow artifact, without adding semantic, runtime, release, or production claims.

## Initiative

BioStack Production Readiness & Monetization

## Linear Issue and Track

- Issue: KEO-78
- Track: M3 — Data & Intelligence Coverage / deterministic evaluation harness

## Starting State

- Starting branch: `main`
- Immutable starting commit: `5462b2bf1b696d16a45986b3e84614957b7a49c6`
- Parcel branch: `codex/keo-78-ci-report-artifact`
- Isolated worktree: `D:\Repos\BioStack-keo78-ci-report-artifact`

## Objective and Expected Artifact

- Add an offline-only CLI that writes the existing `biostack-structural-evaluation-report.v1.json` contract.
- Add a read-only GitHub Actions workflow that runs the focused contract tests, generates the report twice, verifies byte stability and non-authoritative boundaries, and uploads exactly that JSON file.
- Use GitHub's repository-default artifact retention policy by omitting `retention-days`.

## Dependencies

- PR #199 merged as `5462b2bf1b696d16a45986b3e84614957b7a49c6`.
- Structural report version 1.1.0 and its snapshot/comparison loaders are the approved input contract.
- Owner authorization received on 2026-07-14 for CI-only execution and repository-default artifact retention.

## Integration Surfaces

- Validated synthetic fixtures -> offline report CLI.
- Offline report CLI -> GitHub Actions artifact storage.

## Allowed Files

- `.github/workflows/structural-evaluation-report.yml`
- `backend/tools/BioStack.StructuralEvaluationReportCli/BioStack.StructuralEvaluationReportCli.csproj`
- `backend/tools/BioStack.StructuralEvaluationReportCli/Program.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-78-CI-REPORT-ARTIFACT-006.md`

## Prohibited Actions and Do-Not-Touch Boundaries

- No changes to report, snapshot, comparator, loader, schema, fixture, candidate, expected-output, runtime, DI, worker, deployment, Azure, model, billing, auth, or production code.
- No raw prompts, raw candidate output, customer data, health data, PII, secrets, credentials, tokens, environment values, or repository source archive in the artifact.
- No semantic interpretation, factuality/safety assertion, rate, score, threshold, baseline, waiver, regression decision, pass/fail verdict, or production-readiness claim.
- No model, network, database, external provider, live-data, receipt, deployment, restore, promotion, or live-environment execution.
- No explicit artifact retention duration; repository-default GitHub retention remains authoritative.

## Acceptance Criteria

- The CLI requires explicit repository-root and output-directory arguments and writes only the existing versioned report file.
- The workflow has `contents: read` permissions and no secret references.
- The workflow runs focused report tests serially, generates twice, verifies identical file SHA-256 values, and validates the `partial`, `pending-approval`, `not_evaluated`, candidate-untrusted, no-effect, no-model, and no-network boundaries.
- The upload contains exactly the report JSON and omits `retention-days`.
- Local focused tests, CLI generation, deterministic byte comparison, JSON-boundary validation, builds, and scoped diff hygiene pass.
- Standard passive security review finds no blocking data exposure, trust escalation, secret access, external-network, runtime, deployment, or effect-authority path.

## Validation Commands

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~StructuralEvaluationReportBuilderTests`
- `dotnet build backend/tools/BioStack.StructuralEvaluationReportCli/BioStack.StructuralEvaluationReportCli.csproj -m:1`
- Run the CLI twice against the repository fixtures into a temporary directory and compare file hashes.
- Parse the report and assert the non-authoritative boundary fields used by the workflow.
- `git diff --check origin/main...HEAD`

## Human Gates

- Merge remains human-controlled.
- Semantic metric policies, thresholds, regression blocking, raw-output handling, pinned model/runtime execution, live environments, and final KEO-78 completion remain unapproved and out of scope.

## Retry Limit and Escalation Rule

Retry at most three materially similar failed validation cycles. Stop immediately if the implementation requires raw content, semantic policy, secrets, broader artifact contents, explicit retention changes, a model/network call, deployment behavior, or any file outside Allowed Files.

## Required Session Handoff

Record the starting and ending commits, exact changed paths, validation totals/results, security findings and gaps, draft PR, hosted run and artifact evidence, remaining gates, and next safe action. Preserve KEO-78 as In Progress and the release as NO-GO / HOLD.

## Status

Ready for draft-PR publication.

## Validation Results

- Focused `StructuralEvaluationReportBuilderTests`: 4 passed, 0 failed, 0 skipped.
- Full `BioStack.KnowledgeWorker.Tests`: 241 passed, 0 failed, 0 skipped.
- Standalone CLI build: succeeded with 0 warnings and 0 errors.
- Local CLI execution: succeeded twice against the pinned repository fixtures.
- Generated file SHA-256: `5efb740d1e7628320f9de4ab01769c3d4228b39696b0f6c99e49fbed5ea36f3e` on both executions.
- Local artifact directory: exactly one file, `biostack-structural-evaluation-report.v1.json`, 13,293 bytes.
- Boundary assertions: report version 1.1.0; evaluation `partial`; policy `pending-approval`; report/comparison verdicts `not_evaluated`; candidate declarations untrusted; effect authority `none`; model/network flags false; all metric states limited to `observed` or `not_evaluated`.
- One environment-only validation retry occurred: an initial CLI build with `--no-restore` correctly failed because the new project had no assets file; the required restore/build then passed without a code change.
- Local `actionlint` was unavailable. Workflow parsing and execution remain required hosted evidence.

### Hosted Artifact Evidence

- Draft PR: `#200` (`https://github.com/m0r6aN/biostack/pull/200`).
- Workflow: `Structural Evaluation Report Artifact`, run `29347723815`, job `87135701519`, success against head `46df51fd06fb3ddc676ae77f0593b594c3e981f2`.
- Job started `2026-07-14T15:59:16Z` and completed `2026-07-14T15:59:58Z`; focused tests, two-run generation, boundary validation, and upload all reported success.
- Artifact ID: `8316796736`; name: `biostack-structural-evaluation-report-d62ce0ad1c2c94a1a25f75f021f34dbd94905255`; created `2026-07-14T15:59:55Z`; repository-default expiry reported as `2026-10-12T15:59:13Z`; API archive size 13,101 bytes.
- Independent artifact download contained exactly one file, `biostack-structural-evaluation-report.v1.json`, 12,897 bytes, SHA-256 `3b6c55dd69706b33b640942966df852afb2a3d1d5d1b0f6ba7de05b6edc04d8c`.
- Independently parsed hosted fields matched the approved boundary: version 1.1.0; evaluation `partial`; policy `pending-approval`; both verdicts `not_evaluated`; candidate declarations untrusted; effect authority `none`; model/network flags false.
- The hosted file hash differs from the Windows-local file hash because indented JSON uses platform-native line endings. The workflow proves repeatability on its pinned Ubuntu runner; this parcel does not claim cross-platform byte identity.

## Security Review

- Depth/mode: standard, local, passive source/config/diff review.
- Finding: no blocking security finding in this bounded CI artifact lane.
- Verified controls: workflow permission is `contents: read`; no secret, OIDC, `pull_request_target`, model, network-provider, database, Azure, deployment, or live-data reference exists; the upload path names one JSON file rather than a directory or source archive; `retention-days` is omitted; the CLI reads only the fail-closed validated repository loaders and writes through the existing deterministic report builder; the generated report contains structural identifiers, counts, booleans, enums, and SHA-256 digests, not raw prompts or candidate output; trust, verdict, policy, effect, model, and network boundaries remain explicit and fail closed.
- Residual risk: GitHub artifact visibility and repository-default retention are controlled by repository settings; cross-platform report bytes are not identical because of platform-native line endings; action references follow the repository's existing version-tag convention rather than immutable commit pins.
- Coverage gaps: no semantic correctness, factuality, provenance resolution, safety/refusal quality, privacy detector, threshold/regression policy, model/runtime, staging, production, deployment, or live-environment claim was reviewed or cleared.

## Session Handoff

- Starting commit: `5462b2bf1b696d16a45986b3e84614957b7a49c6`.
- Implementation commit: `46df51fd06fb3ddc676ae77f0593b594c3e981f2`; final evidence-only handoff commit and draft PR head are recorded in GitHub and Linear after publication.
- Files changed: exactly the four Allowed Files.
- Tests passed: 4 focused and 241 full; CLI build/generation/hash/boundary checks passed.
- Tests failed: 0 product tests; one corrected missing-restore environment attempt as recorded above.
- Decisions needed: semantic metrics, thresholds, regression blocking, raw-output handling, pinned model/runtime, and final live qualification remain owner gates.
- Blockers: terminal checks for the final evidence-only PR head remain required; KEO-75 and the unapproved semantic/live gates still block full KEO-78 acceptance.
- Next safe action: publish this evidence-only handoff update, verify terminal hosted checks for the final PR head, and reconcile Linear without merging.
- Do not touch: report contracts/loaders/fixtures, raw data, models, runtime, deployment, Azure, production, credentials, or the user's primary dirty worktree.
