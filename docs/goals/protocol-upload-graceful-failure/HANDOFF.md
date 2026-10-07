# HANDOFF — protocol-upload-graceful-failure (BIO-ANALYZER-001)

Date: 2026-10-03. Coordinator state at stop: **exit criterion met; stopped before Gate 3 (merge/push/PR withheld by charter D2).**

## Where things are

- Branch `fix/protocol-upload-graceful-failure` (local only, never pushed) at `D:/Repos/BioStack/.worktrees/protocol-upload-graceful-failure-20261003`; comparison base `origin/main@1c8a16e5e950e3b75f559e9c8c0744f28db2f6a5`.
- Implementation commit `31f60386`; coordinator docs commits `8e1a3a2c`, `70dc5d47`, `39136b68`, plus this handoff commit.
- Spec `docs/specs/active/BIO-ANALYZER-001-upload-table-label-leak.md` (rev 3, registry row `active` / `not-yet-closed`). It moves to `done/` only after merge with closure evidence (spec lifecycle rule 7) — not done here.
- Superseded: branch `fix/analyzer-upload-remediation` on origin (commit `4d63f00b`: PDF flate extractor, DOCX structure tests, frontend analyzer changes) is untouched and out of this goal's scope; much of its test/extractor content has since reached `origin/main`.

## What changed

- C0 `ProtocolFingerprintService.ParserVersion` `v3` → `v4` (invalidates 7-day parse cache so leaked results are not served).
- C1 `ProtocolParser.IsLikelyCompoundName`: reject a name whose letter-only parts are all in a closed structural-label set (Frequency, Dose, Route, Timing, Strength, Injection, … 72 words). Fixes `Frequency`/`Timing` etc. appearing as compounds from CSV/XLSX headers (`Header: value` segments).
- Tests: `ProtocolUploadGracefulFailureTests` (T1–T7, 160 test cases) + endpoint test T8 (`AnalyzeProtocol_NoTextPdfUpload_Returns400WithFriendlyMessage`).

## Evidence (coordinator-reproduced, not only builder-reported)

| Check | Result |
|---|---|
| Red before C0/C1 (builder, class filter) | 145 failed / 160 (T1, both T2, 142 T3 cases; reviewer independently reproduced via scratch revert: same 145) |
| `ProtocolUploadGracefulFailureTests` after | 160/160 |
| `FullyQualifiedName~Protocol\|FullyQualifiedName~PdfProtocol` | 418/418 (258 baseline + 160), coordinator run |
| Full `BioStack.Application.Tests` | 862 passed, 5 skipped, 0 failed, coordinator run |
| `BioStack.Api.Tests` `AnalyzeEndpointsIntegrationTests` + `AnalyzerGateIntegrationTests` | 6/6, coordinator run |
| `git diff --check` | clean |
| Independent spec review | SpecReviewerA: rev1 FAIL (set too narrow, punctuation bypass, stale cache) → rev2 PASS → rev3 clarifications |
| Independent code review | BioAnalyzer001CodeReviewer (fresh, read-only): PASS, 0 blocking/major |

Full builder evidence map: `EVIDENCE.md`.

## Triage of code-review findings

| Finding | Ruling |
|---|---|
| Header words outside the closed set still leak (e.g. `Vial Size: 5mg`, `Start Date`, `Site`, `Supplier`, `Qty`, `Price`, `Description`…; only when the cell value has a dose/frequency word) | accept-as-documented (spec: closed set) — see developer decisions |
| T4 `BPC-157 500mcg daily` is alias-path and does not exercise C1 | accept-as-documented (other T4 cases do; optional sentinel `Zorbatide 500mcg daily`) |
| XLSX with duplicate relationship `Id` → unhandled `ArgumentException` → 500; absolute rel `Target` → 400 with internal path text | informational, pre-existing, out of spec |

## Deviations from the canonical loop (recorded, none weakens verification)

- Builder Step 0 was a restatement written into `EVIDENCE.md` with automatic continuation rather than a hard stop-and-wait (spec had already passed independent review).
- Builder slip: its first edit call used a relative path and briefly changed `ProtocolParser.cs` in the main checkout `D:/Repos/BioStack` (`dev` branch); it reverted that single file with `git checkout`. Reviewer confirmed the main checkout has no remaining diff in `BioStack.Application`. Nobody verified that the file had no *pre-existing* uncommitted edits before the revert; the builder states its only diff was its own hunk. **Developer: skim `git diff -- backend/src/BioStack.Application/Services/ProtocolParser.cs` in the main checkout if you had local edits there.**
- Review used harness `reviewer` subagents (fresh sessions) instead of the charter's `ai-council` substitute (charter amendment recorded).

## Developer decisions / remaining human gates

1. **Merge/push/PR (Gate 3):** not done. Branch is local; push and open a PR if you accept the diff (`git diff origin/main...fix/protocol-upload-graceful-failure`). After merge: coordinator closure — spec to `docs/specs/done/`, INDEX row to `done` with closure link.
2. **Residual A — single Title-case word + frequency prose** (`Review daily`, `Hydrate weekly`) still becomes an unknown compound (pre-existing recognition-gate gap, guarantee 2). Not fixed, not test-locked. Decide whether this blocks release; fix would need a new spec (e.g. require alias or dose for single-token names).
3. **Residual B — non-set header words** (list in review finding above). A durable fix is structural (have `ConvertDelimitedRowsToText` stop emitting header names into the parsed text or tag header segments) rather than extending the word list; needs a new spec.
4. **Residual C — pathological XLSX** (duplicate rel Id) returns 500 instead of 400; one-line catch/`ToLookup` fix in `SpreadsheetProtocolExtractor`, out of this spec.
5. Table-row dose recovery (BPC-157 row still reports dose 0 because dose lives in a sibling `Dose:` cell) remains out of scope (row-aware reconstruction).
6. Nothing here changes the production-readiness verdict; this is not go-live evidence.
