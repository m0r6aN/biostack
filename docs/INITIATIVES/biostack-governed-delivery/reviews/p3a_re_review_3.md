# p3a_re_review_3 — P3-A spec review

**Verdict:** APPROVE-WITH-FIXES
**Spec file:** docs-wt(p3a-rework-3)/docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md
**Spec SHA-256:** 277a7b745be3bd77df6d1ebdcd531a4f16a8b732807cdabec7437100f4242691
**Date:** 2026 (session date not independently knowable; see repo clock)

## Scope note

This is a targeted re-review of PR #523 (branch `docs/p3a-rework-3`, commit `7df5ef4`) against the
five Round-2 TRIAGE-2026-10-08.md closure claims. I diffed `7df5ef4` against the immediately prior
hash (`70ca456`, "rework 3") to isolate exactly what changed in this delta (one file, 17 hunks,
134 insertions / 73 deletions, all confined to `parcels/P3-A.md`), and scanned those changed
regions for new defects. I did not re-litigate findings already closed and unchanged since rework
2 (`1a5a326`).

## Ranked findings

### F1 — MINOR — "thirteen `fixtures/p3a/*.json` fixtures" miscounts by one (manifest is `.md`, not `.json`)
**Claim:** Check 9 and the AC-P3A-05 evidence-map entry both assert "thirteen" JSON fixtures are
processed via a `*.json` glob / produce one `fixture-results.json` entry each.
**Evidence:**
- `parcels/P3-A.md`, "Deterministic verification" check 9: "For each of the thirteen
  `fixtures/p3a/*.json` fixtures: parse JSON; require exactly the keys `input`/`expected`; ...".
- Same file, "Acceptance-to-evidence map": `| AC-P3A-05 | `fixture-results.json` (all thirteen
  fixtures) |`.
- Allowed-surfaces list items 13–24 enumerate exactly 12 `.json` fixture files (8 positive +
  4 negative); item 25 is `REAL-SPEC-COMPATIBILITY-SET.md`, a `.md` file handled separately by
  check 10 (writing to `compatibility-set-check.json`, not `fixture-results.json`).
  `docs/specs/schemas/fixtures/p3a/*.json` therefore globs to exactly 12 files, not 13. Document
  contract 6's header ("Thirteen fixtures under `docs/specs/schemas/fixtures/p3a/`") is counting
  the directory's 13 total files (12 JSON + 1 manifest), which is internally fine as prose, but
  check 9 narrows the pattern to `*.json` and keeps the word "thirteen" — a residual arithmetic
  inconsistency.
- This is **not new to this delta**: the prior hash (`70ca456`) had the identical bug shape
  ("twelve `fixtures/p3a/*.json` fixtures" against 11 actual `.json` files, since that revision
  had only 3 negative fixtures: 8 + 3 = 11). The delta renumbered 12→13 fixtures and 27→28
  surfaces consistently everywhere else, but propagated the same off-by-one count pattern into the
  `*.json`-scoped check text rather than fixing it.
- Functionally low-risk: a real implementation globbing `fixtures/p3a/*.json` naturally returns
  exactly 12 files regardless of the prose word "thirteen," so `verify-p3a.ps1` would not
  mis-iterate; the defect is textual/self-consistency only, not a correctness defect in the
  checked behavior.
**Smallest amendment:** In check 9, change "the thirteen `fixtures/p3a/*.json` fixtures" to "the
twelve `fixtures/p3a/*.json` fixtures (the eight positive and four negative JSON fixtures;
`REAL-SPEC-COMPATIBILITY-SET.md` is handled separately by check 10)"; in the acceptance-to-evidence
map, change "`fixture-results.json` (all thirteen fixtures)" to "`fixture-results.json` (all
twelve `.json` fixtures)".
**Does it change a locked decision?** No.

## Verification of the five targeted Round-2 closure claims

**(1) Independently dispositive placeholder negative fixtures.** Document contract 6 now defines
`negative-tbd-violation-literal.json` (literal `TBD`, no other occurrence) and
`negative-tbd-violation-entity-disguised.json` (only an HTML-character-reference-disguised
occurrence, e.g. `T&#66;D`, no co-located literal). I constructed the adversarial validator the
task specifies — one that performs only a literal-`TBD` regex scan (`\b(TBD|TODO|FIXME)\b`) with
the `decode-html-entities` normalization step (document contract 1, step 6) implemented as a
no-op:
- Against `negative-tbd-violation-literal.json`: the raw `syntheticSpec` body already contains the
  contiguous substring `TBD`; the literal-scan-only validator matches it and correctly reports
  `invalid`/`placeholder-violation`. **This fixture passes** under the broken validator.
- Against `negative-tbd-violation-entity-disguised.json`: the raw body contains `T&#66;D`, which is
  not a contiguous `TBD` substring; with `decode-html-entities` as a no-op, no normalization step
  collapses it to `TBD`, so the literal-scan-only validator finds no match and incorrectly reports
  `valid`. **This fixture fails** (diverges from `expected.result: "invalid"`), making the broken
  validator provably fail the fixture suite, and isolating the failure to exactly the one pipeline
  step that was stubbed — exactly as document contract 6 and the Hard Constraints "Scope of this
  claim" paragraph assert.
- Check 9 (new in this delta) additionally enforces, as a named check
  (`fixture-not-independently-dispositive`), that each of these two `syntheticSpec`s contains
  **exactly one** placeholder-pattern occurrence after normalization — converting document
  contract 6's "independently dispositive" design rule from unchecked prose into an asserted
  invariant. Confirmed present and correctly worded.
- Every case named in the Hard Constraints "Scope of this claim" paragraph (the two fixture names,
  the `decode-html-entities`-no-op isolation claim, and the explicit disclaimer that markdown/HTML-
  syntax-splitting and homoglyph/zero-width/fullwidth/mathematical-alphanumeric disguises are
  pinned-but-not-independently-fixture-proven) matches document contract 6 and AC-P3A-06 verbatim
  in substance and in fixture-name spelling. Counts are consistent: 2 no-TBD fixtures, 1
  missing-field fixture, 1 unknown-extension-point fixture = 4 negative fixtures, matching allowed
  surfaces 21–24 and check 9's "four negative fixtures" text.
**Verified: closed correctly.**

**(2) Term-processing order pinned ordinally.** Document contract 2's bipartite-matching
resolution rule now reads: "process required terms in strict ordinal (byte-value) lexicographic
ascending order of the live, deduplicated `requiredSpecAdditions` union's term strings for the
spec's declared, folded class set — not `delivery_classes` declaration order, not
`delivery-class-controls.json`'s key order, and not any other insertion-dependent order." This
replaces the prior revision's vaguer "fixed order they appear in the live
`requiredSpecAdditions` union" (which implicitly depended on `fold-engine.md`'s internal iteration
order, an unpinned implementation detail of a frozen file P3-A does not own). The new text
explicitly disclaims dependence on `fold-engine.md`'s internal construction order and pins the sort
key to the union's own term strings. Combined with the "lexicographically least still-unconsumed
candidate heading" assignment rule (unchanged), this is now a fully self-contained, deterministic,
implementation-order-independent algorithm: any two conformant implementations given the same
`delivery_classes` set and the same document's heading list must produce an identical
satisfied/unsatisfied term set and an identical per-term heading attribution. **Verified: closed
correctly** (resolves R1-F3).

**(3) AC-P3A-06 scoped to fixture-proven cases.** AC-P3A-06's text was rewritten to explicitly
separate what is fixture-proven ("the literal and HTML-character-reference-splitting disguise
cases, which are the only two disguise classes this parcel's own fixtures independently prove")
from what remains "a mandatory structural obligation enforced by check 12 against every file this
parcel ships (but not independently fixture-proven in isolation ...)". This mirrors the Hard
Constraints "Scope of this claim" paragraph verbatim in substance and closes the prior
overclaiming (R2-F1: "disguised-TBD detection unprovable/unenforced"). The acceptance criterion no
longer asserts blanket adversarial-corpus coverage of every disguise class; it narrows the proven
claim while still pinning the broader normalization pipeline as a mandatory (if unproven-by-fixture)
structural obligation. **Verified: closed correctly.**

**(4) 26-file set bound to the frozen P2 census, not falsifiable by corpus growth.** Document
contract 7 was rewritten from "every file returned by `git ls-files docs/specs/active
docs/specs/done` at `BaseCommit`" (a live, corpus-growth-sensitive query) to "every file path that
appears as a `Spec file` row in P2's **frozen** `docs/specs/schemas/AXIS-REGRESSION-MAP.md` ... not
a live `git ls-files ...` query at this parcel's own `BaseCommit`", with an explicit sentence that
corpus growth or shrinkage after P2's `BaseCommit` changes neither the row count nor the row set.
Check 10 was correspondingly rewritten to require the row set to equal "the set of `Spec file`
values in P2's frozen `docs/specs/schemas/AXIS-REGRESSION-MAP.md`" rather than a row-count formula
derived from a live `git ls-files` count. I independently verified:
- `AXIS-REGRESSION-MAP.md` contains exactly 26 `Spec file` data rows (`grep -c "^| docs/specs/"` =
  26), matching the "26 files" figure cited throughout the spec.
- `AXIS-REGRESSION-MAP.md`'s git history shows it was last touched by P2's own implementation
  commit (`253d07e`) and has not been modified since, including by any of the P3-A rework commits
  — it is genuinely frozen in this worktree, consistent with the Frozen-surfaces section listing
  it as a read-only composed input.
- Document contract 7 and check 10's wording now correctly disclaim any dependency on a live
  `docs/specs/active`/`docs/specs/done` listing for row-set membership, closing the prior
  vulnerability where a ticket spec added or removed after P2's `BaseCommit` could silently change
  this file's row count or content.
**Verified: closed correctly.**

**(5) Citations accurate.** Spot-checked every hash and cross-reference touched by, or load-bearing
for, this delta's claims:
- Charter SHA-256 `4CD390D631...` — matches `sha256sum CHARTER.md` exactly (case-insensitive hex
  match confirmed).
- Plan-review SHA-256 `FBE40053DA...` — matches `sha256sum PLAN-REVIEW.md` exactly.
- P1 spec hash `A55235E81F...` — matches `sha256sum parcels/P1.md` exactly.
- P2 spec hash `527D930FC1...` — matches `sha256sum parcels/P2.md` exactly.
- P2 closure hash `CC5A4EE1F7...` — matches `sha256sum closures/P2.md` exactly.
- `dispatch/P1-GATE2.md`, `closures/P1.md`, `closures/P2.md` all exist as cited.
- Document contract 9's citation of "`parcels/P2.md` document contract 8, lines 392-394 and 527"
  was corrected in this delta from the prior revision's erroneous "document contract 9" (R2-F3's
  disposition). I independently verified: `P2.md`'s section numbered "### 8." is literally titled
  "`docs/specs/INDEX.md` amendment" (the section containing the `coordinator-assigns-at-gate-2`
  literal), and `grep -n "coordinator-assigns-at-gate-2" parcels/P2.md` returns matches at lines
  393 and 527 — both inside the cited 392-394/527 range. The citation is now accurate.
- The claim that "the current, closed `docs/specs/INDEX.md` P2 row no longer carries this literal"
  was independently verified: the live `P2` row in `docs/specs/INDEX.md` carries real links
  (`GATE2-P2-IMPLEMENTATION.md`, `p2_builder`) in the branch/worktree and owner cells, not
  `coordinator-assigns-at-gate-2`.
- `delivery-class-controls.json`'s live `requiredSpecAdditions` union was independently computed
  (Python walk over the JSON) and equals 46 distinct terms, matching document contract 2's
  informative "46 distinct terms as of P2's shipped content" aside.
**Verified: closed correctly; no stale or incorrect citation found in the changed regions.**

## Missing pieces / collisions / unknowns

- None found beyond F1 above. The delta is narrowly scoped to the five claimed closures plus the
  mechanical 27→28/12→13 renumbering they required, and I found no scope creep, no new frozen-
  surface touch, and no new product-capability-semantics leakage introduced by this delta.

## Verification notes

- Confirmed this delta (`70ca456` → `7df5ef4`) touches exactly one file
  (`parcels/P3-A.md`), 17 hunks, 134 insertions / 73 deletions — no other repository file changed.
- Confirmed all five hash citations (charter, plan-review, P1, P2, P2 closure) against the actual
  files in the worktree.
- Confirmed `AXIS-REGRESSION-MAP.md` row count (26) and git-frozen status (untouched since P2's
  `253d07e`).
- Confirmed the P2.md document-contract-8/line-392-394/527 citation against the actual file.
- Confirmed `delivery-class-controls.json`'s live union term count (46) matches the spec's
  informative aside.
- Constructed and reasoned through the adversarial "literal-scan-only, entity-decode-no-op"
  validator against both no-TBD fixtures by hand; confirmed it passes the literal fixture and
  fails the entity-disguised fixture, exactly as document contract 6 and AC-P3A-06 claim.
- Did not re-verify unrelated, unchanged-since-rework-2 sections of the spec (e.g. the
  `EXTENSION-POINTS.md` wording, the anti-heading-soup heading-length-bound constraint, the 15-key
  schema contradiction fix) since those were outside this delta's diff and outside this review's
  assigned scope (prior reviewers already closed those per TRIAGE-2026-10-08.md); spot-reads of
  those sections while reading the file end-to-end turned up nothing inconsistent with the
  Round-2 dispositions.
