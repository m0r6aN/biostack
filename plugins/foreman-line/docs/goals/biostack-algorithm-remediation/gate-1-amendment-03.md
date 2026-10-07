# Narrow Gate 1 Amendment 03 — Sidecar Timeout Terminalization

**Status:** RATIFIED 2026-09-02 — coordinator lint passed

**Scope:** amended D8 timeout semantics and replacement AF-P05 only

**Trigger:** P05 shaping against pinned base `339f259b1a467034db4f57cf9d774c292f11b53a` found that `JobRunner` calls `request_cancel()` before `_mark_timeout()`. The first call installs terminal `cancelled`; ratified first-terminal-writer custody then correctly forbids the later `failed` / `execution_timeout` snapshot. The retained timeout regression therefore cannot be preserved inside the current two-file AF-P05.

Amendments 01 and 02 remain ratified. All unrelated parcel decisions and contingent Gate 2 authorizations remain unchanged. Gate 3 remains ungranted.

## D8 timeout-terminalization supplement

Add this controlling clarification to ratified D8:

> **D8 timeout terminalization.** A runner-observed execution timeout is not a user-cancellation transition. The timeout path must install one complete first terminal snapshot atomically through the job store: `status=failed`, `cancel_requested=true`, `error_code=execution_timeout`, the existing timeout error/progress text, timeout artifact, and one finish timestamp. It must not first call the user-cancellation transition. The store's first-terminal-writer rule then preserves that complete timeout snapshot against every late worker update, identical replay, and later cancellation request. A user-requested cancellation that wins while the record is active remains terminal `cancelled` and is likewise immutable.

This supplement preserves the existing timeout classification and error provenance. It does not add a status, change the timeout duration, alter the public cancellation endpoint, or permit a later terminal writer to replace an earlier one.

## Production ownership

- Atomic lifecycle boundary: `InMemoryJobStore.update` and `request_cancel` in `jobs/store.py`.
- Timeout transition owner: `JobRunner` in `jobs/runner.py`.
- Regression owner: `test_runner_terminal_state_reproduction.py`.

## Replacement Exact Allowed Files — AF-P05

Replace AF-P05 with exactly these three paths:

1. `backend/research-sidecar/src/biostack_research_sidecar/jobs/store.py`
2. `backend/research-sidecar/src/biostack_research_sidecar/jobs/runner.py`
3. `backend/research-sidecar/tests/test_runner_terminal_state_reproduction.py`

Every other path remains forbidden, including models/contracts, routes, executor/source-cap files, configuration, packages/locks, existing adjacent tests, and all P06 files. If these three files are insufficient, P05 stops for another narrow amendment.

## Smallest authorized production change

1. In `store.py`, enforce the exact active/terminal sets and return an already-terminal record unchanged before applying any update field or refreshing `updated_at_utc`; a later `request_cancel` is also a complete no-op.
2. In `runner.py`, remove the timeout path's preceding `request_cancel()` and make the existing timeout store update include `cancel_requested=True` with the existing complete `failed` / `execution_timeout` snapshot.
3. Retain public signatures, status values, timeout settings, ordinary internal-failure behavior, and user-cancellation semantics. No refactor or unrelated cleanup is authorized.

## Required verification and security gates

- Preserve exact test `test_timed_out_job_cannot_be_overwritten_by_late_worker`, replacing its forbidden-overwrite wait with a bounded explicit worker-completion event.
- Final P05 target is exactly 10 passing cases: the retained timeout race, seven terminal semantic rows, one active-state positive control, and one concurrent first-terminal-writer race.
- Adjacent sidecar receipt remains 38 passed plus the same one existing skip; aggregate becomes 48 passed plus that skip, 49 total.
- The timeout case proves the first terminal snapshot is `failed` / `execution_timeout` with `cancel_requested=True`; the user-cancellation row proves `cancelled`; every terminal row proves immutable whole-snapshot custody, identical replay, hostile late write, and later-cancellation no-ops.
- `UV_OFFLINE=1`, local synthetic data, bounded thread primitives, exact AF-P05 diff, base ancestry, lint-delta, clean status, and no registry/provider/network/cloud/production/protected-data access are mandatory.
- The exact candidate requires two fresh independent adversarial reviews plus a separate SG-SIDECAR/SG-SCOPE security review. Any rework invalidates prior reviews.
- P06 remains blocked until the coordinator verifies the final P05 candidate; final P06 integration verification must include that exact accepted P05 behavior.

## Gate 2 and Gate 3 boundary

Ratification reactivates only P05's existing contingent Gate 2 authority after coordinator re-lint, a clean isolated base-rooted worktree, exact replacement AF-P05, and confirmed Step 0. It does not authorize P06 dispatch before the final P05 candidate, or any push, PR, merge, deployment, publication, release, external access, or diagnostic-branch mutation.

Gate 3 remains human-only and ungranted.

## Exact ratification form

> Narrow Gate 1 Amendment 03: I ratify the D8 timeout-terminalization supplement and replace AF-P05 with the three files listed, including `jobs/runner.py`. P05 retains contingent Gate 2 authorization subject to its shaped spec and Step 0; P06 remains downstream of the verified P05 candidate. All other decisions and authorizations remain unchanged. Gate 3 remains ungranted.

## Human receipt

The developer supplied the exact ratification form above on 2026-09-02. The D8 timeout-terminalization supplement and replacement three-file AF-P05 are active; contingent Gate 2 authority for P05 is reactivated subject to its shaped spec and Step 0. P06 remains downstream of the verified P05 candidate. Gate 3 remains ungranted.
