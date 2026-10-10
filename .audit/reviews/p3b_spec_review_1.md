# p3b_spec_review_1 — P3-B spec review

**Verdict:** APPROVE-WITH-FIXES
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p3b-shaping/docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md
**Spec SHA-256:** 7C1B3350D3C4EAAEBD5B91D53FCC7B4365AA3C0B3A700CC9138DE8DCF2C62266
**Date:** 2026-10-09

## Ranked findings

### F1 — MINOR — wrong array index cited for `escalationSemantics.rules`
**Claim:** The spec's claim that `outputType` must equal `safety-escalation` is sourced to
"D-B6 rule 3 ... per `escalationSemantics.rules[3]`" (document contract 2, `escalation` row,
"Frozen source" column).
**Evidence:** `docs/specs/schemas/product-capability-safety-contract.json`, `escalationSemantics.rules`
is a 4-element array (0-indexed): `[0]` = "acute-red-flag-or-emergency: output stops...",
`[1]` = "prescription-treatment-involved: alteration-of-treatment surfaces refuse...",
`[2]` = "Escalation is a distinct output type (class `safety-escalation`), never a footnote...",
`[3]` = "Escalation language is drawn from approved templates...". The quoted text ("Escalation
is a distinct output type ... never a footnote") is `rules[2]`, not `rules[3]`. (By contrast, the
adjacent `function_review_owner` row's citation `functionReviewStatus.rules[2]` is correctly
0-indexed and matches its quoted text exactly — confirming the array is meant to be addressed
0-indexed elsewhere in this same table, which makes this one cell's `[3]` an off-by-one error
rather than a different, consistent convention.)
**Smallest amendment:** Change `escalationSemantics.rules[3]` to `escalationSemantics.rules[2]` in
the `escalation` row's "Frozen source" cell before the spec is approved, so
`CAPABILITY-FIELD-MAP.md`'s builder-transcribed table (which the spec requires be transcribed
"verbatim ... no row's content is inventable or approximate") does not ship a wrong citation.
**Does it change a locked decision?** No — this is a citation/traceability defect only. The
deterministic verifier (checks 7/9) reads the contract live and does not gate pass/fail on this
citation string, so it has no behavioral effect, but it is still a factual error in a "transcribe
verbatim" table the spec itself holds to zero-tolerance precision.

### F2 — MINOR — `capability_claim.behavior: "escalated"` literal for `safety-escalation` guidance
class is not itself sourced to an explicit contract clause in the row's own "Live cross-reference
rule" / "Frozen source" text
**Claim:** Document contract 2's `capability_claim` row requires, for
`guidanceClass: safety-escalation` entries, `behavior` to "equal the literal `escalated`" — but no
`labels.*.behavior.{C1,C2,C3}.value` in the live contract ever equals the bare literal `escalated`
(observed values are `allowed`, `degraded`, `refused`, `refused-and-escalated`,
`degraded-escalates-on-strong-signal`). The literal `escalated` does exist in the contract, but
only as an atomic code definition in `cellSemantics.escalated` ("E — escalate..."), not as a
`labels.*.behavior.*.value`.
**Evidence:** `docs/specs/schemas/product-capability-safety-contract.json` `cellSemantics` (lines
~126-130) vs. every `labels.<label>.behavior.{C1,C2,C3}.value` (enumerated: none equal
`"escalated"` alone). The field-map row's "Frozen source" column cites only
`labels.*.behavior.*.value`, `escalationSemantics.rules`, and `enablementState...publiclyEnabled`
— it does not cite `cellSemantics.escalated` as the source of the bare `"escalated"` literal this
row requires.
**Smallest amendment:** Add `cellSemantics.escalated` to the `capability_claim` row's "Frozen
source" cell, so the literal `"escalated"` the schema requires for `safety-escalation` entries is
traceable to an explicit frozen value rather than appearing self-defined by this parcel. This is
not a product-semantic invention (the code is defined in the frozen contract), but as currently
written it is under-cited relative to the spec's own "composed, not hardcoded" / "every row's
content sourced to a frozen cell" discipline.
**Does it change a locked decision?** No.

## Missing pieces / collisions / unknowns

- None found that rise above the two MINOR items above. The spec is pre-implementation
  (`status: review-candidate`); no fixtures, `CAPABILITY-FIELD-MAP.md`, or `verify-p3b.ps1` exist
  yet in this worktree — consistent with a shaping-stage spec, not a defect.
- The `substanceFunctionRisk` axis's own `controlBindingStatus` (resolved by P0-B) ended at
  `bound-at-this-parcel` with `controlSource` left as `"none"` (not a file-path pointer), while
  P3-B's own document contract 3 sets `productGuidanceClass.controlSource` to a literal path
  (`CAPABILITY-FIELD-MAP.md`). This is an asymmetry between how the two axes' deferrals were
  closed, but it is P0-B's prior precedent, not something P3-B introduces incorrectly — flagged
  here only as an observation, not a finding.

## Verification notes

Checked and found clean:
- **Hash integrity.** Independently recomputed `sha256sum` for every file this spec cites a hash
  for and found all exact matches: charter (`4CD390D6...4EFE22`), P3-A spec
  (`4B928FBB...BA633A`), P3-A closure (`FC27D09D...C71CC7`), P0-B spec (`4A49E3D6...B414347`),
  P0-B closure (`56EFADB4...146981B`), `product-capability-safety-contract.json`
  (`020554BA...41DB7B05D`), `.md` mirror (`C01E9087...2AD1B4159BA`), `classification-axes.schema.json`
  (`24121BC2...F3A64A2B`), `delivery-class-controls.json` (`E1E545CC...B536B068`). No stale or
  wrong hash found.
- **Charter line citations.** Line 133 (`P3-B: after P0-B freezes...`) and line 152 (dependency
  spine `... -> P0-B -> P3-B -> P4 -> P6 -> P7 -> P0-C -> ...`) both verified byte-exact against
  `CHARTER.md`.
- **P0-B's next-step quote.** The exact sentence attributed to P0-B's closure record ("P3-B binds
  the parcel schema (P3-A) to the frozen contract's capability/claim/provenance/missingness/
  function-review/escalation fields; P0-C then proves the allowed/degraded/refused/escalated
  behavior with fixtures") is verbatim-present in `closures/P0-B.md`.
- **No product-allowed-output restatement.** Every live cross-reference rule in document contract
  2 (`CAPABILITY-FIELD-MAP.md`'s table) points at a field path in
  `product-capability-safety-contract.json` rather than embedding a copied matrix cell, locked
  origin, rung, or rule text; the one `extensionSections` key appended
  (`product-capability-safety-overlay`) carries `"bindsProductSemantics": false` and only names
  frontmatter keys to require, never a new allowed/degraded/refused/escalated value. No cell in
  P0-B's frozen contract is restated, paraphrased, weakened, or strengthened anywhere in this
  spec's body. No BLOCKER found on this axis.
- **`CAPABILITY-FIELD-MAP.md` field content vs. live contract.** Spot-checked every cited contract
  path: `functionReviewStatus.rules` (4 rules; rule-1/rule-3 text matches claim exactly, including
  the correctly 0-indexed `rules[2]` citation for `function_review_owner`); `numericProvenance.
  lockedOrigins` (exactly 5 entries, matches "five-value array" claim); `missingInputLadder.rungs`
  (exactly 3 rungs, rung 2 text distinguishes degrade-naming-missingness vs.
  refuse-safety-material as claimed); `preemptionOrder.stages` (stage 1 =
  `acute-red-flag-or-emergency` as claimed); `enablementState.biostackRecommendedOrigination.
  publiclyEnabled` = `false` at this `BaseCommit`, matching the "no premature public enablement"
  hard constraint and D-B1(c) citation (confirmed present in `parcels/P0-B.md`); `labels` object
  carries exactly the ten closed `substanceFunctionRisk` labels with `behavior.{C1,C2,C3}` present
  on every one, matching the C1/C2/C3 column-mapping claim. One indexing defect found (F1) and one
  under-cited-but-not-wrong literal found (F2), both MINOR.
- **Extension-point additive-only invariant (EXTENSION-POINTS.md).** Read `EXTENSION-POINTS.md`'s
  `domain-overlay-insertion` subsection in full; confirmed its pinned sentence (disjointness vs.
  `requiredSpecAdditions` union, no removal/rename/value-mutation of existing keys, named
  `extension-point-not-additive` failure mode) is semantically reproduced correctly in document
  contract 1 and check 6. Independently recomputed the live `requiredSpecAdditions` union from
  `delivery-class-controls.json` and the `SECTION-HEADING-MAP.md` normalization rule
  (lowercase; `/`/`-` → space; whitespace collapsed; trailing `s` stripped per token) and confirmed
  the normalized candidate key `product capability safety overlay` is genuinely disjoint from the
  live union — the "vacuously true but still asserted" claim is correct, not hand-waved.
- **Frontmatter self-reference carve-out.** Independently parsed `parcel-spec.schema.json`'s
  `requiredFrontmatterKeysCommonToBothShapes` (9 keys: `title, status, owner, created, updated,
  delivery_classes, guidance_classes, substance_function_risk, surfaces`) plus `parcel_id`, and
  confirmed this spec's own leading YAML frontmatter contains exactly those 10 keys and no others
  — none of the six new bound fields (`function_review_status`, etc.) appear in this file's own
  frontmatter, consistent with the narrowly-scoped carve-out claim and with `guidance_classes: []`
  / `substance_function_risk: []` genuinely not triggering any of the five conditional bound keys.
- **P3-A carry-over item 3.** Read `parcels/P3-A.md`'s "Carry-over" section item 3 in full; the
  quoted disposition text ("P3-B's own dispatch must either (a) include a fixture or
  compatibility-set row exercising the `coordinator-parcel` branch against its own conforming spec
  file, or (b) explicitly re-affirm this gap's continuation with a named reason") is verbatim.
  P3-B's chosen discharge (option (a), via `positive-coordinator-parcel-p3b-self.json` pointing at
  this parcel's own real, merged file) is a legitimate, concrete discharge of that carry-over, not
  an evasion.
- **P0-C scope boundary.** Read the charter's P0 parcel-tree entries and dependency spine; P0-C is
  explicitly "policy fixtures proving the P0-B allowed, degraded, refused, and escalated behavior
  and that useful guidance survives enforcement" — distinct from P3-B's "bind the schema" role.
  P3-B's spec repeatedly and explicitly excludes proving runtime achievability or guidance survival
  under enforcement (Objective scope-boundary paragraph, Frozen surfaces' final bullet, Stop
  conditions) and ships zero fixtures that assert anything about actual runtime behavior — only
  schema/field presence and live-value agreement. No P0-C encroachment found.
- **Authorization boundary / standing-authorization accuracy.** Confirmed in `CHARTER.md`'s
  "Standing authorizations" section that P0 (unlike P1-P7) is explicitly excluded from inferred
  standing authorization ("No standing dispatch or merge authorization is inferred for P0, P8,
  P9..."). P3-B is parcel-tree item 3 (P3-B), not a P0 subparcel, so the charter's P1-P7 standing
  grant legitimately covers it provided the listed contingencies hold (approved spec, named
  branch/worktree, dispatch within named surfaces, all checks/reviews green) — which this spec's
  own "Standing authorization and tripwire" section correctly restates and gates on. No
  overclaiming of authorization found.
- **Class-overlay / registry mechanics.** `classification-axes.schema.json`'s
  `axes.productGuidanceClass` currently has `controlSource: "none"` and
  `controlBindingStatus: "deferred-to-P3-B"` exactly as claimed as the pre-state; the two-field,
  scoped flip this parcel proposes (to `CAPABILITY-FIELD-MAP.md` path and
  `bound-at-this-parcel`) is a narrow, correctly-scoped mutation that leaves `applicabilityField`,
  `deliveryClass`, and `substanceFunctionRisk` axis objects untouched per the claim.
  `parcel-spec.schema.json` has exactly 15 top-level keys as claimed, with `extensionSections: {}`
  currently empty, confirming the "14 keys byte-identical, one key appended" mechanic is accurate
  against the present baseline.
- **No `TBD`/placeholder.** Grepped the full spec body for `TBD`, `TODO`, `FIXME`, and `{{`
  interpolation syntax — zero matches.
- **Determinism.** `CAPABILITY-FIELD-MAP.md`'s table content, the nine fixtures' exact
  `syntheticSpec`/`expected` shapes, and `verify-p3b.ps1`'s 13 numbered checks are specified with
  concrete, byte-exact expected values (not ranges or vibes) — two independent honest builders
  reading this spec and the frozen contract would produce byte-identical
  `CAPABILITY-FIELD-MAP.md`/`extensionSections` content and functionally equivalent fixtures/
  verifier logic. No material ambiguity found beyond the two MINOR citation issues above.
- **Pre-implementation state.** Confirmed via `git log`/`git status` in the shaping worktree that
  no `fixtures/p3b/`, `CAPABILITY-FIELD-MAP.md`, or `verify-p3b.ps1` exist yet — this is purely a
  shaping-stage spec under review, not an already-built artifact being retroactively justified.
