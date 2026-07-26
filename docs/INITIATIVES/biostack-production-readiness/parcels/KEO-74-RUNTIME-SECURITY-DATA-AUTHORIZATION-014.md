# Parcel: KEO-74-RUNTIME-SECURITY-DATA-AUTHORIZATION-014

Status: implementation, verification, and independent review complete; publish and merge pending.

## Goal

Record Pradic Patel's approval of the narrow runtime security/data boundary for
the seven authorized official-source lanes without implementing or activating a
runtime acquisition worker.

## Human decision

Clint Morgan confirmed that Pradic Patel approved all previously requested
runtime security/data controls.

The governed receipt timestamp is `2026-07-26T10:40:42Z`. The original decision
time was not independently supplied, so this timestamp records receipt in the
canonical packet and is not represented as the original decision time.

## Trigger disposition

- All seven sources detect `new-egress-or-storage-boundary`.
- The six approved API lanes also detect
  `untrusted-bulk-archive-or-parser`.
- `nih-nccih` remains manual review and does not receive the API/parser trigger.

Each source records `securityData.reviewStatus` as `reviewed` and
`securityData.decision` as `approved-with-controls`.

## Approved controls

- Boundary: existing `ResearchOutput/source-acquisition/v1` only.
- Stored material: normalized review-required candidates and minimal receipts
  only.
- Prohibited: raw response bodies, copyrighted full text, private or personal
  data, secrets, database writes, canonical writes, promotion, and
  runtime-visible claim authority.
- API transport: fixed first-party endpoints, disabled redirects, bounded
  response bodies, serialized request budgets, and no automatic retry.
- Access: worker service identity may write; evidence reviewer may read.
- Retention: an explicit positive runtime configuration value is required and
  no default is permitted.
- Lifecycle: atomic writes and deletion, content-free deletion tombstones, and
  quarantine for corrupt or orphan artifacts.
- PubMed: non-secret tool identity `BioStackKnowledgeWorker`; contact email must
  come from external runtime configuration and is not committed.
- NCCIH: operator Clint Morgan and independent reviewer Ellison Nemoy.

## Structural evidence

The canonical artifact validator now asserts the exact seven-source trigger,
approval, timestamp-basis, boundary, transport, access, retention, lifecycle,
PubMed identity, and NCCIH reviewer structure.

The existing source-authorization schema already models the approved trigger
values and requires reviewed security/data approval for an activation-ready
source with a detected trigger. No schema widening is required.

The source registry itself did not change, so the existing corpus-inventory and
structural-report source counts and versions remain unchanged.

## Allowed files

- `research/source-authorization/recommended-seven-source-decisions.v1.json`
- `backend/tests/BioStack.KnowledgeWorker.Tests/ResearchArtifactValidatorTests.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/SourceAcquisitionPlanningTests.cs`
- this parcel record

## Forbidden

- No runtime, worker-mode, dependency-injection, scheduling, configuration,
  adapter, HTTP, database, canonical-ingest, promotion, deployment, frontend, or
  secret change.
- No contact email is committed.
- No live source request is made.
- No commit, push, pull request, merge, or deployment is authorized by this
  parcel.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~ResearchArtifactValidatorTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionPlanningTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
rtk git status --short
```

## Verification results

- Source-authorization schema and artifact suite: 34 passed, 0 failed, 0
  skipped.
- Source-acquisition planning suite: 58 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker suite: 391 passed, 0 failed, 0 skipped.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories and the
  existing FDA test nullable warning remain unchanged.
