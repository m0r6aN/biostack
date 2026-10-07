# Parcel: KEO-74-PUBMED-EUTILITIES-CITATION-ADAPTER-008

Status: implementation and verification complete; independent review corrections applied; publish pending.

## Goal

Implement a bounded PubMed citation-metadata adapter behind the approved KEO-74 acquisition plan without retrieving publication content or adding scheduling, persistence, canonical promotion, or publication.

## Source and endpoints

- Source registry ID: `pubmed`
- Planning input: blocker-free `pubmed-planning-v1` API intents only
- Transformation: `pubmed-eutilities-citation-metadata-v1`
- ESearch: `https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi`
- ESummary: `https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esummary.fcgi`

ESearch uses one bounded query of at most 20 validated distinct terms. Every term is quoted and restricted to `[Title/Abstract]`. The fixed parameters are `db=pubmed`, `retmode=json`, `retstart=0`, `retmax=50`, `sort=pub_date`, and `usehistory=n`. ESummary requests the returned PMIDs in one bounded call with `retmode=json`; the XML-only `version=2.0` parameter is not sent.

The public constructor requires a validated NCBI `tool` name and contact email. They are transmitted only on the actual first-party E-utilities requests. The candidate query URL is a redacted ESearch URL containing neither value, and neither value is copied into candidate metadata. This parcel does not accept an API key.

## Authorization and evidence boundary

The adapter requires:

- a blocker-free, registry-bound PubMed API intent;
- the exact approved registry SHA-256 and complete PubMed provenance contract; and
- a nonempty authorized-use intersection from `mechanism`, `efficacy-claims`, and `interactions`.

The allowlist is citation metadata only:

- article title;
- journal/source;
- raw PubMed publication date;
- publication types;
- languages;
- volume;
- issue;
- pages; and
- e-location.

The raw `pubdate` value is preserved as `publicationDate`; partial dates are allowed and no date is synthesized from sorting metadata. PMID is required and must correlate across ESearch, ESummary UIDs, the keyed result object, and `articleids`. DOI and PMCID are present when supplied; otherwise each is represented as typed `not-provided` provenance with a substantive reason.

Citation metadata and indexing are not evidence of mechanism, efficacy, interactions, study quality, clinical applicability, safety, causality, or certainty. Every candidate remains `review-required`.

## Correlation and fail-closed rules

- ESearch and ESummary must identify their operation through the expected response header type and must not declare an error status or error object.
- ESearch `count`, `retmax`, and `retstart` are nonnegative integer strings; `retstart` must equal zero and returned `retmax` must equal the returned ID-list count.
- ESearch IDs are unique positive canonical PMID strings and the ID count must equal `min(count, 50)`.
- ESummary `result.uids` must exactly match requested PMIDs in ESearch order.
- ESummary must contain exactly one keyed object per requested PMID, with no extras, and each object UID must equal its key.
- Every `articleids` entry must be an object. Identical repeated identifiers may deduplicate; conflicting PMID, DOI, or PMCID values fail closed.
- Output order is the ESearch PMID order.

## Transport and rights controls

- HTTPS, the first-party `eutils.ncbi.nlm.nih.gov` host, and the two endpoint paths are fixed in code.
- Redirects are disabled and the anonymous client has a 20-second timeout.
- ESearch bodies are capped at 256 KiB; ESummary bodies are capped at 1 MiB.
- Requests and responses are bounded to 4096-character URIs, JSON, one ESearch window, and one ESummary call.
- HTTP 429 returns `RateLimited`; HTTP 503 returns `BackPressure`; `Retry-After` is preserved.
- A JSON rate-limit error returns `RateLimited`. Other JSON errors, redirects, HTTP 202, other non-success responses, malformed JSON, non-JSON responses, and correlation failures fail closed.
- A later failure returns no candidates; there is no partial output and no retry.
- A shared, cancellation-safe NCBI gate serializes request starts at least 334 milliseconds apart, remaining within the unauthenticated three-request-per-second ceiling.
- Rights attribution is limited to PubMed citation metadata under the reviewed NLM policy. Abstracts, excerpts, PMC and publisher full text, EFetch, and LinkOut content are excluded.

## Boundaries

- Synthetic fixtures and recording handlers only; tests make no live request.
- No abstracts, excerpts, EFetch, PMC/full text, publisher full text, LinkOut, or arbitrary response fields.
- No runtime registration, environment configuration, scheduler, persistence, database, canonical-ingest, promotion, frontend, publication, or deployment change.
- Exact production NCBI client identity remains external configuration owned by the later runtime-wiring parcel.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~PubMedEutilitiesCitationMetadataAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~FdaOpenFdaDrugLabelAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionPrimitivesTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~PubChemPugRestCompoundAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
rtk git status --short
```

## Acceptance

- PubMed focused tests pass.
- Existing FDA, shared-primitives, PubChem, and full KnowledgeWorker suites remain green.
- Synthetic fixture JSON parses successfully and whitespace checks report no errors.
- The changed-file set remains inside this parcel's adapter, test, synthetic fixtures, and parcel record.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories are reported as pre-existing and unchanged.
- No files are staged, committed, pushed, or published.

## Handoff

- Starting commit: `176ed3a`
- Ending commit: uncommitted changes on `176ed3a`
- PubMed adapter tests: 62 passed, 0 failed, 0 skipped.
- Existing FDA adapter tests: 21 passed, 0 failed, 0 skipped.
- Shared acquisition-primitives tests: 55 passed, 0 failed, 0 skipped.
- Existing PubChem adapter tests: 46 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker tests: 499 passed, 0 failed, 0 skipped.
- Synthetic fixture JSON parsed successfully.
- Warnings: pre-existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories only.
- Exact production NCBI `tool` and contact email remain external runtime configuration and are not selected or stored by this parcel.
- No files were staged, committed, pushed, or published.
