# p3a_impl_review_1 — P3-A implementation review

**Verdict:** PASS-WITH-FIXES
**Commit reviewed:** `4fe58250d78012d134dbdf376a094567fe6d3277` (main, merge of PR #525); P3-A
implementation commit `c3b40076fc963a9c8d72d4cb7e0ef7a8a0b812f3` on `feat/biostack-governance-p3a`,
parent `756c9d154d7a55a24fcc27f3b77ada4f04503028` (the Gate 2 dispatch-record commit, confirmed as
`BaseCommit`).
**Spec (sole build authority):** `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`,
SHA-256 `1c436a2ec4620881d1533bd543bfc1373379fcad0585fd23939146fd2843c398` (matches the builder's
tested hash recorded in `p3a_builder.log`).
**Dispatch record:** `docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P3A-IMPLEMENTATION.md`
**Worktree used for live verification:** `/home/cmorgan76/Repos/biostack-wt/governance-p3a`
(HEAD `c3b4007`, read-only inspection; verifier run produced only the untracked
`artifacts/p3a-verification/` evidence directory, no tracked file touched).
**Date:** 2026-10-09

## Summary

I independently ran `verify-p3a.ps1` (pwsh 7.x, no network) against the real worktree with
`-BaseCommit 756c9d154d7a55a24fcc27f3b77ada4f04503028 -BuilderId p3a_builder -ReviewerIds
p3a_impl_review_1,p3a_impl_review_2 -EvidenceDirectory artifacts/p3a-verification`. It printed
`P3-A verification PASS` and exited `0`; all 14 checks recorded `pass: true` in
`verification-summary.json`. I then independently re-derived or spot-checked each document
contract rather than trusting the transcript. The implementation is substantively faithful to the
spec, with one genuine, named acceptance-criterion gap (AC-P3A-11, carry-over item 3) and one
inherited-from-spec cosmetic defect (broken relative links in `docs/specs/README.md`'s new
section, which the spec itself pins byte-for-byte).

## Ranked findings

### F1 — MAJOR — AC-P3A-11's third carry-over item has no disposition anywhere in the parcel's own 28 deliverables

**Claim:** AC-P3A-11 requires "all three items identified in 'Carry-over' below (the two review-2
low-amendment items plus the self-identified coordinator-parcel-shape testing gap) have a named,
evidenced disposition inside this parcel's own deliverables (not a bare restatement)."
`REAL-SPEC-COMPATIBILITY-SET.md` explicitly discharges items 1 and 2 (its own "## Carry-over items
this file discharges" section, lines 13–36), but item 3 — "the `coordinator-parcel` shape is
specified but exercised by no fixture or compatibility-set row in this parcel" — is never named,
evidenced, or even mentioned in any of the 28 delivered surfaces. I grepped every schema, fixture,
template, README/INDEX, and the verifier itself for `coordinator-parcel-shape`, `exercising`,
`re-affirm`, `testing gap`, `self-identified`, `untested` — the only `coordinator-parcel` hits are
the pinned `specShapes.coordinator-parcel` schema keys/values (required by document contract 1
regardless, not evidence of item 3's disposition) and one incidental mention in
`templates/README.md` line 4 describing template shape, which does not name or discharge the
testing-coverage gap at all.

**Evidence:** `docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md` lines 13–36 (items
1–2 only); `docs/specs/schemas/parcel-spec.schema.json` line 8 (`appliesFrom`, pinned content
only, not a gap disposition); no other hits across
`docs/specs/schemas/{EXTENSION-POINTS.md,SECTION-HEADING-MAP.md}`,
`docs/specs/templates/**`, `docs/specs/scripts/verify-p3a.ps1`, `docs/specs/README.md`,
`docs/specs/INDEX.md`.

**Compounding evidence that this is a real gap, not reviewer misreading:** the verifier's own
check 10 is labeled, in its `Add-PassedCheck` call and in `verification-summary.json`,
`'REAL-SPEC-COMPATIBILITY-SET.md frozen-P2-census compatibility pass (AC-P3A-07/11)'` — i.e. the
verifier itself claims check 10 satisfies AC-P3A-11 in full. But check 10's implementation (spec
checklist item 10; `verify-p3a.ps1` lines ~800–824) only parses and recomputes
`REAL-SPEC-COMPATIBILITY-SET.md`'s table and `Agreement` column — it contains no assertion that
touches carry-over item 3 at all. The check's own name over-claims AC-P3A-11 coverage while one of
the acceptance criterion's three required items goes completely unchecked and undelivered. This is
exactly the "vacuous assertion" failure mode the review mandate asks to rule out: a check that
reports `pass: true` under a label claiming to cover an acceptance criterion it does not fully
cover.

**Smallest amendment:** add one short, clearly-labeled paragraph (e.g. appended to
`REAL-SPEC-COMPATIBILITY-SET.md`'s existing "## Carry-over items this file discharges" section, or
a new short subsection in `templates/README.md`) naming carry-over item 3 verbatim — that no
`coordinator-parcel`-shape fixture or compatibility-set row exists in this parcel's own fixture
set, and that per the spec's own Stop Conditions, P3-B's dispatch must discharge or re-affirm it —
and have `verify-p3a.ps1` assert that paragraph's presence as a small addition to check 10 (or a
new check), not just claim coverage in the check's label. This is additive to the existing 28
surfaces (an edit to an already-allowed file), not a new surface, and does not touch any frozen
file or reopen any locked decision.

**Does it change a locked decision?** No. It is a documentation/evidence completeness gap inside
the parcel's own allowed surfaces; it does not touch any frozen file, does not change scope, and
does not alter any schema/template/fixture semantics already shipped.

### F2 — MINOR — `docs/specs/README.md`'s new P3-A section links are broken relative paths, inherited verbatim from the spec's own pinned text

**Claim:** The appended section in `docs/specs/README.md` (document contract 9) links
`../schemas/parcel-spec.schema.json`, `../schemas/SECTION-HEADING-MAP.md`,
`../schemas/EXTENSION-POINTS.md`, and `../templates/README.md`. Because `README.md` lives at
`docs/specs/README.md`, a `../` prefix resolves to `docs/schemas/...` and `docs/templates/...`,
neither of which exists — the correct relative paths (as the pre-existing P2 section in the same
file already correctly uses, e.g. `[delivery-class-controls.json](schemas/delivery-class-controls.json)`
with no `../`) would be `schemas/parcel-spec.schema.json` and `templates/README.md`. These four
links are dead in any Markdown renderer that resolves relative links against the file's own
directory (GitHub included).

**Evidence:** `docs/specs/README.md` diff (`git diff 756c9d1 c3b4007 -- docs/specs/README.md`),
new lines; compare to the pre-existing P2 section two lines above it in the same file, which
correctly omits the `../` prefix. `verify-p3a.ps1` check 11 (lines ~856–858) asserts these exact
`../`-prefixed strings are present — it checks for byte-for-byte presence of the spec-pinned link
text, not link resolvability, so a genuinely broken link passes the check as designed.

**Root cause / fault attribution:** This is **not a builder deviation**. `parcels/P3-A.md`'s
document contract 9 pins these exact four link strings verbatim ("linking `../schemas/parcel-
spec.schema.json`, `../schemas/SECTION-HEADING-MAP.md`, `../schemas/EXTENSION-POINTS.md`, and
`../templates/README.md`"), and the builder (and verifier) correctly reproduced the spec's own
text exactly. The defect is inherited from the frozen spec, which the builder had no authority to
correct (document contract 9's link strings are prescriptive, and the spec's own frozen-surface
rules forbid "reinterpreting" required deliverable content). I flag it here because it is a real,
observable defect in the shipped product (a dead link in a reference document a human reader will
actually click), even though the builder's implementation fidelity to the spec is exactly correct.

**Smallest amendment:** This cannot be fixed by the P3-A implementation without deviating from its
own sole build authority; it requires either a scoped spec correction (future rework directive
amending document contract 9's four link strings to drop the erroneous `../` prefix) or a future
parcel's permitted edit to `docs/specs/README.md`'s content (not a new append, a correction, which
P3-A's own "exactly one appended section, zero removed/reordered lines" constraint forbids it from
doing itself).

**Does it change a locked decision?** No — it is a correction to inherited spec prose, not a
reopening of any P1/P2 lock or P3-A's own scope/contract decisions.

## Verification notes (checked and found clean)

- **Scope/diff integrity (AC-P3A-09):** `git show 4fe5825 --stat` and `git diff --name-only
  756c9d1 c3b4007` both independently produced exactly the 28 allowed-surface paths, sorted,
  matching the spec's "Exact allowed surfaces" list item-for-item, no more, no fewer. No frozen
  path (`classification-axes.schema.json`, `delivery-class-controls.json`, `fold-engine.md`,
  `routing-output.schema.json`, `AXIS-REGRESSION-MAP.md`, every P2 fixture, `CORE-CONTEXT.md`,
  `active/README.md`, `done/README.md`, every active/done spec, `AGENTS.md`,
  `biostack-production-readiness/`, `frontend/`, `backend/`, `contracts/`, `.github/`) shows any
  diff between `756c9d1` and `c3b4007` (`git diff --stat` on the full frozen-path set returned
  empty).
- **`BaseCommit` correctness:** confirmed `756c9d154d7a55a24fcc27f3b77ada4f04503028` is the commit
  that introduces `GATE2-P3A-IMPLEMENTATION.md` (single-anchor dispatch model), and that
  `c3b4007`'s sole parent is `756c9d1` — the clean single-commit implementation diff the dispatch
  record describes.
- **Document contract 1 (schema, 15 keys incl. `extensionSections: {}`):** parsed
  `parcel-spec.schema.json` with `python3 -m json.tool`; confirmed exactly 15 top-level keys,
  `extensionSections` present as a literal empty object, all pinned literal values (`statusClosed
  Vocabulary`, `noPlaceholderPatterns`, `placeholderNormalizationSteps`, `sanctionedTemplateFillIn
  Marker`/scope, five `*Source` paths) byte-identical to the spec's JSON block.
- **`decode-html-entities` fixture dispositiveness (adversarial construction):** manually traced
  `negative-tbd-violation-entity-disguised.json`'s body (`T&#66;D`, no co-located literal `TBD`)
  against a hypothetical no-op `decode-html-entities` implementation: the raw string `T&#66;D`
  does not match either pinned `noPlaceholderPatterns` regex without decoding, so a validator
  whose decode step is skipped/no-op would score this fixture `valid` and fail the fixture's
  `expected.result: invalid` — confirming the fixture genuinely isolates that one pipeline step, as
  claimed. The shipped `Get-PlaceholderNormalizedText` function uses
  `[System.Net.WebUtility]::HtmlDecode($t)` as step 6, a real, correct, offline HTML5 entity
  decoder (confirmed it runs before the Unicode/NFKC steps, in the pinned order).
  `negative-tbd-violation-literal.json` independently carries only the bare literal `TBD` with no
  entity-disguised co-occurrence, so the two fixtures are genuinely independently dispositive per
  document contract 6's design rule.
- **Document contract 2 (`SECTION-HEADING-MAP.md`):** 46-row term table, header exactly `Control
  term | Canonical alias(es) | Source`, every `Source` cell literal
  `delivery-class-controls.json`; anti-heading-soup matching rule (ordinal-lexicographic
  term-processing order, lexicographically-least-candidate heading assignment,
  distinct-heading-per-term, length-bound ≤ term+4 and ≤10) is implemented in
  `Resolve-RequiredSections` exactly as pinned, not merely described in prose.
- **Document contract 3 (`EXTENSION-POINTS.md`):** exactly three named extension points
  (`delivery-class-extension`, `domain-overlay-insertion`, `template-set-extension`), each with the
  required "This extension point adds no required section..." verbatim sentence, and the two
  pinned append-only/disjointness invariant sentences reproduced byte-for-byte (diffed against the
  spec's quoted text character-by-character — identical). Verifier check 7 asserts both sentences'
  presence by string match, not merely their topic.
- **Document contract 4 (eight templates):** read all eight template files in full. Each has the
  nine common frontmatter keys + `parcel_id`, `delivery_classes` = exactly one class,
  `guidance_classes: []`, `substance_function_risk: []`, a `[REPLACE: ...]` marker on every
  value-bearing key, one heading per required `requiredSpecAdditions` term for that class with
  ≥8-word instructive prose bodies (well over the floor in every case I read), and a literal `##
  Extension points used` heading with body exactly `None.`. No template defines capability/claim/
  evidence/escalation *meaning* — every heading body is an instruction to a future author
  ("Describe...", "State...", "Enumerate..."), not a policy assertion; this correctly stays on the
  structure side of the "Structural validation only" boundary.
- **Document contract 6 (13 fixtures):** all four negative fixtures contain exactly one violation
  each (manually confirmed `negative-missing-required-field.json` omits only `owner`;
  `negative-unknown-extension-point.json`'s `syntheticSpec` has a clean, fully-headed, placeholder-
  free body with only the one unregistered `extension_section` reference). Eight positive fixtures
  all point `input.specPath` at the real template files (no inlined duplication) consistent with
  the "fixture and template can never silently diverge" design intent.
- **Document contract 7 (`REAL-SPEC-COMPATIBILITY-SET.md`):** exactly 26 rows; `Spec file` column
  set diffed byte-identical (sorted) against `AXIS-REGRESSION-MAP.md`'s own 26 `Spec file` values —
  not a live `git ls-files` query, as required. Spot-checked one row (`BIO-FE-001-homepage-live-
  proof-panel.md`) by hand: P2 recorded `conforms`, file's actual headings have no term satisfying
  `contracts` (confirmed no heading containing "contract" as a token), so the file's independent
  `invalid`/`missing-required-section (contracts)` result and `flagged-for-human-review` agreement
  value are correct and genuinely re-derived, not copied.
- **Document contracts 8/9 (verifier; README/INDEX):** `verify-p3a.ps1` (972 lines) implements all
  14 numbered checks with real git/JSON/regex logic (no stubs, no hardcoded `pass: true` outside
  the final check-result recording); independently executed it end-to-end against the real
  worktree — genuine `PASS`, exit `0`. `INDEX.md` diff is exactly one added row (zero removed
  lines), using `coordinator-assigns-at-gate-2` literally in both Branch/worktree and Owner cells,
  matching document contract 9's pinned values cell-for-cell. `README.md` diff is exactly one
  appended, contiguous section with zero removed/reordered lines (see F2 for the link-path defect
  within that otherwise-correct append).
- **No-`TBD` compliance (AC-P3A-06):** grepped every one of the 28 delivered files for
  `TBD`/`TODO`/`FIXME`/`{{...}}`; the only literal hits outside `docs/specs/templates/**` are (a)
  the two no-TBD fixture JSON files' deliberate, single, fixture-proof occurrences, (b) the pinned
  regex-pattern string data inside `parcel-spec.schema.json` and `verify-p3a.ps1` (required, pinned
  literal content, not unresolved-placeholder prose), and (c) pre-existing `BIO-PAIRWISE-00x`
  `TBD`/`[TBD]` cells in `INDEX.md` that predate this parcel's diff entirely (confirmed via `git
  diff` that P3-A's own diff touches none of those rows) — these are the exact carry-over item 1
  gap, correctly named rather than silently present. Check 12's exclusion list for the self-
  referential pattern-storage files (`parcel-spec.schema.json`, the two TBD fixtures, `verify-
  p3a.ps1` itself) is logically necessary — document contract 1 *requires* the schema file to
  literally contain the substring `TBD` as pinned regex-pattern data, so a maximally literal
  reading of check 12 ("require zero matches in every file") would make the parcel's own
  required, pinned content fail its own placeholder check, an unsatisfiable self-contradiction in
  the spec as worded. The builder's exclusion is well-reasoned, narrowly scoped, and documented
  inline with the correct justification (check 5 is the authoritative pinned-content check for the
  schema file; check 9 is the authoritative result-reproduction check for the two fixtures) — I
  did not treat this as a deviation, since no other resolution is possible without the spec itself
  being unsatisfiable, but note it for completeness since it is a check 12 implementation detail
  not explicitly spelled out in the spec text.
- **No product-capability-semantics leakage (AC-P3A-08):** read every template and
  `EXTENSION-POINTS.md` in full; confirmed every reference to guidance-class/substance-function-
  risk-adjacent section names (claims, evidence threshold, escalation, red flags, function-review
  status, etc.) is a *structural heading requirement inherited from P2's already-frozen
  `delivery-class-controls.json`*, never a new definition of what those terms *mean* or what
  product behavior they bind to. `EXTENSION-POINTS.md`'s `domain-overlay-insertion` subsection
  explicitly defers naming/definition to "that future parcel's own approved spec... not a product
  capability, claim, guidance-class, or substance/function-risk binding, which remains P3-B/P0-B's
  exclusive territory." No file binds a guidance-class or substance/function-risk label to an
  allowed/degraded/refused/escalated outcome.
- **Frozen-surface byte-identity:** independently reran the quiet-diff check the verifier performs
  for every frozen path and got zero diffs across the board, matching check 4's PASS.

## Overall assessment

The implementation is a close, careful, largely faithful execution of a very long and exacting
spec, and the verifier is genuinely deterministic and non-vacuous for 13 of its 14 numbered checks
I inspected in depth. The one concrete, evidenced compliance gap (F1 — AC-P3A-11's third carry-
over item has no disposition in any of the 28 deliverables, despite the verifier's check 10 label
claiming to cover AC-P3A-11 in full) is a real, fixable documentation gap inside this parcel's own
permitted surfaces, not a scope violation, not a frozen-surface breach, and not a product-
capability-semantics leak. F2 is a real but spec-inherited defect the builder had no authority to
correct. Neither finding reaches FAIL severity (no scope violation, no frozen-surface change, no
capability-semantics leak, no fabricated fixture/check result), but F1 is a named acceptance
criterion this build does not fully satisfy as shipped, which is enough to withhold a clean PASS.
