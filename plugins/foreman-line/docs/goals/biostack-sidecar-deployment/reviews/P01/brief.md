# P01 Independent Review Brief

## Context

BioStack parcel P01 implements deterministic CI and a production-container contract for an internal scientific-research sidecar. Binding scope and decisions are in:

- `C:\Users\clint\.codex\worktrees\biostack-sidecar-deployment-goal\BioStack\plugins\foreman-line\docs\goals\biostack-sidecar-deployment\charter.md`
- `C:\Users\clint\.codex\worktrees\biostack-sidecar-deployment-goal\BioStack\plugins\foreman-line\docs\goals\biostack-sidecar-deployment\specs\P01-deterministic-sidecar-ci-container-contract.md`

Candidate worktree: `D:\Repos\BioStack-sidecar-P01`

Candidate commit: `56063a4d238a0e8ddabb2ebfa56c64f1de47d027`

Review diff base: `7c628d70f632b237bbc4c01fbe001b56cc5017cf`

Gate 3A and Gate 3B are ungranted. No push, PR, merge, Azure/GitHub mutation, production configuration change, secret operation, provider enablement/call, protected-data use, or public ingress is authorized.

## Inventory

The candidate changes seven P01-allowed files:

1. `.github/workflows/research-sidecar-ci.yml`
2. `backend/research-sidecar/.dockerignore`
3. `backend/research-sidecar/Dockerfile`
4. `backend/research-sidecar/docs/PARCELS.md`
5. `backend/research-sidecar/src/biostack_research_sidecar/app.py`
6. `scripts/verify-research-sidecar-container.mjs`
7. `scripts/verify-research-sidecar-container.test.mjs`

`pyproject.toml` and `uv.lock` were allowed but unchanged. The implementation adds 1,677 lines and removes 19 relative to the review base. The verifier is 894 lines and its test is 603 lines.

## Verified observations

- The goal branch was reconciled to local `origin/main` commit `c05625caf4d633e428bd89f0334029e3c58b9f67`; no P01 file changed between the historical drafting base and that commit.
- Candidate and reviewer-target worktree status were clean before review.
- Exact changed-file allowlist and `git diff --check` pass.
- Existing sidecar suite: 53 passed, one expected legacy-config skip, zero failures.
- Node verifier suite: 148 passed, zero failed/skipped. Tests mutate every named pure evaluator invariant and include child-process import and truncation-boundary redaction cases.
- Strict frozen no-extra dependency audit using pip-audit 2.9.0 reported no known vulnerabilities.
- Base image is `ghcr.io/astral-sh/uv:python3.12-bookworm-slim@sha256:e5b65587bce7de595f299855d7385fe7fca39b8a74baa261ba1b7147afa78e58`.
- Built local image ID is `sha256:e2eee5c2554babcbc668035f1f7953952749ced5764277e879258d278b1e658f`.
- Independent image census: user `biostack`, runtime UID 100, `/app` workdir, exact Python module command, exactly port 8080, 24 venv distributions, and none of `tooluniverse`, `openai`, `google-genai`, `huggingface-hub`, `pip`, `setuptools`.
- Independent filesystem census found no `/app/.env*`, `/app/tests`, `/app/.git`, or `/app/docs`.
- The image contains executable `/usr/local/bin/uv` version 0.9.30. UID 100 can write both `/app` and `/app/.venv`. No `curl` or `wget` executable was found. Python remains executable.
- Local dark verifier returned `{status:"passed",checks:9}` and left no matching container. It proved loopback-only host publication, explicit disabled runtime flags, health disabled state, missing/wrong-token denial, correct-token allowlisted enumeration, privacy/workflow rejection, 202-to-terminal global-kill rejection, docs-route 404s, package/filesystem census, and log-marker absence.
- A coordinator reproduction found the first candidate truncated diagnostics before redaction and exposed a token prefix. Rework commit `56063a4d` now redacts before truncation and adds a binding regression.
- A coordinator reproduction found the first candidate threw during ESM eval import with no `process.argv[1]`. Rework commit `56063a4d` guards direct execution and adds a real child-process regression.
- The CI workflow has `contents: read`, no OIDC or Azure/deploy step, and bounded jobs/commands. It uses `actions/checkout@v4`, `actions/setup-python@v5`, and `actions/setup-node@v4`; downloads gitleaks 8.24.3 by versioned URL without a checksum; installs exact PyPI versions of uv and pip-audit without hashes.
- The CI secret scan is a working-tree `gitleaks dir` using the existing `.gitleaks.toml`. The charter assigns history-scan and rule remediation to later parcel P03.
- SC-01 through SC-04 are locally evidenced. SC-05 through SC-10 remain deferred to human-gated live stages.

## Your task

Review only candidate `56063a4d` against the binding charter/spec and actual code. Attempt to falsify correctness, fail-closed behavior, cleanup ownership, CI enforcement, immutable-image claims, provider-free claims, auth/data/log safety, and the evidence-to-claim mapping. Distinguish a candidate defect from a disclosed/deferred control owned by P02/P03/P04 or Gate 3. Do not edit, commit, build, install, call Azure/providers/GitHub APIs, or mutate anything. Small read-only/local test probes are allowed; leave the worktree clean.

## Output contract

Deliver exactly these two headings in plain text:

`TOP 5 BRUTAL FINDINGS`

Then five numbered one-line findings in the form `[severity] locator — claim — concrete evidence`.

`TOP 5 MOVES`

Then five numbered one-line implementation-ready moves in the form `target — exact smallest change — acceptance evidence`.

Maximum 500 words. Mark a claim `UNVERIFIED` if it cannot be reproduced. Do not fix or commit.
