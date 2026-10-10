# p3a_reverify_1 — P3-A remediation re-verification

**Verdict:** PASS-WITH-FIXES
**Subject:** branch `fix/p3a-remediation-1` (PR #527), worktree
`/home/cmorgan76/Repos/biostack-wt/p3a-remediation-1`, tested hash `69acff8e4051deab1b4dbc598e1c24a0ba552083`
**Dispatch:** `docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P3A-REMEDIATION-1.md`
**Triage source:** `docs/INITIATIVES/biostack-governed-delivery/TRIAGE-2026-10-08.md` §Round 4
**Date:** 2026-10-09

## Summary

The six required `fix` rows in TRIAGE §Round 4 are genuinely closed at the code level, and I
independently reproduced both reviewer-demonstrated adversarial exploits **plus two of my own,
differently-constructed variants** against the committed, unmodified `verify-p3a.ps1` logic — all
four now fail as required. The three-file diff scope claim is exact. However, the PR's own exit
proof **honestly discloses, and I independently reproduced, that the literal committed
`verify-p3a.ps1` does not produce a clean PASS when invoked exactly as the dispatch's
verification contract and this re-verification's own instructions specify** (`BaseCommit
756c9d154d7a55a24fcc27f3b77ada4f04503028`): check 3 throws on a changed-file-count mismatch (28
expected, 33 actual) because five pre-existing, out-of-`AllowedSurfaces`, coordinator-owned files
(`COORDINATOR-DISPATCH-QUEUE.md`, `TRIAGE-2026-10-08.md`, this round's own dispatch record,
`P0-A.md`, and `P3-A.md`) legitimately changed on `main` between the original `756c9d1` anchor and
this remediation's base, none of which this parcel touched or was authorized to touch. The PASS
transcript in the PR body was produced by an **uncommitted, non-repo, local diagnostic copy** of
the script, not the actual shipped file. This is a real, disclosed gap between the dispatch's
literal verification contract ("checks 1–14 pass at the remediated hash") and what the shipped
artifact actually does when run as specified — not a defect in any of the six required fixes
themselves, and smallest-amendment-fixable (see Findings).

## (1) Six required fixes — verified closed

Checked via `git diff 13c5c3f...69acff8` (the builder's own commit, isolated from the coordinator's
prior triage/dispatch commit `13c5c3f`) and direct code/content reading:

| Fix | Evidence | Verdict |
|---|---|---|
| R2-F1 (BLOCKER) — check 11 href shape-only | `verify-p3a.ps1` lines ~903–917: `[regex]::Match($p3aCells[2], '^\[P3-A[^\]]*\]\(([^)]+)\)$')` then `[StringComparer]::Ordinal.Equals(...Groups[1].Value, $PinnedP3ASpecHref)` against pinned literal `../INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`; same pattern for the Goal Charter cell against `../INITIATIVES/biostack-governed-delivery/CHARTER.md`. | Closed |
| R2-F2 (BLOCKER) — check 7 whole-file `.Contains` | `verify-p3a.ps1` ~check 7: `Get-HeadingSections` splits `EXTENSION-POINTS.md` into named subsections; each pinned sentence is asserted via `$extSection.Body.Contains(...)`, scoped to its own named extension point. | Closed |
| R2-F3 (MAJOR) — alias column never consulted | New `$script:HeadingMapAliasMap` populated in check 6 from `SECTION-HEADING-MAP.md`'s `Canonical alias(es)` column; `Resolve-RequiredSections` now builds a candidate-phrase list per term (own text + aliases) and matches against any. `SECTION-HEADING-MAP.md` content confirms every row's alias column is populated (self-referential for current terms, per document contract 2). | Closed |
| R2-F4 (MINOR) — census from working tree | Check 4 now uses `git ls-tree -r --name-only $BaseCommit -- ...` instead of `git ls-files` (working tree) for both `RegressionSpecPaths` and `P2FixturePaths`. | Closed |
| R1-F1 (MAJOR) — carry-over item 3 undisposed | `REAL-SPEC-COMPATIBILITY-SET.md` gained item 3's disposition paragraph verbatim; check 10 asserts `$CompatSetText.Contains($CarryOverItem3Sentence)` against the exact pinned sentence. Confirmed byte-identical sentence in both the file and the check. | Closed |
| R1-F2 (MINOR) — README `../` links | `docs/specs/README.md`'s four P3-A section links changed from `../schemas/...`/`../templates/...` to `schemas/...`/`templates/...`, matching document contract 9 (P3-A.md line ~688, which pins the bare, non-`../`-prefixed forms) exactly; check 11's link-assertion array updated identically. Target files exist at the asserted relative paths. | Closed |

Accept-as-documented items also confirmed present: R2-F5's scope-disclosure sentence sits directly
above the confusables table (`verify-p3a.ps1`, ~line 387); R2-F6 required no code change per
triage.

## (2) Independent adversarial re-exploitation

I built a disposable, fully isolated `git clone` of the worktree into `/tmp` (no writes to the
subject repository or worktree) to run the real remediated `Resolve-RequiredSections`/check-7/
check-11 logic end-to-end without being blocked by the check-3 environmental mismatch described in
§4 below. (To get past check 3/12 purely as an environmental no-op, I extended, in the **disposable
clone only**, `AllowedSurfaces`/the placeholder-scan exclusion list with the same five
out-of-scope coordinator files the PR body identifies — a copy of the builder's own documented
diagnostic technique, applied independently by me, not a reuse of their artifact.) Baseline (no
mutation) in this sandbox: `P3-A verification PASS`, all 14 checks green.

**My own Exploit A variant (bogus-but-shape-valid href, not the published mutation):** swapped the
`Goal Charter` cell's href to point at the `Spec` cell's own target
(`[Governed-delivery charter](../INITIATIVES/biostack-governed-delivery/parcels/P3-A.md)`) —
syntactically valid, anchor text still contains "charter", but wrong file.
Result: **FAILS** — `INDEX.md P3-A row Goal Charter cell href must equal
'../INITIATIVES/biostack-governed-delivery/CHARTER.md' exactly, got
'../INITIATIVES/biostack-governed-delivery/parcels/P3-A.md'.` Confirmed by code inspection that
the pre-remediation shape-only regex (`^\[[^\]]*charter[^\]]*\]\(.+\)$`) would have accepted this
mutation.

**My own Exploit B variant (pinned sentence outside its subsection, not the published mutation):**
rather than moving one sentence to an adjacent subsection (the reviewer's/builder's published
case), I performed a **full swap** — relocated *both* pinned sentences (the
`delivery-class-extension` append-only sentence and the `domain-overlay-insertion` disjointness
sentence) verbatim into the unrelated `template-set-extension` subsection, leaving both origin
subsections without their own sentence while both sentences remain present exactly once each
somewhere in the file (so a whole-file `.Contains` on either sentence alone would still pass).
Result: **FAILS** — `EXTENSION-POINTS.md missing the pinned delivery-class-extension append-only
invariant sentence within its own subsection.` This is a stronger adversarial case than the
published one because it cannot be dismissed as "sentence simply deleted" — both sentences remain
fully present in the document, just relocated, which is exactly the shape of evasion check 7 is
supposed to catch.

Both of my own constructed exploits, independently designed and distinct from the previously
published ones, fail against the remediated logic as required.

## (3) `verify-p3a.ps1` run at the specified parameters

```
pwsh -NoProfile -File docs/specs/scripts/verify-p3a.ps1 \
  -BaseCommit 756c9d154d7a55a24fcc27f3b77ada4f04503028 \
  -BuilderId p3a_builder \
  -ReviewerIds p3a_impl_review_1,p3a_impl_review_2 \
  -EvidenceDirectory artifacts/p3a-verification
```

Run against the actual committed worktree (no modifications): **does not PASS.** Throws at check 3
(`Assert-True`, line 141): `BaseCommit...HEAD changed files count mismatch. Expected 28, got 33.`
The five extra files are exactly the ones the PR body names:
`docs/INITIATIVES/COORDINATOR-DISPATCH-QUEUE.md`, `TRIAGE-2026-10-08.md`,
`dispatch/GATE2-P3A-REMEDIATION-1.md`, `parcels/P0-A.md`, `parcels/P3-A.md`. None of these five is
touched by this remediation and none is in `AllowedSurfaces`.

I also tried the remediation's own stated `baseCommit` (`6e497d1`, per the dispatch record's JSON
block) instead of the value given in my task instructions: still fails check 3, this time
`Expected 28, got 5` (the five files are `TRIAGE-2026-10-08.md`, the dispatch record, `README.md`,
`REAL-SPEC-COMPATIBILITY-SET.md`, and `verify-p3a.ps1`). **There is no `BaseCommit` value for which
the committed, unmodified script currently produces a clean PASS on this branch**, because check
3's `AllowedSurfaces` constant (unchanged by this remediation, and correctly so — it is out of the
remediation's authorized envelope) is calibrated only for the original implementation's isolated
feature-branch diff, not for any re-verification scope that includes coordinator triage/dispatch
paperwork or intervening unrelated `main` commits.

I independently reconstructed (via my own isolated sandbox, §2) that once the five known,
pre-existing, out-of-scope files are accounted for, all 14 checks — including all six remediated
behaviors — do pass cleanly and deterministically. This corroborates the PR body's own disclosed
diagnostic run, without relying on the builder's attestation alone: I derived the same five-file
set and the same clean-PASS result independently, from first principles, by diffing the two
candidate BaseCommits myself.

## (4) Diff scope confirmation

`git diff --stat 13c5c3f 69acff8` (coordinator's triage/dispatch commit → the fixer's own commit):
exactly three files — `docs/specs/README.md`, `docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md`,
`docs/specs/scripts/verify-p3a.ps1`. `README.md`'s diff is exactly the four link strings (confirmed
by reading the unified diff: one line changed, only the four bracketed hrefs mutated, no other
text touched). No stray edits found in the compatibility-set file beyond the pinned carry-over
paragraph. No new defects found in the `verify-p3a.ps1` diff beyond the six targeted fixes (full
diff read end-to-end).

## Ranked findings

**F1 (MAJOR) — exit-proof claim not literally achievable by the shipped artifact.** The dispatch's
`verificationContract` ("verify-p3a.ps1 checks 1–14 pass at the remediated hash") and this
re-verification's own mandate ("run verify-p3a.ps1 clean ... confirm PASS") are not satisfiable by
the actual committed script under any BaseCommit choice consistent with the dispatch record,
because check 3's frozen `AllowedSurfaces` list was authored for an isolated-feature-branch
comparison and was correctly *not* touched by this remediation's authorized envelope (which
doesn't include it). This is fully and honestly disclosed in the PR body, with a reproducible,
disclosed workaround (a local, uncommitted diagnostic copy) rather than hidden or silently
patched around in the committed artifact — so it is not a case of the remediation claiming a false
PASS. But as shipped, the formal exit-proof instrument itself cannot produce the governed-process
"verifier transcript at the remediated hash" its own contract promises, which is a process-fidelity
gap the coordinator should close before relying on this PR as closing evidence.
**Smallest amendment:** either (a) the coordinator's next dispatch record formally acknowledges
check 3's single-anchor limitation as a known, accepted, cross-parcel verifier constraint (not
specific to this remediation) and defines which BaseCommit/HEAD pair is the authoritative one for
"clean PASS" going forward (e.g., always diff against the prior parcel's own feature-branch tip,
not against `main`'s history), or (b) a future, separately-dispatched change updates check 3 to
tolerate a named, pinned allow-list of out-of-scope coordinator paths (not an open-ended
tolerance). Does not change any locked decision; does not require touching any of the six fixes.
**Does it change a locked decision?** No.

**F2 (MINOR) — the dispatch's own worktree has untracked `artifacts/` only; no issue found,** noted
for completeness: `artifacts/p3a-verification/` was absent before my run and is correctly untracked
per check 13/14's own contract (confirmed: `git status` in the subject worktree shows only
`artifacts/` untracked, nothing modified).

No BLOCKER findings. No defects found in the six required fixes themselves, in the three-file diff
scope, or in my two independently-constructed adversarial exploit attempts (both correctly fail).

## Verification notes

- Read `REVIEWER-INSTRUCTIONS.md` and complied: read-only on the subject repo/worktree throughout;
  all adversarial mutation/testing was performed in a disposable `git clone` under `/tmp`, never
  committed to or pushed from the subject repository; no `git add/commit/checkout/merge/rebase/
  stash/reset` issued against `/home/cmorgan76/Repos/biostack` or
  `/home/cmorgan76/Repos/biostack-wt/p3a-remediation-1`; did not read other reviewers' output files
  in `.audit/reviews/`.
- Confirmed `git diff --stat 13c5c3f 69acff8` = exactly the claimed 3 files.
- Confirmed `docs/specs/README.md`'s diff = exactly the 4 link strings, matching P3-A.md document
  contract 9 (lines ~685–692) post-amendment-A1.
- Read the full `verify-p3a.ps1` diff for the six fixes end-to-end; each matches its triage
  disposition and dispatch requirement precisely.
- Ran `verify-p3a.ps1` against the actual worktree with the exact specified parameters: reproduces
  the PR-disclosed check-3 failure exactly (same five extra files, same counts).
- Independently (not from the PR's own diagnostic artifact) built a disposable clone, reproduced
  the five-file discrepancy, neutralized it the same way the PR describes, and got a genuine, fresh
  `P3-A verification PASS` with `pass: true` on all 14 checks in `verification-summary.json`.
- Constructed and ran two of my own adversarial mutations (not the published ones) against the
  real remediated check-7/check-11 logic in the disposable clone; both correctly FAIL.
- Confirmed via code reading that the pre-remediation logic (shape-only regex; whole-file
  `.Contains`) would have passed both of my own mutations, corroborating that these are genuine
  closed regressions, not failures that would occur regardless of the fix.
- Cleaned up: `/tmp/p3a-reverify-clone` and any temp diagnostic files removed after testing.
