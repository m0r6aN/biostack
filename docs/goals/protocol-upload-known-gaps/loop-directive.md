# Loop Directive — protocol-upload-known-gaps

## Ownership block

- Goal: `protocol-upload-known-gaps` (charter: `CHARTER.md`, ratified 2026-10-04).
- Owner: the `/goal` coordinator session started 2026-10-04. One goal, one coordinator; transfer only at a parcel boundary by written handoff.
- Coordinator branch/worktree: `goal/protocol-upload-known-gaps` @ `D:/Repos/BioStack/.worktrees/protocol-upload-known-gaps-20261004`.
- Parcel branches (each from the coordinator branch at its dispatch commit): `fix/analyzer-xlsx-package-robustness` (002), `fix/analyzer-single-word-frequency-gate` (003), `fix/analyzer-spreadsheet-row-reconstruction` (004), worktrees `D:/Repos/BioStack/.worktrees/bio-analyzer-00{2,3,4}-20261004`.
- State: `docs/specs/INDEX.md` rows and `git log` on the coordinator branch are authoritative until `HANDOFF.md` is written.

## Standing authorizations

- Gate 2: authorized by the developer 2026-10-04 for 002, 003, 004. Voided per parcel by failed, stale, or wrong-environment verification.
- Gate 3 (merge/push/PR): **withheld**. Local integration of parcel branches into the coordinator branch is permitted; nothing leaves the machine.

## Queue (strict order)

1. 002 and 003 in parallel: builder (fresh `task` session; Step 0 restatement written to the evidence file, builder continues because the spec already passed independent review) → coordinator closure check on disk → deterministic pass → independent code review (fresh `reviewer`, read-only, hostile-input probing licensed) → triage/rework → integrate into the coordinator branch (002 first, then 003).
2. 004 after both are integrated: same cycle with **two** independent reviewers.
3. Coordinator HANDOFF; stop.

## Per-iteration algorithm

Read `docs/specs/INDEX.md` and `git log` → determine the next unmet step → do exactly that step → record the result in-repo → continue. Builders never approve their own work; reviewers never fix or commit; disputed findings are reproduced by the coordinator before ruling. A real spec gap becomes a coordinator-ratified amendment committed alone before the dependent code, and re-hashes the Gate 2 record.

## Builder environment rules (lesson from 001)

Builders work only in their named worktree using absolute paths. They never edit files in `D:/Repos/BioStack` (the main checkout) or any other worktree.

## Stop conditions

Charter stop conditions plus: deterministic tripwire fires twice; reviewer FAIL the coordinator cannot reproduce and resolve; any need to push/PR/merge/deploy; developer says stop.

## State

Gate 2 recorded for 002 and 003; 004 Gate 2 recorded at its dispatch. See INDEX rows.
