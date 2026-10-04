# Closure Record — BIO-ANALYZER-001 (upload table label leak)

Status: **done**. Coordinator closure 2026-10-04. Gate 3 (merge/push/PR) was withheld at dispatch and executed later on explicit developer authorization; the merge rode PR #471.

- Spec: `docs/specs/done/BIO-ANALYZER-001-upload-table-label-leak.md` (rev 3; sha256 `4A866B6669CDFFACB4F1211CEFD0BD10CBAEDD2F6B1E7F7486AADE9A5E889586`)
- Goal charter / Gate 2: `docs/goals/protocol-upload-graceful-failure/CHARTER.md`, `docs/goals/protocol-upload-graceful-failure/GATE2.md`
- Builder: BioAnalyzer001Builder, branch `fix/protocol-upload-graceful-failure`, worktree `.worktrees/protocol-upload-graceful-failure-20261003`
- Merged implementation: `31f60386` (ProtocolFingerprintService ParserVersion v3 to v4; ProtocolParser closed structural-label set at the end of IsLikelyCompoundName; ProtocolUploadGracefulFailureTests T1-T7 plus AnalyzeEndpointsIntegrationTests T8). Merged to `main` in PR #471, merge commit `2696a750`.
- Green deterministic verification (coordinator-reproduced, recorded in HANDOFF.md):
  - RED before changes: 145 failed / 160 (reviewer independently reproduced same 145 via scratch revert)
  - `ProtocolUploadGracefulFailureTests`: 160/160
  - filter `Protocol|PdfProtocol`: 418/418 (258 baseline + 160)
  - full `BioStack.Application.Tests`: 862 passed / 5 skipped / 0 failed
  - `BioStack.Api.Tests` AnalyzeEndpoints + AnalyzerGate: 6/6
  - `git diff --check`: clean
- Required independent reviews (1 implementation reviewer per Gate 2):
  - Spec review: SpecReviewerA rev1 FAIL (3 blocking, triaged) to rev2 PASS at `70dc5d47`; rev 3 clarification-only
  - Implementation review: BioAnalyzer001CodeReviewer (fresh reviewer session, read-only): PASS, 0 blocking/major; findings triaged in `HANDOFF.md` (2 accept-as-documented, 1 informational pre-existing)
- Resolved rework: none required by the implementation review; three findings triaged as documented residuals (see HANDOFF.md), which shaped the follow-up goal `protocol-upload-known-gaps` (residuals A/B/C) and were closed there by BIO-ANALYZER-002/-003/-004.
- Acceptance-to-evidence map: `docs/goals/protocol-upload-graceful-failure/EVIDENCE.md` (Step 0 restatement and allowed/forbidden surfaces, red run with per-test failure text, green run, Api filter T8, full Application filter).
- Durable evidence: `docs/goals/protocol-upload-graceful-failure/EVIDENCE.md`, `docs/goals/protocol-upload-graceful-failure/HANDOFF.md`, `docs/goals/protocol-upload-graceful-failure/GATE2.md`.
