# Parcel: KEO-78-STRUCTURAL-SNAPSHOT-001

## Goal

Add a deterministic, offline structural evaluation snapshot for the currently merged KEO-73, KEO-76, and KEO-77 artifacts without claiming semantic evaluation or production readiness.

## Initiative

BioStack Production Readiness & Monetization

## Project Track

M3 - Data & Intelligence Coverage

## Wave

W1

## Branch

`codex/keo-78-structural-evaluation-snapshot`

## Worktree

`D:\Repos\BioStack-keo78-structural-snapshot-v2`

## Starting Commit

`212aef914c7716edb46da43e62bc1514a2d8e488`

## Dependencies

- KEO-73 merged through PR #189.
- KEO-76 merged through PR #191.
- KEO-77 merged through PR #192.
- KEO-75 remains incomplete and blocks full KEO-78 acceptance, but does not block this structural-only slice.

## Integration Surfaces

- Offline evaluation taxonomy and matrix
- Offline protocol-design fixture corpus
- Offline adversarial-query corpus

## Security Gate

Security review required before release; this parcel handles only synthetic, offline aggregate metadata.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/StructuralEvaluationSnapshotBuilder.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationSnapshotBuilderTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-78-STRUCTURAL-SNAPSHOT-001.md`

## Forbidden

- Do not edit corpus, taxonomy, matrix, schema, migration, workflow, runtime, DI, endpoint, or deployment files.
- Do not invoke models or networks from product code or tests.
- Do not add factuality, retrieval, citation-quality, safety-quality, refusal-quality, latency, cost, or production metrics not supported by structured inputs.
- Do not invent thresholds, baselines, release gates, or semantic truth.
- Do not acquire sources or change KEO-75.
- Do not merge, deploy, or mutate production.

## Out of Scope

Full KEO-78 acceptance, semantic response evaluation, live model execution, CI regression thresholds, production qualification, and KEO-75 corpus coverage.

## Existing Patterns To Follow

- `backend/src/BioStack.KnowledgeWorker/Pipeline/EvaluationCoverageArtifactLoader.cs` - validated offline taxonomy/matrix metadata.
- `backend/src/BioStack.KnowledgeWorker/Pipeline/ProtocolDesignFixtureCorpusLoader.cs` - validated synthetic fixture metadata.
- `backend/src/BioStack.KnowledgeWorker/Pipeline/AdversarialQueryCorpusLoader.cs` - validated synthetic adversarial metadata.

## Contract

The builder returns a versioned, stable snapshot containing artifact versions, aggregate counts, exact missing/unexpected coverage-case IDs, pending owner roles, and explicit limitations. It must fail closed on artifact-version disagreement and perform no model or network work.

## Required Tests

- Current merged artifacts produce stable expected counts and matching case sets.
- Serialization is deterministic and states the structural-only limitations.
- Exact set differences surface missing and unexpected coverage-case IDs.
- Version disagreement fails closed.

## Acceptance Criteria

- Focused tests pass serially.
- Existing KEO-73/76/77 focused loader tests remain green.
- The implementation touches only Allowed Files.
- The result is described as partial structural evidence, never full KEO-78 completion.

## Verification

- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~StructuralEvaluationSnapshotBuilderTests`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter "FullyQualifiedName~EvaluationCoverageArtifactLoaderTests|FullyQualifiedName~ProtocolDesignFixtureCorpusLoaderTests|FullyQualifiedName~AdversarialQueryCorpusLoaderTests"`
- `git diff --check origin/main...HEAD`

## Evidence Required

- Focused test results
- Commit SHA
- Draft PR and completed hosted checks
- Linear comment retaining the KEO-75/full-harness blocker

## Collision Risk

Low. All files are new and no shared integration point is changed.

## Dependencies and Human Gates

Full KEO-78 remains blocked by KEO-75, approved semantic metrics/thresholds, and later live model/environment validation.

## Retry Limit and Escalation

Stop after three materially similar failures. Escalate immediately if a shared file, contract amendment, semantic threshold, source approval, secret, or live environment is required.

## Session Handoff

- Starting commit: `212aef914c7716edb46da43e62bc1514a2d8e488`
- Ending commit: recorded in the draft PR and Linear evidence comment after publication
- Files changed: the three Allowed Files only
- Commands run:
  - `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter FullyQualifiedName~StructuralEvaluationSnapshotBuilderTests`
  - `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1 --filter "FullyQualifiedName~EvaluationCoverageArtifactLoaderTests|FullyQualifiedName~ProtocolDesignFixtureCorpusLoaderTests|FullyQualifiedName~AdversarialQueryCorpusLoaderTests"`
  - `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj -m:1`
- Tests passed: 4 new focused tests, 28 dependency-loader tests, and 220 full KnowledgeWorker tests
- Tests failed: none
- Decisions needed: none for structural-only slice
- Blockers: KEO-75 and full-harness metric/threshold approvals remain
- Next safe action: publish a draft PR, wait for hosted checks, and record the partial evidence in Linear
- Do not touch: primary checkout dirty files, corpora, schemas, workflows, runtime wiring, cloud resources

## Stop-and-Report Rule

Stop if implementation requires any file outside Allowed Files, a contract amendment, a semantic metric or threshold, model/network execution, source approval, a secret, or a live mutation.
