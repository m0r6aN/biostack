# p0a_reverify_2 — P0-A remediation independent re-verification

**Verdict:** PASS-WITH-FIXES
**Subject:** P0-A remediation, worktree `/home/cmorgan76/Repos/biostack-wt/p0a-remediation-1`, branch `fix/p0a-remediation-1`, tip `f1d00c8653b9f68933fcdee5b97d1933338a68fa`
**Files re-verified (SHA-256, current worktree state):**
- `docs/specs/schemas/canon-precedence.md` = `73d199e04b8af8e7000f360bd0e4e0bd55e69042f7ed73dfa9a1848aa160309f`
- `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md` = `00482564dbbb54e3e14925430cbb48010c465de7a4220a6c079c570311ab5cff`
- `docs/specs/scripts/verify-p0a.ps1` = `8aa485515b297a3ddfa1daba0e35305a83129a4588885a916b770c16c63ef5f8`
**Date:** 2026-10-09

## Task (a) — F1 hazard re-opening attempt: D-K / CI-002 / CI-005

Re-read Coordinator Decision D-K in full
(`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`, `## D-K` heading) and constructed the
mechanical scenario it describes: applying `canon-precedence.md`'s total order to CI-002
(CHARTER.md rank 3 vs. `docs/canon/biostack-protocol-intelligence-canon.md` rank 5) and CI-005
(CHARTER.md rank 3 vs. `docs/product/knowledge-engine-capability-map.md` rank 8) resolves
document authority in favor of the broader/more permissive CHARTER.md text in both cases.

Verified:
- `canon-precedence.md` new §4a transcribes D-K's blockquote constraint verbatim (byte-identical
  to the source in `COORDINATOR-DECISIONS-2026-10-07.md`, confirmed by direct string comparison
  of both quoted blocks — see Verification notes). §4a explicitly states the constraint "governs
  the application of section 3's `compare(A, B)` function wherever its outcome would otherwise be
  read as resolving, weakening, or superseding a narrower safety prohibition," and that
  `compare` itself is unchanged — this is the correct scoping; it does not alter the registry or
  the comparison function's total order.
- `CONTRADICTION-INVENTORY.md`'s CI-002 and CI-005 rows both now carry an explicit **D-K
  constraint notice** inside `proposed_disposition`, naming the exact rank pair (CHARTER.md rank
  3 vs. the narrower document's rank), stating the prohibition "stands unchanged until an
  explicit owner ruling supersedes it," and routing the row to the owner via P0-D. Both rows'
  `status` remains `contradictory` and `proposed_disposition` leading token remains `fix` — no
  row was silently re-labeled `resolved-by-owner-ruling` or `consistent` as a side effect of D-K
  or of rank.
- Checked every other place CI-002/CI-005 are mentioned (`P0-A.md` seed table,
  `CORPUS-COVERAGE-MATRIX.md`, `SOURCE-MANIFEST.md`) for contradicting wording that could let a
  reader apply rank mechanically without D-K's caveat. Found none — all cross-references defer to
  P0-D/owner ruling, consistent with D-K.
- Checked `verify-p0a.ps1`'s new `dk-directional-constraint-present` check (lines ~690-706): it
  asserts D-K's reference and all seven blockquote lines are present verbatim in
  `canon-precedence.md`. This is a genuine, non-trivial deterministic check, not a rubber stamp —
  confirmed by independently re-running it (see Verification notes) and by hand-diffing the two
  quoted blocks.

**Conclusion on (a):** I could not construct a reading, mechanical or otherwise, in which the
current artifact set permits a builder to treat CI-002/CI-005's rank resolution as license to
weaken the safety prohibitions. The F1 hazard is closed. No wording was found where D-K could be
ignored or contradicted by other manifest text.

## Task (b) — adversarial break of the new quotation-content check

Reproduced `verify-p0a.ps1`'s `ConvertTo-NormalizedQuoteText` function verbatim in an isolated
PowerShell script at `/tmp/p0a_adv_test.ps1` (not committed, no repository files touched) and ran
it with the real `pwsh` engine against the real cited file content at `BaseCommit`.

**Finding (MAJOR-leaning, reproducible):** the normalization step collapses *all* whitespace
(`\s+` → single space) uniformly across the entire file, including blank lines, markdown
headings, and — critically — the boundary between a document's permissive (`BioStack may:`) list
and its prohibition (`BioStack must not:`) list, whenever those two lists are adjacent in file
order with no intervening heading (true of `docs/canon/biostack-protocol-intelligence-canon.md`,
cited by CI-001/CI-002/CI-003). A crafted quotation that splices the final "may" bullet directly
onto the start of the "must not" list passes `quotation-content-verified` as a legitimate,
verbatim, contiguous quotation:

```
Adversarial quote (normalized):
Track GLP-1 observability domains such as appetite, GI symptoms, hydration, protein adequacy,
lean-mass proxy, mood, alcohol-craving notes, and discontinuation events. BioStack must not:

Matches as contiguous substring in real cited file (BaseCommit content)?: True
```

This demonstrates the check verifies "is this a literal contiguous span of the cited file,
modulo whitespace/bullet/bold decoration" — not "does the source state this as one coherent
claim." A CI-NNN row author (adversarial or merely careless) could splice text across a bullet
boundary, a paragraph boundary, or (if the heading text itself were included) even a section
boundary, and still pass this check.

**Mitigating factors, why this does not reopen F1 or invalidate the remediation:**
- D-K's own transcription is verified by a *separate* check (`dk-directional-constraint-present`)
  that does exact-line blockquote matching (`precedenceContent.Contains("> $line")`), not the
  splice-prone normalized-substring method — D-K's text itself is not exposed to this weakness.
- I independently re-verified, by direct inspection against the real source files (see Task (c)),
  that every quotation actually present in CI-001, CI-002, and CI-005 today is a genuine,
  non-spliced, in-context quotation. The weakness is latent/potential, not currently exploited in
  this delivery's own content.
- This check did not exist before this remediation (it is new, closing F2); its guarantee is
  narrower than its name implies, but it is still strictly more protective than the pre-remediation
  state (which only verified the cited *path* existed, not any content at all).

**Recommended smallest amendment:** tighten `ConvertTo-NormalizedQuoteText` (or add a second,
stricter pass used only for flagging, not blocking) to refuse collapsing across a blank-line-plus-
different-list-marker boundary (e.g., disallow a normalized match whose un-normalized span crosses
a `\n\n` double-newline or a line matching `^(BioStack (may|must not):|##? )`), so a spliced quote
spanning two originally separate lists/sections fails verification. This does not change any
ranking, disposition, or D-K text — purely a verification-script hardening.

## Task (c) — D-K verbatim + total order unchanged

- Directly diffed the quoted blockquote in `canon-precedence.md` §4a against the source blockquote
  in `COORDINATOR-DECISIONS-2026-10-07.md` `## D-K` — byte-identical (confirmed programmatically,
  both quote extractions matched exactly including markdown bold markers `**document authority
  order only**`, `**stands unchanged**`).
- `git diff 2031370 f1d00c8 -- docs/specs/schemas/canon-precedence.md`: the *only* change is the
  insertion of new `## 4a` section between the pre-existing `## 4. Scope boundary statement` and
  `## 5. Extension rule`. The ranked registry (section 2, ranks 1-14), the comparison function
  (section 3), and the three-property rule (section 1) are byte-for-byte unchanged. Confirmed via
  diff output directly (not re-stated from the builder).
- Independently re-ran `verify-p0a.ps1`'s `precedence-manifest-totality` and
  `precedence-rank-density` checks (via full script execution, see Verification notes): ranks
  1..14 remain dense, gap-free, and unique; zero open-ties; totality statement intact.

**Conclusion on (c):** confirmed. D-K's transcription is verbatim; the manifest's total order is
otherwise unchanged.

## Task (d) — new defects in the delta

Independently re-ran the full `verify-p0a.ps1` script against this worktree's actual current
state (not a hypothetical), exactly as the remediation commit message claims
("Re-ran verify-p0a.ps1 ... 20/20 checks PASS"):

```
pwsh -NoProfile -File docs/specs/scripts/verify-p0a.ps1 \
  -BaseCommit b78e7de8463a3db410c45cf223cd722713fec816 \
  -BuilderId p0a_builder -ReviewerIds p0a_impl_review_1,p0a_impl_review_2 \
  -EvidenceDirectory artifacts/p0a-verification
```

**Finding (MAJOR, reproducible, new in this delta):** all 20 individual `Assert-True`-backed
checks print `PASS:`, but the script then throws an **unhandled terminating exception** —
`Cannot bind argument to parameter 'Content' because it is an empty string.` — and exits with
code 1. No `verification-summary.json` evidence file is written; `artifacts/p0a-verification/`
is left empty. This is reproducible on demand in the current repository state (I ran it twice,
same result both times).

**Root cause, traced:** the remediation's F4 fix (closing `p0a_impl_review_2` F4) pins the
`canonical-write-fencing-violation` check's committed-history diff base (`fencingBaseCommit`) to
`git merge-base HEAD '@{u}'` instead of the literal `BaseCommit`. In this worktree, the branch is
already pushed and `git rev-parse '@{u}'` equals `HEAD` exactly
(`f1d00c8653b9f68933fcdee5b97d1933338a68fa` both), so `git merge-base HEAD '@{u}'` also equals
`HEAD`. The resulting `git diff "$fencingBaseCommit...HEAD"` is therefore **always empty** for
this entire delivery's own three committed changes, and when combined with zero untracked and
zero uncommitted-working-tree changes, `$allChanged` is an empty array. `$allChanged -join "`n"`
then evaluates to an empty string, which PowerShell's `Mandatory` string-parameter binding on
`Write-Utf8Lf -Content` rejects outright, crashing the script before it reaches the evidence-bundle
write.

Confirmed this is **newly introduced** by this remediation, not pre-existing: the
pre-remediation script (commit `2031370`) used the literal `$BaseCommit` (the Gate 2 dispatch
anchor, `b78e7de8...`) for this diff unconditionally — a non-empty diff against the current HEAD
in every case, so this crash could not occur under the pre-remediation script regardless of
push/upstream state.

**Impact:** this is exactly the re-verification posture a reviewer or coordinator will have any
time they check out this already-pushed branch and attempt to independently reproduce the
builder's 20/20 claim (the normal governed-delivery re-verification workflow) — it is not an edge
case specific to my environment. The claim "Re-ran verify-p0a.ps1 ... 20/20 checks PASS" in the
`f1d00c8` commit message is accurate only for the specific moment/working-tree-state the builder
ran it in (almost certainly before pushing, when `@{u}` had not yet converged with `HEAD`); it is
not reproducible as a complete, evidence-bundle-producing run against the delivered, pushed state
today.

**Recommended smallest amendment:** when `$fencingBaseCommit -eq $headCommit` (merge-base equals
HEAD, i.e., nothing unpushed/undiverged remains to diff on the committed-history dimension), skip
the committed-history diff step rather than feeding it an always-empty result into the same
code path that also must handle a genuinely non-empty change set; and/or special-case
`Write-Utf8Lf`'s call site for `changed-files.txt` to pass an explicit `"(no changes)"` sentinel
or use `[AllowEmptyString()]` on `Write-Utf8Lf`'s `$Content` parameter so an empty-but-valid
result does not crash the script. Either change preserves the F4 fix's intent (excluding
unrelated later main-line commits) without making the check silently vacuous *and* crash-prone on
the normal post-push re-verification path.

No other new defects found in the delta. `verify-p0a.ps1`'s other new/changed logic (F2's
quotation-content-verified minus the Task (b) caveat above, F3's CI-001 restoration, R1-MINOR-1's
`handoff_target` field move) all independently re-verified correct against real source files and
real diffs.

## Ranked findings

1. **ID:** reverify2-F1, **severity:** MAJOR. **Claim:** `verify-p0a.ps1` crashes with an
   unhandled exception and produces no evidence bundle when re-run against this worktree's actual
   current (pushed) state, due to the F4 fencing-base pin converging with HEAD. **Evidence:**
   reproduced twice, `docs/specs/scripts/verify-p0a.ps1` lines ~150-176 (fencingBaseCommit logic)
   and line 529 (`Write-Utf8Lf -Path ... 'changed-files.txt' -Content ($allChanged -join "`n")`);
   `git rev-parse '@{u}'` = `git rev-parse HEAD` = `f1d00c8...`; exit code 1 both runs. **Smallest
   amendment:** guard the empty-$allChanged case before the `Write-Utf8Lf` call (see Task (d)
   above). **Changes a locked decision?** No — this is a verification-script robustness fix, not
   a canon/ranking/disposition change.
2. **ID:** reverify2-F2, **severity:** MINOR (documented weakness, not currently exploited).
   **Claim:** `quotation-content-verified`'s normalization collapses structural boundaries
   (bullet lists, paragraphs, adjacent sections), so a spliced, boundary-crossing quotation can
   pass as "verbatim." **Evidence:** reproduced with the real `pwsh` engine running the script's
   own `ConvertTo-NormalizedQuoteText` function against the real cited file at `BaseCommit` (Task
   (b), full repro above). **Smallest amendment:** reject normalized matches whose source span
   crosses a double-newline or a differently-labeled list marker (see Task (b) above). **Changes
   a locked decision?** No.

## Missing pieces / collisions / unknowns

None found beyond the two findings above. D-K's substance, its verbatim transcription, the
CI-002/CI-005 constraint notices, and the manifest's otherwise-unchanged total order are all
independently confirmed sound.

## Verification notes

- Read `COORDINATOR-DECISIONS-2026-10-07.md` in full (D-A through D-K) and
  `canon-precedence.md` in full (sections 1-5, including new 4a).
- Read `CONTRADICTION-INVENTORY.md` CI-001 through CI-011 in full; confirmed CI-002/CI-005 D-K
  notices and CI-001's restored Class D qualifier against the real
  `docs/guidance/biostack-guidance-content-contract.v1.md` text (lines 109-115).
- `git diff 2031370 f1d00c8 -- docs/specs/schemas/canon-precedence.md` and
  `-- .../CONTRADICTION-INVENTORY.md`: reviewed both hunks in full; confirmed scope matches the
  commit message's claimed F1 fix exactly, no unrelated changes.
- Programmatic byte-comparison of the D-K blockquote as it appears in
  `COORDINATOR-DECISIONS-2026-10-07.md` vs. as transcribed in `canon-precedence.md` §4a: exact
  match.
- Independently executed `docs/specs/scripts/verify-p0a.ps1` twice against the real repository
  state with `pwsh` (PowerShell 7.6.6): all 20 named checks print `PASS:`; script then throws and
  exits 1 before writing the evidence bundle (see Task (d)/Finding reverify2-F1).
- Independently executed an isolated, read-only adversarial test
  (`/tmp/p0a_adv_test.ps1`, not committed) reproducing `ConvertTo-NormalizedQuoteText` against the
  real `docs/canon/biostack-protocol-intelligence-canon.md` content at `BaseCommit`: confirmed the
  cross-boundary splice passes (see Task (b)/Finding reverify2-F2).
- No repository files were edited, created, or deleted other than this single output file. No
  git write commands were run. Did not read other reviewers' output files in `.audit/reviews/`.
