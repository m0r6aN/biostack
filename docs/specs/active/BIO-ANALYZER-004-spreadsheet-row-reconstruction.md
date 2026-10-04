---
ticket: BIO-ANALYZER-004
title: Spreadsheet uploads — reconstruct each data row so header names never reach the parser and a row's dose binds to its compound
status: active
revision: 6
owner: clinton.morgan
created: 2026-10-04
updated: 2026-10-04
supersedes: null
superseded_by: null
risk: elevated
delivery_classes: [standard]
guidance_classes: [not-applicable]
substance_function_risk: [not-applicable]
function_review_status: not-applicable
surfaces:
  - backend/src/BioStack.Application/Services/ProtocolIngestionService.cs
  - backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs
  - backend/tests/BioStack.Application.Tests/Services/SpreadsheetRowReconstructionTests.cs
  - backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionServiceTests.cs
  - backend/tests/BioStack.Application.Tests/Services/SpreadsheetProtocolExtractorPackageTests.cs
  - docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-004.md
routing_class: standard-feature
verification_class: judgment-required
permission_profile: reviewer-readonly
data_classification: internal
---

# BIO-ANALYZER-004 — Spreadsheet row reconstruction (Residual B + dose recovery)

## Intent

Close two "known gaps, not fixed" from `docs/goals/protocol-upload-graceful-failure/HANDOFF.md` that share one root cause: `SpreadsheetProtocolExtractor.ConvertDelimitedRowsToText` serialises each data row as `Header: value | Header: value | …`, and `ProtocolParser` then splits on `|` and treats every cell as an independent segment. Consequences: (B) any header word outside BIO-ANALYZER-001's closed set (`Vial Size: 5mg`, `Start Date`, `Supplier`, `Qty`, `Price`, `Description`) leaks as a fake compound whenever its cell value carries a dose or frequency word; and (dose) the compound cell and its `Dose:` cell are different segments, so every spreadsheet compound reports dose 0. After this parcel the extractor emits **one reconstructed line per data row** (`<name> <dose> <frequency> <duration>`) built from recognised header roles, where dose/frequency/duration are reduced to the exact phrase the parser understands; it drops columns the parser cannot use, and no header name ever reaches the parser. The effect is correct per-row dose/frequency for CSV/XLSX uploads and no header-word compounds. Consumers: the uploader, the analyzer scoring and evidence comparison that consume `ProtocolEntryResponse.Dose`, the extracted-text preview, and linked CSV/XLSX (`LinkProtocolExtractor` nests this same extractor).

## Prerequisites and ordering

- `BIO-ANALYZER-001` merged (its `ParserVersion = "v4"`, C1 gate and `ProtocolUploadGracefulFailureTests` T1/T2 are regression locks and MUST pass unmodified).
- `BIO-ANALYZER-002` merged (same class and file; this parcel assumes its corrected relationship resolution and does not touch it).
- `BIO-ANALYZER-003` merged **before** this parcel (both edit `ProtocolFingerprintService.cs`, adjacent lines 10 and 8). Required order: 002 → 003 → 004.
- Facts below cite line numbers as of the comparison base; they shift after 002/003 merge. The builder locates code by member name.

## Verified facts (coordinator + two independent reviewers, branch `fix/protocol-upload-graceful-failure` @ `a12d580d`)

- `ConvertDelimitedRowsToText` (`ProtocolIngestionService.cs` ≈ 604-631): emits `Sheet: {name}`, then for each data row `Header: value` joined by ` | ` when a header exists and `header.Count >= row.Count`; otherwise the bare joined row. `hasHeader` = the first row has any letter. A headerless sheet whose first row contains letters therefore loses that row as a "header" (pre-existing; out of scope).
- Parser side (`ProtocolParser.SplitIntoSegments`, ≈ 404-423): splits on newline, `;`, ` + `, `|`, and additionally splits a segment on commas when every comma clause matches `DosePattern`. A `Vial Size: 5mg` segment has a dose, name slice `Vial Size`, passes `IsLikelyCompoundName` (`Size` is not in the closed set) → leaks. The compound cell and the `Dose:` cell are separate segments → dose 0.
- Parser consumption: name; dose via `DosePattern` — a number followed by `mcg|micrograms?|ug|μg|µg|mg|milligrams?` (a bare number, `IU`, `mL` are not doses; no leading `\b`, no range awareness: `250-500mcg` matches only `500mcg`); frequency via `FrequencyPattern`; duration via `DurationPattern`; `CyclePattern` strips cycle phrases (`8 weeks on, 8 weeks off`) before duration is read. `ProtocolEntryResponse` = `(CompoundName, Dose, Unit, Frequency, Duration, Recognized)`; route/timing/notes are never analysed. `ResolveAliasName` takes the longest alias found anywhere in the segment, so any free-text cell appended to a parsed line can rename the row. `BlendDecomposerService.Decompose` runs only for a segment containing `blend` and then takes the first parenthetical anywhere in it (a blend-named row with a `daily (AM)` frequency cell would create a fake component).
- A row line is re-split by the parser on newline, `;`, `|`, ` + ` and dose-only comma clauses, so un-sanitised mapped cells can fragment a row (a name cell `BPC-157⏎(Wolverine)` yields a fake `(Wolverine)` compound with the dose; `BPC-157 + TB-500` binds the dose to the second compound only).
- `IsLikelyCompoundName` rejects a name slice containing a letterless token, so `Zorbatide 5 weekly` (bare-number dose written as-is) drops the whole row silently; and a name column holding a row index (`Item,Compound,Dose` with `1,BPC-157,500mcg`) yields name `1` and loses the row.
- XLSX cell placement bug: `ReadCellValue` rows are built from `row.Elements(c)` in document order and ignore the cell reference `r="C2"`. Writers omit empty cells, so a blank cell shifts later columns left. `ReadCellValue` returns only `<v>`: `t="inlineStr"` cells (`<is><t>…`, used by streaming writers) read as empty. XLSX already drops all-blank rows (≈ line 525); CSV does not.
- CSV reading: `ReadDelimitedRows` splits on every `,`, trims quotes, treats lone `\r`, `\r\n`, `\n` as row breaks; `Encoding.UTF8.GetString` does not strip a BOM, and `NormalizeExtractedText` (`Trim`) does not strip U+FEFF either.
- NormalizedText is returned to the client via `CreateExtractedTextPreview(ingestion.NormalizedText)` (`ProtocolAnalyzerService.cs` ≈ 150; preview truncated to 400 chars). The frontend only renders `extractedTextPreview` (`whitespace-pre-wrap`) and stores it; it never parses the `Header: value` shape.
- `ProtocolExtractionResult.Warnings` can carry the skipped-row note without a public-signature change (`ConvertDelimitedRowsToText` is private static). Extraction warnings reach the client as `ExtractionWarnings`; the frontend downgrades displayed confidence to `Medium` when any warning is present (`analyzerView.ts` ≈ 304). `IngestAsync` consumes `Warnings.FirstOrDefault()` only when the text is empty.
- Ingestion cache key = `analyzer:ingestion:{mode}:{IngestionVersion}:{hash(bytes)}`, `IngestionVersion = "v1"` (`ProtocolFingerprintService.cs:8`), 7-day TTL (`ProtocolIngestionService.cs:17`); the cached value contains `NormalizedText` and `Warnings`. The parse cache is keyed by hash of `NormalizedText`, so it self-invalidates on text change. No test references `IngestionVersion` or the `analyzer:ingestion` literal.
- Existing tests that assert the current shape and MUST be updated by this parcel: `ProtocolIngestionServiceTests.cs` ≈ lines 42, 43 (`Contains("Compound: BPC-157")`, `Contains("Dose: 500mcg")`) and 79 (`Contains("Compound: BPC-157")`) — **three** assertions; `Contains("Sheet: Stack")` (line 78) stays. In addition, after BIO-ANALYZER-002 merges, `SpreadsheetProtocolExtractorPackageTests.cs` (002 T3/T4/T8) pins a golden `ExtractedText` literal in the old `Header: value` shape for a `Compound`/`BPC-157` workbook; this parcel legitimately changes that text, so the builder updates **only those golden literals** (the absolute/`..`/unreferenced-relationship assertions stay equality checks against them) to the reconstructed shape.
- BIO-ANALYZER-001 `ProtocolUploadGracefulFailureTests` T1/T2 drive CSV/XLSX through `IngestAsync` → `ParseAsync` and assert names only. Reviewer trace: they reconstruct to `BPC-157 500mcg daily 4 weeks` / `Retatrutide 2mg weekly 12 weeks` and pass unchanged. [INFERENCE] until executed — the builder confirms by running them before editing any test.
- `LocalKnowledgeSource` holds five compounds: `BPC-157`, `TB-500`, `MOTS-C`, `NAD+`, `Retatrutide`.
- Dose recovery changes analysis, not only the preview: `Dose`/`Unit` flow into `NormalizedProtocol`, scoring, `ProtocolEvidenceContextComparer`, and generated issues (`ProtocolAnalyzerService.cs` ≈ 86-125). Identical spreadsheet uploads will score and flag differently after this parcel (previously every spreadsheet compound had dose 0).

## Constraints

### Header roles (closed vocabulary, ratified)

Header normalisation for matching: strip BOM/NBSP, trim, lowercase; capture and remove **one** trailing parenthetical group (its text is the *unit hint*, used only if it fully matches a unit — see Dose binding); replace each run of non-letters with a single space; trim. A role matches only when the **entire** normalised header equals a vocabulary word (exact match; never "contains word"). Roles:
  - name: `compound`, `compounds`, `name`, `peptide`, `product`, `substance`, `item`, `medication`, `supplement`, `drug`, `agent`
  - dose: `dose`, `dosage`, `amount`, `strength`, `quantity` (priority in this order, ties by header order)
  - unit: `unit`, `units`
  - frequency: `frequency`, `freq`, `schedule`
  - duration: `duration`, `length`
  - every other header, including `compound name`, `peptide name`, `product name`, `dose/frequency`, `timing`: unmapped (dropped from parsed text). Stated consequence: a table whose only name-like header is `Compound Name` is non-reconstructable and takes the values-only fallback (no dose binding). Extending the vocabulary is a new spec.

### Reconstructable table and row building

- A table is "reconstructable" iff its first non-blank row (the header row; first in document order) contains at least one name-role column. Rows below it are data rows. All-blank rows (no non-whitespace cell) are discarded **before** reconstruction and before any counting, for CSV and XLSX.
- Cells beyond the header width are ignored (unmapped). Cells missing from a short row are empty.
- **Sanitisation**: every mapped cell value — and every cell of a values-only row — has CR/LF/TAB/NBSP and whitespace runs collapsed to one space and is trimmed, before use.
- **Name**: scan name-role columns in header order; the first whose sanitised value, **after embedded-quantity removal and cleanup (below)**, contains at least one letter is the name. If none: the row is skipped and counted. If the name contains `;`, `|` or ` + `, it is split on those separators and each part that contains a letter is emitted as its **own name-only line** (no dose/frequency/duration — today's behavior for such cells; no dose is bound to a multi-compound cell). Other characters, including parentheticals, are left as written. **Embedded quantities:** if the row has a **non-empty** cell in a dose, frequency, or duration role column (even if that cell's value is later omitted by the rules below), the corresponding `DosePattern`/`FrequencyPattern`/`DurationPattern` phrases are removed from the name text, so the column — including an ambiguous or omitted one — wins over a quantity typed into the name (`BPC-157 5mg` + Dose `250mcg` → dose 250mcg; `BPC-157 5mg` + Dose `TBD` → no dose). If the row has no non-empty cell for that kind, the phrase in the name cell is left as written and serves as the value. **Cleanup after removal:** delete empty bracket pairs (`()`, `[]`), delete leading/trailing separator-only tokens (`-`, `–`, `—`, `:`, `,`, `/`), and re-collapse whitespace (`Zorbatide (5mg)` → `Zorbatide`).
- **Dose**: dose-role columns are considered in the priority order above (ties by header order); take the first with a non-empty sanitised value. If the value contains a range — a number, then an optional `DosePattern` unit, then `-`, `–`, `—` or the word `to`, then a number (`250-500mcg`, `250mcg-500mcg`, `250 mcg – 500 mcg`, `0.5mg to 1mg`) — or a comma between digits (`\d,\d`, e.g. `1,000`), the dose is **omitted**. The range test is deliberately fail-safe and may also omit a dose cell that merely contains such a pattern (`500mcg (days 1-5)`, a date); that is intended. Else if it contains a `DosePattern` match, emit **the first matched substring verbatim** (whitespace-collapsed, e.g. `500mcg`, `5 mg`). Else if it is a bare number (`^\d+(\.\d+)?$` or `^\.\d+$`), append a unit and emit `<number> <unit>`: the unit source is the first non-empty `unit`-role cell in the row **if it fully matches** a `DosePattern` unit alternative (case-insensitive; `mcg`, `mg`, `ug`, `μg`, `µg`, `microgram(s)`, `milligram(s)`); otherwise (empty, or not matching, e.g. `IU`, `mL`, a vial count) the dose column's header parenthetical **if it fully matches** a unit; otherwise the dose is omitted. Any other value (`TBD`, `2 IU`, `1E-3`) is omitted. At most one dose is emitted.
- **Frequency**: frequency-role columns in header order; first whose sanitised value contains a `FrequencyPattern` match; emit **only** that match. Else omit.
- **Duration**: duration-role columns in header order; first whose sanitised value contains a `DurationPattern` match and does **not** contain a range (as above) or match the parser's `CyclePattern`; emit **only** that first match. Else omit.
- The extractor carries its own private copies of the dose, frequency, duration, and cycle patterns (the parser stays untouched), each commented "keep in sync with ProtocolParser".
- Line shape: `<name> <dose> <frequency> <duration>`, single-space joined, empty parts omitted; a name with nothing else is emitted alone. Order is fixed because the parser cuts the name slice at the first dose/frequency token.
- **Skipped-row warning**: rows skipped for having no name are counted across all sheets of the upload; if the total N > 0, exactly **one** entry is added to `ProtocolExtractionResult.Warnings`: `N row(s) skipped: no compound name found.` (no sheet name, no row content). Parser-level drops (e.g. a name with no dose/frequency that is not a known alias) are not warned. The CSV path, which returns no warnings today, carries the same warning.

### Non-reconstructable tables (ratified: values-only fallback)

- If the header row has no name-role column (or the sheet has no header row — first row has no letter), every row is emitted as **all of its non-blank cells (up to the XFD cap; the header-width limit does not apply) in column order joined by ` | `**, sanitised, with no header names. Blank cells are omitted (the old bare join kept them as empty fields; the parser discards empty segments, so analysis is unaffected). As today, when the first row has a letter it is treated as a header and not emitted as data (`hasHeader` semantics unchanged; the pre-existing loss of a letter-bearing first row in a headerless sheet is out of scope).

### Cell placement and CSV fidelity (in scope, ratified)

- XLSX: place each cell by the column letters of its `r` attribute (`^[A-Za-z]{1,3}\d+$`; A=0). Rows are held **sparsely** (no padding to the maximum column); in a reconstructable table only columns below the header width are ever materialised, and in the values-only fallback all cells up to the XFD cap are kept. A column index ≥ 16384 (past `XFD`) is ignored. A cell with no `r`, or a malformed `r`, is placed at (previous cell's index + 1, or 0 for the first). A duplicate reference: the last cell wins. `t="inlineStr"` cells are read from `<is>` by concatenating its `<t>` descendants (in scope: same method, same fidelity class; spec 002 assigns cell reading to this parcel).
- CSV: strip a UTF-8 BOM before parsing; delimiter is `,` only; row breaks are `\r\n`, `\n`, or lone `\r` (as today) except inside quotes; a field is quoted only if its first non-space character is `"` — quoted fields support `""` escapes and embedded commas and newlines; a stray `"` inside an unquoted field is literal; unquoted fields are trimmed. An **unterminated quoted field** is a malformed file → `ProtocolIngestionException` with the existing wording `The spreadsheet contains malformed content and could not be read.` (no silent loss of later rows). Semicolon- and tab-delimited `.csv` files are out of scope: they parse as a single column, take the values-only fallback, and MUST NOT throw.
- CSV quote edge: text after a closing quote in the same field (`"abc"def`) is appended literally to the field value.

### Other

- `Sheet: {name}` line is unchanged.
- Parser untouched. `AnalyzeProtocolResponse` shape unchanged. The preview text and, deliberately, the analysis outputs for spreadsheet uploads change (dose recovery).
- `IngestionVersion` `"v1"` → `"v2"`.
- Tests written first; red run captured before the change.

## Change

### C0 — Ingestion-cache invalidation

`ProtocolFingerprintService.IngestionVersion`: `"v1"` → `"v2"`.

### C1 — Cell fidelity

XLSX sparse placement by `r`, `inlineStr`, column cap. CSV: BOM strip + RFC 4180 reader (replaces `ReadDelimitedRows`) with the failure and fallback rules above. All-blank rows dropped for CSV.

### C2 — Role mapping and row reconstruction

Replace the `Header: value` emission in `ConvertDelimitedRowsToText` with the Constraints behavior, the values-only fallback, and the skipped-row count. Threading the count out of the (private static) method to `ProtocolExtractionResult.Warnings` is in scope; private signature changes are expected.

### C3 — Tests

New class `SpreadsheetRowReconstructionTests` (real `ProtocolIngestionService` + `SpreadsheetProtocolExtractor` → real `ProtocolParser` with `LocalKnowledgeSource`; XLSX via the `ZipArchive` pattern of `CreateMinimalXlsx`/`AddEntry` or the builder in `ProtocolUploadGracefulFailureTests` ≈ 344-409, copied locally). Update the **three** shape assertions in `ProtocolIngestionServiceTests` (lines ≈ 42, 43, 79). Known aliases in fixtures are limited to the five `LocalKnowledgeSource` compounds.

| # | Intent | Fixture / expectation |
|---|---|---|
| T1 | Header leak closed | CSV `Compound,Dose,Frequency,Duration,Vial Size,Start Date,Supplier,Qty,Price,Description`; rows `BPC-157` and `Retatrutide`, every column populated (`Vial Size`=`5mg`, `Description` contains `daily`). Entries == {BPC-157, Retatrutide}; no entry named after any header or unmapped-column cell |
| T2 | Dose binding | Same CSV: BPC-157 dose 500, unit `mcg`, frequency `daily`, duration `4 weeks` (per fixture) |
| T3 | Unknown compound | `Compound,Dose,Frequency` header then row `Zorbatide,5mg,weekly` → one entry `Zorbatide`, dose 5, `mg`, `weekly` |
| T4 | Unmapped text cannot rename | `Compound,Dose,Notes`: Notes = `stack with TB-500` on a BPC-157 row → entry BPC-157 only; no TB-500 |
| T5 | XLSX cell shift | Header `Compound,Dose,Route,Frequency`; row cells at `A2`=`BPC-157`, `C2`=`SubQ`, `D2`=`daily`; `B2` absent. Entry BPC-157 has frequency `daily` and dose 0. The assertion is on frequency: without placement-by-`r` the cells shift left, `daily` lands under `Route` (unmapped) and the frequency is lost, so the test cannot pass on role mapping alone |
| T6a | Headerless BOM CSV | First row numeric/no letters, BOM-prefixed → no `\uFEFF` in `NormalizedText` |
| T6b | Quoted cell before mapped columns | `Compound,Notes,Dose,Frequency` with Notes = `"a, b⏎c"` → dose and frequency bind correctly |
| T6c | BOM + header | BOM-prefixed CSV `Compound,Dose,Frequency` (`BPC-157,500mcg,daily`) → header maps (BOM stripped), entry BPC-157 dose 500 `mcg` frequency `daily`; no `\uFEFF` in `NormalizedText` |
| T7 | Unit hints | `Dose (mg)` header with cell `5` → dose 5 `mg`; separate `Dose` + `Unit` (`mcg`) columns with `250` → dose 250 `mcg`; `Dose (per injection)` with `5` → dose omitted; `Unit`=`IU` with a `Dose (mg)` header and cell `5` → invalid unit cell falls through to the hint → `5 mg`; `Unit`=`IU` with a header lacking a hint → dose omitted. Each row uses an alias compound (`BPC-157`/`TB-500`) or carries a frequency, so the name is still emitted after BIO-ANALYZER-003 |
| T8 | Non-reconstructable table | Headers `Foo,Bar` (no name role): `NormalizedText` contains no header word; cell values present; plus `Compound Name,Dose` (unmapped) → same values-only result |
| T9 | Skipped-row warning | Reconstructable table with one valid row and two rows with empty name → `Warnings` has exactly one entry stating `2`; multi-sheet XLSX with 1 + 1 skipped → exactly one entry stating `2`; no row content echoed |
| T10 | Trailing blank rows | CSV ending in `,,` and XLSX with blank row → no warning, no entry |
| T11 | Case variants | `COMPOUND,DOSE,FREQUENCY` and `Compound,Dose,Frequency`: dose 500 `mcg` and frequency `daily` in both |
| T12 | Duplicate-role priority | `Compound,Strength,Dose` with `BPC-157,5mg,250mcg` → dose 250 `mcg`; `Compound,Strength` only → dose 5 `mg` |
| T13 | Name with a letter | `Item,Compound,Dose` with `1,BPC-157,500mcg` → entry BPC-157 dose 500 |
| T14 | Multi-compound name cell (regression lock — green before and after) | Name `BPC-157 + TB-500`, dose `500mcg` → both compounds emitted, neither bound to the dose (dose 0); no fake third entry |
| T15 | Separators and parentheticals in mapped cells | Name cell `BPC-157⏎(Wolverine)` → one entry BPC-157, dose bound, no `(Wolverine)` compound; dose cell `250mcg; 500mcg` and `250mcg, 500mcg` with Frequency column `daily` → dose 250 **and** frequency `daily` (the second clause must not swallow the frequency); blend-named row (name contains `Blend`) with frequency cell `daily (AM)` → frequency `daily`, no entry named after the parenthetical (builder records whether this fixture is red before) |
| T16 | Wider-than-header rows | Trailing comma (`BPC-157,500mcg,daily,`) and an XLSX row with a cell past the header width → extra cells ignored, entry correct |
| T17 | Hostile cell reference | XLSX with `<c r="ZZZZZZZ1">`, a malformed `r` (`1A`), a duplicate `r`, and an `r` past `XFD` in the same sheet as a valid `BPC-157` / `500mcg` / `daily` row: ingestion completes without exception in under 2 s and the valid row still binds dose 500 and frequency `daily`; hostile cells produce no entries |
| T18 | Sparse columns past Z | Header spanning `A`…`AB` (e.g. `AA`=`Frequency`), value at `AA2` → bound correctly |
| T19 | `inlineStr` | Header and data cells written as `t="inlineStr"` → reconstructed row identical to the shared-string equivalent |
| T20 | CSV row breaks and malformed input | `\r\n`, lone `\r`, `\n` give identical results; unterminated quote → `ProtocolIngestionException` with the malformed-content wording; semicolon-delimited `.csv` → no exception, values-only |
| T21 | No-header sheet | First row all numeric → values only; identical to today's output except that blank cells are omitted |
| T22 | Ingestion-cache invalidation (behavioral) | If the test harness cache is inspectable/seedable: seed an entry at the old `v1` key for a file's fingerprint holding stale `NormalizedText`, ingest the same bytes, assert fresh reconstructed text is returned. If the harness cannot seed, record that and treat C0 as diff-review only |
| T23 | BIO-ANALYZER-001 locks | `ProtocolUploadGracefulFailureTests` pass unmodified |
| T24 | Quantity typed into the name | `Compound,Dose,Frequency` row `BPC-157 5mg,250mcg,daily` → dose 250 `mcg`; row `Retatrutide weekly,2mg,daily` → name Retatrutide, frequency `daily`; row `BPC-157 5mg` with no Dose column → dose 5 `mg` (embedded phrase serves) |
| T25 | Ranges and separators in numbers | Each subject case sits in its **own sheet/file** (or uses a distinct alias) together with a positive-control row (`BPC-157,500mcg,…,12 weeks`) that MUST bind dose 500 and duration `12 weeks` — the parser keeps the highest dose per compound name, so a subject row that shares the control's compound could never show dose 0. Subject cases: dose `250-500mcg`, `250mcg-500mcg`, `250 mcg - 500 mcg`, `0.5 to 1 mg`, `0.5mg to 1mg`, `1,000 mcg` → dose omitted (0); Duration `4-6 weeks` → omitted. Names still emitted (alias fixtures, ≤ five aliases, reused across separate sheets/files) |
| T26 | Cycle phrase in a duration cell | Positive control row with Duration `12 weeks` → `12 weeks`; row with Duration `8 weeks on, 8 weeks off` → duration omitted (empty) |
| T28 | Name cleanup and omitted-dose suppression | Unknown compound `Zorbatide (5mg)` with Dose `250mcg`, Frequency `weekly` → entry `Zorbatide`, dose 250 `mcg` (not dropped by a leftover `()`); name `Zorbatide - 5mg` likewise; name `BPC-157 5mg` with Dose `TBD` or `250-500mcg` → dose 0 (the name-embedded 5mg is suppressed) |
| T27 | Dose emitted verbatim | Dose cells `500mcg` and `5 mg` appear in `NormalizedText` as written (`BPC-157 500mcg daily`, `BPC-157 5 mg daily`) |

## Acceptance Criteria

1. AC1 — Before C0-C2 the builder records, per test, red or green. Expected red: T1, T2, T3, T4, T5, T6a, T6b, T6c, T7, T8, T9, T11, T12, T13, T15 (parts), T17, T18, T19, T22, T24, T25 (positive control and its assertions, as a whole), T26 (positive control), T27, T28; expected green-before regression locks: T10, T14, T21, T23 and parts of T16/T20. Red output names the leaked header/cell, shifted value, or dose 0; after the change all pass. Red and green runs saved as evidence.
2. AC2 — The three updated `ProtocolIngestionServiceTests` assertions now assert the reconstructed row (e.g. contains `BPC-157 500mcg daily`; the dose is emitted verbatim) and `Sheet: Stack` still holds; the golden literals in `SpreadsheetProtocolExtractorPackageTests` (BIO-ANALYZER-002) are updated to the reconstructed shape and nothing else in that file changes; no other existing assertion is edited.
3. AC3 — `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol|FullyQualifiedName~SpreadsheetRowReconstructionTests"` green with count ≥ baseline recorded at start plus new tests; full `BioStack.Application.Tests` green; `AnalyzeEndpointsIntegrationTests` + `AnalyzerGateIntegrationTests` green.
4. AC4 — No header name from any spreadsheet appears as a parsed compound for any fixture above (assertion messages print leaked names).
5. AC5 — Diff touches only Allowed Files; in `ProtocolFingerprintService.cs` the only change is the `IngestionVersion` constant; private-method signature changes in `ProtocolIngestionService.cs` are expected; no public signature or response-contract change.
6. AC6 — Nothing pushed, no PR, branch local only.
7. AC7 — A smoke run records, for one CSV with a `Compound,Dose,Frequency,Duration` layout, before and after: the extracted-text preview, the `Protocol[]` doses/units, and the generated issues/score (EVIDENCE), so the developer sees the user-visible and analysis-visible change.

## Out of Scope

- **DOCX and PDF tables**: the same dose-binding gap exists there (`cell | cell | cell`, no header roles). Known gap, not fixed here; needs its own parcel (different extractors, no header semantics).
- Title rows above the header, multi-row headers, merged cells / fill-down, pivoted layouts, `.xls`/`.ods`, semicolon/tab-delimited CSV, locale number formats, headerless sheets whose first row contains letters (pre-existing first-row loss). For a sheet with a title row above the header, the real header row is emitted as values, so Ratified Decision 1 does not close the header leak there.
- Extending the role vocabulary (including `compound name`-style multi-word headers), route/timing/notes analysis, preserving unmapped columns in the preview (ratified: dropped).
- **Residual value-level leaks (documented consequences, not fixed):** in a non-reconstructable table a free-text value cell with a dose and a Title-case word (`Reconstitute 2mg with BAC water`) still yields a fake compound; in a reconstructable table a name-column cell that is a section or summary label (`Subtotal`) with dose/frequency cells now passes the gate, where before the name cell alone was dropped.
- **Other documented residuals:** multi-compound name cells written with `,`, `/`, `&` or `and` (`BPC-157, TB-500`) are not split — the parser's longest-alias rule picks one compound and the dose binds to it, as for pasted text; a dose cell carrying an embedded frequency (`500mcg daily`) contributes only its dose, so the frequency is lost unless a frequency column exists; European decimal commas (`0,5 mg`) and thousands separators are omitted rather than interpreted; a multi-sheet XLSX fixture for T9 has no existing builder and is written by the builder.
- **Closure-round residuals:** duration ranges with duration units on both sides (`4 weeks - 6 weeks`) report the lower bound; embedded-quantity removal runs on the whole name cell **before** any `+`/`;`/`|` split, and applies only when the row has a non-empty dose/frequency/duration cell, so in a multi-compound cell such as `BPC-157 5mg + TB-500` with empty dose/frequency/duration cells the embedded quantity is kept as written (the parser binds it as for pasted text); embedded-quantity cleanup does not remove partial residue such as `(/mL)`.
- Parser changes (`ProtocolParser.cs` is untouched; BIO-ANALYZER-003 owns the single-word gate).
- XLSX package faults (`BIO-ANALYZER-002`), PDF extractor, OCR, frontend, response contracts, feature gate, billing, push/PR/merge/deploy, production-readiness verdict.

## Allowed Files

- `backend/src/BioStack.Application/Services/ProtocolIngestionService.cs`
- `backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs`
- `backend/tests/BioStack.Application.Tests/Services/SpreadsheetRowReconstructionTests.cs` (new)
- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionServiceTests.cs` (only the three shape assertions named in Verified facts)
- `backend/tests/BioStack.Application.Tests/Services/SpreadsheetProtocolExtractorPackageTests.cs` (BIO-ANALYZER-002's file; only its golden `ExtractedText` literals, updated to the reconstructed shape)
- `docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-004.md` (new; builder writes red/green outputs and the AC7 before/after)

Coordinator-owned (builder must not edit): this spec, `docs/specs/INDEX.md`, `HANDOFF.md`, `EVIDENCE.md`, `CHARTER.md`, `loop-directive.md`, Gate 2 record.

## Forbidden

- Any other file; `ProtocolParser.cs`; `IProtocolParser`; response/request contracts; `AnalyzeEndpoints`; other extractors; feature gates; `ParserVersion`/`ScoringVersion`; extending the role vocabulary; appending any unmapped column to a parsed line; editing `ProtocolUploadGracefulFailureTests`; weakening or deleting existing tests beyond the three named assertions and the 002 golden literals; mocks of parser/extractors; push/PR/merge/deploy.

## Verification (run from `backend/` in the Gate-2-named worktree)

1. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~SpreadsheetRowReconstructionTests"` — red before C0-C2, green after.
2. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~ProtocolUploadGracefulFailureTests"` — green, unmodified.
3. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol|FullyQualifiedName~SpreadsheetRowReconstructionTests"` — green.
4. `dotnet test tests/BioStack.Application.Tests` and `dotnet test tests/BioStack.Api.Tests --filter "FullyQualifiedName~AnalyzeEndpointsIntegrationTests|FullyQualifiedName~AnalyzerGateIntegrationTests"` — green.
5. `git diff --check`; `git diff --stat` lists only Allowed Files.

## Stop-and-Report Rule

Builder stops and returns to the coordinator if: any rule breaks an existing test other than the three named assertions and the 002 golden literals; `ProtocolUploadGracefulFailureTests` T1/T2 need modification; the warning count cannot be threaded without changing `ProtocolExtractionResult`'s public shape; a fixture requires a role outside the closed vocabulary or a known alias outside the five in `LocalKnowledgeSource`; T17 shows allocation or time growth tied to the column index; the diff exceeds Allowed Files (split candidates: C1 cell fidelity as its own parcel, then C2).

## Rollback

Revert the commit: restores `Header: value` emission and `IngestionVersion = "v1"` (previously cached `v1` ingestion results become live within TTL, including the old shape and the header leak). Tests are additive except the three edited assertions, which revert with the commit.

## Ratified Decisions (developer, 2026-10-04)

1. Tables with no recognised name column fall back to **values only** (no header names), not to today's `Header: value` output. This closes Residual B's header leak for all spreadsheet tables.
2. CSV fidelity (BOM, RFC 4180 quoting including embedded newlines) and XLSX cell placement by `r` are **in scope** of this parcel.
3. Unmapped columns (route, timing, notes, supplier, etc.) are **dropped** from the parsed text and preview; they are not surfaced via `Artifacts`.
4. Role vocabulary is as listed in Constraints, including `item`, `product`, `agent`, `length`, `schedule`; `timing` is deliberately unmapped.
5. Risk is `elevated`; Gate 2 MUST require a mandatory independent code review.

## Coordinator rulings from spec review (flagged for developer awareness; refine, do not contradict, the decisions above)

- Header matching is **exact** after normalisation; consequently `Compound Name`, `Peptide Name`, `Product Name`, `Dose/Frequency` are unmapped and fall back to values-only. (Reviewer-found: these headers are common.)
- Dose, frequency and duration are reduced to the single phrase the parser recognises; a unitless bare number is omitted rather than written as-is.
- Multi-compound name cells (`A + B`, `A; B`) emit name-only lines; no dose is bound.
- Duplicate role columns: dose priority `dose > dosage > amount > strength > quantity`; name = first name-role cell containing a letter; frequency/duration = first matching column.
- An unterminated CSV quote is a friendly 400, not silent data loss.
- `t="inlineStr"` cell reading is added to scope; sparse row storage with a column cap replaces dense padding.
- One aggregated skipped-row warning per upload; blank rows are never counted.
- Order 002 → 003 → 004 is required.
- A dose is emitted verbatim as found (`500mcg`); a range or digit-comma number in a dose or duration cell is omitted rather than reduced to a bound; a cycle phrase in a duration cell is omitted.
- A dose, frequency, or duration column wins over the same kind of quantity typed into the name cell.
- The values-only fallback keeps all cells to the `XFD` cap and sanitises them; blank cells are omitted.
- Multi-compound lists written with `,`, `/`, `&`, `and` are a documented residual.

## Spec review triage (Spec004ReviewerCorrectness, Spec004ReviewerBlastRadius; rev 2 → rev 3)

| Finding | Ruling |
|---|---|
| Cor-F1 / BR-F5 — multiple columns of one role; name = row index | fix — priority rule; name must contain a letter; T12/T13 |
| Cor-F2 / BR-F9 — unsanitised mapped cells re-split by parser, blend parenthetical, `A + B` | fix — sanitisation, phrase-only reduction, multi-compound ruling; T14/T15 |
| Cor-F3 / BR-F8 — dense padding from `r` is unbounded memory | fix — sparse storage, XFD cap, malformed/duplicate `r` rules, T17, stop condition |
| Cor-F4 / BR-F7 — CSV row-break/blank-row/unterminated-quote/delimiter behavior; spurious warnings | fix — explicit CSV rules; blank rows dropped; T10/T20 |
| Cor-F5 / BR-F6 — bare-number dose written as-is drops the row; unvalidated unit hint | fix — omit unitless bare number; unit must be a `DosePattern` unit; T7 |
| Cor-F6 / BR-F4 — header matcher ambiguous; `Compound Name` | fix — exact match defined; consequence stated and flagged; T8 |
| Cor-F7 — analysis outputs change, not only preview | fix — Intent/Constraints/AC7 |
| Cor-F8 / BR-F13 — warning aggregation and format | fix — one aggregated entry, fixed format, T9 |
| Cor-F9 — `inlineStr` between specs | fix — in scope; T19 |
| Cor-F10 / BR-F1 / BR-F2 / BR-F12 — tests vacuous or under-specified (T3 header, T5 shift, T6 BOM/quote, T11) | fix — fixtures redesigned; T5 uses `Route` column; T6 split; T11 asserts dose/frequency |
| Cor-F11 — AC5 wording | fix |
| Cor-F12 — DosePattern unit list inaccurate | fix |
| Cor-F13 — value-level residual leaks; section labels | accept-as-documented — Out of Scope |
| BR-F3 — rows wider than header | fix — ignored; T16 |
| BR-F10 — three assertions edited; AC3 filter | fix |
| BR-F11 — alias fixtures need stop rule | fix — five known aliases; stop condition |
| BR-F14 — no proof the cache bump invalidates | fix — T22 behavioral, fallback to diff-review |
| BR-F15 — `LinkProtocolExtractor` consumer; line shift; adjacent constants; headerless first row | fix/accept — consumer stated; member-name anchoring; order 003 → 004 required; first-row loss kept out of scope |

### Closure round (rev 3 → rev 4): Spec004ReviewerCorrectness `FAIL` (1 blocking), Spec004ReviewerBlastRadius `PASS` (0 blocking)

| Finding | Ruling |
|---|---|
| Cor-N3 blocking / BR-N1 — dose "normalised to `<number> <unit>`" contradicts AC2 `500mcg` | fix — dose is emitted verbatim (`500mcg`, `5 mg`); only bare-number+unit is built as `<number> <unit>`; T27 |
| Cor-N2 — quantity typed into the name cell overrides the Dose column | fix — column wins: matching phrases removed from the name when a column yields a value; T24 |
| Cor-N1 — dose/duration ranges reduce to the upper bound; `1,000` misread | fix — ranges and digit-comma numbers are omitted; T25; European decimals documented as residual |
| Cor-N4 / BR-N4 — unit-cell validation and fall-through | fix — both sources must fully match a unit; order stated; T7 cases |
| Cor-N5 — cycle phrase reduced to a plain duration | fix — `CyclePattern` copy; duration omitted; T26 |
| Cor-N6 / BR-N7 — comma/slash/ampersand/`and` multi-compound lists | accept-as-documented — Out of Scope residual |
| Cor-N7 / BR-N8 — AC1 red/green list wrong (T14 is a lock; T4/T22/T6a red), T17 not concrete, T6a headerless only, T7 alias fixtures | fix — AC1 rewritten; T17 concrete observable; T6c added; T7 alias note |
| Cor-N8 / BR-N3 — fallback vs header-width contradiction; values-only sanitisation; text after closing quote | fix — header-width limit only for reconstructable tables; fallback keeps all cells to XFD; sanitisation covers fallback; literal-append rule |
| BR-N2 — T15 non-discriminating; blend only for `blend`-named rows | fix — frequency assertion added; blend-named fixture; builder records red/green |
| BR-N5 — embedded frequency in a dose cell lost | accept-as-documented — Out of Scope residual |
| BR-N6 — Blend fact wrong | fix — corrected |
| BR-N9 — T17 allocation not checkable | fix — time bound + valid-row binding |
| BR-N11 — Decision 1 overstated for title-row sheets | fix — qualified in Out of Scope |

### Closure round 2 (rev 4 → rev 5): Spec004ReviewerCorrectness `FAIL` (1 blocking)

| Finding | Ruling |
|---|---|
| M1 blocking — range with units on both sides (`250mcg-500mcg`) emits the lower bound | fix — range test allows an optional unit before the separator; fail-safe over-omission stated as intended; T25 fixtures |
| M2 — embedded-quantity removal leaves `()` / dangling separators; unknown compound dropped | fix — cleanup rule; T28 |
| M3 — omitted-but-non-empty dose cell lets a name-embedded strength become the dose | fix — any non-empty cell of that role suppresses the name-embedded quantity; T28 |
| M4 — AC1 lists wrong (T8 red); T25/T26 vacuous without positive controls | fix — AC1 corrected; positive-control rows required in T25/T26 |

### Closure round 3 (rev 5): Spec004ReviewerCorrectness `PASS` (0 blocking); minors accepted as documented residuals

| Finding | Ruling |
|---|---|
| R1 — duration ranges with duration units on both sides (`4 weeks - 6 weeks`) report the lower bound | accept-as-documented — duration does not drive dose-safety checks; listed in Out of Scope |
| R2 — Name bullet: embedded phrases in multi-compound parts vs "no dose bound" | accept — embedded phrases are kept as for pasted text (today's behavior); clarified in Out of Scope |
| R3 — cleanup leaves partial residue such as `(/mL)` | accept-as-documented — junk name suffix for unknown compounds only; aliases unaffected |

## Context & References

- `docs/goals/protocol-upload-graceful-failure/HANDOFF.md` (Residual B; item 5 "table-row dose recovery").
- `docs/development/ANALYZER_CONFIDENCE_AND_UI_PLAN.md` §6 (row-aware table reconstruction listed as a larger parser change, deferred).
- `docs/specs/active/BIO-ANALYZER-001-upload-table-label-leak.md` (closed label set; T1/T2/T3 regression locks; out-of-scope statement for header words and dose recovery).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionServiceTests.cs` (`CreateService`, `CreateMinimalXlsx`, `AddEntry`, current shape assertions).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs` (CSV/XLSX → parser harness; XLSX builder ≈ 344-409).
- `backend/src/BioStack.Application/Services/ProtocolAnalyzerService.cs` (preview, warnings, scoring consumers).
- Sequencing: 002 → 003 → 004.
