# Goal Charter — BioStack Research Sidecar Deployment

Status: **RATIFIED AT GATE 1 (RECONFIRMED 2026-09-08) — D1-D18 AND THE P01-P04 PLAN STAND, AS AMENDED BY THE SCOPED RE-RATIFICATION OF 2026-09-03 (D5, D12 amended; D19, D20, D21 added; P04 split into P04a/P04b — see `plan-review-findings.md`). CONTINGENT GATE 2 IS GRANTED FOR P01, P02, P03, AND P04a LOCAL WORK ONLY. P04b, GATE 3A, AND GATE 3B REMAIN UNGRANTED.**

Goal slug: `biostack-sidecar-deployment`

Pinned execution base: `c05625caf4d633e428bd89f0334029e3c58b9f67`

Historical drafting base: `4d8754c670a7d4553ade857be80aa170ede85653`

## Coordinator ownership

| Field | Value |
|---|---|
| Owner | Codex coordinator session, accepted by direct Gate 1 instruction on 2026-09-08 at a zero-parcel boundary |
| Goal slug | `biostack-sidecar-deployment` |
| Goal branch | `codex/goal-biostack-sidecar-deployment` |
| Goal worktree | `C:\Users\clint\.codex\worktrees\biostack-sidecar-deployment-goal\BioStack` |
| Prior owner | Claude Opus 5 coordinator session; zero parcels were dispatched and no worktree was in flight at transfer |
| Transfer rule | One goal, one coordinator. Ownership transfers only at a parcel boundary, by editing this block. |

## Gate 1 — ratified 2026-09-03; reconfirmed 2026-09-08

The developer ratified verbatim the exact form in `gate-1-request.md`:

> Gate 1: I ratify D1-D18 and the P01-P04 parcel plan in `charter.md` as written, and grant contingent Gate 2 authorization for isolated shaping, implementation, deterministic verification, adversarial review, and security review of P01-P04 subject to their final exact Allowed Files, Step 0, dependency order, and stop conditions. Gate 3A and Gate 3B remain ungranted. No push, PR, merge, Azure mutation, secret creation/rotation, provider enablement, external call, protected-data use, public ingress, or sidecar/API production configuration change is authorized by this ratification.

The developer repeated that exact ratification on 2026-09-08 after refreshing `main`. This is a reconfirmation of D1-D18 and the P01-P04 umbrella, not a revocation of the already-ratified 2026-09-03 scoped amendments D19-D21 or the P04a/P04b split. The explicit no-mutation sentence keeps P04b outside Gate 2.

### Base reconciliation — 2026-09-08

The goal branch was locally rebased from the historical drafting base onto local remote-tracking `origin/main` at `c05625caf4d633e428bd89f0334029e3c58b9f67`. The only change since the drafting base in a reviewed sidecar-deployment surface was an added source-rights containment check in the pre-existing `.github/workflows/deploy.yml`; it does not invalidate the plan-review findings or any locked decision. No sidecar source, sidecar Dockerfile, sidecar client, Container Apps verifier, or proposed parcel file changed across the base interval. No remote fetch, push, PR, merge, or Azure/provider call was performed for this reconciliation.

Rewritten local goal-history references:

| Historical commit | Rebased commit | Meaning |
|---|---|---|
| `c9476ed` | `15d4656` | Draft charter |
| `bf7ef39` | `b2be37a` | Correct live-verification boundary |
| `cbdc555` | `faa631c` | Record initial Gate 1 ratification and loop directive |
| `a442a43` | `bbc82bc` | Correct parcel branch topology and vendor constraints |
| `fc846a5` | `49a40fa` | Plan-level adversarial review and triage |
| `038b617` | `e0d753c` | Ratify D5/D12/D19-D21 amendments and P04 split |

**Consequence for the exit criterion.** The exit criterion in this charter requires Gate 3A and Gate 3B, which are withheld and are human-only under D18. The goal is therefore not completable under this authorization. The coordinator drives to the reachable terminal state — P01-P04 independently accepted, verified, and adversarially/security reviewed, held unmerged on their parcel branches, with a Gate 3A request package prepared — and then stops. Gate 3A/3B remain open exit conditions carried to the final report.

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
- **D5 — Manual release workflow.** Sidecar deployment is performed only by an explicit `workflow_dispatch` release workflow protected by a GitHub production environment. That workflow never runs on a plain push to `main`. **[Amended 2026-09-03, ratified.]** The repository's pre-existing `.github/workflows/deploy.yml` already triggers on every push to `main` with no path filter and redeploys the production API and frontend unconditionally — this is a standing repo property this goal does not change and cannot suppress within its Allowed Files. The Gate 3A merge is therefore known and disclosed to also trigger that pre-existing workflow: the Gate 3A request package names the resulting API/frontend revision alongside the sidecar's.
- **D6 — Identity and registry.** The release uses the existing GitHub OIDC identity. The sidecar pulls an immutable commit-tagged image from the existing ACR through managed identity with `AcrPull`; ACR admin credentials or long-lived registry passwords may not be introduced.
- **D7 — Service authentication.** A new high-entropy service token is held in the protected GitHub environment and installed as Container Apps application secrets on the sidecar and API. Both containers consume it only through `secretref`; the workflow, logs, receipts, tests, and documentation never print or persist its value. Insecure development auth remains `false`.
- **D8 — Explicit dark configuration.** Dark deployment sets sidecar host `0.0.0.0`, global kill switch `true`, ToolUniverse `false`, hosted fallback `false`, local inference `false`, GPU `false`, max research concurrency `1`, and no workflow-specific relaxation. API `ScientificResearchSidecar__Enabled` remains `false`.
- **D9 — Two human release gates.** Gate 3A may authorize only the exact merge and dark deployment. After dark receipts are green, a fresh Gate 3B may authorize changing the sidecar global kill switch to `false` and API `ScientificResearchSidecar__Enabled` to `true` for scaffold mode. Neither gate authorizes ToolUniverse/provider enablement.
- **D10 — Data boundary.** Only synthetic or operator-entered public compound identifiers and public research parameters may be used. No user profile, protocol history, PHI, PII, protected data, production record payload, or secret may be transmitted to the sidecar or appear in evidence.
- **D11 — Verification.** CI must run the complete sidecar pytest suite, dependency audit, container build, container-contract checks, local dark health/auth/privacy/kill-switch probes, deployment-verifier tests, and secret-scan. No external provider call is permitted in CI or local verification.
- **D12 — Live dark acceptance.** **[Amended 2026-09-03, ratified.]** Gate 3A acceptance requires every one of the following, each independently observable and owned by a named parcel:
  - D12.1 exact image tag/digest recorded (P03, release job output).
  - D12.2 internal-only ingress — `ingress.external === false`, no public/custom domain (P02 bicep, P03 verifier).
  - D12.3 managed-identity image pull, no ACR admin credential used by the sidecar app itself (P02, P03 preflight).
  - D12.4 one-replica bounds enforced in the initial revision template, not a post-create update (P02).
  - D12.5 startup/readiness/liveness probes configured and healthy (P02, P03).
  - D12.6 dark health status proven as **ARM-reported `healthState: Healthy`** on the created revision — not an HTTP body fetch, which cannot resolve against `external:false` ingress from a GitHub-hosted runner (P03 verifier, extending `scripts/verify-containerapp-deployment.mjs`).
  - D12.7 protected-route denial without/with a wrong token, proven locally in the container per SC-03 (P01) and, where the release job can reach the internal FQDN, in production (P03).
  - ~~success with the correct token from the API network boundary~~ **removed from Gate 3A.** This assertion is structurally unachievable while API routing is disabled (D8/SC-06): `ScientificResearchDependencyInjection.cs` registers the live HTTP client and provider only when `Enabled=true`. It is folded into Gate 3B's SC-08, where routing is live by definition.
  - D12.8 no provider egress — recorded as a disclosed limitation: the Consumption profile without a VNet has no egress log to inspect, so this is proven only by D8/D19's configuration (kill switch, disabled flags, no-provider-SDK image) plus the absence of any positive evidence of egress, not by direct observation (P02, P03).
  - D12.9 safe logs — no secret value, no request-content echo, in any captured log (P01, P04; see D-finding on `ScientificResearchSidecarClient.cs` log redaction).
  - D12.10 a tested rollback command, including proof the restored revision's environment still carries `GLOBAL_KILL_SWITCH=true` (P03/P04, per D14).
- **D13 — Controlled scaffold acceptance.** Gate 3B acceptance requires an `AdminOnly` synthetic public-scientific job to reach the sidecar, return a terminal `partial` scaffold result with zero tools invoked, refuse unauthenticated/non-admin API access, and remain non-promotable under EvidenceGate. The deployed image digest must be the exact image built from the candidate whose deterministic P05 monotonic-terminal-state and P06 accepted-source-limit regressions passed. Production fault-injection hooks or false claims of live P05/P06 reproduction are forbidden.
- **D14 — Rollback order.** On any post-enable failure: set API sidecar routing to disabled first, assert the sidecar global kill switch, then return traffic/configuration to the recorded last-good revisions. No destructive resource deletion is part of rollback.
- **D15 — Evidence liveness stays separate.** Real external source-locator ingestion/enrichment is not part of this goal and does not block dark/scaffold deployment. EvidenceGate may not be weakened and citations may not be fabricated. Provider enablement and promotable evidence each require a separate charter and human release approval.
- **D16 — Live preflight.** Before any production mutation, the OIDC release job must perform a non-mutating inventory, validate the expected environment and base application topology, fingerprint masked resource identities, prove no conflicting sidecar app, and stop on drift. The current desktop Azure context is not production authority.
- **D17 — Review depth.** Each implementation parcel receives fresh independent adversarial review. Azure/IaC, auth/secrets, deployment workflow, and live-boundary parcels receive two independent adversarial reviews plus a separate defensive security review. Reviewers never fix or commit.
- **D18 — Gate custody.** Gate 1 is never inferred. Gate 2 covers only the exact named parcels after their specs pass coordinator lint and Step 0. Gate 3A and Gate 3B remain human-only and ungranted until exact candidate commits, image identity, environment, verification, review, cost, and rollback evidence are presented.
- **D19 — No-provider production image.** **[Added 2026-09-03, ratified.]** The deployed sidecar image is built without the ToolUniverse extra (`--build-arg TOOLUNIVERSE_EXTRA=""` or an equivalent no-extra build stage). P01's container contract asserts `openai`, `google-genai`, `huggingface-hub`, `pip`, and `setuptools` are absent from the built image's virtual environment. This shrinks SG-OUTBOUND's real attack surface at zero cost to D1's scope, since D1 already forbids using those SDKs — this decision ensures they are also not *present* to be misused.
- **D20 — Encrypted internal transport.** **[Added 2026-09-03, ratified.]** The sidecar's Container Apps ingress sets `allowInsecure: false`. The API's `ScientificResearchSidecar__BaseUrl` uses `https://`, never `http://`. Both are asserted by P02's verifier. The service token (D7) is the sole barrier to job execution within the managed environment (see discovery/plan-review residual); it may not additionally cross that network in cleartext.
- **D21 — Token rotation and compromise response.** **[Added 2026-09-03, ratified.]** The service token (D7) has a named rotation cadence and a documented emergency-rotation procedure, owned and stated in P04's operator runbook. D14's rollback order gains an explicit step: when rollback is triggered by suspected credential compromise, emergency token rotation precedes revision restore (routine, non-compromise rollbacks are unaffected by this addition).

## Tracks and integration surfaces

| Track | Owner | Surface | Security relevance |
|---|---|---|---|
| Sidecar CI/container | P01 | source tree -> tested immutable image | Supply chain, image contents, non-root runtime |
| Azure runtime | P02 | ACR/identity -> internal Container App | IAM, registry pull, ingress, scale, probes, secrets |
| Release control | P03 | GitHub OIDC -> Azure mutation/verification | Deployment authority, drift, rollback, log hygiene |
| API integration/UAT | P04 | `AdminOnly` API -> service-authenticated sidecar -> EvidenceGate | AuthN/AuthZ, public-only data, non-canonical custody |

## Parcel plan

### P01 — Deterministic sidecar CI and container contract

- Purpose: close pending sidecar CI, prove frozen dependencies and the production container's fail-closed/non-root contract, build without provider traffic, and build without provider SDKs (D19).
- Risk/routing: high supply-chain; frontier builder; two reviews plus security review.
- Allowed Files: **[corrected 2026-09-03 — plan-review findings 2, 3, 6, 19; coordinator authority, not a Gate-1 amendment]**
  - `.github/workflows/research-sidecar-ci.yml`
  - `scripts/verify-research-sidecar-container.mjs`
  - `scripts/verify-research-sidecar-container.test.mjs`
  - `backend/research-sidecar/docs/PARCELS.md`
  - `backend/research-sidecar/Dockerfile` (no-provider-SDK build per D19; base image pinned by digest per D6; `.env` and `tests/` excluded from build context)
  - `backend/research-sidecar/.dockerignore`
  - `backend/research-sidecar/pyproject.toml`
  - `backend/research-sidecar/uv.lock`
  - `backend/research-sidecar/src/biostack_research_sidecar/app.py` (narrow: `docs_url=None, redoc_url=None, openapi_url=None` only — no other change to this file)

### P02 — Internal Azure Container App definition

- Purpose: define the separate internal-only, managed-identity, one-replica, probed sidecar resource without enabling API routing or providers; enforce D20's encrypted-transport requirement; freeze the bicep parameter contract (names + types) as a ratified interface P03 codes against without discovery.
- Dependencies: P01 contract accepted.
- Risk/routing: critical deployment/IAM; frontier builder; two reviews plus security review.
- Allowed Files:
  - `infra/azure/research-sidecar-container-app.bicep`
  - `infra/azure/research-sidecar-container-app.parameters.example.json`
  - `infra/azure/verify-research-sidecar-container-app.ps1`

### P03 — Manual OIDC release workflow and deployment verifier

- Purpose: add preflight (including OIDC-principal RBAC fingerprint and ACR admin-user disclosure, D6), immutable build/push, dark deploy, exact revision/digest verification (ARM `healthState`, D12.6), safe logging, stop conditions, and rollback evidence (including post-restore kill-switch verification, D12.10) under a protected manual workflow. Extends the existing deployment verifier rather than forking a third one.
- Dependencies: P01 and P02 accepted.
- Risk/routing: critical release authority; frontier builder; two reviews plus security review.
- Allowed Files: **[corrected 2026-09-03 — plan-review findings 11, 15]**
  - `.github/workflows/deploy-research-sidecar.yml`
  - `scripts/verify-research-sidecar-deployment.mjs`
  - `scripts/verify-research-sidecar-deployment.test.mjs`
  - `scripts/verify-containerapp-deployment.mjs` (add an `--internal` ARM-`healthState` mode per D12.6; extend, do not fork)
  - `scripts/verify-containerapp-deployment.test.mjs`
  - `.github/workflows/secret-scan.yml` (scan git history, not working-tree-only)
  - `.gitleaks.toml` (restore a real high-entropy rule if the empty `generic-api-key` override is confirmed to suppress detection)

### P04 — Controlled enablement and operator runbook

**[Split 2026-09-03 into P04a/P04b — plan-review finding 13; parcel-boundary correction, not a change to D13's substance. Together P04a+P04b still satisfy D13 in full.]**

#### P04a — Live-boundary verifier, runbook, and negative tests (buildable and reviewable now, under Gate 2)

- Purpose: define the no-provider scaffold canary's verifier and negative auth/data tests, the operator runbook (including D21's rotation procedure and D12.9's log-safety requirement), monitoring evidence design, and the rollback runbook — as code and documentation, not live execution.
- Dependencies: verified P03 dark candidate.
- Risk/routing: critical auth/outbound/evidence boundary; frontier builder; two reviews plus security review.
- Allowed Files: **[corrected 2026-09-03 — plan-review findings 5, 18, 21]**
  - `docs/operations/research-sidecar-deployment-runbook.md`
  - `scripts/verify-research-sidecar-live-boundary.mjs`
  - `scripts/verify-research-sidecar-live-boundary.test.mjs`
  - `backend/src/BioStack.Application/ScientificResearch/ScientificResearchSidecarClient.cs` (narrow: log statement change only — log `code`, never the raw response `Body`, per D12.9)

#### P04b — API configuration transition and live rollback execution (dispatched only after Gate 3B is granted)

- Purpose: the actual production mutation — flip `ScientificResearchSidecar__Enabled` to `true`, run the live admin-only synthetic job, execute disable-first rollback if needed. **Not authorized by this ratification.** Requires a fresh Gate 2-equivalent dispatch approval scoped to P04b specifically, after Gate 3B.
- Dependencies: P04a accepted; Gate 3B granted.
- Risk/routing: critical; frontier builder; two reviews plus security review.
- Allowed Files: `.github/workflows/deploy-research-sidecar.yml` (API-config-transition steps only, additive to P03's release job).

P03 owns the shared deployment workflow first. P04b may touch it only after P03 is accepted and rebased, and only after Gate 3B; the serialization is mandatory. P04a does not touch `deploy-research-sidecar.yml` at all.

## Required scenario matrix

| ID | Environment | Scenario | Required result |
|---|---|---|---|
| SC-01 | local/CI | Complete sidecar suite and dependency/container checks | Green with exact counts recorded; zero provider/network traffic |
| SC-02 | local container | Dark health and configuration | HTTP 200, status `disabled`, kill switch true, providers/inference/GPU false |
| SC-03 | local container | Missing/wrong/correct service auth | Missing and wrong token denied; correct token permits only allowlisted protected routes |
| SC-04 | local container | Privacy and workflow rejection | Protected/private-shaped payload and arbitrary workflow rejected before execution |
| SC-05 | production preflight | OIDC topology and collision inventory | Expected subscription/environment/API/ACR identity fingerprints; no mutation; stop on drift |
| SC-06 | production dark | Immutable internal sidecar deployment | Exact digest/revision healthy, internal ingress only, one replica, API routing disabled |
| SC-07 | *(removed — folded into SC-08, D12 amendment 2026-09-03)* | ~~API-boundary authenticated probe~~ | Unachievable while routing is disabled (D8/SC-06); the correct-token-succeeds assertion now lives in SC-08, where routing is live by definition |
| SC-08 | production scaffold | Admin-only synthetic job, including the API-boundary authenticated probe (absorbed from former SC-07) | Terminal partial scaffold, zero tools, no protected data, EvidenceGate remains closed; correct token succeeds from the API network boundary, missing/wrong token fails, no provider call |
| SC-09 | CI-to-production custody | P05/P06 invariant and image-identity chain | Exact P05/P06 regressions pass before image build; deployed digest is the exact verified candidate image; no production fault-injection hook |
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

The goal is complete only when P01-P04 are independently accepted; the exact release candidate is merged under Gate 3A; the sidecar is deployed from an immutable image as a healthy internal-only, managed-identity, one-replica Container App; dark auth/privacy/kill-switch/network/log/rollback checks pass; the human grants Gate 3B; the API's existing admin-only client is enabled only in no-provider scaffold mode; a synthetic public-scientific job completes with zero tools and remains non-promotable; the deployed digest is proven identical to the candidate carrying green P05/P06 regressions; disable-first rollback is evidenced; all production workflows are green; and Stage-F records exact commits, digests, revisions, configuration, cost posture, reviews, residuals, and deferred provider/locator work.
