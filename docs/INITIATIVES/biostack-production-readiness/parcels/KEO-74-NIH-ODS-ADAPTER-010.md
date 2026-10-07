# KEO-74 NIH ODS adapter 010

## Status

Implemented as a bounded, offline-verifiable adapter parcel. This parcel does
not wire runtime orchestration or persistence, promote claims, retain a live
response, or authorize any source beyond `nih-ods`.
Independent re-review passed after the fail-closed repair.

## Governed mapping

- Source: `nih-ods`
- Planning adapter: `nih-ods-planning-v1`
- Transformation: `nih-ods-fact-sheet-strict-inline-v1`
- Catalog target: ordinal-exact canonical compound `Glutathione` and exactly
  one ordinal-exact search term `Glutathione`; case changes, surrounding
  whitespace, aliases, and additional terms do not match
- Fixed request:
  `https://ods.od.nih.gov/api/?outputformat=XML&readinglevel=Health%20Professional&resourcename=ImmuneFunction`
- Page: Dietary Supplements for Immune Function and Infectious Diseases,
  Health Professional
- Exact section: `N-acetylcysteine and Glutathione`
- Allowed source branches: section introduction and `Efficacy`
- Excluded source branch: `Safety`
- Authorized-use intersection: `identity`, `mechanism`, `efficacy-claims`,
  and `interactions`; the adapter emits only fields actually present in the
  allowed branches and does not infer interaction content.

Any other compound or search term returns `NoMatch` before HTTP. No alias was
added because the governed market catalog supplied no independently reviewed,
safe exact alias for Glutathione.

## Source and schema observations

The official ODS API documentation, official XSD, and live XML were inspected
for structure only. No live response was saved or copied into tests.

- Namespace: `http://tempuri.org/factsheet.xsd`
- Root: `Factsheet`
- Required mapped elements: `FSID`, `LanguageCode`, `Reviewed`, `URL`,
  `Title`, and `Content`
- `Content` is an XML string containing the stripped fact-sheet HTML fragment.
- The live target is an `h3` section with four introductory paragraphs,
  followed by `Efficacy`, a nested `HIV infection` heading and two
  paragraphs, then `Safety`.

The checked-in fixture is visibly synthetic. Its sentences, identifiers, date,
reference, and excluded material are test inventions; only schema names,
required title identity, first-party paths, and section headings model the
official contract.

## Security and transport boundary

- one request per acquisition;
- globally serialized request gate;
- HTTPS, exact first-party host/path/query, no credentials;
- redirect-disabled transport and explicit 3xx rejection;
- no retries or paging;
- 1 MiB response ceiling;
- exactly HTTP 200 is accepted after the explicit 429 and 503 mappings; 202,
  206, 404, and every other status fail closed;
- only `application/xml` and `text/xml`;
- 429 maps to `RateLimited`;
- 503 maps to `BackPressure`;
- 403 raises `access-denied` and does not attempt a browser, cookie, CAPTCHA,
  challenge, or user-agent bypass;
- DTDs are prohibited, the XML resolver is null, external entities are
  disabled, and both the response document and embedded content fragment are
  parsed under hardened XML settings.

## Extraction and rights boundary

The adapter retains bounded, complete source-authored paragraph excerpts only.
Retained paragraphs may contain plain text and the exact attribute-free,
non-namespaced inline element allowlist `em`, `strong`, and `sup`. A paragraph
containing a link, image, table, unknown element, namespaced element,
attributed inline element, comment, processing instruction, or CDATA is omitted
whole and the batch is marked truncated. No descendant text is selectively
deleted from a retained paragraph. The adapter normalizes surrounding
whitespace but does not summarize or fabricate text. It also excludes top-level
images, logos, tables, figures, references, bibliography content, scripts,
styles, separately copyrighted/third-party material, the `Safety` subsection,
and neighboring sections.

Every candidate:

- acknowledges the NIH Office of Dietary Supplements;
- identifies the reviewed public-domain text scope and ODS reuse guidance;
- carries page title, exact section, page updated date, source URL, query URL,
  registry binding, retrieval time, and transformation version;
- requires human claim-level review before canonical promotion;
- records that ODS is not a product label and cannot independently support
  product-specific dosing, a medical conclusion, or individualized guidance;
- requires non-endorsement treatment.

## Operational blocker

On 2026-07-25, a plain anonymous GET to the exact fixed final URI returned
HTTP 403 with a Cloudflare HTML challenge. The adapter classifies this as
`access-denied` and intentionally does not bypass it. This is an operational
source-access blocker for live use, not a parser or fixture failure. Runtime
wiring must remain off until the approved anonymous request succeeds or an
independently reviewed ODS access method is authorized.

## Verification contract

- focused NIH ODS adapter tests;
- shared source-acquisition guard and transport tests;
- FDA adapter regression tests;
- full `BioStack.KnowledgeWorker.Tests` suite;
- XML fixture parse, whitespace, and worktree scope checks.

The focused suite includes wrong namespace, top-level order, duplicate and
unknown top-level elements, invalid and noncanonical FSIDs, wrong language and
source URLs, invalid and noncanonical Reviewed dates, unexpected and namespaced
inline elements, exact 200 enforcement including 202/206/404 rejection, exact
catalog casing and whitespace, and cancellation at both the serialized gate and
HTTP transport.

Verification after the independent-review repair:

- NIH ODS focused tests: 53 passed, 0 failed, 0 skipped;
- shared source-acquisition primitive tests: 55 passed, 0 failed, 0 skipped;
- FDA adapter regression tests: 21 passed, 0 failed, 0 skipped;
- full KnowledgeWorker suite: 444 passed, 0 failed, 0 skipped;
- targeted source and test whitespace formatting verification: passed;
- synthetic XML parse, trailing-whitespace scan, and four-file scope check:
  passed.

The test runs retain pre-existing `System.Security.Cryptography.Xml` 10.0.9
`NU1903` advisory warnings and the pre-existing FDA test nullable warning. This
parcel adds no package or warning.

No commit, push, pull request, live fixture, persistence write, or runtime
registration belongs to this parcel.
