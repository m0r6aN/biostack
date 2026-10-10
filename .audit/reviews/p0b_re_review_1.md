# p0b_re_review_1 — P0-B spec re-review

**Verdict:** APPROVE
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p0b-shaping/docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md (branch `docs/p0b-shaping`, PR #526, commit `9b440a9ca5a968aab269ca0ad327dadb57ef77ac`)
**Spec SHA-256:** `b0068ed653ff825ea1c627e7b32ceca9fca25b039dc99fb1cb555bf93b10967f`
**Date:** 2026-10-08

## Ranked findings

No BLOCKER or MAJOR findings. No unresolved items from the two named prior reviews remain open.
Two cosmetic/informational observations only (both pre-existing in substance, not introduced by
this rework, and not required fixes):

### N1 — MINOR/INFO — `"degraded-escalates-on-strong-signal"` is a fifth string value layered on a stated four-value enum
**Claim:** Deliverables item 9 states `"behavior"` values are "one of `"allowed"`/`"degraded"`/
`"refused"`/`"escalated"`" (P0-B.md ~line 355), but the `interaction-or-contraindication-signal`
`C3` cell is separately encoded as the literal string `"degraded-escalates-on-strong-signal"`
(P0-B.md lines 430-431). The spec explicitly anticipates and disclaims this ("not a fifth behavior
literal") and ties the dynamic upgrade to the ruled `D→E` cell and the function-declared match
threshold — this is a pre-existing, previously-reviewed (both prior reviews found matrix fidelity
for this exact cell "clean") design choice, not a regression introduced by this rework, and does
not create ambiguity once the explanatory sentence is read. No amendment required; noted for
completeness only.
**Changes a locked decision?** No.

### N2 — INFO — D-B6 citation range (`lines 152-163`) includes two trailing non-content lines
**Claim:** Deliverables item 8 cites `P0-B-DESIGN-GATE.md` "D-B6" at "lines 152-163." Direct
inspection shows D-B6's four numbered rules end at line 161; line 162 is blank and line 163 is the
document's closing `---` separator. The quoted rule text itself is verbatim-correct and the range
is not misleading (it does not bleed into a different section, unlike the two citations R1-F2/R2-F5
flagged and which are now fixed — see Verification notes). No amendment required.
**Changes a locked decision?** No.

## Missing pieces / collisions / unknowns

None found. Scope is unchanged from the prior reviews' confirmed boundary: PR #526 still adds
exactly one file (903 lines, confirmed both by `gh pr view 526 --json files` and local `wc -l`),
no dispatch artifacts, no other repository changes.

## Verification notes — disposition-by-disposition

**(1) D14 `mandatoryStopConditions` fold (R1-F1 / R2-F3) — genuinely closed.**
P0-B.md now contains an explicit `### mandatoryStopConditions fold (per class, mirroring the
minimumChecks table above)` section (lines 711-731) with a nine-row table, one row per literal
across all four declared classes, each mapped either to a named existing P0-B stop-condition
bullet or to an explicit `not-applicable` disposition with stated reasoning (privacy's two
not-applicable rows are reasoned, not silent). Independently re-extracted
`delivery-class-controls.json`'s four classes' `mandatoryStopConditions` arrays by direct JSON
parse and diffed against the fold table's left column: all nine literals
(`unsupported certainty`, `prescribed-treatment direction`, `red-flag bypass`, `provenance loss`,
`new sensitive field without approved lifecycle`, `leakage`, `consent bypass`,
`unapproved policy presented as effective`, `policy/enforcement mismatch`,
`missing source/license/review state`, `bypassed promotion`, `unreviewed public claim`) are present
verbatim and correctly attributed to their class — note the schema actually carries 12 literals
across the four classes (health-boundary 4, privacy 3, legal-policy 2, knowledge-promotion 3); the
spec's fold table carries all 12, a superset of "the nine" named in the prior reviews' framing
(which undercounted privacy's third entry in one framing) — no shortfall found either way. The two
previously-missing health-boundary stop conditions are now live, independent "Stop conditions"
bullets with their own prose (lines 851-858: "A disputed `[OPERATIONALIZED — bounded]` criterion...
asserts a certainty level the cited evidence source does not support" for `unsupported certainty`;
"Any artifact text directs... alteration of a prescribed treatment" for `prescribed-treatment
direction`), each explicitly cross-referenced to the fold table. Traceable and auditable.

**(2) Degraded/escalate cell semantics defined in-contract verbatim (R2-F1) — genuinely closed.**
Deliverables item 10, `"cellSemantics"` (P0-B.md lines 378-397), now transcribes
`P0-B-DESIGN-GATE.md` lines 95-96 in full, including the operative parentheticals: `"D — degraded
(allowed with mandatory evidence/explanation/validation additions)"` and `"E — escalate (stop
ordinary output, surface escalation)"`, as a top-level JSON object every `"behavior"` value and the
"Per-label behavior matrix" prose explicitly reference rather than re-stating the bare letter key
alone. A new `cell-semantics-fidelity` deterministic check (Deterministic verification, line 745)
requires both parentheticals to be present byte-for-byte in both artifacts, not just the bare
letters. Direct comparison of design-gate lines 95-96 against the spec's quoted text: byte-identical.

**(3) Machine-readable per-cell scope qualifiers (R2-F2) — genuinely closed.**
Deliverables item 9 (P0-B.md lines 365-375) now specifies a `"scope"` string field on the cell
object, required wherever the ruled table carries a parenthetical qualifier
(`"dose-context-only"` for `pregnancy-or-lactation`'s `C1` cell, `"prescribed-treatment-only"` for
`prescription-treatment-involved`'s `C3` cell), `null`/omitted elsewhere, marked
`[RULED — verbatim, structurally encoded]`. A new `qualified-cell-scope-present` deterministic
check (line 749-753) fails if either qualified cell lacks the field or any unqualified cell's field
is non-null. This closes the prior "laundry-list loophole" finding — the qualifier is no longer
free-text-only.

**(4) Two applicability criteria pinned deterministic threshold/matching semantics (R1-F3) —
genuinely closed.** `investigational-or-unapproved` (P0-B.md line 449) now states explicitly: an
output with zero cited evidence sources is "out-of-scope for this criterion (not vacuously true)"
and defers to `missingInputLadder` rule 2 — closing the ambiguity the prior review flagged between
"vacuously true" and "not-yet-determinable" readings. `interaction-or-contraindication-signal`
(P0-B.md line 452) now states the base-applicability matching algorithm "is function-declared, not
fixed by this contract," using the same deferral pattern already used for
`acute-red-flag-or-emergency`'s triggering-criterion-set language, and explicitly ties the base
threshold to the same threshold already named for the `D→E` strong-signal escalation (one
function-declared threshold, not two independently defined ones). Both closures match the prior
reviews' own named smallest-amendment proposals closely.

**(5) Design-gate line-range citations — all verified accurate against the live file (re-checked
independently, not merely re-trusted).** Re-extracted `P0-B-DESIGN-GATE.md` directly (`wc -l` =
207; `sed -n` at every cited span) and compared against every pinpoint citation in P0-B.md:
- `§1-§6 (lines 23-207)` — file's actual full extent (207 lines total); the stale "lines 23-215"
  from the prior review round is gone.
- D-B1 `(lines 81-91)` — header through the recommendation paragraph's closing sentence: exact.
- D-B2 table `(lines 100-113)` — header row through the controlled-or-illegal-sourcing row plus
  footnote; exact (previously miscited as "99-115").
- D-B2 preemption paragraph `(lines 115-119)` — exact (previously miscited as "117-120").
- D-B2 cell-key sentence `(lines 95-96)` — exact, newly added citation, verified byte-for-byte.
- D-B3 `(lines 121-131)`, D-B4 `(lines 133-140)`, D-B5 `(lines 142-150)`, D-B6
  `(lines 152-163)` — all exact (D-B6's range includes two trailing non-content lines — blank line
  plus closing `---` — see N2 above; not misleading, informational only).
- D-G `(lines 114-123)`, D-H `(lines 125-150)`, D-I `(lines 152-169)` in
  `COORDINATOR-DECISIONS-2026-10-07.md` — all exact, matching the file's actual `## D-G`/`## D-H`/
  `## D-I` section boundaries.

**CRITICAL REGRESSION CHECK — clean, no deviation found.**
- Performed a direct line-by-line diff of P0-B.md's reproduced "Per-label behavior matrix" (10
  rows × C1/C2/C3/Calibration-required, plus the controlled-or-illegal-sourcing footnote) against
  `P0-B-DESIGN-GATE.md` lines 100-113 (file hash `34928ac8c54fe8af9cb7b3a01d36820fe435f2adf4f02f56c20e7cf548d57f5b`,
  matches the spec's own cited hash exactly). **All 10 rows are byte-identical**, including the
  `D→E` cell and both LOCKED rows.
- D-B3, D-B4, D-B5, D-B6 rule text (all numbered sub-rules) independently diffed against the
  design-gate source and found byte-identical to the spec's Deliverables quotations.
- `publiclyEnabled: false` is hard-required at every one of its 8 occurrences in the spec body (JSON
  fragment, prose constraint ×2, "behavior" field note, Legal-policy cataloguing-discipline note,
  `enablement-field-fidelity` check definition, and acceptance criterion 2); no occurrence softens,
  conditions, or omits it. No new carve-out, no silent weakening.
- Independently recomputed SHA-256 for every cited canon source and confirmed exact (case-
  insensitive hex) match to the spec's claimed hashes: charter (`4cd390d6...`), coordinator-
  decisions ledger (`908faa59...`), design-gate (`34928ac8...`), guidance-content-contract v1.0.0
  (`60729197...`), `classification-axes.schema.json` (`49f9fcfa...`), `delivery-class-controls.json`
  (`e1e545cc...`), P0-A.md (`21eae29f...`).
- Confirmed, by direct JSON parse of `delivery-class-controls.json`, that `reviewers: 2` and the
  stated `additionalMergeGate` values (`green-dual-review-required` for health-boundary,
  `recorded-human-approval` for legal-policy, `null` for privacy/knowledge-promotion) match the
  spec's "Review/gates" section exactly.
- No `TBD`/`TODO`/`FIXME`/`{{...}}` placeholder found outside the spec's own rule-prose describing
  the prohibition.
- PR #526 confirmed via `gh pr view 526 --json files,additions,deletions,title`: exactly one file
  added, 903 additions, 0 deletions, no dispatch artifacts — unchanged scope from the prior review
  round's findings.
- Authorization-boundary language (P3-A → P0-A → this spec's own review, in that order, before
  Gate 2 dispatch) is unchanged in substance and remains correctly stated; P0-A.md and P3-A.md
  still read `REVIEW CANDIDATE`/not-yet-closed at this commit, consistent with the spec's claims.

**Scan for new defects introduced by the rework delta — none found.** The added/changed material
(the `mandatoryStopConditions` fold table and its nine Stop-conditions bullets, the `"cellSemantics"`
object and its prose, the `"scope"` field and its prose, the two applicability-criterion threshold
clauses, and the corrected line-range citations) is internally consistent with the rest of the
document: new deterministic checks (`cell-semantics-fidelity`, `qualified-cell-scope-present`) are
correctly cross-referenced from both "Deterministic verification" and "Acceptance criteria" (items
5), the new Stop-conditions bullets are correctly cross-referenced from the fold table and vice
versa, and no new text contradicts the frozen D-B1..D-B6 matrix, the charter's closed vocabulary, or
the `publiclyEnabled: false` posture. No scope creep into P0-C/P3-B/P0-D territory was introduced by
the delta; "Exact allowed surfaces" and "Frozen surfaces" are textually unchanged from what both
prior reviews already found clean.
