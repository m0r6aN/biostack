# Goal Charter — BioStack Research Sidecar Deployment

Status: **DRAFT — AWAITING HUMAN GATE 1; NO PARCEL DISPATCH OR REMOTE MUTATION AUTHORIZED**

Goal slug: `biostack-sidecar-deployment`

Pinned base: `4d8754c670a7d4553ade857be80aa170ede85653`

## Objective

Ship the already-merged BioStack scientific research sidecar as a separately managed, internal-only Azure Container App; prove its immutable image, service-auth, privacy, lifecycle, scale, health, rollback, and API integration boundaries; then enable only the existing admin scaffold path without external-provider execution. Preserve EvidenceGate fail-closed behavior and leave ToolUniverse/provider enablement and real-source-locator enrichment for separately ratified follow-on goals.

## Consumers

- BioStack's existing `ScientificResearchSidecarClient`.
- Existing `/api/v1/admin/research/*` routes protected by `AdminOnly`.
- Platform operators responsible for Azure Container Apps and rollback.
- Evidence reviewers consuming only non-canonical staged candidates.

## Locked decisions proposed for Gate 1

- **D1 — Release boundary.** This goal deploys and verifies the sidecar runtime and enables only scaffold-mode API routing. It does not enable ToolUniverse, hosted inference, local inference, GPU work, arbitrary tools, public routes, or canonical evidence promotion.
- **D2 — Topology.** Deploy `biostack-research-sidecar` as a separate Container App in the existing `biostackmissionctrl-env`, with internal ingress only on port `8080`. The name satisfies Azure's 2–32-character Container App rule. It is not a second container in the public API app and receives no public/custom domain.
- **D3 — Job-state custody.** Because job state is in-memory and execution continues after `202`, enabled operation requires exactly one continuously available sidecar replica: `minReplicas=1`, `maxReplicas=1`. Any move to scale-to-zero or multiple replicas requires a persistent/distributed job-state design in a later charter.
- **D4 — Resource sizing.** The candidate must be measured locally and in Azure before Gate 3. Gate 3 must name the exact CPU/memory allocation and expected steady-state cost posture; no silent sizing or billing expansion is authorized.
- **D5 — Manual release workflow.** Sidecar deployment is performed only by an explicit `workflow_dispatch` release workflow protected by a GitHub production environment. A push to `main` may run CI but may not automatically mutate the sidecar or API configuration.
- **D6 — Identity and registry.** The release uses the existing GitHub OIDC identity. The sidecar pulls an immutable commit-tagged image from the existing ACR through managed identity with `AcrPull`; ACR admin credentials or long-lived registry passwords may not be introduced.
- **D7 — Service authentication.** A new high-entropy service token is held in the protected GitHub environment and installed as Container Apps application secrets on the sidecar and API. Both containers consume it only through `secretref`; the workflow, logs, receipts, tests, and documentation never print or persist its value. Insecure development auth remains `false`.
- **D8 — Explicit dark configuration.** Dark deployment sets sidecar host `0.0.0.0`, global kill switch `true`, ToolUniverse `false`, hosted fallback `false`, local inference `false`, GPU `false`, max research concurrency `1`, and no workflow-specific relaxation. API `ScientificResearchSidecar__Enabled` remains `false`.
- **D9 — Two human release gates.** Gate 3A may authorize only the exact merge and dark deployment. After dark receipts are green, a fresh Gate 3B may authorize changing the sidecar global kill switch to `false` and API `ScientificResearchSidecar__Enabled` to `true` for scaffold mode. Neither gate authorizes ToolUniverse/provider enablement.
- **D10 — Data boundary.** Only synthetic or operator-entered public compound identifiers and public research parameters may be used. No user profile, protocol history, PHI, PII, protected data, production record payload, or secret may be transmitted to the sidecar or appear in evidence.
- **D11 — Verification.** CI must run the complete sidecar pytest suite, dependency audit, container build, container-contract checks, local dark health/auth/privacy/kill-switch probes, deployment-verifier tests, and secret-scan. No external provider call is permitted in CI or local verification.
- **D12 — Live dark acceptance.** Gate 3A acceptance requires exact image tag/digest, internal-only ingress, managed-identity image pull, one-replica bounds, startup/readiness/liveness probes, dark health status, protected-route denial without/wrong token, success with the correct token from the API network boundary, no provider egress, safe logs, and a tested rollback command.
- **D13 — Controlled scaffold acceptance.** Gate 3B acceptance requires an `AdminOnly` synthetic public-scientific job to reach the sidecar, return a terminal `partial` scaffold result with zero tools invoked, preserve the P05 monotonic terminal-state and P06 accepted-source limit invariants, refuse unauthenticated/non-admin API access, and remain non-promotable under EvidenceGate.
- **D14 — Rollback order.** On any post-enable failure: set API sidecar routing to disabled first, assert the sidecar global kill switch, then return traffic/configuration to the recorded last-good revisions. No destructive resource deletion is part of rollback.
- **D15 — Evidence liveness stays separate.** Real external source-locator ingestion/enrichment is not part of this goal and does not block dark/scaffold deployment. EvidenceGate may not be weakened and citations may not be fabricated. Provider enablement and promotable evidence each require a separate charter and human release approval.
- **D16 — Live preflight.** Before any production mutation, the OIDC release job must perform a non-mutating inventory, validate the expected environment and base application topology, fingerprint masked resource identities, prove no conflicting sidecar app, and stop on drift. The current desktop Azure context is not production authority.
- **D17 — Review depth.** Each implementation parcel receives fresh independent adversarial review. Azure/IaC, auth/secrets, deployment workflow, and live-boundary parcels receive two independent adversarial reviews plus a separate defensive security review. Reviewers never fix or commit.
- **D18 — Gate custody.** Gate 1 is never inferred. Gate 2 covers only the exact named parcels after their specs pass coordinator lint and Step 0. Gate 3A and Gate 3B remain human-only and ungranted until exact candidate commits, image identity, environment, verification, review, cost, and rollback evidence are presented.

## Tracks and integration surfaces

| Track | Owner | Surface | Security relevance |
|---|---|---|---|
| Sidecar CI/container | P01 | source tree -> tested immutable image | Supply chain, image contents, non-root runtime |
| Azure runtime | P02 | ACR/identity -> internal Container App | IAM, registry pull, ingress, scale, probes, secrets |
| Release control | P03 | GitHub OIDC -> Azure mutation/verification | Deployment authority, drift, rollback, log hygiene |
| API integration/UAT | P04 | `AdminOnly` API -> service-authenticated sidecar -> EvidenceGate | AuthN/AuthZ, public-only data, non-canonical custody |

## Parcel plan

### P01 — Deterministic sidecar CI and container contract

- Purpose: close pending sidecar CI, prove frozen dependencies and the production container's fail-closed/non-root contract, and build without provider traffic.
- Risk/routing: high supply-chain; frontier builder; two reviews plus security review.
- Proposed Allowed Files:
  - `.github/workflows/research-sidecar-ci.yml`
  - `scripts/verify-research-sidecar-container.mjs`
  - `scripts/verify-research-sidecar-container.test.mjs`
  - `backend/research-sidecar/docs/PARCELS.md`

### P02 — Internal Azure Container App definition

- Purpose: define the separate internal-only, managed-identity, one-replica, probed sidecar resource without enabling API routing or providers.
- Dependencies: P01 contract accepted.
- Risk/routing: critical deployment/IAM; frontier builder; two reviews plus security review.
- Proposed Allowed Files:
  - `infra/azure/research-sidecar-container-app.bicep`
  - `infra/azure/research-sidecar-container-app.parameters.example.json`
  - `infra/azure/verify-research-sidecar-container-app.ps1`

### P03 — Manual OIDC release workflow and deployment verifier

- Purpose: add preflight, immutable build/push, dark deploy, exact revision/digest verification, safe logging, stop conditions, and rollback evidence under a protected manual workflow.
- Dependencies: P01 and P02 accepted.
- Risk/routing: critical release authority; frontier builder; two reviews plus security review.
- Proposed Allowed Files:
  - `.github/workflows/deploy-research-sidecar.yml`
  - `scripts/verify-research-sidecar-deployment.mjs`
  - `scripts/verify-research-sidecar-deployment.test.mjs`

### P04 — Controlled enablement and operator runbook

- Purpose: define and automate the no-provider scaffold canary, API configuration transition, negative auth/data tests, monitoring evidence, and disable-first rollback.
- Dependencies: verified P03 dark candidate; live execution only after Gate 3A, then Gate 3B.
- Risk/routing: critical auth/outbound/evidence boundary; frontier builder; two reviews plus security review.
- Proposed Allowed Files:
  - `docs/operations/research-sidecar-deployment-runbook.md`
  - `scripts/verify-research-sidecar-live-boundary.mjs`
  - `scripts/verify-research-sidecar-live-boundary.test.mjs`
  - `.github/workflows/deploy-research-sidecar.yml`

P03 owns the shared deployment workflow first. P04 may touch it only after P03 is accepted and rebased; the serialization is mandatory.

## Required scenario matrix

| ID | Environment | Scenario | Required result |
|---|---|---|---|
| SC-01 | local/CI | Complete sidecar suite and dependency/container checks | Green with exact counts recorded; zero provider/network traffic |
| SC-02 | local container | Dark health and configuration | HTTP 200, status `disabled`, kill switch true, providers/inference/GPU false |
| SC-03 | local container | Missing/wrong/correct service auth | Missing and wrong token denied; correct token permits only allowlisted protected routes |
| SC-04 | local container | Privacy and workflow rejection | Protected/private-shaped payload and arbitrary workflow rejected before execution |
| SC-05 | production preflight | OIDC topology and collision inventory | Expected subscription/environment/API/ACR identity fingerprints; no mutation; stop on drift |
| SC-06 | production dark | Immutable internal sidecar deployment | Exact digest/revision healthy, internal ingress only, one replica, API routing disabled |
| SC-07 | production dark | API-boundary authenticated probe | Correct token succeeds from API network boundary; missing/wrong token fails; no provider call |
| SC-08 | production scaffold | Admin-only synthetic job | Terminal partial scaffold, zero tools, no protected data, EvidenceGate remains closed |
| SC-09 | production scaffold | P05/P06 invariant probes | Late completion cannot overwrite terminal state; accepted materialization respects source cap |
| SC-10 | production | Disable-first rollback | API disabled, kill asserted, last-good state restored and health reverified |

## Security gates

- **SG-IMAGE:** locked dependency graph, non-root image, no secret/build-context leakage, immutable identity.
- **SG-IAM:** OIDC least privilege, managed-identity ACR pull, no ACR admin credential introduction.
- **SG-NETWORK:** internal ingress only, no public/custom domain, service-token enforcement.
- **SG-DATA:** public-scientific-only payloads, privacy rejection, payload-safe logs, zero protected-data evidence.
- **SG-OUTBOUND:** ToolUniverse, hosted fallback, local inference, and GPU remain disabled; no provider traffic.
- **SG-EVIDENCE:** staging remains non-canonical and EvidenceGate remains closed without a stable external locator.
- **SG-RELEASE:** manual protected environment, exact SHA/digest, drift stop, human Gate 3A/3B, disable-first rollback.

## Standing authorizations requested

- **Gate 1:** ratify D1-D18 and the four-parcel plan as written.
- **Contingent Gate 2:** after Gate 1 and plan-level adversarial review, authorize isolated shaping/build/review for P01-P04 only, subject to exact Allowed Files, Step 0, dependency order, and every stop condition.
- **Gate 3A:** not requested and not granted by Gate 1. It will name the exact merge set, image SHA/digest, Azure resource fingerprints, CPU/memory/cost, dark configuration, and rollback.
- **Gate 3B:** not requested and not granted by Gate 1 or Gate 3A. It will be requested only after live dark verification is green and will name the exact scaffold enablement/configuration and UAT procedure.

## Stop conditions

Stop and return to the human if:

1. the OIDC preflight cannot prove the intended subscription, resource group, environment, API app, ACR, identity, or absence/safe ownership of a sidecar app;
2. any implementation requires a file outside an exact parcel's Allowed Files;
3. any provider, hosted model, Ollama, GPU model, ToolUniverse tool, or protected/production payload is required;
4. managed-identity ACR pull cannot be established without new credentials or unratified IAM scope;
5. a secret value appears in output, evidence, git, process arguments captured by logs, or an artifact;
6. the sidecar would be publicly reachable or unauthenticated;
7. scale would be zero or greater than one while scaffold API routing is enabled;
8. test counts drift, a new skip/failure appears, a security review blocks, or a live probe differs from the ratified result;
9. EvidenceGate would need weakening or a locator/citation would need fabrication;
10. a production mutation is required before explicit Gate 3A or Gate 3B;
11. rollback cannot be demonstrated without deletion, reset, force-push, or unrelated resource mutation.

## Exit criterion

The goal is complete only when P01-P04 are independently accepted; the exact release candidate is merged under Gate 3A; the sidecar is deployed from an immutable image as a healthy internal-only, managed-identity, one-replica Container App; dark auth/privacy/kill-switch/network/log/rollback checks pass; the human grants Gate 3B; the API's existing admin-only client is enabled only in no-provider scaffold mode; a synthetic public-scientific job completes with zero tools and remains non-promotable; P05/P06 live invariants and disable-first rollback are evidenced; all production workflows are green; and Stage-F records exact commits, digests, revisions, configuration, cost posture, reviews, residuals, and deferred provider/locator work.
