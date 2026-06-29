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

## Protocol Intelligence feature entitlements

`CurrentSubscription.features` must expose these exact feature keys so API and frontend gates do not infer access from marketing copy:

| Feature key | Availability | Enforcement note |
| --- | --- | --- |
| `protocol_intelligence_contracts` | Observer+ | Contract metadata only; never expose restricted source text. |
| `protocol_phase_map` | Operator+ | Reviewed phase-map payloads. |
| `reviewed_relationship_graph` | Operator+ | Reviewed relationship cards only; unreviewed artifacts stay hidden. |
| `source_quality_tracker` | Operator+ | Source-quality tracker and identity/regulatory uncertainty context. |
| `glp1_observability_pack` | Operator+ | Observational GLP-1 modules without medication instructions. |
| `side_effect_ambiguity_detector` | Commander | Side-effect ambiguity panels and related review payloads. |
| `longitudinal_protocol_intelligence_report` | Commander | Longitudinal review and report-generation hooks. |
| `high_risk_warning_first_guardrails` | All tiers | Safety warnings are never paywalled or disabled. |

Safety warnings, high-risk guardrails, Unknown states, and refusal boundaries are not monetization features. Paid tiers unlock deeper reviewed intelligence, not medical authority.

## Protocol Intelligence analytics contract

Allowed event names:

- `protocol_intelligence_viewed`
- `protocol_intelligence_unknown_state_viewed`
- `operator_upgrade_from_relationship_gate_clicked`
- `commander_upgrade_from_ambiguity_gate_clicked`
- `high_risk_warning_viewed`
- `source_quality_warning_viewed`

Analytics payloads may include only non-sensitive operational metadata such as tier, route, protocol ID hash, visible panel IDs, upgrade gate ID, and warning category ID. Do not log protocol text, compound notes, symptoms, medical details, source excerpts, or generated explanation text.
