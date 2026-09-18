# P01 Fresh Review Brief — Recovery 01

## Context

P01 is the deterministic CI and production-container-contract parcel for BioStack's scientific research sidecar. It is local/test-only. P01 must establish a frozen, non-root, provider-SDK-free image and a fail-closed verifier without provider, scientific/model, Azure, production, protected-data, secret, repository-remote, deployment, or public-ingress activity.

Candidate: `59102d3c8170968d2c9d316ec5b07a934042cee5`

Approved base/spec commit: `7c628d70f632b237bbc4c01fbe001b56cc5017cf`

Worktree: `D:\Repos\BioStack-sidecar-P01`

Branch: `parcel/sidecar-P01`

Local candidate image: `sha256:d66d06a831cedd7784ca238ef56cb0e35653d382390f44436576bb9f29df169c`

## Exact Allowed Files

1. `.github/workflows/research-sidecar-ci.yml`
2. `scripts/verify-research-sidecar-container.mjs`
3. `scripts/verify-research-sidecar-container.test.mjs`
4. `backend/research-sidecar/docs/PARCELS.md`
5. `backend/research-sidecar/Dockerfile`
6. `backend/research-sidecar/.dockerignore`
7. `backend/research-sidecar/pyproject.toml`
8. `backend/research-sidecar/uv.lock`
9. `backend/research-sidecar/src/biostack_research_sidecar/app.py`

The base diff contains eight of these files; `uv.lock` is allowed but unchanged. No outside path is present.

## Objective inventory

- Workflow selects exact Python `3.12.12` and Node `22.23.1`, pins actions by commit, creates a hash-enforced verification-tool environment, runs the frozen Python suite and exact-count tripwire, audits runtime and build-backend closures, runs Node mutation tests, scans secrets, builds the local image, runs a build-context exclusion probe, enforces a real-Docker TERM fixture, then runs the normal nine-check verifier.
- Dockerfile pins builder and runtime images by digest, installs the locked no-extra runtime set, hash-locks the complete Hatchling build closure in a builder-only environment, copies only named metadata/README/source paths, removes installers, makes `/app` non-writable, and runs as a non-root user.
- Verifier validates immutable local image identity before execution, exact image configuration, exact canonical 24-distribution equality, filesystem/tool absence and ownership, dark configuration, health, auth, privacy/workflow rejection, terminal kill-switch behavior, closed docs routes, leakage markers, no-pull execution, bounded diagnostics, and ownership-proven cleanup.
- Catchable TERM/INT handling reserves cleanup time; delayed-create claims receive initial reconciliation and one bounded outer recovery. SIGKILL, host loss, Docker-daemon loss, and creation appearing only after both bounded windows are documented residuals.

## Verified observations

- Candidate worktree is clean; approved base remains an ancestor; `git diff --check` passed.
- Host Node mutation suite: 262/262 passed.
- Exact digest-pinned hardened Linux Node suite: 262/262 passed.
- Python suite: 53 passed, one expected skip.
- Frozen dependency sync, strict runtime audit, strict hash-locked build-backend audit, hash-enforced CI bootstrap, YAML parse, syntax checks, gitleaks scan, scope scan, and zero-leftover checks were reported green for the exact candidate.
- Candidate image build produced the stated immutable image ID; an independent credential-shaped build-context fixture produced the identical image and was absent from `/app`.
- Normal local container verifier independently passed 9/9 against the stated image and left zero P01 names/labels.
- Narrow Gate 2 Recovery 01 authorized one isolated rerun of the unchanged checked-in TERM fixture and only the Debian `docker.io` acquisition it required.
- The literal 104-line TERM run body had SHA-256 `E77BC29DC9051787A0BEE7FCAB2EBDD19D570A5D3E6886AACCFC43A78E48A0BD`.
- The exact cached Node digest used signed `deb.debian.org` sources for bookworm, updates, and security. The one authorized helper used Docker client `20.10.24+dfsg1` against server `29.7.2` and exited 0. Its internal contract required verifier exit 1 within 20 seconds and zero owned remnants.
- Independent postchecks found zero ownership-label containers, zero exact TERM fixture names, zero helper name, unchanged candidate HEAD/worktree, and unchanged image identity.
- Current local `main` is `b263b37a3ea839224271e901f8031ed79d7ba1f1`; its changes since the pinned charter base do not overlap P01–P04 Allowed Files. No rebase/merge is authorized here.

## Review task

Inspect the exact candidate directly. Do not read any other P01 review output, lead verdict, directive, or incident conclusion; this brief is the only goal artifact you may use. Do not edit, commit, build, install, pull, fetch, push, call a provider or remote service, or touch production/Azure/secrets/protected data. Safe local read-only commands and already-installed test runners are permitted. Review the complete base diff and relevant unchanged context.

Determine whether any reproducible release-blocking P01 defect remains. Focus on correctness and fail-closed behavior, ownership/race cleanup, exact package and build-context enforcement, CI/runtime reproducibility, supply-chain integrity, auth/privacy/log leakage, outbound/provider absence, and evidence overclaim. Distinguish code defects from explicit residuals and future P02–P04 obligations.

## Output contract

Return exactly these sections, maximum 900 words:

1. `VERDICT`
2. `FINDINGS` — numbered; each includes severity, exact locator, evidence, impact, and smallest remediation. State `None` if no actionable finding.
3. `REQUIRED MOVES` — numbered, implementation-ready; state `None` if no fix is required.
4. `COVERAGE GAPS`
5. `P01 RELEASE BLOCKER: YES|NO`
6. `WORKTREE CLEAN: yes|no`

Do not modify any file.
