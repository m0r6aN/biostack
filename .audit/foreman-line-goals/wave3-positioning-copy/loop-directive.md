# Loop directive — wave3-positioning-copy

## Ownership
- Owner: the Coordinator session that last updated `charter.md` §Ownership. One goal, one coordinator; transfer only at parcel boundaries. If §Ownership names another live coordinator, STOP and report.
- Authoritative state: this repo-local directory only (not plugin-global goals).

## Standing authorizations (verbatim scope)
- **Gate 1:** D1–D5 ratified by the developer 2026-09-24. H1–H5 are re-opened and need explicit developer ruling. Silence never counts as ratification.
- **Gate 2 (dispatch):** covered by the developer's standing authorization for non-destructive decisions and dispatch, **effective only once W3-P0 evidences Waves 1 and 2 merged on `main`** and H1–H5 are ruled. Contingency: any spec gap becomes a ratified amendment committed alone before code.
- **Gate 3 (merge):** NOT granted. Every merge stops for the developer.

## Hygiene
- Never touch the primary checkout's dirty working tree. All builds and reviews run in isolated worktrees under `.worktrees/`.
- Reviewers never fix and never commit. Builders run the Step 0 restate-and-stop gate.

## Queue (strict order)
1. W3-P0 re-run: check `origin/main` for the Wave 2 evidence set (`w3-p0-reconciliation.md` §Wave 2).
2. D5 pre-build copy review on the packet text (H5 input).
3. W3-P1 → W3-P2 (base = Wave 2 merge SHA) → W3-P3 → W3-P4 (human).

## Per-iteration algorithm
Run W3-P0 re-check. If Wave 2 is not merged or H1–H5 are not ruled: update the §Ownership state, write a stop report, and STOP. Otherwise run the charter's parcel cycle (shaping → lint on disk → Gate 2 → builder worktree → closure check → deterministic pass → two adversarial reviews (doctrine-sensitive) → triage → rework → Gate 3 stop).

## Stop conditions
The charter's stop conditions apply, plus: H1–H5 unruled; Gate 3 reached; human comprehension check (W3-P4) pending.

## Wakeup pacing
No self-scheduled wakeups while blocked on human or Wave 2 action. Resume by `/goal resume wave3-positioning-copy`.
