# Track C — Production Operations Release-Candidate Evidence

Date: 2026-07-28  
Repository: `D:\Repos\BioStack`  
Starting commit: `7b10831` (`main`, also `origin/main`)  
Isolated branch: `codex/track-c-production-ops-20260728`  
Environment: local worktree only; no production deployment performed

## Scope and handling boundary

This ledger covers KEO-65, KEO-80, KEO-81, KEO-82, KEO-83, and KEO-193. It records implementation surfaces and verifiable evidence, not a production GO decision.

No production credentials were read, rotated, or entered. No production Azure resource was queried or changed. No customer data, raw evidence, PII, or secret values are included here.

The main BioStack worktree had pre-existing user changes (`package-lock.json` deletion and `.codex-remote-attachments/`); they were left untouched. All changes in this track are isolated to the worktree named above.

## Linear state

| Issue | Current state | Track C finding | Release effect |
|---|---|---|---|
| KEO-65 | In Progress | The code has production fail-closed checks, but the live security review, credential-rotation/history decision, hosted scans, and finding disposition are not evidenced here. | Blocking |
| KEO-80 | In Progress | CI contains SHA-pinned image deployment, revision checks, and API TLS binding. No target-environment deployment, configuration review, migration rehearsal, DNS proof, or certificate-renewal evidence was available locally. | Blocking |
| KEO-81 | Todo | No backup/restore drill evidence is present. The application requires Postgres in production; the bootstrap path is not durable storage. | Blocking |
| KEO-82 | Todo | The API currently configures console logging, but no privacy-safe redaction exercise, dashboards, alerts, responder, or synthetic-failure evidence is present in this track. | Blocking |
| KEO-83 | Todo | CI verifies that a candidate revision becomes healthy and serves a smoke path. A timed rollback rehearsal and migration rollback constraints are not evidenced. | Blocking |
| KEO-193 | Done | Linear marks the contract/integration issue Done, but its acceptance still requires staging conformance, durable authenticated receipts, negative tenancy tests, and smoke/rollback evidence. Those live behaviors remain unverified in this local run. | Blocking for integrated RC |

## Existing release surfaces inspected

- `.github/workflows/deploy.yml`: PR checks, backend restore/test, frontend `npm ci`/audit/test/build, GitHub OIDC login, SHA-tagged image push, revision readiness polling, API custom-domain TLS binding, and health smoke checks.
- `scripts/verify-containerapp-deployment.mjs` and its test: fails closed on incomplete Azure state, failed revisions, non-matching image, or a non-ready latest revision.
- `backend/src/BioStack.Api/Program.cs`: production Postgres enforcement, EF migration-on-startup, `/health`, `/health/keon`, CORS, rate limiting, authentication, and console logging.
- `backend/src/BioStack.Api/Auth/ProductionAuthConfiguration.cs`: HTTPS frontend/CORS origin validation and exactly-one email-provider requirement.
- `backend/src/BioStack.Application/Services/StripeProductionConfiguration.cs`: live/restricted Stripe key, webhook, distinct price IDs, and HTTPS return URL validation.
- `backend/Dockerfile`: non-root runtime, `/health` Docker healthcheck, and .NET 10 runtime image.
- `infra/azure/deploy-container-apps.ps1`: bootstrap-only Azure path. It is not treated as production evidence; its ACR admin credential flow requires separate security review before reuse.

## Verification evidence

| Command | Environment | Result | Evidence meaning |
|---|---|---|---|
| `rtk test node --test scripts/verify-containerapp-deployment.test.mjs` | isolated local worktree | 6 passed, 0 failed | Deployment readiness guard behavior is covered locally. |
| `rtk test node scripts/sync-product-contract.mjs --check` | isolated local worktree | passed | Product contract mirrors are synchronized. |
| `rtk dotnet restore backend/BioStack.sln --force-evaluate` | isolated local worktree | passed | Dependencies restored; no secret values involved. |
| `rtk proxy dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductionAuthConfigurationTests" --verbosity minimal` | isolated local worktree | 6 passed, 0 failed | Production auth configuration guardrails pass locally. |
| same command filtered to `ProductionMigrationBaselineConfigurationTests` | isolated local worktree | 2 passed, 0 failed | Production migration warning configuration tests pass locally. |
| same command filtered to `ProductionMigrationHistoryBaselineTests` | isolated local worktree | 4 passed, 0 failed | Migration history baseline tests pass locally. |
| `rtk dotnet test backend/BioStack.sln --no-restore --verbosity minimal` | isolated local worktree | timed out at 180 seconds | Not green evidence; the full solution result is unknown. |

Local evidence is necessary but does not satisfy the staging or production release gates.

## Exact release-candidate evidence still required

### CI and release identity — KEO-64 / KEO-80

- Successful hosted CI run for the exact candidate SHA, including backend tests, frontend tests/build, dependency gates, and deployment-readiness guard.
- Image digests and revision names for API and web, reconciled to the exact SHA; do not use `latest` as the release identity.
- Non-production environment configuration review by named owner without exposing values.
- Migration rehearsal against a production-like Postgres snapshot or disposable clone, including schema validation and startup behavior.

### Secrets, auth, and security — KEO-65

- Secret inventory by key name and source only; no values in tickets or logs.
- Human decision on whether any previously committed credential requires invalidation/history remediation.
- Hosted secret/dependency scans and defensive security review with no open Critical/High findings; Medium findings need owner, mitigation, and expiry or waiver.
- Negative tests for CORS, cookie/session, webhook signature, replay, unknown Stripe price, expired credential, wrong audience, and cross-tenant access.

### Database, DNS, CORS, and certificates — KEO-80

- Exact resource-scoped reads for the intended staging environment: Container Apps, managed environment, Postgres, registry, DNS, custom domains, certificates, and identity bindings.
- CORS origin equals the deployed frontend HTTPS origin; no placeholder origin remains.
- API and frontend custom domains, DNS records, certificate binding, renewal ownership, and HTTPS smoke evidence.
- Migration execution is measured and reversible within the approved migration policy; no unreviewed destructive migration.

### Backup and restore — KEO-81

- Pinned production-like backup/image/manifest, isolated restore target, named owner, drill window, measured RPO/RTO, schema/integrity/application checks, corrective actions, and teardown evidence.
- Recurring schedule, encryption, retention, access control, and alerting evidence.

### Observability — KEO-82

- Correlation ID contract and privacy-safe structured fields.
- Redaction tests showing that auth tokens, cookies, payment identifiers, provider PII, prompts, raw evidence, and private traces do not enter logs.
- Availability, latency, error, auth, database, billing, and dependency metrics with dashboards, actionable alerts, escalation owner, and synthetic-failure exercise.

### Health and rollback — KEO-83

- Startup/liveness/readiness behavior for API, database, and Keon dependency without sensitive output.
- Candidate withheld when health or dependency checks fail.
- Timed rollback rehearsal to the previous healthy immutable revision, including traffic verification, migration constraints, and recovery evidence.

### BioStack ↔ Keon integration — KEO-193

- Same versioned producer/consumer fixture and conformance suite.
- Staging submit/poll and receipt retrieval against exact release SHAs.
- Correct tenant/actor success plus wrong tenant, actor, audience, expired credential, replay, and timeout fail-closed tests.
- Collective CI, Control→Host service delegation, Gateway health, offline-verifier compatibility, and receipt/Kompress durability evidence.

## Environment readiness

| Environment | Status | Evidence |
|---|---|---|
| Local isolated worktree | Partial | Guard/config tests pass; full solution timed out. |
| Staging / release-candidate environment | Pending | No live environment evidence was collected in this track. |
| Production | Blocked by instruction | Deployment and production resource changes were explicitly out of scope. |

## Decisions and blockers

1. Name the staging environment and human owner for the release-candidate evidence run.
2. Confirm the authoritative deployment target and whether the existing `api.biostack.cc` domain is still canonical; the workflow only proves API-domain handling locally.
3. Decide the approved backup/restore target and RPO/RTO before KEO-81 can move.
4. Decide whether observability is implemented in-repo or through an approved Azure provider, with the data-retention boundary recorded.
5. Ratify whether KEO-193’s Done state represents contract completion only or requires the remaining live acceptance evidence before integrated RC readiness.

## Handoff

### Starting state

- Base commit: `7b10831`.
- Isolated worktree: `D:\Repos\BioStack\.worktrees\track-c-production-ops`.
- Branch: `codex/track-c-production-ops-20260728`.
- Main worktree user changes preserved and not inspected beyond status.

### Files changed in this track

- `infra/azure/README.md`: corrected production storage/readiness boundary and CI authority wording.
- `docs/operations/track-c-production-operations-release-candidate.md`: this evidence ledger.

### Next safe action

First repair and verify the frontend lockfile/CI reproducibility gap: `npm ci --dry-run --ignore-scripts` currently fails because `frontend/package-lock.json` is out of sync with `frontend/package.json`. Then run a coordinator-approved staging evidence pass using a disposable/non-production environment and redacted configuration metadata only. Attach the exact SHA, resource-scoped reads, migration result, backup/restore drill, observability exercise, rollback rehearsal, and KEO-193 conformance results to the corresponding Linear issues. Do not deploy production or enter live credentials until the human release gates are explicitly recorded.
