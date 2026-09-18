# P01 Narrow Gate 2 Recovery 01 — Verification Receipt

Date: 2026-09-11

Candidate: `59102d3c8170968d2c9d316ec5b07a934042cee5`

Result: **GREEN — REVIEW DISPATCH AUTHORIZED**

## Step 0

- Branch: `parcel/sidecar-P01`
- Candidate HEAD exactly matched the authorized commit.
- Worktree was clean before and after execution.
- Approved base `7c628d70f632b237bbc4c01fbe001b56cc5017cf` remained an ancestor.
- Base diff contained exactly eight files, all inside P01's unchanged nine-file allowlist; no outside file was present.
- Local `biostack-research-sidecar:p01-ci` resolved to immutable image ID `sha256:d66d06a831cedd7784ca238ef56cb0e35653d382390f44436576bb9f29df169c`.
- No P01 ownership label or recovery helper name existed before the run.

## Source-boundary proof

Before enabling the authorized helper network, the exact cached Node image was inspected under `--network none`, `--pull=never`, a read-only filesystem, dropped capabilities, and `no-new-privileges`. `/etc/apt/sources.list.d/debian.sources` named only signed official Debian endpoints:

- `http://deb.debian.org/debian` for `bookworm` and `bookworm-updates`;
- `http://deb.debian.org/debian-security` for `bookworm-security`.

An initial read-only source-inspection command failed from shell quoting before any network access. It was not the authorized TERM fixture execution. A corrected direct file read succeeded under `--network none`.

## Exact fixture identity

The literal YAML `run` body of `Prove catchable TERM cleans the verifier-owned container` was extracted without editing source, tests, workflow, timeout, image, or configuration.

- Lines: 104
- Bytes: 3,398
- SHA-256: `E77BC29DC9051787A0BEE7FCAB2EBDD19D570A5D3E6886AACCFC43A78E48A0BD`
- Repo mount: read-only at `/work`
- Fixture mount: read-only at `/fixture.sh`
- Helper image: cached exact digest `node:22@sha256:c601a46abb4d2ab80a9dc3da208d50d1122642d53f17a101926ace71e5a9bf1c`, with `--pull=never`
- Docker socket: local Docker Desktop Linux socket
- Synthetic CI identity: `GITHUB_RUN_ID=9110001`, `GITHUB_RUN_ATTEMPT=1`

## Authorized external acquisition and execution

The single authorized helper invocation ran `apt-get update -qq` and installed only Debian's `docker.io` client with `--no-install-recommends`, then executed the literal fixture. No retry was performed.

- Docker client: `20.10.24+dfsg1`
- Docker server: `29.7.2`
- Outer helper exit: `0`
- Fixture contract: the exact verifier PID received TERM, exited exactly `1` within the 20-second grace, did not emit its ownership marker, and left no exact-name or owner-label container.

## Independent postconditions

- Zero containers with label `io.biostack.p01.owner`.
- Zero containers named `biostack-p01-ci-term-9110001-1`.
- Zero containers named `biostack-p01-ci-shell-recovery01`.
- Candidate HEAD remained `59102d3c8170968d2c9d316ec5b07a934042cee5`.
- Candidate worktree remained clean.
- Candidate image identity remained `sha256:d66d06a831cedd7784ca238ef56cb0e35653d382390f44436576bb9f29df169c`.
- The temporary extracted fixture was removed after its identity and result were recorded.

## Boundary statement

The only external access was the expressly authorized Debian package acquisition. No provider, scientific/model, production, Azure, protected-data, secret, repository-remote, deployment, or public-ingress call or mutation occurred. No source, test, workflow, timeout, image, or configuration change occurred.

Per Narrow Gate 2 Recovery 01, the next authorized action is fresh independent adversarial and security review of the unchanged candidate. P02 remains blocked pending P01 acceptance. Gate 3A and Gate 3B remain ungranted.
