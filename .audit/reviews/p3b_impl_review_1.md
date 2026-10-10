# p3b_impl_review_1 — P3-B implementation review

**Verdict:** PASS-WITH-FIXES
**Subject:** `/home/cmorgan76/Repos/biostack-wt/p3b-impl`, branch `feat/p3b-schema-binding`, PR #533
**Commit reviewed (tip):** `dd92fa577fb1bee0b40053cb34a6ea81c780b11d`
**BaseCommit (per GATE2-P3B-IMPLEMENTATION.md / dispatch record):** `789a6106381fffe1dde99f94c702ccb7b3761a15` ("docs(coordinator): Gate 2 — P3-B implementation dispatch") — confirmed equal to `origin/main` HEAD at review time.
**Build authority:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md`
**Normative frozen input:** `docs/specs/schemas/product-capability-safety-contract.json`/`.md` (P0-B, closed), `docs/specs/schemas/EXTENSION-POINTS.md`/`parcel-spec.schema.json` (P3-A, closed)
**Date:** 2026-10-09 (shaping/dispatch dates in repo); review performed same session.

**File hashes (computed independently, this review, SHA-256, repo HEAD = `dd92fa5`):**
- `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md` — `b4535c95215eb57c0f18dd27865a2a0be0839746bc382a5f0943c0311a685dfc` (identical to `git show 7b3225c:...`, see F3 below)
- `docs/specs/schemas/CAPABILITY-FIELD-MAP.md` — `828b2534fa9461393aa5452db2db15f4535044c042f4fa7d74bed4f92a350643`
- `docs/specs/schemas/parcel-spec.schema.json` — `0061ee037138e39096144112591b1faef653623992bc8fac46b7deac5c83b7ee`
- `docs/specs/schemas/classification-axes.schema.json` — `cfcf3140ce50f913d41a99456076c77b86ffa0603044c286ea54f846587fe1ac`
- `docs/specs/scripts/verify-p3b.ps1` — `e4e7591e555504b4392c7537203ea338f565062b82e7bb1878ad62c8e28f1182`

**Frozen P0-B/P1/P2/P3-A artifact bit-identity (main `789a610` vs tip `dd92fa5`, independently re-hashed, item 5):** `product-capability-safety-contract.json` (`020554ba...`), `.md` (`c01e9087...`), `SECTION-HEADING-MAP.md`, `EXTENSION-POINTS.md`, `delivery-class-controls.json`, `fold-engine.md`, `routing-output.schema.json`, `AXIS-REGRESSION-MAP.md`, `CHARTER.md` — all **MATCH** byte-for-byte. Clean.

## Summary

The delivered binding content is faithful: the `extensionSections.product-capability-safety-overlay`
append is the single additive key the spec pins, all 14 other `parcel-spec.schema.json` top-level
keys are byte-identical, `CAPABILITY-FIELD-MAP.md`'s six-row table is a byte-for-byte transcription
of the spec's own document-contract-2 table (independently diffed), every "Live cross-reference
rule" cell describes a lookup rather than embedding a copied contract value, the
`classification-axes.schema.json` diff touches only the two pinned `productGuidanceClass` fields,
the changed-file set matches the spec's named surfaces, every frozen P0-B/P1/P2/P3-A artifact is
provably byte-identical to `main`, the D→E `interaction-or-contraindication-signal` stage-4
escalation fixture resolves correctly (independently re-derived against the live
`preemptionOrder.stages` — the label is absent from stages 1–3's explicit lists, so the residual
rule at `stages[3]` correctly yields `preemptionStage: 4`), and all ten fixtures — run through the
actual verifier, which **recomputes** every fixture's expected result/reason via
`Test-CapabilitySafetyOverlay` rather than trusting the fixture's own `expected` block — reproduce
`valid`/`invalid` exactly, including every required negative fixture (vacuous `numeric_provenance`,
missing function-review-owner-adjacent status, absent `escalation`) genuinely failing. I confirmed
this non-vacuousness by tampering a copy of `negative-claim-behavior-drift.json` in a throwaway
sandbox clone (behavior weakened to match the live cell, removing the drift) and the verifier
correctly threw and failed. `verify-p3b.ps1` runs clean (`P3-B verification PASS`, all 13 checks)
against the real worktree at the named `BaseCommit`.

However, three independent, demonstrated gaps keep this from a clean PASS: (F1) the builder's own
disclosed 17th-path deviation — bringing in `parcels/P3-B.md` from a different, not-yet-merged
upstream branch — bypasses a Stop Condition this exact spec names, and materially changes
AC‑P3B‑08/check‑3's literal 16-surface pin to 17 inside the verifier itself, unilaterally, rather
than stopping for coordinator reconciliation as the spec requires; (F2) check 4's own code comment
claims the 17th path is "independently verified byte-identical against the already-reviewed
upstream commit 7b3225c," but no such comparison exists in the code — I built a tampered sandbox
copy of the real `P3-B.md` prose and the full 13-check verifier still printed `PASS`; (F3) check
11's placeholder-normalization pipeline omits the pinned `unicode-confusables-skeleton` step that
P3-A's own, analogous verifier implements, letting a homoglyph-smuggled "TBD" evade detection — I
constructed and ran this adversary. None of these three findings indicate the *currently delivered*
content is itself wrong (I independently confirmed the delivered `P3-B.md` content is in fact
byte-identical to `7b3225c`, and no placeholder-evasion or surface-set violation is actually present
in the shipped diff) — they are verifier/process soundness gaps, the same category of finding the
P0-B implementation reviews flagged against `verify-p0b.ps1`.

## Ranked findings

### F1 — MAJOR (process/authorization): disclosed 17th-path deviation bypasses a named Stop Condition instead of stopping for coordinator reconciliation

**Claim:** P3-B.md's own "Stop conditions" section states the builder must "Stop and return to the
coordinator if: ... Any required target path is absent or materially different from the contract
assumed here... A branch, worktree, base commit, spec hash, owner, or reviewer assignment is
ambiguous." The Gate 2 dispatch record (`GATE2-P3B-IMPLEMENTATION.md`) asserted "the spec is merged
canon via PR #532" and named `BaseCommit = "single anchor = the commit that adds THIS file to
main"` (i.e., `789a610`). Both are independently reproducible as false: `git show
789a610:docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md` fails (`fatal: path ...
exists on disk, but not in '789a610'`) — the dispatch record's "merged canon" claim does not hold
at the actual, named `BaseCommit`.

**Evidence:** The spec's "Exact allowed surfaces" section names exactly 16 paths and AC-P3B-08
requires "the changed-file set equals exactly the 16 allowed surfaces." The actual delivered diff
(`git diff --name-only 789a610...dd92fa5`) has **17** changed paths, and `verify-p3b.ps1`'s own
`$AllowedSurfaces` constant and check-3 label have been edited by the builder to assert 17, not 16
(`"exact changed-file set equals the 17 allowed surfaces (16 pinned + 1 disclosed prerequisite
merge-in..."`). This is a unilateral, self-authorized widening of the spec's own pinned
deterministic-check cardinality, performed by the builder rather than the coordinator, in a
situation the spec's own Stop Conditions name almost verbatim ("required target path is absent...
BaseCommit ambiguous").

**Mitigating facts (independently confirmed by this reviewer):** the deviation is prominently
disclosed (commit message, script header comment, `verification-summary.json`'s
`disclosedDeviation` field, and — per the commit message — the PR body); the brought-in content is
byte-identical to an already-authored, already-dual-reviewed commit (`7b3225c`, see F3 for the
verifier's own failure to assert this); and the brought-in file touches no frozen contract or
product surface. This is not a covert attempt to smuggle unauthorized content — it is an honest,
well-reasoned workaround to an inconsistency between the Gate 2 record and the actual repository
state. But "disclosed" does not discharge the obligation the spec itself names: the correct action
per this parcel's own Stop Conditions was to halt and let the coordinator either (a) merge PR #532
first and re-cut `BaseCommit`, or (b) explicitly re-author the Gate 2 record / spec to authorize a
17-path dispatch. Proceeding and amending the verifier's own pinned cardinality is a boundary the
builder was not granted.

**Smallest amendment:** Before Gate 3 merge, the coordinator must either (a) sequence/merge PR #532
first and re-verify this PR against the corrected, 16-surface `BaseCommit`, discharging the
deviation entirely, or (b) issue an explicit written reconciliation ruling authorizing the 17-path
exception, after which `verify-p3b.ps1`'s check-3/AC-P3B-08 language should be updated to reference
that ruling by ID rather than self-declaring the exception.

**Does this affect the currently-delivered content?** No semantic drift — the content itself is
correct and independently byte-verified (see F3's clean-identity finding). This is a process/
authorization-boundary gap, not a content defect.

**Changes a locked decision?** No — it does not alter any ruled D-B1..D-B6 cell or P0-B value; it
alters this parcel's own pinned deterministic-check surface count, which only the coordinator may
authorize.

### F2 — MAJOR (verifier): check 11's placeholder-normalization pipeline omits the pinned `unicode-confusables-skeleton` step, letting a homoglyph "TBD" evade detection

**Claim:** `parcel-spec.schema.json`'s `placeholderNormalizationSteps` (read live, 9 steps) ends
with `"unicode-confusables-skeleton"`. The Hard constraints section requires "No unresolved
placeholder... reusing both P3-A mechanisms rather than redeclaring them." P3-A's own
`verify-p3a.ps1` implements all 9 steps, including a documented confusables-skeleton table (lines
~384-430) and asserts its hardcoded pattern copy equals the live schema value
(`Assert-SequenceEqual -Actual @($SchemaDoc.noPlaceholderPatterns) -Expected
$PinnedPlaceholderPatterns`). `verify-p3b.ps1`'s `Get-PlaceholderNormalizedText` (lines ~230-249)
implements only 8 of the 9 steps — it never performs a confusables-skeleton pass — and nowhere
asserts its own hardcoded `$PinnedPlaceholderPatterns` constant still equals the live
`noPlaceholderPatterns` value in the schema (the two happen to match today, but nothing in the
verifier enforces that).

**Evidence (reproduced in `/tmp`, standalone PowerShell, not the audited repo):** I extracted
`verify-p3b.ps1`'s exact `Get-PlaceholderNormalizedText`/`Test-PlaceholderViolation` functions
verbatim into an isolated script and ran:
```
$adv = "Owner: T" + [char]0x0412 + "D pending decision"   # Cyrillic VE (U+0412) in place of Latin B
Test-PlaceholderViolation -Text $adv   # => False
```
The text reads as "Owner: TBD pending decision" to a human eye (Cyrillic `В`, U+0412, is the
canonical Latin-B confusable P3-A's own table maps back to `B`), but P3-B's verifier's
`Test-PlaceholderViolation` returns `False` — it does not detect this as a placeholder. P3-A's own
verifier, run against the same string, would detect it (its confusables table maps `0x0412` to
`'B'` before applying the `\b(TBD|...)\b` pattern).

**Does this affect the currently-delivered content?** No — I separately grepped every file this
parcel changed for literal `TBD|TODO|FIXME|\{\{` and found no genuine violation in new content
(the only literal "TBD" matches are (a) pre-existing, untouched rows in `INDEX.md` from an
unrelated initiative, correctly excluded by check 11's added-lines-only scoping for that file, and
(b) the pattern-definition strings inside `parcel-spec.schema.json`/`verify-p3b.ps1` themselves,
which check 11 does not scan as content for the reasons given above). This is a verifier
robustness gap, not current drift.

**Smallest amendment:** Port P3-A's confusables-skeleton table and its live-pattern
cross-assertion into `verify-p3b.ps1`'s `Get-PlaceholderNormalizedText`/pattern-definition block,
exactly as `verify-p3a.ps1` already does, since the Hard constraints section explicitly says this
mechanism is reused, not redeclared.

**Changes a locked decision?** No.

### F3 — MAJOR (verifier): check 4's code comment claims an independent byte-identity verification of the disclosed 17th path that the code does not perform

**Claim:** Check 4's trailing comment block (script lines ~579-583) states: "The disclosed deviation
path (`parcels/P3-B.md`) is independently verified byte-identical against the already-reviewed
upstream commit `7b3225c` on `docs/p3b-shaping`, since no `BaseCommit`-relative diff is possible for
a path absent at `BaseCommit`." The actual code immediately following this comment only (a) asserts
the file's existence (`Test-Path`) and (b) asserts its leading YAML frontmatter is parseable
(`ConvertFrom-FrontmatterText`) — it never reads, hashes, or diffs the file against commit `7b3225c`
or any other reference. No other check in the script performs this comparison either.

**Evidence (reproduced in a throwaway git clone at `/tmp`, never the audited worktree):**
1. `git clone /home/cmorgan76/Repos/biostack-wt/p3b-impl /tmp/p3b_sandbox; cd /tmp/p3b_sandbox; git checkout feat/p3b-schema-binding`.
2. Edited `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md`'s Objective paragraph only
   (not the frontmatter — e.g., "Bind BioStack's generic parcel-spec schema (P3-A) to the" →
   "Bind BioStack's generic parcel-spec schema (P3-A), subtly altered by tampering, to the") and
   committed it.
3. `git diff --stat 7b3225c -- .../P3-B.md` confirms the sandbox copy is no longer byte-identical
   to the already-dual-reviewed commit.
4. Re-ran `verify-p3b.ps1` with the identical `BaseCommit`/`BuilderId`/`ReviewerIds`/
   `EvidenceDirectory` arguments used against the real repo. Result: **`P3-B verification PASS`** —
   all 13 checks, including check 4, pass unchanged.

This means the one mechanism this parcel actually has for defending the integrity of the one
non-pinned, out-of-band path it introduces is presence-plus-parseability only — any prose
alteration to this spec file (reinterpreting scope, softening a constraint, adding a claim) would
sail through every one of the 13 checks undetected, as long as the frontmatter block is untouched
and still declares empty `guidance_classes`/`substance_function_risk` (so the self-reference
carve-out still applies). Separately I independently confirmed (outside this tool, directly, not
relying on the verifier) that the **actual delivered** `P3-B.md` at `dd92fa5` *is* byte-identical to
`7b3225c` (`diff <(git show 7b3225c:.../P3-B.md) <(git show dd92fa5:.../P3-B.md)` — zero output;
matching SHA-256 `b4535c95...`), so this finding is about the verifier's structural soundness, not
about present drift.

**Smallest amendment:** Add an explicit check (either a new numbered check or folded into check 4)
that does what the comment already claims: `git show 7b3225c:<path>` vs. the working-tree file,
byte-for-byte, failing closed if they differ, or — if the coordinator instead ratifies option (b)
of F1 — ties this assertion to whatever upstream reference commit the reconciliation names.

**Changes a locked decision?** No.

### F4 — MINOR (spec-prose inconsistency, already correctly resolved by the builder): the approved spec's own description of `positive-escalation-stage4-interaction-signal.json` is internally inconsistent about `numeric_provenance`

**Claim:** P3-B.md's document-contract-4 description of this fixture states: "`numeric_provenance`
omitted (no `deterministic-calculation` entry and no `curated-evidence-guidance` entry present)."
But the same spec's own `CAPABILITY-FIELD-MAP.md` `numeric_provenance` row (document contract 2)
pins the trigger as "at least one `capability_claim` entry's `guidanceClass` is
`deterministic-calculation` **or** `personalized-protocol-recommendation`" (unconditional for
either) — and this fixture's sole `capability_claim` entry's `guidanceClass` **is**
`personalized-protocol-recommendation`. By the field map's own rule, `numeric_provenance` is
therefore **triggered**, not exempt, for this fixture; the spec's fixture-description prose
overlooked its own field-map rule.

**Evidence:** `docs/specs/schemas/fixtures/p3b/positive-escalation-stage4-interaction-signal.json`
as delivered **includes** `"numeric_provenance": ["source-studied"]` — the builder correctly
followed the live field-map rule rather than the spec's erroneous descriptive prose, and
`verify-p3b.ps1`'s check 9 independently recomputes this fixture as `valid` (confirmed: re-running
the fixture with `numeric_provenance` *omitted*, matching the spec's literal prose, would make the
field-map's own trigger fire with the key absent, which the validator's own logic maps to
`missing-required-frontmatter-key` — i.e., following the spec's literal prose here would have
produced an incorrect, non-conforming fixture).

**Smallest amendment:** Fix the approved spec's fixture-10 description in a future spec amendment
to read "`numeric_provenance` populated (`personalized-protocol-recommendation` unconditionally
triggers this key per the field map's own rule)" instead of "omitted." No file in this
implementation needs to change — the delivered fixture is already correct.

**Changes a locked decision?** No.

## Missing pieces / collisions / unknowns

- Dual-review envelope is correctly architecture + standard class (charter D8), two independent
  reviewer IDs named in the Gate 2 record (`p3b_impl_review_1`, `p3b_impl_review_2`), consistent
  with this review's own identity.
- P3-A's carry-over item 3 (untested `coordinator-parcel`-shape branch) — re-read against
  `P3-A.md`'s own quoted disposition text ("P3-B's own dispatch must either (a) include a fixture
  or compatibility-set row exercising the `coordinator-parcel` branch against its own conforming
  spec file, or (b) explicitly re-affirm this gap's continuation") — is discharged via option (a):
  `positive-coordinator-parcel-p3b-self.json` points at the real, merged `parcels/P3-B.md` and the
  verifier evaluates it under the `coordinator-parcel` shape with the self-reference carve-out
  correctly scoped (confirmed: this file's frontmatter has both `guidance_classes` and
  `substance_function_risk` empty and no `function_review_status` key, exactly matching the
  carve-out's precondition). Independently re-verified `valid` is the correct result.
- No `TBD`/`TODO`/`FIXME`/`{{...}}` placeholder exists in any genuinely new content this parcel
  ships (confirmed by direct grep across the full changed-file set, with the two caveats discussed
  under F2's "does this affect the currently delivered content" — neither is a real violation).
- I did not find any case where a bound field's live cross-reference rule hardcodes or copies a
  contract value as a static literal inside `CAPABILITY-FIELD-MAP.md` or `verify-p3b.ps1`'s
  `Test-CapabilitySafetyOverlay` (every comparison reads `$Contract.*` live at verification time) —
  AC-P3B-07/"Composed, not hardcoded" is honored for the product-semantic surfaces. The only
  "hardcoded, not read live" gap I found is the placeholder-pattern constant itself (F2), which is
  outside the contract-semantics scope that constraint names but still a real deviation from "reuse
  P3-A's mechanisms."

## Verification notes

What I checked and found clean (independently, against the actual repository, not relying on any
self-attestation in the diff or commit messages):

1. **Scope/changed-file set:** `git diff --name-only 789a610...dd92fa5` = exactly the 17 files
   named in the commit message (16 pinned + the disclosed `parcels/P3-B.md`); no other path
   touched; `frontend/`, `backend/`, `contracts/`, `.github/` untouched (not even listed in the
   diff — confirmed by their total absence from the changed-file list).
2. **extensionSections additive-only invariant:** `diff <(git show 789a610:.../parcel-spec.schema.json) <(git show dd92fa5:...)` shows exactly one key (`extensionSections`) changed, from `{}` to the one pinned `product-capability-safety-overlay` object with the exact six `boundFrontmatterKeys` and other five fields pinned in document contract 1; normalized term `product capability safety overlay` confirmed disjoint from the live `requiredSpecAdditions` union in `delivery-class-controls.json` (direct enumeration of all seven delivery classes' `requiredSpecAdditions` arrays — no collision).
3. **CAPABILITY-FIELD-MAP.md fidelity:** programmatically extracted the pinned table (and the "Trigger evaluation is cumulative" paragraph) from both P3-B.md's document contract 2 and the delivered `CAPABILITY-FIELD-MAP.md` and diffed them — byte-identical.
4. **classification-axes.schema.json scope:** `diff` shows exactly `controlSource`/`controlBindingStatus` under `productGuidanceClass` changed, to exactly the pinned values; nothing else in the file differs.
5. **D→E stage-4 fixture:** independently re-derived from the live contract — `labels.interaction-or-contraindication-signal.behavior.C3.value` = `"degraded-escalates-on-strong-signal"` (matches fixture); `preemptionOrder.stages[0..2].labels` does not contain `interaction-or-contraindication-signal`; `stages[3]` ("calibrating labels union their obligations") is the correct residual rule, yielding `preemptionStage: 4` — matches the fixture and the verifier's independent computation.
6. **Negative fixtures genuinely fail, and the checker is non-vacuous:** ran the real `verify-p3b.ps1` (`pwsh -NoProfile -ExecutionPolicy Bypass -File docs/specs/scripts/verify-p3b.ps1 -BaseCommit 789a6106381fffe1dde99f94c702ccb7b3761a15 -BuilderId p3b_builder -ReviewerIds p3b_impl_review_1,p3b_impl_review_2 -EvidenceDirectory artifacts/p3b-verification`) against the real worktree — `P3-B verification PASS`, 13/13 checks, `fixture-results.json` shows all six negative fixtures independently recomputed as `invalid` with the exact pinned `reason`. In a throwaway sandbox clone, weakening `negative-claim-behavior-drift.json`'s claimed `behavior` to match the live (non-drifted) cell value, while leaving `expected.result: "invalid"` unchanged, made the verifier throw and fail with a clear mismatch message — confirming check 9 recomputes rather than trusting fixtures' own `expected` blocks.
7. **Frozen P0-B/P1/P2/P3-A artifacts:** independently re-hashed nine named frozen paths (charter, both P0-B contract artifacts, `SECTION-HEADING-MAP.md`, `EXTENSION-POINTS.md`, `delivery-class-controls.json`, `fold-engine.md`, `routing-output.schema.json`, `AXIS-REGRESSION-MAP.md`) at `origin/main` (`789a610`) vs. the implementation tip (`dd92fa5`) — all nine SHA-256 digests match exactly.
8. **17th-path byte-identity:** independently diffed `git show 7b3225c:parcels/P3-B.md` against `git show dd92fa5:parcels/P3-B.md` — zero output, SHA-256 match (`b4535c95...`). The delivered content is genuinely byte-identical to the named upstream commit, even though (F3) the verifier itself does not assert this.
9. **Carry-over discharge:** `positive-coordinator-parcel-p3b-self.json` correctly points at the real merged spec file; re-confirmed the file's own frontmatter (empty `guidance_classes`/`substance_function_risk`, no `function_review_status` key) matches the self-reference carve-out's stated precondition exactly.
10. **Clean-tree/evidence discipline:** ran the verifier end-to-end; `artifacts/p3b-verification/` was created untracked, `verification-summary.json` named all 13 checks `pass: true` plus `BaseCommit`/`HeadCommit`/`BuilderId`/`ReviewerIds`, and `git status --porcelain=v1 --untracked-files=all` showed only `?? artifacts/p3b-verification/...` lines, matching check 13's requirement. Evidence directory removed by this reviewer afterward to leave the worktree exactly as found (read-only discipline).
