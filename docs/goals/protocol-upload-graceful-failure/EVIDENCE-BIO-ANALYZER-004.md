# EVIDENCE — BIO-ANALYZER-004 (spreadsheet row reconstruction)

Builder: BioAnalyzer004Builder. Branch `fix/analyzer-spreadsheet-row-reconstruction`, start commit `65e2242b`.
Spec: `docs/specs/active/BIO-ANALYZER-004-spreadsheet-row-reconstruction.md` rev 6.
Spec SHA-256 (`certutil`): `3ec9c94bc116da48eda5753a4e49462eb12a4a392579ac0c70f48b0421451d3c` = Gate 2 `specSha256` (case-insensitive match).

## Step 0 — Restatement and consistency check

**Change.**
- C0 `ProtocolFingerprintService.IngestionVersion` `"v1"` -> `"v2"` (only change in that file).
- C1 `SpreadsheetProtocolExtractor`: XLSX cell placement by `r` (sparse, XFD cap, malformed/duplicate rules), `t="inlineStr"`; RFC 4180 CSV reader with BOM strip, unterminated quote -> `ProtocolIngestionException("The spreadsheet contains malformed content and could not be read.")`, blank rows dropped for CSV and XLSX.
- C2 header-role mapping (exact match, closed vocabulary), per-row reconstruction (`<name> <dose> <frequency> <duration>`), values-only fallback, aggregated skipped-row warning threaded to `ProtocolExtractionResult.Warnings`; private copies of the dose/frequency/duration/cycle patterns in the extractor.
- C3 new `SpreadsheetRowReconstructionTests` (T1-T28); update ONLY the three named assertions in `ProtocolIngestionServiceTests` and ONLY the golden `ExtractedText` literal in `SpreadsheetProtocolExtractorPackageTests`.

**Allowed files.** `ProtocolIngestionService.cs`, `ProtocolFingerprintService.cs`, `SpreadsheetRowReconstructionTests.cs` (new), `ProtocolIngestionServiceTests.cs` (three assertions), `SpreadsheetProtocolExtractorPackageTests.cs` (golden literal), this evidence file.

**Forbidden.** Anything else; `ProtocolParser.cs`; `IProtocolParser`; contracts; `AnalyzeEndpoints`; other extractors; feature gates; `ParserVersion`/`ScoringVersion`; extending the role vocabulary; appending unmapped columns to a parsed line; editing `ProtocolUploadGracefulFailureTests`; weakening existing tests beyond the named ones; mocks of parser/extractors; push/PR/merge/deploy.

**Stop triggers.** An existing test (other than the three assertions and 002 golden) breaks; `ProtocolUploadGracefulFailureTests` T1/T2 need changes; warning cannot be threaded without changing `ProtocolExtractionResult`'s public shape; a fixture needs a role outside the vocabulary or an alias outside the five in `LocalKnowledgeSource`; T17 shows allocation/time growth tied to column index; diff exceeds Allowed Files.

**Consistency check of the spec.** No contradiction found that requires a design guess. Interpretations I had to fix (all derivable from spec text, recorded for the reviewer):
1. "first with a non-empty sanitised value" for dose: the first non-empty dose-role cell (priority order) decides; if it is omitted by a rule the dose is omitted (no fall-through to a lower-priority dose column). Duration: literal reading — the first duration column whose value has a `DurationPattern` match and neither a range nor a cycle phrase.
2. Header width = highest non-blank header column index + 1 (style-only blank header cells do not widen it). Data cells beyond it are read for blankness (a row whose only content is past the header is *not* an all-blank row, so it is counted as a skipped no-name row) but are never stored.
3. Past-XFD cells are dropped; for "previous cell's index + 1" placement the dropped cell still advances the running index.

## Baselines (before any change, from `<worktree>/backend`)

| Command | Result |
|---|---|
| `dotnet test tests/BioStack.Application.Tests` | Passed 895, Skipped 5, Failed 0, Total 900 |
| `dotnet test tests/BioStack.Application.Tests --no-build --filter "FullyQualifiedName~Protocol\|FullyQualifiedName~PdfProtocol"` | Passed 451 (= 438 + 13, includes 002/003 tests) |
| `dotnet test tests/BioStack.Application.Tests --no-build --filter "FullyQualifiedName~ProtocolUploadGracefulFailureTests"` | Passed 160 |

## RED run (tests written first; production code at start commit `65e2242b`)

Production files `ProtocolIngestionService.cs` and `ProtocolFingerprintService.cs` were restored to `HEAD` for this run (copied aside, `git show HEAD:<path>`, restored afterwards); only the new test class existed.

```
cd backend
dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~SpreadsheetRowReconstructionTests"
Failed!  - Failed:    52, Passed:     6, Skipped:     0, Total:    58, Duration: 207 ms
```

(A first RED attempt had a defective BOM assertion — `Assert.DoesNotContain("\uFEFF", …)` uses culture comparison and matched any string — so it was replaced with `Contains('\uFEFF')` and RED was re-captured; the table below is from the corrected run.)

## GREEN run (C0-C2 implemented)

```
dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~SpreadsheetRowReconstructionTests"
Passed!  - Failed:     0, Passed:    58, Skipped:     0, Total:    58
```

## Per-test red / green (AC1)

Rows are xUnit test cases (theory rows listed separately). "Failure observed in RED" is the first line(s) of the assertion message and names the leaked header/cell, shifted value or dose 0 (text after `Text:` is the pre-change extracted text).

| Test | RED (before) | GREEN (after) | Failure observed in RED |
|---|---|---|---|
| T1_UnmappedHeaderColumns_NeverBecomeCompounds | **RED** | green | Expected exactly {BPC-157, Retatrutide}. Leaked: [Vial Size] Missing: []. Parsed: BPC-157 [0  ] \| Vial Size [10mg  ] \| Retatrutide [0  ]. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 4 weeks  |
| T10_BlankXlsxRows_AreNotCountedOrEmitted | green | green |  |
| T10_TrailingBlankCsvRows_AreNotCountedOrEmitted | green | green |  |
| T10b_BlankCsvRows_AreNotEmittedInText | **RED** | green | Assert.Equal() Failure: Strings differ / ↓ (pos 11) / Expected: "Sheet: CSV\nBPC-157 500mcg daily" / Actual:   "Sheet: CSV\nCompound: BPC-157 \| Dose: 500m"··· |
| T11_HeaderCaseVariants_MapIdentically(header: "Compound,Dose,Frequency") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\| but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily |
| T11_HeaderCaseVariants_MapIdentically(header: "COMPOUND,DOSE,FREQUENCY") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\| but was 0\|\|\|. Text: Sheet: CSV // COMPOUND: BPC-157 \| DOSE: 500mcg \| FREQUENCY: daily |
| T12_DuplicateDoseRoles_FollowPriority | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 250\|mcg\|\| but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Strength: 5mg \| Dose: 250mcg |
| T13_NameColumnWithoutLetter_FallsThroughToNextNameColumn | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|\| but was 0\|\|\|. Text: Sheet: CSV // Item: 1 \| Compound: BPC-157 \| Dose: 500mcg |
| T14_MultiCompoundNameCell_BindsNoDose | green | green |  |
| T15a_LineBreakInNameCell_DoesNotFragmentTheRow | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|\| but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 // Compound: (Wolverine) \| Dose: 500mcg |
| T15b_MultipleDosesInOneCell_KeepFirstAndDoNotSwallowFrequency(doseCell: "\"250mcg, 500mcg\"") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 250\|mcg\|daily\| but was 0\|\|\|. Text: Sheet: CSV // BPC-157 \| 250mcg \| 500mcg \| daily |
| T15b_MultipleDosesInOneCell_KeepFirstAndDoNotSwallowFrequency(doseCell: "\"250mcg; 500mcg\"") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 250\|mcg\|daily\| but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 250mcg; 500mcg \| Frequency: daily |
| T15c_BlendNamedRow_ParentheticalFrequencyDoesNotCreateComponent | **RED** | green | Expected exactly {Wolverine Blend}. Leaked: [] Missing: [Wolverine Blend]. Parsed: . Text: Sheet: CSV // Compound: Wolverine Blend \| Dose: 500mcg \| Frequency: daily (AM) |
| T16a_CsvRowsWiderThanHeader_IgnoreExtraCells | **RED** | green | Expected exactly {BPC-157, TB-500}. Leaked: [Junk] Missing: []. Parsed: BPC-157 [0  ] \| TB-500 [0  ] \| Junk [9mg weekly ]. Text: Sheet: CSV // BPC-157 \| 500mcg \| daily \| // TB-500 \| 2mg \| weekly \| Junk 9mg weekly |
| T16b_XlsxCellPastHeaderWidth_IsIgnored | **RED** | green | Expected exactly {BPC-157}. Leaked: [Junk] Missing: []. Parsed: BPC-157 [0  ] \| Junk [9mg weekly ]. Text: Sheet: Stack // BPC-157 \| 500mcg \| daily \| Junk 9mg weekly |
| T17_HostileCellReferences_AreBoundedAndDoNotBreakTheValidRow | **RED** | green | Expected exactly {BPC-157}. Leaked: [] Missing: [BPC-157]. Parsed: . Text: Sheet: Stack |
| T17b_ValuesOnlyFallback_KeepsCellsUpToXfdAndDropsPastIt | **RED** | green | Assert.Equal() Failure: Strings differ / ↓ (pos 12) / Expected: "Sheet: Stack\nBPC-157 \| FarEdge" / Actual:   "Sheet: Stack" |
| T18_SparseColumnsPastZ_BindCorrectly | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 0\|\|daily\| but was 0\|\|\|. Text: Sheet: Stack // Compound: BPC-157 \| Filler B: daily |
| T19_InlineStrings_ReconstructLikeSharedStrings | **RED** | green | BioStack.Application.Services.ProtocolIngestionException : We could not extract readable protocol text from that source. |
| T2_DoseBindsToTheRowsCompound | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|4 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 4 weeks \| Vial Size: 5mg \| Start Date: 2026-10-01 \| Su |
| T20a_RowBreakStyles_GiveIdenticalResults | **RED** | green | Assert.Equal() Failure: Strings differ / ↓ (pos 11) / Expected: "Sheet: CSV\nBPC-157 500mcg daily\nTB-500 2m"··· / Actual:   "Sheet: CSV\nCompound: BPC-157 \| Dose: 500m"··· |
| T20b_UnterminatedQuote_IsAFriendlyIngestionException | **RED** | green | Assert.Throws() Failure: No exception was thrown / Expected: typeof(BioStack.Application.Services.ProtocolIngestionException) |
| T20c_SemicolonDelimitedCsv_DoesNotThrowAndFallsBackToValuesOnly | **RED** | green | Assert.DoesNotContain() Failure: Sub-string found / ↓ (pos 11) / String: "Sheet: CSV\nCompound;Dose;Frequency: BPC-1"··· / Found:  "Compound" |
| T20d_QuoteHandling_FollowsTheSpecifiedRules | **RED** | green | Assert.Equal() Failure: Strings differ / ↓ (pos 11) / Expected: "Sheet: CSV\nsay "hi" \| x"y\nabcdef \| z\npadd"··· / Actual:   "Sheet: CSV\nFoo: say ""hi \| Bar: x"y\nFoo: "··· |
| T21_NoHeaderSheet_EmitsValuesOnly | green | green |  |
| T21b_NoHeaderSheet_OmitsBlankCells | **RED** | green | Assert.Equal() Failure: Strings differ / ↓ (pos 15) / Expected: "Sheet: CSV\n1 \| 3\n4 \| 5" / Actual:   "Sheet: CSV\n1 \| \| 3\n4 \| 5 \|" |
| T22_IngestionCacheVersionBump_IgnoresStaleV1Entry | **RED** | green | Assert.NotEqual() Failure: Strings are equal / Expected: Not "analyzer:ingestion:fileupload:v1:393e8569d19e25a0d"··· / Actual:       "analyzer:ingestion:fileupload:v1:393e8569d19e25a0d"··· |
| T24a_DoseColumnWinsOverQuantityTypedIntoName | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 250\|mcg\|daily\| but was 5\|mg\|\|. Text: Sheet: CSV // Compound: BPC-157 5mg \| Dose: 250mcg \| Frequency: daily |
| T24b_FrequencyColumnWinsOverFrequencyTypedIntoName | **RED** | green | Entry 'Retatrutide' expected dose\|unit\|frequency\|duration = 2\|mg\|daily\| but was 0\|\|weekly\|. Text: Sheet: CSV // Compound: Retatrutide weekly \| Dose: 2mg \| Frequency: daily |
| T24c_EmbeddedQuantity_ServesWhenNoDoseColumnHasAValue | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 5\|mg\|daily\| but was 5\|mg\|\|. Text: Sheet: CSV // Compound: BPC-157 5mg \| Frequency: daily |
| T25_DoseRangesAndSeparators_AreOmitted(subjectDose: "\"1,000 mcg\"") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|12 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 12 weeks // TB-500 \| 1 \| 000 mcg \| daily \| 4 weeks |
| T25_DoseRangesAndSeparators_AreOmitted(subjectDose: "0.5 to 1 mg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|12 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 12 weeks // Compound: TB-500 \| Dose: 0.5 to 1 mg \| Fr |
| T25_DoseRangesAndSeparators_AreOmitted(subjectDose: "0.5mg to 1mg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|12 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 12 weeks // Compound: TB-500 \| Dose: 0.5mg to 1mg \| F |
| T25_DoseRangesAndSeparators_AreOmitted(subjectDose: "250 mcg - 500 mcg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|12 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 12 weeks // Compound: TB-500 \| Dose: 250 mcg - 500 mc |
| T25_DoseRangesAndSeparators_AreOmitted(subjectDose: "250-500mcg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|12 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 12 weeks // Compound: TB-500 \| Dose: 250-500mcg \| Fre |
| T25_DoseRangesAndSeparators_AreOmitted(subjectDose: "250mcg-500mcg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|12 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 12 weeks // Compound: TB-500 \| Dose: 250mcg-500mcg \|  |
| T25_DurationRange_IsOmitted | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|12 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 12 weeks // Compound: TB-500 \| Dose: 2mg \| Frequency: |
| T26_CyclePhraseInDuration_IsOmitted | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\|12 weeks but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Frequency: daily \| Duration: 12 weeks // TB-500 \| 2mg \| daily \| 8 weeks on \| 8 wee |
| T27_DoseIsEmittedVerbatim | **RED** | green | Assert.Contains() Failure: Sub-string not found / String:    "Sheet: CSV\nCompound: BPC-157 \| Dose: 500m"··· / Not found: "BPC-157 500mcg daily" |
| T28a_NameCleanup_DoesNotDropUnknownCompound(nameCell: "Zorbatide - 5mg") | **RED** | green | Expected exactly {Zorbatide}. Leaked: [] Missing: [Zorbatide]. Parsed: . Text: Sheet: CSV // Compound: Zorbatide - 5mg \| Dose: 250mcg \| Frequency: weekly |
| T28a_NameCleanup_DoesNotDropUnknownCompound(nameCell: "Zorbatide (5mg)") | **RED** | green | Expected exactly {Zorbatide}. Leaked: [] Missing: [Zorbatide]. Parsed: . Text: Sheet: CSV // Compound: Zorbatide (5mg) \| Dose: 250mcg \| Frequency: weekly |
| T28b_OmittedDoseCell_SuppressesNameEmbeddedDose(doseCell: "250-500mcg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 0\|\|\| but was 5\|mg\|\|. Text: Sheet: CSV // Compound: BPC-157 5mg \| Dose: 250-500mcg |
| T28b_OmittedDoseCell_SuppressesNameEmbeddedDose(doseCell: "TBD") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 0\|\|\| but was 5\|mg\|\|. Text: Sheet: CSV // Compound: BPC-157 5mg \| Dose: TBD |
| T3_UnknownCompound_IsEmittedWithItsDose | **RED** | green | Expected exactly {Zorbatide}. Leaked: [] Missing: [Zorbatide]. Parsed: . Text: Sheet: CSV // Compound: Zorbatide \| Dose: 5mg \| Frequency: weekly |
| T4_UnmappedText_CannotRenameARow | **RED** | green | Expected exactly {BPC-157}. Leaked: [TB-500] Missing: []. Parsed: BPC-157 [0  ] \| TB-500 [0  ]. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 500mcg \| Notes: stack with TB-500 |
| T5_XlsxMissingCell_DoesNotShiftLaterColumns | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 0\|\|daily\| but was 0\|\|\|. Text: Sheet: Stack // Compound: BPC-157 \| Dose: SubQ \| Route: daily |
| T6a_HeaderlessBomCsv_HasNoBomInText | **RED** | green | NormalizedText still contains U+FEFF. |
| T6b_QuotedCellBeforeMappedColumns_KeepsBindings | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 500\|mcg\|daily\| but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Notes: a \| Dose: b // Compound: c \| Notes: 500mcg \| Dose: daily |
| T6c_BomPrefixedHeader_StillMaps | **RED** | green | NormalizedText still contains U+FEFF. |
| T7_UnitHints(csv: "Compound,Dose (mg),Unit\nBPC-157,5,IU", expectedDose: 5, expectedUnit: "mg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 5\|mg\|\| but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose (mg): 5 \| Unit: IU |
| T7_UnitHints(csv: "Compound,Dose (mg)\nBPC-157,5", expectedDose: 5, expectedUnit: "mg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 5\|mg\|\| but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose (mg): 5 |
| T7_UnitHints(csv: "Compound,Dose (per injection)\nBPC-157,5", expectedDose: 0, expectedUnit: "") | green | green |  |
| T7_UnitHints(csv: "Compound,Dose,Unit\nBPC-157,250,mcg", expectedDose: 250, expectedUnit: "mcg") | **RED** | green | Entry 'BPC-157' expected dose\|unit\|frequency\|duration = 250\|mcg\|\| but was 0\|\|\|. Text: Sheet: CSV // Compound: BPC-157 \| Dose: 250 \| Unit: mcg |
| T7_UnitHints(csv: "Compound,Dose,Unit\nBPC-157,5,IU", expectedDose: 0, expectedUnit: "") | green | green |  |
| T8_NonReconstructableTable_FallsBackToValuesOnly(csv: "Compound Name,Dose\nBPC-157,500mcg") | **RED** | green | Assert.Equal() Failure: Strings differ / ↓ (pos 11) / Expected: "Sheet: CSV\nBPC-157 \| 500mcg" / Actual:   "Sheet: CSV\nCompound Name: BPC-157 \| Dose:"··· |
| T8_NonReconstructableTable_FallsBackToValuesOnly(csv: "Foo,Bar\nBPC-157,500mcg") | **RED** | green | Assert.Equal() Failure: Strings differ / ↓ (pos 11) / Expected: "Sheet: CSV\nBPC-157 \| 500mcg" / Actual:   "Sheet: CSV\nFoo: BPC-157 \| Bar: 500mcg" |
| T9_MultiSheetXlsx_AggregatesSkippedRowsAcrossSheets | **RED** | green | Assert.Single() Failure: The collection was empty |
| T9_SkippedRows_AreCountedInOneAggregatedWarning | **RED** | green | Assert.Single() Failure: The collection was empty |

Expected-vs-observed against the AC1 list:
- Expected green-before locks that were green: T10 (warnings/entries; CSV and XLSX), T14, T21, and the two T7 sub-cases whose expected dose is 0 (`Dose (per injection)`, `Unit=IU` without hint). T23 is verification command 2 (160 green before and after, unmodified).
- Deviations from the AC1 forecast (all recorded, none weaken a requirement): (a) the CSV blank-row *text* check was split out of T10 into **T10b** (RED: the old reader emitted `Compound:`-shaped lines for blank rows) so that T10 stays the green lock the spec describes; (b) T21b (blank cells omitted in the values-only fallback) is an additional case and is RED before (the spec's T21 wording "identical except blank cells omitted" is split into the green lock T21 and T21b); (c) T20a/T20c/T20d are RED before (old text shape, header leaked, quote handling) rather than partly green; (d) T15c (blend-named row with `daily (AM)`) was RED before (the whole row was dropped — no entry at all), as recorded per the spec.
- All spec-listed expected-RED tests were RED: T1, T2, T3, T4, T5, T6a, T6b, T6c, T7 (3 of 5 cases; the other 2 are green-before sub-cases), T8, T9 (both), T11, T12, T13, T15a/b/c, T17, T18, T19, T22, T24, T25 (positive control fails), T26, T27, T28.

## Existing tests changed (AC2)

Only these were edited:
- `ProtocolIngestionServiceTests.cs` lines 42, 43, 79: `Contains("Compound: BPC-157")` -> `Contains("BPC-157 500mcg daily")`; `Contains("Dose: 500mcg")` -> `Contains("BPC-157 500mcg")`; `Contains("Compound: BPC-157")` -> `Contains("BPC-157 500mcg daily")`. `Contains("Sheet: Stack")` unchanged.
- `SpreadsheetProtocolExtractorPackageTests.cs` line 18 `GoldenExtractedText`: `"Sheet: Stack\nCompound: BPC-157 | Dose: 500mcg | Frequency: daily"` -> `"Sheet: Stack\nBPC-157 500mcg daily"`. The explanatory comment on line 17 ("captured from the unmodified extractor") was left untouched (spec: nothing else in that file changes), so it is now historical.

Proof that nothing else in the existing suite depended on the old shape: with the *new* production code and the *old* versions of these two test files, `--filter "FullyQualifiedName~ProtocolIngestionServiceTests|FullyQualifiedName~SpreadsheetProtocolExtractorPackageTests"` gave `Failed: 9, Passed: 9` — failing exactly `IngestAsync_CsvUpload_PreservesRowStructure`, `IngestAsync_XlsxUpload_ReadsWorksheetRows` and the seven golden-based 002 tests (T3 x2, T4 x2, T8 x3). No other existing test failed.

## Verification commands (final tree, from `<worktree>/backend`)

| # | Command | Result |
|---|---|---|
| 1 | `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~SpreadsheetRowReconstructionTests"` | RED 52 failed / 6 passed of 58 (before) -> **Passed 58 / 58** (after) |
| 2 | `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~ProtocolUploadGracefulFailureTests"` | **Passed 160 / 160** (before: 160 / 160; file unmodified) |
| 3 | `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol\|FullyQualifiedName~PdfProtocol\|FullyQualifiedName~SpreadsheetRowReconstructionTests"` | **Passed 509 / 509** (baseline 451 + 58 new = 509) |
| 4a | `dotnet test tests/BioStack.Application.Tests` | **Passed 953, Skipped 5, Failed 0, Total 958** (baseline 895 + 5 skipped = 900; +58 new) |
| 4b | `dotnet test tests/BioStack.Api.Tests --filter "FullyQualifiedName~AnalyzeEndpointsIntegrationTests\|FullyQualifiedName~AnalyzerGateIntegrationTests"` | **Passed 6 / 6** |
| 5 | `git diff --check` | exit 0, no output; `git diff --stat` lists only Allowed Files (see final report) |

T22 (behavioural cache invalidation): the harness's `ProtocolAnalysisCache` is seedable, so the test seeds an `IngestionCacheDto` with the old shape at `analyzer:ingestion:fileupload:v1:{fingerprint}`, asserts the current cache key differs from it, and asserts the ingestion returns the fresh reconstructed text. RED before (current key equalled the v1 key), GREEN after (`IngestionVersion = "v2"`). It is not diff-review-only.

T17 (hostile references): ingestion of the hostile sheet completes in well under the 2 s bound; no time/allocation growth tied to the column index was observed (stop trigger not hit). Cells past the header width are never stored, cells past XFD are dropped, and the sparse `Dictionary<int,string>` per row holds only non-blank cells.

## AC7 smoke — before / after (CSV `Compound,Dose,Frequency,Duration`)

Throw-away test (`TempAc7SmokeTests`, real `ProtocolAnalyzerService` with `SpreadsheetProtocolExtractor`, real parser/scoring, `LocalKnowledgeSource`), run once on the start-commit production code ("before") and once on the new code ("after"); the temporary test file was deleted and is not in the commit.

### Fixture A (`Compound,Dose,Frequency,Duration` / `BPC-157,500mcg,daily,4 weeks` / `TB-500,2mg,twice weekly,6 weeks`)

Before:
```
ExtractedTextPreview: Sheet: CSV\nCompound: BPC-157 | Dose: 500mcg | Frequency: daily | Duration: 4 weeks\nCompound: TB-500 | Dose: 2mg | Frequency: twice weekly | Duration: 6 weeks
Protocol: BPC-157 | dose=0 | unit='' | freq='' | dur='' | recognized=True
Protocol: TB-500 | dose=0 | unit='' | freq='' | dur='' | recognized=True
Score=61 Scored=True ParseConfidence=medium Base=50 Syn=11 Red=0 Interf=0
IssueCount=0 ExtractionWarnings=[] UnknownCompounds=[]
```
After:
```
ExtractedTextPreview: Sheet: CSV\nBPC-157 500mcg daily 4 weeks\nTB-500 2mg twice weekly 6 weeks
Protocol: BPC-157 | dose=500 | unit='mcg' | freq='daily' | dur='4 weeks' | recognized=True
Protocol: TB-500 | dose=2 | unit='mg' | freq='twice weekly' | dur='6 weeks' | recognized=True
Score=61 Scored=True ParseConfidence=medium Base=50 Syn=11 Red=0 Interf=0
IssueCount=0 ExtractionWarnings=[] UnknownCompounds=[]
```
Fixture A changes the preview text and the `Protocol[]` doses/units/frequencies/durations. Score (61) and issues (0) are **unchanged** for these two modest doses — no recorded dose breaches an evidence-context range.

### Fixture B (same layout, five compounds with high doses, so dose recovery reaches the issue generator)

CSV: `BPC-157,5000mcg,daily,4 weeks`; `TB-500,50mg,daily,6 weeks`; `Retatrutide,12mg,weekly,12 weeks`; `MOTS-C,10mg,daily,4 weeks`; `NAD+,500mg,daily,4 weeks`.

Before:
```
ExtractedTextPreview: Sheet: CSV\nCompound: BPC-157 | Dose: 5000mcg | Frequency: daily | Duration: 4 weeks\nCompound: TB-500 | Dose: 50mg | Frequency: daily | Duration: 6 weeks\nCompound: Retatrutide | Dose: 12mg | Frequency: weekly | Duration: 12 weeks\nCompound: MOTS-C | Dose: 10mg | Frequency: daily | Duration: 4 weeks\nCompound: NAD+ | Dose: 500mg | Frequency: daily | Duration: 4 weeks
Protocol: BPC-157 | dose=0 | unit='' | freq='' | dur=''   (same for TB-500, Retatrutide, MOTS-C, NAD+: dose 0, no unit/frequency/duration)
Score=61 Scored=True ParseConfidence=medium Base=50 Syn=11 Red=0 Interf=0
IssueCount=0
```
After:
```
ExtractedTextPreview: Sheet: CSV\nBPC-157 5000mcg daily 4 weeks\nTB-500 50mg daily 6 weeks\nRetatrutide 12mg weekly 12 weeks\nMOTS-C 10mg daily 4 weeks\nNAD+ 500mg daily 4 weeks
Protocol: BPC-157 | dose=5000 | unit='mcg' | freq='daily' | dur='4 weeks'
Protocol: TB-500 | dose=50 | unit='mg' | freq='daily' | dur='6 weeks'
Protocol: Retatrutide | dose=12 | unit='mg' | freq='weekly' | dur='12 weeks'
Protocol: MOTS-C | dose=10 | unit='mg' | freq='daily' | dur='4 weeks'
Protocol: NAD+ | dose=500 | unit='mg' | freq='daily' | dur='4 weeks'
Score=61 Scored=True ParseConfidence=medium Base=50 Syn=11 Red=0 Interf=0
Issue: [evidence_context] The recorded 5000 mcg amount is 10 to 20 times the initiation range used in the reviewed trials. (BPC-157)
Issue: [evidence_context] The recorded 50 mg amount is 10 to 25 times the initiation range used in the reviewed trials. (TB-500)
Issue: [evidence_context] The recorded 12 mg amount is 6 to 6 times the initiation range used in the reviewed trials. (Retatrutide)
Issue: [evidence_context] The recorded 10 mg amount is 2 to 2 times the initiation range used in the reviewed trials. (MOTS-C)
IssueCount=4
```
So identical spreadsheet uploads now produce different (dose-aware) analysis: the user-visible preview, the `Protocol[]` doses/units, and generated issues all change (score unchanged in these fixtures).

## Notes for the reviewer (interpretations, no spec deviations)

- Cleanup (empty brackets / leading/trailing separator-only tokens) runs on every name candidate after embedded-quantity removal, per the spec Name bullet. (Corrected after code review F1: the first commit gated cleanup on removal having changed the value, which dropped rows named `Zorbatide ()`/`Zorbatide -` and emitted the junk name `Zorbatide()`; see the review-fix section below.)
- Duration: columns are tried in header order and the first whose value has a `DurationPattern` match with no range and no cycle phrase wins (skipping a ranged/cycle column in favour of a later one).
- Header width = highest non-blank header column + 1. A data row whose only content sits past the header width is *not* blank: it is skipped and counted (no name).
- XLSX rows are read lazily so columns past the header width are never stored; cells past XFD or with a dropped reference still advance the "previous cell" index used to place cells with no/malformed `r`.
- Non-ASCII dose-unit forms are written as regex escapes (Greek small letter mu, micro sign) in the private pattern copies.
- `ProtocolParser.cs` is untouched.

## Review fix — F1 (name cleanup gating)

Code review (VERDICT: FAIL, 1 blocking + 4 info; report `D:/tmp/BIO-ANALYZER-004-review-A.md`) found F1: `AppendReconstructed` called `CleanName` only when embedded-quantity removal changed the name cell, deviating from the spec Name bullet ("after embedded-quantity removal **and cleanup**"). Observed before the fix (reviewer probes, real ingestion + `ProtocolParser`): `Zorbatide (),250mcg,weekly` -> 0 entries (row silently lost at `IsLikelyCompoundName`), `Zorbatide -,250mcg,weekly` -> 0 entries, `Zorbatide(),250mcg,weekly` -> entry `Zorbatide()`.

Fix: `ProtocolIngestionService.cs` `AppendReconstructed` now applies `CleanName(stripped)` unconditionally to every name candidate (the gated `if (!string.Equals(stripped, candidate))` trigger is gone; `CleanName` comment updated). F2–F5 (info) left as recorded by the review: F2 pre-existing/bounded, F3 spec-consequence, F4 parser-resolved, F5 optional 16384+-column CSV parity, not taken.

Regression lock: `T28a_NameCleanup_DoesNotDropUnknownCompound` gained the three review-probe name cells (`Zorbatide ()`, `Zorbatide -`, `Zorbatide()`) plus a text assertion that locks the preview to `Sheet: CSV` followed by the line `Zorbatide 250mcg weekly`, so the cleaned name is checked in the text as well as in the entries. All three cases fail on the pre-fix code (reviewer-observed behavior above) and pass after.

Suites after the fix (from `<worktree>/backend`):

| # | Command | Result |
|---|---|---|
| 1 | `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~SpreadsheetRowReconstructionTests"` | **Passed 61 / 61** (58 + 3 new theory rows) |
| 2 | `dotnet test tests/BioStack.Application.Tests` | **Passed 956, Skipped 5, Failed 0, Total 961** (was 953 + 3) |

```
dotnet test tests/BioStack.Api.Tests --filter "FullyQualifiedName~AnalyzeEndpointsIntegrationTests|FullyQualifiedName~AnalyzerGateIntegrationTests"   -> Passed 6 / 6
```
