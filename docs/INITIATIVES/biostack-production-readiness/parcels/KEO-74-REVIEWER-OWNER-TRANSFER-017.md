# Parcel: KEO-74-REVIEWER-OWNER-TRANSFER-017

Status: reviewer ownership transferred; NCCIH distinct-reviewer blocker remains
fail closed.

## Decision receipt

The authorized task states that Ellison Nemoy is leaving and all of his
BioStack responsibilities transfer to Clint Morgan. Clint Morgan is bound to
Microsoft Entra object id `461a4112-8e91-41cb-afef-6889b8f48ff0`.

The governed receipt timestamp is `2026-07-26T12:22:29Z`. The original transfer
decision time was not supplied, so this is explicitly receipt time and is not
represented as the original decision time.

The binding receipt is:

`research/source-authorization/keo-74-reviewer-owner-transfer-receipt.v1.json`

It binds the exact source-authorization decision artifact and schema by
SHA-256.

## Transfer disposition

- The `evidence-reviewer` owner assignment transfers to Clint Morgan.
- The seven source-specific evidence-promotion assignments transfer to Clint
  Morgan.
- Assignment is not approval. Every evidence-promotion decision remains
  `review-required` with a null decision and timestamp.
- Clint Morgan's product-owner assignment remains separate and approved only
  at its existing product-capability stage.
- Role assignments remain unique even though Clint now holds two roles. The
  artifact no longer asserts that all four roles must be held by four distinct
  people.

## NCCIH separation-of-duties blocker

The NCCIH workflow requires different operator and reviewer identifiers before
a manual-capture candidate can be ready. Clint Morgan is the assigned NCCIH
operator and now owns the transferred reviewer responsibility, so he cannot
independently review a capture he performs.

No runtime guard is changed. NCCIH operator-created candidates remain blocked
until a distinct authorized person is assigned and performs the reviewer
action. The transfer does not name Clint Morgan as an independent reviewer.

## Scope

This parcel changes only reviewer/owner metadata, its schema and invariant
tests, the binding receipt, and affected KEO-74 governance documentation.

It does not authorize or perform Blob, runtime, Azure, live source, database,
canonical-ingest, promotion, deployment, commit, push, or pull-request work.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~ResearchArtifactValidatorTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~NccihManualReviewCandidateWorkflowTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
```

## Verification results

- Source-authorization artifact and transfer-receipt suite: 35 passed, 0
  failed, 0 skipped.
- NCCIH manual-review workflow suite: 71 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker suite: 850 passed, 0 failed, 0 skipped.
- `git diff --check`: passed.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories and
  existing nullable-analysis warnings remain unchanged.
