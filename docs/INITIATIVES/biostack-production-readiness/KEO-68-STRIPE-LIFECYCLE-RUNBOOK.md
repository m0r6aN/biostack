# KEO-68 Stripe lifecycle runbook

Status: deterministic controls implemented; production approval and live transaction evidence pending.

## Authority and policy

- `contracts/product-contract.v1.json` is authoritative for plan codes, USD monthly prices, and entitlement thresholds.
- Stripe subscription state is the billing authority. Only `Active` and `Trialing` retain paid access. `PastDue`, `Canceled`, `Unpaid`, `Incomplete`, `IncompleteExpired`, and `Paused` resolve to Observer.
- A refund event is an auditable payment event, not an entitlement grant or downgrade by itself. Subscription events remain authoritative for access.
- Existing customer data is retained after downgrade. New gated actions and the Observer active-compound limit are enforced by the API.

## Required production configuration

All values are required before a release can advertise or accept paid plans:

- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `Stripe__OperatorPriceId` for the approved $12 USD monthly recurring price
- `Stripe__CommanderPriceId` for the approved $29 USD monthly recurring price
- `Stripe__CheckoutSuccessUrl`
- `Stripe__CheckoutCancelUrl`
- `Stripe__PortalReturnUrl`

Production startup fails closed when a value is absent or a billing return URL is not absolute HTTPS. Record identifiers and secret-version references in the release evidence store; never copy secret values into tickets, logs, screenshots, or this repository.

Migration `20260713222500_HardenStripeWebhookLifecycle` is intentionally additive: four receipt columns and a last-attempt timestamp backfill. The repository's pre-existing EF model snapshot does not describe the complete schema, so ordinary migration scaffolding proposes an unsafe whole-schema rewrite. Do not regenerate this migration or accept a broad scaffold until that baseline is repaired and reviewed; use the checked migration SQL as the release artifact.

## Webhook endpoint

Endpoint: `POST /api/v1/billing/stripe/webhook`

The endpoint accepts at most 1 MiB, validates the Stripe signature before processing, and records one durable receipt per Stripe event ID. Supported lifecycle events are:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_failed`
- `invoice.payment_succeeded`

Other valid signed events are acknowledged and receipted without changing entitlements. Re-delivery of a processed event returns success without applying state again.

## Unknown-price quarantine and replay

An unrecognized or missing Stripe price ID never maps to a paid plan or silently becomes a successful Observer reconciliation. The signed event is recorded with:

- `processingStatus = quarantined`
- `failureCode = unknown_stripe_price`
- an incremented attempt count and last-attempt timestamp

The webhook returns HTTP 409 so Stripe retains the failed delivery. Administrators can inspect non-secret receipt metadata at `GET /api/v1/admin/billing/stripe/events/quarantined`.

Replay procedure:

1. Confirm the price is an approved monthly product and matches product-contract v1.
2. Correct the production price-ID configuration without recording the secret value in evidence.
3. Redeploy/restart only through the approved release path.
4. Re-deliver the same event from Stripe Workbench or allow Stripe's retry.
5. Confirm the same receipt moves to `processed`, attempt count increments, exactly one subscription row exists, and the quarantine list no longer contains the event.

## Authorized live qualification

This must be performed by the billing owner in the approved Stripe account and deployed environment. Capture event IDs, timestamps, plan code, application revision, non-secret configuration version, and redacted screenshots or exported receipts.

1. Observer account starts Operator checkout and returns through the configured success URL.
2. Signed events create one Operator subscription and grant Operator only after the verified webhook.
3. Re-deliver each event; subscription and receipt counts remain unchanged.
4. Open the customer portal and return through the configured portal URL.
5. Record a renewal or controlled test-clock advance; the paid-through date advances.
6. Produce `invoice.payment_failed`; access resolves immediately to Observer and stored customer data remains readable.
7. Produce `invoice.payment_succeeded` plus the corresponding subscription update; approved paid access is restored.
8. Cancel at period end; access remains only through the current paid-through timestamp, then resolves to Observer.
9. Repeat checkout for Commander and confirm Commander-only gates.
10. Execute the billing-owner-approved refund/cancellation scenario and confirm subscription state, entitlement state, and retained data agree.

## Closeout evidence

KEO-68 cannot be marked Done from deterministic tests alone. Closeout requires:

- human approval of both live products/prices and refund authority;
- production URL and secret-version inventory;
- authorized live Operator and Commander transaction evidence;
- webhook replay, failure, recovery, cancellation/expiration, portal, refund, and retained-data evidence;
- confirmation that no raw secrets or customer financial details entered logs or issue comments.
