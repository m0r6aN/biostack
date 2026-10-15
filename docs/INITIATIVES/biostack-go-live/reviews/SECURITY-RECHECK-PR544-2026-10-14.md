# Security re-check — PR 544

**Date:** 2026-10-14
**Reviewer:** independent security-auditor (did not write `b3cdde20323f554f0519dc57bfc1c1395241fbc8`)
**Commit:** `b3cdde20323f554f0519dc57bfc1c1395241fbc8`
**Tree reviewed:** `5b686e69536f2eedf0195ee77466f20b08a490d8` (ancestor check passed; later commits on this branch are documentation). `origin/main` is `9550964256d507d03b789df8f0eb4c448a6f7ac8` and also contains the commit.
**Prior review:** `docs/INITIATIVES/biostack-go-live/reviews/SECURITY-REVIEW-2026-10-14.md`
**Mode:** Current source plus repeated unauthenticated GETs. No Stripe call. No POST, PUT, PATCH, or DELETE. No live charge. No waiver.

Medium findings stay blocking. F-005 is counsel-owned and is not resolved. A source status is not a production pass. Nothing below is accepted risk.

## Production anchor

Last successful Deploy to Azure Container Apps run is `38136962345` at `6c9d8ecc` on 2026-10-11. That commit is the parent of the fix, so the running revision does not contain it.

Runs `38602773660` (PR 544) and `38603798795` (PR 545) failed the high-severity NuGet audit before Azure login. `.github/workflows/deploy.yml:59-61` runs that audit; Azure login is later at `.github/workflows/deploy.yml:102-109`. The reported advisories were `System.Security.Cryptography.Pkcs` GHSA-846f-f764-76mx and `System.Security.Cryptography.Xml` GHSA-7hqp-7c5m-4vq6, GHSA-c399-9853-6jmj, and GHSA-r955-8q5j-3h83. This re-check did not re-run the audit. The tree still directly references `System.Security.Cryptography.Xml` 10.0.10 in `backend/src/BioStack.Infrastructure/BioStack.Infrastructure.csproj:19` and `backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj:22`. Pkcs is not pinned here.

Repeated unauthenticated GETs on 2026-10-14:

- `GET https://api.biostack.cc/openapi/v1.json` returned 200, 92651 bytes, in 30.0 seconds.
- `GET https://api.biostack.cc/scalar/v1` returned 200, HTML titled BioStack Mission Control API.

## Findings

| ID | source status | production status | evidence |
|---|---|---|---|
| F-001 | source-addresses-finding | production-open | `backend/src/BioStack.Api/Program.cs:579-587` maps OpenAPI and Scalar only when `IsProduction()` is false. Live GET still returns 200 for both URLs. Deployed sha `6c9d8ecc` predates the guard. OWASP API9:2023 / CWE-200. |
| F-002 | source-partial | production-open | `Program.cs:193-210` still keys auth-start (5/10 min) and auth-verify (10/10 min) on `ClientIpResolver.PartitionKey`. `ClientIpResolver.cs:12-18` and `:77-80` take the first `X-Forwarded-For` hop when the socket peer is a configured proxy, loopback, or any 10/8, 172.16/12, 192.168/16, or 100.64/10 address (`ClientIpResolver.cs:36-59`). `ClientIpResolverTests.cs:24-44` locks in leftmost-wins. `AuthStartAccountBudget` (`Program.cs:244`, `AuthEndpoints.cs:101-104`, `AuthStartAccountBudget.cs:6-21`) allows 3 sends per 30 minutes per email inside one process only, and does not cover verify. OWASP API4:2023 / CWE-770. |
| F-003 | source-partial | production-open | `LeadEndpoints.cs:11-18` and `:35-71` add a 254-character email check, source allow-list, and honeypot. `LeadEndpoints.cs:26` and `Program.cs:233-240` add 5/hour on the same partial partition key. `LeadEndpoints.cs:56-60` still stores the normalized email. Distinct addresses are not capped once the partition key can be chosen. OWASP API4:2023 / CWE-770. |
| F-004 | source-partial | production-open | Log templates at `MagicLinkDelivery.cs:59`, `:123`, and `:177`, `ContactMessageSender.cs:62-65`, and `ContactEndpoints.cs:70` pass `LogRedaction.EmailDomain` (`LogRedaction.cs:5-18`), not the raw link. The domain is still written. `ContactEndpoints.cs:70` also logs the exception object, which can carry the recipient. `LogRedactionTests.cs:8-13` checks the helper only. OWASP A09:2021 / CWE-532. |
| F-005 | source-open | production-open | `frontend/src/app/terms/page.tsx:18-26` and `frontend/src/app/privacy/page.tsx:18-26` still say legal review is required and that the copy is not final. This commit does not change them. This re-check is not counsel approval and does not resolve F-005. SG7. |
| F-006 | source-partial | production-open | `BillingService.cs:155-218` inserts the receipt before side effects and catches `DbUpdateException`. `BioStackDbContext.cs:498` and migration `20260422125251_RecoverBillingTierEnforcement.cs:695-699` keep `StripeEventId` unique. `BillingService.cs:194-198` deletes that receipt on any other exception after handlers may already have saved (`BillingService.cs:326`, `:396`, `:416`), so a later delivery can claim the same event. Tests seen are sequential, not concurrent, and were not run. OWASP API6:2019 / CWE-362. |
| F-007 | source-partial | production-open | `infra/azure/deploy-container-apps.ps1:108` now passes `--admin-enabled false`, and `:118` and `:221` use `--registry-identity`. The script has no `az acr update --admin-enabled false` and no `AcrPull` assignment. `infra/azure/source-acquisition-acr-transition.bicep:39` still sets `adminUserEnabled: true`. `verify-source-acquisition-network-foundation.ps1:74-76` and `:165` fail unless admin stays enabled. Live registry state was not queried. `.github/workflows/deploy.yml` updates images and does not disable admin. OWASP A05:2021 / CWE-250. |

## Finding notes

### F-001

The source guard matches the remediation that Production must not map the documents. It is only as strong as `ASPNETCORE_ENVIRONMENT=Production` (`deploy-container-apps.ps1:141`). It is not admin authentication and not a private network path. Production still serves the anonymous schema and Scalar UI, so F-001 remains blocking.

### F-002

Trusting every private and CGNAT peer, then the leftmost forwarded address, does not implement a platform trusted-client identity. If the ingress appends `X-Forwarded-For`, the caller chooses the bucket. The account budget reduces magic-link volume on a single instance and resets on restart or another replica. Auth verify has no account budget. Deployed client-IP behavior was not tested.

### F-003

Format, length, known source, honeypot, and a rate-limit policy are present in source. The rate limit uses the F-002 partition, so unbounded distinct-email inserts remain possible if that key is caller-controlled. Storing the lead email is still the write path.

### F-004

Full magic-link URLs are no longer arguments to the cited log templates. That does not meet "remove email values": the domain remains, and the contact failure log includes `ex`. Development inbox memory still holds the raw link (`MagicLinkDelivery.cs:53`); that object is not the production log line.

### F-005

Not resolved. Dated human legal approval is still required before either page is authoritative.

### F-006

Concurrent check-then-insert is narrower while the receipt row exists. It is not closed across the failure path that removes the row after partial subscription or invoice saves. No live Stripe replay was performed.

### F-007

Greenfield `acr create` no longer requests an admin user. An existing registry is not turned off by this script, and the network-foundation template still preserves admin. Source intent is not a live-registry pass.

## Checkout plan claim

`frontend/src/lib/entitlements.tsx:63-65` passes the selected `plan` to `createCheckoutSession`. The declared `returnPath` argument at `:41` is not used. `frontend/src/app/pricing/pricing-client.tsx:39-42` and `:98-100` send `operator` or `commander` from the public pricing buttons. `frontend/src/app/billing/page.tsx:96`, `:228`, and `:255` send the same plan codes from the signed-in billing page through that page's own checkout function.

Tests read:

- `frontend/src/__tests__/lib/entitlements.test.tsx:46-58` asserts both plans through a harness, not through the billing page.
- `frontend/src/__tests__/app/pricing-client.test.tsx:53-65` asserts Operator and Commander on the public pricing path.
- `frontend/src/__tests__/app/BillingPageIntent.test.tsx:76-91` and `:159-175` assert a successful Operator session. `:101-113` only preserves `plan=commander` when authentication fails. No test asserts that signed-in Commander checkout calls `createCheckoutSession` with `commander`.

Command run from `frontend` under Node v24.7.0 (package engines ask for Node >=26.8.2):

`npm test -- src/__tests__/app/pricing-client.test.tsx src/__tests__/lib/entitlements.test.tsx src/__tests__/app/BillingPageIntent.test.tsx`

Result: 3 files passed, 9 tests passed. An earlier vitest invocation from the repository root did not load `frontend/vitest.config.ts`, failed alias resolution, and is not evidence. Backend security tests were read and not run. No checkout session was created against Stripe.

## After a future deploy

An independent reviewer still needs to re-check after a future deploy. The revision that reaches Azure must be shown to return 404 for both public schema URLs, to rate-limit on the ingress's real client identity, to keep ACR admin disabled if that is the deployed control, and to have passed the dependency gate that stopped runs `38602773660` and `38603798795`. F-005 still requires counsel, not another source read.

source re-check is not a go-live verdict.
