# BIO-ANALYZER-004 — Implementation Review B (blast radius / tests / contracts)

- Parcel: BIO-ANALYZER-004 "spreadsheet row reconstruction" (merged, not yet closed)
- Reviewer: BioAnalyzer004CodeReviewerB — fresh read-only session; lens: **blast radius / tests / contracts**
- Diff under review: `65e2242b..92bd98f1` (2a205e08 implementation, b05482ee name-cleanup fix, 92bd98f1 evidence note)
- Worktree: `D:/Repos/BioStack/.worktrees/bio-analyzer-004-20261004` (branch `fix/analyzer-spreadsheet-row-reconstruction`)
- Spec: `docs/specs/active/BIO-ANALYZER-004-spreadsheet-row-reconstruction.md` rev 6; sha256 verified by me:
  `3ec9c94bc116da48eda5753a4e49462eb12a4a392579ac0c70f48b0421451d3c` (matches Gate 2 `specSha256`)
- Reviewer A's report was NOT read; all judgments below derive from the diff, spec, code, and my own test/probe runs.

## Findings table

| ID | Severity | file:line | Observed evidence | Required change |
|---|---|---|---|---|
| B-F1 | P3 (info) | backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionServiceTests.cs:43 | `Assert.Contains("BPC-157 500mcg", …)` is strictly subsumed by line 42 `Assert.Contains("BPC-157 500mcg daily", …)` for the same fixture (`Compound,Dose,Frequency\nBPC-157,500mcg,daily`, lines 32-40): any string containing the line-42 literal contains the line-43 literal, so line 43 cannot fail independently — a vacuous lock left from the mechanical rewrite of the old orthogonal pair (`Compound:` name + `Dose:` value). | Either drop line 43 or give it an orthogonal target (e.g. assert the verbatim `5 mg` variant from T27 here). |
| B-O1 | INFO | backend/src/BioStack.Application/Services/ProtocolIngestionService.cs:948-951 → frontend/src/components/tools/analyzer/analyzerView.ts:299-308 | Blast-radius observation (not a defect): the new skipped-row warning is the first non-empty `ExtractionWarnings` channel for spreadsheet uploads; `confidenceLabel` downgrades displayed confidence to `Medium` whenever any extraction warning exists, so any spreadsheet with >=1 name-less row now displays Medium confidence. Consequence is stated in the spec's Verified facts (deliberate), but no test covers it and `EVIDENCE-BIO-ANALYZER-004.md` does not record it. | None blocking. Suggest one line in the evidence record noting the frontend confidence consequence (or a frontend test in a later parcel). |
| B-O2 | INFO | backend/tests/BioStack.Application.Tests/Services/SpreadsheetRowReconstructionTests.cs:280-289 (T14) | Coverage gap: the spec's multi-compound rule names three separators (`;`, `|`, ` + `) but T14 locks only ` + `. I probed the other two (P1/P2 below): behavior is correct via `MultiCompoundSeparatorPattern` (ProtocolIngestionService.cs:515), but a regression in the `;`/`|` alternatives would go unnoticed. | Optional: add `;` and `|` theory rows to T14. |
| B-O3 | INFO | backend/tests/BioStack.Application.Tests/Services/SpreadsheetProtocolExtractorPackageTests.cs:17; backend/src/BioStack.Application/Services/ProtocolParser.cs:427-428 | Stale comments after cutover: "captured from the unmodified extractor" (golden now from the modified extractor; evidence acknowledges, spec restricted the edit to the literal) and the parser comment claiming the spreadsheet extractor emits `cell | cell | cell` rows (now only the values-only fallback does). Parser file was Forbidden to edit. | None in this parcel; follow-up doc pass. |

No blocking findings (P0-P2): none.

## Spec-interpretation risks seen through this lens

1. Documented residual R1 confirmed by probe P3: duration cell `4 weeks - 6 weeks` reports the **lower** bound (`4 weeks`) because `RangePattern` requires the optional unit *before* the separator. Matches the spec's accepted-as-documented R1 ruling exactly — no deviation, but readers of the output should know durations may under-report.
2. Documented residual (closure BR-N5) confirmed by probe P4: dose cell `500mcg daily` emits only `500mcg`; the frequency is lost unless a frequency column exists.
3. Multi-compound cells whose parts are **unknown** compounds produce name-only lines that the parser's gates then drop entirely (probe P5: `Zorbatide | Otherzide` -> 0 entries, no warning). Spec-conformant ("no dose is bound to a multi-compound cell"; parser-level drops are not warned), but user-visible silent loss for unknown pairs; T14 deliberately uses known aliases, which is why it does not surface this.
4. Past-XFD cells still advance the "previous cell" index for no/malformed-`r` placement (evidence interpretation 3). Literal reading of the spec's "(previous cell's index + 1)" supports it; consistent between code, evidence, and T17.
5. All "keep in sync with ProtocolParser" pattern copies verified **identical** to ProtocolParser.cs:12-29 (dose units use `\u03bc`/`\u00b5` regex escapes vs the parser's literal mu characters — regex-equivalent). Sync claim holds.

## AC-relevant checks (contract / test claims)

- AC5 / contracts — `git diff 65e2242b..92bd98f1 --stat` observed touching **exactly** the six Allowed Files (ProtocolIngestionService.cs +546/-29, ProtocolFingerprintService.cs 1 line, the three test files, the evidence doc). `ProtocolFingerprintService.cs` diff is only `IngestionVersion` "v1"->"v2" (observed full file diff). `AnalyzeProtocolResponse` / `ProtocolEntryResponse` are byte-identical to the pre-change checkout (blast-radius trace); no public signature change in `ProtocolIngestionService.cs` (all added members private; `ExtractAsync`/`CanHandle` signatures unchanged); `ProtocolExtractionResult` constructed with the same 4-argument shape; T22 uses the pre-existing public `GetIngestionCacheKey`. AC5 **holds**.
- AC2 — exactly three assertion lines changed in `ProtocolIngestionServiceTests.cs` (42, 43, 79) and one golden literal in `SpreadsheetProtocolExtractorPackageTests.cs` (:18, consumed at :45/:53/:63/:112); `Contains("Sheet: Stack")` (:78) kept; nothing else in those files changed (observed diffs). Evidence's "9 failures with old test files" cross-check supports that no other existing assertion depended on the old shape. (B-F1 notes the new line 43 is redundant, not wrong.)
- AC1 — evidence records per-case RED/GREEN with concrete failure text naming leaked headers/shifted values/dose 0; forecast deviations (T10b/T21b split, T20a/c/d red, T15c red) are recorded and none weaken a requirement. Regression locks (T10, T14, T21, T23 + the two zero-dose T7 rows) are consistent with the spec's AC1 list.
- AC3 — I ran the suites myself: see below; counts match the evidence exactly.
- AC7 — evidence contains before/after smoke for one `Compound,Dose,Frequency,Duration` CSV: preview, `Protocol[]` doses/units/frequencies/durations, and issue count (0 -> 4 evidence_context issues on the high-dose fixture) recorded; score unchanged at 61 in both fixtures. Satisfies the AC wording (see B-O1 for the one consequence not recorded).

## Tests audit (discriminating assertions / fixture isolation / boundaries)

- Harness `AssertNames`/`AssertEntry` enforce the **exact** entry-name set and the exact `dose|unit|frequency|duration` tuple per compound (with full text in failure messages) — no vacuous locks found among the 61 new cases except the pre-existing-file edit B-F1.
- Fixture isolation: the parser keeps the highest dose per compound name (ProtocolParser.cs:236-270, `MergeEntries` :244-247 — verified), so a same-compound positive-control row could mask a dose-0 subject; T25/T26 correctly use distinct aliases (BPC-157 control vs TB-500 subject) and every dose-0 assertion (T5, T14, T18, T25 subject, T28b) sits on a compound with no other row in its run. Isolation holds.
- Boundary coverage matches the spec's rules: XFD cap and hostile refs (T17/T17b), header-width limiting (T16), duplicate `r` last-wins (T17), inlineStr (T19), BOM (T6a/c), RFC 4180 quoting incl. embedded newline and literal-append (T6b/T20d), unterminated quote (T20b), semicolon CSV fallback (T20c), row breaks (T20a), blank-row dropping (T10/T10b), case variants (T11), duplicate roles (T12), name-letter fallthrough (T13), unit hints incl. fall-through order (T7), values-only fallback (T8), warning aggregation (T9), cache invalidation (T22), embedded quantities (T24), ranges/commas (T25), cycles (T26), verbatim dose (T27), unconditional cleanup (T28a incl. the three review-F1 regression rows).
- Green locks in the evidence file assert concrete behavior (exact text equalities, exact tuples, `Assert.Single(warnings)` + exact warning string + no row-content echo) — verified against the test source.

## Probes I ran (scratch test file created and deleted; `git status --short` empty at end)

| Probe | Input | Observed result | Assessment |
|---|---|---|---|
| P1 | name cell `BPC-157; TB-500`, Dose `500mcg` | text `BPC-157` / `TB-500` (name-only lines); entries BPC-157 dose 0, TB-500 dose 0 | spec rule for `;` holds (untested by suite, see B-O2) |
| P2 | name cell `BPC-157 \| TB-500`, Dose `500mcg` | same as P1 | spec rule for `\|` holds (untested by suite) |
| P3 | Duration `4 weeks - 6 weeks` | line `BPC-157 500mcg 4 weeks`; entry duration `4 weeks` | documented residual R1 confirmed |
| P4 | Dose cell `500mcg daily`, no Frequency column | line `BPC-157 500mcg`; entry dose 500, frequency empty | documented residual (BR-N5) confirmed |
| P5 | name cell `Zorbatide \| Otherzide`, Dose `250mcg`, Frequency `weekly` | text `Zorbatide` / `Otherzide` (name-only); **0 entries**, no warning | spec-conformant (multi-compound binds no dose; parser drops are not warned) — silent loss for unknown pairs, noted as interpretation risk 3 |

## Suites run (from `<worktree>/backend`, observed output)

| Command | Result |
|---|---|
| `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~SpreadsheetRowReconstructionTests"` | Passed 61 / 61, Failed 0 (expected 61/61 — match) |
| `dotnet test tests/BioStack.Application.Tests` | Passed 956, Skipped 5, Failed 0, Total 961 (expected 956/5 — match; includes ProtocolUploadGracefulFailureTests unmodified and all 002/003 suites) |
| `git diff 65e2242b..92bd98f1 --stat` | only the 6 Allowed Files (AC5) |
| `git status --short` (end of session) | empty — no scratch leftovers, no repo files modified |

Other blast-radius facts verified: `Sanitise` (ProtocolIngestionService.cs:900) is private to `SpreadsheetProtocolExtractor`; `ProtocolExtractorSupport` and all other extractors (Plain/Pdf/Docx/Ocr/Link) are outside the diff; ingestion cache key `analyzer:ingestion:{mode}:v2:{fingerprint}` (7-day TTL) — v1 entries are never read (behavioral lock T22); parse cache is keyed by hash of NormalizedText and analysis caches by hash of NormalizedProtocol, so all self-invalidate on the new shape; frontend renders `extractedTextPreview` opaquely (`whitespace-pre-wrap`) and never parses the shape; the `Sheet: {name}` line is unchanged (a dose/frequency word in a sheet name can still leak a fake compound — pre-existing, out of scope).

VERDICT: PASS
