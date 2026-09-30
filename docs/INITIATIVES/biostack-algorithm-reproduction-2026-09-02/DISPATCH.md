# Dispatch

Each parcel uses a branch named `codex/test-repro-<parcel>` and its own worktree, based on the contracts commit. Parcel agents may edit only the exact files named in `PARCELS.md` and the corresponding parcel handoff section in `SESSION-HANDOFFS.md`.

Required handoff: branch, commit, allowed-file diff, test command, exit status, exact expected failure, unexpected failures, and residual uncertainty.
