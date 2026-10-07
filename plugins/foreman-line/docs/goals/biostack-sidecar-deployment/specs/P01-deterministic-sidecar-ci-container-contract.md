# Parcel: P01-deterministic-sidecar-ci-container-contract

## Goal

Produce deterministic sidecar CI and a locally executable production-container contract that proves the image is non-root, fail-closed, provider-SDK-free, and incapable of exposing FastAPI documentation routes.

## Initiative

`biostack-sidecar-deployment`

## Project Track

Research sidecar / CI / production image

## Wave

W1

## Branch

`parcel/sidecar-P01`

## Worktree

`D:\Repos\BioStack-sidecar-P01`

## Dependencies

- None. Branch from goal commit containing this approved spec.

## Integration Surfaces

- IS-IMAGE: source and lockfile -> immutable production container
- IS-CI: sidecar source -> GitHub pull-request verification
- IS-AUTH-DATA: local HTTP probe -> service-auth and public-scientific-only boundary

## Security Gate

SG-IMAGE, SG-DATA, SG-OUTBOUND, and SG-EVIDENCE. Two independent adversarial reviews and one separate defensive security review are required before coordinator acceptance.

## Allowed Files

- `.github/workflows/research-sidecar-ci.yml`
- `scripts/verify-research-sidecar-container.mjs`
- `scripts/verify-research-sidecar-container.test.mjs`
- `backend/research-sidecar/docs/PARCELS.md`
- `backend/research-sidecar/Dockerfile`
- `backend/research-sidecar/.dockerignore`
- `backend/research-sidecar/pyproject.toml`
- `backend/research-sidecar/uv.lock`
- `backend/research-sidecar/src/biostack_research_sidecar/app.py`

If any other file is needed, stop and request a spec amendment before editing or creating it.

## Forbidden

- Do not push, open a PR, merge, or edit `main`.
- Do not call Azure, mutate GitHub settings, create or rotate a secret, enable a provider, call a scientific/model provider, use protected data, expose public ingress, or change sidecar/API production configuration.
- Do not edit any file outside Allowed Files, including existing sidecar tests.
- In `app.py`, make only the ratified FastAPI-constructor change that disables `/docs`, `/redoc`, and `/openapi.json`; no route, exception, auth, privacy, job, or response behavior may otherwise change.
- Do not delete the ToolUniverse optional dependency or its locked graph. This parcel excludes it from the deployed image; it does not remove the separately governed future capability from source.
- Do not add, upgrade, or relax dependencies. If a lockfile change is necessary for anything other than a mechanically derived, dependency-neutral no-extra build contract, stop.
- Do not use the unsafe empty form `--extra ${TOOLUNIVERSE_EXTRA}`. An empty shell value would make the next option become the extra name.
- Do not claim network egress prevention from a configuration-only or package-census check. P01 proves absence of provider SDKs and disabled runtime flags, not a network-policy control.
- Do not persist the synthetic local service token in git, logs, receipts, workflow artifacts, or command output.

## Out of Scope

Azure topology, managed identity, ACR role assignment, production deployment, service-token creation/storage, API routing, P02-P04 work, ToolUniverse/provider enablement, local or hosted inference, GPU operation, persistent job state, real source-locator promotion, EvidenceGate changes, and any live Gate 3 scenario.

## Existing Patterns To Follow

- `.github/workflows/deploy.yml` — Python 3.12 sidecar dependency audit and read-only workflow permissions; do not inherit its automatic production-deploy behavior.
- `.github/workflows/secret-scan.yml` — repository secret-scan remains separate; P01 does not edit it.
- `scripts/verify-containerapp-deployment.mjs` and `.test.mjs` — ESM CLI, exported pure evaluators, fail-closed argument/state validation, Node built-in test runner.
- `backend/research-sidecar/tests/test_health_and_jobs.py` — canonical auth, privacy, workflow, and global-kill behavior.
- `backend/research-sidecar/tests/test_runner_terminal_state_reproduction.py` and `test_request_constraint_reproduction.py` — retained P05/P06 regression identities; P01 must run, not rewrite, them.

## Contract

### Step 0 — restate and stop

Before editing, the builder must report all of the following and stop for coordinator approval:

1. Branch, worktree, `HEAD`, goal-base commit, and a clean `git status`.
2. The nine exact Allowed Files and confirmation that three new files are absent before work: `.github/workflows/research-sidecar-ci.yml`, `scripts/verify-research-sidecar-container.mjs`, and `scripts/verify-research-sidecar-container.test.mjs`.
3. D1, D6, D8, D11, D12.7-D12.9, D18, and D19 in the builder's own words.
4. The no-push/no-PR/no-merge/no-Azure/no-production-mutation/no-provider-call boundary.
5. The existing full-suite tripwire: at least 53 passing tests, exactly one expected legacy-config skip, zero failures.
6. The intended no-extra Dockerfile mechanism and why an empty value passed to `--extra` is invalid.
7. How the base-image OCI digest will be verified. A registry metadata read or image pull used solely to resolve/build the declared base is ordinary dependency resolution for this local parcel; any scientific/model/provider call is forbidden. If a verified digest cannot be resolved, stop rather than guessing.
8. Any missing decision, file, dependency, or unverifiable acceptance criterion. A flag that changes a ratified decision stops the parcel.

No implementation begins until the coordinator rules Step 0 green.

### Production image contract

- `Dockerfile` pins `ghcr.io/astral-sh/uv:python3.12-bookworm-slim` to a verified OCI digest using `tag@sha256:...`; the builder records the resolution command and digest in the handoff.
- The default production build omits the `tooluniverse` extra through explicit conditional/no-extra build logic. It never relies on an empty argument following `--extra`.
- The optional ToolUniverse source/lock capability remains intact for separately governed future builds.
- The final image runs as a non-root user, exposes container port 8080, and starts `python -m biostack_research_sidecar`.
- `/app/.venv` contains none of these distributions: `tooluniverse`, `openai`, `google-genai`, `huggingface-hub`, `pip`, `setuptools`. The check uses Python package metadata and fails closed on malformed output.
- The build context excludes `.env`, `.env.*`, `tests/`, `.git`, virtual environments, caches, local artifacts, and documentation not required at runtime. The built image contains no `/app/.env`, `/app/tests`, or `.git` path.
- The production FastAPI app returns 404 for `/docs`, `/redoc`, and `/openapi.json`.

### Local dark runtime contract

The verifier starts the candidate image with a random Docker name, publishes only `127.0.0.1` on an ephemeral host port, uses a clearly synthetic process-only token, and sets every dark value explicitly:

- `BIOSTACK_RESEARCH_HOST=0.0.0.0`
- `BIOSTACK_RESEARCH_SERVICE_TOKEN=<synthetic process-only value>`
- `BIOSTACK_RESEARCH_ALLOW_INSECURE_DEV_AUTH=false`
- `BIOSTACK_RESEARCH_GLOBAL_KILL_SWITCH=true`
- `BIOSTACK_RESEARCH_TOOLUNIVERSE_ENABLED=false`
- `BIOSTACK_RESEARCH_HOSTED_FALLBACK_ENABLED=false`
- `BIOSTACK_RESEARCH_LOCAL_INFERENCE_ENABLED=false`
- `BIOSTACK_RESEARCH_GPU_ENABLED=false`
- `BIOSTACK_RESEARCH_MAX_CONCURRENT_RESEARCH_JOBS=1`

The verifier must prove:

- `/health` becomes reachable within a bounded timeout and reports `status=disabled`, kill switch true, ToolUniverse false, and concurrency one.
- A protected endpoint denies a missing token and a wrong token; the correct synthetic token succeeds without invoking a provider.
- A protected/private-shaped payload is rejected before execution.
- An arbitrary/non-allowlisted workflow is rejected before execution.
- With the global kill switch true, an otherwise valid public-scientific request returns 202 and then reaches terminal `rejected_by_policy` with the global-kill reason. The verifier must not misstate the kill switch as pre-admission denial.
- Container logs and verifier output contain neither the token nor submitted request values.
- The container is always removed in a `finally` cleanup path. Cleanup is limited to the exact random container name created by the verifier.

### Verifier interface

`scripts/verify-research-sidecar-container.mjs` must expose testable pure functions for parsing/evaluating contract observations and a CLI that accepts at minimum `--image <local-tag>`. Host port and container name are generated safely unless explicitly supplied for tests. Unknown, duplicate, missing, or malformed options fail closed. Child processes use argument arrays (`execFile`/`spawn`), never string-built shell commands. External/process errors are caught at the CLI boundary and returned as concise redacted failures.

### CI contract

`.github/workflows/research-sidecar-ci.yml`:

- runs on pull requests and pushes affecting the sidecar, its verifier, or the workflow itself;
- has `contents: read` and no `id-token: write` or deployment permission;
- uses Python 3.12 and a documented `uv` version/install path;
- runs frozen dependency synchronization/checks, the complete sidecar pytest suite, a strict third-party dependency audit, Node verifier tests, a production no-extra Docker build, and the container-contract verifier;
- performs no Azure login, registry push, deployment, provider enablement, or scientific/model/provider request;
- applies bounded timeouts and preserves useful failure output without printing environment variables or the synthetic token.

### Parcel index contract

Update `backend/research-sidecar/docs/PARCELS.md` only to mark `p11-contract-tests-ci` complete and point to the workflow/verifier after every required local check is green. Do not change unrelated parcel statuses or human gates.

## Required Tests

- `scripts/verify-research-sidecar-container.test.mjs` covers every argument-validation branch and every pure contract evaluator, including mutation tests that make each named invariant fail independently.
- The Node tests exercise hostile names/paths/output values and prove no shell construction, log injection, or cleanup overreach.
- The full existing sidecar suite runs unchanged, including the exact P05/P06 reproduction files.
- The production image is built locally without the ToolUniverse extra and is probed by the verifier.
- No live Azure or provider test is added or run.

## Acceptance Criteria

- Only Allowed Files differ from the approved goal base.
- The full sidecar suite reports at least 53 passed, exactly 1 expected skip, and 0 failed; fewer passes, an additional skip, or any failure stops acceptance.
- A local Ruff census proves P01 introduces no new finding in `app.py`. Pre-existing findings are counted and disclosed rather than silently converted into this parcel's scope; zero full-repository Ruff findings is not a P01 gate.
- Frozen dependency and strict dependency-audit commands pass, with exact commands and versions recorded.
- Node verifier tests pass and each named invariant is proven to fail when its observation/fixture is mutated in that dimension.
- The no-extra production image builds from a digest-pinned base, runs as non-root, has the exact command/port, excludes forbidden build-context content, and lacks every D19 distribution.
- Local dark health/auth/privacy/workflow/kill-switch probes pass with bounded waits and no external provider execution.
- `/docs`, `/redoc`, and `/openapi.json` each return 404 in the production container.
- Captured logs/output contain no synthetic token or request-content echo.
- `git diff --check` passes; no secret-like value is introduced; reviewer worktrees remain clean.
- SC-01 through SC-04 are evidenced locally. SC-05 through SC-10 remain explicitly `DEFERRED-TO-GATE-3`; P01 makes no live-environment claim.
- Two independent adversarial reviews and one defensive security review have no unresolved blocking finding.

## Verification

Run from the parcel worktree unless a working directory is stated:

1. `git status --short` and `git diff --name-only <approved-goal-base>...HEAD` — only Allowed Files.
2. `git diff --check` — no whitespace errors.
3. In `backend/research-sidecar`: `uv sync --frozen --extra dev`, then `.\.venv\Scripts\python.exe -m pytest tests -q` — at least 53 passed, exactly 1 skipped, 0 failed.
4. In `backend/research-sidecar`, record Ruff findings for `app.py` both at the approved goal base and at `HEAD`; compare rule/message counts and inspect shifted locators — P01 adds no finding. A nonzero pre-existing census does not authorize unrelated cleanup.
5. In `backend/research-sidecar`: export the frozen default/no-extra third-party dependency set and run `pip-audit --strict` against it; record tool versions and the exact command.
6. `node --test scripts/verify-research-sidecar-container.test.mjs` — all tests pass.
7. `docker build --file backend/research-sidecar/Dockerfile --tag biostack-research-sidecar:p01 backend/research-sidecar` — succeeds using the default no-extra production path.
8. `node scripts/verify-research-sidecar-container.mjs --image biostack-research-sidecar:p01` — every image and local-dark assertion passes.
9. Inspect the final image configuration and package census independently of verifier output; record non-root user, command, port, base digest, and absent distributions.
10. Search the diff and captured output for token/request fixtures and provider-enablement flags; no secret or prohibited enablement is present.

If the host shell differs, use semantically identical commands and record them exactly. Do not weaken counts, timeouts, or assertions to make a run pass.

## Evidence Required

- Starting and ending commit IDs and goal-base commit.
- Exact changed-file list.
- `uv`, Python, pytest, Ruff, pip-audit, Node, and Docker versions.
- Full-suite pass/skip/fail count, with P05/P06 test identities named.
- Node test count and mutation-binding evidence.
- Docker build result, immutable base digest, final image ID, local tag, final image configuration, non-root identity, and forbidden-distribution census.
- Redacted local probe summary for health, auth negatives/positive, privacy rejection, arbitrary-workflow rejection, kill-switch terminal state, and docs-route 404s.
- Confirmation that no provider/Azure call, push, PR, merge, secret creation, public ingress, or production mutation occurred.
- Independent adversarial-review outputs, security-review report, coordinator triage, and clean-reviewer-worktree checks.
- Completed Session Handoff fields below.

## Collision Risk

Medium. `Dockerfile`, `.dockerignore`, `pyproject.toml`, and `uv.lock` define the future provider-capable build surface; this parcel must preserve that optional source contract while changing only the default production build. The new CI workflow is isolated. `app.py` is a serialization-sensitive runtime entry point and is restricted to the three FastAPI documentation URL arguments.

## PR Notes

- What changed: deterministic sidecar CI plus a digest-pinned, provider-SDK-free production-container contract.
- Why: D6, D8, D11, D12.7-D12.9, D19, SC-01-SC-04, and SG-IMAGE/SG-DATA/SG-OUTBOUND.
- Risk: build-argument mistakes could silently install provider dependencies; verifier or cleanup mistakes could overclaim or affect unrelated local containers.
- Verification: full Python suite, changed-file Ruff, strict dependency audit, Node verifier tests, production image build, independent image census, and bounded local dark probes.
- Evidence: attach only redacted local artifacts and review reports; no token or request body.

## Session Handoff

- Starting commit:
- Ending commit:
- Files changed:
- Commands run:
- Tests passed:
- Tests failed:
- Decisions needed:
- Blockers:
- Next safe action:
- Do not touch: P02-P04 files, Azure/GitHub/production state, providers, secrets, protected data, `main`.

## Stop-and-Report Rule

Stop and report if implementation requires a product or security decision absent from the ratified charter; a file outside Allowed Files; a public/cross-project contract change; a dependency addition/upgrade; a guessed base-image digest; fewer than 53 passing sidecar tests, more than one skip, or any failure; a live Azure/provider/protected-data operation; a production mutation; an inability to prove a named invariant independently; unsafe secret/log handling; or any scope/network drift. Do not work around a stop condition.
