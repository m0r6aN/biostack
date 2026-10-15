# BIO-GOLIVE-004 spec review 2

- Reviewer: spec-review-2
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-004-client-identity-and-receipt-hold.md`
- Tree read: `HEAD` `5b686e69`
- Verdict: **PASS**

No go-live recommendation. This review does not authorize deploy, Refresh, a live Stripe call, or a production write. Scope is privacy and testability of this review candidate only. F-001, F-005, and F-007 were not re-audited.

## Focus results

| # | Check | Result |
| --- | --- | --- |
| 1 | Named production logs still cannot take an email, domain, magic link, or exception object | **Pass** |
| 2 | In-process auth budget residual is honest and is not called a multi-replica closure | **Pass** |
| 3 | `BillingWebhookReceiptTests` can prove a second delivery does not run the handler without calling Stripe | **Pass** |
| 4 | Allowed files cover the work, including what billing tests already use | **Pass** |
| 5 | Lead capture does not need an edit | **Pass** |

## Findings

No Blocker or Major findings. Nits below do not change the verdict. No fix checklist.

| ID | Severity | Finding |
| --- | --- | --- |
| N-1 | Nit | The objective says this parcel "closes" the F-002 source gap. The dependency section is the controlling text: the per-email budget stays in process memory, a second replica keeps its own budget, and that residual is not a waiver. |
| N-2 | Nit | F-003 is described as still source-partial "only because" lead capture uses the same key. After the shared resolver change, the recheck's stored-email write and lack of a per-address cap would still be open. That does not require a lead-file edit. |
| N-3 | Nit | The log scan should treat `ex.Message` and `ex.ToString()` as passing the exception. Those strings can carry a recipient. The contract already forbids email content at the named sites. |
| N-4 | Nit | Say that the unexpected webhook exception still propagates, or name the result if it is swallowed, so criterion 5 has one first-call assertion. The second-call proof does not depend on that choice. |

### 1 — Production logs

Pass. The spec does not leave a permitted production log argument for an email, a domain, a magic link, or an exception object at the sites F-004 named.

Current production arguments are only these:

- `ContactEndpoints.cs:70` passes `ex` and `LogRedaction.EmailDomain(email)`.
- `ContactMessageSender.cs:62-65` passes the domain.
- `MagicLinkDelivery.cs:59`, `:123`, and `:177` pass the domain. The link is not a log argument today.

The spec forbids those arguments. Contracts require the contact-failure, contact-send, and magic-link-send templates to contain no email, domain, link, or exception object. Minimization repeats that ban for the named sites and also bans forwarded headers there. The test scan fails if those calls pass an exception, an email, a domain, or a link. `LogRedaction.EmailDomain` may remain for helper tests and is not a log line by itself; the named call sites must not pass it.

The development inbox may still hold the link in memory (`MagicLinkDelivery.cs:53`). The spec says that object is not a log line. `Program.cs:630-631` maps the inbox GET only when the host is Development. That is not permission to log the link.

Other existing exception logs, such as `AnalyzeEndpoints.cs:69`, are outside this parcel. The spec does not add them and does not authorize them.

### 2 — Auth budget residual

Pass. The residual matches the code and is not described as closing multi-replica abuse.

`AuthStartAccountBudget` is a process-local `ConcurrentDictionary` (`AuthStartAccountBudget.cs:10-21`), registered as a singleton (`Program.cs:244`). `AuthEndpoints.cs:101-104` consumes it for magic-link start only: 3 sends per 30 minutes per normalized email. It does not cover verify. A second process, replica, or restart has an empty dictionary. The fixed-window IP policies are also in-process (`Program.cs:193-240`). Access Control says rate-limit keys stay in process memory.

The spec records the same limit: the per-email magic-link budget stays in process memory, a second replica can hold its own budget, the residual is not a waiver, and this parcel adds neither Redis nor a table. Out of scope includes "A shared multi-replica auth budget." It does not say verify gained an account budget, and it does not say multi-replica abuse is closed. N-1 is only the broader verb "closes" in the objective.

### 3 — Second delivery without Stripe

Pass. Criterion 5 is provable on the existing SQLite test database. `ClaimStripeEventReceiptAsync` reclaiming only `quarantined` is why a `failed` row does not run the handler again. The spec does not need a new reclaim branch.

Today the generic catch deletes the receipt and rethrows (`BillingService.cs:194-198`). Insert of a duplicate `StripeEventId` hits the unique index (`BioStackDbContext.cs:498`). The catch then updates a row only when its status is `quarantined` (`BillingService.cs:228-235`). Any other count, including a `failed` row, returns null (`:236-237`). `ProcessStripeEventAsync` then returns `AlreadyProcessed` before the handler switch (`:156-161`).

The spec keeps that quarantine update, adds `failed`, and replaces delete with save of `failed` plus a failure code. `ProcessingStatus` is a 32-character string with no value check (`BioStackDbContext.cs:496`; migration `20260713222500_HardenStripeWebhookLifecycle.cs:30-35`). `failed` needs no migration and no schema file edit.

`BillingAndFeatureGateTests` already calls `ProcessStripeEventAsync` twice on SQLite with hand-built `Event` objects and no Stripe HTTP (`BillingAndFeatureGateTests.cs:177-207` and `:250-262`). `EnsureCreated` applies the unique index (`:421-428`). The same shape proves criterion 6.

An unexpected exception that does not call Stripe is a local event whose handler touches `Data` before any `SubscriptionService` or `CustomerService`. Example: `Type = "customer.subscription.updated"` and `Data = null`. `ProcessSubscriptionEventAsync` (`BillingService.cs:373`) throws before a side effect. `checkout.session.completed` with a `SubscriptionId` must not be used: `ProcessCheckoutCompletedAsync` (`:366-367`) calls Stripe. `UnknownStripePriceException` and `UnmappedStripeCustomerException` stay on the quarantine path and do not prove `failed`.

Second-call proof, still with no Stripe call: the first call leaves one row at `failed`; the second call returns `AlreadyProcessed`, does not throw again, and does not change subscription rows. That is the handler switch not running. A partial-save fixture is unnecessary because the skip happens before the switch.

Do not use EF InMemory for this proof. It does not enforce the unique index, so the second insert would not take the claim path.

### 4 — Allowed files

Pass. A builder can meet the spec without editing an unlisted file.

The SQLite helper, event factory, and current-user mock are private methods on `BillingAndFeatureGateTests` (`:391-436`), which is not allowed. They are not a shared fixture. `BillingWebhookReceiptTests.cs` is allowed and can repeat that local setup. `BioStack.Application.Tests.csproj` already compiles this pattern through the Application reference; Sqlite is already referenced by Infrastructure. No project-file edit is required. SDK glob includes the new test file.

`StripeWebhookEvent.cs` and `BillingService.cs` are allowed for the status constant and the catch change. `Program.cs` already calls `PartitionKey` for every named policy, so the numeric limits stay without an edit. `AuthStartAccountBudget.cs` stays unedited because the residual is explicitly not implemented here.

No other test locks the private-range or leftmost behavior. Only `ClientIpResolverTests.cs` does, and that file is allowed.

### 5 — Lead capture

Pass. The spec does not require a lead-capture edit.

`LeadEndpoints.cs` is not in the allowed list. Verified fact 5 and the out-of-scope list forbid lead validation and lead-column changes. `LeadEndpoints.cs:26` only calls `RequireRateLimiting("lead-capture")`. The partition is `ClientIpResolver.PartitionKey` in `Program.cs:233-240`. Changing the resolver changes that key without editing `LeadEndpoints.cs`, `LeadCapture.cs`, or `LeadEndpointsIntegrationTests.cs`. Criterion 7's re-check of F-003's partition key is a diff read of that shared call. N-2 notes what remains open after that read.

## Commands

Spec and source were read. Tests were not run. The new receipt tests are not in the tree, and this review does not implement them.

- `git rev-parse --short HEAD` → `5b686e69`
- `rtk` is not installed in this shell, so Git was run directly.

No go-live recommendation.
