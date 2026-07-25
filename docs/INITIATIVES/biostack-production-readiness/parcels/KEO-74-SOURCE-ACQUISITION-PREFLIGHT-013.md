# Parcel: KEO-74-SOURCE-ACQUISITION-PREFLIGHT-013

Status: implementation, verification, and independent review complete; publish and merge pending.

## Goal

Add a deterministic, in-memory preflight that proves a source-acquisition plan
is internally consistent and can be routed across the approved seven-source
shape without performing acquisition or persistence.

## Scope

Allowed files:

- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceAcquisitionExecutionPreflight.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/SourceAcquisitionExecutionPreflightTests.cs`
- this parcel record

## Contract

- Accept an existing `SourceAcquisitionPlan` plus explicit available-adapter
  descriptors.
- Enforce the authoritative approved mapping:
  - `fda` -> `fda-planning-v1` / `api`
  - `pubchem` -> `pubchem-planning-v1` / `api`
  - `pubmed` -> `pubmed-planning-v1` / `api`
  - `clinicaltrials` -> `clinicaltrials-planning-v1` / `api`
  - `dailymed` -> `dailymed-planning-v1` / `api`
  - `nih-ods` -> `nih-ods-planning-v1` / `api`
  - `nih-nccih` -> `nih-nccih-planning-v1` / `manual-review`
- Require exactly the selected seven source IDs and exactly one descriptor per
  source.
- Require every request to have one intent for every selected source.
- Reject duplicate request/source intent identities.
- Verify declared ready and blocked counts against the actual plan.
- Preserve each intent's exact source ID, planning adapter ID, candidate method,
  and registry-binding SHA-256 in immutable in-memory entries.
- Assign a deterministic one-based ordinal independent of input enumeration
  order.
- Classify entries as `blocked`, `manual-review-pending`, `ready-automated`, or
  `unsupported-or-mismatched` without invoking an adapter.
- Fail closed on an unknown source, missing or duplicate descriptor, descriptor
  planning-adapter or method mismatch, invalid or inconsistent registry hash,
  unsupported method, contradictory intent state, or contradictory plan counts.
- Report deterministic issues instead of throwing for null intents,
  descriptors, intent collections, blocking-reason collections, nested nulls,
  and blank identity or routing fields.
- Support an optional strict campaign expectation. The current recommended-seven
  expectation requires 70 unique requests, 490 intents, 490 ready, zero blocked,
  and seven sources before `CanActivate` can be true.
- Require zero blocked and zero unsupported/mismatched entries for
  `CanActivate`, even without a campaign expectation.
- Mark only `ready-automated` entries as dispatchable. Manual-review-pending is
  structurally valid and can coexist with `CanActivate`, but it is explicitly
  non-runnable and must never be sent to an automated adapter.

## Security and data boundary

This parcel performs no HTTP request and no filesystem, database, canonical,
promotion, scheduling, configuration, dependency-injection, or runtime-mode
change.

Durable candidate artifacts and receipts remain blocked. The governed source
decision contract lists `new-egress-or-storage-boundary` as a conditional
security/data trigger, while all seven current source entries declare no
detected trigger. The decision packet also leaves normalized-candidate snapshot
retention unresolved. Before a later runner persists candidates, checkpoints,
manual-review tasks, attempts, or receipts, Pradic Patel must review the newly
declared storage boundary and its retention, access, deletion, and failure
handling controls.

This preflight does not authorize acquisition, storage, evidence promotion,
canonical writes, or medical/prescriptive output.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionExecutionPreflightTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
rtk git status --short
```

## Deferred runtime inputs

The later acquisition runner still needs:

- Pradic Patel's security/data decision for the new storage boundary;
- approved retention, access, deletion, and corrupt/orphan artifact handling;
- a configured PubMed NCBI tool name and contact email;
- a durable output root and atomic-write/resume contract;
- merged implementations for all six API adapters;
- transient retry/checkpoint policy for rate limiting, back pressure, and
  unexpected failures; and
- explicit worker-mode and database-free runtime wiring.

No commit, push, pull request, merge, deployment, or live source request is
authorized by this parcel.

## Verification results

- Focused preflight suite: 44 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker suite: 435 passed, 0 failed, 0 skipped.
- No new compiler errors or warnings were introduced.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories remain
  unchanged.
