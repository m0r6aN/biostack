# BIO-GOLIVE-004 implementation review 1

- Reviewer: impl-review-1
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-004-client-identity-and-receipt-hold.md`
- Commit: `bd4a0ac960a820a7c4970e636b9b8dc05f9491b0`
- Branch: `fix/bio-golive-004-identity-and-receipts`
- Worktree: `D:\Repos\BioStack\.worktrees\bio-golive-004-identity-and-receipts`
- Author: m0r6aN \<morganclint76@gmail.com\> (2026-10-14 19:14:17 -0400)
- Verdict: **PASS**

This reviewer did not write commit `bd4a0ac`. No go-live recommendation. This review does not authorize deploy, Refresh, a live Stripe call, a production write, or a change to the production-readiness NO-GO. F-001, F-005, and F-007 stay open. The in-process per-email magic-link budget residual recorded in the spec is unchanged and is not a waiver.

## Definition of Done

| # | Item | Result |
| --- | --- | --- |
| 1 | Diff touches only allowed files | **Pass** |
| 2 | Untrusted peer with `X-Forwarded-For` is bucketed by the socket address | **Pass** |
| 3 | Configured trusted peer uses the rightmost forwarded address that is not itself a trusted proxy | **Pass** |
| 4 | Private and CGNAT peers are untrusted when absent from `RateLimit:TrustedProxies` | **Pass** |
| 5 | Named log templates do not pass an exception object, email address, domain, or magic link | **Pass** |
| 6 | Unexpected webhook exception keeps the receipt, sets `failed` and `unexpected_processing_error`, rethrows, and a second delivery does not run the handler | **Pass** |
| 7 | Status `failed` is not reclaimed; status `quarantined` still is | **Pass** |
| 8 | Focused `ClientIpResolverTests`, `LogRedactionTests`, and `BillingWebhookReceiptTests` pass | **Pass** |
| 9 | Independent re-check of F-002, F-003's partition key, F-004, and F-006; F-001, F-005, and F-007 stay open | **Pass** |

## Diff scope

`git diff-tree --no-commit-id --name-status -r bd4a0ac` lists nine files. `HEAD` is that commit and the worktree was clean. `LogRedaction.cs` is allowed and was not edited; `EmailDomain` remains only for the helper test and is not called from the named sites.

| Path | Status | Allowed |
| --- | --- | --- |
| `backend/src/BioStack.Api/Auth/ContactMessageSender.cs` | M | Yes |
| `backend/src/BioStack.Api/Auth/MagicLinkDelivery.cs` | M | Yes |
| `backend/src/BioStack.Api/Endpoints/ContactEndpoints.cs` | M | Yes |
| `backend/src/BioStack.Api/Security/ClientIpResolver.cs` | M | Yes |
| `backend/src/BioStack.Application/Services/BillingService.cs` | M | Yes |
| `backend/src/BioStack.Domain/Entities/StripeWebhookEvent.cs` | M | Yes |
| `backend/tests/BioStack.Api.Tests/Security/ClientIpResolverTests.cs` | M | Yes |
| `backend/tests/BioStack.Api.Tests/Security/LogRedactionTests.cs` | M | Yes |
| `backend/tests/BioStack.Application.Tests/Services/BillingWebhookReceiptTests.cs` | A | Yes |

No edits to `infra/azure/`, legal pages, seed data, production configuration, lead-capture files, or `Program.cs`. Rate-limit policies still call `ClientIpResolver.PartitionKey` with the previous limits: auth-start 5/10 min, auth-verify 10/10 min, provider-access 5/hour, contact 3/hour, lead-capture 5/hour (`Program.cs` policies, not in this diff).

## Client identity

`IsTrustedProxy` returns true only for `IPAddress.IsLoopback` or an exact address parsed from `RateLimit:TrustedProxies`. The previous 10/8, 172.16/12, 192.168/16, and 100.64/10 checks are gone. `PartitionKey` reads `X-Forwarded-For` only after that trust check. Otherwise it returns the normalized socket address.

For a trusted peer, `RightmostUntrustedForwardedAddress` walks hops from the right and returns the first address that is not itself a trusted proxy. The contract header `203.0.113.9, 198.51.100.20` therefore yields `198.51.100.20`. An unparseable hop returns null, and the key falls back to the socket address rather than a hop farther left. If every hop is trusted, the key is the socket address.

Tests lock an untrusted public peer, a configured `10.0.0.4` peer, the rightmost-untrusted contract, an all-trusted header, and unconfigured peers `10.1.2.3`, `172.16.0.4`, `192.168.1.9`, and `100.64.0.4` with an empty proxy list.

## Log redaction

Named production calls are now event and category only:

- `ContactEndpoints` logs `LogWarning("Failed to deliver contact message ({Category})", category)` and does not pass the caught exception.
- `ContactMessageSender` logs the category only.
- `MagicLinkDelivery` logs three fixed templates and does not pass `contact` or `magicLink`.

`InMemoryMagicLinkDelivery` still enqueues `DevMagicLinkMessage` with the link. That object is not a log argument. `LogRedactionTests` reads those three source files, extracts logger calls, and rejects any argument other than the message template or `category` / `message.Category`. That rejects `ex`, `ex.Message`, `EmailDomain`, an address, and `magicLink`. The templates still say "magic link" as the event name. They do not contain the link value, a domain, an email address, or an exception object.

## Webhook receipt hold

`StripeWebhookProcessingStatuses.Failed` is `"failed"`. On an unexpected exception, `ProcessStripeEventAsync` no longer calls `Remove`. It sets the existing receipt to `failed`, sets `FailureCode` to `unexpected_processing_error`, saves, and uses `throw`.

`ClaimStripeEventReceiptAsync` still reclaims only `ProcessingStatus == quarantined`. Any other existing row, including `failed`, makes the claim return null, and the second delivery returns `AlreadyProcessed` before the handler switch. The unique index on `StripeEventId` is unchanged (`BioStackDbContext`). Both lengths fit the existing columns (`failed` is 6 of 32; `unexpected_processing_error` is 27 of 128). No migration was added.

`BillingWebhookReceiptTests` uses SQLite `EnsureCreated` and hand-built Stripe `Event` objects. The failure fixture uses `customer.subscription.updated` with `Data = null`, which throws `NullReferenceException` in `ProcessSubscriptionEventAsync` before a Stripe service call. The quarantine fixture reclaims `unknown_stripe_price` into `processed` with attempt count 2. The failed row stays failed with attempt count 1.

## Finding re-check

- F-002: this diff removes automatic private and CGNAT trust and leftmost `X-Forwarded-For` selection. The single-process auth budget is still the residual the spec leaves open.
- F-003 partition key: lead capture still uses `PartitionKey` and no lead file changed. Stored lead email and the lack of a per-address cap are outside this parcel.
- F-004: the named call sites no longer pass `LogRedaction.EmailDomain` or the contact exception.
- F-006: the failure path keeps the receipt, so a later delivery cannot claim the same `StripeEventId`. Quarantine reclaim remains.
- F-001, F-005, and F-007: not edited and not closed.

## Findings

No Blocker or Major findings. No fix checklist.

| ID | Severity | Finding |
| --- | --- | --- |
| N-1 | Nit | A loopback socket peer is trusted in `IsTrustedProxy`, but no test uses a loopback peer with an empty proxy list. Forwarded `127.0.0.1` is covered only as a hop that is skipped. |
| N-2 | Nit | If `SaveChangesAsync` inside the failure `catch` throws, that new exception replaces the original one. The successful save path does `throw` and the test observes `NullReferenceException`. |

N-1 and N-2 do not change the verdict.

Out of scope, not charged to this commit: existing NU1902/NU1903 advisories for `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` printed during restore. Those packages are not in the allowed file list.

## Commands

`rtk` is not installed in this shell. Git and `dotnet` were run directly from the worktree.

```text
git rev-parse HEAD
bd4a0ac960a820a7c4970e636b9b8dc05f9491b0

git diff-tree --no-commit-id --name-status -r bd4a0ac960a820a7c4970e636b9b8dc05f9491b0
M  backend/src/BioStack.Api/Auth/ContactMessageSender.cs
M  backend/src/BioStack.Api/Auth/MagicLinkDelivery.cs
M  backend/src/BioStack.Api/Endpoints/ContactEndpoints.cs
M  backend/src/BioStack.Api/Security/ClientIpResolver.cs
M  backend/src/BioStack.Application/Services/BillingService.cs
M  backend/src/BioStack.Domain/Entities/StripeWebhookEvent.cs
M  backend/tests/BioStack.Api.Tests/Security/ClientIpResolverTests.cs
M  backend/tests/BioStack.Api.Tests/Security/LogRedactionTests.cs
A  backend/tests/BioStack.Application.Tests/Services/BillingWebhookReceiptTests.cs

git status --porcelain=v1
(empty)
```

```text
dotnet test backend/tests/BioStack.Api.Tests --filter "FullyQualifiedName~ClientIpResolverTests|FullyQualifiedName~LogRedactionTests" --nologo
Passed!  - Failed: 0, Passed: 14, Skipped: 0, Total: 14, Duration: 38 ms - BioStack.Api.Tests.dll (net10.0)
```

The 14 results are 9 `ClientIpResolverTests` (including four private/CGNAT rows) and 5 `LogRedactionTests` (including three `EmailDomain` rows).

```text
dotnet test backend/tests/BioStack.Application.Tests --filter FullyQualifiedName~BillingWebhookReceiptTests --nologo
```

A first run overlapped the API build and failed before any test executed: `CS2012` because `BioStack.Domain.dll` and `BioStack.Cognition.CollectiveAdapter.dll` were locked. The same command run alone then passed:

```text
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 1 s - BioStack.Application.Tests.dll (net10.0)
```
