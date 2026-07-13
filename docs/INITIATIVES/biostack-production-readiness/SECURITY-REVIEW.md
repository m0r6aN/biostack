# Defensive Security Review — KEO-65

## Verdict

**HOLD / not release-ready** for commit `2bdb7ba`.

The current tree fails closed for production application secrets and has useful authentication, authorization, webhook-signature, ownership, and deployment-identity controls. Release remains blocked by four high-severity findings, incomplete historical secret closeout, and several medium hardening items. No production secret value is reproduced in this report.

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
- Status: **remediation implemented in isolated parcel; integration pending**
- Evidence: `backend/src/BioStack.Api/Endpoints/ReceiptEndpoints.cs`, `backend/src/BioStack.Infrastructure/Governance/SpineRepository.cs`, and authenticated frontend receipt consumers.
- Impact: unauthenticated callers can retrieve receipt metadata by URI or enumerate by subject/actor, exposing tenant, actor, subject, evidence-reference, and integrity metadata across users.
- Required remediation: require authentication; scope non-admin reads to `ReceiptActor.User(currentUserId)`; return `404` for direct cross-user lookup; retain explicit admin investigation access.
- Parcel: `SEC-RECEIPT-001` in `D:/Repos/BioStack-sec-receipts`.

### SR-02 — Link analyzer can be used for SSRF and unbounded response consumption

- Severity: **High**
- Status: **open**
- Evidence: `backend/src/BioStack.Application/Services/ProtocolIngestionService.cs` (`LinkProtocolExtractor`).
- Impact: an authenticated caller can submit an HTTPS URL that resolves or redirects to private/loopback/link-local infrastructure. The default client follows redirects and buffers the full response without a response-size limit, enabling internal reachability probes and memory/resource exhaustion.
- Required remediation: resolve and reject private, loopback, link-local, multicast, and metadata-service targets on every redirect; use a dedicated client with redirect control; stream with a strict byte ceiling and content/time limits; add DNS rebinding and redirect-chain tests.

### SR-03 — Provider-access request endpoint leaks state and permits unauthorized reopen/overwrite

- Severity: **High**
- Status: **open**
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
- Status: **open**
- Evidence: `backend/src/BioStack.Api/Endpoints/AuthEndpoints.cs`.
- Impact: concurrent verification requests can observe the same unconsumed challenge before persistence and each issue a valid session.
- Required remediation: consume with a conditional database update/transaction and require exactly one affected row before issuing a session; add a concurrent replay test.

### SR-06 — Known moderate PostCSS vulnerability in frontend dependency graph

- Severity: **Medium**
- Status: **open**
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
- Status: **open**
- Evidence: consent acceptance/gate services accept and persist the requested version, while authorization verifies acceptance exists rather than matching the server-required consent version.
- Impact: a client may create evidence for a version that was not the currently required disclosure.
- Required remediation: select the required consent document/version server-side, bind acceptance to its immutable hash, and require that version/hash at the gate.

### SR-09 — Backend production container runs as root

- Severity: **Medium**
- Status: **open**
- Evidence: backend Dockerfile does not declare a non-root runtime user.
- Impact: a successful process compromise has unnecessary container privileges.
- Required remediation: create/use an unprivileged runtime user, make only required paths writable, retain a read-only filesystem where possible, and verify health/startup behavior.

### SR-10 — Public lead capture lacks abuse controls

- Severity: **Low**
- Status: **open**
- Evidence: public lead endpoint lacks a dedicated rate limit and strong input normalization/validation.
- Impact: database spam and operational noise.
- Required remediation: add a bounded request policy, strict length/email validation, duplicate handling, and monitoring without leaking registration state.

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
| Focused auth, billing, provider-access, ownership, and consent security tests | 29 passed; 4 build warnings |
| `.NET` vulnerable package audit with transitive dependencies | No vulnerable packages reported across solution projects |
| Frontend production dependency audit | 2 moderate findings in one PostCSS advisory chain |
| Current-tree secret scan | One documented phrase false positive; no confirmed current secret value |
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

- SG1 authentication/session: **blocked** by SR-05.
- SG2 authorization/ownership: **blocked** until SEC-RECEIPT-001 integrates and SR-03 closes.
- SG3 input/egress safety: **blocked** by SR-02.
- SG4 secrets: **blocked** by SR-04 and incomplete full-history scan.
- SG5 dependency/container hardening: **blocked** by SR-06 and SR-09.
- SG6 operational controls: **blocked pending** SR-07 deployed-path verification.
- SG7 evidence/closeout: **blocked** until remediation commits, hosted scans, rotation evidence, and retests are attached.

## Owner / External Actions

These do not block continued local remediation but do block release:

1. Rotate/invalidate the historical callback credential in its receiving system and record date/system/operator evidence without the value.
2. Approve the Git-history treatment after confirming distribution scope.
3. Provide or authorize a hosted full-history secret-scan run if the local container remains unable to complete.
4. Confirm the production proxy topology/known proxy ranges for forwarded-header configuration.

