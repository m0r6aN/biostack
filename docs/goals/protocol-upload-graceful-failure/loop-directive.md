# Loop Directive — protocol-upload-graceful-failure

## Ownership block

- Goal: `protocol-upload-graceful-failure` (charter: `CHARTER.md`, Gate 1 ratified 2026-10-03).
- Owner: the `/goal` coordinator session started 2026-10-03. One goal, one coordinator; transfer only at a parcel boundary by written handoff.
- Branch/worktree: `fix/protocol-upload-graceful-failure` @ `D:/Repos/BioStack/.worktrees/protocol-upload-graceful-failure-20261003`; comparison base `1c8a16e5e950e3b75f559e9c8c0744f28db2f6a5`.
- State: see `HANDOFF.md` once written; until then the registry row in `docs/specs/INDEX.md` and the git log on the branch are authoritative.

## Standing authorizations (verbatim from charter, with contingencies)

- Gate 2 (dispatch): delegated to the coordinator for BIO-ANALYZER-001 under D1–D5. Contingency: spec approved only after the independent spec review passes and triage closes every finding; `GATE2.md` records spec hash, builder, branch, worktree, comparison base, permissions, checks, evidence path, reviewers. Any failed/stale/wrong-environment verification voids it.
- Gate 3 (merge/push/PR): **withheld**. The coordinator stops before any outward action.

## Queue (strict order)

1. BIO-ANALYZER-001 — spec review (1 independent reviewer) → triage → Gate 2 → builder (fresh `task` session, Step 0 restate-and-stop) → coordinator closure check on disk → deterministic pass → adversarial code review (fresh `reviewer`) → triage/rework → HANDOFF.
2. Queue empty → stop.

## Per-iteration algorithm

Read `docs/specs/INDEX.md` row + `git log` on the branch → determine the next unmet step above → do exactly that step → record result in-repo → continue. Builders never approve their own work; reviewers never fix or commit; disputed findings are reproduced by the coordinator before ruling.

## Stop conditions

Charter stop conditions plus: deterministic tripwire fires twice; reviewer verdict FAIL that the coordinator cannot reproduce and resolve; any need to push/PR/merge/deploy; developer says stop.

## Pacing

Subagent completion notifications are the wake signal; no polling.
