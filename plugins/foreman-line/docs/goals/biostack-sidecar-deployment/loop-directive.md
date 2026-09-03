# /goal Loop Directive — `biostack-sidecar-deployment`

Generated 2026-09-03 by the coordinator on Gate 1 ratification. Modeled on `plugins/foreman-line/docs/kickstarters/foreman-line-coordinator-loop.md`, governed by `plugins/foreman-line/docs/COORDINATOR-PATTERN.md`, parcel mechanics per `plugins/foreman-line/skills/parcel-driven-development/SKILL.md`.

## COORDINATOR OWNERSHIP — read before dispatching anything

> **Queue owner: the Claude Opus 5 coordinator session that claimed this goal on 2026-09-03.** The prior Codex coordinator drafted the charter through `bf7ef39` and left no live ownership claim and no in-flight parcel; ownership was claimed at a clean parcel boundary (zero parcels started). Exactly one coordinator owns this queue at a time. If you are not the owner, do not dispatch. If ownership is ever ambiguous, report to Clint and wait — never assume. (Rule earned 2026-07-15 when a second coordinator committed onto a live parcel branch, `491fb80`, benign only by luck.)

Goal branch: `codex/goal-biostack-sidecar-deployment`
Goal worktree: `C:\Users\clint\.codex\worktrees\biostack-sidecar-deployment-goal\BioStack`
Pinned base: `4d8754c670a7d4553ade857be80aa170ede85653`

## Who you are

The Coordinator (D4): you consume verification results, you never produce them. You route rework, ratify spec amendments, run deterministic passes, and triage adversarial reviews. State sources of truth, read at the start of every iteration:

- `charter.md` — ratified D1-D18, parcel plan, SC-01..SC-10, SG-*, stop conditions, exit criterion. **Binding.**
- `discovery.md` — repo/runtime facts as of the pinned base.
- `plan-review-findings.md` — plan-level adversarial review + coordinator triage. **Binding where triaged `fix`.**
- This file — operational overlay.

## Standing authorizations (granted by Clint at Gate 1, 2026-09-03; scoped to this goal only)

1. **Gate 2 / dispatch approval** is granted for exactly parcels **P01, P02, P03, P04** — no others. Dispatch is contingent on: the parcel's spec passing coordinator lint, its final exact Allowed Files being fixed in the spec, its Step 0 restate-and-stop gate being present in the kickstarter, and its charter dependency order being satisfied. A new parcel idea is a stop-and-report, not a dispatch.
2. **Step 0 rulings** stay with the coordinator — except a flag that requires modifying the ratified charter, which becomes a coordinator-drafted amendment requiring Clint's ratification, committed alone before any code.
3. **Local-only work is authorized:** worktree creation, branch creation, local commits on parcel branches, local test/lint/build runs, local Docker container builds and probes against `127.0.0.1`.
4. **Gate 3A and Gate 3B are NOT granted.** Explicitly denied by the ratification text and by D18.

## Explicitly DENIED by the Gate 1 ratification text (verbatim scope)

No **push**, **PR**, **merge**, **Azure mutation**, **secret creation/rotation**, **provider enablement**, **external call**, **protected-data use**, **public ingress**, or **sidecar/API production configuration change**.

Operational consequences, spelled out so no agent has to infer them:

- No `git push`, no `gh pr create`, no merge to `main`. Parcel branches stay local.
- No `az` command that mutates. No `az login` against production. SC-05, SC-06, SC-07, SC-08, SC-10 are **not runnable** in this goal and must not be claimed green.
- No real service token is generated, stored, or committed. Local probes use an obviously-synthetic throwaway value that never leaves the local container/env and never enters git, receipts, or evidence.
- No network egress to any scientific provider, model host, or ToolUniverse endpoint — in CI config, in tests, or in local runs. Local container probes bind `127.0.0.1` only.
- Package installs from public registries (npm/PyPI) for local build/test are ordinary tooling, not "provider enablement" — but dependency **changes** to the sidecar are out of scope for every parcel unless that parcel's Allowed Files include the manifest.

## The reachable terminal state (this goal cannot reach its exit criterion)

The charter's exit criterion requires Gate 3A and Gate 3B, both human-only under D18 and both withheld. Per `COORDINATOR-PATTERN.md`, a human gate is a **loop stop condition**, not an agent-reachable state. The coordinator therefore drives to:

> P01-P04 each shaped, built, deterministically verified, adversarially reviewed (two independent reviews each per D17) and security-reviewed, accepted by coordinator closure check, and **held unmerged on their local parcel branches**; a Gate 3A request package written naming the exact candidate commits, the verification chain, residuals, and everything the human must do by hand; loop stopped.

Gate 3A and Gate 3B are recorded as **open exit conditions** carried to the final report. Parcel-level green does not roll up into goal-level satisfied (lesson #33).

## Queue (strict order — charter dependency order is mandatory)

| # | Parcel | Depends on | Routing | Review depth (D17) |
|---|---|---|---|---|
| 1 | **P01** — Deterministic sidecar CI and container contract | — | frontier builder | 2 adversarial + 1 security |
| 2 | **P02** — Internal Azure Container App definition | P01 accepted | frontier builder | 2 adversarial + 1 security |
| 3 | **P03** — Manual OIDC release workflow and deployment verifier | P01 + P02 accepted | frontier builder | 2 adversarial + 1 security |
| 4 | **P04** — Controlled enablement and operator runbook | P03 accepted **and rebased** | frontier builder | 2 adversarial + 1 security |

**Serialization rule (charter, mandatory):** P03 owns `.github/workflows/deploy-research-sidecar.yml` first. P04 may touch that file only after P03 is accepted and P04 is rebased onto it. This is the one collision the charter names; the plan review is tasked with finding others.

## Per-parcel cycle

Shaping session (docs-only) → coordinator lint (verify every factual claim on disk) → Gate 2 check → builder dispatch in its own worktree/branch with a Step 0 restate-and-stop gate → rule on flags (a real spec gap becomes a ratified amendment committed alone before code) → completion claim mapping every AC to evidence with a test count (wrong-shaped claims are presumptively empty) → coordinator closure check against disk BEFORE re-running anything → deterministic pass on the coordinator's machine → two independent adversarial reviews + one security review, fresh sessions, reviewers never fix and never commit, hostile-input probing licensed → triage (fix / accept-as-documented / informational); reproduce disputed findings yourself before ruling → rework with its own Step 0 gate and a test-count tripwire → **HOLD (no merge — Gate 3A ungranted)** → Stage-F-partial closure (spec annotated, lessons appended, charter/loop state updated).

## Worktree and branch convention

| Parcel | Branch | Branched from | Worktree |
|---|---|---|---|
| P01 | `parcel/sidecar-P01` | goal branch | `D:\Repos\BioStack-sidecar-P01` |
| P02 | `parcel/sidecar-P02` | goal branch + P01 | `D:\Repos\BioStack-sidecar-P02` |
| P03 | `parcel/sidecar-P03` | goal branch + P01 + P02 | `D:\Repos\BioStack-sidecar-P03` |
| P04 | `parcel/sidecar-P04` | goal branch + P03 (rebased) | `D:\Repos\BioStack-sidecar-P04` |
| Reviews | detached | the parcel commit under review | `D:\Repos\BioStack-sidecar-<parcel>-review-<A|B|SEC>` |

Worktrees live on `D:` beside the main checkout (existing repo precedent: `D:\Repos\BioStack-api-tls-fix`, `D:\Repos\BioStack-ci-kompress`). Never `C:\Users\clint\.codex\worktrees\` — that tree is Codex-managed and holds other agents' live work. The branch and worktree are named in every directive, never ambient.

### Parcels branch from the GOAL branch, not from `main` (coordinator decision, 2026-09-03)

`main` at the pinned base carries **none** of this goal's canon: no `charter.md`, no `plan-review-findings.md`, no spec, no standing constraints. A builder branched from `main` could not read its own binding scope. Parcels therefore branch from `codex/goal-biostack-sidecar-deployment`, which is `main` plus docs-only commits.

Consequences, made explicit so no agent has to infer them:

- A parcel branch's diff **versus the goal branch** is exactly that parcel's Allowed Files. That diff — not the diff versus `main` — is what the coordinator closure-checks and what a reviewer reviews.
- Inheriting the goal docs from the base branch is **not** a violation of a parcel's Allowed Files. Allowed Files govern what the builder *writes*. A builder still must not edit any goal doc.
- The eventual Gate 3A merge candidate is the goal branch with the parcel branches integrated. Composing it is Gate 3A work and is not authorized here.
- This is a branch-topology decision, not a charter amendment: the charter fixes the pinned base and the parcel dependency order, and both are preserved.

### Where specs live

`plugins/foreman-line/docs/goals/biostack-sidecar-deployment/specs/P0N-<slug>.md`. BioStack has no `docs/specs/` tree, so the goal directory holds them. Specs are written by the shaping session, linted and approved by the coordinator, and committed to the **goal branch** before the parcel branch is cut — so the builder inherits its own spec.

### Standing constraints are vendored, not referenced

`plugins/foreman-line/docs/kickstarters/STANDING-CONSTRAINTS.md` **does not exist in this repository** — it ships with the Foreman Line plugin install. A verbatim copy is vendored to `plugins/foreman-line/docs/goals/biostack-sidecar-deployment/STANDING-CONSTRAINTS.md` so it travels with every parcel branch. Kickstarters reference the vendored path. Never cite the plugin-install path to an agent working in a BioStack worktree.

The primary checkout `D:\Repos\BioStack` carries user-owned untracked `.audit/` and `.codex-temp/`. It is **read-only for this goal** and stays untouched.

## Deterministic pass environment (verified on this machine 2026-09-03)

- Shell: **PowerShell**. Run `node -v` first every pass. Verified: node `v24.7.0`, npm `11.6.2`, uv at `C:\Users\clint\miniconda3\Scripts\uv.exe`, python at `C:\Users\clint\miniconda3\python.exe`, Docker server `29.7.2` (daemon up).
- **`rtk` is NOT installed on this machine** despite `AGENTS.md` instructing an `rtk` prefix. All verification runs raw. Never let an agent claim an `rtk`-prefixed command succeeded.
- Never read an exit code through a truncated pipeline. Capture the tail *and* `$LASTEXITCODE`.
- Python: the sidecar venv is `backend\research-sidecar\.venv`. A fresh parcel worktree has **no** `.venv` — the builder must create one (`uv sync` / `uv venv`) inside its own worktree and must not reuse or mutate the primary checkout's venv.

### Baseline test counts (coordinator-verified on disk, primary checkout, base `4d8754c`)

| Suite | Command | Result |
|---|---|---|
| sidecar pytest | `.\.venv\Scripts\python.exe -m pytest tests -q` in `backend\research-sidecar` | **53 passed, 1 skipped** |

This is the tripwire number. Discovery claimed 53 + 1 expected legacy-config skip; the coordinator reproduced it rather than inheriting it. Any parcel completion claim reporting fewer than 53 passing sidecar tests, or a second skip, or a new failure, trips and gets rejected without further inspection.

## Per-iteration algorithm

1. Read `charter.md`, `plan-review-findings.md`, and this file. Identify the active queue item and the cycle step it sits at.
2. If a background agent was expected: **check for its completion first.** If it is not running and delivered no completion claim, assume process death, not completion. Disk state without a claim is UNCLAIMED work — never accept it as done. Recover by dispatching a fresh agent with a resume directive whose Step 0 restates the ORIGINAL directive, inventories disk against it, states the live test count, flags gaps and half-written files, then stops for a ruling.
3. Advance as far as the iteration allows. Builders, shapers, and reviewers are fresh sessions/agents with kickstarters that include standing constraints by reference (the vendored `plugins/foreman-line/docs/goals/biostack-sidecar-deployment/STANDING-CONSTRAINTS.md`).
4. Verify every claim on disk before accepting it. Green checks verify state; only per-item closure checks verify work.
5. Rework directives say "every X," never "the listed X" — findings are a floor, not a ceiling, and no role is exempt from the sweep, including the coordinator.
6. Where two reviews disagree, reproduce the disputed finding yourself before triaging. The reproduction is the tie-breaker at triage and the closure proof at acceptance.
7. When a parcel spec restates a charter exit criterion or an SC-xx row, **diff the two texts word by word.** A criterion naming a produced artifact (a live API response, a real deployed digest) is satisfied only by that artifact; a fixture imitating it is a self-graded claim (lesson #33). Given Gate 3A/3B are ungranted, expect several SC rows to be fixture-only by necessity — those must be labeled **DEFERRED-TO-GATE-3**, never green.

## Loop-stop conditions

Call `ScheduleWakeup stop:true`, then write the report. Do not ask questions mid-loop.

The eleven charter stop conditions apply verbatim and are not restated here. In addition, this loop stops when:

12. Any work requires Gate 3A or Gate 3B, i.e. any push, PR, merge, or Azure mutation — **including the goal's own exit criterion.** This is the expected terminal stop.
13. A tripwire fires twice on the same parcel (test count, wrong-shaped claim, false closure).
14. A security review blocks and the finding cannot close within the parcel.
15. Triage of any review re-opens Gate 1 for a locked decision. The re-open is **scoped, not blanket**: name exactly which downstream work each re-opened decision blocks, and let provably orthogonal work proceed under the standing authorizations. Over-holding is a real cost, not a safe default (lesson #27).
16. Queue empty: P01-P04 all accepted and held. Write the Gate 3A request package and the final report.

## Wakeup pacing

Blocked only on running background agents → schedule a 1200-1800s fallback and yield; completion notifications are the primary wake signal and the wakeup is insurance. Actively coordinating → keep working, no wakeup. Never schedule short wakeups to poll harness-tracked agents.

## Commit attribution

Local parcel and goal commits end with:

```
Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
```
