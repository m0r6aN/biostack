# Goal Charter — Protocol Upload Graceful-Failure Remediation (P01-R)

**Status:** DRAFT — awaiting Gate 1 ratification
**Coordinator:** this `/goal` session (pi / Claude)
**Goal slug:** `protocol-upload-graceful-failure`
**Working branch (isolated):** `fix/analyzer-upload-remediation`
**Worktree:** `D:/Repos/BioStack/.worktrees/analyzer-upload-remediation-20260922` (branched from `main` @ `e5b75e0`)
**Date:** 2026-09-22

## Objective

Close the *smallest remaining release-blocking gap* in the uploaded protocol
analyzer so that the two user-stated guarantees hold and are locked by
regression tests:

1. **Ordinary PDF and DOCX uploads fail gracefully** — no crash, no 500, no
   wall of fake compounds; the user gets an honest, bounded result (or a clean
   4xx with a friendly message when the file has no readable protocol text).
2. **Real protocol content parses without treating document headers, prose,
   citations, or table labels as compounds.**

Deliverable = a committed fix + deterministic test evidence + a **durable
handoff** on the isolated branch. The remediation is explicitly NOT deployed,
merged, pushed, or claimed go-live.

## What the investigation already established (coordinator reads on disk)

- The compound **recognition gate** (PR #111), the **P1 score-confidence**
  fields, the numeric-alias guard, and the DOCX *protocol-packet* golden test
  are **already merged to `main`**.
- All 45 targeted analyzer/ingestion/parser tests **PASS on current `main`**
  in the isolated worktree (verified `dotnet test`, see EVIDENCE).
- The analyzer endpoint (`AnalyzeEndpoints.cs`) already maps
  `ProtocolIngestionException → HTTP 400 {message}` and any unexpected
  `Exception → ProblemDetails`, i.e. graceful at the boundary.
- A throwaway Stage-Zero probe (since deleted) confirmed the *correct*
  behavior for: prose-only DOCX (`parseConfidence=none`, `scored=false`,
  0 entries), image-only PDF (friendly ingestion failure → 400), and a
  real-protocol PDF (recognizes `BPC-157`/`Retatrutide`, rejects
  title/prose/citation lines).

## The confirmed residual defect (the actual remediation target)

**Table/field labels leak as unknown compounds on the
`SpreadsheetProtocolExtractor` (CSV/XLSX) path** (and any table row that the
extractor renders as `"Label: value"` in a single segment).

Reproduction (probe, now removed): a CSV with header row
`Compound,Dose,Frequency` and data rows yields a parsed entry
`Frequency[-]` — the literal column label — because:

- `ConvertDelimitedRowsToText` emits `Frequency: daily` / `Frequency: Goal`
  segments; `Frequency` is a FrequencyPattern word but it *leads*, leaving a
  name-slice of `Frequency:`;
- `BuildNameSlice` strips dosing verbs (`dose|take|…`) but **not** the
  structural labels `Frequency`, `Compound`, `Route`, `Timing`, `Duration`,
  `Administration`, `Directions`, `Schedule`, `Protocol`, `Goal`, `Note`,
  `Reference`, `Version`, `Tracking`, `Baseline`, `Evidence`, `Phase`,
  `Support`, `Stack`, `Materials`, `Blood`, `Work`, `Week`, `Weeks`, `Day`,
  `Days`, `Month`, `Months`;
- `IsLikelyCompoundName("Frequency")` returns true (Title-case, ≥3 letters),
  so the segment is emitted as a fake compound.

This is exactly the "table labels as compounds" failure mode the ledger lists
as release-blocking for the analyzer. It is **narrow** (a name-slice label
blacklist / shape rule) and **low-risk**.

## Locked decisions (D1–D5) — ratify at Gate 1

### D1 — Scope: fix + lock, no PDF-extraction hardening
Smallest release-blocking remediation = (a) fix the table-label leak above;
(b) add regression tests that lock **all four** graceful clauses:
DOCX prose-only, CSV/XLSX table-labels, real-protocol PDF, PDF-no-text →
400; (c) one endpoint integration test proving `ProtocolIngestionException`
uploads map to HTTP 400. **Out of scope** (per
`docs/development/ANALYZER_CONFIDENCE_AND_UI_PLAN.md` §6): rewriting the
hand-rolled regex PDF extractor, image/PDF OCR hardening, row-aware table
reconstruction, and any frontend P2 work. *Recommendation: accept as written.*

### D2 — Isolation & outward boundaries
All work happens **only** in the named worktree/branch. I will make **local
commits on `fix/analyzer-upload-remediation`** so the fix + tests + handoff are
durable. I will **NOT** push, open a PR, merge, deploy, touch cloud/settings,
or claim go-live. *Recommendation: accept; the developer performs push/merge
manually after review.*

### D3 — Verification method (a real constraint, not a preference)
This pi harness exposes **no subagent/Task dispatch tool** (verified via
`jev_find_tools`), so I cannot run the canonical Coordinator loop of separate
fresh builder + fresh adversarial-reviewer sessions. Proposed adaptation that
preserves "verification produced by others": I build in the worktree, then
route the diff to the **`ai-council` skill** (independent external frontier
CLIs: grok / codex / gemini / claude) for adversarial review, and I reproduce
any disputed finding before ruling. The durable handoff will record this
deviation and list "run one canonical fresh adversarial-review session" as a
remaining human gate. *Recommendation: accept the ai-council substitution; if
you require a strict canonical review, tell me and I will stop at build and
hand the diff to you to review in a separate session.*

### D4 — Where durable artifacts live
Goal charter, loop directive, plan-review findings, evidence, and
**HANDOFF** under
`D:/Repos/BioStack/.worktrees/analyzer-upload-remediation-20260922/docs/goals/protocol-upload-graceful-failure/`
(version-controlled on the branch, isolated). The handoff names exact test
counts, commands, and the remaining human/environment gates.
*Recommendation: accept.*

### D5 — Exit criterion
`goal exit` = the four graceful clauses are each asserted by a green test on
the branch; the table-label leak is fixed and regression-locked; full targeted
protocol test class stays green (≥ prior 45, plus new tests); a durable HANDOFF
exists on the branch with exact evidence and remaining gates; nothing pushed or
merged. *Recommendation: accept.*

## Parcel decomposition (single parcel)

- **P01-R (one parcel, standard→risk-judgment class):** fix table-label
  name-slice rejection + add the four graceful-failure regression tests +
  endpoint 400 mapping test + write HANDOFF. Risk: low-med (parser behavior +
  new tests). Routing: builder tier; mandatory one adversarial review
  (ai-council per D3), because "does coverage actually lock the guarantee / is
  there another leak path" is judgment-heavy (charter lesson #12 spirit).

Dependency order: trivially single; the P01-R test additions depend on the
label-rejection fix (write test → see it fail → fix → see it pass).

## Standing authorizations requested at Gate 1

- **Gate 2 (dispatch):** delegate to me to shape+build P01-R in the worktree
  under D1–D5 without a per-parcel dispatch approval round-trip.
- **Gate 3 (merge):** **withheld** — you (or a human) perform merge/push. I
  will stop before any outward action. (Matches your "do not merge/push" rule.)

## Stop conditions (universal)

- Exit criterion met → final report + stop.
- A frozen contract (public `AnalyzeProtocolResponse` shape, feature gate, or
  ingestion exception contract) needs modification → stop, escalate.
- The table-label fix trips a non-obvious regression I cannot close in-parcel →
  stop, report.
- Anything outward-facing beyond D2 (push, PR, deploy, cloud) → never without
  explicit human action.
- You say stop.
