# Parcel: KEO-74-PUBCHEM-PUG-REST-IDENTITY-ADAPTER-007

Status: implementation, verification, and independent review complete; publish pending.

## Goal

Implement a narrow PubChem identity adapter behind the approved KEO-74 acquisition plan without adding scheduling, persistence, canonical promotion, publication, or mechanism claims.

## Source and endpoint

- Source registry ID: `pubchem`
- Planning input: blocker-free `pubchem-planning-v1` API intents only
- Transformation: `pubchem-pug-rest-compound-identity-v1`
- Exact-name property endpoint:
  `https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/{term}/property/MolecularFormula,MolecularWeight,SMILES,InChI,InChIKey,ExactMass/JSON`
- Modify Date endpoint:
  `https://pubchem.ncbi.nlm.nih.gov/rest/pug_view/data/compound/{cid}/JSON?heading=Modify%20Date`

At most five validated, distinct terms are queried sequentially. Each successful exact-name response must contain exactly one property record, and every successful alias must resolve to the same CID. Ambiguous multi-record or cross-alias resolution fails closed. A successful intent emits at most one candidate.

## Authorization and data boundary

The adapter requires the exact approved registry SHA-256, all PubChem-required provenance fields, and an authorized `identity` field use before HTTP. A mechanism-only intent is rejected.

The v1 allowlist is limited to:

- molecular formula;
- molecular weight;
- SMILES;
- InChI;
- InChIKey; and
- exact mass.

The adapter excludes contributor annotations, descriptions, synonyms, bioassays, mechanism claims, and arbitrary response fields. It stores no raw response. Output remains in-memory and `review-required`.

`recordUpdateDate` is accepted only from one fixed-heading PUG View section when:

- `RecordType` is `CID`;
- `RecordNumber` equals the requested CID;
- exactly one ISO date is present;
- exactly one referenced source has `SourceName` and `SourceID` equal to `PubChem`; and
- the reference URL has the official `https://pubchem.ncbi.nlm.nih.gov` origin.

The candidate guard then validates every registry-required field, including `pubchemCid` and `recordUpdateDate`.

## Transport controls

- The HTTPS scheme, first-party host, endpoint families, property list, and heading are fixed in code.
- Redirects are disabled and the anonymous client has a 20-second timeout.
- Bodies are capped at 1 MiB by declared and streamed size and must be JSON.
- No paging, automatic retry, credentials, or arbitrary request URL is supported.
- A shared local gate serializes requests and spaces request starts by at least 200 milliseconds. This yields at most 5 requests per second and 300 per minute, staying inside both approved ceilings.
- HTTP 429 or 503 returns `RateLimited` and preserves `Retry-After` without parsing a possible HTML body.
- PubChem `X-Throttling-Control` Request Count or Request Time Red/Black returns `RateLimited`; Service Red/Black returns `BackPressure`.
- A throttle or back-pressure signal on any later request discards the entire in-progress batch.
- Local pacing is cancellation-aware and shared by adapter instances. It waits only to satisfy the approved request-start interval; the adapter never retries a source response.
- Per-term 404 is a no-match. If every term is a no-match, the batch is `NoMatch`.
- HTTP 202, redirects, other non-success responses, malformed JSON, missing or mismatched identifiers, changed Modify Date attribution, and invalid search terms fail closed.

## Boundaries

- Tests use synthetic fixtures and a recording message handler; no live request is made.
- No runtime registration, scheduler, persistence, database, canonical-ingest, promotion, publication, frontend, or deployment path is wired.
- PubChem identity metadata does not establish clinical efficacy, safety, dosing, contraindications, or suitability.
- Evidence-review responsibility is assigned to Clint Morgan
  (`461a4112-8e91-41cb-afef-6889b8f48ff0`); canonical promotion remains
  review-gated.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~PubChemPugRestCompoundAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~FdaOpenFdaDrugLabelAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionPrimitivesTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
rtk git status --short
```

## Acceptance

- The focused PubChem test matrix passes.
- Existing FDA, shared-primitives, and full KnowledgeWorker suites remain green.
- The changed-file set remains inside this parcel's adapter, synthetic fixture, test, and parcel-record scope.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories are reported as pre-existing and unchanged.
- Publishing and merging remain human-controlled.

## Handoff

- Starting commit: `554ea2b`
- Ending commit: uncommitted changes on `554ea2b`
- PubChem adapter tests: 46 passed, 0 failed, 0 skipped.
- Existing FDA adapter tests: 21 passed, 0 failed, 0 skipped.
- Shared acquisition-primitives tests: 55 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker tests: 437 passed, 0 failed, 0 skipped.
- Synthetic fixture JSON parsed successfully.
- Whitespace checks reported no errors.
- Warnings: pre-existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories only.
- No files were staged, committed, pushed, or published.
