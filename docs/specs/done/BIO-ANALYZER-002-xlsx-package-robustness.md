---
ticket: BIO-ANALYZER-002
title: XLSX upload — malformed or absolute-target relationships must yield a friendly 400, never a 500 or a false rejection
status: active
revision: 3
owner: clinton.morgan
created: 2026-10-04
updated: 2026-10-04
supersedes: null
superseded_by: null
risk: low
delivery_classes: [standard]
guidance_classes: [not-applicable]
substance_function_risk: [not-applicable]
function_review_status: not-applicable
surfaces:
  - backend/src/BioStack.Application/Services/ProtocolIngestionService.cs
  - backend/tests/BioStack.Application.Tests/Services/SpreadsheetProtocolExtractorPackageTests.cs
  - docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-002.md
routing_class: implementation/standard
verification_class: equivalence-provable
permission_profile: reviewer-readonly
data_classification: internal
---

# BIO-ANALYZER-002 — XLSX package robustness (Residual C)

## Intent

Close the "known gap, not fixed" recorded as Residual C in `docs/goals/protocol-upload-graceful-failure/HANDOFF.md`, and the sibling defect found while shaping it. `SpreadsheetProtocolExtractor` resolves worksheet parts from `xl/_rels/workbook.xml.rels` in a way that (a) throws an unhandled `ArgumentException` (→ HTTP 500) when two relationships share an `Id`, and (b) mis-resolves an absolute relationship `Target` (`/xl/worksheets/sheet1.xml` → `xl/xl/worksheets/sheet1.xml`), so a structurally valid workbook is rejected with a 400 whose message names an internal archive path. After this parcel, every package-structure fault in an uploaded XLSX maps to `ProtocolIngestionException` with a friendly message, and a valid workbook with absolute or `..`-relative targets parses. Consumers: the uploader (honest bounded result), and guarantee 1 of the P01-R charter (ordinary uploads fail gracefully).

## Prerequisites

- `BIO-ANALYZER-001` merged to the comparison base. This is an ordering prerequisite only: this parcel has no code dependency on its changes and does not edit its tests.

## Verified facts (coordinator + independent reviewer, branch `fix/protocol-upload-graceful-failure` @ `a12d580d`)

- `ProtocolIngestionService.cs:502-509`: the relationship map is built over **all** `Relationship` elements that have both `Id` and `Target`: `ToDictionary(Id, "xl/" + Target.Replace('\\','/').TrimStart('/'), StringComparer.OrdinalIgnoreCase)`. `ToDictionary` throws `ArgumentException` on a duplicate key; the comparer is case-insensitive, so Ids differing only by case also collide. Worksheet lookup happens later, per `sheet` element, at lines 513-521.
- The enclosing `try` (lines 494-552) catches only `ProtocolIngestionException`, `InvalidDataException`, `XmlException`. `ArgumentException` escapes; `AnalyzeEndpoints` maps any non-ingestion `Exception` to `Results.Problem` (500).
- `TrimStart('/')` followed by an unconditional `xl/` prefix makes every absolute target resolve one directory too deep. `ZipArchive.GetEntry` performs no path normalisation and is ordinal case-sensitive, so `xl/../xl/worksheets/sheet1.xml` and `xl/xl/...` both resolve to "missing". ECMA-376 Part 2 (OPC) permits absolute part-name targets for internal relationships. [INFERENCE] Some XLSX writers emit absolute targets; not verified against a specific generator (no Python/openpyxl available). The defect is real from the code regardless.
- `LoadXml` (lines 555-562) throws `ProtocolIngestionException($"The spreadsheet is missing {path}.")`, embedding the internal archive path in a user-facing message. `LoadXml("xl/workbook.xml")` runs first (line 499), so a workbook with no `workbook.xml` never reaches the rels load (line 500).
- Not currently tested: any corrupt-zip or missing-part XLSX case in `ProtocolIngestionServiceTests` (it has only `Contains` assertions on valid workbooks, lines 76-80). `ProtocolUploadGracefulFailureTests.cs` (BIO-ANALYZER-001, lines ≈ 344-409) contains a parameterised XLSX builder the new tests MAY copy; that file MUST NOT be edited.

## Constraints

- Only package-structure handling in `SpreadsheetProtocolExtractor` changes. For a valid workbook with relative targets the emitted `ExtractedText` MUST be byte-identical before and after (this parcel does not touch `ConvertDelimitedRowsToText` or cell reading — `BIO-ANALYZER-004` owns those). The constraint is enforced by a golden captured in the red run (see T3). Line breaks in `ExtractedText` come from `AppendLine`/`Environment.NewLine` and differ by OS (`\r\n` on Windows, `\n` on Linux CI), so the golden MUST be compared after normalising `\r\n` to `\n` on both sides (or be built with `Environment.NewLine`); it is exact in every other respect.
- No change to `ProtocolIngestionException`, `AnalyzeEndpoints`, `IProtocolTextExtractor`, response contracts, or the feature gate.
- All user-facing messages produced on these paths MUST contain none of: `Exception`, `   at `, `BioStack.`, an archive part path (`xl/`, `.xml`), or a `.rels` name.
- **Resolver scope (lazy).** Target resolution and its resolution faults (steps 3-4 below) run **only** for the relationship a `sheet` element references via `r:id`. A relationship that no sheet references is never resolved and never faults on resolution (including ones with odd, external, or escaping targets), preserving today's behavior; the duplicate-`Id` check below still covers all relationships. A `sheet` whose `r:id` is absent from the map is skipped as today.
- **Resolver algorithm** (developer-ratified 2026-10-04 for absolute and `..` handling), applied to the referenced relationship's `Target`:
  1. Replace `\` with `/`.
  2. If the result starts with `/`: base is the package root and all leading `/` are removed. Otherwise: base is `xl/` (the directory of `xl/workbook.xml`).
  3. Split into segments; drop empty and `.` segments; a `..` segment removes the previous segment; a `..` with nothing left to remove (escape above the package root) is a structural fault. This applies identically to the absolute and relative branches.
  4. An empty result (no segments after removing the base — e.g. `Target="/"` or a `..` that collapses to the root) is a structural fault. A relative `Target=""` resolves to the base directory `xl`, which `GetEntry` does not find; that is a friendly missing-part error under step 5, not a step-4 fault. Both outcomes are friendly 400s.
  5. Look the result up with the existing ordinal, case-sensitive `GetEntry`. Case-insensitive part-name matching, percent-decoding, and `#fragment`/`?query` handling are out of scope and unchanged (a target needing them is a friendly missing-part 400).
- Duplicate relationship `Id` (over all relationships with `Id` and `Target`, case-insensitive) is a structural fault → friendly `ProtocolIngestionException` (reuse the existing wording `The spreadsheet contains malformed content and could not be read.`). It MUST NOT silently pick a winner. This is eager, and is a 500→400 change only.
- Tests are written first; red run captured before the change.

## Change

### C1 — Relationship map without `ToDictionary` throw

Build the Id→raw-`Target` map with an explicit loop that detects a duplicate Id and raises `ProtocolIngestionException` with the malformed-content message. Keep `StringComparer.OrdinalIgnoreCase`. Store the raw target; resolution happens lazily (C2).

### C2 — Correct target resolution

Add a private static resolver implementing the Constraints algorithm, called from the per-sheet loop only for the referenced relationship. A relationship with a missing `Target` or `Id` continues to be ignored.

### C3 — Friendly missing-part message

`LoadXml`: replace `The spreadsheet is missing {path}.` with a message that does not echo the path, e.g. `The spreadsheet is missing required content and could not be read.` Exact wording is the builder's choice within the Constraints; tests assert the Constraints properties, not the wording.

### C4 — Tests (new class `SpreadsheetProtocolExtractorPackageTests`, Application.Tests)

Real `ProtocolIngestionService` + `SpreadsheetProtocolExtractor`, in-memory XLSX built with the `ZipArchive` pattern of `ProtocolIngestionServiceTests.CreateMinimalXlsx`/`AddEntry` (copy locally; do not edit those files).

| # | Intent | Expectation |
|---|---|---|
| T1 | Two relationships with the same `Id` | `ProtocolIngestionException`, message satisfies the Constraints message rule; NOT `ArgumentException` |
| T2 | Two `Id`s differing only by case (`rId1`/`RID1`) | same as T1 |
| T3 | Valid workbook with **relative** `Target="worksheets/sheet1.xml"` and non-empty content (e.g. `Compound`/`BPC-157` rows) — golden. Same workbook with **absolute** `Target="/xl/worksheets/sheet1.xml"` | Both ingest successfully. `ExtractedText` of both equals one golden literal captured in the red run, compared after `\r\n`→`\n` normalisation (pre-change the relative case already produces it; the absolute case is red). |
| T4 | Relative target using `../xl/worksheets/sheet1.xml`, and an absolute target with `..` (`/xl/../xl/worksheets/sheet1.xml`) | both succeed with the T3 golden `ExtractedText` (red pre-change: expected deterministic, `GetEntry` does not normalise) |
| T5 | A sheet references a relationship whose Target escapes the root (`../../evil.xml`); the archive ALSO contains a root-level `evil.xml` holding a valid worksheet | friendly `ProtocolIngestionException` satisfying the message rule; no content from `evil.xml` emitted (red pre-change on the message rule, since the old message echoes `xl/…xml`) |
| T6 | Sheet relationship points at a part that does not exist | friendly `ProtocolIngestionException`; message satisfies the message rule (no `xl/`, no `.xml`) |
| T7a | `xl/workbook.xml` absent | friendly `ProtocolIngestionException`; message rule |
| T7b | `xl/workbook.xml` present, `xl/_rels/workbook.xml.rels` absent | friendly `ProtocolIngestionException`; message rule |
| T8 | Workbook contains an **unreferenced** relationship with an escaping or empty `Target` (e.g. `../../x`, or `TargetMode="External"` with an http URL) alongside a valid referenced sheet | ingestion succeeds with the T3 golden (locks the lazy-resolution constraint; green both before and after) |

## Acceptance Criteria

1. AC1 — Before C1-C3, T1, T2, the absolute-target half of T3, T4, T5, T6, T7a, T7b fail (T6/T7a/T7b/T5 on the message rule; T1/T2 on exception type); output names the observed exception type or message. T8 and the relative-target half of T3 pass before and after. After, all pass. Red and green runs saved as evidence.
2. AC2 — Existing `ProtocolIngestionServiceTests` XLSX cases and the `Protocol|PdfProtocol` filter stay green (count ≥ the baseline the builder records at start; zero failures).
3. AC3 — No exception other than `ProtocolIngestionException` escapes `IngestAsync` for T1, T2, T5, T6, T7a, T7b (assert the type exactly).
4. AC4 — `git diff` touches only Allowed Files; no contract/signature change.
5. AC5 — Nothing pushed, no PR, branch local only.
6. AC6 — If the pathological-package probes or any additional probe the builder tries (e.g. relationship element without `Id`, sheet element without `r:id`, `Target=""`) surface a further non-`ProtocolIngestionException` escape, the builder records it in `EVIDENCE-BIO-ANALYZER-002.md` and stops; it does not widen the fix.

## Out of Scope

- Cell reading, column alignment, header handling, `ConvertDelimitedRowsToText`, CSV parsing, `t="inlineStr"` — all `BIO-ANALYZER-004`.
- Zip-bomb / decompressed-size limits, encrypted packages, macro-enabled workbooks, `.xls`/`.ods`.
- Case-insensitive part-name matching, percent-encoded or fragment/query targets (see Constraints step 5).
- Skipping a single bad sheet while keeping the others (any bad referenced sheet still fails the upload with a friendly 400, as today).
- Endpoint-level test: `ProtocolIngestionException` → 400 is already locked by BIO-ANALYZER-001 T8; this parcel asserts at the ingestion layer.
- Frontend, response contracts, feature gate, billing, push/PR/merge/deploy, any production-readiness verdict.

## Allowed Files

- `backend/src/BioStack.Application/Services/ProtocolIngestionService.cs`
- `backend/tests/BioStack.Application.Tests/Services/SpreadsheetProtocolExtractorPackageTests.cs` (new)
- `docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-002.md` (new; builder writes red/green outputs)

Coordinator-owned (builder must not edit): this spec, `docs/specs/INDEX.md`, `HANDOFF.md`, `EVIDENCE.md`, `CHARTER.md`, `loop-directive.md`, Gate 2 record.

## Forbidden

- Any other file; `ConvertDelimitedRowsToText`; `ProtocolParser.cs`; `ProtocolFingerprintService.cs` (no cache-version bump is needed: failures are not cached and valid-text output is unchanged); mocks of the extractor; editing `ProtocolUploadGracefulFailureTests` or `ProtocolIngestionServiceTests`; deleting or weakening existing tests; push/PR/merge/deploy.

## Verification (run from `backend/` in the Gate-2-named worktree)

1. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~SpreadsheetProtocolExtractorPackageTests"` — red before C1-C3, green after.
2. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol|FullyQualifiedName~SpreadsheetProtocolExtractorPackageTests"` — green.
3. `git diff --check`; `git diff --stat` lists only Allowed Files.

## Stop-and-Report Rule

Builder stops and returns to the coordinator if: fixing the tests requires a file outside Allowed Files; the byte-identical `ExtractedText` constraint cannot be met; any existing test breaks; AC6 fires; the resolver algorithm contradicts the output of a real generator the builder can construct.

## Rollback

Revert the commit. No data, config, migration, or cache impact (no cache-version change).

## Ratified Decisions (developer, 2026-10-04)

1. Duplicate relationship `Id` is rejected with a friendly 400; first-wins is not used. Reason: OPC forbids duplicate Ids and guessing a winner could parse the wrong sheet.
2. `..` segments are resolved; only an escape above the package root is rejected.

## Coordinator rulings from spec review (flagged for developer awareness; refine, do not contradict, the decisions above)

- Resolution is lazy (referenced relationships only), so unreferenced odd relationships never newly fail an upload that parses today.
- Case-insensitive part-name matching is not added.

## Spec review triage (Spec002Reviewer, rev 2 → rev 3)

| Finding | Ruling |
|---|---|
| F1 blocking — resolver eager vs lazy undecided | fix — lazy, referenced rels only; T8 locks it |
| F2 — T5 not discriminating; "no read outside archive" untestable | fix — T5 gets root-level `evil.xml`, message-rule assertion, red list; wording removed |
| F3 — byte-identical constraint had no golden | fix — T3/T4 golden `ExtractedText` literal captured in red run; closure review N1: compare after `\r\n`→`\n` normalisation (Windows dev vs Linux CI) |
| F4 — resolver underspecified (`..` in absolute, leading `\`, `//`, empty, case) | fix — ordered algorithm in Constraints; case/percent/fragment explicitly out of scope; closure review N2/N3: scope bullet reworded, step 4 vs `Target=""` clarified |
| F5 — AC6 named `EVIDENCE.md`; surfaces omitted evidence file | fix |
| F6 — T7 second half unreachable | fix — T7a/T7b |
| F7 — T4 "if it reproduces" | fix — stated deterministic red |
| F8 — "corrupt-zip already tested" fact wrong | fix — fact corrected; builder helper in 001 tests noted |

## Context & References

- `docs/goals/protocol-upload-graceful-failure/HANDOFF.md` (Residual C; review finding "XLSX with duplicate relationship `Id`… absolute rel `Target`").
- `docs/specs/active/BIO-ANALYZER-001-upload-table-label-leak.md` (T8 endpoint mapping; message-rule style from T7).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionServiceTests.cs` (`CreateService`, `CreateMinimalXlsx`, `AddEntry`).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs` (≈ lines 344-409, parameterised XLSX builder; read-only).
- `backend/src/BioStack.Api/Endpoints/AnalyzeEndpoints.cs` (exception → HTTP mapping; read-only here).
- Sequencing: independent of `BIO-ANALYZER-003`; must merge before `BIO-ANALYZER-004` (same class, same file). Interaction: the golden `ExtractedText` literal pinned by T3/T4/T8 uses the old `Header: value` shape; `BIO-ANALYZER-004` legitimately changes that shape and its Allowed Files permit updating only those golden literals in this test class. This parcel's guarantee (relative vs absolute/`..` equality, no change for valid workbooks) is independent of the literal's content.
