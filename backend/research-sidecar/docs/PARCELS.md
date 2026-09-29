# Parcel Index: Scientific Research Sidecar

## Initiative

BioStack ToolUniverse Scientific Research Sidecar

## Status

| Parcel | Wave | Status | Notes |
|---|---|---|---|
| p0-repository-truth | W0 | **done** | `docs/PHASE0-REPOSITORY-TRUTH.md` |
| p0-adr-001 | W0 | **done** | `docs/adr/ADR-001-scientific-research-sidecar.md` |
| p1-guidance-contract-v1 | W0 | **fully ratified** | All gates passed 2026-08-02; Class A–C unblocked |
| p2-sidecar-scaffold | W1 | **done** | Python package, health, jobs, kill switches, privacy gate |
| p2-dotnet-abstractions | W1 | **done** | Application abstractions only; no HTTP client yet |
| p2-tooluniverse-pin | W2 | **done** | `tooluniverse==1.4.0` base-only; allowlist v1; pin receipt |
| p2-workflow-sequences | W2 | **done** | Per-workflow allowlisted tool sequences |
| p2-dotnet-research-client | W2 | **done** | `ScientificResearchSidecarClient` + DI (disabled by default) |
| p1-guidance-ratification-package | W1 | **fully ratified** | All human gates Passed 2026-08-02 (Clint Morgan) |
| p8-evidence-comparison | W3 | **done** | Deterministic Class B comparison; 12 vs 0.5–1.0 mg example test |
| p2-ollama-adapter | W2 | pending | Probe only today; full inference adapter next |
| p2-kompress-research-profile | W2 | pending | Tenant/job isolation beyond admin endpoints |
| p5-typed-scientific-entities | W3 | pending | Published regimens, studies, AE records |
| p7-review-staging-wire | W3 | **done** | Sidecar results stage into existing review store/lifecycle |
| p8-analyzer-evidence-context | W3 | **done** | Class B comparison on `/api/analyze/protocol` |
| p11-contract-tests-ci | W2 | **done** | Frozen sidecar suite, hash-locked CI bootstrap, dependency audits, pinned two-stage no-extra image build, immutable local-image verification, and local dark container contract in `.github/workflows/research-sidecar-ci.yml` via `scripts/verify-research-sidecar-container.mjs`. Hatchling `1.32.0` and its complete six-package build closure are artifact-hash locked and audited in a builder-only environment; the project wheel is built without PEP 518 isolation or network resolution, installed locally without dependency resolution, and no build tool crosses into the runtime image. The Docker context is default-deny and admits only named metadata, README, and `src`; CI proves a credential-shaped fixture cannot be copied from the transmitted context. The runtime census requires exact equality with the locked 24-distribution set and the reviewed baked environment, plus a null/empty entrypoint. Catchable TERM/INT handling reserves bounded reconciliation and cleanup time, and CI uses an unpredictable invocation identity, fail-closed preflight/final queries, and exact-ID label-revalidated recovery to prove the real verifier exits 1 and removes its owned fixture before the normal 9/9 run; SIGKILL, host loss, and Docker-daemon loss remain non-atomic interruption boundaries. |

P01 hardening evidence (2026-09-09): CI selects exact Python `3.12.12` and Node
`22.23.1` patches; 53 Python tests passed with one expected skip; 282 verifier
mutation tests passed on the host and in the exact digest-pinned local Node 22 image
with a read-only filesystem and repository mount, no network, no pull,
dropped capabilities, and `no-new-privileges`; the strict runtime and build-closure dependency audits and secret scan passed;
and all nine dark-container checks passed against local immutable image ID
`sha256:d66d06a831cedd7784ca238ef56cb0e35653d382390f44436576bb9f29df169c`.
A bounded manual real-Docker proof observed the verifier-owned container in
`Created`, triggered the direct CLI's SIGTERM handler, returned nonzero in 1.721
seconds, and left zero containers carrying the P01 ownership label. Catchable signal
handling is capped at 15 seconds, strictly inside CI's 20-second post-TERM grace;
the direct CLI retains a failed lifecycle-cleanup claim for one bounded, label-revalidated
outer cleanup attempt and reports primary, lifecycle-cleanup, and outer-cleanup failures separately.
The same interruption contract is an enforceable CI gate using one exact verifier PID,
one exact random-labeled container proof, and fixture-scoped trap recovery before the
normal nine-check verifier run.
Creation that materializes only after both bounded reconciliation windows is an explicit
hard-late residual: the random name, label, cidfile, and full-ID evidence support safe
attribution, but P01 does not claim automatic cleanup beyond those windows.

Exact local Linux verifier-test reproduction:

```sh
docker run --rm --pull=never --name biostack-p01-node22-repro \
  --network none --read-only --cap-drop ALL \
  --security-opt no-new-privileges \
  --tmpfs /tmp:rw,exec,nosuid,size=64m \
  --mount "type=bind,source=${PWD},target=/work,readonly" \
  --workdir /work \
  node:22@sha256:c601a46abb4d2ab80a9dc3da208d50d1122642d53f17a101926ace71e5a9bf1c \
  node --test scripts/verify-research-sidecar-container.test.mjs
```

P01 proves warning-level log hygiene only for its local dark container. P02 must set
`BIOSTACK_RESEARCH_LOG_LEVEL=warning` in the deployment configuration and add a
static assertion for that exact name/value. P03 must verify the effective deployed
revision reports `log_level: warning` before accepting runtime leakage evidence, and
must retain commit, local-image, pushed-digest, and effective-revision custody. These
are explicit downstream obligations, not production-deployment evidence from P01.

## Dependency graph

```text
p0-repository-truth → p0-adr-001 → p1-guidance-contract-v1
p0-adr-001 → p2-sidecar-scaffold → p2-dotnet-abstractions
p2-sidecar-scaffold → p2-tooluniverse-pin (human gate)
p2-dotnet-abstractions → p4-http-client-infrastructure
p1-guidance-contract-v1 → p8-evidence-comparison (public copy)
p4-http-client-infrastructure → p7-review-staging-wire
```

## Human gates remaining

1. ~~Guidance Content Contract v1.~~ → **Fully ratified** 2026-08-02 (all product/legal/governance/clinical/public rows Passed).
2. ~~Approve ToolUniverse exact version (not `[all]`).~~ → pinned `1.4.0`
3. Raise Docker Desktop memory before container GPU PoC.
4. Approve any model pull (e.g. gemma4:12b) only after measured gap.
5. Enable `ScientificResearchSidecar:Enabled=true` in target environments after sidecar deploy.
