# Security Review — BioStack

**Date:** 2026-10-14
**Reviewer:** independent security-auditor (did not implement the reviewed code)
**Depth:** Standard
**Mode:** Local source plus passive public observation
**Coordinator disposition:** accepted as blocking evidence. No finding was reproduced by exploit. No waiver.

## Executive summary

BioStack is not ready to treat its security gates as passed. Current source has real controls: HTTP-only cookies, server-side session checks, atomic magic-link consumption, signed Stripe webhook verification, price allow-listing, owner-scoped profile access, and a public knowledge serializer that omits named dose and schedule fields. A passive read of `https://api.biostack.cc/api/v1/knowledge/compounds/BPC-157` on 2026-10-14 returned observational keys and none of the sixteen withheld action fields.

Those controls do not close the release gates. Production still serves an unauthenticated OpenAPI document and Scalar UI. Auth rate limits key only on the direct socket address. Public lead intake stores unbounded email text with no rate limit. Logging has no redaction policy. Terms and privacy pages still say legal review is required. Live auth, billing lifecycle, secret rotation, backup, and abuse-control evidence were not retested and are not passes.

**Critical/High findings are not resolved.** No Critical finding was proven. High findings F-001, F-002, and F-003 remain open.

## Findings summary

| ID | Severity | Title | Locator | Framework |
|---|---|---|---|---|
| F-001 | High | Production API schema and Scalar UI are anonymous | `backend/src/BioStack.Api/Program.cs:566` | OWASP API9:2023 / CWE-200 |
| F-002 | High | Auth rate limits do not identify the real client | `backend/src/BioStack.Api/Program.cs:192` | OWASP API4:2023 / CWE-770 |
| F-003 | High | Public lead intake is unbounded and stores raw email | `backend/src/BioStack.Api/Endpoints/LeadEndpoints.cs:15` | OWASP API4:2023 / CWE-770 |
| F-004 | Medium | Logs can retain contact addresses and raw magic links | `backend/src/BioStack.Api/Endpoints/ContactEndpoints.cs:69` | OWASP A09:2021 / CWE-532 |
| F-005 | Medium | Public legal policies still require human legal review | `frontend/src/app/terms/page.tsx:18` | SG7 |
| F-006 | Medium | Webhook idempotency is check-then-insert | `backend/src/BioStack.Application/Services/BillingService.cs:155` | OWASP API6:2019 / CWE-362 |
| F-007 | Medium | Azure deploy script enables ACR admin credentials | `infra/azure/deploy-container-apps.ps1:108` | OWASP A05:2021 / CWE-250 |
| F-008 | Low | Production responses lack baseline security headers | `backend/src/BioStack.Api/Program.cs:561` | OWASP A05:2021 / CWE-693 |
| F-009 | Low | JWT signing key length is not fail-closed | `backend/src/BioStack.Api/Program.cs:234` | OWASP A02:2021 / CWE-326 |
| F-010 | Low | Bearer JWTs are not bound to revocable sessions | `backend/src/BioStack.Application/Services/JwtTokenService.cs:31` | OWASP A07:2021 / CWE-613 |
| F-011 | Informational | Public knowledge boundary holds in current source and one live sample | `backend/src/BioStack.Contracts/Responses/KnowledgeEntryResponse.cs:18` | OWASP API3:2023 |
| F-012 | Informational | Consent is enforced on mapped writes, but wording is unapproved | `backend/src/BioStack.Application/Services/ConsentGate.cs:11` | SG7 |

## High findings (blocking)

### F-001: Production API schema and Scalar UI are anonymous

- **CVSS-lite:** AV:N/AC:L/PR:N/UI:N/S:U/C:L/I:N/A:N
- **Confidence:** High
- **Locator:** `backend/src/BioStack.Api/Program.cs:566-571`; live `https://api.biostack.cc/openapi/v1.json` and `https://api.biostack.cc/scalar/v1`
- **Evidence:** No development-only guard around `MapOpenApi()` or `MapScalarApiReference()`. Passive GET on 2026-10-14 returned HTTP 200 for both. The OpenAPI document was about 92 KB.
- **Impact:** Current route and schema inventory, including admin, billing, auth, and provider paths, is public.
- **Remediation:** Do not map OpenAPI or Scalar in Production. If operators need them, require admin authentication and a non-public network path. Confirm the deployed revision returns 404 for both URLs.

### F-002: Auth rate limits do not identify the real client

- **CVSS-lite:** AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:L/A:L
- **Confidence:** High
- **Locator:** `backend/src/BioStack.Api/Program.cs:192-210`; `backend/src/BioStack.Api/Endpoints/AuthEndpoints.cs:49-55`
- **Evidence:** `auth-start` and `auth-verify` partition only on `Connection.RemoteIpAddress`. `UseForwardedHeaders` does not appear in the API source. Windows are 5 starts and 10 verifies per 10 minutes.
- **Impact:** Clients behind one ingress address can share one bucket and lock out sign-in, or a distributed caller can stay under a per-address cap. Live ingress behavior was not tested and is not a pass.
- **Remediation:** Rate-limit on the platform’s trusted client identity. Add a per-email send budget. Prove deployed behavior with bounded tests.

### F-003: Public lead intake is unbounded and stores raw email

- **CVSS-lite:** AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:L/A:L
- **Confidence:** High
- **Locator:** `backend/src/BioStack.Api/Endpoints/LeadEndpoints.cs:15-49`
- **Evidence:** `POST /api/v1/leads/capture` is anonymous, has no rate-limit policy, and does not validate email format or length before insert. This was not actively exercised.
- **Impact:** An anonymous caller can fill `LeadCaptures` and create storage and privacy-processing load.
- **Remediation:** Require email validation, length limits, honeypot, and the provider-intake rate limit. Reject unknown sources. Add an abuse test before treating the provider/privacy gate as closed.

## Medium findings (blocking by default)

### F-004: Logs can retain contact addresses and raw magic links

Locator: `ContactEndpoints.cs:67-69`, `MagicLinkDelivery.cs:57`, `ContactMessageSender.cs:60-63`, `Program.cs:62-63`. No redaction policy. Remove token and email values from logs and add a regression.

### F-005: Public legal policies still require human legal review

Locator: `frontend/src/app/terms/page.tsx:18-26`, `frontend/src/app/privacy/page.tsx:18-26`. This review is not counsel approval. Dated human approval is required before either page is authoritative.

### F-006: Webhook idempotency is check-then-insert

Locator: `BillingService.cs:153-238`. Signature verification and price quarantine exist. Concurrent deliveries can both pass the read before the unique `StripeEventId` insert. Insert the receipt before side effects, or catch the unique violation.

### F-007: Azure deploy script enables ACR admin credentials

Locator: `infra/azure/deploy-container-apps.ps1:108-122`. Disable ACR admin and pull with managed identity. Source intent is not a live-registry pass.

## Low and informational

F-008 missing baseline security headers. F-009 JWT key length is not fail-closed. F-010 bearer JWTs are not bound to revocable sessions; production issuance was not proven. F-011 one live BPC-157 response omitted the sixteen withheld action fields; free-text `notes` and `benefits` still need content review, and one GET is not catalog coverage. F-012 consent is enforced on mapped writes as `bio-observational-v1`; the wording is unapproved.

## Coverage gaps

No authenticated live sign-in, expiry, replay, or logout test. No proof of deployed client-IP identity. No Stripe lifecycle or replay evidence in this session. No inspection of deployed secrets, historical rotation, backup, restore, monitoring, or rollback. No full catalog crawl for dosing language in free text. No dependency rescan. SonarCloud project keys are empty. Repository environment-protection rules were not inspected.

Unverified live controls are not passes.

## Recommended next steps

1. Disable production OpenAPI and Scalar, then confirm both URLs return 404.
2. Fix client identity for rate limits and add a per-account magic-link send budget.
3. Add validation and rate limiting to lead capture.
4. Remove email addresses and magic links from logs.
5. Obtain dated human legal approval. This review does not provide it.
6. Close webhook concurrency and complete the billing lifecycle matrix.
7. Disable ACR admin credentials.
