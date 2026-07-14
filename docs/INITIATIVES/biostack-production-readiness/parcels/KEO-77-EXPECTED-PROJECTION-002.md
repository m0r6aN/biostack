# Parcel: KEO-77-EXPECTED-PROJECTION-002

## Objective

Expose a deterministic, metadata-only per-case projection of already validated KEO-77 expected declarations so KEO-78 can compare untrusted candidate declarations without parsing raw corpus content or redefining truth.

## Authorization

The coordinator stopped on the missing projection contract after PR #196. Clint Morgan explicitly authorized this narrow contract amendment on 2026-07-14.

## Starting Point

- Branch: `codex/keo-77-expected-projection`
- Immutable base: `f055d7f73e3c2d20a90eac39681c4fe04855dc95`
- Worktree: `D:\Repos\BioStack-keo77-expected-projection`

## Allowed Write Paths

- `backend/src/BioStack.KnowledgeWorker/Pipeline/AdversarialQueryCorpusLoader.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/AdversarialQueryCorpusLoaderTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-77-EXPECTED-PROJECTION-002.md`

All other paths are read-only.

## Contract

- Add an ordered `ExpectedCases` projection to the loaded KEO-77 record.
- Each entry exposes only the case ID and declarations already schema-validated and policy-validated by the loader: answer disposition, safety status, handling class, human-review flag, receipt event class and decision codes, and citation mode and synthetic source IDs.
- The projection contains no query, synthetic context, warnings, expected prose, raw candidate content, customer data, credentials, model configuration, or environment values.
- Corpus JSON, schema, versions, cases, expected values, and acceptance meaning remain unchanged.
- KEO-78 remains responsible for later comparison and reporting. This parcel adds no comparator, score, threshold, verdict, baseline, waiver, runtime registration, or effect authority.

## Security Review Boundary

Standard local passive review is required because the projection supplies safety/refusal/provenance expectations. Verify that only validated metadata crosses the boundary and that no raw or privacy-bearing text is exposed. This parcel cannot clear semantic accuracy, privacy-leakage detection, model transport, runtime, CI, deployment, or live-product gates.

## Acceptance Criteria

- Current corpus exposes exactly one ordered projection per validated case.
- Projection values match the already validated KEO-77 fixture.
- The declaration record has only the eight approved metadata properties.
- Existing fail-closed corpus tests remain green.
- Full KnowledgeWorker tests pass serially with zero warnings.
- Exactly the three allowed files change and scoped diff hygiene passes.

## Validation

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~AdversarialQueryCorpusLoaderTests`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1`
- `git diff --check origin/main...HEAD`

## Dependencies and Human Gates

- Depends on merged KEO-77 and PR #196 candidate-envelope contracts.
- The amendment is explicitly authorized; no additional human gate applies to this repository-only projection.
- Semantic definitions, thresholds, production comparison policy, deployment, and live qualification remain human-gated and out of scope.

## Prohibited Actions

- Do not change the corpus, schema, fixture values, versions, generated output, CI, runtime registration, or deployment files.
- Do not add or retain raw prompts, outputs, health/customer data, secrets, production values, or operational unsafe guidance.
- Do not invoke Claude, Ollama, another model, a network data source, a database, or a live environment for implementation or validation.
- Do not merge, deploy, restore, rotate credentials, or change Linear completion status.

## Retry and Escalation

Retry at most three materially similar validation failures. Stop and escalate if the projection requires raw content, changes KEO-77 meaning, conflicts with PR #196, or needs any path outside the allowed set.

## Validation Results

- Focused `AdversarialQueryCorpusLoaderTests`: 12 passed, 0 failed, 0 warnings.
- Full `BioStack.KnowledgeWorker.Tests`: 235 passed, 0 failed, 0 warnings.
- No retry cycle was required.

## Security Review

- Depth/mode: standard, local, passive source and diff review.
- Finding: no blocking finding in this bounded amendment.
- Verified controls: projection occurs only after the existing schema, source, matrix, fixture, safety/refusal consistency, synthetic-source, ordering, personal-data, and non-prescriptive-language checks succeed; the new records expose only the case ID plus eight enum/boolean/ID-list metadata properties; no raw query, context, expected prose, warnings, candidate content, customer data, secret, model, network, database, runtime, effect, CI, or deployment path was added.
- Residual risk: downstream KEO-78 code must continue treating candidate declarations as untrusted and must not convert structural equality into semantic or production truth.
- Coverage gaps: semantic factuality, privacy-leakage detection, refusal quality, threshold policy, model transport, runtime integration, live environment behavior, and production qualification remain uncleared.

## Required Handoff

Record base and ending commits, changed paths, focused/full test totals, security findings and coverage gaps, PR and hosted-check evidence, remaining gates, and the next safe KEO-78 action.
