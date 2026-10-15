# BIO-GOLIVE-004 spec review

- Reviewer: spec-review-1
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-004-client-identity-and-receipt-hold.md`
- Spec content hash: `154cbced2e7d81edee87e617ee7f3e1f955b1198` (untracked working-tree file; `git hash-object` only, not a committed blob)
- Tree: branch `docs/go-live-library-record`, HEAD `5b686e69536f2eedf0195ee77466f20b08a490d8`
- Verdict: **PASS**

The five verified facts match this HEAD. A builder can implement the rightmost-forwarded-address rule, the named log change, and the failed-receipt hold without an unresolved product decision. Status `failed` does not enter the quarantine reclaim query. F-007 is out of scope for a reason that is present in `infra/azure/source-acquisition-acr-transition.bicep` and its verifier. No blocker or major finding. This review does not authorize implementation, Refresh, a production write, or go-live. The privacy fold still expects a second reviewer.

## Definition of Done

| DoD item | Status | Evidence |
| --- | --- | --- |
| Fact 1: loopback, configured proxies, and 10/8, 172.16/12, 192.168/16, 100.64/10 are trusted | Pass | `ClientIpResolver.IsTrustedProxy` at `ClientIpResolver.cs:36-59`. The method is private. Its behavior matches the fact. |
| Fact 2: leftmost `X-Forwarded-For` hop, including a CGNAT peer with an empty proxy list | Pass | `FirstForwardedAddress` at `ClientIpResolver.cs:77-80` takes `Split(',', 2)[0]`. See nit N1 for which test locks "leftmost." |
| Fact 3: contact failure logs the exception and domain; magic-link and contact senders log the domain | Pass | `ContactEndpoints.cs:70`; `MagicLinkDelivery.cs:59`, `:123`, `:177`; `ContactMessageSender.cs:62-65`. `LogRedaction.EmailDomain` returns the substring after `@`. |
| Fact 4: unexpected exceptions delete the receipt; quarantine reclaim is a separate path | Pass | Insert at `BillingService.cs:215-218`. Delete at `:194-198`. Reclaim updates only `quarantined` at `:228-237`. |
| Fact 5: lead capture already validates email, source, and honeypot and limits on `PartitionKey`; no lead file in this parcel | Pass | `LeadEndpoints.cs:35-66`. Policy `lead-capture` is 5/hour on `ClientIpResolver.PartitionKey` at `Program.cs:233-240`. Lead files are not in Allowed Files. |
| Rightmost-forwarded-address rule is implementable with no open decision | Pass | Surfaces and the contract example `203.0.113.9, 198.51.100.20` → `198.51.100.20`. Forbidden bullet bars the leftmost hop. |
| Log change is implementable with no open decision | Pass | Drop the exception, address, domain, and link from the five cited templates. Keep `LogRedaction.EmailDomain` for the helper test. Keep the in-memory inbox object at `MagicLinkDelivery.cs:53`. |
| Failed-receipt hold is implementable with no open decision | Pass | Add `Failed = "failed"` on the existing string column (`HasMaxLength(32)`, no check constraint). Replace `Remove` with a saved `failed` row. Unique `StripeEventId` then makes a later delivery `AlreadyProcessed`. |
| Quarantine reclaim is not broken by `failed` | Pass | `ClaimStripeEventReceiptAsync` reclaims only `ProcessingStatus == Quarantined`. `failed` matches zero rows and returns null. The two quarantine catches stay above the general catch. |
| F-007 is explicitly out of scope for a real reason visible in the ACR transition | Pass | `source-acquisition-acr-transition.bicep:39` sets `adminUserEnabled: true`. The verifier fails if that literal is missing or false (`verify-source-acquisition-network-foundation.ps1:74-76` and `:133`) and, on a live what-if, if admin is not enabled (`:165`). Allowed Files do not include `infra/azure/`. |

## Findings

| id | severity | location | finding |
| --- | --- | --- | --- |
| N1 | nit | Fact 2 (line 55); `ClientIpResolverTests.cs:35-44` | The CGNAT empty-list test sets a one-hop header, `198.51.100.22`, so it locks "private peer trusts the header," not leftmost versus rightmost. Leftmost is locked by `FirstForwardedAddress` and by `ClientIpResolverTests.cs:23-33` (`198.51.100.10, 10.0.0.4` → `198.51.100.10`) on a configured proxy, not on the CGNAT fixture. The fact is still true of the code. The new contract replaces both behaviors. |
| N2 | nit | Surfaces (line 63); contract (line 74); tests item 2 (line 127); `MagicLinkDelivery.cs:59` | "Production log templates" and "named production log calls" can be misread as leaving the development queue log alone. The determined reading is to strip the domain from all five F-004 templates, including line 59. AC4 says the named templates, and F-004 cites line 59. The inbox object at line 53 is not a log line and stays. |
| N3 | nit | Surfaces (line 65); `BillingService.cs:194-198` | The failure-code literal and whether the unexpected exception is still rethrown are not pinned. That is latitude, not an open decision. Use a short `FailureCode` in the style of `unknown_stripe_price`, not exception text (`FailureCode` max length is 128). Keep the existing `throw` so `BillingEndpoints.cs`, which is not an allowed file, stays unchanged. Swallowing also meets AC5, because any non-quarantine result is already HTTP 200. |
| N4 | nit | Surfaces (line 62) | A trusted peer whose header has no address that is not a trusted proxy is unspecified. Today's code falls back to the socket when the parsed hop is null (`ClientIpResolver.cs:15-22`). Keeping that fallback does not conflict with AC1–AC3. |
| M1 | minor | Retention (line 100) | "Failed webhook rows remain until an existing operator process deletes them" names a deleter this repository does not have. The only `StripeWebhookEvents` delete is the failure-path `Remove` this parcel stops. `GET /api/v1/admin/billing/stripe/events/quarantined` lists quarantined rows and does not delete them. KEO-68 replays quarantine; it does not delete receipts. The normative rules still hold: no retention change, no new deletion endpoint, and the failure path must not delete the row. No builder choice depends on the missing process. |

## Verified facts

- `IsTrustedProxy` returns true for a configured `RateLimit:TrustedProxies` address, for `IPAddress.IsLoopback`, and for IPv4 or IPv4-mapped addresses in 10/8, 172.16/12 (`bytes[1]` 16–31), 192.168/16, and 100.64/10 (`bytes[1]` 64–127). Other address families are not trusted by the range check.
- `FirstForwardedAddress` returns the leftmost parseable hop. `PartitionKey_UsesForwardedClientWhenPeerIsPlatformPrivate` uses peer `100.64.0.4`, an empty configuration, and header `198.51.100.22`, and expects that header. `UseForwardedHeaders` does not appear in the C# sources. `PartitionKey` is the header reader.
- Contact failure passes `ex` and `LogRedaction.EmailDomain(email)` to `LogWarning`. The three magic-link `LogInformation` calls and the contact-sender `LogInformation` call pass `LogRedaction.EmailDomain` and do not pass the raw link. `InMemoryMagicLinkDelivery` still enqueues the raw link at `MagicLinkDelivery.cs:53`.
- `ProcessStripeEventAsync` inserts an `accepted` receipt before the handler. `UnknownStripePriceException` and `UnmappedStripeCustomerException` quarantine that row. Any other exception removes it and calls `SaveChanges`, then rethrows. A later insert can reuse the same `StripeEventId` because the unique index is on that column (`BioStackDbContext.cs:498`).
- Reclaim is `ExecuteUpdate` where `StripeEventId` matches and status is `quarantined`, setting status to `accepted`. Any other stored status, including a future `failed`, yields `claimed != 1`, a null claim, and `AlreadyProcessed`. The handler does not run. `GetQuarantinedStripeEventsAsync` also filters `quarantined` only. Adding `failed` does not widen either query unless a builder edits them. The spec says not to.
- `StripeWebhookProcessingStatuses` is `accepted`, `processed`, and `quarantined`. There is no `failed` constant. `ProcessingStatus` is required text with max length 32. No migration is required for a new status string. `BillingWebhookReceiptTests.cs` is not on disk. The spec says to add it. `BillingAndFeatureGateTests` already builds `BillingService` on a temporary SQLite database and replays quarantine without calling Stripe.
- Lead capture rejects a filled honeypot, requires `MailAddress.TryCreate` within 254 characters, and allows only two sources. It does not need an edit for this parcel. F-003's partition key changes only because `PartitionKey` changes.
- `source-acquisition-acr-transition.bicep:39` sets `adminUserEnabled: true`. `verify-source-acquisition-network-foundation.ps1:74-76` requires that source text and rejects `adminUserEnabled: false`. Line 133 requires the compiled property to be true. Line 165 fails a live query unless admin is enabled. `source-acquisition-network-foundation.bicep:86` treats admin enabled as part of transitional reachability. Disabling the registry in this parcel would edit forbidden infra and fail that verifier. The spec's out-of-scope statement matches that constraint. It does not claim the live registry was queried.

## Determined readings

- Trusted means loopback or `RateLimit:TrustedProxies` for both the socket peer and a forwarded hop. Private and CGNAT ranges are not trusted unless listed. From a trusted peer, walk the comma-separated header from the right and keep the first address that is not trusted. The contract example is that case: both hops are public, so the key is `198.51.100.20`.
- An unlisted peer, including an unlisted private or CGNAT peer, ignores `X-Forwarded-For` and uses the socket address.
- Named log calls keep the static event text and, for contact only, the already allow-listed category. They do not pass `ex`, an email, a domain, a link, or a forwarded address. `LogRedaction.EmailDomain` remains for `LogRedactionTests` and is not used at those call sites.
- On an unexpected exception, set the existing receipt to `failed` with a short failure code and save it. Do not remove it. Do not treat `failed` as `quarantined`. A second delivery is `AlreadyProcessed`.

## Commands

- `Get-Command rtk` was absent (`RTK=no`). Git and the validator ran directly.
- `git rev-parse HEAD` → `5b686e69536f2eedf0195ee77466f20b08a490d8`
- `git rev-parse --abbrev-ref HEAD` → `docs/go-live-library-record`
- `git hash-object docs/specs/active/BIO-GOLIVE-004-client-identity-and-receipt-hold.md` → `154cbced2e7d81edee87e617ee7f3e1f955b1198`
- `git status --short` for the spec and the cited source files lists only `?? docs/specs/active/BIO-GOLIVE-004-client-identity-and-receipt-hold.md`. The resolver, tests, log sites, billing service, status constants, bicep, and verifier were clean at this HEAD.
- `docs/specs/scripts/validate-spec.ps1 -SpecPath docs/specs/active/BIO-GOLIVE-004-client-identity-and-receipt-hold.md -RepoRoot D:\Repos\BioStack` exited 0. Output: `shape` `ticket-spec`, `result` `valid`, `reason` null, `reviewerCount` 2. `standingAuthorizationEligible: true` is validator output only. It is not dispatch authority and not a go-live decision.

## Fix checklist

None. No blocker or major finding.

## Summary

Verdict PASS for BIO-GOLIVE-004, reviewer spec-review-1, date 2026-10-14.
Facts 1–5 match the resolver, its tests, the five log sites, the receipt delete, and lead capture on this HEAD.
The rightmost untrusted hop, the log redaction, and the failed receipt hold do not need another product decision.
Status `failed` is outside the `quarantined` reclaim filter, so quarantine replay stays intact.
F-007 stays out of scope because the ACR transition bicep sets `adminUserEnabled: true` and the verifier fails closed if admin is disabled.
Findings are minor M1 and nits N1–N4. None is a blocker or a major.
The spec validator returned `valid` for shape `ticket-spec`. Cited source files were clean at `5b686e69`.
The spec file itself is still untracked. Its content hash is `154cbced2e7d81edee87e617ee7f3e1f955b1198`.
This review does not authorize implementation, Refresh, a production write, or go-live.
Privacy routing still expects a second reviewer. This file is only spec-review-1.
