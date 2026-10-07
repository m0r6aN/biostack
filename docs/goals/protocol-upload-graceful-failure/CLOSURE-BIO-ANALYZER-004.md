# Closure Record — BIO-ANALYZER-004 (spreadsheet row reconstruction)

Status: **done**. Coordinator closure 2026-10-04. Gate 3 (merge/push/PR) was withheld at dispatch and executed later on explicit developer authorization; the merge rode PR #471.

- Spec: `docs/specs/done/BIO-ANALYZER-004-spreadsheet-row-reconstruction.md` (rev 6; sha256 `3EC9C94BC116DA48EDA5753A4E49462EB12A4A392579AC0C70F48B0421451D3C`)
- Goal charter / Gate 2: `docs/goals/protocol-upload-known-gaps/CHARTER.md`, `docs/goals/protocol-upload-known-gaps/GATE2-BIO-ANALYZER-004.md`
- Builder: BioAnalyzer004Builder, branch `fix/analyzer-spreadsheet-row-reconstruction`, worktree `.worktrees/bio-analyzer-004-20261004`
- Merged implementation: `2a205e08` (SpreadsheetProtocolExtractor row reconstruction: header-role mapping, per-row name/dose/frequency/duration emission, cell placement by `r` reference with hostile-reference bounds, values-only fallback, skipped-row warning aggregation; `IngestionVersion` v1 to v2; SpreadsheetRowReconstructionTests T1-T28) plus review-rework fix `b05482ee` (name cleanup runs on every name candidate) and evidence note `92bd98f1`. Merged to `main` in PR #471, merge commit `2696a750`.
- Green deterministic verification (final tree, recorded in EVIDENCE-BIO-ANALYZER-004.md):
  - RED before changes: 52 failed / 6 passed of 58 (per-test failure text recorded)
  - `SpreadsheetRowReconstructionTests`: 61/61 (58 + 3 review-F1 regression rows)
  - `ProtocolUploadGracefulFailureTests`: 160/160 (file unmodified)
  - filter `Protocol|PdfProtocol|SpreadsheetRowReconstructionTests`: 509/509
  - full `BioStack.Application.Tests`: 956 passed / 5 skipped / 0 failed
  - `BioStack.Api.Tests` AnalyzeEndpoints + AnalyzerGate: 6/6
  - `git diff --check`: clean
- Required independent reviews (two implementation reviewers per Gate 2), both reports archived at `docs/goals/protocol-upload-graceful-failure/reviews/BIO-ANALYZER-004/`:
  - BioAnalyzer004CodeReviewerA (read-only; lens correctness / hostile input): initial review **FAIL** (blocking F1 name-cleanup gating; 4 info findings F2-F5) on `2a205e08`, re-review of fix `b05482ee` **PASS**. Report: `code-review-A.md`.
  - BioAnalyzer004CodeReviewerB (read-only; lens blast radius / tests / contracts): **PASS** on `92bd98f1`, 0 blocking, 1 P3 nit. Report: `code-review-B.md`.
- Resolved rework: **F1** — `CleanName` was gated on embedded-quantity removal having changed the name cell, causing silent row loss (`Zorbatide ()`) and junk entry names (`Zorbatide()`). Fixed at `b05482ee` (cleanup unconditional per spec Name bullet) with three T28a regression rows and a preview-text assertion. Info findings F2-F5 left as reviewed: F2 pre-existing bounded DTD expansion (optional follow-up parcel), F3 consequence of the ratified cell-placement rule, F4 resolved by parser alias normalization, F5 optional CSV/XLSX parity at 16384+ columns (not taken).
- Reviewer B findings triage: one P3 — `ProtocolIngestionServiceTests` line 43 `Contains("BPC-157 500mcg")` is subsumed by line 42 `Contains("BPC-157 500mcg daily")`. Ruling: **accept-as-documented** (no behavioral impact; AC2 pins the three-assertion shape, amendment not warranted for reduced-only protection).
- Acceptance-to-evidence map: `docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-004.md` (AC1 red/green per test with observed failure text, AC2 diff limited to the three assertion lines plus one golden literal, AC3 verification commands with counts, AC4 no header leaks with discriminating messages, AC5 Allowed Files only and no public contract change, AC6 branch local only through both reviews, AC7 before/after smoke with analysis-visible changes) plus the "Review fix — F1" section for the rework cycle.
- Durable evidence: `docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-004.md`, `docs/goals/protocol-upload-graceful-failure/reviews/BIO-ANALYZER-004/code-review-A.md`, `docs/goals/protocol-upload-graceful-failure/reviews/BIO-ANALYZER-004/code-review-B.md`, `docs/goals/protocol-upload-known-gaps/GATE2-BIO-ANALYZER-004.md`, PR #471 (merge commit `2696a750`).
- Authorization note: the Gate 2 permission envelope was `local-only:no-push:no-network-mutation` and the goal directive withheld Gate 3. Push, PR, and merge were executed only on explicit developer instruction after the re-review PASS; that override is recorded in the PR body and here.
