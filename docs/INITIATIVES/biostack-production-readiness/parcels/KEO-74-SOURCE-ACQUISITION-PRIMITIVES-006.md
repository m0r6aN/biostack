# Parcel: KEO-74-SOURCE-ACQUISITION-PRIMITIVES-006

Status: implementation, verification, and independent review complete; publish pending.

## Goal

Add the narrow, source-neutral candidate, provenance, intent-guard, HTTP-safety, and request-budget primitives needed by the remaining approved official-source adapters without changing the merged FDA adapter.

## Branch and worktree

- Branch: `codex/keo74-source-acquisition-primitives-20260725`
- Worktree: `D:\Repos\BioStack-keo74-pubchem-adapter-20260725`
- Base: `main@7cfbef0`

## Scope

Allowed files:

- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceAcquisitionAdapter.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceAcquisitionGuards.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceAcquisitionTransport.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/SourceAcquisitionPrimitivesTests.cs`
- this parcel record

The existing FDA adapter and tests are regression inputs only and must remain unchanged.

## Contract

- Preserve the existing positional `SourceAcquisitionCandidate` constructor while making `QueryUrl` nullable for governed manual capture.
- Add empty-by-default authorized-field, source-specific provenance, rights-attribution, document-provenance, and reuse-boundary metadata.
- Represent source-specific provenance as an explicit `present`, `not-provided`, or `not-applicable` state. Optional absence states require source/field allowlists; blank, `N/A`, and `unknown` values never satisfy provenance.
- Preserve an optional manual-capture audit with independent operator/reviewer identity, ordered UTC timestamps, an approved decision, notes, and explicit safety/rights attestations.
- Validate core candidate invariants unconditionally: canonical source ID, exact lowercase registry SHA-256, nondefault UTC retrieval, reviewed rights, review-required promotion state, absolute HTTPS source URL, and non-null collections.
- Validate every declared common or source-specific required provenance key. Hard-required source identifiers and update/version fields remain source-adapter responsibilities and cannot use an optional-absence allowance.
- Validate blocker-free, registry-bound acquisition intents and their complete declared provenance set before any transport.
- Provide a redirect-disabled anonymous HTTP client factory and bounded response-body reader.
- Provide a serialized request gate with optional multiple sliding-window budgets and an optional UTC-day budget. A serialization-only mode is valid for sources whose approved policy does not declare a numeric quota.
- Preserve existing `Completed`, `NoMatch`, and `RateLimited` enum values and add `BackPressure` for service-busy semantics that are distinct from an explicit rate-limit response.

## Manual-capture boundary

A candidate cannot pass the approved-manual-capture audit guard unless:

- operator and reviewer are nonblank and distinct;
- capture and review timestamps are nondefault UTC values, review follows capture, and candidate retrieval is not earlier than review;
- decision is `approved` and notes are nonempty;
- the reviewer attests that only source-authored text was captured, restricted third-party material was excluded, acknowledgement was retained, no endorsement was implied, and the capture makes no individualized advice, dosing direction, regulatory claim, or safety-critical conclusion.

`QueryUrl` may be null for the approved NCCIH manual-review lane. This parcel does not implement that workflow.

## Forbidden

- No source adapter implementation or refactor.
- No runtime registration, scheduling, persistence, database, or canonical-ingest behavior.
- No registry, source-decision, schema, credential, deployment, or frontend change.
- No external source request or source payload fixture.
- No evidence promotion or medical/prescriptive output.

## Adapter-owned follow-ups

The shared parcel deliberately defers these source-specific assertions to each adapter suite:

- completeness and exact field coverage of rights attribution, document provenance, and reuse acknowledgement;
- service-specific `Retry-After` handling and any back-pressure latch beyond the local serialized request budget;
- DailyMed section-to-document-provenance alignment; and
- hard-required source identifiers, versions, and update dates beyond the shared candidate invariant and typed availability contract.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionPrimitivesTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~FdaOpenFdaDrugLabelAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
rtk git status --short
```

## Acceptance

- Shared primitive tests pass.
- The existing FDA focused suite and full KnowledgeWorker suite remain green.
- The FDA implementation has no diff.
- The changed-file set remains inside the parcel allowlist.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories are reported as pre-existing and are not changed by this parcel.

## Handoff

- Starting commit: `7cfbef0`
- Ending commit: uncommitted changes on `7cfbef0`
- Shared primitive tests: 55 passed, 0 failed, 0 skipped.
- Existing FDA adapter tests: 21 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker tests: 391 passed, 0 failed, 0 skipped.
- Warnings: pre-existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories only.
- Publishing and merging remain human-controlled.
- Independent review's shared-primitive test conditions were closed by the 55-test focused matrix; adapter-owned assertions remain deferred to their adapter suites as listed above.
- Next safe action: commit and publish as a separate prerequisite PR before source-adapter parcels are based on it.
