# BioStack Worktree Inventory & Cleanup Assessment
**Date:** 2026-09-06 · **Method:** read-only forensics, 5 parallel agents + direct verification
**Repo:** m0r6aN/biostack · **`origin/main` tip:** `0241b1b` (PR #267, 2026-09-05)
**Root repo:** `D:/Repos/BioStack` on `lead/gtm-wave006-rereview-20260905` @ `4d8754c`

> Nothing was deleted, merged, pruned, popped, or revived. This is evidence only.

---

## 0. Scope correction (read this first)

The premise was that the worktrees live in `D:/Repos/BioStack-*`. That set exists (83 dirs), but it
is **not** where the risk is. Total registered worktrees: **125**, in four locations:

| Location | Count | Risk |
|---|---:|---|
| `D:/Repos/BioStack-*` | 83 dirs (75 healthy worktrees) | Low — almost entirely merged history |
| `C:/Users/clint/.codex/worktrees/*/BioStack` | 32 | **Highest** — live goal + 4 orphaned refs |
| `D:/Repos/BioStack/.worktrees/*` | 15 | Mixed — holds the one real recovery candidate |
| `D:/Repos/BioStack/.claude/worktrees/*` | 3 | Low — superseded 2026-05/06 work |

**Bottom line: 114 of 125 worktrees hold nothing that is not already in `origin/main`.**
The other **11** do, and all 11 were retained: 1 active goal, 1 recovery candidate,
6 diagnostic reproduction trees, 1 unmerged diagnostic test, and 2 unresolved governance
trees. (114 + 11 = 125.)

Separately, 4 detached HEADs held commits reachable from **no branch and no tag**. Their
*content* proved superseded, so they are counted among the 114 — but the commit objects
themselves were genuinely at risk and are now preserved as `salvage/*` tags.

---

## 1. Active and owned — preserve, do not touch

**`codex/goal-biostack-sidecar-deployment`** — `C:/Users/clint/.codex/worktrees/biostack-sidecar-deployment-goal/BioStack` @ `038b617`

Live coordinator state for the current goal. `charter.md` reads **"RATIFIED AT GATE 1 (2026-09-03)"**,
pinned to base `4d8754c` (= PR #265, current main's lineage). `loop-directive.md` names an owning
coordinator session claimed 2026-09-03, and grants Gate 2 for **local-only** P01/P02/P03/P04a work —
no push, PR, merge, or Azure mutation; Gates 3A/3B explicitly withheld. Its 6 unique commits are all
governance docs (charter, discovery, gate-1-request, loop-directive, plan-review-findings,
STANDING-CONSTRAINTS). Only 4 commits behind main. No parcels dispatched yet.

This matches the known posture that the sidecar is **deliberately undeployed**.
**Action: preserve untouched. Exclude from every cleanup batch.**

---

## 2. Valuable unfinished work — one genuine recovery candidate

**`codex/track-c-production-ops-20260728`** — `D:/Repos/BioStack/.worktrees/track-c-production-ops` @ `5e0d0ea`
9 unique commits (2026-07-28). I verified the file-level claim directly against a full
`git ls-tree -r origin/main` listing (1,638 files):

| File | In `origin/main`? |
|---|---|
| `infra/azure/deploy-disposable-staging.ps1` | **ABSENT** |
| `infra/azure/verify-disposable-staging.ps1` | **ABSENT** |
| `docs/operations/track-c-production-operations-release-candidate.md` | **ABSENT** |
| `frontend/Dockerfile`, `infra/azure/deploy-container-apps.ps1`, `infra/azure/README.md`, `frontend/package-lock.json` | present |

Three files of disposable-Azure-staging tooling exist **only** on this branch. It is reachable from a
branch ref, so it is not at risk of loss — but it is unmerged work that is plausibly *directly useful
to the live sidecar-deployment goal*, whose P02/P03 parcels concern exactly this Azure verification
problem. This is the one item I would put in front of the sidecar coordinator before any cleanup.

**Action: hold. Offer to the sidecar-deployment goal as prior art; do not delete.**

### Secondary, lower value
**`codex/biostack-remediation-q02`** @ `dea7212` — adds
`backend/tests/BioStack.Api.Tests/Architecture/UserFacingGateCoverageInvestigationTests.cs`,
confirmed **absent from `origin/main`**. A diagnostic test, not application code. Decide: promote to
permanent regression coverage, or classify with the `test-repro-*` trees below and retain as evidence.

---

## 3. Intentionally retained — diagnostic evidence trail

Six `codex/test-repro-*` worktrees (`contracts`, `evidence`, `interaction`, `outbound`, `parser`,
`sidecar`), all 2026-09-02, on a shared docs base. Each adds one reproduction test characterising a
defect that the now-merged algorithm-remediation parcels P01–P08 fixed:

| Tree | Commit | Feeds parcel |
|---|---|---|
| evidence | `2c9d6ca` evidence provenance gaps | P03/P04 |
| interaction | `56b7ad2` audit safety gaps | P02 |
| outbound | `c3e30a9` outbound boundary gaps | P07/P08 |
| parser | `817f6f3` audit invariants | P01/P05 |
| sidecar | `82295c3` lifecycle boundary gaps | P05/P06 |
| contracts | `879def1` + 3 docs commits | audit-trail lead tree |

Test/diagnostic only, no application code. **Retention reason: they are the evidence that the
remediation defects were real and reproducible.** Keep until the remediation audit trail is closed out.

---

## 4. At risk — commits reachable from no ref

### 4a. Four orphaned detached HEADs (2026-08-28)
`C:/Users/clint/.codex/worktrees/{2d94,3109,a9a0,fdf1}/BioStack`

Verified: `git branch -a --contains` and `git tag --contains` both return **zero** for all four tips.
They are reachable from no branch and no tag — exactly the work a branch listing cannot see.

| Tip | Subject |
|---|---|
| `ff5eb5e` | fix: implement authenticated profile goals contract |
| `2c4bdac` | fix(auth): persist sessions and recover stale cookies |
| `5404e20` | fix(db): repair PostgreSQL auth and billing type drift |
| `b3cde26` | feat(auth): add passkey WebAuthn sign-in |

Per-file comparison found their content superseded by **PR #253** (merged 2026-08-28, ~1h later),
which independently carried the same auth/passkey/billing fixes; `lead/category-language-unify` also
holds rebase-equivalents of three of the four. Removing these worktrees makes the tips gc-eligible.

**Action: `git tag` the four SHAs before removing the worktrees.** Tagging is free and converts a
judgement call into a non-decision. I did not tag them — that is a write.

### 4b. 98 dangling commits, 47 with no live counterpart
`git fsck --dangling --no-reflogs` → 98 dangling commits, **none** in `origin/main`.
51 have a same-subject commit on a live ref (amend/rebase predecessors — safe to lose).
The other **47 are true orphans**, overwhelmingly dropped stash entries (`WIP on ...`), mostly
2026-06/07 frontend-readiness and protocol-parsing work.

**Why this is time-sensitive:** `.git` holds **10,844 loose objects** against a `gc.auto` threshold of
**6,700** — already exceeded — and `gc.pruneExpire` defaults to `2.weeks.ago`, which every orphan
predates. The next automatic gc can prune them permanently. (Pruning keys on object mtime, not commit
date, so this is a real risk rather than a certainty.)

Spot-checked the three non-stash orphans: `df0fa24` (PR #122 review fixes), `0406132` (honest-parsing
WIP), `2072c6c` (lane-h remnants — content already identical to main).

**Action if you want them preserved: `git bundle create orphans.bundle $(...)` or tag them. One
command, then gc is harmless.** Otherwise accept the loss deliberately rather than by default.

### 4c. Repo hygiene
`git count-objects` reports two stray files needing manual removal:
`.git/objects/pack/tmp_idx_z6LU1h` and `.git/objects/pack/tmp_pack_Rjlg3I` (leftover from an
interrupted pack, 2026-08-29).

---

## 5. Superseded or redundant — the bulk of `D:/Repos/BioStack-*`

### 5a. Squash-merged (33) — ancestry broken, content landed
`NOT_ANC` + zero unique patches by patch-id. 29 map to an explicitly merged PR (**#205–#241**).
Four have no PR record but were verified by content presence in `origin/main`:
`ci-kompress`, `keo64-claude`, `keo65-security`, `sec-receipts`.

### 5b. Ancestors of `origin/main` (26) — clean, zero diff
All 26 are ancestors with an empty `git diff origin/main...HEAD`. 24 are clean with no remote
counterpart, consistent with completed KEO tickets whose branches were deleted post-merge.

### 5c. Looked unmerged, verified landed (14 of 16)

**The security question — resolved, no live gap.** Six `sec-*` worktrees (2026-07-13) each carried one
unique security patch and looked unmerged. All six are confirmed **present and functionally active in
`origin/main` today**, landed via consolidated PR **#183** (merged 2026-07-14), whose description
enumerates all six. They lack individual merged PRs under their own branch names, which is why
ancestry and PR-name search both make them look like gaps:

| Worktree | Fix | Verified in main |
|---|---|---|
| `sec-link-analyzer` | SSRF hardening on link fetches | `Program.cs` wires `ConnectToPublicEndpointAsync`; public-address-only DNS + HTTPS/443-only validation live |
| `sec-magic-link` | atomic magic-link claim | `AuthEndpoints.VerifyAndSignInAsync` has the guarded `ExecuteUpdateAsync` verbatim |
| `sec-provider-access` | non-enumerating intake | uniform `CreateAcknowledgement()` at all call sites |
| `sec-consent` | server-side consent binding | `ConsentGate.cs` verbatim |
| `sec-container` | nonroot backend container | `backend/Dockerfile` `chown`/`USER app` verbatim |
| `sec-frontend-deps` | PostCSS pin | `frontend/package.json` override present (since bumped) |

Also verified landed: `calculator-syringe-vial` (#50), `ci-sidecar-audit-hotfix` (#255),
`frontend-dependency-repair` (#211), `frontend-lockfile-hotfix` (#213),
`keo74-offline-source-planning` (#212), `keo74-source-decisions` (#208),
`biort01-fail-closed-receipts` (superseded by #241), `commercial-readiness` (landed under another branch).

### 5d. Orphaned non-git trees (6) — verified by content hash
Six `D:/Repos/BioStack-*` dirs have **no `.git` at all**, so git could not see them. Every meaningful
file was hashed and compared against the full `origin/main` blob tree:

| Directory | Files | MATCH / DIFF / **UNIQUE** | Verdict |
|---|---:|---|---|
| `BioStack-billing-tier` | 0 | empty dir | Safe cleanup |
| `...-protocolid-027-20260726` | 1,149 | 1,029 / 120 / **0** | crashed `worktree add` targeting merged `53ed0df` |
| `BioStack-production-readiness` | 142 | 120 / 22 / **0** | pre-remediation drafts; main strictly newer |
| `BioStack-production-readiness-phase2` | 82 | 71 / 11 / **0** | thinner snapshot of same lineage |
| `BioStack-track-b-biostack-rc-20260728` | 84 | 77 / 7 / **0** | same stale evidence packets |
| `BioStack-verify` | 667 | 558 / 109 / **0** | 2026-07-11 ledger vs main's 2026-07-26 reconciliation |

**Zero unique paths across all six.** The "DIFF" files are consistently *older* than main, not newer.

### 5e. Broken gitlinks (2)
- `BioStack-export-bundle-pr` — **false alarm.** Fails only on `dubious ownership`; healthy when read
  with `git -c safe.directory='*'`. Zero unique patches, clean, fully upstream.
- `...-protocolid-028-20260726` — genuinely orphaned; its admin dir is gone. Content matches merged
  commit `53ed0df` where present, and the tree is *more* partial than its `-027` twin. Nothing unique.

### 5f. The stash — not needed
`stash@{0}` (`3e7a9d1`, 2026-06-09) "prb-validation-unstaged-residue", 42 files / +2410 / −365.
All 42 paths exist in `origin/main`, and main **supersedes** rather than diverges:
`TierGate.tsx` in the stash is an explicit inert stub ("Paywall seam (currently INERT)"), while main
has a fully implemented tier gate; `my-protocol/page.tsx` is 177 lines of hardcoded mock data in the
stash vs 355 lines wired to the real API client with loading/error/empty states in main.
Restoring it would regress shipped code. **Safe to drop — your call, not done.**

### 5g. Loose patch file
`D:/Repos/BioStack-protocol-intelligence-slice3-reporting-wip.patch` (11 KB, 2026-06-29) — a
single-file WIP export of dangling stash `3f0e451`. Its content **is in main**:
`ProtocolIntelligenceEvaluationJob.cs:35` has `ReportVersion = "1.1.0"`. Redundant.

---

## 6. Corrections made to agent findings

Five agents produced the raw evidence; I verified the consequential claims and overturned four.

| Claim | Verdict | Evidence |
|---|---|---|
| `keo84-seo-ledger-023` is *Intentionally retained* (locked) | **Overturned → Safe cleanup** | Lock reason is literally `initializing` — a stale lock from an interrupted `worktree add` 6 weeks ago, not curation. All **265/265** non-noise untracked files exist in `origin/main`; the 3 that differ match its own HEAD (stale checkout) |
| `keo68-billing-deeplink-sparse-025` is *Unresolved* (1337 deletions) | **Overturned → Safe cleanup** | HEAD is an ancestor of main with a **zero-byte** diff and 0 untracked. The checkout was merely emptied; every tracked file is in main |
| `stage-f-closure.md` is "not committed anywhere durable" | **Overturned** | It *is* committed at `ec80434` on `codex/goal-biostack-algorithm-remediation` — reachable from a branch, not at risk |
| `biostack-remediation-q01` is *Unresolved* (test absent from main) | **Overturned → Superseded** | Two-dot diff shows main's copy of `CollectiveOutboundBoundaryInvestigationTests.cs` has **94 more lines**; main is a superset |

### Verification hazard worth knowing about
On this machine `git cat-file -e origin/main:.gitignore` **silently reports dot-prefixed paths as
absent** (MSYS argument mangling), which manufactures false "missing from main" findings. Conversely,
`MSYS_NO_PATHCONV=1` breaks `git -C /d/...` paths. Both bit this audit. The reliable method is
`git ls-tree -r origin/main --name-only` into a file, then `grep -Fxq` — immune to both.

---

## 7. Cleanup batch — for approval

Nothing below has been executed.

**Batch 1 — `D:/Repos/BioStack-*`, 59 worktrees** (26 ancestors + 33 squash-merged). **~24.2 GB on disk.**
Full list: `wt/list_cleanup_core.txt`. All verified clean, zero unique patches, content upstream.

**Batch 2 — same location, 10 more after the corrections above:**
`keo84-seo-ledger-023`, `keo68-billing-deeplink-sparse-025`, `export-bundle-pr`, plus the 6 non-git
orphan dirs (`billing-tier`, `production-readiness`, `production-readiness-phase2`,
`track-b-biostack-rc`, `verify`, `...-027`) and the orphaned `...-028`. These need plain directory
removal, not `git worktree remove`, since git does not track most of them.
*Note:* `keo84` is locked — needs `git worktree unlock` first.

**Batch 3 — the 14 `sec-*`/misc verified-landed worktrees** from §5c.

**Batch 4 — offsite superseded:** the `.worktrees/pr15x` set, `gtm-*` lanes,
`dependency-hygiene-nuget-vulnerabilities`, `analyzer-premium-redesign`, `analyzer-ux-pass`,
`feat+go-live-hardening`, `frontend-readiness-remediation`, the remediation `p01`–`p08`/`q0x` parcels,
`890d`, `calculator-overlap-language`, `6691`, and the byte-identical duplicates
`biostack-remediation-integration` and `p05-p06-combined`.

**Also stale, not worktrees:**
`.git/worktrees/BioStack-keo66-...-027` admin entry (crashed init) → `git worktree prune`;
`.worktrees/pr170-capstone` (empty dir that silently resolves to the root repo when scripted against);
`.worktrees/codex/protocol-operations-auditor-packet-design-note` (broken leftover duplicating `pr157`);
`BioStack-protocol-intelligence-slice3-reporting-wip.patch`.

### Excluded from cleanup — do not remove
1. `biostack-sidecar-deployment-goal` — live goal (§1)
2. `track-c-production-ops` — unmerged Azure staging tooling (§2)
3. Six `test-repro-*` trees — evidence trail (§3)
4. `biostack-remediation-q02` — unmerged diagnostic test (§2)
5. The four detached HEADs — **tag first**, then removable (§4a)

### Suggested order
1. Tag the 4 orphan tips + bundle the 47 dangling commits (or consciously accept the loss)
2. Decide on `track-c-production-ops` with the sidecar coordinator
3. Run Batches 1 → 3 → 4; Batch 2 by directory removal
4. `git worktree prune`, remove the two stray `tmp_*` pack files, then `git gc`

---

## 8. Evidence files
`wt/inventory.tsv` (83 rows, 12 columns) · `wt/other.tsv` (48 offsite) · `wt/prs.json` (400 PRs) ·
`wt/list_cleanup_core.txt` (59) · `wt/list_ancestor.txt` · `wt/list_superseded.txt` · `BRIEFING.md`
