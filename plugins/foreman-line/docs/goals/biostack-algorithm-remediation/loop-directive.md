# Coordinator Loop Directive — BioStack Algorithm Remediation

## Ownership

**Queue owner:** Codex goal task `01a061dd-255b-7942-83bb-b6ad82450ac3` (`/root`).

One goal has one coordinator. Ownership may transfer only at a parcel boundary through an explicit update to this block. If another live coordinator claims this queue, stop and report; never assume shared ownership.

## Current state

- Goal branch: `codex/goal-biostack-algorithm-remediation`
- Remediation base: `main` / `origin/main` at `339f259b1a467034db4f57cf9d774c292f11b53a`
- Gate 1: D1-D14 ratified 2026-09-02
- Plan review: complete; triage commit `ef4376b0a186dd0c0fe9e4b7772bde07e39756eb`
- Narrow Gate 1 Amendment 01: pending for D6, D8, and D9
- Gate 2: contingent authorization granted for Q01-Q04 and P01-P07
- Gate 3: ungranted
- Diagnostic coordination and five parcel branches: preserved and unmerged

## Standing authorizations

The developer granted exactly:

> Gate 1: I ratify D1-D14 as written and grant contingent Gate 2 dispatch authorization for Q01-Q04 and P01-P07. Gate 3 remains ungranted.

Operational effect:

1. Q01-Q04 and P01-P07 may be shaped and dispatched only after their specs exactly match the ratified charter and any ratified amendment, name an isolated branch/worktree, pass Step 0, and retain exact Allowed Files.
2. While Amendment 01 is pending, P03 is blocked by D6; P05/P06 are blocked by D8; P07 is blocked by D9. Orthogonal shaping may continue for P01, P02, P04, and Q01-Q04.
3. The authority does not permit a new parcel, changed Allowed Files, external network/provider access, production/cloud data, secrets, pushes, pull requests, merges, deployment, publication, release, or cleanup of diagnostic branches.
4. Gate 3 remains human-only for the exact candidate commit set and exact merge/release action after the complete verification chain is green.

## Queue

The coordinator advances this queue in dependency order while allowing only collision-free work to run concurrently.

1. Record Amendment 01 ratification or requested changes. Unblock only its affected parcels.
2. Q04 frontend dependency readiness; command-only, no repository changes.
3. Shape and lint P01 parser semantics.
4. Shape and lint P02 protocol-ingestion cancellation/OCR boundary.
5. Shape and lint P04 evidence provenance with the repaired staging/gate harness.
6. Shape and lint Q01 Collective outbound, Q02 endpoint inventory, and Q03 regex adjudication as three independent evidence lanes.
7. After Amendment 01: shape and lint P03 interaction safety.
8. After Amendment 01: shape and lint P05 terminal-state custody, then P06 source-cap enforcement; P05 precedes final P06 integration verification.
9. After Amendment 01 and successful Q04: shape and lint P07 authenticated-consented frontend relay.
10. For each shaped parcel: Gate 2 contingency check, isolated dispatch, Step 0 ruling, build/investigation, closure check, deterministic pass, required adversarial/security review, and rework loop.
11. Assemble the exact green candidate commit set and request human Gate 3. Do not push, open a PR, merge, deploy, or release before approval.
12. After exact Gate 3 approval: rebase/verify/merge only the approved set, verify merged main and CI, then complete Stage F.

## Per-iteration algorithm

1. Read the charter, `plan-review-findings.md`, this directive, active specs, and handoffs. Reconcile them with Git/worktree state before trusting carryover.
2. Fetch `origin` before each dispatch wave. If `origin/main` moves materially from the pinned base, stop affected dispatch and apply the charter's base-change rule.
3. Shape one parcel in a fresh frontier session. The shaper may write only its spec in this goal's coordination directory.
4. Coordinator-lint every factual claim on disk: file existence, test-project inclusion, source ownership, dependencies, exact commands, baseline counts, collision points, and security gates.
5. Confirm the Gate 2 contingencies. Create or verify one `codex/` branch and one isolated worktree named in the spec. No parcel branches from a diagnostic branch.
6. Dispatch a fresh builder/investigator. Step 0 restates goal, base, exact Allowed Files, forbidden work, invariants, tests, security/network limits, and stop conditions, then stops for coordinator confirmation before any edit.
7. Rule on flags. Any missing decision, new file, production dependency, changed invariant, or new reproduced Q defect stops the affected parcel for a narrow amendment.
8. On completion claim, inspect the exact commit and diff before rerunning anything. Wrong-shaped claims are presumptively empty. Verify all required files and no forbidden files.
9. Consume deterministic evidence: targeted regression, adjacent suites, exact count delta, aggregate scope, `git diff --check`, ancestry, clean status, and network/local-fake receipts.
10. Dispatch fresh read-only adversarial review on the exact candidate commit. Use two independent adversarial reviewers plus a separate defensive security reviewer for P02, P04, P05, P06, and P07. Reviewers never fix or commit.
11. Triage every finding as fix, accept-as-documented, or informational. Reproduce disputed findings before ruling. Any rework changes the candidate commit and invalidates earlier acceptance.
12. Hold all candidate commits unmerged until the complete goal-level chain is green and the human grants exact Gate 3.

## Worktree and branch convention

- Branch: `codex/biostack-remediation-<parcel-lowercase>`
- Worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-<parcel-lowercase>\BioStack`
- Every worktree starts from the Gate-2-verified base and records its starting commit.
- Existing diagnostic worktrees and branches are read-only evidence sources and are never used as parcel bases.
- BioStack does not vendor the Foreman permission-profile emitter. The coordinator therefore verifies each local worktree and dispatches agents with exact Allowed Files and explicit no-network/no-external-provider constraints.

## Universal stop conditions

Stop the affected parcel and report when:

- a pending or reopened human gate controls the next action;
- current `origin/main` differs materially from the verified base;
- an exact Allowed File is missing or insufficient;
- a production/public-contract decision is absent;
- an invariant can pass only by weakening or deleting its regression;
- a real external provider, network registry, cloud resource, production database, protected data, secret, or payload dump would be required;
- a security finding cannot close within the parcel;
- the same tripwire or false-closure condition fires twice;
- any push, PR, merge, deployment, publication, release, or diagnostic-branch mutation is proposed before exact Gate 3.

## Stage-F requirements

Stage F records exact merged commits, verification counts and commands, review/security dispositions, Q-lane outcomes, residual risk, lessons, and branch custody. It verifies that the original diagnostic coordination and parcel branches still exist and remain unmerged. Remediation worktree cleanup occurs only after approved merges; diagnostic cleanup is never implied.
