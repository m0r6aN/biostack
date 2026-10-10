# p4_re_review_1 — P4 spec re-review (reworked spec, PR #536)

**Verdict:** APPROVE-WITH-FIXES
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p4-fix-1/docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md
**Spec SHA-256:** 7925ea4a646b90118eab585ff09f3cb976d9c434dd9e4ec62f5d122010de459d
**Branch / tip:** docs/p4-fix-1 @ a533a96 ("docs(governed-delivery): P4 spec fix 1 — review findings closed")
**Date:** 2026 (re-review pass, current session)

## Ranked findings

### RR1 (MAJOR) — Stale cross-reference: spec cites "`verify-p3b.ps1`'s own check 9" for the
Result/Reason computation it needs, but the live (already-amended, already-composed)
`verify-p3b.ps1` has moved that computation to check 10; check 9 is now an unrelated check
**Claim:** Document contract 2's `positive-real-spec-p3b-cross-check.json` description (P4.md
lines ~365-371) and Deterministic verification check 7 (lines ~703-711), plus AC-P4-05's prose
(line ~809), all state that `verify-p3b.ps1`'s own check 9 computes/establishes the `Result`/
`Reason` pair for `parcels/P3-B.md` via `Test-CapabilitySafetyOverlay`. This was correct in the
pre-remediation `verify-p3b.ps1` R1/R2 reviewed against, but P4.md's own Lineage section now
explicitly cites the **post-amendment-A1** P3-B spec/closure (content hash
`4E6FF3122585E27EA7F324AC3991C54779BA086EC0C9204894BB3C9735736FD2`, remediation commit `e43aa40`,
amendment commit `7f231d3`) — i.e. this rework is already composing on top of the *amended*
`verify-p3b.ps1`. In that live file, **check 9** (`docs/specs/scripts/verify-p3b.ps1:811-831`,
`Add-PassedCheck -Number 9 -Name 'rungApplied vocabulary live-counted against frozen
missingInputLadder.rungs...'`) is an unrelated rung-count-vs-ladder consistency check that
computes no `Result`/`Reason` pair at all. The actual per-fixture `Result`/`Reason` computation
via `Test-CapabilitySafetyOverlay` — including for the self-reference fixture
`positive-coordinator-parcel-p3b-self.json` — lives in **check 10** (lines 833-877,
`Add-PassedCheck -Number 10 -Name 'twelve p3b fixtures reproduce expected result/reason via live
cross-reference...'`), whose loop is the only place `$FixtureResults` (containing `result`/`reason`
per fixture) is populated and later written to `fixture-results.json` (line 968). A builder who
follows the literal first option in check 7's text ("invoke `verify-p3b.ps1`'s own check 9 logic")
would invoke code that has no `Result`/`Reason` to compare — check 9's logic is a rung-vocabulary
count, not a capability-safety-overlay computation. The fallback option ("or re-run `verify-p3b.ps1`
itself, read-only") remains achievable (it still exercises check 10 internally and exposes
`fixture-results.json`), so this is not an unimplementable blocker, but it is a verifiable,
byte-level misattribution of a frozen, already-merged artifact's content — exactly the class of
error this spec's own "Structural-block quote matching" Hard Constraint (and its repeated
insistence on reading every composed source live rather than trusting a stale description) exists
to prevent, and it was not caught by either prior reviewer because neither reviewer's quoted line
numbers (`verify-p3b.ps1:677-717`, R1-F4) correspond to the live, already-amended check numbering
either — both are citing a pre-remediation version of the file that no longer matches what this
rework itself already depends on.
**Evidence:** `docs/specs/scripts/verify-p3b.ps1` lines 811-831 (check 9, rung-vocabulary,
unrelated) vs. lines 833-877 (check 10, the actual `Result`/`Reason` fixture-reproduction loop
including `positive-coordinator-parcel-p3b-self`) and line 968 (`fixture-results.json` write);
P4.md lines 365, 368, 370, 703, 705, 706, 711, 809 (all say "check 9").
**Smallest amendment:** Replace every "`verify-p3b.ps1`'s own check 9`" reference in P4.md
(document contract 2's `positive-real-spec-p3b-cross-check.json` bullet, Deterministic verification
check 7, and AC-P4-05) with "check 10", re-verified against the live, post-amendment-A1
`verify-p3b.ps1`.
**Does it change a locked decision?** No — corrects a factual citation of a read-only prior
artifact; does not alter `verify-p3b.ps1` itself or any locked decision. This is the same class of
defect R1-F4 named (and this rework's fix for R1-F4 correctly fixed the *field-shape* overclaim)
but the *check-number* half of the same citation was not re-verified against the live file during
the fix, so a residual, narrower version of the same defect survives.

### RR2 (MINOR) — Composition-verification (check 10 of `verify-p4.ps1`) pins illustrative, not
mandatory, mutation targets, leaving a narrow two-implementer gap in the mechanical anti-hardcoding
proof itself
**Claim:** Hard Constraints' "Composed, not hardcoded" bullet and Deterministic verification
check 10 (the AC-P4-02 mutation-overlay test) both introduce their five example mutations with "for
example" (e.g., "add one throwaway required-section term to `SECTION-HEADING-MAP.md`'s `standard`
entry... flip `delivery-class-controls.json`'s `standard` `reviewers` value..."). The detection
mechanism itself (mutate-and-require-output-to-track) is sound and adversarially effective (I
verified it would catch both full hardcoding — a fixture-input-keyed lookup table ignores the
mutated overlay entirely and fails by producing stale output — and partial hardcoding of any one
of the five sources, since check 10 requires *all five* mutations to be reflected). But because
the exact mutated field/value in each of the five sources is only given as an illustrative example
rather than a pinned, mandatory probe, two honest implementers of `verify-p4.ps1` could choose
different specific mutations, which is immaterial to the check's detection power but means the
check's own exact content is not fully determined by the spec text alone.
**Evidence:** P4.md Hard Constraints "Composed, not hardcoded" bullet; Deterministic verification
check 10 ("for example, add one throwaway required-section term...").
**Smallest amendment:** Replace "for example" with a mandatory, pinned mutation per source (field
name, pre-mutation and post-mutation value, and which fixture/synthetic-spec exercises it),
mirroring the specificity already used elsewhere in this spec's fixture descriptions.
**Does it change a locked decision?** No.

### RR3 (MINOR) — Placeholder-exclusion scoping (check 8) still carries a binary "whichever"
branch rather than a single pinned location per fixture
**Claim:** Check 8's exclusion language narrows R2-F7's original "prose intent only" gap
meaningfully (closed-world, JSON-pointer-scoped, two named fixtures only, scan still runs over
`expected.*` and all other keys) but still reads "exactly the single string value (within
`input.syntheticSpec.headings[]` or `input.syntheticSpec.frontmatter.<field>`, whichever the
fixture's own authored content places it in)" — i.e. the spec names two candidate locations and
defers the actual choice to the builder's own fixture authoring, rather than pinning one.
**Evidence:** P4.md Deterministic verification check 8.
**Smallest amendment:** Once the builder authors the two fixtures, `verify-p4.ps1`'s own check 8
implementation should hardcode the one actual JSON-pointer path used (which is knowable once the
fixture content is written) rather than leaving two live candidate branches in the verifier
indefinitely — a cheap, mechanical tightening available at build time, not shaping time.
**Does it change a locked decision?** No — this is residual tightening of an already-adequate fix;
not a blocker.

## Verification of each closed-finding claim (R1/R2, checklist)

- **R1-F1 (unknown-extension-point fixture)** — CLOSED. `negative-unknown-extension-point-reference.json`
  added, exercised via the general `-SyntheticSpecJson` entrypoint (not P3-A's narrower verifier),
  named in surfaces, allowed surfaces, document contract 2, check 6, AC-P4-03. Confirmed present.
- **R1-F2 (`pathPattern` grammar underspecified)** — CLOSED. I ran my own two-implementer test:
  the grammar is now pinned (`*` = one-or-more non-`/` characters, never spans `/`; full-string
  anchoring; `|` split into independent full-string-anchored alternatives) and I traced both real
  probe paths (`parcels/P3-B.md` → `coordinator-parcel`; `CHARTER.md` → `unrecognized-shape`)
  against the live `pathPattern` strings (`docs/INITIATIVES/*/parcels/*.md`,
  `docs/specs/active/*.md|docs/specs/done/*.md`, read from `parcel-spec.schema.json`) and both
  resolve unambiguously and identically under this grammar. No residual determinism gap found.
- **R1-F3 (`invalid-status` unnamed/unproven)** — CLOSED. New leaf reason `invalid-status` named
  in Hard Constraints and stage 2; `negative-invalid-status-value.json` fixture added and proves
  it; confirmed `parcel-spec.schema.json`'s `statusClosedVocabulary` is a live four-value closed
  set the fixture's `"in-progress-ish"` value correctly falls outside of.
- **R1-F4 (cross-verifier "field for field" overclaim)** — PARTIALLY CLOSED. The *field-shape*
  overclaim (claiming check 9 already computed `foldSummary`/`boundFieldsChecked`) is correctly
  fixed — the rework now explicitly says check 9 "never computes a `foldSummary` or a
  `boundFieldsChecked` array" and scopes the claimed agreement to `Result`/`Reason` only. However,
  the *check number* itself is now wrong against the live, already-amended `verify-p3b.ps1` (see
  RR1, above) — a narrower residual of the same underlying "trust a stale description of a frozen
  artifact" defect.
- **R1-F5 ("seven reasons" inconsistency)** — CLOSED. No "seven" literal remains in the spec;
  stage 7's reason count is stated as "four," the Hard Constraints enumerate eleven existing +
  one new = twelve, and the output contract's "twelve closed-vocabulary `reason` literals" is
  internally consistent with the Hard Constraints list. Independently recounted: 11 prior +
  `invalid-status` = 12, matches.
- **R2-F1 (AC-P4-02 "composed not hardcoded" has no mechanical check)** — CLOSED, with the minor
  residual noted at RR2. I designed my own adversarial hardcoded-linter test: (a) a
  fully-hardcoded fixture-input→output lookup table fails check 10 because the mutated-overlay
  re-invocation reuses the same fixture input but a mutated live source, and a lookup-table
  implementation (keyed on input only) would return the stale, pre-mutation answer for all five
  mutated sources, failing by name (`hardcoded-value-detected`); (b) a partially-hardcoded
  implementation (four sources hardcoded, one genuinely live) is also caught because check 10
  requires *every one* of the five mutations to be reflected, not just one. The check is
  mechanically sound against both full and partial hardcoding.
- **R2-F2 (no synthetic triggered-and-satisfied P3-B binding fixture, no twin)** — CLOSED.
  `positive-synthetic-capability-claim-satisfied.json` (fully triggered, independently-authored
  label/class pair, `curated-evidence-guidance`/`ordinary` — confirmed both are live
  closed-vocabulary members of `classification-axes.schema.json`, and confirmed
  `product-capability-safety-contract.json`'s `labels.ordinary.behavior.C2.value` is `"allowed"`,
  consistent with the fixture's claimed match) and its structural twin
  `negative-synthetic-capability-claim-unsatisfied-twin.json` (single-field drift,
  `capability-claim-drift`) are both present, named in surfaces/allowed-surfaces/document
  contract 2/checks 6/AC-P4-03/AC-P4-04. This closes the single-point-of-truth evasion vector the
  original BLOCKER named.
- **R2-F3 (no homoglyph/confusables regression fixture)** — CLOSED.
  `negative-placeholder-homoglyph-confusables.json` added, explicitly targeting
  `placeholderNormalizationSteps` step 9 (`unicode-confusables-skeleton`) — confirmed this step is
  the ninth and final entry in the live `parcel-spec.schema.json` pipeline. Named throughout
  surfaces, document contract 2, check 8 (with pinned exclusion), AC-P4-03.
- **R2-F4 (no empty/malformed-input resilience fixture)** — CLOSED.
  `negative-synthetic-empty-malformed-input.json` added with pinned `expected.result`/`reason`
  (naming every missing required key); `verify-p4.ps1` check 9 added specifically asserting
  no-crash/deterministic-exit-1 behavior, explicitly citing `closures/P0-A.md`'s empty-diff-crash
  precedent by name. Named in surfaces, AC-P4-10, acceptance-to-evidence map.
- **R2-F5 (Hard-constraints/check-7 contradiction on "no new named failure reason")** — CLOSED.
  Hard Constraints now explicitly distinguishes `validate-spec.ps1`'s own output `reason`
  vocabulary (twelve literals, one new: `invalid-status`) from `verify-p4.ps1`'s own internal,
  parcel-local numbered-check-failure identifiers (`general-linter-parcel-verifier-disagreement`,
  `general-entrypoint-crashed-on-degenerate-input`, `hardcoded-value-detected`), stating they are
  "never conflated" — resolves the contradiction the original finding identified.
- **R2-F6 (AC-P4-03 mis-labels fold-stop fixture as "missing-field")** — CLOSED. AC-P4-03 now
  reads "the two missing-field cases... plus, separately, the fold-stop case..., whose stop
  pre-empts any required-section resolution entirely — a scalar-conflict mechanism, not itself a
  missing-field case" — explicit, correct taxonomy.
- **R2-F7 (placeholder-scan exclusion scoping loose)** — MOSTLY CLOSED, residual noted at RR3.
  Check 8 now pins a closed-world, JSON-pointer-scoped, two-fixture-only exclusion, with the scan
  explicitly still running over `expected.*` and every other key — a real narrowing from "prose
  intent" to a mechanically closed exclusion, with a small remaining "whichever" binary choice.

## Fixture/check/surface count consistency (item f)

Independently recounted from the live spec text, not trusted from prose claims:
- Frontmatter `surfaces:` list: 2 scripts + 16 fixtures + 2 registry files = 20. Matches "Exact
  allowed surfaces" (numbered 1-20) exactly, item-for-item.
- Fixture count: 16 distinct fixture filenames counted in Required document contract 2, Exact
  allowed surfaces, check 6, AC-P4-03, and the acceptance-to-evidence map — consistent everywhere
  I checked; no stray "ten" (the one remaining "ten fixtures" reference, line 77, correctly refers
  to P3-B's own ten fixtures under `fixtures/p3b/`, a different parcel's artifact, not a P4 count).
- `verify-p4.ps1` checks: numbered 1 through 12 in Deterministic verification, consistent with
  "sixteen fixture-reproduction checks" (check 6), "20 surfaces" (checks 3/4), and the
  acceptance-to-evidence map's references to checks 7/8/9/10. No gap or duplicate check number
  found.

## Governance-contract semantics (item g) — byte-spot-checked, all clean except RR1

Computed/compared directly against the live worktree (`biostack-wt/p4-fix-1`):
- Charter SHA-256 `4CD390D631487DA8E97509205A186F242C4A83B762FFFA894BEEC8A21F4EFE22` — matches.
- Charter line 134 (P4's parcel-tree row) and line 152 (dependency spine) — byte-for-byte match.
- Charter D8 (line 93, dual-review rule) — byte-for-byte match.
- P3-B spec SHA-256 `4E6FF3122585E27EA7F324AC3991C54779BA086EC0C9204894BB3C9735736FD2` and closure
  SHA-256 `5C73868F34A03087F7AD1CEDF3498BD462686BB05C2B50294D3FEF75F540A10C` — both match (confirms
  this rework is already shaped against the *post-amendment-A1* P3-B artifacts, as it claims).
- P3-A spec SHA-256 `4B928FBB80F042934B01E1F66371BF14967307B0D99BEA5BC02ACBDBF3BA633A` — matches.
- P0-B spec SHA-256 `4A49E3D6086670857D0190AD0032E8DE0C50DED14F9830C2ECFABE1A4B414347` — matches.
- `product-capability-safety-contract.json`/`.md` SHA-256 — both match
  (`020554BA...` / `C01E90877...`).
- `classification-axes.schema.json` SHA-256 `CFCF3140CE50F913D41A99456076C77B86FFA0603044C286EA54F846587FE1AC`
  and `delivery-class-controls.json` SHA-256 `E1E545CC01D1EC316FC3F137E80792ABAC1F5D071BD09E15C8B5C804B536B068` — both match.
- `delivery-class-controls.json`'s `standard` (6 required-section terms, 1 reviewer) and
  `health-boundary` (9 terms, 2 reviewers) rows independently re-read; union arithmetic (15
  distinct terms, max reviewer 2) matches the multilabel fixture's claims.
- `parcel-spec.schema.json`'s `statusClosedVocabulary` (4 literals) and
  `placeholderNormalizationSteps` (9 steps, step 9 = `unicode-confusables-skeleton`) — both read
  live and match the spec's descriptions exactly.
- `product-capability-safety-contract.json`'s `labels.ordinary.behavior.C2.value` = `"allowed"`
  and `labels.minor-or-age-uncertain.behavior.C2.value` = `"degraded"` (confirmed unchanged from
  prior reviews) — both consistent with the new and existing fixtures' claims.
- `CAPABILITY-FIELD-MAP.md`'s six-row table re-read; trigger conditions for `capability_claim`
  (non-empty `guidance_classes` **or** `substance_function_risk`) match the new triggered-fixture's
  declared axes.
- No change found to any frozen artifact (charter, P1/P2/P3-A/P0-A/P0-B/P3-B closed records,
  `product-capability-safety-contract.json`/`.md`, `classification-axes.schema.json`,
  `delivery-class-controls.json`, `fold-engine.md`, `routing-output.schema.json`,
  `SECTION-HEADING-MAP.md`, `EXTENSION-POINTS.md`, `CAPABILITY-FIELD-MAP.md`,
  `parcel-spec.schema.json`'s 14 non-`extensionSections` keys) — none of these are touched by this
  parcel's allowed surfaces, and I independently confirmed none of P4's claims about their content
  misdescribe the live files, **except** the stale `verify-p3b.ps1` check-number citation at RR1.
- The one genuine inaccuracy found (RR1) is a citation of a *read-only prior artifact's internal
  check numbering*, not a change to, or misstatement of, any ruled governance-contract semantic
  (D1-D18, product doctrine, guidance classes, substance/function-risk labels, D-B1..D-B6 cells).
  It does not change a locked decision.

## Missing pieces / collisions / unknowns

- RR1 (stale check-9/10 citation) is the only substantive residual defect found; it is narrow,
  cheap to fix (a find-and-replace of "check 9" → "check 10" in three spec locations, re-verified
  against the live file), and does not block the check-7 mechanism from being implementable via
  its own stated fallback ("or re-run `verify-p3b.ps1` itself").
- RR2/RR3 are minor tightening opportunities, not correctness defects; the mechanisms they touch
  (composition-verification, placeholder-exclusion scoping) are already adversarially sound as
  written, just slightly under-pinned in exact wording.
- No scope creep into P6/P7/P0-C/P5 territory found; the three load-bearing exclusions, Frozen
  surfaces, Standing authorization/tripwire, and Stop conditions sections are mutually consistent
  and none of the 16 (18 vs. original 10) fixtures or 20 (vs. original 14) allowed surfaces touch
  `frontend/`, `backend/`, `contracts/`, or `.github/`.
- Did not find any `TBD`/unresolved placeholder in the spec body itself outside the deliberately
  quoted fixture-design prose (e.g., the `T&#66;D`/homoglyph examples, which are descriptions of
  fixture content, not unresolved spec placeholders).
- Did not read any other reviewer's output file, per instructions.

## Verification notes

- Computed `sha256sum` of the spec file directly: matches the hash recorded above.
- Verified `git log`/`git rev-parse` on `biostack-wt/p4-fix-1`: `HEAD` = `a533a96`, branch
  `docs/p4-fix-1`, parent chain through `cf2398c` (PR #535 merge) and `7872f2e` (PR #534 merge) —
  consistent with the spec's own claimed lineage (P3-B remediation/amendment already merged before
  this spec was authored).
- Re-read the full reworked spec end-to-end (both halves, offset 0 and offset 669) before forming
  any verdict.
- Re-read both prior review files (`p4_spec_review_1.md`, `p4_spec_review_2.md`) in full and
  checked every one of their 12 findings (R1-F1..F5, R2-F1..F7) individually against the live
  reworked spec and live repository files, not against either reviewer's own say-so.
- Read `docs/specs/scripts/verify-p3b.ps1` directly (full check-numbering scan, lines 687-970) to
  verify the cross-verifier claim at the center of R1-F4/R2-F2 — this is where RR1 was found.
- Read `docs/specs/schemas/parcel-spec.schema.json`, `delivery-class-controls.json`,
  `classification-axes.schema.json`, `product-capability-safety-contract.json`,
  `CAPABILITY-FIELD-MAP.md`, and `EXTENSION-POINTS.md` directly to spot-check every live-value
  claim the rework makes; all matched except the issue at RR1.
- Did not run any git write command, did not edit any repository file other than this output file.
