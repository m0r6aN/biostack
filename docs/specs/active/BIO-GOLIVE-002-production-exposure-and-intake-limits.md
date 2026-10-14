---
ticket: BIO-GOLIVE-002
title: Close production schema exposure, auth client identity, and lead-capture bounds
status: review-candidate
owner: clinton.morgan
created: 2026-10-14
updated: 2026-10-14
delivery_classes: [standard, privacy]
guidance_classes: []
substance_function_risk: []
function_review_status: not-applicable
surfaces:
  - backend/src/BioStack.Api/Program.cs
  - backend/src/BioStack.Api/Endpoints/LeadEndpoints.cs
  - backend/src/BioStack.Contracts/Requests/LeadCaptureRequest.cs
  - backend/src/BioStack.Api/Security/ClientIpResolver.cs
  - backend/tests/BioStack.Api.Tests/Security/ProductionSchemaExposureTests.cs
  - backend/tests/BioStack.Api.Tests/Security/AuthClientIdentityRateLimitTests.cs
  - backend/tests/BioStack.Api.Tests/Security/LeadCaptureBoundsTests.cs
  - docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-002-recheck.md
routing_class: implementation/privacy
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: confidential
---

# BIO-GOLIVE-002 — Production schema exposure, auth client identity, and lead-capture bounds

## Objective

Three high findings from `docs/INITIATIVES/biostack-go-live/reviews/SECURITY-REVIEW-2026-10-14.md` remain open. F-001 serves anonymous OpenAPI and Scalar on production `api.biostack.cc`. F-002 partitions sign-in rate limits on the socket address. F-003 accepts unbounded lead email with no rate limit. This parcel closes those three findings in source and names the production re-check a different reviewer must perform. Medium findings F-004 through F-007 stay open and still block launch.

## Initiative (Product Lane)

`biostack-go-live`, next-work item 2. This parcel does not waive F-004 through F-007, does not approve terms or privacy, and does not change the production-readiness NO-GO.

## Branch and Worktree

Proposed only. Gate 2 assigns both. No builder implements this spec while it is a review candidate.

- Branch: `fix/bio-golive-002-exposure-and-limits`
- Worktree: `D:/Repos/BioStack/.worktrees/bio-golive-002-exposure-and-limits`

## Dependencies

- Independent of BIO-GOLIVE-001 and BIO-GOLIVE-003. Different files. Separate worktrees.
- Production confirmation of F-001 is a passive GET after a human deploy. This parcel does not deploy.
- Live ingress trusted-header configuration is an environment fact. Tests must prove the resolver ignores spoofed forwarded headers unless the immediate peer is a configured trusted proxy. A passing unit test is not a production identity proof.

## Verified Facts

1. `Program.cs` calls `MapOpenApi()` and `MapScalarApiReference()` after the production data-protection check, with no environment guard. Resume probe on 2026-10-14: `GET https://api.biostack.cc/openapi/v1.json` returned 200 and 92,651 bytes. `GET https://api.biostack.cc/scalar/v1` returned 200.
2. Policies `auth-start`, `auth-verify`, `provider-access`, and `contact` all partition on `Connection.RemoteIpAddress`. `UseForwardedHeaders` is absent.
3. `POST /api/v1/leads/capture` trims email and source, rejects blanks, and inserts raw email. It has no format check, no length bound, no source allow-list, and no rate-limit policy.
4. `POST /api/v1/provider-access/requests` already uses `RequireRateLimiting("provider-access")`, `MailAddress.TryCreate`, a 255-character email bound, and a honeypot field that is acknowledged and not stored. That endpoint is the pattern to mirror. It is not modified by this parcel.
5. The security reviewer who wrote the 2026-10-14 report did not implement the reviewed code. The reviewer who re-checks this fix must be a different person from the builder.

## Surfaces

- `backend/src/BioStack.Api/Program.cs` — stop mapping OpenAPI and Scalar when the environment is Production. Replace socket-address partitions for `auth-start` and `auth-verify` with the trusted client identity. Add a lead-capture policy and a per-account auth-start send budget.
- `backend/src/BioStack.Api/Security/ClientIpResolver.cs` — new. Resolves the client address from the immediate socket unless that socket is a configured trusted proxy, in which case it uses one configured forwarded header.
- `backend/src/BioStack.Api/Endpoints/LeadEndpoints.cs` — validate, bound, allow-list, and rate-limit capture.
- `backend/src/BioStack.Contracts/Requests/LeadCaptureRequest.cs` — add the honeypot field.
- Three new test files named in Tests.
- `docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-002-recheck.md` — written by the independent reviewer after source re-check, and updated by the coordinator after the human production GET for F-001.

## Contracts

- Non-production keeps OpenAPI and Scalar mapped, so local and test hosts can still read them.
- Production requests to `/openapi/v1.json` and `/scalar/v1` return 404. No admin-authenticated alternative is added in this parcel.
- `auth-start` keeps 5 permits per 10 minutes per trusted client identity, and adds 3 permits per 30 minutes per normalized email. `auth-verify` keeps 10 permits per 10 minutes per trusted client identity. Queue limit stays 0. Rejection stays HTTP 429.
- A missing or untrusted forwarded header never becomes the partition key. If the immediate peer is not in the trusted-proxy allow-list, the socket address is the key even when `X-Forwarded-For` is present.
- Lead capture accepts only sources already used by the product: `clinics-coaches-pricing`. Any other source returns 400 and stores nothing.
- Lead email must pass `MailAddress.TryCreate` and be at most 254 characters. Source must be at most 80 characters. A non-empty honeypot returns 204 and stores nothing.
- Lead capture uses a fixed window of 5 requests per hour per trusted client identity. The stored row remains email, source, and timestamp. No IP address is stored.

## Data Inventory

- Lead email: an email address a visitor submits on the pricing clinic form. Stored in `LeadCaptures` when validation passes and the honeypot is empty.
- Lead source: the allow-listed form identifier `clinics-coaches-pricing`.
- Trusted client address: used only as an in-memory rate-limit partition key. It is not written to `LeadCaptures`, not written to logs by this parcel, and not returned in the response.
- Auth email for the send budget: used only as an in-memory partition key. This parcel does not add a new auth log line.

No new sensitive field is introduced. The lead email field already exists.

## Purpose

Lead email is collected so the operator can reply to a clinic or coach who asked for follow-up from the pricing page. The auth email partition exists to cap magic-link sends to one account. The client-address partition exists to cap anonymous abuse of sign-in and lead capture. Neither partition is a marketing or analytics store.

## Consent

The existing pricing form submits the address the visitor typed. This parcel does not add a new consent ceremony and does not mark the privacy policy approved. A submission that fails validation is not stored. A honeypot submission is not stored.

## Minimization

- Reject malformed and over-long email before insert.
- Reject unknown sources before insert.
- Do not store the client address, user agent, request body, or forwarded header.
- Do not log the email address or the rate-limit partition key.
- The per-account send budget uses the normalized address as a memory key only.

## Retention

This parcel does not change the existing `LeadCaptures` retention behavior. There is still no automated deletion job. That gap remains outside this parcel and does not authorize a new retention design here. The independent reviewer must record that lead rows can still persist until a later privacy parcel defines deletion. This is an accepted residual of F-003's storage finding, not a waiver of F-004 through F-007.

## Export

This parcel adds no export endpoint. A visitor cannot self-export a lead row. Export remains a later privacy parcel. No new data category is created that would require a new export in this change.

## Deletion

This parcel adds no deletion endpoint. Honeypot and invalid submissions are never inserted. A stored lead row remains until an existing operator process or a future parcel deletes it. This parcel must not add a production delete script.

## Access Control

- OpenAPI and Scalar are unreachable in Production, including to an anonymous caller.
- Lead capture stays anonymous and becomes rate-limited. Stored rows stay on the existing database access path. This parcel adds no public read endpoint for `LeadCaptures`.
- Rate-limit partition keys are visible only to the API process memory.

## Acceptance Criteria

1. In the Production environment, `/openapi/v1.json` and `/scalar/v1` are not mapped and a request to each returns 404. In Development and Testing they remain mapped.
2. `auth-start` and `auth-verify` partition on `ClientIpResolver`, not on a direct `RemoteIpAddress` read.
3. A request whose `X-Forwarded-For` differs from the socket, and whose socket is not a trusted proxy, shares the socket bucket and does not create a new bucket.
4. A second auth-start for the same normalized email, after 3 sends inside 30 minutes, returns 429 even when each request presents a different trusted client address.
5. `POST /api/v1/leads/capture` returns 400 for a missing `@`, a 255-character local part, an empty source, and a source other than `clinics-coaches-pricing`, and inserts nothing.
6. A honeypot submission returns 204 and inserts nothing.
7. The sixth lead capture from one trusted client identity inside one hour returns 429 and inserts nothing.
8. A reviewer who did not write the fix re-checks F-001, F-002, and F-003 against the diff and the tests. F-004 through F-007 remain open in the re-check record.
9. After human production deploy, passive GET of both production schema URLs returns 404. That transcript is required before F-001 can be marked closed in the field. Local tests alone do not close it.

## Tests

1. `ProductionSchemaExposureTests` boots the API in Production and in Development and asserts the two route outcomes.
2. `AuthClientIdentityRateLimitTests` covers spoofed forwarded headers, trusted-proxy parsing, and the per-email send budget.
3. `LeadCaptureBoundsTests` covers format, length, source allow-list, honeypot, and the sixth request.
4. Focused command: `dotnet test backend/tests/BioStack.Api.Tests --filter "FullyQualifiedName~BioStack.Api.Tests.Security"`.
5. The production 404 transcript is a human-operated passive GET. The builder does not call production and does not print a connection string.

## Rollback

Revert the parcel commit. Production would again map OpenAPI and Scalar on the next deploy of that revert, and rate limits would return to the socket address. No schema migration is included, so revert has no data-loss step. Lead rows written under the new validation remain valid rows of the existing table.

## Out of Scope

- F-004 log redaction, F-005 legal approval, F-006 webhook idempotency, and F-007 ACR admin credentials. They stay open and blocking.
- F-008 through F-012.
- Changing provider-access or contact policies, except that the new resolver may be used by `auth-start` and `auth-verify` only.
- Storing client IP, adding lead export, or adding lead deletion.
- Terms, privacy, disclaimer, and subscription-policy text.
- Any live auth abuse attempt against production.

## Allowed Files

- `backend/src/BioStack.Api/Program.cs`
- `backend/src/BioStack.Api/Security/ClientIpResolver.cs`
- `backend/src/BioStack.Api/Endpoints/LeadEndpoints.cs`
- `backend/src/BioStack.Contracts/Requests/LeadCaptureRequest.cs`
- `backend/tests/BioStack.Api.Tests/Security/ProductionSchemaExposureTests.cs`
- `backend/tests/BioStack.Api.Tests/Security/AuthClientIdentityRateLimitTests.cs`
- `backend/tests/BioStack.Api.Tests/Security/LeadCaptureBoundsTests.cs`

Coordinator-owned: this spec, `docs/specs/INDEX.md`, and `docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-002-recheck.md`.

## Forbidden

- Mapping an authenticated production OpenAPI alternative in this parcel.
- Trusting `X-Forwarded-For` from an unlisted peer.
- Logging email addresses, magic links, or raw forwarded headers.
- Editing `ProviderAccessEndpoints.cs`, billing code, frontend legal pages, or Azure deploy scripts.
- A production exploit, credential probe, or secret print.
- Marking F-004 through F-007 waived.
