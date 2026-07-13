# Defensive Security Review — KEO-65

## Verdict

**HOLD / not release-ready** for hosted remediation candidate `c96bc3b` (verified application/dependency state `fb0ed84`; review baseline `2bdb7ba`).

The candidate remediates and integrates SR-01, SR-02, SR-03, SR-05, SR-06, SR-08, and SR-09. On immutable SHA `c96bc3b`, hosted backend/frontend validation, production dependency audit, production frontend build, offline verification, and current-tree Gitleaks all pass. Release remains blocked by historical secret rotation and full-history closeout (SR-04), deployed proxy identity verification (SR-07), live-environment evidence, and the broader release gates. No production secret value is reproduced in this report.

## Scope and Method

Reviewed on 2026-07-13:

- API authentication, session issuance/consumption, role authorization, CORS, and rate limiting.
- Receipt, analyzer/link ingestion, provider access, billing/webhook, lead, consent, and admin endpoints.
- Frontend auth middleware and authenticated receipt consumers.
- Dockerfiles, compose, deployment scripts, GitHub Actions, and dependency manifests.
- Current working tree plus a targeted pre-remediation secret scan.

This was a defensive source/configuration review with focused automated tests and dependency/secret scanners. It was not a penetration test, production traffic test, cloud-control-plane audit, or proof that historical credentials have been invalidated.

## Threat Model

### Protected assets

- User identity, session cookies, subscription state, consent evidence, protocol/check-in/profile data.
- Decision receipts, subject identifiers, actor identifiers, evidence references, and integrity hashes.
- Provider-access requests and contact information.
- Stripe webhook integrity and deployment credentials.
- Knowledge-ingest governance boundaries and administrative review state.

### Trust boundaries and entry points

1. Browser to Next.js frontend and ASP.NET API over HTTPS.
2. Authenticated user to actor-owned data and governed receipt evidence.
3. Administrator to provider, knowledge-ingest, and governance operations.
4. API to untrusted remote URLs submitted to the link analyzer.
5. Stripe to the public billing webhook.
6. GitHub Actions to Azure through workload identity.
7. Local/development compose and scripts to databases and registries.

### Primary attacker capabilities

- Anonymous internet requests, including replay and enumeration.
- Authenticated low-privilege requests with attacker-controlled URLs and identifiers.
- Concurrent requests against one-time authentication material.
- Supply-chain exploitation of known vulnerable packages.
- Use of credentials exposed in repository history until independently rotated.

## Findings

### SR-01 — Receipt API permits public and cross-user evidence disclosure

- Severity: **High**
- Status: **remediated and integrated locally**
- Evidence: `backend/src/BioStack.Api/Endpoints/ReceiptEndpoints.cs`, `backend/src/BioStack.Infrastructure/Governance/SpineRepository.cs`, and authenticated frontend receipt consumers.
- Impact: unauthenticated callers can retrieve receipt metadata by URI or enumerate by subject/actor, exposing tenant, actor, subject, evidence-reference, and integrity metadata across users.
- Required remediation: require authentication; scope non-admin reads to `ReceiptActor.User(currentUserId)`; return `404` for direct cross-user lookup; retain explicit admin investigation access.
- Parcel: `SEC-RECEIPT-001` in `D:/Repos/BioStack-sec-receipts`.

### SR-02 — Link analyzer can be used for SSRF and unbounded response consumption

- Severity: **High**
- Status: **remediated and integrated locally**
- Evidence: `backend/src/BioStack.Application/Services/ProtocolIngestionService.cs` (`LinkProtocolExtractor`).
- Impact: an authenticated caller can submit an HTTPS URL that resolves or redirects to private/loopback/link-local infrastructure. The default client follows redirects and buffers the full response without a response-size limit, enabling internal reachability probes and memory/resource exhaustion.
- Required remediation: resolve and reject private, loopback, link-local, multicast, and metadata-service targets on every redirect; use a dedicated client with redirect control; stream with a strict byte ceiling and content/time limits; add DNS rebinding and redirect-chain tests.

### SR-03 — Provider-access request endpoint leaks state and permits unauthorized reopen/overwrite

- Severity: **High**
- Status: **remediated and integrated locally**
- Evidence: `backend/src/BioStack.Api/Endpoints/ProviderAccessEndpoints.cs`.
- Impact: an anonymous caller who knows an email address can learn request state/identifiers and, for a closed request, overwrite submitted attributes, clear ownership, and reopen the workflow. IP rate limiting reduces volume but does not establish authority.
- Required remediation: return a uniform non-enumerating acknowledgement; require a verified ownership challenge before returning or mutating an existing request; make reopen an authenticated/admin or token-bound transition; preserve immutable audit history.

### SR-04 — Historical callback secret requires rotation and history closeout

- Severity: **High**
- Status: **open; owner/external action required**
- Evidence: a redacted scanner finding in the parent of the current remediation commit identified a callback secret in `backend/src/BioStack.Api/appsettings.json`. The current tree no longer contains the active value. A second scanner hit in `docs/product/knowledge-engine-capability-map.md` is a documented phrase false positive.
- Impact: removing a secret from the current tree does not invalidate copies in Git history, clones, caches, logs, or the receiving system.
- Required remediation: identify the receiving integration, rotate/invalidate the credential, record rotation evidence without the value, decide whether history rewrite is necessary, add an exact-path/rule allowlist for the documented false positive, and complete a full-history hosted scan.
- Verification limitation: the local full-history container scan timed out; therefore history is **unverified**, not clean.

### SR-05 — Magic-link consumption is not atomic

- Severity: **Medium**
- Status: **remediated and integrated locally**
- Evidence: `backend/src/BioStack.Api/Endpoints/AuthEndpoints.cs`.
- Impact: concurrent verification requests can observe the same unconsumed challenge before persistence and each issue a valid session.
- Required remediation: consume with a conditional database update/transaction and require exactly one affected row before issuing a session; add a concurrent replay test.

### SR-06 — Known moderate PostCSS vulnerability in frontend dependency graph

- Severity: **Medium**
- Status: **remediated and integrated locally**
- Evidence: `npm audit --omit=dev --audit-level=moderate` reports GHSA-qx2v-qp2m-jg93 through the Next.js dependency graph; the lockfile contains an affected nested PostCSS version.
- Impact: crafted CSS input in an affected processing path can trigger incorrect parsing behavior. Practical exposure depends on whether untrusted CSS is processed at runtime/build time.
- Required remediation: update the supported Next.js dependency graph or apply a reviewed package override to PostCSS `>=8.5.10`, then rebuild and rerun focused UI tests/audit.

### SR-07 — Rate-limit identity is not proxy-aware

- Severity: **Medium**
- Status: **open / deployment behavior requires verification**
- Evidence: API rate-limit partitioning uses `RemoteIpAddress`; forwarded-header processing was not identified before the limiter.
- Impact: behind a reverse proxy, unrelated clients may share one partition (availability risk), or an incorrectly trusted forwarding configuration may make limits bypassable.
- Required remediation: configure known proxies/networks and forwarded headers before rate limiting; verify the effective client identity in the deployed Azure path.

### SR-08 — Consent acceptance trusts an arbitrary client-provided version

- Severity: **Medium**
- Status: **remediated and integrated locally**
- Evidence: consent acceptance/gate services accept and persist the requested version, while authorization verifies acceptance exists rather than matching the server-required consent version.
- Impact: a client may create evidence for a version that was not the currently required disclosure.
- Required remediation: select the required consent document/version server-side, bind acceptance to its immutable hash, and require that version/hash at the gate.

### SR-09 — Backend production container runs as root

- Severity: **Medium**
- Status: **remediated and integrated locally**
- Evidence: backend Dockerfile does not declare a non-root runtime user.
- Impact: a successful process compromise has unnecessary container privileges.
- Required remediation: create/use an unprivileged runtime user, make only required paths writable, retain a read-only filesystem where possible, and verify health/startup behavior.

### SR-10 — Public lead capture lacks abuse controls

- Severity: **Low**
- Status: **open**
- Evidence: public lead endpoint lacks a dedicated rate limit and strong input normalization/validation.
- Impact: database spam and operational noise.
- Required remediation: add a bounded request policy, strict length/email validation, duplicate handling, and monitoring without leaking registration state.

## Integrated Local Remediation

| Finding / lane | Integrated commit |
|---|---|
| SR-01 receipt authorization | `cf0b6bc` |
| SR-02 analyzer egress | `e0b8e2c` |
| SR-03 provider intake | `84766a9` |
| SR-05 magic-link atomicity | `62d941b` |
| SR-06 PostCSS dependency | `fb0ed84` |
| SR-08 consent version | `8fca351` |
| SR-09 backend container user | `afb3ff9` |
| KEO-64 frontend validation repair | `44eac22` |
| KEO-64 offline-verification fetch repair | `565805a` |
| KEO-64 PR-only hosted validation | `fe9184b` |
| Current-tree Gitleaks allowlist closeout | `c96bc3b` |

`fb0ed84` is the exact application/dependency state used for the full local test, audit, and production-build evidence. Hosted runs `29283101748`, `29283101730`, and `29283101738` pass on `c96bc3b`. The hosted Gitleaks result scans the current checkout; it is not proof that the historical credential was rotated or that full Git history is clean.

## Positive Controls Confirmed

- Session cookie is `HttpOnly`; production configuration uses `Secure` and `SameSite=Lax`.
- JWT validation checks issuer, audience, signature, and lifetime; session records are revalidated.
- Admin endpoints use an explicit `AdminOnly` policy.
- Stripe webhook processing requires signature verification and fails closed without a configured secret.
- Unknown Stripe price mappings fail closed to the observer tier.
- Current production secret placeholders are blank/fail closed rather than containing a usable credential.
- Deployment workflow uses GitHub/Azure workload identity rather than a stored publish profile.
- Existing ownership-isolation and consent-gate tests passed in the focused security baseline.
- Frontend production container declares a non-root user.
- Canonical knowledge ingest has an authenticated administrative boundary and explicit override fencing.

## Verification Evidence

| Check | Result |
|---|---|
| Focused API and application security tests | 57 passed |
| Full backend regression suite | 1,088 passed across 5 projects |
| Backend solution build | 15 projects; 0 errors; 4 pre-existing xUnit analyzer warnings |
| `.NET` vulnerable package audit with transitive dependencies | No vulnerable packages reported across solution projects |
| Full frontend regression suite | 125 files; 900 tests passed |
| Frontend production build | Passed TypeScript and generated 51 static pages |
| Frontend production dependency audit | Zero vulnerabilities; nested Next.js PostCSS resolved to 8.5.10 |
| Hosted candidate validation | Run `29283101748` passed on `c96bc3b`; backend, install, audit, frontend tests, and production build passed; Azure/image/deploy steps skipped |
| Hosted offline verification | Run `29283101730` passed on `c96bc3b` |
| Hosted current-tree secret scan | Run `29283101738` passed on `c96bc3b` after a narrow prose/generated-artifact allowlist |
| Pre-remediation tree secret scan | One confirmed callback secret plus the documented phrase false positive |
| Full-history secret scan | Timed out; unverified |

## Abuse Cases Required Before Release

- Anonymous and cross-user receipt lookup/enumeration.
- Analyzer URLs resolving to loopback, RFC1918, link-local, IPv6 local, cloud metadata, and a public-to-private redirect.
- Analyzer response exceeding the byte ceiling and a slow response timeout.
- Provider request email enumeration, replay, and closed-request reopen attempts.
- Two concurrent consumers for one magic-link challenge.
- Spoofed/absent forwarded headers through the real Azure proxy chain.
- Consent acceptance for a stale, future, or invented version.

## Release Gate Mapping

- SG1 authentication/session: **local remediation passed**; live auth-flow evidence remains required.
- SG2 authorization/ownership: **local remediation passed**; hosted/live evidence remains required.
- SG3 input/egress safety: **local remediation passed**; hosted/live evidence remains required.
- SG4 secrets: **blocked** by SR-04 and incomplete full-history scan.
- SG5 dependency/container hardening: **local remediation passed**; hosted image/workflow evidence remains required.
- SG6 operational controls: **blocked pending** SR-07 deployed-path verification.
- SG7 evidence/closeout: **blocked** until remediation commits, hosted scans, rotation evidence, and retests are attached.

## Owner / External Actions

These do not block continued local remediation but do block release:

1. Rotate/invalidate the historical callback credential in its receiving system and record date/system/operator evidence without the value.
2. Approve the Git-history treatment after confirming distribution scope.
3. Provide or authorize a hosted full-history secret-scan run if the local container remains unable to complete.
4. Confirm the production proxy topology/known proxy ranges for forwarded-header configuration.
