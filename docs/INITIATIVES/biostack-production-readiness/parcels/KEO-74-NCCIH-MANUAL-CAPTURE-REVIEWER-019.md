# Parcel: KEO-74-NCCIH-MANUAL-CAPTURE-REVIEWER-019

Status: distinct NCCIH manual-capture reviewer assigned; reviewer action and
evidence approval remain pending and fail closed.

## Decision receipt

The authorized task assigns Sandy Morgan as the distinct NCCIH manual-capture
reviewer. Clint Morgan retains overall evidence-review ownership and remains
the assigned NCCIH operator.

The governed receipt timestamp is `2026-07-26T16:27:48Z`. The original
assignment decision time was not supplied, so this is explicitly receipt time
and is not represented as the original decision time.

The binding receipt is:

`research/source-authorization/keo-74-nccih-manual-capture-reviewer-receipt.v1.json`

It is an immutable overlay that binds the exact source-authorization decision
artifact, its schema, and the preceding reviewer-owner transfer receipt by
SHA-256. The previously bound artifacts remain unchanged.

## Assignment disposition

- Sandy Morgan is assigned only as the distinct NCCIH manual-capture reviewer.
- Sandy Morgan's Microsoft Entra object id was not supplied and is not
  invented. The bound schema permits name-only human assignment because the
  human name is required and the Entra object id is optional.
- Clint Morgan remains the `evidence-reviewer` owner and the source-specific
  evidence-promotion assignee. This overlay does not transfer that ownership.
- Clint Morgan remains the NCCIH manual-capture operator.
- Assignment is not a completed reviewer action or evidence approval.
  Evidence promotion remains `review-required` with null decision and decision
  timestamp.

## Fail-closed boundary

No runtime guard changes. The NCCIH workflow still requires distinct,
substantive operator and reviewer identifiers. An operator-created candidate
remains blocked until Sandy Morgan performs the reviewer action with a
reviewer identifier distinct from the operator identifier and every existing
manual-capture validation succeeds.

This parcel does not activate a source or authorize or perform live source,
runtime, Azure, Blob, database, canonical-ingest, promotion, deployment,
commit, push, or pull-request work.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~ResearchArtifactValidatorTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~NccihManualReviewCandidateWorkflowTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
```

## Verification results

- Source-authorization artifact, schema representation, and reviewer-overlay
  receipt suite: 36 passed, 0 failed, 0 skipped.
- NCCIH manual-review workflow suite: 71 passed, 0 failed, 0 skipped.
- Receipt SHA-256 binding verification: passed.
- The full KnowledgeWorker command exceeded its 180-second command window, so
  its result is inconclusive and no full-suite pass is claimed.
- Independent governance review remains pending for the coordinator.
- `git diff --check`: passed.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories and
  existing nullable-analysis warnings remain unchanged.
