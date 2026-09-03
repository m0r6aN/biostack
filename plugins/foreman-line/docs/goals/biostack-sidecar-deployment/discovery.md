# Discovery — BioStack Research Sidecar Deployment

Status: **COMPLETE FOR GATE 1 DRAFTING — LIVE OIDC INVENTORY REQUIRES A RATIFIED PREFLIGHT**

Date: 2026-09-03

## Repository and release identity

- Repository: `m0r6aN/biostack`.
- Default branch: `main`.
- Pinned starting commit: `4d8754c670a7d4553ade857be80aa170ede85653`.
- Pinned starting tree: `17b3ff60c74098f67edec222f1854f6486df7fe5`.
- Goal branch: `codex/goal-biostack-sidecar-deployment`.
- Goal worktree: `C:\Users\clint\.codex\worktrees\biostack-sidecar-deployment-goal\BioStack`.
- The primary checkout contains user-owned untracked `.audit/` and `.codex-temp/` content. It is read-only for this goal and remains untouched.

## Existing implementation

| Track | Location | Current state |
|---|---|---|
| Python sidecar | `backend/research-sidecar` | FastAPI/Python 3.12 image, non-root runtime, bearer service auth, privacy gate, job lifecycle, kill switches, pinned ToolUniverse optional extra, allowlisted workflows |
| BioStack API client | `backend/src/BioStack.Application/ScientificResearch` | Typed HTTP client and disabled provider fallback already shipped |
| Admin API | `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs` | Sidecar job routes exist under the existing `AdminOnly` authorization policy |
| Evidence staging | Application evidence/review services | Sidecar artifacts remain non-canonical and fail closed without a stable external source locator |
| Deployment | `.github/workflows/deploy.yml` | Audits sidecar dependencies but builds, pushes, updates, and verifies only API and frontend images |
| Sidecar CI | `backend/research-sidecar/docs/PARCELS.md` | `p11-contract-tests-ci` remains marked pending |

The algorithm-remediation release merged the P05 terminal-state fix and P06 accepted-source cap fix into `main`. Its verified release chain reported 53 sidecar tests passed and one expected legacy-config skip. This goal must rerun the complete sidecar suite on its own candidate; prior evidence is a starting fact, not release evidence for a new tree.

## Runtime defaults that matter

- Sidecar bind defaults to loopback. A separate Container App therefore must set `BIOSTACK_RESEARCH_HOST=0.0.0.0`; the settings model then requires a non-empty service token and forbids insecure development auth.
- `BIOSTACK_RESEARCH_GLOBAL_KILL_SWITCH` defaults to `false`; dark deployment must explicitly set it to `true`.
- GPU and local inference default enabled; the production scaffold release must explicitly disable both.
- ToolUniverse and hosted fallback default disabled and remain disabled throughout this goal.
- The API's `ScientificResearchSidecar:Enabled` default is `false`; dark deployment preserves that value.
- The sidecar job store is in-memory and job execution continues after the HTTP `202` response. Scale-to-zero or multiple replicas can lose or partition job state.

## Azure and GitHub evidence

- The merged-main deployment run `33743439471` authenticated successfully through GitHub OIDC and operated in managed environment `biostackmissionctrl-env` in `eastus` on the Consumption workload profile.
- That run showed the production API at minimum replicas `0`, maximum replicas `1`, with a single API container. The workflow secrets mask the subscription, tenant, resource group, registry, and app names.
- The active desktop Azure identity can authenticate but cannot see the BioStack Container Apps/resource group used by GitHub OIDC. It must not be treated as the production control identity.
- No production sidecar Container App or sidecar image deployment is evidenced by repository workflow or merged-main logs.
- A non-mutating OIDC preflight must resolve and fingerprint the exact subscription, tenant, resource group, registry, managed environment, API app, identities, registry configuration, scale settings, and existing sidecar-name collision before any Azure mutation.

## Existing contracts and constraints

- Guidance Content Contract v1 is fully ratified. Class D personalized direction remains prohibited.
- Only `public_scientific` and `public_metadata` payloads may cross the API-to-sidecar boundary. No profile, protocol-history, PHI, PII, protected data, or provider credentials may appear in test evidence or logs.
- Protected sidecar routes require a bearer service token. `/health` is unauthenticated but must reveal no secrets or payloads.
- ToolUniverse package version is pinned to `1.4.0`; arbitrary tool execution and `[all]` extras are forbidden.
- Sidecar output is candidate evidence only. It cannot become canonical without the existing human review/promotion gates.
- Current sidecar claims lack stable external source locators. This is an intentional fail-closed liveness limitation, not authority to weaken EvidenceGate or fabricate citations.

## Discovery conclusions

1. Deployment plumbing, not algorithm implementation, is the missing production track.
2. A separate internal-only Container App best preserves independent lifecycle, resources, rollback, and future scaling while keeping the API contract unchanged apart from environment configuration.
3. Initial enabled operation must use one continuously available replica because the job store is in-memory and work continues after `202`.
4. Dark deployment and scaffold-mode API enablement can be completed without any external scientific-provider call.
5. ToolUniverse/provider enablement and real-source-locator enrichment require later, separately ratified goals.
