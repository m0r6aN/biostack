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
| p11-contract-tests-ci | W2 | pending | CI job for sidecar pytest |

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
