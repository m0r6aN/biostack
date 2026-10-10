# p4_spec_review_2 — P4 spec review

**Verdict:** APPROVE-WITH-FIXES
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p4-shaping/docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md
**Spec SHA-256:** f38c81fa61970e0656f8bc5ee1d2d8e53bbb74b5d90c1eb5bae88cef082cab26
**Date:** 2026-10-09

## Ranked findings

### F1 — BLOCKER — "Composed, not hardcoded" (AC-P4-02) has zero deterministic/mechanical check; it is reviewer-judgment only
**Claim:** The parcel's single most load-bearing anti-gaming guarantee — that `validate-spec.ps1`
reads every term set, control value, and contract cell **live, from disk, at validation time**,
never hardcoding a copy — is enforced nowhere in the ten numbered `verify-p4.ps1` checks. It is
listed in the acceptance-to-evidence map as reviewer-scan only.
**Evidence:** P4.md "Hard constraints" ("Composed, not hardcoded... a validator that hardcodes a
copy of any term set, control value, or contract cell fails review"); acceptance-to-evidence map
row `AC-P4-02 | linter-structural-check.json plus reviewer scan of validate-spec.ps1`. None of the
ten `verify-p4.ps1` checks (Deterministic verification, checks 1-10) perturbs a live source file
(a throwaway copy of `classification-axes.schema.json`, `delivery-class-controls.json`,
`CAPABILITY-FIELD-MAP.md`, or `product-capability-safety-contract.json` with one term/value
changed) and re-runs a fixture to confirm the linter's output tracks the change rather than a
cached/embedded constant. A builder can satisfy every fixture (fixed-point comparisons only) with
a fully hardcoded copy of today's term sets and pass all ten `verify-p4.ps1` checks, all nine
AC items with mechanical evidence, and still violate the parcel's defining "dependency-light,
general-purpose" promise — detectable only if a human reviewer happens to read every line of
`validate-spec.ps1` and notice the absence of a live read. This initiative's own history
(`closures/P0-B.md`: "blocklist marketed as paraphrase-proof" / "ruled text unpinned" failures,
caught only by adversarial GLM re-verification after a mechanical check missed them) is direct
precedent for exactly this failure mode recurring here.
**Smallest amendment:** Add an eleventh `verify-p4.ps1` check: in a scratch copy of the repo (or a
temp-file overlay), mutate one live value in each of the five composed sources (e.g. flip
`delivery-class-controls.json`'s `standard.reviewers` from `1` to `9`, add a throwaway label to
`classification-axes.schema.json`, add a throwaway row to `CAPABILITY-FIELD-MAP.md`, flip one
`product-capability-safety-contract.json` behavior cell, add a throwaway required-section term to
`SECTION-HEADING-MAP.md`), invoke `validate-spec.ps1` against a synthetic fixture that exercises
that exact value, and require the output to reflect the mutated value — failing by name
(`hardcoded-value-detected`) if the output still matches the pre-mutation value.
**Changes a locked decision?** No — strengthens an existing AC, invents no new scope.

### F2 — BLOCKER — No fixture or positive-synthetic case proves a genuinely *triggered and satisfied* P3-B capability-field binding; the only such proof is an opaque, single-real-file cross-check
**Claim:** Both synthetic "positive" fixtures (`positive-ticket-spec-standard-single.json`,
`positive-coordinator-parcel-multilabel-fold.json`) deliberately declare `guidance_classes: []`
and `substance_function_risk: []`, so none of CAPABILITY-FIELD-MAP's five conditional bound
fields ever trigger in either — stage 7 of the validator is exercised only in its *vacuous*
(not-triggered) branch by every author-controlled fixture. The sole case that exercises a
*triggered-and-passing* capability binding (`capability_claim`, `missingness`, `numeric_provenance`,
`escalation` all resolved and agreeing with the frozen contract) is
`positive-real-spec-p3b-cross-check.json`, which reads the real `parcels/P3-B.md` file and is
graded solely by agreement with `verify-p3b.ps1`'s own check 9 (check 7 of `verify-p4.ps1`).
**Evidence:** Required document contract 2's ten fixture descriptions (P4.md, "### Required
document contract 2"); none of the nine non-real-file fixtures declares a non-empty
`guidance_classes`/`substance_function_risk` *and* a correctly-passing `capability_claim` —
`negative-capability-field-missing-on-trigger.json` is the only other fixture touching the
trigger, and it is a *failure* case (field omitted).
**Why this is an evasion vector:** A builder's `validate-spec.ps1` implementation could special-
case (pattern-match on `specPath == "docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md"`)
or otherwise delegate to a cached/derived answer for that one path, satisfying check 7's
field-for-field agreement with `verify-p3b.ps1` without the stage-7 binding logic being correct,
or even present, for any other real or synthetic triggered-and-passing case. No fixture would
catch this because no fixture besides the real-file one ever exercises the success path.
**Smallest amendment:** Add one eleventh fixture,
`positive-synthetic-capability-claim-satisfied.json`, with a fully-triggered, fully-correct,
independently-authored `capability_claim`/`missingness`/`numeric_provenance`/`escalation` set
(distinct label/class pair from any already used) and `expected.result: "valid"`,
`expected.boundFieldsChecked` naming every row pass. This closes the single-point-of-truth gap
without expanding scope.
**Changes a locked decision?** No.

### F3 — MAJOR — Missing negative fixture for the homoglyph/confusables placeholder-evasion class, the exact class this initiative already found and fixed once
**Claim:** P4 ships exactly one placeholder-defeat fixture
(`negative-placeholder-entity-disguised-multilabel.json`), exercising only
`decode-html-entities` (pipeline step 6 of 9). It exercises none of
`strip-unicode-category-Cf-and-Cc` (zero-width), `nfkc-normalize` (fullwidth), or
`unicode-confusables-skeleton` (homoglyph) — step 9, the exact step P3-B's own remediation added
specifically to kill "homoglyph-TBD evasion" (`closures/P3-B.md`: "unicode-confusables-skeleton
step added (homoglyph-TBD evasion dead)"; `closures/P3-B.md` coordinator-reproduction row: "all
three adversarial reproductions (homoglyph TBD, broken escalation binding, vacuous
function-review) confirmed fixed"). P4's own fixture description explicitly states this
parcel's placeholder pipeline is "this parcel's own, independent implementation of the pipeline
(not merely trusted from P3-A's own fixture)" — i.e. P4 is re-implementing the exact code path
that previously regressed on this exact evasion class, with zero regression coverage for it.
**Evidence:** `parcel-spec.schema.json` `placeholderNormalizationSteps` (9 steps, verified live,
step 9 = `unicode-confusables-skeleton`); `closures/P3-B.md` lines quoted above; P4.md Required
document contract 2's single placeholder fixture only tests step 6.
**Smallest amendment:** Add one fixture,
`negative-placeholder-homoglyph-confusables.json` (Cyrillic/Latin lookalike `TВD`-style skeleton
match, single-violation, otherwise clean), `expected.reason: "placeholder-violation"`. This is a
direct, cheap regression test against a previously-exploited, previously-fixed evasion class and
belongs in any "composes, never rediscovers" parcel per its own stated discipline
(Lineage section: "this spec inherits both by composition..., not by rediscovery").
**Changes a locked decision?** No — adds one fixture within the already-declared pipeline scope.

### F4 — MAJOR — No empty/malformed-input resilience fixture, despite this exact crash class being previously found and fixed in this initiative
**Claim:** P0-A's remediation record (`closures/P0-A.md`) explicitly names "verifier empty-diff
crash fixed (literal-BaseCommit pinning + empty-content sentinels)" as a hardening lesson, and
names it as carrying forward to future verifier-template work. P4 builds the first
*general-purpose*, arbitrary-file-reading linter in this initiative (`-SpecPath` against any
repo file; `-SyntheticSpecJson` against arbitrary caller-supplied JSON), which is exactly the
surface most exposed to empty/malformed-input crashes, yet none of the ten mandated fixtures
tests an empty frontmatter object, an empty `headings: []` array, a zero-byte `-SpecPath` target,
or a `-SyntheticSpecJson` file missing the `frontmatter`/`headings` keys entirely. `verify-p4.ps1`
also has no check requiring `validate-spec.ps1` to fail closed (non-crashing, deterministic
`"invalid"`/`"unrecognized-shape"` result, nonzero exit) on such input.
**Evidence:** `closures/P0-A.md` line 10 (quoted above); Required document contract 2's ten
fixtures (none covers this class); Deterministic verification checks 1-10 (none covers this
class).
**Smallest amendment:** Add one fixture exercising a `-SyntheticSpecJson` payload with an empty
`frontmatter: {}` and empty `headings: []`, `expected.result: "invalid"`,
`expected.reason: "missing-required-frontmatter-key"` (naming every required key), and add a
`verify-p4.ps1` check asserting `validate-spec.ps1` exits deterministically (no stack trace / no
PowerShell terminating error) on that input.
**Changes a locked decision?** No.

### F5 — MAJOR — Internal contradiction: Hard constraints claim "no new named failure reason" while check 7 explicitly introduces one
**Claim:** The Hard constraints section states, without qualification: "this parcel invents no
new allowed/degraded/refused/escalated behavior... and introduces no new named failure reason
beyond the [eleven reasons listed]." Deterministic verification check 7 then explicitly
introduces `general-linter-parcel-verifier-disagreement` and self-describes it: "the one check
this parcel ships whose failure name is new, because no prior parcel's verifier ever
cross-checked itself against another parcel's verifier on the same real file."
**Evidence:** P4.md "Hard constraints" bullet 1; P4.md Deterministic verification check 7.
**Smallest amendment:** Narrow the Hard constraints sentence to scope it explicitly to
`validate-spec.ps1`'s own output `reason` field (the pinned seven/eleven reasons), and
explicitly except `verify-p4.ps1`'s own internal check-failure names (which are allowed to be
parcel-local, exactly as every prior `verify-p*.ps1`'s own numbered-check failure text already
is) — removing the absolute, unscoped "no new named failure reason" claim that check 7
contradicts on its face.
**Changes a locked decision?** No — clarifies wording only.

### F6 — MINOR — AC-P4-03 mis-labels the fold-stop fixture as a "missing-field" case
**Claim:** AC-P4-03 groups `negative-incompatible-controls-scalar-conflict.json` among "three
missing-field cases," but that fixture's mechanism is a scalar-conflict fold stop
(`incompatible-controls`), not a missing field. This is a taxonomy looseness that could mislead a
reviewer skimming the AC list for missing-field coverage.
**Evidence:** P4.md Acceptance criteria, AC-P4-03: "the three missing-field cases
(`negative-missing-required-frontmatter-field.json`, `negative-capability-field-missing-on-trigger.json`,
and the fold-stop case `negative-incompatible-controls-scalar-conflict.json`...)".
**Smallest amendment:** Reword to "two missing-field cases... plus the fold-stop case..." instead
of grouping all three under "missing-field."
**Changes a locked decision?** No.

### F7 — MINOR — Placeholder-scan exclusion scoping (check 8) is described loosely enough to admit smuggling
**Claim:** Check 8 excludes "the one `input.syntheticSpec` payload string each relevant fixture
deliberately carries" from the placeholder scan, but the spec does not pin *how* the excluded
span is mechanically identified (byte range, JSON-pointer path, exact string match against a
pinned literal) — only prose intent. A sufficiently motivated builder could interpret "the one...
payload string" broadly enough to exclude more of a fixture file than intended, hiding a second,
undeclared placeholder inside the same fixture under the same exclusion.
**Evidence:** P4.md Deterministic verification check 8: "...is scoped to exclude the one
`input.syntheticSpec` payload string each relevant fixture deliberately carries, exactly as every
prior parcel's own fixture-vs-file placeholder-scan scoping already works" — no cross-reference
to a specific prior-parcel mechanism definition for exact scoping.
**Smallest amendment:** Pin the exclusion to the specific JSON key path(s)
(`input.syntheticSpec.frontmatter.<field>` or `input.syntheticSpec.headings[n]` — whichever field
each fixture's deliberate violation lives in) named explicitly per fixture, rather than "the one...
payload string" left to builder interpretation; require the scan to still run, unexcluded, over
every other key in the same fixture file (including `expected.*`).
**Changes a locked decision?** No.

## Missing pieces / collisions / unknowns

- No fixture or check exercises a **spliced-quote** or **merged-clause** evasion against the
  byte-for-byte `behavior` comparison in `capability_claim` cross-reference (e.g. trailing
  whitespace, case variance, or a compound string concatenating two valid cell values). The
  live-comparison rule in `CAPABILITY-FIELD-MAP.md` is "byte-for-byte," which is a strong design,
  but P4 never fixture-proves that near-miss variants are correctly rejected rather than
  fuzzy-matched by an over-permissive comparator. Recommend one additional negative fixture
  (near-miss case variance or trailing whitespace on an otherwise-correct `behavior` value).
- The spec's lineage section cites P3-B, P3-A, P0-B, and P2 as inherited sources but never cites
  `closures/P0-A.md` directly, even though P0-A's closure record is the origin of the
  empty-content-crash and quote-splice hardening lessons (F4, above) and explicitly says they
  "travel with the verifier-template notes... recorded for P4/P6/P7 verifier work" (per
  `closures/P0-A.md`'s own carry-over line, itself slightly inconsistent with what
  `closures/P3-A.md` actually contains — a pre-existing minor documentation gap in this
  initiative's own closure chain, not introduced by P4, but P4 does not close it either).
- No scope creep into P6/P7/P0-C found: the three "load-bearing exclusions," the Frozen surfaces
  list, and the Standing authorization/tripwire section are thorough and internally consistent
  with the charter's stated boundaries for those parcels. The one borderline item is check 7's
  cross-verifier-agreement mechanic (F5) — it compares two *tools*, not two *reviewers*, so it is
  not actually P7 territory, but the spec's own wording ("the one check... whose failure name is
  new") invites exactly this scrutiny and should be tightened (see F5).

## Verification notes

- Computed SHA-256 of the spec file directly; recorded above.
- Verified all nine inline hash citations (charter, P3-B spec, P3-B closure, P3-A spec, P0-B spec,
  `product-capability-safety-contract.json`, `.md`, `classification-axes.schema.json`,
  `delivery-class-controls.json`) against `sha256sum` of the actual files in the worktree — **all
  nine match exactly, no stale hashes found.**
- Verified charter line 134 and line 152 quotes byte-for-byte against `CHARTER.md` — exact match,
  correct line numbers.
- Verified charter D8 quote against `CHARTER.md` line 93 — exact match.
- Verified `classification-axes.schema.json`'s `deliveryClass` label set excludes `architecture`
  (confirms the spec's claim that "architecture" is a charter-tree designation, not a
  `deliveryClass` label) and confirmed `standard`/`health-boundary`'s control rows
  (`requiredSpecAdditions` counts of 6 and 9 respectively, union = 15 distinct terms, no overlap;
  `reviewers` max-scalar = 2) against `delivery-class-controls.json` — matches the
  `positive-coordinator-parcel-multilabel-fold.json` fixture's claimed `expected` values exactly.
- Verified `product-capability-safety-contract.json`'s `labels.minor-or-age-uncertain.behavior.C2.value`
  is `"degraded"` (not `"allowed"`) — matches `negative-capability-claim-drift-referential.json`'s
  described drift exactly.
- Verified `parcel-spec.schema.json`'s nine-step `placeholderNormalizationSteps` pipeline order
  and `noPlaceholderPatterns` content — matches spec's description, and confirmed the
  under-tested step (`unicode-confusables-skeleton`, step 9) is real (F3).
- Verified `fold-engine.md`'s `testOnlyScalarField`/`fixtureOnly` synthetic-conflict mechanism
  against the actual P2 fixture `fixtures/negative-synthetic-scalar-conflict.json` — field names
  (`fixtureOnly`, `testOnlyScalarField`, `testOnlyScalarValues`) match exactly.
- Verified `CAPABILITY-FIELD-MAP.md`'s field-map table content (six rows, trigger conditions,
  live cross-reference rules) against the actual file — matches spec's paraphrased descriptions;
  no misrepresentation found.
- Verified the frontmatter of P4.md itself carries every `requiredFrontmatterKeysCommonToBothShapes`
  key plus `parcel_id`, and `function_review_status: not-applicable` — structurally compliant with
  its own stated rule.
- Did not find any change to a frozen artifact, any claim or consumption of authorization the
  spec's gate does not grant, or any touch of a product allowed-output decision.
- Did not read any other reviewer's output file, per instructions.
