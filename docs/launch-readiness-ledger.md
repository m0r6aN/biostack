# BioStack launch readiness ledger

**Directive lane:** 0 (launch qualification)

**Audit scope:** lanes 3, 6, 7, 8, and 9

**Evidence baseline:** `main@235fb72883f8210c05f7855cb2ab6bf9e20d4841`

**Audit date:** 2026-07-11

**Current recommendation:** **NO-GO**

This is the single current launch ledger. A row may use only `verified`, `failed`, `blocked`, `obsolete`, or `not tested`. `verified` means the cited deterministic repository check passed; it does not imply a live-environment, legal, security, accessibility, or business approval. External owners are roles, not recorded human approvals.

## Release decision

BioStack is not qualified for production launch. The latest hosted main deployment workflow still failed before build/deploy; this branch repairs the `Keon.Kompress` restore path and high-severity transitive dependency locally, but no hosted run exists for the release SHA. Legal policies are unapproved placeholders, the frontend has no consent-recording experience, no durable backup/restore evidence exists, and live Stripe, auth, database, health, monitoring, rollback, accessibility, analytics, SEO, and support acceptance have not been demonstrated.

Release may be reconsidered only after every `failed` row is corrected and reverified, every release-blocking `blocked` row has external evidence attached, and the release owner records a new decision. Rows marked `not tested` are not passes.

## Lane 1 — calculator visuals and beginner clarity

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Syringe and vial visuals are prominent on active `/tools` dose and mix modes | verified | `SyringeDrawVisualizer`, `VialVisualizer`, and direct rendering in `ToolsDecisionSurface`; invalid inputs omit the meter/fill, over-capacity is announced, and both mL and U-100 units are stated. | QA owner: retain route-level regression coverage. | Required implementation is present on the production route rather than an orphan preview. |
| Beginner field help, worded summary, and unit/magnitude checks | verified | `ToolsDecisionSurface` restates powder, liquid, amount, concentration, mL, and U-100 units and keeps warnings math-only/non-prescriptive. | Product/safety owner: approve final wording. | Reduces first-use interpretation risk without adding administration instructions. |
| Focused calculator tests pass on the integrated branch | blocked | The calculator lane added zero/invalid, fractional, 10/50/100/over-capacity, accessibility, 375px/desktop, and active-route tests. Multiple primary-checkout Vitest runs emitted no result and left orphaned workers; they were terminated and are not counted as passes. | Frontend/CI owner: run the exact focused files in clean Node 22 CI and attach results. | Release blocker: integrated calculator behavior is not test-qualified. |
| Browser verification at mobile, tablet, and desktop widths | not tested | No successful integrated browser run was available. | Accessibility/QA owner: verify 375px, tablet, and desktop visibility, keyboard flow, text equivalents, and no console errors. | Release blocker for the customer-facing calculator acceptance gate. |

## Lane 2 — payment path

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Advertised packaging matches implemented checkout intervals | verified | Leadership fallback is monthly-only; `frontend/src/lib/marketing.ts`, pricing/billing surfaces, and commercialization docs no longer advertise annual checkout. Focused commercial-lane pricing tests passed. | Product owner: retain monthly-only until annual price IDs and contracts are implemented. | Removes the monthly/annual contradiction. |
| Checkout success and cancellation states are explicit | verified | `frontend/src/app/billing/page.tsx` renders dedicated `checkout=success` and `checkout=cancelled` messages. | QA owner: exercise both return URLs from Stripe test mode. | Local UI seam exists; live redirect behavior remains untested. |
| Webhook signature, idempotency, renewal, failure, cancellation, expiry, and downgrade | not tested | Signature and stored Stripe-event idempotency code exist, but no complete deployed lifecycle or replay evidence was produced for this release. | Billing owner: execute Stripe test/live lifecycle matrix and verify fail-closed Observer downgrade. | Revenue and entitlement blocker. |
| Production Stripe products, monthly prices, secrets, URLs, and Customer Portal | blocked | Required configuration keys are documented, but no production values or live portal evidence were available to this session. | Billing/platform owner: configure Operator/Commander monthly prices, secret/webhook secret, checkout/portal URLs, and portal policy. | Direct revenue blocker. |
| Authorized live transaction and refund/cancellation cycle | not tested | No real charge, refund, cancellation, renewal, or payment-failure exercise was performed. | Billing/release owner: run and document an authorized low-value live cycle before public launch. | Mandatory launch gate. |

## Lane 4 — provider conversion

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Provider request creates a durable, reviewable, privacy-minimal lead | verified | `ProviderAccessEndpoints`, `ProviderAccessRequest` entity/migration, consent version/timestamp, normalized email, idempotency/unique-email handling, honeypot, rate limit, admin list/update, confirmation UI; 4 focused integration tests and focused frontend tests passed in the isolated commercial lane. | QA/security owner: rerun after integration and verify deployed persistence/rate-limit headers. | The former no-op provider CTA now has a real server-backed path. |
| Provider request avoids health/protocol detail and overclaiming | verified | Form captures contact/organization/role/consent only; provider copy labels multi-client, export, sharing, and revocation workflows as pilot rather than available functionality. | Privacy/product owner: approve final intake fields and pilot terms. | Maintains the non-prescriptive/privacy boundary. |
| Internal notification, follow-up ownership, and operational SLA | blocked | Admin queue/status/owner fields exist; no notification destination, staffed owner, or response SLA is configured. | Provider-operations owner: assign queue ownership, notification, response target, and escalation. | Provider revenue blocker until requests are actively handled. |

## Lane 5 — unfinished customer-facing surfaces

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Printable-reference-card no-op CTA is removed | verified | Commercial lane removed the empty `Email me a printable reference card` control from `ToolsDecisionSurface`. | Product owner: only restore after a durable consented lead flow exists. | Removes a conversion-critical no-op. |
| All public/paid TODO, mock, preview, coming-soon, and no-op surfaces are qualified | not tested | This release fixed the named provider/printable-card paths and prior portal mock/tier work is on main, but no complete deployed crawl and entitlement-by-claim inventory was performed. | Product/QA owner: complete route/CTA/paid-claim crawl against the candidate. | Release blocker for honest paid claims. |

## Lane 3 — authentication, onboarding, and ownership

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| First-party session and bearer authentication are configured | verified | `backend/src/BioStack.Api/Program.cs`; cookie is HTTP-only, production-secure, server-side session validated, JWT issuer/audience/lifetime/signature validated. Focused command below. | Security owner: review configuration before launch. | Required control exists in code; live behavior remains separate. |
| Auth start and verify endpoints are rate-limited | verified | `backend/src/BioStack.Api/Program.cs` and auth endpoint mappings; fixed windows are 5/10 minutes and 10/10 minutes by remote IP. | Security owner: validate proxy/client-IP behavior in Azure. | Code control exists; forwarded-IP behavior is not proven. |
| Production magic-link delivery is configured and exercised | blocked | `backend/src/BioStack.Api/Program.cs` selects Azure Communication Email or SMTP; `infra/azure/deploy-container-apps.ps1` warns when SMTP is absent. No live credential or delivery evidence was provided. | Platform owner: configure an email provider and complete sign-in, expiry, replay, and callback tests on the deployed origin. | Release blocker: users may be unable to sign in. |
| Canonical onboarding route is present | not tested | Static inspection found `frontend/src/app/start/page.tsx` and the `/onboarding` redirect, but the focused frontend test was unavailable because the clean install did not complete. | Product owner: approve final onboarding content; frontend owner: rerun the focused test in a clean environment. | Route plumbing appears present but is not currently test-qualified. |
| End-to-end deployed auth/onboarding loop | not tested | No live URL, test identity, or email-delivery evidence was used in this lane. | QA owner: test anonymous start, sign-in, callback, session persistence, sign-out, expired link, and return URL. | Release blocker until passed. |
| Profile ownership isolation | verified | `OwnershipGuard`, owner-filtered `PersonProfileRepository`, and focused `OwnershipIsolationIntegrationTests`. | Security owner: retain regression test in required CI. | Deterministic server-side isolation passes focused tests. |
| Protected frontend routes require a session cookie | not tested | Static inspection found the cookie gate in `frontend/src/middleware.ts`, but `frontend/src/__tests__/middleware.public-routes.test.ts` could not run after the clean install timed out. | Security owner: rerun the focused test and verify direct deployed-route behavior and cookie domain. | Release evidence is incomplete. |

## Lane 6 — legal and consent

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Approved Terms of Service | failed | `frontend/src/app/terms/page.tsx` explicitly says legal review is required and copy is not final. This audit adds `noindex,nofollow` and removes the placeholder from the sitemap; that is containment, not approval. | Legal owner: provide dated, approved terms and effective version. | Release blocker. |
| Approved Privacy Policy | failed | `frontend/src/app/privacy/page.tsx` explicitly says legal review is required and copy is not final. This audit adds `noindex,nofollow` and removes the placeholder from the sitemap; that is containment, not approval. | Privacy/legal owner: approve policy covering health-related data, subprocessors, retention, deletion, rights, and contact. | Release blocker. |
| Authenticated write consent is enforced by the API | verified | `RequireConsentFilter`, endpoint `.RequireConsent()` mappings, and focused `ConsentGateIntegrationTests`. | Product/legal owner: confirm the versioned consent text represented by `bio-observational-v1`. | Server control passes focused tests. |
| User can review and record informed consent in the frontend | failed | No frontend call to `/api/v1/consent` exists. API failures link to `/onboarding/consent`, but `frontend/src/app/onboarding/page.tsx` redirects all onboarding paths to `/start`; no consent screen or acceptance control was found. | Product + legal + frontend owners: implement explicit versioned consent review/acceptance and refusal path. | Release blocker: protected writes cannot be completed by a new user. |
| Consent wording and policy versions have human approval | blocked | Code defaults to `bio-observational-v1`; no signed approval artifact is in scope. | Legal owner: approve exact text/version and retention evidence. | Release blocker; cannot be inferred from tests. |

## Lane 7 — environment, deployment, data, and operations

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Production secrets are supplied outside source control | blocked | `.env.example` documents required variables and GitHub deploy uses repository secrets/OIDC. Checked-in `backend/src/BioStack.Api/appsettings.json` contains development-looking JWT/callback/database values; actual production secret rotation and GitHub/Azure configuration were not inspected. | Security/platform owner: confirm all non-development values are unused in production, rotate if ever exposed, and validate secret inventory. | Release blocker until attested and verified. |
| Secret scanning workflow configuration is present | blocked | `.github/workflows/secret-scan.yml`; this audit adds the previously missing `.gitleaks.toml` extending Gitleaks defaults. Gitleaks is not installed locally, so configuration parsing was not tested in this lane. | Security owner: run Gitleaks in hosted CI, require the workflow, and obtain a green PR/main run. | Configuration gap is corrected, but release evidence remains blocked on a hosted pass. |
| Current deploy workflow passes | failed | Hosted run `29162842576` failed restoring `Keon.Kompress`; this branch vendors the four pinned packages, configures the repository feed, copies all solution projects into Docker restore, and passed 1,063 backend tests plus an API Docker build locally. No hosted run exists for this SHA. | Backend/platform owner: obtain a green hosted workflow for the exact release SHA. | Release blocker remains until hosted evidence exists. |
| Production dependency set has no known high-severity advisory | verified | `Microsoft.Bcl.Memory` is pinned to 10.0.9 (patched floor is 10.0.4 for GHSA-73j8-2gch-69rq); `dotnet list BioStack.sln package --vulnerable --include-transitive --no-restore` reported no vulnerable packages, and 252 API tests passed with zero warnings. | Security/backend owner: enforce vulnerability auditing in CI. | The identified high-severity dependency is remediated locally. |
| Deployment is gated before Azure mutation | verified | `.github/workflows/deploy.yml` runs backend and frontend tests before Azure login and container-app updates. | Platform owner: add environment protection/manual production approval if required by policy. | Prevented the failed build from deploying. |
| Production database is PostgreSQL | verified | `backend/src/BioStack.Api/Program.cs` rejects missing/non-Postgres production configuration and runs EF migrations; `.env.example` documents provider/connection variables. | DBA/platform owner: validate the actual target, least privilege, TLS, capacity, and migration plan. | Code fails closed; live database remains blocked below. |
| Live database connectivity and migrations | not tested | No production connection or deployment was exercised. | DBA: run migration rehearsal and smoke test against a release-like database. | Release blocker until passed. |
| Automated backups, retention, restore drill, and recovery objectives | failed | No production backup policy, retention, RPO/RTO, restore procedure, or successful restore-drill artifact was found. `infra/azure/README.md` still describes an obsolete ephemeral SQLite deployment path. | DBA/platform owner: configure Postgres backups, document RPO/RTO, and record a restore drill. | Release blocker; user data recovery is unproven. |
| Azure deployment documentation matches production database enforcement | failed | `infra/azure/README.md` says SQLite/ephemeral storage is current, while `Program.cs` rejects SQLite in Production and the script defaults `DatabaseProvider` to `sqlite`. | Platform owner: update script/docs to default to and require PostgreSQL for production. | Release blocker: documented default cannot start successfully. |
| Ephemeral SQLite is an acceptable production deployment path | obsolete | `Program.cs` now requires PostgreSQL in Production, superseding the older SQLite guidance in `infra/azure/README.md` and the script's SQLite default. | Platform owner: remove the obsolete production path from script/docs; keep SQLite explicitly development-only if needed. | Must not be used as a launch path. |
| Basic API health endpoint exists | verified | `backend/src/BioStack.Api/Program.cs` maps `/health`; `docker-compose.yml` probes it. | Platform owner: keep endpoint unauthenticated and low-cost. | Repository health seam exists. |
| Live readiness/liveness probes validate API and database | failed | Azure script/workflow contains no Container Apps health-probe configuration; `/health` uses default checks and no database readiness check was registered. | Platform/backend owner: define startup, liveness, and readiness probes, including an appropriate database readiness signal. | Release blocker: unhealthy revisions may receive traffic. |
| Production CORS origin is allow-listed | verified | `Program.cs` uses configured origins with credentials; Azure script sets the final public frontend origin. | Security/platform owner: verify the deployed custom origin and reject placeholder/local origins in production configuration. | Code/config seam exists; live headers remain untested. |
| General API abuse controls | failed | Rate limiting is attached only to auth start/verify. No global or sensitive non-auth endpoint rate limit was found. | Security/backend owner: threat-model and apply bounded limits to public analyze, knowledge, lead, and other costly/abusable surfaces. | Release blocker for an internet-facing launch. |
| Structured logs and sensitive-data redaction | failed | `Program.cs` clears providers and adds console logging only. No structured correlation policy, redaction test, or health-data logging policy enforcement was found. | Security/platform owner: define structured logging, correlation, retention, access, redaction, and tests. | Release blocker for incident response/privacy confidence. |
| Monitoring, alerting, dashboards, and owner rotation | failed | No Application Insights/OpenTelemetry/Sentry integration, alerts, SLOs, or on-call owner artifact was found. | Platform owner: configure telemetry and alerting for availability, errors, latency, auth, database, and deploy health. | Release blocker: failures may go undetected. |
| Rollback procedure is documented and rehearsed | failed | Workflow pushes immutable SHA tags but updates apps directly and has no rollback job, traffic-shift check, smoke gate, or rehearsal artifact. | Platform owner: document/rehearse revision rollback and database-forward/rollback constraints. | Release blocker. |

## Lane 8 — analytics, accessibility, SEO, and support

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Privacy-safe product analytics reaches an analytics backend | failed | `frontend/src/lib/analyzerAnalytics.ts` only dispatches browser `CustomEvent`; no collector/persistence integration was found. `docs/billing/tier-enforcement.md` defines a useful no-sensitive-data boundary but does not implement it. | Product/data/privacy owners: choose approved events, consent basis, destination, retention, and verify payload redaction. | Blocks launch measurement; privacy review required before enabling. |
| Automated accessibility acceptance | not tested | Components include labels/ARIA and accessibility-focused history exists, but no axe/Playwright accessibility gate or current WCAG report was found or run. | Accessibility/QA owner: audit keyboard, focus, semantics, contrast, zoom, screen reader, errors, and mobile at WCAG 2.2 AA target. | Release blocker for public acceptance. |
| SEO routes and metadata plumbing | verified | `frontend/src/app/layout.tsx`, `robots.ts`, and `sitemap.ts` provide language, title/description, robots, and sitemap. Unapproved legal placeholders are now excluded/noindexed. | Marketing owner: verify production domain, canonical tags, social cards, and Search Console after deployment. | Baseline exists; live indexing remains untested. |
| Live SEO/crawl behavior | not tested | No deployed-domain HTTP, rendered metadata, robots, sitemap, canonical, or status-code crawl was performed. | Marketing/QA owner: crawl the production candidate and attach results. | Required before public announcement. |
| Customer support route, contact, SLA, and escalation | failed | No dedicated support/contact route or operational support policy was found; marketing copy mentions priority support without a verified delivery channel. | Support/product owner: publish contact path, response expectations, escalation, privacy-safe intake, and ownership schedule. | Release blocker for paid/public support readiness. |

## Lane 9 — delivery workflows

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Main deployment workflow is green | failed | Latest run `29162842576` failed; earlier recent deployment runs also failed in `gh run list --limit 15`. | Engineering/platform owner: repair restore and obtain a green run for the exact release SHA. | Release blocker. |
| Secret scan is green and required | blocked | Historical runs failed because `.gitleaks.toml` was absent. Local config is repaired here, but no hosted run exists for this commit and branch protection was not verified. | Security/repo owner: run it, make it required, triage findings, and record green evidence. | Release blocker until hosted evidence exists. |
| Static security/quality analysis is configured and green | failed | `.github/workflows/sonarcloud.yml` has empty `sonar.projectKey` and `sonar.organization`; recent listed runs failed. | Security/repo owner: configure the project or replace/remove the nonfunctional workflow with an approved scanner. | Release blocker for claimed scan coverage. |
| Production approval, environment protection, and separation of duties | blocked | Deploy triggers directly on pushes to `main`; repository environment rules/branch protection/human approvers were not inspected or approved in this lane. | Repo/platform owner: configure protected production environment and named approval policy. | Release blocker for controlled production change. |
| Post-deploy smoke test and automatic halt/rollback | failed | Deploy workflow ends after container-app image updates; no health check, user-journey smoke, traffic validation, or rollback step exists. | Platform/QA owner: add release-SHA health/smoke checks and defined failure handling. | Release blocker. |
| Offline verification kit workflow | verified | Latest listed `Protocol Operations Offline Verification Kit` run `29162842618` passed. | Release owner: treat this as evidence only for its stated offline-kit scope. | Does not offset failed production launch controls. |

## Deterministic verification record

Run sequentially from the repository root unless noted otherwise:

```text
rtk git rev-parse HEAD
  235fb72883f8210c05f7855cb2ab6bf9e20d4841 (pre-change baseline)

rtk gh run list --limit 15
  latest deploy 29162842576: failed
  latest offline verification kit 29162842618: passed

rtk gh run view 29162842576 --log-failed
  NU1101: Keon.Kompress unavailable from nuget.org

rtk gh run list --workflow secret-scan.yml --limit 5
  five listed runs: failed

rtk gh run list --workflow sonarcloud.yml --limit 5
  five listed runs: failed

rtk dotnet restore tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --verbosity minimal
  historical audit result: passed with NU1903 for Microsoft.Bcl.Memory 9.0.4

rtk dotnet test BioStack.sln --verbosity minimal -m:1
  1,063 passed across 18 projects (CI repair lane)

rtk dotnet list BioStack.sln package --vulnerable --include-transitive --no-restore
  no vulnerable packages after direct Microsoft.Bcl.Memory 10.0.9 pin

rtk dotnet test tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --verbosity minimal -m:1
  252 passed, 0 warnings after dependency remediation

rtk docker build --file Dockerfile --tag biostack-api:ci-kompress-restore .
  passed; clean solution restore and API publish completed inside Linux image build

rtk dotnet test tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter FullyQualifiedName~ConsentGateIntegrationTests --verbosity minimal
  14 passed

rtk dotnet test tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --no-build --filter FullyQualifiedName~OwnershipIsolationIntegrationTests --verbosity minimal
  1 passed

rtk dotnet test tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --no-build --filter FullyQualifiedName~AuthEndpointsIntegrationTests --verbosity minimal
  10 passed

rtk npm ci
  timed out after 184 seconds in the clean worktree

rtk npm test -- --pool=threads --maxWorkers=1 src/__tests__/middleware.public-routes.test.ts src/__tests__/app/start/page.test.tsx
  unavailable: clean install did not complete; vitest/config could not be resolved

rtk npx vitest run <calculator-and-commercial-focused-files> --pool=forks --maxWorkers=1
  integrated primary checkout: no result; bounded runs timed out and orphan workers were terminated

rtk proxy gitleaks version
  unavailable: gitleaks is not installed locally
```

The commit SHA is reported in the lane handoff. No deployment, secret change, database mutation, legal approval, or production smoke test was performed by this audit.

## Decision ownership

Only the release owner may change the recommendation after reviewing attached evidence from legal, privacy, security, platform/DBA, accessibility/QA, support, product/data, and marketing owners. This document records technical qualification evidence; it does not substitute for those approvals.
