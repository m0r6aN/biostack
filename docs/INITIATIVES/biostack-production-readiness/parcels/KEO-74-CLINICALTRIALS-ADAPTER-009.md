# Parcel: KEO-74-CLINICALTRIALS-ADAPTER-009

Status: implementation, verification, and independent review complete; publish and merge pending.

## Goal

Add one bounded ClinicalTrials.gov API v2 adapter that retrieves public study-registration metadata for an approved acquisition intent and emits provenance-complete, review-required candidates without promoting registry content into efficacy, safety, or interaction conclusions.

## Branch and worktree

- Branch: `codex/keo74-clinicaltrials-adapter-20260725`
- Worktree: `D:\Repos\BioStack-keo74-clinicaltrials-adapter-009-20260725`
- Base: `554ea2b`

## Scope

Allowed files:

- `backend/src/BioStack.KnowledgeWorker/Pipeline/ClinicalTrialsGovV2AcquisitionAdapter.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/ClinicalTrialsGovV2AcquisitionAdapterTests.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/Fixtures/clinicaltrials-v2-studies.synthetic.json`
- this parcel record

No runtime registration, scheduler, persistence, database, canonical-ingest, registry, authorization-decision, credential, deployment, or frontend change is included.

## Source contract

- Source: `clinicaltrials`
- Planning adapter: `clinicaltrials-planning-v1`
- Transformation: `clinicaltrials-gov-v2-study-metadata-v1`
- Fixed endpoint: `GET https://clinicaltrials.gov/api/v2/studies`
- Official API documentation:
  - `https://clinicaltrials.gov/data-api/api`
  - `https://clinicaltrials.gov/data-api/about-api/search-areas`
  - `https://clinicaltrials.gov/data-api/about-api/study-data-structure`
  - `https://clinicaltrials.gov/about-site/terms-conditions`

The request uses only `query.intr` with explicit `AREA[InterventionName]` clauses. Validated terms are quoted and joined by a parenthesized `OR` expression. The adapter sends `format=json`, an explicit retrieval field list, `pageSize=50`, and `countTotal=true`. It never supplies a page token, follows pagination, or retries.

## Candidate boundary

Allowed identity metadata:

- NCT identifier;
- brief and official titles;
- registering organization;
- study type and conditions;
- lead sponsor name and class; and
- intervention names, types, and other names.

Allowed efficacy-context metadata:

- registered study phase; and
- registered primary and secondary outcome measure names and time frames.

Outcome descriptions, posted result measurements, analyses, statistics, and conclusions are not emitted.

When `interactions` is authorized, the adapter may emit only interventions co-listed with an exact matched intervention. The field is explicitly labeled as registered-study design context. It is not an interaction, safety, compatibility, or effectiveness claim. If an exact matched intervention cannot be established, the adapter emits no co-listed context.

Every emitted candidate requires at least one returned intervention `name` or
`otherNames` value to match a requested search term exactly, using
case-insensitive comparison. An unmapped returned study is not accepted as a
candidate.

## Provenance and rights

Every candidate requires:

- `nctId`;
- `overallStatus`;
- `phase`;
- `lastUpdateSubmitDate`;
- canonical study and exact query URLs;
- exact registry binding;
- UTC retrieval timestamp;
- reviewed rights status;
- transformation version; and
- `review-required`.

The adapter accepts typed `not-applicable` phase provenance only for the official ClinicalTrials.gov `NA` enum. A missing, blank, mixed `NA`, or unknown phase fails closed. API v2 identity is recorded on every candidate. First/last posted dates and lead-sponsor provenance are added when returned.

`lastUpdateSubmitDate`, `studyFirstPostDate`, and `lastUpdatePostDate` must be
exactly `yyyy-MM-dd`. `overallStatus` must be one of the current official
values: `ACTIVE_NOT_RECRUITING`, `COMPLETED`, `ENROLLING_BY_INVITATION`,
`NOT_YET_RECRUITING`, `RECRUITING`, `SUSPENDED`, `TERMINATED`, `WITHDRAWN`,
`AVAILABLE`, `NO_LONGER_AVAILABLE`, `TEMPORARILY_NOT_AVAILABLE`,
`APPROVED_FOR_MARKETING`, `WITHHELD`, or `UNKNOWN`.
Because the shared provenance guard correctly rejects an unqualified
placeholder value of `unknown`, the official `UNKNOWN` enum is retained with
explicit ClinicalTrials.gov OverallStatus-enum context.

Candidates retain ClinicalTrials.gov and submitter attribution, the reviewed terms URL, covered-field lineage, a non-endorsement requirement, and explicit exclusions for results analyses, outcome conclusions, linked documents, and personal contact details.

## Transport boundary

- Anonymous, redirect-disabled HTTPS client.
- Exact first-party host and path.
- One globally serialized request at a time.
- No invented numeric quota.
- Twenty-second timeout.
- Two MiB response maximum.
- JSON only.
- Maximum 50 studies.
- `429` returns `RateLimited`.
- `503` returns `BackPressure`.
- `Retry-After` is preserved without retry.
- `202`, `400`, `404`, redirects, other failures, malformed/non-JSON bodies, excess studies, and missing hard provenance fail closed.
- Because `countTotal=true` is fixed, `totalCount` is required and cannot be
  smaller than the returned study count.
- An empty `studies` array returns `NoMatch` only when `totalCount` is zero and
  no next-page token is present.
- A next-page token must be present exactly when `totalCount` exceeds the
  returned first-page count. That coherent condition marks the batch truncated;
  no next request is issued.
- Because the request is the first page with `pageSize=50`, the returned study
  count must equal `min(totalCount, 50)`; an underfilled first page fails closed.
- Duplicate NCT identifiers fail closed, whether the duplicated records are
  identical or conflicting.

## Synthetic verification

The fixture is visibly synthetic. It contains no copied source record or live-source payload. Tests cover:

- exact endpoint, query area, quoting, explicit fields, and no page token;
- invalid intent, stale registry, unsupported field use, query injection, control characters, and term count rejection before HTTP;
- identity, registered-outcome, sponsor, rights, reuse, document, and hard provenance output;
- exclusion of descriptions, measurements, conclusions, and interaction claims;
- exact authorized-use intersection;
- typed official `NA` phase handling and missing/invalid phase rejection;
- exact intervention-name or other-name matching before candidate output;
- duplicate identical and conflicting NCT identifier rejection;
- required total-count and coherent first-page token/truncation validation;
- exact `yyyy-MM-dd` submitted/posted dates and complete official
  overall-status validation;
- `NoMatch`, truncation, `429`, `503`, `Retry-After`, and no retry;
- `202`, `400`, `404`, redirects, non-JSON, malformed JSON, excess studies, and response-size rejection;
- missing NCT identifier, overall status, and last-update date rejection; and
- serialized gate acquisition before transport.

## Verification commands

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~ClinicalTrialsGovV2AcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionPrimitivesTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~FdaOpenFdaDrugLabelAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
rtk git status --short
```

## Stop gates

- No evidence promotion is authorized.
- Registered design fields must never be described as measured efficacy, safety, or interaction evidence.
- Any endpoint expansion, pagination, retry, persistence, runtime wiring, credential, private data, source-text expansion, or new egress boundary requires a separate reviewed parcel.
- Publishing and merging remain human-controlled.

## Handoff

- ClinicalTrials.gov adapter tests: 69 passed, 0 failed, 0 skipped.
- Shared acquisition-primitives tests: 55 passed, 0 failed, 0 skipped.
- Existing FDA adapter tests: 21 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker tests: 460 passed, 0 failed, 0 skipped.
- Fixture JSON validation: passed.
- Diff whitespace validation: passed.
- Warnings: pre-existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories only.
- No live ClinicalTrials.gov request or source payload was used.
- No commit, publish, or merge was performed.
