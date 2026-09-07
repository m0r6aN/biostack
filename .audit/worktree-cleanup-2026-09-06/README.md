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

## Preserved material

- **Four orphaned commits** (reachable from no branch or tag) are now permanent tags:
  `salvage/orphan-profile-goals-contract`, `salvage/orphan-auth-session-recovery`,
  `salvage/orphan-db-auth-billing-repair`, `salvage/orphan-passkey-webauthn`.
- **98 dangling commits** archived to a verified git bundle before `gc` pruned them.
  Stored **outside the repo** (18 MB binary, deliberately not committed) at:
  `D:/Repos/_biostack-salvage-2026-09-06/dangling-commits.bundle`
  Restore with: `git fetch D:/Repos/_biostack-salvage-2026-09-06/dangling-commits.bundle 'refs/tags/*:refs/tags/recovered/*'`
  Contents listed in `dangling-manifest.txt`.
- **Uncommitted changes** from five worktrees captured as patches in `uncommitted-patches/`
  before those worktrees were force-removed (includes a 27-file staged tree from the
  remediation integration worktree). Reapply with `git apply <name>.patch`.

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
`dangling-manifest.txt` archived commits · `hold-list.txt` retained worktrees
