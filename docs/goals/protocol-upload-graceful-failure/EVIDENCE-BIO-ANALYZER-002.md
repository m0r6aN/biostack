# EVIDENCE — BIO-ANALYZER-002 (XLSX package robustness)

Builder: BioAnalyzer002Builder. Branch `fix/analyzer-xlsx-package-robustness`, start commit `426b33ae`. Spec rev 3 (sha256 `D80022D7…5B34`, matches Gate 2).

## Step 0 restatement

- Change: `SpreadsheetProtocolExtractor` in `ProtocolIngestionService.cs` only — C1 explicit relationship loop (duplicate Id, case-insensitive → malformed-content `ProtocolIngestionException`, raw target stored); C2 lazy `ResolveWorkbookTarget` per referenced sheet (spec algorithm steps 1-5); C3 `LoadXml` missing-part message without path. C4 new `SpreadsheetProtocolExtractorPackageTests` (T1–T8 incl. T7a/T7b).
- Allowed files: the three in the spec. Forbidden: everything else (cell reading, `ConvertDelimitedRowsToText`, other tests).
- Stop triggers: file outside Allowed; golden not met; existing test breaks; AC6 escape; resolver contradicts real generator.
- Spec consistent; no design guess required.

## Golden (captured on unmodified code, relative target, after `\r\n`→`\n`)

`"Sheet: Stack\nCompound: BPC-157 | Dose: 500mcg | Frequency: daily"`

## RED (before C1–C3; tests only)

Command (from `backend/`): `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~SpreadsheetProtocolExtractorPackageTests"`

Result: `Failed: 9, Passed: 4, Skipped: 0, Total: 13`.
- Failed: T1 (`ArgumentException … Key: rId1`), T2 (`ArgumentException … Key: RID1`), T3-absolute (`missing xl/xl/worksheets/sheet1.xml`), T4 ×2 (`missing xl/../xl/worksheets/sheet1.xml`, `missing xl/xl/../xl/worksheets/sheet1.xml`), T5 (message contains `xl/`: `missing xl/../../evil.xml`), T6 (`xl/worksheets/missing…`), T7a (`xl/workbook.xml`), T7b (`xl/_rels/workbook.xml…`).
- Passed: T3-relative, T8 ×3 (pass before and after, as AC1 requires).

## GREEN (after C1–C3)

1. `…--filter "FullyQualifiedName~SpreadsheetProtocolExtractorPackageTests"` → `Passed: 13, Failed: 0, Skipped: 0`.
2. `…--filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol|FullyQualifiedName~SpreadsheetProtocolExtractorPackageTests"` → `Passed: 431, Failed: 0, Skipped: 0` (431 − 13 new = 418 pre-existing matching tests).
3. Full `BioStack.Application.Tests` → `Passed: 875, Failed: 0, Skipped: 5, Total: 880` (baseline 862 + 13 new).
4. `git diff --check` → clean.

## AC status

AC1 met (red list above; green all pass). AC2 met (existing XLSX test and filter green, 0 failures). AC3 met (`AssertIngestionFailure` asserts exact `ProtocolIngestionException` type for T1, T2, T5, T6, T7a, T7b). AC4 met (diff limited to the three Allowed Files; no signature change). AC5 met (local commit only). AC6: no additional escape observed; T8 covers an unreferenced empty/escaping/external target, other probes not separately run.

## Deviation note

One edit to `ProtocolIngestionService.cs` was initially applied by a relative path to the main checkout (`D:/Repos/BioStack/backend/...`) rather than the worktree. It was detected immediately, the main-checkout diff was confirmed to be solely that edit and reverted with `git checkout -- <file>`, leaving the main checkout clean; the change was then applied in the worktree.
