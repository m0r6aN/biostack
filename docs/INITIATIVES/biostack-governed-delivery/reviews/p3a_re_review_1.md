# p3a_re_review_1 — P3-A spec review

**Verdict:** REJECT
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p3a-rework-3/docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md
**Spec SHA-256:** 70792a4da439626ca12461779b38690daa5fca7348b46d6d361c8b9d622d0595
**Date:** 2026-10-10

## Triage checklist (TRIAGE-2026-10-08.md, P3-A dispositions)

| Row | Disposition required | Verdict here |
|---|---|---|
| R1-F1 schema 14-vs-15 key contradiction | pin 15 keys incl. `extensionSections: {}`; check 5 checks values not presence | **CLOSED** — verified |
| R1-F2 entity-splitting bypass | extend normalization + negative fixture | **NOT GENUINELY CLOSED** — see F1 below |
| R1-F3 bipartite matching under-specified | pin deterministic resolution | **NOT GENUINELY CLOSED** — see F2 below |
| R1-F4 + R2-F6 `BaseCommit` staleness | state anchor pinned at Gate 2, not shaping time | **CLOSED** — verified |
| R1-F5 + R2-F4 `EXTENSION-POINTS.md` P3-B-adjacent example | reword to neutral label | **CLOSED** — verified |
| R2-F1 disguised-`TBD` unprovable/unenforced | add enforcement+fixture, or narrow absolute claim | **PARTIALLY CLOSED** — see F3 below |
| R2-F2 template content-quality obligations uncheckable | make checkable | **CLOSED** — verified |
| R2-F3 frozen-artifact citation error (`coordinator-assigns-at-gate-2`) | correct citation | **CLOSED** — verified |
| R2-F5 snake_case mapping misattribution | correct attribution | **CLOSED** — verified |
| R2-F7 normalization over-strip (`*`/`_`/`\`) | scope to placeholder context | **CLOSED** — verified |

## Ranked findings

### F1 — BLOCKER — The reworked `negative-tbd-violation.json` fixture cannot actually prove the new `decode-html-entities` step works, and the Hard-constraints section asserts fixture coverage (a markup-splitting case) that document contract 6 does not define — reopens R1-F2

**Claim:** The rework's headline fix for R1-F2 (HTML-entity-splitting bypass, originally BLOCKER)
is to add a ninth `decode-html-entities` normalization step and extend the existing
`negative-tbd-violation.json` fixture to also cover the entity-disguised case. As specified, this
fixture **cannot distinguish** "entity-decode implemented correctly" from "entity-decode never
implemented at all," because it co-locates a plain literal `TBD` violation in the same document
as the entity-disguised violation, and the check that consumes it only asserts the aggregate
`expected.result`/`expected.reason`, not per-case evidence.

**Evidence:**
- Document contract 6 (lines 591–597): `negative-tbd-violation.json` "contains two independent
  cases, each of which alone would already fail validation: (1) the literal substring `TBD`
  outside any sanctioned-marker scope, and (2) a second, separate occurrence disguised as an HTML
  character reference (e.g. `T&#66;D`), proving the `decode-html-entities` normalization step
  ... actually collapses an entity-split disguise ... not merely that the pipeline step is pinned;
  `expected.result` is `"invalid"`, `expected.reason` is `"placeholder-violation"`."
- Check 9 ("Deterministic verification", item 9): for negative fixtures, the verifier "run[s] the
  validator directly against the inlined `syntheticSpec` ... and require[s] `expected.result` to
  equal `"invalid"` with the pinned `expected.reason` value exactly as named in document contract
  6." There is no sub-check that isolates which of the two co-located violations triggered the
  result, no per-occurrence count, and no second fixture devoted solely to the entity-disguised
  case.
- A validator that implements the literal `TBD` scan only, and treats `decode-html-entities` as a
  complete no-op (never calling an entity-decode function at all, i.e. exactly the "token stub"
  failure mode the original review-2 finding warned about), still finds case (1)'s literal `TBD`
  substring, reports `invalid`/`placeholder-violation`, and **passes check 9 and AC-P3A-05 for
  this fixture with zero evidence the entity-decode step does anything.** The fixture's own
  stated purpose ("proving the `decode-html-entities` normalization step actually collapses an
  entity-split disguise ... not merely that the pipeline step is pinned") is therefore false as
  constructed — it proves exactly the opposite of what it claims to add, and the "twelve
  fixtures" count was not increased (document contract 6's header still enumerates one no-`TBD`
  fixture, one missing-field fixture, one unknown-extension-point fixture, plus the manifest — no
  13th fixture was added), confirming the entity case was folded into the existing fixture rather
  than given independent, dispositive coverage.
- Separately, the Hard-constraints section (lines 216–218) claims: "`negative-tbd-violation.json`
  (document contract 6) proves the literal case and **one markup-splitting case** and the
  HTML-character-reference-splitting case are each caught" — but document contract 6's own
  fixture description (lines 591–597) defines only **two** cases (plain literal, entity
  reference). No markup-splitting case (e.g. `T<!--x-->BD`, `T**BD**`) is anywhere described as
  part of this fixture. This is a new, self-introduced internal contradiction between the
  Hard-constraints claim and document contract 6's actual content — the rework asserts fixture
  coverage that does not exist in the deliverable it cites.

**Smallest amendment:** Split `negative-tbd-violation.json` into independently dispositive cases —
e.g. make the existing fixture's `syntheticSpec` body contain **only** the entity-disguised
occurrence (no co-located plain-literal `TBD`), so a no-op `decode-html-entities` implementation
fails this fixture and a correct one passes it; correct the Hard-constraints sentence to match
document contract 6's actual two-case (not three-case) content, or add a genuine, independently
isolated markup-splitting-only case if a third case is truly intended.

**Does it change a locked decision?** No — it is confined to this parcel's own new deliverable
and proof design; it does not touch charter/plan-review/P1/P2 content.

---

### F2 — MAJOR — The "pinned, deterministic" bipartite-matching resolution depends on a term-processing order (the `requiredSpecAdditions` union's element order) that is itself never pinned anywhere in frozen P2 content or in this parcel — reopens R1-F3

**Claim:** The rework's fix for R1-F3 (bipartite-match ambiguity, originally MAJOR) asserts a
"pinned, deterministic resolution" — process required terms "in the fixed order they appear in
the live `requiredSpecAdditions` union ... (the same deterministic order document contract 1's
fold-live derivation already produces)" — and claims this makes `Reason` "reproducible across
implementations, not implementation-defined." This claim is false as written: the *order* of that
union is never pinned as a textual rule anywhere in P2's frozen `fold-engine.md`,
`classification-axes.schema.json`, `routing-output.schema.json`, or P2's own spec prose, nor in
P3-A itself.

**Evidence:**
- `docs/specs/schemas/fold-engine.md` (`union-set` definition, line 27, and Algorithm step 5,
  line 73): "the fold collects these fields into a **deduplicated set**" / "Union the `union-set`
  fields" — described mathematically as a set, with no stated serialization/element order for the
  resulting array across multiple declared classes.
- `docs/specs/schemas/routing-output.schema.json`: `requiredSpecAdditions` is typed `"type":
  "array"` with no ordering constraint, enum, or convention documented.
- P2's own fixture `docs/specs/schemas/fixtures/positive-multilabel-health-privacy.json`
  demonstrates one concrete order (declared-class order, then within-class array order) for a
  two-class union, but this is a single worked example inside a fixture file, never stated as a
  normative rule in `fold-engine.md`'s prose — and P2's `verify-p2.ps1` check 9 ("deep-compare the
  result to `expected` after normalizing key order and whitespace," P2.md line 508) only normalizes
  **object key** order, not array element order, meaning P2's own reference check already treats
  exact array order as load-bearing per-fixture, but P2 pins this only for the handful of class
  combinations its nine fixtures happen to cover — not as a general rule for all 2^8 possible
  declared-class combinations P3-A's fold-live resolver must handle.
- P3-A.md (lines 433–434) cites "document contract 1's fold-live derivation" as the source of this
  "deterministic order," but document contract 1 (lines 385–392) only says the verifier should
  "obtain the live `requiredSpecAdditions` union for the spec's declared classes" — it never states
  what order that union's elements are in.

Two independent, correct implementations of this document contract (this parcel's own
`verify-p3a.ps1` and a future P4 reimplementer) could reasonably compute the union element order
differently for a spec declaring more than one delivery class not covered by P2's five multi-label
fixtures (e.g. iterating `delivery-class-controls.json`'s own top-level class-key order filtered to
selected classes, vs. iterating the spec's own `delivery_classes` frontmatter declaration order) —
producing a different greedy term-processing order, and therefore a different reported
`satisfied: false` term identity, for the same ambiguous real-corpus document the original finding
was concerned about. The claimed fix moves the nondeterminism one level up without closing it.

**Smallest amendment:** Pin the union-order rule explicitly inside P3-A's own document contract 1
(not merely cite "the same order fold-live derivation already produces"), e.g.: "process the spec's
`delivery_classes` array in frontmatter declaration order; for each class, append its
`requiredSpecAdditions` array entries in `delivery-class-controls.json`'s declared order, skipping
any term already present from an earlier class" — and have check 6/8/9's embedded resolver
implement exactly that rule, byte-for-byte.

**Does it change a locked decision?** No — it is a request to add a textual rule entirely inside
P3-A's own new deliverable; it does not reopen `fold-engine.md` or any other frozen P2 content
(P2's own fixtures remain consistent with the proposed rule).

---

### F3 — MAJOR — AC-P3A-06 still makes the unqualified "catching homoglyph, zero-width...
disguises" claim the new Hard-constraints disclaimer explicitly narrows, leaving an unresolved
internal contradiction at the acceptance-criterion (closure-gate) level — R2-F1 only half-fixed

**Claim:** The rework's fix for R2-F1 (disguised-`TBD` detection unprovable/unenforced, originally
BLOCKER) adds a "Scope of this claim (what is fixture-proven, not merely pinned)" disclaimer to the
Hard-constraints section, correctly narrowing "defeats disguise class X" to "the corresponding
pipeline step is pinned ... not ... an independent adversarial-corpus proof of UTS #39
confusables-table fidelity." This narrowing was **not propagated to AC-P3A-06**, the actual
acceptance criterion a closure reviewer checks the parcel against.

**Evidence:**
- AC-P3A-06 (Acceptance criteria section): "no file ... contains the sanctioned marker or any of
  the banned placeholder patterns after the pinned `placeholderNormalizationSteps` pipeline is
  applied **(catching homoglyph, zero-width, soft-hyphen, fullwidth, and mathematical-alphanumeric
  disguises**, and markdown/HTML-syntax-splitting disguises via HTML comments, HTML/XML tags,
  inline-code backtick spans, emphasis markers, and escape backslashes — not only the literal ASCII
  form)..." — this is an unqualified, absolute claim, with no reference to the new "Scope of this
  claim" disclaimer, and no acknowledgment that no fixture exercises the homoglyph/zero-width/
  fullwidth/math-alphanumeric classes.
- Compare the Hard-constraints disclaimer (lines 216–221) which states explicitly that only "the
  literal case and ... the HTML-character-reference-splitting case" are fixture-proven, and that
  UTS#39 confusables fidelity "remains a structural (implementation/code-review-time) obligation on
  the verifier rather than a spec-level fixture claim."
- AC-P3A-06's own word "catching" directly contradicts the Hard-constraints' own narrowed reading
  — a future closure reviewer reading only the Acceptance Criteria section (the actual pass/fail
  gate `AC-P3A-11`/evidence-map references) would reasonably conclude the homoglyph/zero-width
  disguise classes are proven caught, which the parcel's own Hard-constraints text now says is not
  true. AC-P3A-06's own disguise-class enumeration is also stale: it never mentions
  HTML-character-reference-splitting at all, the very class this rework's headline fix targets.
- The "Dual review" section cites "the no-`TBD`/sanctioned-marker boundary (AC-P3A-06)" as one of
  the mandatory reviewer-covered items without flagging this residual scope gap for the reviewer's
  attention.

**Smallest amendment:** Reword AC-P3A-06 to match the Hard-constraints disclaimer exactly — e.g.
"... after the pinned `placeholderNormalizationSteps` pipeline is applied, which pins (but, per the
Hard-constraints scope disclaimer, fixture-proves only the literal and HTML-character-reference-
splitting cases of) homoglyph, zero-width, ... disguises" — so the acceptance criterion cannot be
read, in isolation, as certifying a stronger guarantee than the parcel's own checks actually
enforce.

**Does it change a locked decision?** No.

## Missing pieces / collisions / unknowns

- F1 and F3 together mean the rework's central, headline claim — "a disguised `TBD`, however
  disguised, is exactly as invalid as a literal one" (still present verbatim in the Hard-constraints
  opening sentence) — remains only partially fixture-proven, and the one new piece of "proof" this
  rework adds (the extended `negative-tbd-violation.json` fixture) is constructed in a way that
  cannot actually demonstrate the new mechanism works. This is the same category of adversarial
  evasion ("letter satisfied, spirit violated") the original dual review was convened to catch, now
  reintroduced inside the rework's own fix for that exact finding.
- F2 is a genuine residual ambiguity, not a hypothetical one: P2's own fixture set only pins union
  order for five class combinations out of 2^8 possible declared-class sets; any spec (including a
  future P3-B/P4-authored one) declaring an uncovered combination has no textually pinned union
  order to anchor the "fixed term order" the anti-heading-soup resolver depends on.
- No new scope creep found: `git diff 1a5a326 70ca456 -- .../parcels/P3-A.md` touches only
  `parcels/P3-A.md`; no other repository file changed on this branch (`git status` clean, diff stat
  confirms single-file change).
- No locked-decision drift found: charter, plan-review, `parcels/P1.md`, `parcels/P2.md`, and
  `closures/P2.md` SHA-256 hashes cited in this spec's "Lineage and dependencies" all match the
  live repository content exactly (recomputed independently — see Verification notes). The
  `coordinator-assigns-at-gate-2` citation (R2-F3) now correctly points at `parcels/P2.md`'s own
  dispatch-time text rather than the current, closed `INDEX.md`, and that citation was verified
  against `parcels/P2.md` lines 390–397/524–530.
- The R1-F4/R2-F6 `BaseCommit` fix, R1-F5/R2-F4 extension-point wording fix, R2-F2 template
  content-quality floor, R2-F3 citation correction, R2-F5 attribution correction, and R2-F7
  normalization-scoping narrowing were all independently verified against the diff and found
  correctly closed with no new defect.

## Verification notes

- Computed SHA-256 of the reviewed spec file directly from
  `biostack-wt/p3a-rework-3/docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`:
  recorded above.
- Diffed `1a5a326` (rework-2, the hash both original reviews assessed) against `70ca456`
  (rework-3 HEAD) for `parcels/P3-A.md` in full (207 insertions / 81 deletions) and read every
  changed hunk plus the surrounding unchanged context for each of the ten triage rows.
- Confirmed, by direct SHA-256 computation against `biostack` main-repo canon, that the charter
  (`4cd390d6...`), plan-review (`fbe40053...`), `parcels/P1.md` (`a55235e8...`), `parcels/P2.md`
  (`527d930f...`), and `closures/P2.md` (`cc5a4ee1...`) hashes cited in the spec's Lineage section
  all match exactly. No stale/forged hash found.
- Read `docs/specs/schemas/fold-engine.md` in full (including the `union-set` definition and
  Algorithm section) and `docs/specs/schemas/routing-output.schema.json` to confirm neither pins an
  element order for the `requiredSpecAdditions` union — basis for F2.
- Read `docs/specs/schemas/fixtures/positive-multilabel-health-privacy.json` and confirmed it
  demonstrates, but does not textually pin as a rule, one possible union-order convention.
- Read `parcels/P2.md` lines 388–397 and 524–530 to confirm the corrected `coordinator-assigns-at-
  gate-2` citation (R2-F3 fix) is now accurate.
- Confirmed via `git diff --stat` and `git status` that this branch's only change relative to
  `main` is the single file `parcels/P3-A.md` — no scope creep into any other surface.
- Did not independently execute or hand-simulate `verify-p3a.ps1` (no such script exists yet; this
  is a spec-only review of a review-candidate with no implementation).
- Did not read any other reviewer's output file, per independence instructions.
- Did not execute, edit, or create any file other than this review output.
