# p0b_re_review_2 — P0-B spec review

**Verdict:** APPROVE-WITH-FIXES
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p0b-shaping/docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md
**Spec SHA-256:** b0068ed653ff825ea1c627e7b32ceca9fca25b039dc99fb1cb555bf93b10967f
**Date:** 2026-10-09 (re-review of PR #526, commit 9b440a9, branch docs/p0b-shaping)

## Ranked findings

### F1 — BLOCKER (borderline MAJOR): `"dose-context"` scope qualifier is never deterministically defined anywhere in canon, yet is now a hard, machine-checkable gate

- **Claim:** The spec newly turns the `pregnancy-or-lactation` `C1` cell's parenthetical
  `R (dose-context)` into a required, machine-readable `"scope": "dose-context-only"` field, and
  adds a dedicated deterministic check (`qualified-cell-scope-present`) that fails if this field
  is missing or wrongly null. But the term "dose-context" (and its sibling "dosing-context," used
  for numeric-provenance origin markers and public-UX gating) is **never operationally defined**
  anywhere in this spec, `P0-B-DESIGN-GATE.md`, the charter, or
  `docs/guidance/biostack-guidance-content-contract.v1.md`. There is no deterministic test in the
  "Per-label applicability criteria" table, or anywhere else, that says what makes a given `C1`
  function invocation's output "dose-context" versus not.
- **Evidence:**
  - `parcels/P0-B.md` lines ~366-371 (Deliverables item 9, `"behavior"` bullet): introduces the
    `"scope"` field and the `"dose-context-only"` / `"prescribed-treatment-only"` literal values,
    calling the qualifier "part of the ruled table's cell value ... checkable with the same rigor
    as the bare letter."
  - `parcels/P0-B.md` line 424 and `P0-B-DESIGN-GATE.md` line 109: both carry
    `pregnancy-or-lactation | R (dose-context) | D | **R** | ...` — the only place "dose-context"
    is ever used as a scope qualifier, and it is never expanded.
  - `rg -n "dose-context|dosing-context"` across `parcels/P0-B.md`,
    `P0-B-DESIGN-GATE.md`, `CHARTER.md`, and `docs/guidance/biostack-guidance-content-contract.v1.md`
    returns only the four already-cited usages (two in each of P0-B.md/gate, one each in the
    contract and provenance sections) — zero definitions.
  - The "Per-label applicability criteria" entry for `pregnancy-or-lactation` (the only place that
    could define this) tests only pregnancy/lactation *status* declaration — it never mentions
    "dose-context" at all, so the scope boundary of the `C1` refusal is not even referenced by the
    applicability test that is supposed to drive it.
- **Why this is the loophole the task asks for:** this is precisely a case where "the owner-ruled
  matrix" says **R** for pregnancy-or-lactation `C1` scoped to "dose-context," and the contract's
  own machinery (the new `"scope"` field + `qualified-cell-scope-present` check) only verifies
  that the *label* `"dose-context-only"` is present — not that any given surface's actual
  dose-context-ness is correctly, deterministically assessed. A builder (or, later, a function
  author) can declare a dosing-related `C1` surface "not dose-context" with zero objective test to
  contradict that declaration, and the deterministic verifier as specified has nothing to check it
  against. This lets a product surface ship `pregnancy-or-lactation` + `C1` without the owner-ruled
  refusal while remaining formally "`matrix-fidelity`-passing," because the undefined term, not an
  owner ruling, is what decided the outcome.
- **Smallest amendment:** add one deterministic applicability/scope test — e.g., "a `C1` output is
  `dose-context` when its declared output includes a dose amount, frequency, concentration, or
  administration instruction for a specific substance" — to the Deliverables section (as an
  `[OPERATIONALIZED — bounded]` clause, the same pattern already used for the other applicability
  criteria), and require `qualified-cell-scope-present`/`matrix-fidelity` to check actual presence
  of dose-bearing output fields, not merely the string literal.
- **Does it change a locked decision?** No — it does not touch the ruled `R (dose-context)` cell
  value itself, only closes the operational gap in what triggers it. This is exactly the kind of
  bounded operationalization the spec's own machinery (item (b) in "Normative input and authority
  sources") already permits and requires for every other label's criterion.

### F2 — MINOR: two citation line-ranges are inaccurate (content is byte-correct; line pointers are not)

- **Claim:** Two `[RULED — verbatim]` citations give line ranges that do not bound the quoted text
  correctly, which fails the spec's own stated discipline ("Real-corpus grounding... verifiably
  present, verbatim, in `P0-B-DESIGN-GATE.md` at the cited location").
  1. `cellSemantics` (Deliverables item 10) and the "Per-label behavior matrix" intro both cite
     `P0-B-DESIGN-GATE.md` "lines 95-96" for the quoted sentence *"Cell values: **A** = allowed,
     **D** = degraded (... additions), **R** = refused, **E** = escalate (stop ordinary output,
     surface escalation)."* Verified directly: in the gate doc, that sentence actually spans lines
     **95-97** — the closing clause "output, surface escalation)" is on line 97, not 96
     (`sed -n '95,97p' docs/.../P0-B-DESIGN-GATE.md`). Both citations in `parcels/P0-B.md` (around
     line 381 and line 410) undercount by one line.
  2. `"escalationSemantics"` (Deliverables item 8) cites D-B6 as "lines 152-163," but D-B6's actual
     content (header + 4 numbered rules) ends at line 161; lines 162-163 are a blank line and the
     `---` section divider, not part of D-B6's ruled text. Every other `[RULED — verbatim]`
     citation in this spec (D-B2 table/preemption, D-B3, D-B4, D-B5) ends its range exactly at the
     last content line, making this one inconsistent with the spec's own established pattern.
- **Evidence:** `sed -n '95,97p'` and `sed -n '152,163p'` of
  `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` (both re-verified directly
  against the committed, hash-matched file; gate file hash
  `34928ac8c54fe8af9cb7b3a01d36820fe435f2adf4f02f56c20e7cf548d57f5b` matches the spec's cited
  "Design-gate decision package SHA-256").
- **Smallest amendment:** change both citations from "lines 95-96" to "lines 95-97," and the
  escalationSemantics citation from "lines 152-163" to "lines 152-161."
- **Does it change a locked decision?** No — purely a citation-accuracy fix; the quoted text
  itself is byte-correct in both cases.

### F3 — MINOR: residual ambiguity in "function-declared" matching thresholds (self-flagged, under-constrained)

- **Claim:** The `interaction-or-contraindication-signal` applicability criterion and its `D→E`
  "strong-signal" escalation trigger both explicitly defer their matching algorithm and threshold
  to "the function's own declared evidence-source match threshold," with no requirement that this
  threshold itself be reviewed, evidence-grounded, or bounded in any way by this contract. The same
  pattern is used (and explicitly cross-referenced) for `acute-red-flag-or-emergency`'s
  "function-declared" triggering-criterion set.
- **Evidence:** `parcels/P0-B.md`, "Per-label applicability criteria" table,
  `interaction-or-contraindication-signal` row, and the paragraph immediately below the table
  tying the base-applicability threshold to the `D→E` escalation threshold ("share one
  function-declared threshold, not two independently defined ones").
- **Assessment:** this is a legitimate, deliberate design choice (mirrors D15's function-specific
  review model, and the spec is honest about doing it, not hiding it) and is not itself
  inconsistent with D-I's ruling — D-B2's "escalate on strong signal" text does not itself specify
  a threshold. But it means two functions handling materially identical interaction data could
  declare different thresholds and reach different label attachments/escalations for the same
  underlying signal, with nothing in this contract to catch that divergence. This is not a defect
  P0-B's own matrix-fidelity check can close (it is a downstream, per-function concern), but it is
  a residual ambiguity reviewers and the owner should be aware doesn't resolve at this parcel.
- **Smallest amendment:** none required for this parcel's own closure; recommend a one-line note in
  "What this parcel does not close" flagging that per-function match-threshold consistency is not
  adjudicated here and is a candidate concern for the eventual runtime-integration parcel's own
  review.
- **Does it change a locked decision?** No.

## Missing pieces / collisions / unknowns

- No collision found between this spec's Deliverables and P0-C's or P3-B's stated future scope;
  "Frozen surfaces," "Exact allowed surfaces," and "Stop conditions" all explicitly fence off
  `parcel-spec.schema.json`, fixture creation, and any runtime-integration work, and repeatedly
  disclaim that P0-C/P3-B/P9/any runtime parcel is authorized by this spec's existence.
- No content found in this spec that states, implies, or could reasonably be read as claiming
  `biostack-recommended` origination or any `C3` behavior is publicly available today — the
  `enablementState.biostackRecommendedOrigination.publiclyEnabled: false` requirement is stated as
  a hard, literal-boolean constraint with its own dedicated deterministic check
  (`enablement-field-fidelity`), and "Hard constraints"/"Stop conditions" both independently
  reinforce it.
- Per-label behavior matrix (full 10-row table, C1/C2/C3 cells + calibration text) in
  `parcels/P0-B.md` lines 415-429 is byte-identical to `P0-B-DESIGN-GATE.md` lines 100-113
  (verified by direct `diff` of the extracted table text, including the footnote). No drift found
  in the ruled matrix's substantive content.
- `preemptionOrder`, `numericProvenance` (D-B3), `missingInputLadder` (D-B4),
  `functionReviewStatus` (D-B5), and `escalationSemantics` (D-B6) transcriptions were each checked
  against their cited gate-doc spans and found content-accurate (modulo the F2 line-range
  inaccuracies, which do not change the quoted text).

## Verification notes

- Computed spec SHA-256 directly: `b0068ed653ff825ea1c627e7b32ceca9fca25b039dc99fb1cb555bf93b10967f`
  — matches no value asserted inside the spec itself (the spec does not self-hash, as expected for
  an un-merged review candidate).
- Computed `P0-B-DESIGN-GATE.md` SHA-256 directly:
  `34928ac8c54fe8af9cb7b3a01d36820fe435f2adf4f02f56c20e7cf548d57f5b` — matches the spec's cited
  "Design-gate decision package SHA-256" exactly.
  `wc -l` on the gate file returns 207 lines, matching the spec's claim that §1-§6 spans lines
  23-207 "the file's actual full extent."
- Confirmed branch `docs/p0b-shaping`, commit `9b440a9ca5a968aab269ca0ad327dadb57ef77ac`
  ("docs(governed-delivery): P0-B spec fix 1 — review findings closed"), single-file diff touching
  only `parcels/P0-B.md` — consistent with this being a review-findings-closure commit, not a new
  surface expansion.
- Confirmed the ten substance/function-risk labels in the "labels" deliverable and both tables
  (behavior matrix, applicability criteria) are exactly the same ten labels, same spelling, in the
  same order both places — no vocabulary drift.
- Confirmed `enablementState` JSON fragment's five fields plus `publiclyEnabled: false` are stated
  identically in both the Deliverables section's required-fragment code block and the Hard
  constraints/Stop conditions restatements — internally consistent.
- Confirmed no `TBD`/`TODO`/`FIXME`/`{{...}}` placeholder literal appears anywhere in the spec text
  outside of the rule text that names those literals as forbidden markers (`rg` scan).
- Did not find any sentence anywhere in the spec that states or implies this parcel's own merge
  enables product behavior, advances production readiness, or authorizes a not-yet-granted parcel
  — the "What this parcel does not close" section and repeated "Authorization boundary" language
  are consistent and non-contradictory throughout.
- Did not independently re-verify the charter SHA-256, coordinator-decisions ledger whole-file
  hash, or the two P2 substrate file hashes cited in "Lineage and dependencies" (out of scope for
  this targeted re-review's five named tasks, which center on the matrix/D-B rules, enablement
  semantics, and scope boundaries); no evidence of tampering was observed in the cited values'
  internal consistency with the rest of the document.
