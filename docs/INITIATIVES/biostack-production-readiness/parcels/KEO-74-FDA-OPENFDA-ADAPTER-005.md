# Parcel: KEO-74-FDA-OPENFDA-ADAPTER-005

Status: implementation, verification, and independent review complete; publish pending.

## Goal

Implement the first transport-capable official-source adapter behind the approved KEO-74 acquisition plan without adding scheduling, persistence, canonical promotion, or publication.

## Source and endpoint

- Source registry ID: `fda`
- Dataset: openFDA drug product labeling
- Fixed endpoint: `https://api.fda.gov/drug/label.json`
- Transformation: `fda-openfda-drug-label-v1`
- Planning input: blocker-free `fda-planning-v1` intents only

The query uses exact quoted terms across the documented harmonized generic-name, brand-name, and substance-name fields. One bounded request is emitted per approved intent with a maximum of 100 returned records and no automatic paging or retry.

## Authorization and data boundary

The adapter requires:

- a `Ready` FDA/API intent with no blocking reasons;
- the exact approved source-registry SHA-256;
- the expected FDA planning-adapter ID; and
- every required FDA provenance field.

It emits in-memory, `review-required` candidates only. The allowlist includes:

- source identity: `id`, `set_id`, `version`, and `effective_time`;
- harmonized names, manufacturer, route, dosage form, application number, and product NDC;
- indications and usage, contraindications, warnings, boxed warnings, warnings and cautions, adverse reactions, and drug interactions.

Each candidate preserves the exact registry binding SHA-256 alongside its source
item ID, item-specific API URL, originating query URL, source update date,
retrieval time, rights status, and transformation version. Output field groups
are intersected with the intent's authorized field uses.

The adapter does not emit arbitrary response fields, GMDN content, or `dosage_and_administration`. FDA label content remains product-, version-, formulation-, route-, and jurisdiction-specific and may not be treated as individualized medical guidance.

## Transport controls

- HTTPS and the first-party `api.fda.gov` host are fixed in code.
- The provided anonymous client disables redirects and has a 20-second timeout.
- A shared gate serializes calls and enforces at most 120 requests per minute
  and 900 requests per UTC day before any HTTP request is sent.
- Response headers are inspected before parsing.
- Redirects, non-JSON responses, malformed JSON, non-success statuses, missing source item IDs, stale plan bindings, and invalid search terms fail closed.
- The response body is capped at 2 MiB by both declared and streamed size.
- HTTP 404 becomes a no-match result.
- HTTP 429 and `Retry-After` are surfaced without automatic retry.
- No authentication secret is accepted or logged.

## Boundaries

- No live source retrieval is run by the test suite.
- No raw response is persisted.
- No job, scheduler, database, canonical-ingest, promotion, publication, frontend, or deployment path is wired.
- No automatic pagination, concurrency, or retry is implemented.
- Evidence-review responsibility is assigned to Clint Morgan
  (`461a4112-8e91-41cb-afef-6889b8f48ff0`); review remains required before any
  candidate can become canonical.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --disable-build-servers --filter FdaOpenFdaDrugLabelAcquisitionAdapterTests
rtk proxy dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --disable-build-servers
rtk git diff --check
```

Expected result: 336 tests passed, 0 failed, 0 skipped.
