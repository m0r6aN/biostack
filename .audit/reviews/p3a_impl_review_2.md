# p3a_impl_review_2 — P3-A implementation review

**Verdict:** PASS-WITH-FIXES
**Role:** Independent read-only adversarial implementation reviewer (retrospective)
**Commit reviewed:** `4fe58250d78012d134dbdf376a094567fe6d3277` (main, merge of PR #525)
**Implementation commit:** `c3b40076fc963a9c8d72d4cb7e0ef7a8a0b812f3` (`feat(governed-delivery): P3-A
parcel schema implementation`), parent `756c9d154d7a55a24fcc27f3b77ada4f04503028` (Gate 2
dispatch record = `BaseCommit`). Verified `git merge-base --is-ancestor c3b4007 4fe5825` = true.
**Build authority:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`,
SHA-256 `1c436a2ec4620881d1533bd543bfc1373379fcad0585fd23939146fd2843c398`
**Verifier reviewed:** `docs/specs/scripts/verify-p3a.ps1`,
SHA-256 `fae585f68965460fb770fc46226e58d50573830a82e0e0dfd3de541b886b1c66`
**Date:** 2026-10-09

## Summary

The shipped implementation (28 files, matching the spec's "Exact allowed surfaces" list exactly)
is well-constructed and, as shipped, honestly conforms to the letter of `parcels/P3-A.md` on
every item I spot-checked: 46-term `SECTION-HEADING-MAP.md` matches the live
`delivery-class-controls.json` union exactly; all eight templates carry the 8-word content floor
and the literal `## Extension points used` / `None.` block; `INDEX.md`/`README.md` link targets
are correct; `EXTENSION-POINTS.md` legitimately carries the pinned sentences in all three
subsections; the two no-`TBD` fixtures are each genuinely single-violation. I ran the actual
verifier (`pwsh 7.6.6`) against the real worktree at `BaseCommit=756c9d1` and it reports
`P3-A verification PASS` honestly.

However, per the review brief, I built and ran adversarial mutations against a throwaway clone in
`/tmp/p3a_adv/repo` (never touching the reviewed repo or worktree) to test whether
`verify-p3a.ps1` would catch deliberately nonconforming content. **Two independent, reproducible
exploits below make `verify-p3a.ps1` print `P3-A verification PASS` on content that plainly
violates the spec's own document contracts**, and I found one deeper structural gap (the
canonical-alias matching engine) between what document contract 2 defines and what the verifier
actually executes. These are implementation defects in the *verifier*, not defects in the shipped
deliverables' content as actually authored — the shipped files are not nonconforming, but the
gate that is supposed to prove they (and any future rework) are conforming has exploitable holes.

## Ranked findings

### F1 — BLOCKER (empirically demonstrated): check 11's INDEX.md link-cell validation accepts
any href, including a nonexistent path, as long as a regex shape matches

**Claim:** Document contract 9 requires the `INDEX.md` `P3-A` row's `Spec` cell to link to
`parcels/P3-A.md` and the `Goal Charter` cell to link to the charter. "Deterministic verification"
check 11 is supposed to enforce this ("parse the added row and require its ten cells to equal
exactly the pinned values in document contract 9"). In the actual `verify-p3a.ps1`, the assertion
is only:

```
Assert-True ($p3aCells[2] -match '^\[P3-A[^\]]*\]\(.+\)$') '... Spec cell must be a link to this file.'
Assert-True ($p3aCells[3] -match '^\[[^\]]*charter[^\]]*\]\(.+\)$') '... Goal Charter cell must be a link target naming the charter.'
```
(`docs/specs/scripts/verify-p3a.ps1`, lines ~843-844 in the shipped file). `(.+)` accepts *any*
non-empty parenthesized text as the href — it never resolves or compares the link target to
`docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md` or the charter path.

**Evidence (reproduced):** In `/tmp/p3a_adv/repo` (clone of the real governed-delivery worktree
at `c3b4007`), I rewrote only the `P3-A` row's `Spec` and `Goal Charter` cells to:
`[P3-A spec-ish totally wrong](../nonexistent/bogus-path-does-not-exist.md)` and
`[some charter reference](../also/bogus/nowhere.md)` (single-line diff, confirmed via
`git diff 756c9d1...HEAD -- docs/specs/INDEX.md`, no other rows touched). I then ran:

```
pwsh -NoProfile -ExecutionPolicy Bypass -File docs/specs/scripts/verify-p3a.ps1 \
  -BaseCommit 756c9d154d7a55a24fcc27f3b77ada4f04503028 -BuilderId p3a_builder \
  -ReviewerIds p3a_impl_review_1,p3a_impl_review_2 -EvidenceDirectory artifacts/p3a-verification
```
Output: `P3-A verification PASS`.

**Smallest amendment:** In check 11, additionally assert the extracted href equals exactly
`docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md` (resolved relative to
`docs/specs/INDEX.md`) for the `Spec` cell, and resolves to the charter path cited in "Lineage and
dependencies" for the `Goal Charter` cell — i.e. compare the captured `(.+)` group against the
pinned literal path, not just assert its shape.

**Does it change a locked decision?** No — this tightens an existing, already-pinned acceptance
criterion (AC-P3A-10/document contract 9); it adds no new obligation.

### F2 — BLOCKER (empirically demonstrated): check 7's EXTENSION-POINTS.md pinned-sentence check
is a global file-wide count, not a per-subsection presence check

**Claim:** Document contract 3 states explicitly: "**Each extension point's subsection must
also state, verbatim, the sentence**..." (the non-binding sentence), for all three named
extension points individually. The actual check is:

```
$nonBindingCount = ([regex]::Matches($ExtPointsText, [regex]::Escape($NonBindingSentence))).Count
Assert-True ($nonBindingCount -ge 3) 'EXTENSION-POINTS.md must contain the non-binding sentence at least once per extension point (>=3 total).'
```
This counts occurrences anywhere in the whole file and only requires the aggregate to reach 3 —
it never confirms each of the three subsections (`delivery-class-extension`,
`domain-overlay-insertion`, `template-set-extension`) individually carries its own copy.

**Evidence (reproduced):** In the same sandbox clone, I removed the sentence from the
`domain-overlay-insertion` and `template-set-extension` subsections entirely (0 occurrences in
each) and added two extra copies inside `delivery-class-extension` instead, so the file-wide
total stayed at 4 (`>= 3`). `awk` confirmed the per-subsection distribution: 3 occurrences under
`delivery-class-extension`, 0 under the other two. Re-running `verify-p3a.ps1` with the same
command as F1 again printed `P3-A verification PASS`.

**Smallest amendment:** Split `EXTENSION-POINTS.md` into its three `##` subsections (the same
split pattern used for `AppendOnlySentence`/`DisjointnessSentence` string-containment checks a
few lines later) and assert the non-binding sentence appears at least once inside *each*
subsection's own text span, not merely `>= 3` globally.

**Does it change a locked decision?** No — tightens AC-P3A-03/document contract 3 to what it
already states.

### F3 — MAJOR: the fold-live required-section resolver never consults
`SECTION-HEADING-MAP.md`'s "Canonical alias(es)" column — the alias-matching rule in document
contract 2 is not actually implemented

**Claim:** Document contract 2 defines satisfaction as: "a heading... contains... the same
normalization applied to **the term itself or to any listed alias**," and gives worked examples
of terms needing a materially different alias (`tests` → "Deterministic verification";
`missingness` → "Missing-input behavior"; `data inventory` → "Data map"). `SECTION-HEADING-MAP.md`
is pinned as `sectionHeadingMapSource` in the schema and is supposed to be the live bridge the
resolver consults.

**Evidence:** `Resolve-RequiredSections` (`docs/specs/scripts/verify-p3a.ps1`, function at
~line 310) takes only `(Terms, Headings)` — it never receives or reads any alias data. All four
call sites (`Test-ParcelSpec` line ~479, template pre/post checks lines ~656/~679) pass only the
raw `requiredSpecAdditions` term strings and the document's headings; `SECTION-HEADING-MAP.md`'s
parsed `$mapRows`/`$mapTerms` variables exist only inside check 6's own scope (lines ~597-611),
used solely to assert the map's *term column* equals the live union — never to feed alias data
into matching. `grep -n "Resolve-RequiredSections"` confirms these are the only call sites.

This is currently **invisible in practice** because the shipped `SECTION-HEADING-MAP.md` sets
every term's "Canonical alias(es)" cell to the term itself (e.g. `tests | tests |
delivery-class-controls.json`), sidestepping the spec's own worked examples rather than using
materially different aliases, so raw-term matching happens to be sufficient for every template,
fixture, and the real-spec compatibility pass as shipped. But the matching engine itself cannot
honor an alias even if one were legitimately declared: this directly undermines (a) the
"generic, extensible" framing P3-A's objective claims, and (b) `EXTENSION-POINTS.md`'s own
`delivery-class-extension` mechanic, which explicitly anticipates a future amending parcel
appending "an additional alias to an existing row" — per the current engine, that future alias
would be parsed, shape-checked, and committed, but silently never consulted at resolution time,
producing a false `missing-required-section` for any heading that legitimately uses it instead of
the literal term text.

**Smallest amendment:** Thread `SECTION-HEADING-MAP.md`'s parsed alias lists into
`Resolve-RequiredSections` (one term → one-or-more candidate normalized strings, matched by the
same contiguous-token-subsequence rule against each candidate) instead of matching only the raw
term string.

**Does it change a locked decision?** No — implements an already-pinned document contract rule
that is currently unimplemented; does not add scope.

### F4 — MINOR: check 4's "frozen at BaseCommit" census is computed from the current working
tree/HEAD, not from `BaseCommit`, deviating from the spec's literal wording

**Claim:** Check 4 is specified as: "`git diff --quiet` ... for ... every path returned by
`git ls-files docs/specs/active docs/specs/done` **at `BaseCommit`**." The implementation runs
`Invoke-Git -Arguments @('ls-files', 'docs/specs/active', 'docs/specs/done')` with no `BaseCommit`
ref/tree argument, which enumerates the current index/working tree (effectively HEAD), not
`BaseCommit`'s tree. Same pattern for the `P2FixturePaths` enumeration a few lines later.

**Practical impact:** low — check 3's exact-28-file `Assert-SequenceEqual` against
`git diff --name-only BaseCommit...HEAD` independently catches any addition, deletion, or
modification to a path outside the 28 allowed surfaces regardless of this census's accuracy, so I
could not construct a scenario where this specific deviation alone allows a nonconforming result
to slip past both checks. It is nonetheless a literal deviation from the spec's pinned wording and
a latent risk if check 3 is ever weakened or reused independently (e.g. by a future P4 linter that
reuses this pattern without check 3's redundancy).

**Smallest amendment:** `git ls-files --with-tree=$BaseCommit docs/specs/active docs/specs/done`
(or equivalent `git ls-tree -r --name-only $BaseCommit -- ...`) instead of a bare `git ls-files`.

**Does it change a locked decision?** No.

### F5 — MINOR: the UTS #39 confusables-skeleton step is a ~35-codepoint hand-picked map, not
the actual UTS #39 bundled table; two placeholder-disguise shapes outside the spec's named
disguise classes also evade the pipeline

**Claim:** Document contract 1 step 9 calls for "the Unicode Technical Standard #39
confusables-skeleton transform (a deterministic, offline, bundled-data-table lookup...)." The
shipped `$script:ConfusablesMap` (`verify-p3a.ps1` ~lines 379-387) is a manually curated table of
~35 Cyrillic/Greek Latin-lookalike codepoints, not the UTS #39 confusables data file. The spec's
own "Scope of this claim" disclaimer in Hard Constraints explicitly concedes this exact gap is
"a structural ... obligation on the verifier rather than a spec-level fixture claim," so this is
not a fixture-contract violation, but it is a real, verifiable gap between the document contract's
literal wording ("the ... UTS #39 ... transform") and the shipped code (a small hand-picked
subset), worth recording as evidence.

Separately, and not discussed anywhere in the spec's disguise-class enumeration: I confirmed (via
a standalone extraction of `Get-PlaceholderNormalizedText` run in `/tmp/p3a_adv/test_norm.ps1`)
that inserting a combining diacritical mark (category `Mn`, e.g. U+0300 COMBINING GRAVE ACCENT,
`T` + U+0300 + `BD`) or a plain, non-markup punctuation character directly between letters (e.g.
`T·BD`, middle dot) both survive the full 9-step pipeline unchanged and do **not** match
`\bTBD\b` after normalization — unlike the correctly-caught zero-width-space control case I also
tested (`T\u200BB\u200BD`, which the pipeline does collapse and match, confirming steps 1-9 work
as designed for their stated classes). Neither combining-mark nor bare-punctuation insertion is
one of the three disguise classes the spec's Hard-constraints "Scope of this claim" paragraph
names (homoglyph/zero-width/fullwidth/math-alphanumeric; markdown/HTML-syntax-splitting;
HTML-entity-splitting), so this is not a breach of a specific document-contract promise, but it is
a genuine, easily-reproduced placeholder-laundering path the pipeline misses, worth naming per the
review brief's task (e).

**Smallest amendment:** none required to satisfy the spec as written (both gaps fall outside its
named disguise-class commitments); if closing them is desired, add a `strip-unicode-category-Mn`
normalization step and/or bundle the real UTS #39 data table.

**Does it change a locked decision?** No.

### F6 — MINOR (confirms, does not newly report): the 8-word "content-quality floor" (check 8)
is trivially satisfiable by non-prose tokens

The spec's own "Structural validation only" disclaimer in Hard Constraints already states this
check counts "whitespand-delimited word tokens," not substantive prose, so a section body
reading `a a a a a a a a` would pass. I confirmed the implementation (`$wordTokens.Count -ge 8`
on `$strippedBody -split '\s+'`) matches this disclaimed scope exactly — not a new finding, but
confirmed via code reading as requested; no amendment needed since the spec pre-disclaims this
precise limitation.

## Missing pieces / collisions / unknowns

- I could not find any per-subsection assertion anywhere in `verify-p3a.ps1` for the two other
  pinned sentences (`AppendOnlySentence`, `DisjointnessSentence`) either — both use the same
  whole-file `.Contains(...)` pattern as F2's sentence, but since each of those two sentences is
  pinned to occur exactly once (not "once per subsection"), this is lower risk than F2 and I did
  not attempt to construct a distinct exploit for them; flagging as an area worth the same
  per-subsection scoping fix applied to F2 for consistency.
- `README.md`'s check 11 assertions (`heading`/4 links/scope sentence) are also whole-file
  `.Contains(...)` checks against the final file text, not scoped to the diff-added block. I did
  not find an exploit here distinct from F1/F2's pattern (no pre-existing README content
  coincidentally satisfies these strings), but it is the same class of weak check and worth
  tightening alongside F1/F2 if the verifier is ever reused as a template for future parcels.
- I did not find any surface touched outside the allowed 28-file list, nor any frozen-artifact
  drift, in the actual shipped `c3b4007` commit — `git show c3b4007 --stat` shows exactly 28
  changed files and I independently ran `verify-p3a.ps1` against the real worktree (clean, no
  mutation) and it reported a genuine `PASS`.

## Verification notes

- Confirmed `4fe5825` is the current `main` HEAD and that `c3b4007` (28 files, 1847 insertions,
  0 deletions) is an ancestor merged into it via PR #525.
- Confirmed `BaseCommit` = `756c9d154d7a55a24fcc27f3b77ada4f04503028` from
  `docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P3A-IMPLEMENTATION.md`.
- Ran `verify-p3a.ps1` (PowerShell 7.6.6) against the real `biostack-wt/governance-p3a` worktree,
  unmodified, at the pinned `BaseCommit`: clean `P3-A verification PASS`.
- Independently recomputed the 46-term live union from `delivery-class-controls.json` in Python
  and confirmed it matches `SECTION-HEADING-MAP.md`'s row count (46) exactly.
- Spot-checked `negative-tbd-violation-literal.json`, `negative-tbd-violation-entity-disguised.json`,
  `negative-missing-required-field.json`, `negative-unknown-extension-point.json`: each is
  single-violation as required by document contract 6, and the placeholder text in the two `TBD`
  fixtures is positioned so neither of `noPlaceholderPatterns`' two regexes double-counts the one
  violation (verified the exact fixture text does not trigger the line-start `(TBD...)\s*[:|\-]`
  pattern in addition to the word-boundary pattern).
- Spot-checked `REAL-SPEC-COMPATIBILITY-SET.md`'s carry-over item 1 prose: it correctly describes
  the `BIO-PAIRWISE-00x` `INDEX.md` rows' ad hoc placeholder cells without ever writing the
  literal string `TBD` itself (confirmed against the real `INDEX.md` content, which does contain
  literal `TBD (coordinator to assign Gate 2)` / `[TBD]`) — this is a legitimate, deliberate
  design choice (not evasion): `REAL-SPEC-COMPATIBILITY-SET.md` is itself scanned by check 12's
  placeholder scan (it is not in `$PlaceholderScanExclusions`), so paraphrasing instead of quoting
  the literal avoids the file tripping its own no-`TBD` rule while still satisfying the spec's
  "named, surfaced finding" obligation (Carry-over item 1). I verified this reading is consistent
  and does not constitute placeholder-laundering.
- All adversarial reproduction artifacts live only under `/tmp/p3a_adv/` (a disposable clone); no
  write, commit, or mutation was made to `/home/cmorgan76/Repos/biostack` or
  `/home/cmorgan76/Repos/biostack-wt/governance-p3a`. No git write commands were run against
  either real repository or worktree.
