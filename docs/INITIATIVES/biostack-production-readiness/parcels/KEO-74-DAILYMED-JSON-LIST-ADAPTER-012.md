# Parcel: KEO-74-DAILYMED-JSON-LIST-ADAPTER-012

Status: implementation, verification, and independent re-review complete; publish and merge pending.

## Goal

Implement a thin, JSON-only DailyMed list adapter that acquires bounded SPL
identity records from an exact approved intent. The parcel does not retrieve
label sections, wire runtime execution, persist evidence, or promote candidates.

## Branch and worktree

- Branch: `codex/keo74-dailymed-json-adapter-20260725`
- Worktree: `D:\Repos\BioStack-keo74-dailymed-json-adapter-012-20260725`
- Base: shared source-acquisition primitives commit `554ea2b`

## Source and request contract

- Source registry ID: `dailymed`
- Planning adapter: `dailymed-planning-v1`
- Transformation: `dailymed-spl-list-json-identity-v1`
- Fixed endpoint:
  `https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json`
- Fixed request parameters, in order:
  `drug_name=<intent.CompoundName>`, `name_type=both`, `pagesize=50`, and
  `page=1`
- One anonymous, redirect-disabled HTTPS request is serialized with a
  20-second timeout.
- No retry, automatic paging, concurrency, credentials, or alternate endpoint
  is implemented.

The adapter accepts only a blocker-free, registry-bound, exact DailyMed/API
intent whose authorized uses include `identity` and whose provenance declaration
contains the complete DailyMed requirement set. Compound names are bounded,
canonical, and restricted to safe term characters before the request gate or
HTTP client can run.

## Response and correlation contract

Only the `/spls.json` list shape is accepted. The adapter requires:

- JSON content below 1 MiB;
- page 1 and 50 elements per page;
- canonical nonnegative JSON integers for all page counts and a positive JSON
  integer for each SPL version;
- `data.Count == min(total_elements, 50)`;
- coherent `total_pages` and next/previous page metadata;
- a populated `next_page` represented as a JSON integer, while the API's
  unpopulated page sentinels remain the literal string `null`;
- `current_url` exactly equal to the request URL;
- no more than 50 returned list records;
- unique canonical lowercase SPL set IDs;
- canonical positive SPL version strings;
- bounded, control-free titles that contain the normalized compound term; and
- invariant DailyMed `MMM dd, yyyy` published dates, normalized to
  `yyyy-MM-dd`.

HTTP 429 maps to `RateLimited`, HTTP 503 maps to `BackPressure`, and both
preserve `Retry-After` without retrying. Redirects, HTTP 202, all other
non-success statuses, non-JSON content, malformed shapes, incoherent metadata,
oversized responses, duplicates, and failed source/query correlation fail
closed. A coherent zero-result page maps to `NoMatch`.

## Candidate, provenance, and rights boundary

Every in-memory candidate is `review-required`, authorizes only `identity`, and
emits exactly:

- `label_title`
- `spl_set_id`
- `label_version`
- `published_date`

The item URL is canonical
`https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=<splSetId>`.
Source-specific provenance preserves `splSetId` and `labelVersion` as present.
Because the JSON list does not contain them, `ndc` and `effectiveDate` are
explicitly `not-provided`; `sectionName` and `sectionCode` are explicitly
`not-applicable`.

Rights scope is limited to DailyMed SPL list-record identity metadata and
retains the NLM acknowledgement and terms link. Reuse explicitly excludes SPL
section text, SPL XML, media and linked documents, third-party material, and
product-specific claims. List-record identity does not establish product
equivalence, indications, efficacy, safety, dosing, contraindications,
interactions, or suitability.

## Boundaries

- No SPL XML, section text, bulk archives, media, NDC detail, or linked
  documents.
- No live source call or captured live fixture.
- No runtime registration, trigger, scheduler, persistence, database,
  canonical-ingest, promotion, publication, frontend, or deployment change.
- No individualized medical guidance, dosing direction, or product-specific
  conclusion.
- Evidence review by Ellison Nemoy remains required before canonical promotion.

## Independent-review closure

The initial implementation modeled numeric values as JSON strings, matching
the published documentation example but not the current live `/spls.json`
wire shape. The parser and visibly synthetic fixture now model
`spl_version`, `elements_per_page`, `total_pages`, `total_elements`,
`current_page`, and populated `next_page` as JSON integers. String, float,
exponent, signed, leading-zero, overflow, and mixed representations fail
closed. The configurable response ceiling is also constrained to at most the
fixed 1 MiB limit.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~DailyMedSplListJsonAcquisitionAdapterTests
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~SourceAcquisitionPrimitivesTests
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --filter FullyQualifiedName~FdaOpenFdaDrugLabelAcquisitionAdapterTests
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore
rtk git diff --check
rtk git status --short
```

## Handoff

- Starting commit: `554ea2b`
- Ending commit: uncommitted changes on `554ea2b`
- DailyMed focused tests: 86 passed, 0 failed, 0 skipped.
- Shared source-acquisition primitive tests: passed.
- Existing FDA adapter tests: passed.
- Full KnowledgeWorker tests: 477 passed, 0 failed, 0 skipped.
- `git diff --check`: passed.
- Warnings: pre-existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903`
  advisories and the pre-existing FDA test nullable warning only.
- Publishing and merging remain human-controlled.
