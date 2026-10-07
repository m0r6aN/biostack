# Lead Independent Verdict — P01

Written before any external seat output was read.

TOP 5 BRUTAL FINDINGS

1. [HIGH] `backend/research-sidecar/Dockerfile:1,34` — the six forbidden distributions are absent, but the runtime retains `/usr/local/bin/uv` and gives UID 100 write access to `/app/.venv`, so the no-installer hardening rationale is only partially realized — direct container census reports uv 0.9.30 and both paths writable.
2. [MEDIUM] `.github/workflows/research-sidecar-ci.yml:27-38,106-119` — mutable action tags and an unchecked executable tarball remain in a workflow classified as high supply-chain risk — three `uses:` entries are version tags and gitleaks is executed without digest/checksum verification.
3. [MEDIUM] `.github/workflows/research-sidecar-ci.yml:120-126` — `gitleaks dir` proves only the checkout and inherits the known ambiguous empty generic-key rule; it cannot support a history-wide secret-safety claim — P03 owns the disclosed remediation, so P01 wording must remain checkout-only.
4. [LOW] `scripts/verify-research-sidecar-container.mjs:520-566` — package and filesystem observations are taken from a caller-supplied local image while Dockerfile source is checked separately; the standalone verifier does not prove that image was built from that Dockerfile — CI ordering supplies this custody only inside that workflow.
5. [LOW] `scripts/verify-research-sidecar-container.mjs:445-466` — the synthetic service token is passed in Docker CLI argv, making it observable to same-host process inspection during the local test — value is throwaway/local and no production credential is involved.

TOP 5 MOVES

1. `backend/research-sidecar/Dockerfile` — use a digest-pinned multi-stage build with a Python runtime stage that excludes `uv`, keep application/venv root-owned and readable/executable by `biostack` — census proves `uv`, pip, setuptools, provider SDKs absent and `/app`/venv non-writable.
2. `.github/workflows/research-sidecar-ci.yml` — pin GitHub Actions to full commit SHAs and verify the gitleaks archive against a pinned SHA-256 before execution — static tests bind each immutable identity/checksum.
3. P01 evidence language — state exactly “checked-out-tree scan”; retain history/rule remediation as P03-blocking release evidence — review finds no history-wide P01 claim.
4. `scripts/verify-research-sidecar-container.mjs` — optionally accept/build from an explicit image ID artifact or output the inspected ID so downstream custody records it; do not imply standalone source-to-image provenance — test rejects tag retargeting between build and inspect.
5. `scripts/verify-research-sidecar-container.mjs` — pass the local token through a temporary Docker env-file with restrictive ACL and guaranteed deletion, or document same-host argv exposure as a local-only residual — tests prove cleanup and output non-disclosure.
