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
| p11-contract-tests-ci | W2 | **done** | Frozen sidecar suite, hash-locked CI bootstrap, dependency audit, pinned two-stage no-extra image build, immutable local-image verification, and local dark container contract in `.github/workflows/research-sidecar-ci.yml` via `scripts/verify-research-sidecar-container.mjs`. The PEP 518 backend is exact-version pinned (`hatchling==1.32.0`), but `uv.lock` does not encode an artifact hash for isolated build-system resolution; the pinned builder image and exact backend version bound that accepted residual without claiming artifact-level lock coverage. |

P01 hardening evidence (2026-09-08): 53 Python tests passed with one expected skip;
197 verifier mutation tests passed; the strict dependency audit and secret scan passed;
and all nine dark-container checks passed against local immutable image ID
`sha256:fcd907ec3688997042d62c48d9bd0695305b7cb3622c837108101911cb13c2be`.

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
