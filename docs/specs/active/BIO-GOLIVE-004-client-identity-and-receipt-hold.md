---
ticket: BIO-GOLIVE-004
title: Trusted client identity, log redaction, and webhook receipt hold
status: active
owner: clinton.morgan
created: 2026-10-14
updated: 2026-10-14
delivery_classes: [standard, privacy]
guidance_classes: []
substance_function_risk: []
function_review_status: not-applicable
surfaces:
  - backend/src/BioStack.Api/Security/ClientIpResolver.cs
  - backend/tests/BioStack.Api.Tests/Security/ClientIpResolverTests.cs
  - backend/src/BioStack.Api/Security/LogRedaction.cs
  - backend/src/BioStack.Api/Endpoints/ContactEndpoints.cs
  - backend/src/BioStack.Api/Auth/MagicLinkDelivery.cs
  - backend/src/BioStack.Api/Auth/ContactMessageSender.cs
  - backend/tests/BioStack.Api.Tests/Security/LogRedactionTests.cs
  - backend/src/BioStack.Domain/Entities/StripeWebhookEvent.cs
  - backend/src/BioStack.Application/Services/BillingService.cs
  - backend/tests/BioStack.Application.Tests/Services/BillingWebhookReceiptTests.cs
routing_class: implementation/privacy
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: confidential
---

# BIO-GOLIVE-004 — Trusted client identity, log redaction, and webhook receipt hold

## Objective

`reviews/SECURITY-RECHECK-PR544-2026-10-14.md` left F-002, F-004, and F-006 source-partial. F-003 stays source-partial only because lead capture uses the same client-identity key. This parcel closes those source gaps. It does not deploy, does not waive F-001, F-005, or F-007, and does not change the production-readiness NO-GO.

## Initiative (Product Lane)

`biostack-go-live`. Coordinator decision on 2026-10-14: F-007 stays open because `source-acquisition-acr-transition.bicep` and its verifier intentionally keep ACR admin enabled during that transition. Disabling the live registry is outside this parcel.

## Branch and Worktree

Proposed only. Gate 2 assigns both. No builder implements this spec while it is a review candidate.

- Branch: `fix/bio-golive-004-identity-and-receipts`
- Worktree: `D:/Repos/BioStack/.worktrees/bio-golive-004-identity-and-receipts`

## Dependencies

- PR #544 is already on `main`. This parcel edits that code. It does not reopen BIO-GOLIVE-002's file list except where the re-check named a remaining defect.
- Production proof of the client IP remains a later deploy check. Unit tests are not that proof.
- The per-email magic-link budget stays in process memory. A second replica can hold its own budget. That residual is recorded here and is not a waiver. This parcel does not add Redis or a new table for it.

## Verified Facts

1. `ClientIpResolver.IsTrustedProxy` returns true for loopback, configured `RateLimit:TrustedProxies`, and every 10/8, 172.16/12, 192.168/16, and 100.64/10 address.
2. `FirstForwardedAddress` returns the leftmost `X-Forwarded-For` hop. `ClientIpResolverTests` locks a CGNAT peer with an empty proxy list to that leftmost hop.
3. `ContactEndpoints` logs the exception object and `LogRedaction.EmailDomain`. Magic-link and contact senders log the domain. The domain is part of the email address.
4. `BillingService.ProcessStripeEventAsync` inserts the receipt, then on an unexpected exception calls `Remove` and `SaveChanges`, deleting the row. A later delivery can claim the same `StripeEventId`. Quarantine reclaim of status `quarantined` is a separate path and stays.
5. Lead capture already validates email, source, and honeypot, and limits on `ClientIpResolver.PartitionKey`. No lead-capture file changes in this parcel.

## Surfaces

- `ClientIpResolver` — a peer is trusted only when it is loopback or listed in `RateLimit:TrustedProxies`. Private and CGNAT ranges are not automatically trusted. When the peer is trusted, the partition key is the rightmost forwarded address that is not itself a trusted proxy. When the peer is not trusted, the socket address is the key and the header is ignored.
- Log call sites named above — log the event and category only. Do not pass the exception object, the address, the domain, or the magic link to the logger. `LogRedaction.EmailDomain` remains for tests of the helper and is not used by those call sites.
- `StripeWebhookProcessingStatuses` — add `Failed` = `failed`.
- `BillingService` — on an unexpected exception, set the existing receipt to `failed` with a failure code and save it. Do not remove the row. A later delivery of the same event finds the row and is already processed. Quarantine handling stays as it is.
- Tests named below.

## Contracts

- Unlisted public peers never contribute `X-Forwarded-For` to the bucket.
- A trusted peer and header `203.0.113.9, 198.51.100.20` yields `198.51.100.20`.
- A CGNAT peer and an empty trusted-proxy list yields the CGNAT socket address, not the header.
- Lead capture, auth, contact, and provider-access policies keep their current numeric limits and keep calling `PartitionKey`.
- Production log templates for contact failure, contact send, and magic-link send contain no email, domain, link, or exception object.
- The development magic-link inbox may still hold the link in memory. That object is not a log line.
- A failed webhook receipt remains stored. Status `failed` is not reclaimed into `accepted`. Status `quarantined` can still be reclaimed.

## Data Inventory

- Client address: in-memory rate-limit key only. Not stored and not logged by this parcel.
- Email domain and exception text: removed from the named log templates.
- Stripe event id: already stored. This parcel changes the failure path from delete to a `failed` status.

## Purpose

The client key stops one caller from choosing another caller's rate-limit bucket. The log change keeps addresses and links out of production logs. The receipt hold stops a replay after a handler has already saved.

## Consent

This parcel does not collect a new field and does not approve privacy copy.

## Minimization

- Do not log email, domain, magic link, forwarded headers, or exception objects from the named sites.
- Do not store the client address.
- Do not add a new lead column.

## Retention

No retention change. Failed webhook rows remain until an existing operator process deletes them. Lead retention is unchanged.

## Export

No new export.

## Deletion

No new deletion endpoint. The webhook failure path must not delete the receipt.

## Access Control

Rate-limit keys stay in process memory. Webhook receipts stay on the existing database path. No new public read is added.

## Acceptance Criteria

1. An untrusted peer with `X-Forwarded-For` set is bucketed by the socket address.
2. A configured trusted peer is bucketed by the rightmost forwarded address that is not a trusted proxy.
3. A private or CGNAT peer is untrusted when it is absent from `RateLimit:TrustedProxies`.
4. The named log templates do not include the exception object, an email address, a domain, or a magic link.
5. After an unexpected exception in webhook processing, the receipt row still exists with status `failed`, and a second delivery of that event does not run the handler again.
6. Quarantine reclaim still works for status `quarantined`.
7. An independent reviewer who did not write the fix re-checks F-002, F-003's partition key, F-004, and F-006 against the diff. F-001, F-005, and F-007 stay open.

## Tests

1. Update `ClientIpResolverTests` for criteria 1 through 3.
2. Update `LogRedactionTests` so a scan of the named production log calls fails if they pass an exception, an email, a domain, or a link.
3. Add `BillingWebhookReceiptTests` for criteria 5 and 6 using the existing test database or the existing billing test double. Do not call Stripe.
4. Focused commands: `dotnet test backend/tests/BioStack.Api.Tests --filter "FullyQualifiedName~ClientIpResolverTests|FullyQualifiedName~LogRedactionTests"` and `dotnet test backend/tests/BioStack.Application.Tests --filter FullyQualifiedName~BillingWebhookReceiptTests`.

## Rollback

Revert the parcel commit. Rate limits return to the current private-range leftmost behavior. Webhook failures would delete receipts again. No migration is included.

## Out of Scope

- F-001 production OpenAPI, F-005 legal approval, and F-007 ACR admin, including any edit to `infra/azure/`.
- A shared multi-replica auth budget.
- Production deploy, Refresh, live Stripe calls, and secret prints.
- Changing lead validation, prices, or checkout plan selection.

## Allowed Files

- `backend/src/BioStack.Api/Security/ClientIpResolver.cs`
- `backend/tests/BioStack.Api.Tests/Security/ClientIpResolverTests.cs`
- `backend/src/BioStack.Api/Security/LogRedaction.cs`
- `backend/src/BioStack.Api/Endpoints/ContactEndpoints.cs`
- `backend/src/BioStack.Api/Auth/MagicLinkDelivery.cs`
- `backend/src/BioStack.Api/Auth/ContactMessageSender.cs`
- `backend/tests/BioStack.Api.Tests/Security/LogRedactionTests.cs`
- `backend/src/BioStack.Domain/Entities/StripeWebhookEvent.cs`
- `backend/src/BioStack.Application/Services/BillingService.cs`
- `backend/tests/BioStack.Application.Tests/Services/BillingWebhookReceiptTests.cs`

Coordinator-owned: this spec, `docs/specs/INDEX.md`, and the go-live status files.

## Forbidden

- Trusting a private or CGNAT range that is not listed in configuration.
- Selecting the leftmost forwarded hop.
- Logging `ex`, an email, a domain, or a magic link from the named call sites.
- Deleting a webhook receipt on the failure path.
- Editing Azure templates, legal pages, the seed, or production configuration.
- A live charge, a production exploit, or a secret print.
