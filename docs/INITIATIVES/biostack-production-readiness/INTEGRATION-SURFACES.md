# Integration Surfaces

| ID | Producer -> consumer | Contract | Auth/data | Positive, negative, failure evidence | Environments | Gate/status |
|---|---|---|---|---|---|---|
| S1 | Browser -> frontend/API | session, routes, return path | identity + profile data | sign-in succeeds; protected route denies; email/callback failure recovers | staging, production smoke | SG1 / blocked |
| S2 | Billing UI/API -> Stripe | monthly tier + webhook lifecycle | payment/customer identifiers | checkout/renew/cancel; bad signature denied; retries idempotent | Stripe test, production-authorized | SG2 / blocked |
| S3 | API -> PostgreSQL | migrations, ownership, backups | customer health/profile data | CRUD/ownership; cross-user denied; restore proves recovery | staging, production realm | SG3 / blocked |
| S4 | Provider form -> review queue | privacy-minimal request + status/SLA | contact/org PII | durable intake; overlarge/abusive input denied; notification failure visible | staging | SG4 / blocked |
| S5 | GitHub Actions -> Azure | SHA images and app update | OIDC/secrets | tests/build/deploy/smoke; gate failure prevents mutation; rollback works | CI, production | SG5 / failing |
| S6 | Product -> analytics/support | privacy-safe events + escalation | metadata, no health payload | events/support route work; sensitive data excluded; backend outage observable | staging, production | SG6 / blocked |

All surfaces are release-blocking.
