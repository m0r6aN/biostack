## Billing Tier Enforcement

BioStack billing supports three product tiers:

- `Observer`: free plan with up to 5 active compounds.
- `Operator`: unlocks paid stack intelligence and removes the active compound cap.
- `Commander`: unlocks protocol review, pattern, drift, sequence, and mission-control intelligence.

Stripe configuration lives under the `Stripe` config section:

- `SecretKey`
- `WebhookSecret`
- `OperatorPriceId`
- `CommanderPriceId`
- `CheckoutSuccessUrl`
- `CheckoutCancelUrl`
- `PortalReturnUrl`

Webhook ingestion is idempotent through the `StripeWebhookEvents` table. Subscription state is persisted in `Subscriptions`, and the effective tier falls back to `Observer` when paid access expires.
