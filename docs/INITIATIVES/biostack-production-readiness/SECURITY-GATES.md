# Security Gates

| Gate | Surface/threat | Required evidence | Status | Blocking |
|---|---|---|---|---|
| SG1 | Auth/session/ownership bypass | live auth flow, deny tests, ownership isolation review | blocked | yes |
| SG2 | Billing forgery/replay/secrets | webhook signature/idempotency tests, secret/config review | blocked | yes |
| SG3 | Sensitive data loss/cross-user access | authz review, backup encryption/restore and redaction proof | blocked | yes |
| SG4 | Provider PII abuse/retention | rate-limit, minimization, access, retention and deletion review | blocked | yes |
| SG5 | Supply chain/deployment credentials | required scans, OIDC/RBAC, immutable artifact and rollback review | blocked | yes |
| SG6 | Logging/analytics leakage | structured logging and telemetry payload review | blocked | yes |
| SG7 | Public safety/legal claims | non-prescriptive copy, legal/privacy and consent approval | blocked | yes |

A dedicated defensive security review is required before release. No waiver exists. Waivers require named authority, scope, expiry, rationale, compensating controls, and release-owner acceptance.
