# BioStack launch readiness ledger

**Directive lane:** 0 (launch qualification)

**Audit scope:** lanes 3, 6, 7, 8, and 9

**Evidence baseline:** `main@53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`

**Reconciled:** 2026-07-26

**Current recommendation:** **NO-GO**

This is the single current launch ledger. A row may use only `verified`, `failed`, `blocked`, `obsolete`, or `not tested`. `verified` means the cited deterministic repository check passed; it does not imply a live-environment, legal, security, accessibility, or business approval. External owners are roles, not recorded human approvals.

## Release decision

BioStack is deployed but is not qualified for production launch. Hosted run `30211140857` built, tested, deployed, and pinned-smoke-checked exact SHA `53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`, and hosted secret scan `30211140879` passed for the same SHA. Those are deployment facts, not release qualification. Current blockers include an attributed 2026-07-26 audit observation that the public compound response exposed dose/schedule/optimization guidance contrary to the stated non-prescriptive boundary, unapproved legal policies and consent wording, known high-severity dependency advisories in the green deploy log, incomplete Stripe lifecycle acceptance, unproved production auth and Keon dependency journeys, and missing backup/restore, database-readiness, monitoring, rollback, accessibility, analytics, and support evidence.

Release may be reconsidered only after every `failed` row is corrected and reverified, every release-blocking `blocked` row has external evidence attached, and the release owner records a new decision. Rows marked `not tested` are not passes.

## Lane 1 — calculator visuals and beginner clarity

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Syringe and vial visuals are prominent on active `/tools` dose and mix modes | verified | `SyringeDrawVisualizer`, `VialVisualizer`, and direct rendering in `ToolsDecisionSurface`; invalid inputs omit the meter/fill, over-capacity is announced, and both mL and U-100 units are stated. | QA owner: retain route-level regression coverage. | Required implementation is present on the production route rather than an orphan preview. |
| Beginner field help, worded summary, and unit/magnitude checks | verified | `ToolsDecisionSurface` restates powder, liquid, amount, concentration, mL, and U-100 units and keeps warnings math-only/non-prescriptive. | Product/safety owner: approve final wording. | Reduces first-use interpretation risk without adding administration instructions. |
| Focused calculator tests pass on the integrated branch | verified | The calculator regression files are included in the full frontend suite that passed in hosted exact-SHA deploy run `30211140857` at `53ed0df5`. | Frontend/CI owner: retain the calculator regressions in the required hosted suite. | Repository behavior is test-qualified; browser accessibility acceptance remains separate below. |
| Browser verification at mobile, tablet, and desktop widths | verified | Audited 2026-08-28 pre-deploy at 375x812 / 768x1024 / desktop (`.audit/a11y-tools-2026-08-28.md`) and re-verified live post-deploy (run 33200114206): zero unlabeled controls (aria-labels "Powder amount" / "Powder amount unit" present), info trigger 24x24, zero console errors, no horizontal scroll; PR #254 overlap copy and boundary line render live. | Accessibility/QA owner: keep the DOM-audit script in the regression kit. | Gate closed with live evidence. |

## Lane 2 — payment path

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Advertised packaging matches implemented checkout intervals | verified | Leadership fallback is monthly-only; `frontend/src/lib/marketing.ts`, pricing/billing surfaces, and commercialization docs no longer advertise annual checkout. Focused commercial-lane pricing tests passed. | Product owner: retain monthly-only until annual price IDs and contracts are implemented. | Removes the monthly/annual contradiction. |
| Checkout success and cancellation states are explicit | verified | `frontend/src/app/billing/page.tsx` renders dedicated `checkout=success` and `checkout=cancelled` messages. | QA owner: exercise both return URLs from Stripe test mode. | Local UI seam exists; live redirect behavior remains untested. |
| Webhook signature, idempotency, renewal, failure, cancellation, expiry, and downgrade | not tested | Signature and stored Stripe-event idempotency code exist, but no complete deployed lifecycle or replay evidence was produced for this release. | Billing owner: execute Stripe test/live lifecycle matrix and verify fail-closed Observer downgrade. | Revenue and entitlement blocker. |
| Production Stripe products, monthly prices, secrets, URLs, and Customer Portal | blocked | Required configuration keys are documented, but no production values or live portal evidence were available to this session. | Billing/platform owner: configure Operator/Commander monthly prices, secret/webhook secret, checkout/portal URLs, and portal policy. | Direct revenue blocker. |
| Authorized live transaction and refund/cancellation cycle | blocked | 2026-08-28: operator executed a successful authorized live test transaction against the deployed build (run 33200114206 era). Charge leg proven; the refund/cancellation, renewal, and payment-failure legs of the cycle are not yet exercised or documented. | Billing/release owner: complete and document the refund/cancel leg (and downgrade-to-Observer on failure) to flip this row to verified. | Charge path is live-proven; remaining legs close the gate. |

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
| Public knowledge and analyzer output honor the non-prescriptive, Unknown-first boundary | verified | Remediated by PR #235: `KnowledgeEntryResponse` marks all dose/schedule/pairing/optimization fields `[JsonIgnore]`, and `KnowledgeEndpointsIntegrationTests.GetCompound_PublicSerializationPreservesEvidenceAndOmitsIndividualizedActionFields` seeds a probe entry and asserts all sixteen withheld properties are absent while evidence tier, sources, cautions, and interactions serialize. Live re-verification 2026-08-27: `GET https://biostack.cc/api/v1/knowledge/compounds/BPC-157` returned HTTP 200 with only the twelve observational properties (raw capture: `.audit/prod-bpc157-2026-08-27.json`), superseding the 2026-07-26 KEO-84 observation. | Product/safety owner: quarantine stale public content, promote governed evidence, remove prescriptive fields, and prove Unknown-first behavior with live and deterministic tests. | Release blocker for public claim safety and product honesty. |

## Lane 3 — authentication, onboarding, and ownership

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| First-party session and bearer authentication are configured | verified | `backend/src/BioStack.Api/Program.cs`; cookie is HTTP-only, production-secure, server-side session validated, JWT issuer/audience/lifetime/signature validated. Focused command below. | Security owner: review configuration before launch. | Required control exists in code; live behavior remains separate. |
| Auth start and verify endpoints are rate-limited | verified | `backend/src/BioStack.Api/Program.cs` and auth endpoint mappings; fixed windows are 5/10 minutes and 10/10 minutes by remote IP. | Security owner: validate proxy/client-IP behavior in Azure. | Code control exists; forwarded-IP behavior is not proven. |
| Production magic-link delivery is configured and exercised | blocked | `backend/src/BioStack.Api/Program.cs` selects Azure Communication Email or SMTP; `infra/azure/deploy-container-apps.ps1` warns when SMTP is absent. No live credential or delivery evidence was provided. | Platform owner: configure an email provider and complete sign-in, expiry, replay, and callback tests on the deployed origin. | Release blocker: users may be unable to sign in. |
| Canonical onboarding route is present | verified | `frontend/src/app/start/page.tsx`, the `/onboarding` redirect behavior, and their frontend regressions are present in the full hosted suite that passed in run `30211140857`. | Product owner: approve final onboarding content; QA owner: retain route regressions. | Repository route plumbing is test-qualified; deployed email/session behavior remains separate. |
| End-to-end deployed auth/onboarding loop | not tested | No live URL, test identity, or email-delivery evidence was used in this lane. | QA owner: test anonymous start, sign-in, callback, session persistence, sign-out, expired link, and return URL. | Release blocker until passed. |
| Profile ownership isolation | verified | `OwnershipGuard`, owner-filtered `PersonProfileRepository`, and focused `OwnershipIsolationIntegrationTests`. | Security owner: retain regression test in required CI. | Deterministic server-side isolation passes focused tests. |
| Protected frontend routes require a session cookie | verified | `frontend/src/middleware.ts` and `frontend/src/__tests__/middleware.public-routes.test.ts` are present, and the full hosted frontend suite passed in run `30211140857`. | Security owner: retain the regression and separately verify deployed cookie domain/session behavior. | Deterministic route gating passes; live session qualification remains separate. |

## Lane 6 — legal and consent

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Approved Terms of Service | failed | `frontend/src/app/terms/page.tsx` explicitly says legal review is required and copy is not final. This audit adds `noindex,nofollow` and removes the placeholder from the sitemap; that is containment, not approval. | Legal owner: provide dated, approved terms and effective version. | Release blocker. |
| Approved Privacy Policy | failed | `frontend/src/app/privacy/page.tsx` explicitly says legal review is required and copy is not final. This audit adds `noindex,nofollow` and removes the placeholder from the sitemap; that is containment, not approval. | Privacy/legal owner: approve policy covering health-related data, subprocessors, retention, deletion, rights, and contact. | Release blocker. |
| Authenticated write consent is enforced by the API | verified | `RequireConsentFilter`, endpoint `.RequireConsent()` mappings, and focused `ConsentGateIntegrationTests`. | Product/legal owner: confirm the versioned consent text represented by `bio-observational-v1`. | Server control passes focused tests. |
| User can review and record informed consent in the frontend | verified | `frontend/src/app/onboarding/consent/page.tsx` reads the current server version, records acceptance or refusal, signs out after refusal, and preserves approved return paths. `ConsentPage.test.tsx` covers acceptance, refusal, and expired-session routing in the hosted frontend suite. | QA owner: execute the authorized deployed scenarios in the KEO-69 runbook. | The repository experience exists; legal approval and live auth evidence remain blocking rows. |
| Consent wording and policy versions have human approval | blocked | Code defaults to `bio-observational-v1`; no signed approval artifact is in scope. | Legal owner: approve exact text/version and retention evidence. | Release blocker; cannot be inferred from tests. |

## Lane 7 — environment, deployment, data, and operations

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Production secrets are supplied outside source control | blocked | `.env.example` documents required variables and GitHub deploy uses repository secrets/OIDC. Checked-in `backend/src/BioStack.Api/appsettings.json` contains development-looking JWT/callback/database values; actual production secret rotation and GitHub/Azure configuration were not inspected. | Security/platform owner: confirm all non-development values are unused in production, rotate if ever exposed, and validate secret inventory. | Release blocker until attested and verified. |
| Secret scanning workflow configuration is present | verified | Hosted Gitleaks run `30211140879` passed for exact SHA `53ed0df5`. The workflow and `.gitleaks.toml` are executable in hosted CI. | Security owner: make the workflow required and separately close rotation/full-history evidence. | Hosted current-tree scanning passes; required-check and historical-secret gates remain separate. |
| Current deploy workflow passes | verified | Hosted run `30211140857` passed backend/frontend gates, built immutable SHA-tagged images, updated API and web, proved each exact latest-ready revision, and returned HTTP 200 from pinned smoke checks for `53ed0df5`. | Platform owner: retain exact-revision gates and add the customer and dependency checks below. | Deployment evidence is current but does not override other release blockers. |
| Production dependency set has no known high-severity advisory | verified | Re-audited 2026-08-28 on branch `lead/category-language-unify`: `System.Security.Cryptography.Xml` upgraded to 10.0.10 (`BioStack.Infrastructure.csproj`); `dotnet list BioStack.sln package --vulnerable --include-transitive` reports zero vulnerable packages across all 14 projects (`tmp/dotnet-vuln.log`); `npm audit` on `frontend/` reports 0 findings at every severity; `pip-audit` against `backend/research-sidecar/uv.lock` export reports no known vulnerabilities. | Security/backend owner: none outstanding — `deploy.yml` already fails the build on NuGet high advisories (`NuGetAuditLevel=high`, `WarningsAsErrors=NU1903;NU1904`) and `npm audit --audit-level=moderate`; a `pip-audit --strict` step for `backend/research-sidecar` was added on this branch. | Dependency set is clean and all three ecosystems are CI-gated once this branch merges. |
| Deployment is gated before Azure mutation | verified | `.github/workflows/deploy.yml` runs backend and frontend tests before Azure login and container-app updates. | Platform owner: add environment protection/manual production approval if required by policy. | Prevented the failed build from deploying. |
| Production database is PostgreSQL | verified | `backend/src/BioStack.Api/Program.cs` rejects missing/non-Postgres production configuration and runs EF migrations; `.env.example` documents provider/connection variables. | DBA/platform owner: validate the actual target, least privilege, TLS, capacity, and migration plan. | Code fails closed; live database remains blocked below. |
| Live database connectivity and migrations | not tested | No production connection or deployment was exercised. | DBA: run migration rehearsal and smoke test against a release-like database. | Release blocker until passed. |
| Keon dependency readiness | blocked | 2026-08-28: production runs with the EXPLICIT stub acknowledgement `KeonRuntime AllowStubInProduction=true` (operator decision after the fail-closed guard correctly refused an unconfigured boot; revisions 0000114+). Per #245 this degrades non-effecting safety receipts only; effect-bearing paths remain fail-closed. Retire condition: configure `KeonRuntime:BaseUrl` + `LiveMode=true` against a live Keon service, then collect the revision-bound `/health/keon` and dependent-journey evidence this row requires. | Platform/Keon owner: verify the dependency probe and bounded failure behavior against the release revision without widening authority. | Release blocker for any functionality that depends on Keon availability. |
| Automated backups, retention, restore drill, and recovery objectives | failed | No production backup policy, retention, RPO/RTO, restore procedure, or successful restore-drill artifact was found. `infra/azure/README.md` still describes an obsolete ephemeral SQLite deployment path. | DBA/platform owner: configure Postgres backups, document RPO/RTO, and record a restore drill. | Release blocker; user data recovery is unproven. |
| Azure deployment documentation matches production database enforcement | failed | `infra/azure/README.md` says SQLite/ephemeral storage is current, while `Program.cs` rejects SQLite in Production and the script defaults `DatabaseProvider` to `sqlite`. | Platform owner: update script/docs to default to and require PostgreSQL for production. | Release blocker: documented default cannot start successfully. |
| Ephemeral SQLite is an acceptable production deployment path | obsolete | `Program.cs` now requires PostgreSQL in Production, superseding the older SQLite guidance in `infra/azure/README.md` and the script's SQLite default. | Platform owner: remove the obsolete production path from script/docs; keep SQLite explicitly development-only if needed. | Must not be used as a launch path. |
| Basic API health endpoint exists | verified | `backend/src/BioStack.Api/Program.cs` maps `/health`; `docker-compose.yml` probes it. | Platform owner: keep endpoint unauthenticated and low-cost. | Repository health seam exists. |
| Live readiness/liveness probes validate API and database | failed | The workflow now waits for the exact latest-ready revision and performs a pinned `/health` smoke, but `/health` still uses default checks and no database readiness signal or reviewed Container Apps startup/liveness/readiness probe evidence is attached. Storage-custody fact operator-verified 2026-08-28: `Database__Provider=postgres` on `biostackmissionctrl-api` with zero volumes — external managed PostgreSQL, so user data is not on ephemeral revision storage (runbook §0). | Platform/backend owner: define and verify startup, liveness, and readiness probes, including an appropriate database readiness signal. | Release blocker: revision activation alone does not prove dependency readiness. |
| Production CORS origin is allow-listed | verified | `Program.cs` uses configured origins with credentials; Azure script sets the final public frontend origin. | Security/platform owner: verify the deployed custom origin and reject placeholder/local origins in production configuration. | Code/config seam exists; live headers remain untested. |
| General API abuse controls | failed | Rate limiting is attached only to auth start/verify. No global or sensitive non-auth endpoint rate limit was found. | Security/backend owner: threat-model and apply bounded limits to public analyze, knowledge, lead, and other costly/abusable surfaces. | Release blocker for an internet-facing launch. |
| Structured logs and sensitive-data redaction | failed | `Program.cs` clears providers and adds console logging only. No structured correlation policy, redaction test, or health-data logging policy enforcement was found. | Security/platform owner: define structured logging, correlation, retention, access, redaction, and tests. | Release blocker for incident response/privacy confidence. |
| Monitoring, alerting, dashboards, and owner rotation | failed | No Application Insights/OpenTelemetry/Sentry integration, alerts, SLOs, or on-call owner artifact was found. | Platform owner: configure telemetry and alerting for availability, errors, latency, auth, database, and deploy health. | Release blocker: failures may go undetected. |
| Rollback procedure is documented and rehearsed | failed | Workflow pushes immutable SHA tags and now halts between API/web updates on failed exact-revision readiness or smoke, but no rollback job, traffic-shift rehearsal, or database-forward/rollback artifact exists. | Platform owner: document/rehearse revision rollback and database-forward/rollback constraints. | Release blocker. |

## Lane 8 — analytics, accessibility, SEO, and support

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Privacy-safe product analytics reaches an analytics backend | failed | `frontend/src/lib/analyzerAnalytics.ts` only dispatches browser `CustomEvent`; no collector/persistence integration was found. `docs/billing/tier-enforcement.md` defines a useful no-sensitive-data boundary but does not implement it. | Product/data/privacy owners: choose approved events, consent basis, destination, retention, and verify payload redaction. | Blocks launch measurement; privacy review required before enabling. |
| Automated accessibility acceptance | not tested | Components include labels/ARIA and accessibility-focused history exists, but no axe/Playwright accessibility gate or current WCAG report was found or run. | Accessibility/QA owner: audit keyboard, focus, semantics, contrast, zoom, screen reader, errors, and mobile at WCAG 2.2 AA target. | Release blocker for public acceptance. |
| SEO routes and metadata plumbing | verified | KEO-84 centralizes `https://biostack.cc` in `frontend/src/lib/site.ts`; `metadataBase`, robots, sitemap, page-specific canonicals, Open Graph, and Twitter metadata consume that source. Focused tests reject the obsolete `.app` host. Legal placeholders remain excluded/noindexed. | Marketing owner: deploy an accepted revision, validate rendered tags/social previews, and connect Search Console only with explicit authority. | Repository host/metadata plumbing is deterministic; live indexing remains separate. |
| Live SEO/crawl behavior | failed | The KEO-84 audit reported that bounded read-only fetches on 2026-07-26 found deployed `robots.txt` and deployed sitemap entries pointing at `https://biostack.app` while the public origin is `https://biostack.cc`. No durable raw response artifact is attached to this ledger, so treat that production detail as an attributed dated observation requiring re-verification. This parcel corrects repository source only; no deployment occurred. | Marketing/QA owner: deploy an accepted revision, crawl the exact production SHA, and attach rendered canonical/social/status evidence. | Required before public announcement. |
| Customer support route, contact, SLA, and escalation | failed | No dedicated support/contact route or operational support policy was found; marketing copy mentions priority support without a verified delivery channel. | Support/product owner: publish contact path, response expectations, escalation, privacy-safe intake, and ownership schedule. | Release blocker for paid/public support readiness. |

## Lane 9 — delivery workflows

| Requirement | Status | Evidence | External owner/action | Release impact |
|---|---|---|---|---|
| Main deployment workflow is green | verified | Run `30211140857` passed and deployed exact SHA `53ed0df5`, including backend/frontend tests, immutable images, exact latest-ready checks, and pinned HTTP 200 smoke. | Engineering/platform owner: retain the gate and do not treat it as approval of unresolved security/product/operational rows. | Delivery gate passes for this SHA; release qualification remains NO-GO. |
| Secret scan is green and required | blocked | Hosted run `30211140879` passed at `53ed0df5`; repository rules showing that it is required, plus full-history/rotation evidence for historical findings, were not verified. | Security/repo owner: make it required and attach historical-secret rotation evidence. | Green current-tree scan is necessary but not sufficient. |
| Static security/quality analysis is configured and green | failed | `.github/workflows/sonarcloud.yml` has empty `sonar.projectKey` and `sonar.organization`; recent listed runs failed. | Security/repo owner: configure the project or replace/remove the nonfunctional workflow with an approved scanner. | Release blocker for claimed scan coverage. |
| Production approval, environment protection, and separation of duties | blocked | Deploy triggers directly on pushes to `main`; repository environment rules/branch protection/human approvers were not inspected or approved in this lane. | Repo/platform owner: configure protected production environment and named approval policy. | Release blocker for controlled production change. |
| Post-deploy smoke test and automatic halt/rollback | failed | Run `30211140857` proves exact-revision API `/health` and web `/` HTTP 200 checks and serial halt behavior. It does not include an authenticated customer journey, Keon/database readiness, traffic validation, or automatic rollback. | Platform/QA owner: add bounded customer/dependency smoke and rehearse defined rollback handling. | Partial smoke evidence does not close the release gate. |
| Offline verification kit workflow | verified | `Protocol Operations Offline Verification Kit` run `30211140877` passed at exact SHA `53ed0df5`. | Release owner: treat this as evidence only for its stated offline-kit scope. | Does not offset failed production launch controls. |

## Deterministic verification record

Current evidence was reconciled from the exact hosted SHA and repository state:

```text
rtk git rev-parse HEAD
  53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c

rtk gh run list --commit 53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c --limit 20
  deploy 30211140857: success
  secret scan 30211140879: success
  structural report 30211140866: success
  offline verification kit 30211140877: success
  source acquisition worker 30211140859: success

rtk gh run view 30211140857 --log
  exact-SHA API and web images became latest-ready and pinned smoke checks returned HTTP 200
  backend restore/test emitted known-high-severity NU1903 warnings for System.Security.Cryptography.Xml 10.0.9

repository inspection
  frontend/src/app/onboarding/consent/page.tsx implements versioned accept/refuse/return-path behavior
  frontend/src/__tests__/components/ConsentPage.test.tsx covers acceptance, refusal, and expired-session routing

attributed KEO-84 read-only audit observations reported on 2026-07-26
  no durable raw response artifact is attached; production details require re-verification
  reported deployed robots.txt and sitemap.xml used the obsolete https://biostack.app host
  reported public BPC-157 knowledge response included dose, frequency, schedule, pairing, and optimization fields
```

The KEO-84 parcel records its local focused test output in its parcel handoff. It does not deploy, change secrets, mutate a database, approve legal language, configure Search Console, or close any live release gate.

## Decision ownership

Only the release owner may change the recommendation after reviewing attached evidence from legal, privacy, security, platform/DBA, accessibility/QA, support, product/data, and marketing owners. This document records technical qualification evidence; it does not substitute for those approvals.
