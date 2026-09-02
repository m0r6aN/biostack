# Coordinator loop directive

## Ownership

One coordinator owns this goal and its records. Ownership transfers only at a parcel boundary through an updated handoff. The coordination branch is `codex/test-repro-contracts`; the five parcel branches remain independent and intentionally unmerged.

## Standing authorizations

- Gate 1: D1-D6 are ratified as written on 2026-09-02 by explicit developer direction. This ratification changes no scope, exit criterion, or Gate 3 posture.
- Gate 2: the dated authority in `DISCOVERY.md` permits the named test-only reproduction parcels, using synthetic data and local intercepting fakes only. No new parcel dispatch is pending.
- Gate 3: none. No merge, release, provider enablement, external transmission, production fix, or publication is authorized.

## Queue

1. Record plan-review findings and triage.
2. Reconcile all five parcel handoffs against branch ancestry, exact Allowed Files, and assertion-level evidence.
3. Record the frontend dependency environment blocker without relabeling it as a test result.
4. With Gate 1 ratified, stop at the next human or external boundary. Any requested rework must receive a new scoped directive and fresh review; intentionally failing branches still do not merge.

## Per-iteration algorithm

1. Read the current initiative records and ownership block.
2. Verify claims on disk before accepting them; wrong-shaped or environmental claims are not assertion-level reproductions.
3. Check branch/worktree cleanliness and exact Allowed Files before any rerun.
4. Run only the narrowest deterministic command named by the parcel handoff.
5. Preserve reproduced, not reproduced, inconclusive, and environment-blocked outcomes separately.
6. Stop at a human gate or any scope, contract, security, or merge-boundary change.

## Stop conditions

- A new decision, scope change, or exit-criterion change is not explicitly ratified.
- The execution base, Allowed Files, or test-only posture changes.
- A harness/environment failure cannot be separated from an assertion failure.
- Any request would merge an intentionally failing branch or authorize a production/external effect.
