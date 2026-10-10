# p3b_spec_review_2 — P3-B spec review

**Verdict:** APPROVE-WITH-FIXES
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p3b-shaping/docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md
**Spec SHA-256:** 7C1B3350D3C4EAAEBD5B91D53FCC7B4365AA3C0B3A700CC9138DE8DCF2C62266
**Date:** 2026-10-09

## Ranked findings

### F1 — BLOCKER — Deterministic check 5 pins a factually wrong field count for the `extensionSections` entry

**Claim:** Check 5 of "Deterministic verification" is internally inconsistent with document
contract 1 and therefore not implementable as literally written, breaking the "Deterministic,
without network access or non-PowerShell dependencies" guarantee the spec claims for itself.

**Evidence:** Document contract 1 (parcel body, the `product-capability-safety-overlay` JSON
block) pins exactly **six** top-level keys: `addedBy`, `appliesToShapes`, `bindsProductSemantics`,
`contractSource`, `fieldMapSource`, `boundFrontmatterKeys`. Check 5 (Deterministic verification,
item 5) reads: *"require `extensionSections` to equal exactly
`{"product-capability-safety-overlay": {...}}` with **the nine fields** pinned in document
contract 1, in the exact values pinned there."* There is no reading of document contract 1 that
yields nine fields — six top-level keys, or six keys plus the six-element `boundFrontmatterKeys`
array counted item-by-item (twelve), or any other natural count, none of which is nine. A builder
implementing `verify-p3b.ps1` literally from this sentence must either (a) invent three phantom
keys to make the count match the spec text (a product-semantic/schema invention this parcel's own
hard constraints forbid), (b) silently ignore the "nine" and check the real six (the only correct
behavior, but not what the text says), or (c) fail every correct builder submission because the
written check can never be satisfied by a spec-compliant six-key object. Two honest implementers
reading only this sentence (without cross-checking document contract 1 line-by-line) will diverge.

**Smallest amendment:** In "Deterministic verification," item 5, change "the nine fields pinned in
document contract 1" to "the six fields pinned in document contract 1."

**Does it change a locked decision?** No — it is a textual correction to match the spec's own
already-pinned JSON block; it does not touch P0-B's frozen contract or any ruled cell.

### F2 — BLOCKER — `escalation.preemptionStage` cross-reference is indeterminate for `interaction-or-contraindication-signal`'s own escalating case, and no fixture exercises it

**Claim:** `CAPABILITY-FIELD-MAP.md`'s `escalation` row requires `preemptionStage` to "correctly
place every escalating `capability_claim` label in its live `preemptionOrder` stage," but the
frozen contract (`product-capability-safety-contract.json`, `preemptionOrder.stages`) assigns no
stage number to the label `interaction-or-contraindication-signal` at all — and this is exactly
the label whose own C3 cell (`degraded-escalates-on-strong-signal`) is one of the `escalation`
row's named trigger conditions.

**Evidence:**
- `product-capability-safety-contract.json`, `preemptionOrder.stages`: stage 1 =
  `["acute-red-flag-or-emergency"]`, stage 2 = `["controlled-or-illegal-sourcing"]`, stage 3 =
  `["minor-or-age-uncertain", "pregnancy-or-lactation", "prescription-treatment-involved"]`, stage
  4 = `[]` (role text: `"calibrating labels union their obligations"`). `interaction-or-
  contraindication-signal` appears in **none** of the four stages' `labels` arrays, including
  stage 4's (which is the literal empty array `[]`, not a named catch-all list).
- P3-B.md, `CAPABILITY-FIELD-MAP.md` document-contract-2 table, `escalation` row, "Required when"
  column, explicitly lists `degraded-escalates-on-strong-signal` as a trigger behavior — the exact
  literal `labels.interaction-or-contraindication-signal.behavior.C3.value` carries in the frozen
  contract. The same row's "Live cross-reference rule" column requires `preemptionStage` to
  "correctly place every escalating `capability_claim` label in its live `preemptionOrder` stage."
  There is no deterministic live value to place this label against.
- None of the nine required fixtures exercises this case: `positive-capability-bearing.json` uses
  `interaction-or-contraindication-signal` only at its non-escalating `degraded` (C2) value;
  `negative-escalation-missing.json` uses `acute-red-flag-or-emergency` (unambiguously stage 1).
  The gap is therefore invisible to both the deterministic verifier and to review unless a
  reviewer independently re-derives every label's stage membership against the live contract (this
  review did).
- This is precisely the scenario P3-B's own Stop Conditions name: *"A fixture's expected result
  cannot be derived deterministically from a live read of `product-capability-safety-contract.json`
  without a human policy call."* As currently drafted, the spec does not stop for it, does not name
  it, and ships a required-field rule a builder cannot satisfy without inventing a stage assignment
  the frozen contract does not make — which would itself violate AC-P3B-07 ("no product-semantic
  invention") and the Hard-constraints bullet forbidding any new applicability/behavior rule beyond
  what the contract already freezes.

**Smallest amendment:** Either (a) add a tenth fixture (or amend an existing one) exercising
`interaction-or-contraindication-signal`'s escalating case and have the field-map row state
explicitly what `preemptionStage` value is required when a label has no named stage (e.g., "stage
4, the calibrating-labels default, applies to any label absent from stages 1-3's explicit lists"),
sourced from the contract's own stage-4 role text — or (b) invoke the Stop-Conditions clause now
and name this as a carried-forward, bounded gap exactly as P3-A did for the `coordinator-parcel`
shape, rather than silently shipping an indeterminate required check.

**Does it change a locked decision?** No — it does not ask to change the frozen contract; it asks
the spec to either resolve or explicitly name an ambiguity the frozen contract itself leaves open.

### F3 — MAJOR — `numeric_provenance`'s trigger condition is narrower than the Objective's own prose promise, risking a vacuous provenance binding for `curated-evidence-guidance` dose-context outputs

**Claim:** The Objective section states this parcel binds "(3) each numeric output's provenance
origin" without qualification. `CAPABILITY-FIELD-MAP.md`'s actual `numeric_provenance` row
triggers only when "at least one `capability_claim` entry's `guidanceClass` is
`deterministic-calculation` or `personalized-protocol-recommendation`" — `curated-evidence-
guidance` (C2) is excluded from the trigger entirely. But the frozen contract's own
`numericProvenance` rules apply to "every displayed number," and `doseContextDefinition` defines
dose-context by the *shape of the output value* (a compound amount/dose figure), not by which
guidance class produced it — a `curated-evidence-guidance` function that cites a literature dose
range (a plausible, common C2 output) can carry a dose-context numeric output with no
`numeric_provenance` requirement under this field map, because its `guidanceClass` is neither of
the two named triggers.

**Evidence:** Objective paragraph, item 3 ("each numeric output's provenance origin"); P0-B
contract, `numericProvenance.rules` ("Every displayed number carries a machine-readable origin...
Missing provenance → the numeric output is refused (fail-closed)"); `doseContextDefinition` (keyed
to value shape, not guidance class); CAPABILITY-FIELD-MAP `numeric_provenance` row's "Required
when" column (two-guidanceClass trigger only).

**Smallest amendment:** Either narrow the Objective's prose to match the actual, narrower trigger
("each numeric output's provenance origin, for functions declaring a `deterministic-calculation`
or `personalized-protocol-recommendation` capability"), or broaden `CAPABILITY-FIELD-MAP.md`'s
trigger to also cover any `curated-evidence-guidance` entry whose function declares a dose-context
output. Either direction removes the ambiguity; leaving both as currently written invites two
honest implementers (and two honest reviewers) to disagree about whether a C2-only, dose-bearing
spec needs `numeric_provenance`.

**Does it change a locked decision?** No — this is a request to align P3-B's own prose/trigger
scoping with itself; it proposes no new or altered contract cell.

### F4 — MINOR — Escalation trigger's label-only clause may over-trigger relative to the contract's own scoping

**Claim:** The `escalation` row's "Required when" clause fires for *any* `capability_claim` entry
naming label `prescription-treatment-involved`, regardless of `guidanceClass`/behavior — including
C1/C2 entries whose behavior is merely `degraded` (not refused or escalated). The contract's own
`escalationSemantics` text ties the escalation template specifically to the "alteration-of-
treatment" (C3, `prescribed-treatment-only` scope) case. Requiring the `escalation` object
unconditionally whenever the label is merely present (even at C1/C2 `degraded`) is conservative,
not a weakening, but it is one more place where CAPABILITY-FIELD-MAP's own trigger text is broader
than the plain reading of the contract text it cites, and is worth a one-line clarifying note so a
builder does not read it as a drafting slip (as this review initially did, until re-checking
against F1/F2/F3's pattern) and narrow it to match the contract on their own initiative.

**Smallest amendment:** Add one clause to the `escalation` row's "Live cross-reference rule"
column: "this trigger clause is deliberately conservative and does not require the behavior column
to also equal `refused`/`escalated`/etc. — label presence alone is sufficient and intentional."

**Does it change a locked decision?** No.

## Missing pieces / collisions / unknowns

- No collision found between the new `product-capability-safety-overlay` extension-point key and
  the live `delivery-class-controls.json` `requiredSpecAdditions` union after normalization
  (verified: none of the eight classes' terms normalize to `product capability safety overlay`).
- `classification-axes.schema.json`'s `productGuidanceClass` axis currently reads `controlSource:
  "none"` / `controlBindingStatus: "deferred-to-P3-B"` exactly as the spec claims (verified live);
  the parallel `substanceFunctionRisk` axis already reads `controlBindingStatus:
  "bound-at-this-parcel"` (resolved by P0-B), consistent with the spec's "now-resolved" framing.
- All nine cited SHA-256 hashes (charter, P3-A spec/closure, P0-B spec/closure, contract .json/.md,
  classification-axes schema, delivery-class-controls schema) were independently recomputed from
  the live worktree and match the spec's citations byte-for-byte — **no stale hash found**.
- `parcel-spec.schema.json`'s 15 top-level keys, its `requiredFrontmatterKeysCommonToBothShapes`
  (9 keys), and the `coordinator-parcel` shape's `appliesFrom: "P3-B.md-forward"` clause were all
  independently verified against the live file and match the spec's claims; P3-B.md's own
  frontmatter does carry exactly those 9 keys plus `parcel_id`, confirming the self-reference
  carve-out's factual premise.
- The C1/C2/C3 ↔ `deterministic-calculation`/`curated-evidence-guidance`/`personalized-protocol-
  recommendation` mapping CAPABILITY-FIELD-MAP relies on is already frozen in the contract's own
  prose (`.md` lines 204-208), not invented by P3-B — no scope creep there.
- No evidence of scope creep into P0-C's territory: the spec's Objective, Frozen surfaces, and Stop
  Conditions all explicitly and repeatedly carve out "proving the behavior is achievable at
  runtime" as P0-C's exclusive, separately-gated territory, and no deliverable or fixture in this
  spec attempts runtime behavior proof.
- No evidence this parcel claims authorization it was not granted: the Authorization-boundary
  section's "no new owner design-gate ruling required" argument is consistent with the charter's
  P1-P7 standing authorization and the fact that every bound value is checked, live, against
  P0-B's already-owner-ruled contract — I found no new allowed-output decision smuggled in.
- Vacuous-binding check: the `boundFrontmatterKeys` are real, closed-vocabulary, live-cross-checked
  fields (not decorative) and the negative fixtures (`negative-capability-claim-missing.json`,
  `negative-claim-behavior-drift.json`, `negative-numeric-provenance-missing.json`,
  `negative-escalation-missing.json`, `negative-function-review-status-invalid.json`,
  `negative-premature-public-enablement-claim.json`) each target one concrete, falsifiable
  violation — this is not a vacuous binding in its common case, but F2/F3 above identify two
  specific sub-cases where the binding's coverage has a real hole.

## Verification notes

- Recomputed SHA-256 for all nine cited frozen-surface files directly from the worktree; all
  matched the spec's citations exactly (command: `sha256sum <path>`, uppercased).
- Parsed `parcel-spec.schema.json` live and counted/diffed its top-level keys, `extensionSections`
  (confirmed empty `{}` at this `BaseCommit`), `requiredFrontmatterKeysCommonToBothShapes`, and the
  `coordinator-parcel` shape definition — all match the spec's claims.
- Parsed `classification-axes.schema.json` live and confirmed both axes' current `controlSource`/
  `controlBindingStatus` values match the spec's "before" state claims exactly.
- Parsed `delivery-class-controls.json` live and computed the `requiredSpecAdditions` union by hand
  to check the additive-only, non-collision invariant (check 6) — no collision.
- Read `product-capability-safety-contract.json` and `.md` in full for `preemptionOrder`,
  `numericProvenance`, `missingInputLadder`, `functionReviewStatus`, `escalationSemantics`, and
  every `labels.*` entry, to independently re-derive the `CAPABILITY-FIELD-MAP.md` table's claimed
  live cross-reference rules rather than trust the spec's own description — this is how F2 and F3
  were found.
- Confirmed P3-A's own "Carry-over" item 3 and "Frozen surfaces" final bullet text verbatim against
  the live `parcels/P3-A.md` file; P3-B's citations of both are accurate.
- Confirmed `docs/specs/README.md`, `docs/specs/INDEX.md`, `SECTION-HEADING-MAP.md`,
  `EXTENSION-POINTS.md`, `fold-engine.md`, `routing-output.schema.json`, `AXIS-REGRESSION-MAP.md`
  all exist at the claimed paths (registry/substrate pre-existence claim is accurate).
- Did not read any other reviewer's output file, per instructions.
