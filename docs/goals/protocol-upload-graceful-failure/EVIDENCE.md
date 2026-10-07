# EVIDENCE — BIO-ANALYZER-001 (spec rev 3)

Worktree `.worktrees/protocol-upload-graceful-failure-20261003`, branch `fix/protocol-upload-graceful-failure`, base HEAD 39136b68 (comparison base 1c8a16e5). All commands run from `backend/`.

## Step 0 restatement

**Change.**
- C0: `ProtocolFingerprintService.ParserVersion` `"v3"` -> `"v4"` so cached parse results (key `analyzer:parse:parser-{ParserVersion}:...`, 7-day TTL) from the leaky parser are not reused.
- C1: in `ProtocolParser.cs` add a closed `static readonly HashSet<string> StructuralLabelWords` (OrdinalIgnoreCase, the exact 72-word set from the spec) and, at the end of `IsLikelyCompoundName` (after existing token checks, which are untouched), reject the name when its letter-only parts (split on `[^\p{L}]+`, empties dropped) number at least one and every part is in the set. Names with any non-set letter-part (`BPC-157`, `Blood Flow Peptide`, `Frequency Booster`) are unaffected.

**Tests.** New class `backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs`: T1 CSV header leak (ingestion -> parser); T2 XLSX + ALL-CAPS CSV variant; T3 theory over every set word x {`: daily`, `: 500mcg`} plus 6 compound-header shapes; T4 non-regression (`BPC-157`, `Blood Flow Peptide`, `Peptide Alpha`, `Frequency Booster`); T5 prose-only DOCX via analyzer (no entries, not scored, confidence `none`); T6 real-protocol PDF with escaped parens (extracted text contains citation + narrative; names == {BPC-157, Retatrutide}); T7 no-text PDF -> `ProtocolIngestionException` with friendly message. T8 lives in `backend/tests/BioStack.Api.Tests/Integration/AnalyzeEndpointsIntegrationTests.cs` (multipart no-text PDF as Operator -> 400, `message` contains "readable text").

**Allowed:** ProtocolParser.cs, ProtocolFingerprintService.cs, the new test class, AnalyzeEndpointsIntegrationTests.cs, this EVIDENCE.md. **Forbidden:** everything else (contracts, IProtocolParser, AnalyzeEndpoints, extractors, gates, frontend, ScoringVersion), extending the C1 set, other heuristics, fixing the `Review daily` residual, mocks of parser/extractors, push/PR/merge. Spec/charter/GATE2/HANDOFF/loop-directive/INDEX are frozen.

**Stop triggers:** C1 breaks an existing test; T5-T8 need out-of-allowed-file changes; a label leak outside the C1 set; a test needs a mock of parser/extractor; Api multipart upload cannot be built. None triggered.

Spec is internally consistent and buildable; proceeded.

## Red run (before C0/C1; only the new test class existed)

Command: `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~ProtocolUploadGracefulFailureTests"`

```
Failed!  - Failed:   145, Passed:    15, Skipped:     0, Total:   160, Duration: 295 ms - BioStack.Application.Tests.dll (net10.0)
```

- T1 `T1_CsvUpload_HeaderLabelsAreNotCompounds` RED: `Header labels leaked as compounds: Frequency | Timing. All parsed: BPC-157 | Frequency | Timing | Retatrutide`
- T2 `T2_XlsxUpload_HeaderLabelsAreNotCompounds` RED: same message (`Frequency | Timing`)
- T2 `T2_CsvUpload_AllCapsHeaderLabelsAreNotCompounds` RED: `Header labels leaked as compounds: FREQUENCY | TIMING. All parsed: BPC-157 | FREQUENCY | TIMING | Retatrutide`
- T3: 142 of 150 cases RED, e.g. `Segment 'Work: daily' leaked entries: Work`, `Segment 'Vial: 500mcg' leaked entries: Vial`, plus all six compound-header shapes:
  - `Dose/Frequency: daily` => `/Frequency`
  - `Frequency (per week): daily` => `Frequency (per week)`
  - `Dose (mg): 5 mg` => `(mg)`
  - `Injection Frequency: daily` => `Injection Frequency`
  - `Dosing Schedule: weekly` => `Dosing Schedule`
  - `FREQUENCY: daily` => `FREQUENCY`
- T3 cases NOT red pre-C1 (8, already rejected by existing logic, e.g. dose-verb/ <=2-letter handling): `Dose: daily`, `Dose: 500mcg`, `Mg: daily`, `Mg: 500mcg`, `Ml: daily`, `Ml: 500mcg`, `Iu: daily`, `Iu: 500mcg`.
- T4 (4), T5, T6, T7 passed pre-C1 (expected: T5 fixture's `Note:` line did not leak; T6/T7 are guarantees already held).

## Green run (after C0 + C1)

```
Passed!  - Failed:     0, Passed:   160, Skipped:     0, Total:   160, Duration: 460 ms - BioStack.Application.Tests.dll (net10.0)
```

## Api filter (T8)

`dotnet test tests/BioStack.Api.Tests --filter "FullyQualifiedName~AnalyzeEndpointsIntegrationTests"`
```
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 2 s - BioStack.Api.Tests.dll (net10.0)
```

## Full Application filter

`dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol"`
```
Passed!  - Failed:     0, Passed:   418, Skipped:     0, Total:   418, Duration: 770 ms - BioStack.Application.Tests.dll (net10.0)
```
418 = 258 baseline + 160 new.

`git diff --check` (pre-commit, working tree): exit 0, no output. `git diff --stat 1c8a16e5..HEAD` is recorded in the final report by the builder after commit (a file cannot contain its own commit's stat).

## Acceptance criteria

| AC | Status | Evidence |
|---|---|---|
| AC1 red then green | MET | Red: 145 failed / 160 (T1, both T2, 142 T3 cases, names leaked above; T5 was not red). Green: 160/160. |
| AC2 T1-T7 pass; baseline holds | MET | Class 160/160 (ProtocolUploadGracefulFailureTests.cs:66-202); filter 418/418 (= 258 + 160). |
| AC3 T4 no regression | MET | `T4_RealCompoundNames_AreStillEmitted` (ProtocolUploadGracefulFailureTests.cs:123), 4/4 in green run. |
| AC4 Api T8 green | MET | AnalyzeEndpointsIntegrationTests.cs:115, filter 2/2. |
| AC5 diff only Allowed Files, no contract change | MET | Only ParserVersion value (ProtocolFingerprintService.cs:10) and ProtocolParser.cs:37-54, 334-340 (additive private members) + tests + this file. |
| AC6 nothing pushed | MET | No push/fetch/remote changes performed; single local commit only. |

## Deviations / notes

- Process slip: my first `edit` call used a relative path and briefly modified `ProtocolParser.cs` in the main checkout `D:/Repos/BioStack`. I reverted that single file with `git checkout -- backend/src/BioStack.Application/Services/ProtocolParser.cs` (its only diff was my own hunk) and verified no Application diff remains there; other pre-existing uncommitted changes in main were untouched.
- Documented residual (`Review daily`-style lines) untouched, not locked by tests, per spec.
- T3 includes `Dose: daily` etc. (non-red pre-C1) as permitted.
