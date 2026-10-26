# Tracks

> **2026-10-25 owner rulings (supersede this file where they conflict):** Clinton Morgan is the sole owner, operator and decision maker; his sign-off is final. No human-gated approval: the two-reviewer AI council (`docs/operations/ai-council-review.md`) is the content/approval gate, and disagreement escalates to Clint with options. No legal review: legal items are known risks, not blockers. Current gates: `docs/launch-readiness-ledger.md`.

| ID | Track | Owner | Status | Release responsibility |
|---|---|---|---|---|
| T1 | Frontend experience | frontend owner | failing | Full CI, browser, accessibility, auth/onboarding |
| T2 | API and data | backend owner | active | Authz, consent, migrations, health, abuse controls |
| T3 | Billing | product + billing owner | blocked | Monthly-only Stripe configuration and lifecycle |
| T4 | Provider operations | operations owner | spec-ready | Intake queue, notification, SLA and privacy |
| T5 | Platform/deployment | platform owner | failing | CI, Azure deploy, probes, monitoring, rollback |
| T6 | Legal/privacy/security | Clint (AI council for content) | blocked | Consent sign-off and security gates; legal items are known risks |
| T7 | Evidence/release | initiative coordinator | active | State reconciliation and GO/NO-GO decision |

No track is complete solely because code merged.
