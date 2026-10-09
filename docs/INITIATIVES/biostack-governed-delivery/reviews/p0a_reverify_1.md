# p0a_reverify_1 — P0-A remediation re-verification

**Verdict:** PASS-WITH-FIXES
**Subject:** P0-A remediation, worktree `/home/cmorgan76/Repos/biostack-wt/p0a-remediation-1`
(branch `fix/p0a-remediation-1`, tip `f1d00c8653b9f68933fcdee5b97d1933338a68fa`)
**Authority:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md`; Coordinator Decision
D-K (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`, `## D-K` heading)
**Prior reviews being closed:** `.audit/reviews/p0a_impl_review_1.md`,
`.audit/reviews/p0a_impl_review_2.md` (both PASS-WITH-FIXES, retrospective implementation
reviews of PR #528)
**Date:** 2026-10-09

Artifact SHA-256 at reviewed tip (`f1d00c8`):
- `docs/specs/schemas/canon-precedence.md`:
  `73d199e04b8af8e7000f360bd0e4e0bd55e69042f7ed73dfa9a1848aa160309f`
- `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md`:
  `00482564dbbb54e3e14925430cbb48010c465de7a4220a6c079c570311ab5cff`
- `docs/specs/scripts/verify-p0a.ps1`:
  `8aa485515b297a3ddfa1daba0e35305a83129a4588885a916b770c16c63ef5f8`

## Scope and method

Read-only. Read the remediation commit (`git show f1d00c8`), the full 745-line
`verify-p0a.ps1`, both prior implementation reviews, D-K's canon text, and the three changed
files in full. Ran `verify-p0a.ps1` live in the named worktree (no repository state mutated;
the untracked `artifacts/p0a-verification/` scratch directory it produces was removed afterward,
restoring the worktree to a clean `git status`). Performed two disposable-clone adversarial tests
in `/tmp` (never touching `biostack` or the named worktree): (1) a fabricated non-seed quotation
in CI-011, to confirm F2's new check is non-vacuous; (2) an uncommitted edit to `CHARTER.md`, to
confirm F4's new dirty-tree detection is non-vacuous. Both disposable clones were deleted after
use.

## 1. D-K directional-application constraint — verbatim transcription and row-level notices

**Verbatim transcription confirmed.** `COORDINATOR-DECISIONS-2026-10-07.md` lines 206-212 (the
`## D-K` blockquote) and `canon-precedence.md`'s new "## 4a. Directional-application constraint"
section (added by this commit) were extracted and diffed byte-for-byte
(`diff /tmp/dk_source.txt /tmp/dk_manifest.txt`): **identical, zero diff output.** All seven
blockquote lines, including the embedded bold markers (`**document authority order only**`,
`**stands unchanged**`) and the full closing sentence citing the charter's must-not list, D12,
and D-I's staged-split posture, are reproduced exactly.

**CI-002/CI-005 notices confirmed.** Both rows' `proposed_disposition` fields carry a
**`D-K constraint notice:`** sentence (`CONTRADICTION-INVENTORY.md` lines 52, 100) stating the
manifest's rank order for the specific document pair in question "must not be read as resolving
this row," citing D-K by name, and restating the "stands unchanged... never weaken it by rank
alone... routes to the owner through P0-D's disposition process" substance. This directly closes
`p0a_impl_review_2`'s F1 (the BLOCKER-candidate finding that triggered D-K in the first place):
the manifest is no longer a free-standing total order that a reader could apply, unwarned, to
conclude a permissive document supersedes a narrower safety prohibition.

**Finding:** clean. This is the most consequential of the four findings and is fully closed.

## 2. Fix-row closure audit (`p0a_impl_review_1` + `p0a_impl_review_2`)

### F2 (MAJOR) — quoted-content verification now real, confirmed non-vacuous by adversarial test

Read the new `quotation-content-verified` check (`verify-p0a.ps1` lines ~504-620): it parses every
`CI-NNN` row's `source_a`/`source_b` field, resolves each quoted span to its most-recently-preceding
cited path token (handling multi-citation fields and the two bare-filename aliases `CHARTER.md`/
`README.md`), fetches the cited file's real content at `BaseCommit` via `git show`, and asserts
each quoted segment (ellipsis-split, normalized only for markdown bullet/bold/whitespace
decoration) is a substring of the real file content. This is a genuine content check, not a path
probe — the pre-existing `unresolvable-citation` check (which F2 correctly diagnosed as
path-only) is left intact and unchanged; `quotation-content-verified` is additive.

**Adversarial confirmation (disposable clone, `/tmp/p0a_adv2`, deleted after use):** replicated
`p0a_impl_review_2`'s exact F2 reproduction — altered CI-011's `source_b` quotation from the real
`docs/specs/INDEX.md` text `"TBD (coordinator to assign Gate 2)"` to a fabricated `"TBD
(coordinator to assign Gate 99, completely fabricated text)"`, committed, and re-ran
`verify-p0a.ps1`. Result: the first 18 checks passed, then the script **threw** at
`quotation-content-verified` with the exact diagnostic `CI-011.source_b: quoted segment not found
... 'TBD (coordinator to assign Gate 99, completely fabricated text)'` and a nonzero exit. The
previously-uncaught fabrication is now caught. **F2 is closed and the fix is confirmed real, not
vacuous.**

### F3 (MINOR) — Class D quote restored byte-exact

CI-001's `source_b` field now reads `*"**Status:** **Prohibited** unless product and regulatory
posture deliberately change via a new contract version."*` immediately before the previously-elided
bullet quotations. Independently `grep`'d against
`docs/guidance/biostack-guidance-content-contract.v1.md` line 110: `**Status:** **Prohibited**
unless product and regulatory posture deliberately change via a new contract version.` —
character-exact match, including bold markdown decoration reproduced as source typography (per
this inventory's own citation-apparatus rule). **F3 is closed.**

### F4 (MINOR) — diff-scope fix: functionally correct, but introduces a reproducible script crash on a clean/synced tip (new finding, see §3)

`canonical-write-fencing-violation` now unions three sources: committed history since a
merge-base-pinned `fencingBaseCommit`, untracked files, and `git diff --name-only HEAD` (staged +
unstaged tracked changes). Confirmed via adversarial test: an **uncommitted** edit to `CHARTER.md`
(the exact gap `p0a_impl_review_2`'s F4 named) is now caught — the check throws
`canonical-write-fencing-violation: ... CHARTER.md is outside the allowed-surfaces list.` This
closes the specific gap F4 named. Separately confirmed the merge-base pin is *necessary*, not
decorative: running the **pre-remediation** script (`c0d8642`) against the current repository
state produces a **false-positive** fencing violation (`docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md
is outside the allowed-surfaces list`) because other, unrelated initiatives landed commits between
`BaseCommit` and the review point — exactly the false-attribution risk the merge-base pin is
designed to avoid. However, this same pin is also the root cause of a new defect; see §3.

### R1-MINOR-1 (field placement) — closed

`CI-005`'s `priority: P0-D1-first` flag is now in the `handoff_target` field (`CONTRADICTION-
INVENTORY.md` line 101: `` `P0-D3` — **`priority: P0-D1-first`** — ... ``), matching `P0-A.md`'s
literal "in its `handoff_target` note" requirement. `proposed_disposition` no longer carries the
flag. `verify-p0a.ps1`'s `missing-method-state-coverage` check (`$fields['handoff_target'] -match
'P0-D1-first'`) now reads the correct field. **Closed.**

## 3. `verify-p0a.ps1` run result: 20 content checks PASS, but the script does not exit clean at the delivered tip — NEW finding

**Claim.** The remediation commit message states "Re-ran verify-p0a.ps1 ... 20/20 checks PASS."
This is true of the 20 named content checks' *internal logic*, but **not of the script run as a
whole, as delivered, at a clean checkout of the branch's own tip.**

**Reproduction.** In the named worktree, at `f1d00c8` with a clean working tree (`git status`
clean except the script's own regenerable `artifacts/` output) and the branch fully synced with
its upstream (`git merge-base HEAD @{u}` == `HEAD`):

```
pwsh -NoProfile -File docs/specs/scripts/verify-p0a.ps1 -BaseCommit b78e7de... \
  -BuilderId reverify_test -ReviewerIds p0a_reverify_1 -EvidenceDirectory artifacts/p0a-verification
```

All 20 `PASS:` lines print, in order, including `quotation-content-verified` and
`dk-directional-constraint-present`. The script then **throws** `Cannot bind argument to
parameter 'Content' because it is an empty string.` and **exits 1**, before printing the final
`P0-A verification PASS (20 checks)` summary line and before writing the evidence bundle.
Reproduced twice, deterministically, from a fully clean `artifacts/` state (`rm -rf artifacts &&
<rerun>` gives the identical failure both times).

**Root cause.** F4's new `fencingBaseCommit` (`git merge-base HEAD @{u}`) correctly resolves to
`HEAD` itself when the branch has no local-only commits beyond what is pushed (the normal state
of a delivered, reviewable branch tip — exactly this worktree's actual state). `git diff
--name-only $fencingBaseCommit...HEAD` is then empty; the only untracked files are inside the
excluded `artifacts/p0a-verification/*` pattern; and there is no working-tree drift. `$allChanged`
is therefore a genuinely empty array, and `$allChanged -join "`n"` is an empty string.
`Write-Utf8Lf`'s `$Content` parameter is `[Parameter(Mandatory = $true)][string]` with no
`[AllowEmptyString()]` attribute — PowerShell's binder rejects an empty string for a mandatory
string parameter lacking that attribute, regardless of the `$true`/`$false` of "mandatory" in the
ordinary null-check sense. This is a genuine, reproducible defect in the script as delivered, not
an environment artifact: I confirmed the *pre-remediation* script (run against the same repository
state) instead produces a **different**, also-incorrect result — a false-positive fencing
violation naming an unrelated sibling parcel's file — so this is not simply "the old script was
fine and the new one broke it" in one direction; both the pre- and post-remediation scripts fail
to run cleanly at this exact tip, for different reasons, and F4's fix trades the old false-positive
failure mode for this new crash-on-empty-diff failure mode. Confirmed this is specific to the
"no further local drift beyond the pushed tip" case, not F4's core detection logic: a dirty-tree
adversarial test (uncommitted edit to `CHARTER.md`) correctly triggers the fencing check itself,
*before* reaching the evidence-write step, and does not crash — the crash only manifests when
`$allChanged` is genuinely, legitimately empty, which is precisely the state of a clean,
fully-delivered branch tip with nothing left to flag.

**Why this is not a BLOCKER.** All 20 checks' own verification logic runs to completion and
reports correct, genuine PASS results in every case tested (clean content, fabricated-quote
adversary, dirty-tree adversary) — the substantive verification machinery this task asked me to
re-check (D-K transcription check, quotation-content-verified, the fencing check's detection
logic itself) is sound. The defect is confined to the evidence-bundle-writing tail of the script
and is a one-line PowerShell binder quirk, not a logic or content-verification defect.

**Why this is not ignorable.** It directly contradicts this task's criterion (3) ("verify-p0a.ps1
runs clean 20/20 at the remediated content") in the literal sense a coordinator or the next
reviewer would experience: running the script exactly as documented, at exactly the delivered,
pushed tip, with a clean working tree, **does not exit 0 and does not produce the evidence
bundle** the parcel's acceptance criteria require as closure evidence. It would also block any
CI/gate automation that checks the script's exit code rather than grepping its stdout for `PASS`
lines.

**Smallest amendment:** add `[AllowEmptyString()]` to `Write-Utf8Lf`'s `$Content` parameter (both
call sites write content that may legitimately be empty — an empty `changed-files.txt` is the
correct, truthful record when nothing changed relative to the pinned fencing base). No other
change needed; this does not touch any of the 20 checks' pass/fail logic, any rank, any
`CI-NNN` disposition, or D-K's text.

**Does it change a locked decision?** No.

**Severity:** MAJOR (script-completion defect in the delivered verifier, reproducible at the
delivery tip as documented; does not affect the correctness of any individual check's verdict).

## 4. Product allowed-output decisions — none introduced

Scanned the full diff (`git diff 765e11e f1d00c8`, 3 files) for any unquoted "BioStack
must/may/does not/is/should" assertion in the remediation's own voice. Every new sentence either
(a) restates D-K's already-ruled constraint, attributed by name and citation, or (b) is analytical
commentary on what the manifest's rank order does/does not resolve, structurally identical in
form to the pre-existing CI-row prose `p0a_impl_review_1`/`_2` already cleared. No new rank
assignment, no new `status`/`proposed_disposition` value, no new allowed/degraded/refused/
escalated behavior statement. `verify-p0a.ps1`'s own `no-unattributed-claim` heuristic check
(unchanged by this commit) also passed live against the new content. **Clean.**

## 5. Allowed-surfaces / frozen-surface integrity

`git diff --name-only 765e11e f1d00c8` touches exactly 3 files: `CONTRADICTION-INVENTORY.md`,
`canon-precedence.md`, `verify-p0a.ps1` — all three are on P0-A's original "Exact allowed
surfaces" list (confirmed against `p0a_impl_review_1`'s own independently-verified 9-file
allowed-surfaces set, of which these three are a subset). `git diff b78e7de f1d00c8 --stat --
CHARTER.md docs/canon docs/guidance README.md knowledge-engine-capability-map.md` returns empty —
zero touches to any canon/guidance/frozen document across the full remediation lineage
(`b78e7de..f1d00c8`, including the two D-J/D-K coordinator-decision commits and the original
P0-A implementation). **Clean.**

## Ranked findings

### REVERIFY-F1 — MAJOR: `verify-p0a.ps1` throws and exits non-zero when run at a clean, fully-synced branch tip, despite all 20 named checks passing

See §3 above for full detail, reproduction, root cause, and smallest amendment
(`[AllowEmptyString()]` on `Write-Utf8Lf`'s `$Content` parameter). Does not change a locked
decision. Blocks a literal re-run of the script's documented invocation from completing with
exit 0 and from producing the evidence bundle at exactly the state this remediation delivers.

## Missing pieces / collisions / unknowns

- None found beyond REVERIFY-F1. All four prior fix rows (F2, F3, F4's detection-logic gap,
  R1-MINOR-1) are substantively closed; D-K's transcription and row-notice requirements are both
  met exactly as specified.
- Did not independently re-verify every one of the 11 `CI-NNN` rows' quotations against real
  source files beyond what `quotation-content-verified` itself does live (the check is now
  genuinely content-derived, confirmed non-vacuous by direct adversarial reproduction, so I relied
  on its live PASS for full-row coverage rather than re-doing all ~30+ quotations by hand).
- Did not read any other reviewer's output file, consistent with the independence rule.

## Verification notes

- Ran `verify-p0a.ps1` live, read-only, in `/home/cmorgan76/Repos/biostack-wt/p0a-remediation-1`
  at tip `f1d00c8`, twice from a clean `artifacts/` state: both runs printed all 20 `PASS:` lines
  identically, then threw the identical `Write-Utf8Lf` empty-string exception and exited 1 (see
  §3). Removed the resulting untracked `artifacts/` directory afterward; `git status` in the
  worktree is clean.
- Diffed D-K's canon blockquote (`COORDINATOR-DECISIONS-2026-10-07.md` lines 206-212) against
  `canon-precedence.md`'s section 4a transcription: byte-identical, zero diff.
- Confirmed F3's restored Class D quotation character-exact against
  `docs/guidance/biostack-guidance-content-contract.v1.md` line 110.
- Adversarial test 1 (disposable clone `/tmp/p0a_adv2`, deleted after use): fabricated CI-011's
  non-seed quotation; `quotation-content-verified` correctly threw and identified the exact
  fabricated string; prior 18-check behavior (`p0a_impl_review_2`'s original F2 reproduction)
  would have passed this same mutation cleanly.
- Adversarial test 2 (same disposable clone): uncommitted edit to `CHARTER.md`;
  `canonical-write-fencing-violation` correctly threw before reaching any later check.
- Confirmed the pre-remediation script (`c0d8642`) run against current repository state produces
  a different, also-nonzero-exit false positive (unrelated sibling-parcel file), establishing that
  F4's merge-base pin was a necessary fix for a real problem, independent of the new defect it
  introduces.
- Confirmed exactly 3 files changed across the full remediation lineage, all on P0-A's allowed
  surfaces; zero frozen-surface (CHARTER/canon/guidance/README/capability-map) touches across
  `b78e7de..f1d00c8`.
- No git write command issued against `biostack` or the named worktree; the only artifact this
  review wrote is this output file.
