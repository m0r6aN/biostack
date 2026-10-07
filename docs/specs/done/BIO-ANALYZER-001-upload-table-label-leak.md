---
ticket: BIO-ANALYZER-001
title: Protocol upload — table/field labels must not become compounds; lock graceful-failure guarantees
status: active
revision: 3
owner: clinton.morgan
created: 2026-10-03
updated: 2026-10-03
supersedes: null
superseded_by: null
risk: standard
delivery_classes: [standard]
guidance_classes: [not-applicable]
substance_function_risk: [not-applicable]
function_review_status: not-applicable
surfaces:
  - backend/src/BioStack.Application/Services/ProtocolParser.cs
  - backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs
  - backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs
  - backend/tests/BioStack.Api.Tests/Integration/AnalyzeEndpointsIntegrationTests.cs
  - docs/goals/protocol-upload-graceful-failure/EVIDENCE.md
routing_class: standard-fix
data_classification: internal
---

# BIO-ANALYZER-001 — Table/field labels leak as compounds; lock graceful-failure guarantees

## Goal

Charter: `docs/goals/protocol-upload-graceful-failure/CHARTER.md` (Gate 1 ratified 2026-10-03, D1–D5).

Close the one remaining release-blocking gap in the uploaded-protocol analyzer and lock both user guarantees with regression tests:

1. Ordinary PDF/DOCX/CSV/XLSX uploads fail gracefully (no crash/500, no wall of fake compounds; honest bounded result or clean 4xx with a friendly message).
2. Real protocol content parses without treating document headers, prose, citations, or table labels as compounds — **within the documented residual below**.

## Verified facts on the comparison base (coordinator, `1c8a16e5e950e3b75f559e9c8c0744f28db2f6a5`)

- Reproduced: CSV `Compound,Dose,Frequency,Route,Duration` with rows `BPC-157,500mcg,daily,SubQ,4 weeks` etc. → `SpreadsheetProtocolExtractor` emits segments `Compound: BPC-157 | Dose: 500mcg | Frequency: daily | ...`; `ProtocolParser.ParseAsync` returns entries `BPC-157`, **`Frequency`** (freq=`daily`), `Retatrutide`. (`ProtocolEntryResponse.Recognized` defaults to `true` at parser level; the analyzer overwrites it in `MarkRecognizedEntries`, so end-to-end assertions MUST NOT rely on `Recognized` for the leak.)
- Mechanism: `ProtocolParser.ParseSegment` recognition gate (≈ lines 175–191): segment `Frequency: daily` matches `FrequencyPattern` (the word `daily`, not `Frequency`); `BuildNameSlice` returns `Frequency`; `IsLikelyCompoundName("Frequency")` is true (Title-case alphabetic, length ≥ 3, ≤ 3 tokens, not a schedule code). `BuildNameSlice` strips only `take|inject|dose|with|for|at|on|and` as whole words, so `Dosing`, `Injection`, `Dose/Frequency`→`/Frequency`, `(mg)` survive.
- `AnalyzeEndpoints.AnalyzeProtocol` already maps `ProtocolIngestionException` to `Results.BadRequest(new { message })` (around both `ParseRequestAsync` and `AnalyzeAsync`) and any other `Exception` to `Results.Problem`. The 400 for an upload is produced inside `AnalyzeAsync`, after the feature gate (`EnsureEnabled`), so T8 needs an Operator subscription.
- Parse cache key = `analyzer:parse:parser-{ProtocolFingerprintService.ParserVersion}:{ParseFingerprint}` (`ProtocolAnalyzerService.cs:84`, 7-day TTL); `ParserVersion` is const `"v3"` (`ProtocolFingerprintService.cs:10`). Cached leaked results would outlive the fix unless the version is bumped. `ParserVersion` has no test dependents (only `ScoringVersion = "v3"` is referenced in `ProtocolFingerprintServiceTests.cs`).
- Already tested on base (do not duplicate): `PdfProtocolExtractorFlateTests` (incl. `ExtractAsync_CorruptFlateDecodeStream_…` → "did not expose readable text" at the extractor layer), `ProtocolIngestionDocxStructureTests`, `ProtocolAnalyzerDocxPacketGoldenTests`, `ProtocolIngestionServiceTests` (CSV/DOCX/XLSX extraction shape; `CreateMinimalXlsx`, `AddEntry` helpers).
- Baseline: `dotnet test backend/tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol"` = 258 passed, 0 failed.

## Out of Scope

- Rewriting the regex PDF extractor, OCR hardening, row-aware table reconstruction (e.g. recovering the dose that lives in a sibling `Dose: 500mcg` cell — the BPC-157 row therefore still reports dose 0; this is known and unchanged), frontend work, any change to `AnalyzeProtocolResponse`, `IProtocolParser`, feature gating/billing, or the `ProtocolIngestionException` contract.
- **Documented residual (accept-as-documented, reported in HANDOFF; NOT fixed here, NOT locked by tests):** a prose line consisting of a single Title-case word plus a frequency word (e.g. `Review daily`, `Hydrate weekly`) still passes the recognition gate and is emitted as an unknown compound. Fixtures in T5/T6 therefore must not contain such lines; the builder must not "fix" this under this parcel.
- Header words outside the C1 set (the set is a closed ruling for this parcel; a new leak word is reported, not added). Label word + non-label word names (e.g. `Compound Epitalon`, `Injection Site`, `Frequency Type`) are accepted by design; T4 locks that.
- Push, PR, merge, deploy, cloud/settings. Any change to production-readiness gate status.

## Change

### C0 — Parse-cache invalidation

`ProtocolFingerprintService.ParserVersion`: `"v3"` → `"v4"` (one-line change). Rationale: C1 changes parse output for identical text; without the bump cached leaked results survive 7 days.

### C1 — Structural-label rejection

In `ProtocolParser.cs`, add a closed `static readonly HashSet<string> StructuralLabelWords` (`StringComparer.OrdinalIgnoreCase`) and make `IsLikelyCompoundName` return `false` when the candidate name, tokenised on runs of non-letters (`[^\p{L}]+`, empty parts dropped), has **at least one** part and **every** part is in the set. A name with zero letter-parts is left to the existing must-contain-a-letter rule. Tokenising on non-letters makes `/Frequency`, `Frequency (per week)`, `(mg)`, `Dose/Frequency`, `BPC-157`→`BPC` all evaluate on letters only. The check is a pure reject placed after the existing token checks; existing checks are not modified.

Set (exact, closed):

`Frequency, Frequencies, Compound, Compounds, Dose, Doses, Dosage, Dosages, Dosing, Route, Timing, Time, Duration, Durations, Administration, Directions, Schedule, Schedules, Protocol, Goal, Goals, Note, Notes, Reference, References, Version, Tracking, Baseline, Evidence, Phase, Support, Stack, Materials, Blood, Work, Week, Weeks, Day, Days, Month, Months, Name, Item, Product, Amount, Unit, Units, Total, Strength, Quantity, Concentration, Injection, Vial, Regimen, Cycle, Medication, Substance, Drug, Agent, Peptide, Supplement, Starting, Maintenance, Target, Max, Min, Current, Per, Mg, Mcg, Ml, Iu`

Consequences accepted: a name made only of these words is never a compound (e.g. a bare `Peptide`); a name with any other letter-part (e.g. `Blood Flow Peptide`, `Peptide Alpha`, `BPC-157`, `GHK-Cu`) is unaffected. Applies on every input path (paste and upload) because the gate is shared. No other production logic changes.

### C2 — Regression tests (new class `ProtocolUploadGracefulFailureTests`, Application.Tests)

Tests use real classes (no mocks of parser/extractors), use `LocalKnowledgeSource`, and are written first; the red run is captured before C0/C1.

| # | Test intent | Layer / fixture requirements |
|---|---|---|
| T1 | CSV: header `Compound,Dose,Frequency,Route,Duration,Timing,Notes` (Title-case) + ≥2 rows with alias compounds `BPC-157` and `Retatrutide` (dose, frequency, route, duration, timing, note cells populated). Assert parsed names contain both compounds and **no** name equals any header label (case-insensitive). Assertion messages must print the leaked names. | `ProtocolIngestionService.IngestAsync` (real `SpreadsheetProtocolExtractor` + `ProtocolNormalizationService`) → `ProtocolParser.ParseAsync(NormalizedText)` |
| T2 | Same data as in-memory XLSX built with the `ZipArchive` pattern of `ProtocolIngestionServiceTests.CreateMinimalXlsx`/`AddEntry` (copy locally; do not edit that file); additionally an ALL-CAPS header variant (`COMPOUND,DOSE,FREQUENCY`) in T1 or T2. Same assertions. | same route |
| T3 | `[Theory]` over **every** word in the C1 set, segments `"<Word>: daily"` and `"<Word>: 500mcg"`, plus compound-header shapes `Dose/Frequency: daily`, `Frequency (per week): daily`, `Dose (mg): 5 mg`, `Injection Frequency: daily`, `Dosing Schedule: weekly`, `FREQUENCY: daily`: zero entries each. Red-run evidence lists which cases fail pre-C1 (not all will; cases already rejected by dose-verb stripping are acceptable as non-failing). | `ProtocolParser.ParseAsync` |
| T4 | Non-regression: `"BPC-157 500mcg daily"`, `"Blood Flow Peptide 500mcg daily"`, `"Peptide Alpha 2mg weekly"` each emit exactly one entry with the expected name. Also `"Frequency Booster 2mg daily"` emits one entry (a label word plus a non-label word is not a label). | parser |
| T5 | Prose-only DOCX built from headings, multi-word lowercase narrative sentences, a `Note:` line, and a citation line with a year and URL; **no dosing and no single-word-plus-frequency lines** (see residual). The `Note:` line may contain a frequency/dose word (then T5 is legitimately red pre-C1; that is NOT a stop condition) or not. Via analyzer service with `IProtocolTextExtractor[] { PlainTextProtocolExtractor, DocxProtocolExtractor }` as in `ProtocolAnalyzerDocxPacketGoldenTests`: assert `Protocol` entries empty, `Scored == false`, `ParseConfidence == "none"`, no issue references any prose/citation fragment. | analyzer service |
| T6 | Real-protocol PDF (uncompressed content stream, `BT (…) Tj ET` per line; literal `(` and `)` inside line text MUST be escaped as `\(` / `\)` because `ExtractTextOperators` ends a string at the first unescaped `)`, otherwise the line is silently dropped and the exclusion assertions pass vacuously) containing a title line, a lowercase narrative sentence, a citation line with a parenthesised year, `BPC-157 500mcg daily`, `Retatrutide 2mg weekly`. Via real `ProtocolIngestionService` + `PdfProtocolExtractor` + `ProtocolParser`: first assert the extracted text contains the citation and narrative lines (proves they were read), then entry names are exactly {BPC-157, Retatrutide} (compare as sets, canonical alias names). Fixture lines must avoid the documented residual. | ingestion → parser |
| T7 | Image-only/no-text PDF (`%PDF-1.4` with a stream containing no text operators) through `ProtocolIngestionService.IngestAsync`: throws `ProtocolIngestionException`; `Message` contains `readable text` (OrdinalIgnoreCase) and contains none of `Exception`, `   at `, `BioStack.`. | ingestion (extends the extractor-level case already covered) |

### C3 — Endpoint test (existing `AnalyzeEndpointsIntegrationTests`, Api.Tests)

T8: sign in, give the user an Operator subscription (existing helpers `SignInAsync`, `UpsertSubscriptionAsync`), POST `multipart/form-data` to `/api/analyze/protocol` using `MultipartFormDataContent` with a `ByteArrayContent` part named `file`, filename `no-text.pdf`, content-type `application/pdf`, bytes duplicated locally from T7 (test projects cannot share the fixture), plus form field `inputType=FileUpload`. Assert status `400` (not 500) and JSON body has a non-empty string `message` containing `readable text`. This is the first multipart test in Api.Tests; add a tiny private helper in the same class.

## Acceptance Criteria

1. AC1 — Before C0/C1, T1/T2 and the label-bearing T3 cases fail (T5 may too, see T5) with output naming the leaked label (e.g. `Frequency`); after, all pass. Red and green runs saved as evidence, including the list of T3 cases that were red.
2. AC2 — T1–T7 pass; the 258 baseline tests still pass (total ≥ 258 + new Application tests; zero failures).
3. AC3 — T4 passes: no real-compound regression from C1.
4. AC4 — `dotnet test tests/BioStack.Api.Tests --filter "FullyQualifiedName~AnalyzeEndpointsIntegrationTests"` green incl. T8.
5. AC5 — Diff touches only the Allowed Files; no public contract/type/signature changes (`ParserVersion` constant value only).
6. AC6 — Nothing pushed, no PR opened, branch exists only locally.

## Allowed Files

- `backend/src/BioStack.Application/Services/ProtocolParser.cs`
- `backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs`
- `backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs` (new)
- `backend/tests/BioStack.Api.Tests/Integration/AnalyzeEndpointsIntegrationTests.cs`
- `docs/goals/protocol-upload-graceful-failure/EVIDENCE.md` (new; builder writes red/green outputs and counts)

Coordinator-owned (builder must not edit): this spec, `CHARTER.md`, `GATE2.md`, `HANDOFF.md`, `loop-directive.md`, `docs/specs/INDEX.md`.

## Forbidden

- Any other file; any change to `IProtocolParser`, response/request contracts, `AnalyzeEndpoints.cs`, extractors, feature gates, frontend, `ScoringVersion`.
- Extending the C1 set, adding other name heuristics, or loosening other `IsLikelyCompoundName` rules; fixing the documented residual.
- Deleting or weakening existing tests. Mocks of `ProtocolParser`/extractors in T1–T7.
- Push/PR/merge/deploy.

## Verification (deterministic, run from `backend/` in the named worktree)

1. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~ProtocolUploadGracefulFailureTests"` — red before C0/C1 (T1–T3 subset), green after.
2. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol"` — green, count ≥ 258 + new.
3. `dotnet test tests/BioStack.Api.Tests --filter "FullyQualifiedName~AnalyzeEndpointsIntegrationTests"` — green.
4. `git diff --check`; `git diff --stat origin/main...HEAD` lists only Allowed Files plus coordinator docs.

## Stop-and-Report Rule

Builder stops and returns to the coordinator if: C1 breaks any existing test; T5–T8 expose a defect requiring changes outside Allowed Files (record observed vs expected, do not fix); a label leak outside the C1 set appears (report the word, do not extend); a required test needs a mock of the parser/extractor; the Api.Tests multipart upload cannot be constructed as specified.

## Rollback

Revert the commit: `ProtocolParser.cs` hunk + `ParserVersion` value; tests are additive. No data, migration, or config impact; reverting also returns the cache key to `v3` (previously cached `v3` entries become live again within their TTL).

## Existing Patterns To Follow

- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionDocxStructureTests.cs` (parser/extractor harness, `CreateParser`).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolAnalyzerDocxPacketGoldenTests.cs` (analyzer construction and assertions).
- `backend/tests/BioStack.Application.Tests/Services/PdfProtocolExtractorFlateTests.cs` (PDF byte construction).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionServiceTests.cs` (`CreateService`, `CreateMinimalXlsx`, `AddEntry`).
- `backend/tests/BioStack.Api.Tests/Integration/AnalyzeEndpointsIntegrationTests.cs` (`SignInAsync`, `UpsertSubscriptionAsync`).

## Spec review triage (SpecReviewerA, rev 1 → rev 2)

| Finding | Ruling |
|---|---|
| C1 closed set misses `Dosing`/`Strength`/`Injection`… | fix — set extended; closed ruling |
| Punctuation/parenthetical bypass (`Dose/Frequency`, `(mg)`) | fix — tokenise on non-letters; T3 cases |
| Ruling must live in spec, not builder | fix — set is the ruling; residual recorded |
| Prose `Review daily` leaks (pre-existing); T5/T6 fixture-dependent | accept-as-documented residual; fixtures constrained; carried to HANDOFF as developer decision |
| Parse cache stale after fix | fix — C0 `ParserVersion` bump |
| T7 duplicates extractor test; unspecified assertion | fix — concrete assertions; layer difference stated |
| T1/T2 must go through `IngestAsync` normalization; Title-case/ALL-CAPS | fix |
| T3 partly vacuous pre-C1 | fix — red evidence lists failing cases; T4 sentinels |
| T8 construction details | fix — multipart form, gate, bytes duplicated |
| `Recognized=true` wording; charter mechanism sentence | fix — spec wording corrected; charter mechanism corrected in amendment log |

## Spec re-review closure (SpecReviewerA on rev 2 @ `70dc5d47`, `VERDICT: PASS`, 0 blocking)

| Finding | Ruling |
|---|---|
| T5 `Note:` red/green ambiguity; T6 PDF paren escaping makes exclusions vacuous | fix — rev 3 clarification (T5/T6/AC1) |
| Residual should name label + non-label word joins | fix — rev 3 Out of Scope bullet |
| C1 placement/comparer unspecified; EVIDENCE.md missing from frontmatter surfaces | fix — rev 3 (clarification only; no semantic change after PASS) |
