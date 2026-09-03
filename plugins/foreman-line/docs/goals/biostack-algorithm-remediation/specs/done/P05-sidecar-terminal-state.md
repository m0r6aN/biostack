---
parcel: P05
title: Sidecar terminal-state custody
status: complete-merged-pr-265
goal: biostack-algorithm-remediation
initiative: BioStack Algorithm Remediation
project: BioStack research sidecar
wave: Wave 1 - independent fixes
risk: critical lifecycle integrity
routing: frontier implementation plus SG-SIDECAR
branch: codex/biostack-remediation-p05
worktree: 'C:\Users\clint\.codex\worktrees\biostack-remediation-p05\BioStack'
base: 339f259b1a467034db4f57cf9d774c292f11b53a
base_tree: 0f6d0b609ce255aad5cca81698eb3ad0917cfda6
diagnostic_commit: 82295c3f36b412b9917eaf047a70b10ee2a67cdc
---

# P05 - Sidecar terminal-state custody

Coordinator lint: **PASSED 2026-09-02 after Narrow Gate 1 Amendment 03.** The ratified D8 timeout-terminalization supplement and replacement three-file AF-P05 resolve the pinned cancel-then-fail conflict. Dispatch remains contingent on the named clean isolated worktree and an exact fresh Step 0 restatement.

Review disposition: **REWORK REQUIRED 2026-09-02.** Candidate `923900c66d7046c282d397ba60d85b9eebcf1a58` passed the shaped verification chain but both adversarial review A and the separate security review found that `update` validates and mutates fields in one loop. A trailing invalid key can therefore raise `AttributeError` after partially installing an incomplete immutable terminal snapshot. Rework must prevalidate all keys under the existing lock before any mutation, preserve `AttributeError`, prove the entire active snapshot is unchanged, and strengthen the preserved timeout case's complete-snapshot assertions without changing the exact 10-case census. All reviews of that candidate are invalidated; the replacement requires two fresh adversarial reviews and a fresh security review.

Final disposition: **REVIEW COMPLETE 2026-09-02.** Rework candidate `2f4a092fb7f0ec534996e6ad1116e8b50d612b77` closes the invalid-key atomicity finding and complete-timeout assertion gap. Target `10/10`, adjacent `38 passed / 1 expected skip`, aggregate `48 passed / 1 expected skip`, exact eight-finding lint baseline, scope, ancestry, and offline boundaries are green. Two fresh adversarial reviews and a fresh separate SG-SIDECAR/SG-SCOPE review ACCEPT this exact hash. It is local and held unmerged; Gate 3 remains ungranted. P06 may be shaped against this exact accepted candidate.

## Goal and outcome

Remediate R-SIDE-01 from the pinned `main` base. The in-memory sidecar job store must make the first terminal transition atomically authoritative and preserve that complete terminal snapshot against every late worker update, identical replay, and later cancellation request. Preserve the diagnostic timeout/late-worker scenario as a regression, repair its plan-review-invalid wait with a deterministic worker-completion signal, add direct hostile state/race coverage for the full ratified D8 contract, make the smallest production change in AF-P05, and return one committed local candidate plus deterministic evidence for independent review.

**Shaping disposition: active under contingent Gate 2.** The pinned runner calls `request_cancel` before `_mark_timeout`. Amendment 03 authorizes the smallest resolution: the timeout path installs one atomic `failed` / `execution_timeout` terminal snapshot with `cancel_requested=True`, and the store rejects every later terminal mutation. A fresh Step 0 and coordinator confirmation remain mandatory before edits.

This parcel does not implement or verify the `maximum_source_count` half of D8; P06 owns that disjoint production behavior. P05 establishes the terminal-custody candidate that must precede P06 shaping and final combined P06 verification.

This parcel does not authorize a push, pull request, merge, deployment, provider enablement, publication, release, or diagnostic-branch mutation. Gate 3 is human-only and remains ungranted.

## Authority, dependencies, and gates

- Controlling goal: `biostack-algorithm-remediation`.
- Controlling decisions: ratified D1-D14 in `plugins/foreman-line/docs/goals/biostack-algorithm-remediation/charter.md`; P05 depends specifically on D1, D2, amended D8, and D10-D14.
- Narrow Gate 1 Amendments 01 and 03 are ratified. Amendment 01 supplies the exact lifecycle custody contract; Amendment 03 supplies the timeout-terminalization supplement and replacement AF-P05.
- Remediation base: `339f259b1a467034db4f57cf9d774c292f11b53a` from `main` / `origin/main`; pinned tree `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`.
- Diagnostic evidence source: `82295c3f36b412b9917eaf047a70b10ee2a67cdc`, whose retained lifecycle test blob is `689fb7db2545ce1df87cabfb7e448da77d05b14a`. Import only the named test-file contents into the remediation branch; do not merge, rebase, cherry-pick, rewrite, delete, or clean the diagnostic branch or its history.
- Plan-review dependencies: F2 requires a deterministic worker-completion signal instead of waiting for the forbidden overwrite; F3 requires the complete D8 state/snapshot contract and serializes P06 after P05; F11 requires exact commands, grounded base counts, offline controls, expected deltas, and evidence receipts.
- Gate 2 is contingent: coordinator lint must pass against the amended charter; local `main` and the verified `origin/main` ref must remain at the pinned base; the named isolated branch/worktree must start from that base/tree; and the builder's Step 0 restatement must be accepted before edits.
- SG-SCOPE and SG-SIDECAR are mandatory. Two fresh adversarial reviews and one separate defensive security review are required on the exact final candidate commit. Any review failure blocks Gate 3.
- Gate 3 remains ungranted. The parcel candidate stays local and unmerged until the complete goal chain is green and the human approves the exact commit/merge set and release action.

P01-P04, P07, P08, and Q01-Q04 are not code dependencies. P06 is a downstream custody dependency only: no P06 shaping or dispatch proceeds until P05 has a final candidate, and final P06 integration verification must contain the exact accepted P05 candidate behavior before exercising the P06 candidate.

## Ratified timeout-terminalization resolution

The exact pinned call sequence is visible at `jobs/runner.py:91-98` and `jobs/store.py:92-108` in base `339f259b1a467034db4f57cf9d774c292f11b53a`:

1. `JobRunner._run_job` catches `FuturesTimeoutError`.
2. It calls `self._store.request_cancel(job_id)`.
3. For every active status, `request_cancel` writes terminal `cancelled`, sets `finished_at_utc`, and refreshes `updated_at_utc`.
4. The runner then calls `_mark_timeout`, which attempts a second terminal write through `store.update`: `failed`, `error_code="execution_timeout"`, timeout progress/error text, and a failed artifact.

Under amended D8, step 3 is the first terminal transition and must win. A correct guard confined to `store.py` therefore makes step 4 an idempotent no-op and leaves a `cancelled` record without `execution_timeout` provenance. Conversely, allowing step 4 to replace step 3 would directly violate first-terminal-writer custody. `request_cancel` has no reason parameter and is called by both the public cancellation endpoint and the timeout runner, so `store.py` cannot distinguish these cases without an unratified heuristic or public-contract change.

The preserved diagnostic scenario explicitly observes a timeout failure with `error_code="execution_timeout"`, and R-SIDE-01 is framed as a late worker overwriting that timeout failure. The required amended-D8 coverage also distinguishes timeout from cancellation and failure. Silently changing the regression to accept `cancelled` would weaken the reproduced invariant and discard terminal error provenance.

The ratified resolution adds exactly `backend/research-sidecar/src/biostack_research_sidecar/jobs/runner.py` to AF-P05 and authorizes the timeout path to install one atomic first terminal snapshot containing `status=failed`, `cancel_requested=True`, `error_code="execution_timeout"`, the existing timeout error/progress text, one finish timestamp, and timeout artifact, without first calling the user-cancellation transition. `store.py` enforces first-terminal-writer custody for every later update/cancellation. User cancellation that wins while active remains terminal `cancelled` and immutable. No other timeout policy or runner behavior may change.

## Exact branch, worktree, and source custody

- Branch: `codex/biostack-remediation-p05`.
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-p05\BioStack`.
- Starting commit: `339f259b1a467034db4f57cf9d774c292f11b53a`.
- Starting tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`.
- At shaping time, neither the named local branch nor the named worktree exists. If either appears under another owner before dispatch, stop for coordinator reconciliation.
- The diagnostic branch remains an evidence source only. The builder reconstructs the one diagnostic test path by copying its content, then repairs and extends that file in place. No diagnostic commit is incorporated by merge or cherry-pick.

## Exact Allowed Files - AF-P05

The builder may edit exactly these files:

1. `backend/research-sidecar/src/biostack_research_sidecar/jobs/store.py`
2. `backend/research-sidecar/src/biostack_research_sidecar/jobs/runner.py`
3. `backend/research-sidecar/tests/test_runner_terminal_state_reproduction.py`

The third path is absent on the pinned base and must be reconstructed from diagnostic commit `82295c3f36b412b9917eaf047a70b10ee2a67cdc`, then repaired in place. No substitute, nearby file, or fourth file is authorized.

## Forbidden and out of scope

- Every repository path outside AF-P05 is forbidden, including `contracts/models.py`, `workflows/executor.py`, request/response models, app routes, configuration, package/project/lock files, existing adjacent tests, P06's source-cap regression, frontend/backend .NET code, schemas, workflows, and goal documents.
- Do not implement `maximum_source_count`, alter result/claim/provenance/tool materialization, or begin P06 work in this parcel.
- No public API redesign, status-enum change, durable-storage design, schema or serialization change, timeout-policy change, executor/runner refactor, concurrency-setting change, dependency upgrade, unrelated lint cleanup, or broad copy-on-read rewrite.
- Do not weaken, delete, rename away, skip, or replace `test_timed_out_job_cannot_be_overwritten_by_late_worker`. Repair its completion observation while retaining the same timeout-versus-late-worker sequence and exact test name.
- No arbitrary sleeps or polling for a status to leave `failed`; correct terminal custody makes that condition impossible. Bounded waits may observe only explicit `threading.Event`, `threading.Barrier`, or thread-join completion signals.
- No secrets, protected or production data, payload dumps, cloud resources, production databases, external providers, registries, or unapproved network access.
- Tests use synthetic requests, local in-memory state, monkeypatched workflow functions, local thread primitives, and deterministic fake artifacts/results. No sidecar server, Docker container, ToolUniverse provider call, HTTP listener, or external connection is needed.
- Do not stage, commit, push, merge, open a pull request, deploy, publish, release, or clean any diagnostic branch until the relevant authority is explicit. The builder may create the one local candidate commit requested by the coordinator only after verification; reviewers remain read-only.

If any required change falls outside AF-P05, stop for a charter/spec amendment. Do not substitute a nearby file.

## Verified existing behavior on the pinned base

- `ResearchJobStatusCode` defines four active states: `queued`, `resolving_identity`, `gathering_evidence`, and `normalizing`; and six terminal states: `pending_review`, `completed`, `failed`, `cancelled`, `partial`, and `rejected_by_policy`.
- `InMemoryJobStore.update` holds an `RLock`, applies every supplied field directly, and always refreshes `updated_at_utc`. It has no terminal-state guard, so a later worker update can overwrite an earlier terminal record despite the lock.
- `InMemoryJobStore.request_cancel` also holds the same `RLock`. It marks active records `cancelled`, but it sets `cancel_requested` and refreshes `updated_at_utc` even when the record is already terminal. Therefore a later cancellation request mutates a terminal snapshot on the pinned base.
- `JobRunner` submits `execute_research_job` to a worker executor and waits from a separate waiter thread. On timeout it calls `request_cancel`, then `_mark_timeout`, which stores a `failed` artifact with `error_code="execution_timeout"`. The still-running executor may later write its own terminal result through `store.update`.
- The diagnostic test deterministically blocks the workflow sequence until the runner stores the timeout failure, then releases the worker. Its current `_wait_until(status != FAILED)` is an observation of the bug: after the production fix, that wait can never succeed, so the harness would time out before asserting custody.
- The current store returns the existing in-memory `JobRecord` object. Current production writers use the synchronized `update` and `request_cancel` boundaries; no pinned production caller directly assigns lifecycle fields outside the store.
- The pinned sidecar has five adjacent test files and 39 collected cases: `test_executor_status.py` (4), `test_health_and_jobs.py` (15), `test_inference_policy.py` (4), `test_tooluniverse_allowlist.py` (11), and `test_workflow_sequences.py` (5). The grounded baseline run is 38 passed and 1 expected skip (`test_legacy_config_copy_has_not_drifted`).
- The target reproduction path is absent on the pinned base, so its base count is zero. The diagnostic snapshot contains one target case.
- A pinned-tree `uv run --offline ruff check .` currently reports 37 pre-existing findings. AF-P05's `store.py` accounts for five pre-existing `UP017` findings and `runner.py` has three pre-existing findings (`I001` and two `UP017`), so the three-file AF baseline is eight findings. The candidate must introduce no new lint finding and must not widen scope to clean ambient debt.

## Exact amended D8 contract

The ratified decision is controlling:

> Active states are `queued`, `resolving_identity`, `gathering_evidence`, and `normalizing`. Terminal states are `pending_review`, `completed`, `failed`, `cancelled`, `partial`, and `rejected_by_policy`. The first terminal transition wins atomically at the store boundary. Once terminal, later worker updates return the existing record unchanged: they cannot change status, `finished_at_utc`, progress, partial/error fields, artifact, tools, or `updated_at_utc`; an identical replay is an idempotent no-op. A later cancellation request may not alter the terminal snapshot. `maximum_source_count` is enforced in the executor before accepted results, claims, provenance, and tool counts are materialized. P05 precedes final P06 integration verification.

P05 binds the terminal-custody half of that decision as follows:

1. The active set is exactly `{queued, resolving_identity, gathering_evidence, normalizing}`. Updates to active records continue to work; the fix may not freeze a newly created job or prevent normal active-state progress.
2. The terminal set is exactly `{pending_review, completed, failed, cancelled, partial, rejected_by_policy}`. No current terminal status is omitted, and no active status is silently treated as terminal.
3. An active-to-terminal `update` or active `request_cancel` acquires the store lock and installs one complete first terminal snapshot. Competing terminal writers are serialized under the same boundary; the first lock winner owns the record.
4. Once a record is terminal, every later `update` returns the already stored record without applying supplied fields and without refreshing `updated_at_utc`. This includes a status-only write, a progress-only write, an identical terminal replay, and a hostile write carrying a different terminal status, finish time, progress, partial/error fields, artifact, and tool list.
5. An identical terminal replay is a true idempotent no-op. It does not refresh timestamps or replace equal-looking mutable fields.
6. Once a record is terminal, `request_cancel` returns that record without changing `cancel_requested`, status, timestamps, progress, partial/error fields, artifact, or tools. Cancellation may be the first terminal writer only while the record is active.
7. Terminal custody is snapshot-wide and never field-by-field. A concurrent race may finish with either complete contender snapshot according to lock order, but never a mixed snapshot and never the later contender's overwrite of the first.
8. Missing-job behavior remains unchanged (`None`). Unknown-field validation for an active record remains unchanged (`AttributeError`). TTL behavior and the four-active-state expiry exclusion remain unchanged.
9. Except for the ratified atomic timeout write in `JobRunner`, the change is store-local. The builder does not alter other runner behavior, workflow execution, status contracts, API serialization, or source-cap behavior.
10. On timeout, `JobRunner` must not first call the user-cancellation transition. Its first terminal write atomically carries `failed`, `cancel_requested=True`, `error_code="execution_timeout"`, the existing timeout error/progress text, timeout artifact, and one finish timestamp. If user cancellation wins while active, `cancelled` remains the immutable first terminal snapshot.

If the builder believes a seventh terminal status, a different cancellation policy, a copy-on-read contract, another runner change, or any executor change is required, stop for a narrow decision rather than widening D8.

## Required regression harness

The following is the complete required harness under ratified Amendment 03 after fresh Step 0 confirmation.

Preserve the class/module and exact original test name:

- `test_timed_out_job_cannot_be_overwritten_by_late_worker`

Repair and extend `test_runner_terminal_state_reproduction.py` with these exact scenario groups:

1. **Preserved synchronized timeout race — 1 case.** Retain the synthetic request, one-second runner timeout, blocked local workflow sequence, `execution_timeout` failure observation, and late-worker release. Before constructing `JobRunner`, monkeypatch the `biostack_research_sidecar.jobs.runner.execute_research_job` binding with a wrapper that sets a dedicated `worker_completed` event in `finally`. After the timeout terminal snapshot is observed, capture every governed field, release the sequence, wait directly on `worker_completed`, and then assert the record still exactly equals the captured timeout snapshot. Do not wait for a forbidden status overwrite. Always release the sequence and call `runner.shutdown(wait=True)` in `finally`.
2. **Terminal custody theory — 7 cases.** Parameterize exact semantic rows for timeout failure (`failed` + `execution_timeout`), non-timeout failure (`failed` + `worker_error`), cancellation, partial, policy rejection, pending review, and completion. Each row starts active, installs its first terminal snapshot through `update` except that the cancellation row uses `request_cancel`, captures a deep immutable comparison value for every `JobRecord` field, performs an identical replay, performs a progress-only late update, performs a hostile different-terminal update with different finish time/progress/partial/error/artifact/tools, and performs a later `request_cancel`. After each operation, assert the entire record remains equal to the first captured snapshot, including `cancel_requested` and `updated_at_utc`.
3. **Active-state positive control — 1 case.** Drive one newly created record through the exact active statuses `queued`, `resolving_identity`, `gathering_evidence`, and `normalizing`, with unique progress values, and assert each accepted update is visible. This prevents an overbroad fix that freezes all store updates.
4. **Concurrent first-terminal-writer race — 1 case.** Use a `threading.Barrier` to release two local writer threads simultaneously against one active record. Give each contender a complete, uniquely identifiable terminal payload. Join both threads with bounded waits, assert neither raised, and assert the final whole-record lifecycle snapshot equals exactly contender A or contender B. It may reflect either lock winner, but it must not contain mixed fields and must remain unchanged after a later cancellation request.

The final target census is 10 discovered/passed cases: 1 preserved timeout race, 7 terminal-custody rows, 1 active-state positive control, and 1 simultaneous terminal-writer race. This is +10 cases relative to the pinned base, where the target file is absent, and +9 cases relative to the one-case diagnostic snapshot.

Use deep value snapshots for assertions; do not retain only another alias to the mutable `JobRecord`. All thread/event/barrier waits and joins must be bounded and must fail with explicit diagnostics. No assertion may depend on wall-clock ordering beyond the runner's existing one-second timeout contract and explicit synchronization events.

## Smallest production change

The smallest complete production change is:

1. the store-local terminal guard above; and
2. in `runner.py`, replace the timeout's cancel-then-fail pair with one `store.update` that atomically includes `cancel_requested=True` and every timeout terminal field already produced by `_mark_timeout`.

Keep the existing lock as the atomicity boundary. Do not introduce another store, scheduler, queue, process, persistence layer, dependency, status value, public signature, or unrelated reformat. No implementation begins before fresh Step 0 confirmation.

## Security gates

### SG-SCOPE

- Exact AF-P05 only; synthetic fixtures and local in-memory concurrency only.
- No secrets, protected data, payload dumps, production/cloud/database access, external providers, registries, Docker, or external network.
- Verify base/tree ancestry, exact diff scope, `git diff --check`, and final clean status.
- Under current authority, any required runner edit stops the parcel; after a narrow amendment, only the exact ratified runner change is permitted. Any executor, model, config, package, lockfile, adjacent-test, P06, or other unlisted edit always stops the parcel.

### SG-SIDECAR

- The exact active and terminal memberships match amended D8 and cannot drift through an incomplete guard.
- First-terminal-writer custody is atomic under the existing store lock. Review for check-then-act gaps, lock escape, mixed terminal snapshots, and timestamp refresh after terminalization.
- If the recommended amendment is ratified, the timeout path performs one complete `failed` / `execution_timeout` terminal write with `cancel_requested=True`; review must reject any remaining cancel-then-fail sequence or special-case terminal overwrite.
- Timeout failure, ordinary failure, cancellation, partial, policy rejection, pending review, and completion each retain the first whole terminal snapshot.
- Identical replay, progress-only late update, conflicting terminal update, and later cancellation are all no-ops after terminalization.
- Active updates and first active cancellation remain functional; the fix cannot produce a fail-closed-looking dead job by ignoring all writes.
- The repaired timeout race observes actual worker-function completion through an explicit event. It does not wait for, induce, or assume the forbidden overwrite.
- Thread waits are bounded and cleanup always releases the fake worker. Review for leaked threads, executor shutdown hangs, deadlocks, and false-green assertions caused by shared mutable aliases.
- Tests do not call ToolUniverse or any provider. All workflow results/artifacts are synthetic and local.

## Deterministic verification and count receipts

These commands/counts are grounded on the pinned tree and define the verification chain under ratified Amendment 03 after Step 0 confirmation.

Run commands from the P05 worktree's `backend/research-sidecar` directory in PowerShell. `rtk` is preferred when available; if unavailable, record that once and run the exact underlying commands. Do not restore from or contact a registry. `UV_OFFLINE=1` and `uv run --offline` are mandatory. If the pinned lock/cache cannot create the external environment offline, stop as `ENVIRONMENT_BLOCKED`; do not remove `--offline`.

Keep the uv environment and receipts outside the repository so test setup does not create an in-worktree `.venv`:

```powershell
$p05ReceiptDir = Join-Path ([System.IO.Path]::GetTempPath()) 'biostack-p05-receipts'
$p05UvEnv = Join-Path ([System.IO.Path]::GetTempPath()) 'biostack-p05-uv-env'
New-Item -ItemType Directory -Force -Path $p05ReceiptDir | Out-Null
$env:UV_PROJECT_ENVIRONMENT = $p05UvEnv
$env:UV_OFFLINE = '1'
```

Before importing the diagnostic test, record the base census and adjacent receipt:

```powershell
Test-Path -LiteralPath 'tests/test_runner_terminal_state_reproduction.py'
uv run --offline pytest --collect-only -q
uv run --offline pytest tests/test_executor_status.py tests/test_health_and_jobs.py tests/test_inference_policy.py tests/test_tooluniverse_allowlist.py tests/test_workflow_sequences.py -q -rs --junitxml "$p05ReceiptDir/p05-base-adjacent.xml"
uv run --offline ruff check src/biostack_research_sidecar/jobs/store.py src/biostack_research_sidecar/jobs/runner.py --output-format concise
```

Expected pinned-base facts are: target path absent; full collection 39; adjacent execution 38 passed and 1 expected skip, with zero failures/errors; and exactly eight existing findings across the production files (five `UP017` in `store.py`; one `I001` and two `UP017` in `runner.py`). A mismatch stops edits for coordinator reconciliation. The nonzero base ruff exit is ambient pinned-tree evidence, not permission to clean it.

After implementation, run the exact target and adjacent commands:

```powershell
uv run --offline pytest tests/test_runner_terminal_state_reproduction.py -q -rs --junitxml "$p05ReceiptDir/p05-new-target.xml"
uv run --offline pytest tests/test_executor_status.py tests/test_health_and_jobs.py tests/test_inference_policy.py tests/test_tooluniverse_allowlist.py tests/test_workflow_sequences.py -q -rs --junitxml "$p05ReceiptDir/p05-new-adjacent.xml"
```

Expected final counts are target 10 passed with zero failed/skipped/errors, and adjacent 38 passed plus the same 1 expected skip with zero failed/errors. Then run the aggregate sidecar suite:

```powershell
uv run --offline pytest -q -rs --junitxml "$p05ReceiptDir/p05-new-sidecar.xml"
```

The final sidecar receipt must report 49 collected/executed cases, 48 passed, the same 1 expected skip, and zero failed/errors: exactly +10 collected/executed/passed relative to the 39-case pinned baseline. Read exact JUnit counts and timestamps from the receipt and preserve the console exit code; do not rely on source counting or an intermediate progress line.

Run the AF-P05 lint-delta and scope checks:

```powershell
uv run --offline ruff check src/biostack_research_sidecar/jobs/store.py src/biostack_research_sidecar/jobs/runner.py tests/test_runner_terminal_state_reproduction.py --output-format concise
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git rev-parse '339f259b1a467034db4f57cf9d774c292f11b53a^{tree}'
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git status --short --branch --untracked-files=all
```

The final AF ruff output may contain only the same eight pinned findings: five `UP017` in `store.py`, one `I001` and two `UP017` in `runner.py`; the new test must add zero findings and no finding type/path/count may increase. Full-repository ruff remains a coordinator aggregate baseline check and is not authority for P05 to edit other files. The name-only output must equal the ratified AF-P05 exactly, base ancestry must succeed, the base tree receipt must equal `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`, and final status must have no tracked or untracked residue after the candidate commit. Record that all tests used offline package resolution, synthetic local values, in-process fakes, and no external provider/network/cloud/production-data path.

## Evidence package

The builder's completion evidence must include:

- exact candidate commit, parent commit, verified base ancestry, and pinned base-tree receipt;
- `git diff --stat`, `git diff --check`, and the exact AF-P05 name-only list;
- diagnostic source commit/blob and confirmation that the diagnostic branch remains preserved and unmerged;
- preserved original test name and a concise explanation of the worker-completion repair;
- base collection/adjacent and final target/adjacent/aggregate JUnit paths, exact commands, exit codes, counts, skips, and timestamps;
- explicit results for all seven terminal semantic rows, active-state progression, simultaneous writer race, identical replay, progress-only late write, conflicting terminal write, later cancellation, and complete-snapshot comparison;
- AF-P05 ruff baseline/final delta and the known full-tree baseline note;
- explicit offline/no-network/no-provider/no-production-data receipt;
- changed-file summary, smallest-fix rationale, residual risk, and any environment limitation;
- final `git status --short --branch --untracked-files=all` and a session handoff.

Builder tests are necessary but not sufficient. The coordinator must inspect the exact candidate diff, complete terminal-state membership, race harness, and receipts before review dispatch.

## Review plan

After an amendment, confirmed Step 0, and a green candidate commit, dispatch three fresh, read-only reviewers against that exact commit:

1. Adversarial reviewer A: amended D8 semantic custody, exact active/terminal membership, atomic timeout write ordering, first-writer atomicity, immutable whole snapshot, active positive control, smallest-change discipline, and adjacent regression risk.
2. Adversarial reviewer B: preservation/repair of the diagnostic timeout race, worker-completion signal validity, seven-row state coverage, concurrent-race determinism, deep-snapshot assertions, count receipts, false-green opportunities, and AF-P05 scope.
3. Separate defensive security reviewer: SG-SIDECAR and SG-SCOPE, timeout cancel-then-fail ordering, check-then-act races, cancellation/timeout provenance overwrite, mixed-field snapshots, mutable-alias false greens, deadlock/thread leakage, lifecycle bypasses, information leakage, network/provider boundaries, and any path that can alter a terminal record after the first writer.

Reviewers do not edit, fix, stage, or commit. Every finding is dispositioned as fix, accept-as-documented, or informational. Any rework produces a new candidate commit and invalidates all earlier acceptances; rerun verification and obtain three fresh reviews. Security-review failure blocks Gate 3 unless the human explicitly names and accepts the residual risk.

## P05-before-P06 custody and collision risk

- P05 owns `store.py`; P06 owns `workflows/executor.py`. Their exact production files are disjoint, but both participate in D8 and share the sidecar aggregate suite.
- The P05 builder completes one candidate and the coordinator verifies its diff/evidence before P06 shaping or dispatch proceeds. That exact P05 candidate hash becomes a pinned input to the P06 spec and handoff; P06 does not silently reimplement, amend, or supersede terminal custody.
- Both P05 adversarial reviewers and the separate SG-SIDECAR/SG-SCOPE reviewer must accept the exact P05 candidate before final combined P06 integration verification. Review may run while P06 is being shaped or built, but an unaccepted P05 hash cannot serve as final D8 integration evidence.
- Final P06 integration verification occurs in coordinator-controlled combined-candidate custody with the exact accepted P05 candidate applied before the exact P06 candidate. It reruns P05's target, P06's target, the five adjacent suites, and the aggregate offline sidecar suite. A plain P06-from-base green run is not final D8 integration evidence.
- The Gate 3 merge order must place P05 before P06. Any rework to P05 invalidates the P05 reviews and the combined P06 verification; both must be repeated against the new exact hashes.
- At shaping time, `codex/biostack-remediation-p05` and `C:\Users\clint\.codex\worktrees\biostack-remediation-p05\BioStack` do not exist. If `origin/main` moves, another owner changes AF-P05, or the branch/worktree appears before dispatch, stop for coordinator reconciliation. Do not rebase or combine work silently.
- P05 remains local and unmerged until the complete goal chain and exact human Gate 3 are complete.

## Step 0 - restate and stop

The builder must send one fresh restatement containing all of the following, then stop for explicit confirmation:

1. goal, initiative, P05 identifier, Wave 1, critical-lifecycle risk, and P05-before-P06 dependency;
2. branch `codex/biostack-remediation-p05`, worktree `C:\Users\clint\.codex\worktrees\biostack-remediation-p05\BioStack`, base `339f259b1a467034db4f57cf9d774c292f11b53a`, and tree `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`;
3. the three ratified exact AF-P05 paths and the rule that every other file is forbidden;
4. the exact four active states, six terminal states, first-terminal-writer atomicity, whole immutable terminal snapshot, identical replay, later cancellation, active-update positive requirements, and the atomic timeout snapshot that does not first call user cancellation;
5. the preserved test name, repaired worker-completion event, seven terminal semantic rows, active-state and concurrent-writer controls, exact target 10 / adjacent 39 / aggregate 49 counts, expected one adjacent skip, and exact commands;
6. SG-SCOPE, SG-SIDECAR, synthetic/offline/no-provider/no-network limits, two adversarial plus separate security review, Gate 3 prohibition, and every stop rule.

Silence is not confirmation. Any discrepancy in the restatement blocks edits.

## Handoff contract

The builder hands the coordinator one concise record containing:

- `P05`, branch, worktree, base/tree, candidate commit, and parent commit;
- exact changed files and confirmation they equal AF-P05;
- concise production/test changes and preservation of `test_timed_out_job_cannot_be_overwritten_by_late_worker`;
- exact command/exit-code/count receipt table and JUnit paths;
- scope/ancestry/status outputs, lint-delta receipt, and offline/no-network receipt;
- exact P05 candidate hash to pin into P06 custody;
- open findings, residual risks, or a precise `none` statement;
- explicit statements: local candidate only, not pushed, no PR, not merged, not deployed/released, Gate 3 ungranted, diagnostic branches untouched;
- reviewer-ready diff command and evidence paths needed by the two adversarial reviewers and separate security reviewer.

The coordinator verifies the handoff against Git and disk. A handoff claim is not acceptance by itself.

## Stop rules

Stop the parcel and report without widening scope if any of the following occurs:

- `origin/main` differs materially from the pinned base/tree or base ancestry fails;
- a named AF-P05 file is absent/insufficient, or a required change touches any other file;
- Step 0 is not explicitly confirmed;
- the active/terminal membership, cancellation semantics, whole-snapshot behavior, or idempotent replay contract needs a new decision;
- the invariant can pass only by weakening/deleting the preserved race, waiting for a forbidden overwrite, using sleeps instead of completion signals, freezing active updates, or changing runner behavior beyond the ratified timeout write, executor behavior, or model behavior;
- offline dependencies are unavailable, a test needs a provider/network/cloud/production/protected-data path, or an external connection is attempted;
- target/adjacent/aggregate counts, expected skip, or lint delta differ from this grounded contract without a reconciled source explanation;
- a thread does not complete within its bounded wait, cleanup cannot shut down the runner, or the race harness leaks work;
- a security finding cannot close inside AF-P05;
- the same tripwire or false-closure condition fires twice;
- P06 shaping/dispatch is proposed before the coordinator-verified P05 candidate is pinned, or final combined verification is proposed before that exact P05 candidate has all required review acceptances;
- any push, PR, merge, deployment, publication, release, or diagnostic-branch mutation is proposed before exact human Gate 3;
- the builder or a reviewer is asked to edit outside its authorized role.

BioStack does not contain the Foreman emitter/linter required for a `ShapingResult`. This spec intentionally creates no `ShapingResult` JSON. Dispatch remains contingent on coordinator lint, the clean isolated worktree, unchanged pinned base/tree, and confirmed Step 0.
