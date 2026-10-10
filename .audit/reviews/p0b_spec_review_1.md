# p0b_spec_review_1 — P0-B spec review

**Verdict:** APPROVE-WITH-FIXES
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p0b-shaping/docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md (PR #526, branch `docs/p0b-shaping`, adds this one file only — confirmed via `gh pr view 526`)
**Spec SHA-256:** `2b13dbe6b18463314fc54884d990d7e3ec96e4f56a5bd92f92096be4084bb5e7`
**Date:** 2026 (session date; see repo clock — not independently dated by this reviewer)

## Ranked findings

### F1 — MAJOR — D14 `mandatoryStopConditions` fieldwise fold is not explicitly performed (completeness gap)
**Claim:** The spec explicitly, by name, folds `additionalMergeGate` (`green-dual-review-required` + `recorded-human-approval`, "Review/gates") and `minimumChecks` (the "Deterministic verification" table) across the four declared classes, but never performs the equivalent explicit, named fold of `mandatoryStopConditions` from `delivery-class-controls.json`. The nine class-declared stop-condition literals — health-boundary: `unsupported certainty`, `prescribed-treatment direction`, `red-flag bypass`, `provenance loss`; privacy: `new sensitive field without approved lifecycle`, `leakage`, `consent bypass`; legal-policy: `unapproved policy presented as effective`, `policy/enforcement mismatch`; knowledge-promotion: `missing source/license/review state`, `bypassed promotion`, `unreviewed public claim` — appear **nowhere** in P0-B.md (verified by direct grep; zero matches for any of the nine literal strings).

**Evidence:** `docs/specs/schemas/delivery-class-controls.json` (SHA-256 `e1e545cc01d1ec316fc3f137e80792abac1f5d071bd09e15c8b5c804b536b068`, each class's `mandatoryStopConditions` array); P0-B.md "Stop conditions and standing-authorization tripwire" section (no fold table); contrast with `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md:669-688`, which contains an explicit "`mandatoryStopConditions` fold (per class, mirroring the `minimumChecks` table above)" section mapping every one of the same nine literals, class by class, to a named P0-A tripwire. P0-B's own text twice claims to mirror "P0-A's own adaptation discipline" (Deliverables intro to "Deterministic verification"; "Numeric provenance" item 2 framing) but does not carry forward this specific structural element.

**Effect on substance:** Most of the nine entries are *loosely* traceable to existing P0-B stop-condition bullets (e.g., `leakage` → "Any personal-data token is found in a draft artifact"; `unapproved policy presented as effective` → the public-availability-claim bullet), but at least three have no clean, demonstrable mapping in the current text: `policy/enforcement mismatch`, `bypassed promotion`, and the distinct-from-red-flag-bypass `unsupported certainty`/`prescribed-treatment direction` pairing. Without an explicit table, a builder or reviewer cannot audit this fold deterministically — exactly the "class-control completeness" failure mode the review charter asks to be disproved.

**Smallest amendment:** Add a "`mandatoryStopConditions` fold" subsection (same structure as P0-A.md:669-688), one row per of the nine literals, each mapped to the specific existing Stop-conditions bullet (or class-section sentence) that already covers it, or a new bullet added where no mapping currently exists.

**Changes a locked decision?** No — this is a structural/completeness fix to the spec's own class-control folding discipline, not a change to any D-B1..D-B6 ruled content.

---

### F2 — MINOR — Two internal line-range citations to the frozen design-gate document are inaccurate
**Claim:** Two of the spec's own pinpoint citations to `P0-B-DESIGN-GATE.md` do not match the file's actual line numbers, even though the quoted *text* in both cases is verbatim-correct.

**Evidence (verified directly by `sed -n` / `nl` against the live, hash-matched `P0-B-DESIGN-GATE.md`):**
- P0-B.md, Deliverables item 4 (`"preemptionOrder"`): cites "`P0-B-DESIGN-GATE.md` §3, 'D-B2,' the paragraph following the per-label table (lines 117-120)". The actual preemption paragraph is lines **115-119** (line 115: "Multi-label composition follows D14's fieldwise fold..."; line 119: "...no label erases another's obligations."); line 120 is a blank line and lines 115-116 — the paragraph's actual opening — are outside the cited range.
- P0-B.md, Deliverables item 9 (`"labels"` → `"behavior"`): cites the D-B2 table at "lines 99-115". The actual table (header through footnote) spans lines **100-113**; the cited range both starts one line early (99 is blank) and bleeds two lines past the table into the *next* section's opening sentence (114 is blank, 115 is the first line of the D-B2 preemption paragraph that item 4 separately and differently cites).

**Smallest amendment:** Correct the two line-range citations to `(lines 115-119)` for the preemption paragraph and `(lines 100-113)` for the table, matching the file as hash-verified.

**Changes a locked decision?** No — the quoted *text content* of both spans is byte-for-byte correct (independently re-verified); only the parenthetical line numbers are off. Low severity because it does not affect `matrix-fidelity` substance, but the spec's own "Real-corpus grounding" constraint and its repeated emphasis on reviewer-reproducible `sed -n` citations make citation precision a stated design value this fails on two of its own anchors.

---

### F3 — MINOR/MAJOR (determinism) — Two per-label applicability criteria leave matching/threshold semantics undefined, risking two-implementer divergence
**Claim:** Reviewer instructions ask whether two honest builders could produce materially different results from the same text. Two of the ten `"applicabilityCriterion"` definitions (P0-B.md, "Per-label applicability criteria" table) rely on undefined matching logic:

1. `investigational-or-unapproved`: "True when the cited evidence source's own regulatory-status field states investigational, unapproved, or off-label for the declared use, **or no approved-use record exists in the evidence source for the declared use**." The spec does not define what happens when **zero evidence sources are cited** for a declared use (an output with no evidence citation at all) — one honest builder could read "no approved-use record exists" as vacuously true (label attaches to every uncited claim), another could treat it as not-yet-determinable pending evidence (label does not attach until an evidence source is actually consulted). This changes behavior materially (A/D/D vs. not-applicable) for any output lacking a cited source.
2. `interaction-or-contraindication-signal`: "True when **a matching interaction/contraindication record exists** in the cited evidence source for the user's declared concurrent substances, medications, or conditions." No matching algorithm (exact substance-name match vs. drug-class/mechanism match vs. fuzzy match) is specified at the P0-B contract layer; the spec later references "the function's own declared evidence-source match threshold" only for the `D→E` strong-signal escalation upgrade, not for base applicability. Two builders could reasonably differ on how permissive/strict the base match is before a function even declares its own threshold.

**Evidence:** P0-B.md, "Per-label applicability criteria" table rows for `investigational-or-unapproved` and `interaction-or-contraindication-signal`.

**Smallest amendment:** Add one clause to each row: (a) for `investigational-or-unapproved`, state explicitly whether an output with zero cited evidence sources is in-scope or out-of-scope for this criterion (defer to each function's own declared evidence-citation requirement, consistent with the contract's existing "defers to that function's own missing-input ladder behavior" escape hatch); (b) for `interaction-or-contraindication-signal`, state explicitly that base-applicability matching semantics are themselves function-declared (same deferral pattern used for `acute-red-flag-or-emergency`'s "triggering criterion set is itself function-declared"), closing the ambiguity the same way the spec already closes it for the locked labels.

**Changes a locked decision?** No — both criteria are marked `[OPERATIONALIZED — bounded]`, explicitly this parcel's own content, reviewable and amendable at P0-B's own tier without reopening the owner gate (per the spec's own stated provenance-marker rule).

## Missing pieces / collisions / unknowns

- No `requiredClosureEvidence` fieldwise-fold table exists either (same gap pattern as F1), but this is consistent with P0-A's own precedent (P0-A.md also omits an explicit closure-evidence fold table), so it is not flagged as a P0-B-specific regression — noted here only so a later reviewer does not re-flag it as novel.
- `verify-p0b.ps1` does not yet exist (expected — it is a dispatch-time deliverable, not a shaping-time one); this reviewer did not evaluate script correctness since no such script exists at this commit, only the spec's description of its required checks.
- The spec's `BaseCommit` defined-term section correctly distinguishes the shaping anchor (`main@13c5c3f5...`) from the not-yet-named dispatch-time pin; this reviewer confirmed `13c5c3f5...` is a real, ancestor commit of the current spec-authoring commit (`6dae6d1`), consistent with the claim.

## Verification notes

**MATRIX FIDELITY (primary duty) — clean.** Performed a direct, line-by-line diff of the spec's reproduced "Per-label behavior matrix" (10 rows × C1/C2/C3/Calibration-required, plus the controlled-or-illegal-sourcing footnote) against `P0-B-DESIGN-GATE.md` lines 100-113 (file hash `34928ac8c54fe8af9cb7b3a01d36820fe435f2adf4f02f56c20e7cf548d57f5b`, matches the spec's cited hash exactly). **All 10 rows are byte-identical**, including the `D→E` cell for `interaction-or-contraindication-signal` and both LOCKED rows. D-B3 (numeric provenance, 4 rules), D-B4 (missing-input ladder, 3 rules), D-B5 (function-review, 4 rules), and D-B6 (escalation semantics, 4 rules) were each independently diffed against the design-gate source text and found verbatim-identical to the spec's "Deliverables" quotations. The D-B1(c) `enablementState` JSON fragment's prose content matches D-I's ruling text and the design-gate §3 "D-B1" option-(c) text; `"publiclyEnabled": false` is correctly hard-required with no softening. Preemption order text (D14 fieldwise fold + four-stage order + the two trailing sentences) matches verbatim modulo the F2 citation-line defect (text itself correct).

**No deviation from the owner's D-I ruling found.** Checked specifically for: softened/strengthened label rows (none found — exact cell-for-cell match), changed preemption order (none — stage order and both trailing sentences reproduced verbatim), altered provenance rules (none — all 4 D-B3 rules verbatim, fail-closed rule 4 intact), and any enablement state permitting `biostack-recommended` origination pre-v2.0.0 (none — `"publiclyEnabled": false` is a hard constraint with an explicit stop condition and its own `enablement-field-fidelity` check; every `C3` cell is additionally gated by `"enablementGatesC3": true`).

**Hashes independently recomputed and verified exact-match** (case differences only; content identical) for: charter (`4cd390d6...`), coordinator-decisions ledger (`908faa59...`), design-gate doc (`34928ac8...`), guidance-content-contract v1.0.0 (`60729197...`), `classification-axes.schema.json` (`49f9fcfa...`), `delivery-class-controls.json` (`e1e545cc...`), and P0-A.md (`21eae29f...`). All match the spec's cited hashes exactly.

**D-G/D-H/D-I ledger text re-verified against live file:** D-G (lines 114-123), D-H (lines 125-150), D-I (lines 152-169) citations are all exactly accurate (unlike the two internal design-gate citations in F2); the D-I block quoted verbatim in the spec's "Lineage and dependencies" matches the live ledger text exactly, word for word.

**Ten-label vocabulary closure — clean.** The ten label names and spellings in the spec's behavior matrix and applicability-criteria table match `classification-axes.schema.json`'s `substanceFunctionRisk.labels` array exactly (verified by direct extraction), and the schema's `applicabilityField` entries all correctly carry `"determinationAuthority": "deferred-to-P0-B-deterministic-criteria"` / `"status": "deferred"` as the spec claims is the P2 binding this parcel targets.

**Class-overlay completeness — mostly clean, one gap.** `requiredSpecAdditions` for all four classes (health-boundary: 9 items, privacy: 8, legal-policy: 5, knowledge-promotion: 5) are each present in the spec's "Mandatory class sections," in the schema's own order, confirmed against `delivery-class-controls.json` directly. `minimumChecks` fold in "Deterministic verification" is complete and correctly attributed per class. `additionalMergeGate` fold in "Review/gates" is complete and correctly attributed (`green-dual-review-required` + `recorded-human-approval`; privacy and knowledge-promotion correctly noted as contributing none). The one gap is `mandatoryStopConditions` (F1, above).

**Authorization boundary — stated clearly and correctly, dispatch properly gated on P0-A freeze.** The spec states, multiple times and unambiguously, that dispatch (Gate 2) is blocked pending (1) P3-A closed, (2) P0-A merged/closed with canon precedence frozen, (3) this spec's own dual review and Gate 3 merge — in that order — and that none of the three has occurred. Independently confirmed via `gh pr view 526` (this PR adds only the spec file, no dispatch artifacts) and via live file status checks: P3-A.md and P0-A.md both still read `review-candidate`/`REVIEW CANDIDATE — builder dispatch blocked`; `docs/specs/INDEX.md` carries a P3-A row (`review-candidate`) but no P0-A row at all, exactly as the spec claims.

**No-TBD compliance — clean.** Grepped the spec file for `TBD`, `TODO`, `FIXME`, `{{` — the only matches are inside the spec's own rule-definition prose describing the prohibition, not actual placeholders.

**Cannot be read as authorizing P0-C/P0-D — clean.** The "What this parcel does not close" section and the final "Stop conditions" bullet both explicitly disclaim authorization of P0-C, P3-B, P0-D, and any runtime-integration parcel; "Exact allowed surfaces" names only 6 files/paths and explicitly fences `parcel-spec.schema.json` (P3-B's job) and all P2/P3-A fixtures as frozen/untouched.

**Scope and PR contents — clean.** `gh pr view 526` confirms exactly one file added (816 lines, matching local `wc -l`), no other repository changes; consistent with "shaping and review only, no dispatch" framing.
