# Worktree Inventory & Cleanup — 2026-09-06

Audit receipt for a full inventory and cleanup of BioStack git worktrees.
Findings and rationale: **[INVENTORY-REPORT.md](INVENTORY-REPORT.md)**.

## Outcome

| Metric | Before | After |
|---|---:|---:|
| Registered worktrees | 125 | 12 |
| `D:/Repos/BioStack-*` dirs | 83 | 2 |
| Local branches | 221 | **221** |
| Loose git objects | 10,844 | 0 |

**No commit history was lost.** Worktree removal deletes only checkouts; every branch survived.
Disk reclaimed: ~24 GB of redundant working trees and build output.

The end state above is independently reproducible in `post-cleanup-verification.txt`,
generated from live git state rather than asserted. Note the batch logs record only the
scripted loops: 8 worktrees refused removal inside those loops and were resolved by separate
commands afterwards, so `batch1-log.txt` and `batch4-log.txt` carry `FAIL` entries that look
unresolved on their own. Each one is accounted for, with the evidence checked before forcing,
in **`followup-removals-log.txt`**.

## Preserved material

- **Four orphaned commits** (reachable from no branch or tag) are now permanent tags:
  `salvage/orphan-profile-goals-contract`, `salvage/orphan-auth-session-recovery`,
  `salvage/orphan-db-auth-billing-repair`, `salvage/orphan-passkey-webauthn`.
- **98 dangling commits** archived to a verified git bundle before `gc` pruned them.
  Stored **outside the repo** (18 MB binary, deliberately not committed) at:
  `D:/Repos/_biostack-salvage-2026-09-06/dangling-commits.bundle`
  Restore with: `git fetch D:/Repos/_biostack-salvage-2026-09-06/dangling-commits.bundle 'refs/tags/*:refs/tags/recovered/*'`
  Contents listed in `dangling-manifest.txt`.
- **Uncommitted changes** from five worktrees captured in `uncommitted-patches/` before those
  worktrees were force-removed (includes a 27-file staged tree from the remediation
  integration worktree). Reapply tracked-file changes with `git apply <name>.patch`.
  **Untracked files are not in the patches** — a file git never tracked produces no diff, so
  `analyzer-premium-redesign.patch` is empty by design. Those files were copied verbatim
  instead and are restored by copying, not by `git apply`:
  `uncommitted-patches/analyzer-premium-redesign-untracked/frontend/pnpm-lock.yaml`.
  Each `<name>.status` file lists what was present, so `??` entries can be matched to the
  `-untracked/` copies.

## Retained worktrees (deliberately not removed)

| Worktree | Reason |
|---|---|
| `codex/goal-biostack-sidecar-deployment` | Live goal, Gate 1 ratified 2026-09-03 |
| `codex/track-c-production-ops-20260728` | Unmerged Azure disposable-staging tooling (3 files absent from main) |
| 6 × `codex/test-repro-*` | Diagnostic reproduction evidence for the remediation parcels |
| `codex/biostack-remediation-q02` | Unmerged diagnostic test absent from main |
| `governance-p1`, `governed-delivery-charter` | Unresolved — needs a human decision |

## Files

`INVENTORY-REPORT.md` full findings · `inventory-dRepos.tsv` / `inventory-offsite.tsv` raw per-worktree
evidence · `batch{1,2,3,4}-log.txt` removal logs · `*-before.txt` pre-cleanup snapshots ·
`dangling-manifest.txt` archived commits · `hold-list.txt` retained worktrees ·
`followup-removals-log.txt` the 8 removals that refused inside the batch loops ·
`post-cleanup-verification.txt` end state generated from live git
